using UnityEngine;
using static SoftKit;

/// v0.4.2 rideable toy scooter (小綿羊). [E] near it -> ride (faster, noisy putt-putt; chasing pets lose you at speed),
/// can't ride into houses/shops and can't loot while riding -> [E] to hop off. V toggles third/first person.
public class Moto : Interactable
{
    public static Moto Cur; public static int Mounts, Escapes; public static float AutoThrottle, AutoSteer; public static bool Auto, FirstPerson;
    public const float MaxSpd = 9.5f;
    public float speed; Transform model, rider, riderHead; BoxCollider box; float escT, yaw, vy, steerVis, noiseT, toastT, putT, origR; Color col;
    public bool Riding => Cur == this;
    public override string Prompt => Riding ? "[E] 下車（下車才能翻東西、進屋）　V 換視角" : "[E] 騎上小綿羊";
    public override Vector3 Point => transform.position + Vector3.up * 0.8f;

    static Quaternion Q(float x, float y, float z) => Quaternion.Euler(x, y, z);
    static Color Dk(Color c, float k = 0.8f) => new Color(c.r * k, c.g * k, c.b * k, 1);
    static Color Lt(Color c, float k = 0.35f) => Color.Lerp(c, Color.white, k);
    static Color H(string hex) { ColorUtility.TryParseHtmlString("#" + hex, out var c); return c; }
    public static Moto Build(Transform root, Vector3 pos, float yaw, Color c)
    {
        var g = Group(root, "RideScooter", pos, yaw); var m = g.gameObject.AddComponent<Moto>(); m.col = c; m.reach = 2.6f;
        var md = Group(g, "model", Vector3.zero); m.model = md; var dark = Plum;
        // chunky toy proportions: fat wheels, bubble body, big round headlight face
        foreach (float z in new[] { -0.7f, 0.72f }) { Cyl(md, new Vector3(0, 0.34f, z), 0.34f, 0.26f, Dk(dark), 0.12f, Q(0, 0, 90), 20); Cyl(md, new Vector3(0, 0.34f, z), 0.15f, 0.3f, Butter, 0.06f, Q(0, 0, 90), 14); }
        Block(md, new Vector3(0, 0.5f, 0.02f), new Vector3(0.48f, 0.2f, 1.0f), c, 0.1f, false);                 // floor board
        Ball(md, new Vector3(0, 0.75f, -0.5f), new Vector3(0.7f, 0.62f, 1.0f), c, 18);                           // bubble rear body
        Ball(md, new Vector3(0, 1.02f, -0.45f), new Vector3(0.5f, 0.2f, 0.78f), Cream, 14);                      // seat pillow
        Block(md, new Vector3(0, 0.92f, 0.55f), new Vector3(0.56f, 1.0f, 0.26f), c, 0.13f, false, 0, -1, 0.25f); // leg shield
        Ball(md, new Vector3(0, 0.6f, 0.74f), new Vector3(0.44f, 0.36f, 0.55f), Lt(c), 14);                     // front fender
        Cyl(md, new Vector3(0, 1.42f, 0.58f), 0.05f, 0.4f, Grey);
        Cyl(md, new Vector3(0, 1.6f, 0.56f), 0.05f, 0.82f, Grey, 0.025f, Q(0, 0, 90), 10);
        foreach (int s in new[] { -1, 1 }) Ball(md, new Vector3(s * 0.42f, 1.6f, 0.56f), new Vector3(0.12f, 0.12f, 0.16f), dark, 8);
        Ball(md, new Vector3(0, 1.52f, 0.66f), new Vector3(0.42f, 0.34f, 0.26f), c, 14);                         // head nacelle
        Ball(md, new Vector3(0, 1.52f, 0.79f), new Vector3(0.24f, 0.24f, 0.06f), Butter, 12);                    // headlight
        foreach (int s in new[] { -1, 1 }) { Ball(md, new Vector3(s * 0.2f, 1.82f, 0.5f), new Vector3(0.14f, 0.14f, 0.05f), White, 8); Cyl(md, new Vector3(s * 0.17f, 1.72f, 0.52f), 0.015f, 0.16f, Grey); } // mirrors
        Ball(md, new Vector3(0, 0.85f, -1.03f), new Vector3(0.22f, 0.14f, 0.08f), Rose, 10);                    // tail light
        Ball(md, new Vector3(0.36f, 0.8f, -0.5f), new Vector3(0.05f, 0.22f, 0.22f), White, 10);                  // heart decal-ish dots
        Ball(md, new Vector3(-0.36f, 0.8f, -0.5f), new Vector3(0.05f, 0.22f, 0.22f), White, 10);
        // rider (kid with a round helmet) — scaled to 0 while parked
        var rd = Group(g, "rider", Vector3.zero); m.rider = rd;
        Ball(rd, new Vector3(0, 1.4f, -0.35f), new Vector3(0.46f, 0.6f, 0.36f), Sky, 14);                        // body (hoodie)
        foreach (int s in new[] { -1, 1 })
        {
            var arm = Cyl(rd, new Vector3(s * 0.25f, 1.52f, 0.1f), 0.065f, 0.62f, Sky, 0.03f, Q(70, 0, s * -12), 10);
            Ball(rd, new Vector3(s * 0.36f, 1.6f, 0.47f), Vector3.one * 0.12f, H("ffd2b0"), 8);                   // hands on grips
            var leg = Cyl(rd, new Vector3(s * 0.15f, 1.0f, -0.05f), 0.08f, 0.5f, LavD, 0.04f, Q(80, 0, 0), 10);
            Ball(rd, new Vector3(s * 0.15f, 0.66f, 0.2f), new Vector3(0.14f, 0.1f, 0.22f), Coral, 8);              // shoes on the board
        }
        var head = Group(rd, "head", new Vector3(0, 1.93f, -0.3f)); m.riderHead = head;
        Ball(head, Vector3.zero, Vector3.one * 0.38f, H("ffd2b0"), 14);
        Ball(head, new Vector3(0, 0.07f, -0.02f), new Vector3(0.46f, 0.36f, 0.46f), Butter, 16);                // helmet
        Block(head, new Vector3(0, 0.0f, 0.2f), new Vector3(0.36f, 0.05f, 0.08f), ButterD, 0.02f, false);       // visor
        rd.localScale = Vector3.zero;
        m.box = g.gameObject.AddComponent<BoxCollider>(); m.box.size = new Vector3(0.8f, 1.5f, 2.1f); m.box.center = new Vector3(0, 0.75f, 0);
        return m;
    }

    public override void Use()
    {
        var p = Player.I; if (!p || !p.alive) return;
        if (Riding) { Dismount(); return; }
        if (Cur || SlideRide.Busy || Player.Riding) return;
        Cur = this; Mounts++; speed = 0; vy = 0; yaw = transform.eulerAngles.y;
        box.enabled = false; rider.localScale = Vector3.one;
        origR = p.cc.radius; p.Teleport(new Vector3(transform.position.x, transform.position.y + 0.08f, transform.position.z), yaw);
        p.cc.radius = 0.55f; Player.Riding = true; if (p.hand) p.hand.gameObject.SetActive(false);
        Sfx.Play("confirmation_001", 0.5f, 1.3f); Pet.Noise(transform.position, 6f);
    }
    public void SetHeading(float y) { yaw = y; if (Player.I) Player.I.yaw = y; }
    public void Dismount()
    {
        var p = Player.I; if (!Riding || !p) return;
        var fwd = Quaternion.Euler(0, yaw, 0) * Vector3.forward; var right = Quaternion.Euler(0, yaw, 0) * Vector3.right;
        var basePos = p.transform.position; p.cc.enabled = false; Vector3 spot = basePos + right * 1.0f; bool ok = false;
        foreach (var off in new[] { -right * 1.0f, right * 1.0f, -fwd * 1.7f, fwd * 1.7f, -right * 1.5f, right * 1.5f })
        {
            var sp = basePos + off;
            if (Physics.CheckCapsule(sp + Vector3.up * 0.35f, sp + Vector3.up * 1.05f, 0.3f, Art.ShotMask, QueryTriggerInteraction.Ignore)) continue;
            spot = sp; ok = true; break;
        }
        if (!ok) spot = basePos;   // nowhere free: step off in place (scooter is moved half a metre forward below)
        transform.position = (ok ? basePos : basePos + fwd * 0.6f) - Vector3.up * 0.08f; transform.rotation = Quaternion.Euler(0, yaw, 0);
        model.localRotation = Quaternion.identity; rider.localScale = Vector3.zero; box.enabled = true; speed = 0;
        Cur = null; p.cc.radius = origR > 0 ? origR : 0.3f; p.cc.enabled = true; p.Teleport(spot, yaw); ResetCam(p); Player.Riding = false;
        Sfx.Play("impactSoft_heavy_000", 0.4f, 1.4f);
    }
    static void ResetCam(Player p) { p.camRig.localPosition = new Vector3(0, 1.25f, 0); p.camRig.localRotation = Quaternion.identity; if (p.cam) { p.cam.transform.localPosition = Vector3.zero; p.cam.transform.localRotation = Quaternion.identity; p.cam.fieldOfView = 75; } }
    public static bool InNoRide(Vector3 w) { foreach (var b in LevelGen.NoRide) if (b.Contains(new Vector3(w.x, b.center.y, w.z))) return true; return false; }

    void Update()
    {
        if (!Riding) return;
        var p = Player.I; if (!p || !p.alive) { Dismount(); return; }
        float dt = Time.deltaTime; bool canAct = Game.I && !Game.I.InputLocked && p.controlEnabled;
        float thr = 0, st = 0;
        if (canAct)
        {
            thr = Auto ? AutoThrottle : Input.GetAxisRaw("Vertical"); st = Auto ? AutoSteer : Input.GetAxisRaw("Horizontal");
            if (!Auto) { yaw += Input.GetAxisRaw("Mouse X") * p.Sens * 0.6f; p.pitch = Mathf.Clamp(p.pitch - Input.GetAxisRaw("Mouse Y") * p.Sens, -30, 40); }
            if (Input.GetKeyDown(KeyCode.V)) FirstPerson = !FirstPerson;
        }
        float target = thr > 0 ? thr * MaxSpd : thr < 0 ? (speed > 0.5f ? 0 : thr * 2.5f) : 0;
        float acc = thr < 0 && speed > 0.5f ? 14f : (Mathf.Abs(target) > Mathf.Abs(speed) ? 6f : 4f);
        speed = Mathf.MoveTowards(speed, target, acc * dt);
        yaw += st * 100f * dt * Mathf.Clamp(Mathf.Abs(speed) / 3f, 0.35f, 1f) * (speed < -0.1f ? -1 : 1);
        var fwd = Quaternion.Euler(0, yaw, 0) * Vector3.forward;
        var pos = p.transform.position;
        if (Mathf.Abs(speed) > 0.05f && InNoRide(pos + fwd * Mathf.Sign(speed) * 1.3f + fwd * speed * dt))
        {   // houses/shops are no-ride zones: bump + hint
            speed = 0; if (Time.time > toastT && Game.I) { toastT = Time.time + 2.5f; Game.I.Toast("小綿羊不能騎進屋子喔！按 E 下車再走進去。"); Sfx.Play("error_004", 0.4f); }
        }
        if (p.cc.isGrounded) vy = -1f; else vy -= 14f * dt;
        var flags = p.cc.Move(fwd * speed * dt + Vector3.up * vy * dt);
        if ((flags & CollisionFlags.Sides) != 0 && Mathf.Abs(speed) > 2f) { speed *= 0.4f; p.Shake(0.35f); Sfx.Play("impactSoft_heavy_000", 0.35f, 1.2f); }
        p.yaw = yaw; p.transform.rotation = Quaternion.Euler(0, yaw, 0);
        // putt-putt noise vs. speed escape
        noiseT -= dt; if (noiseT <= 0 && p.inRaid) { noiseT = 0.6f; Pet.Noise(p.transform.position, 4.5f + Mathf.Abs(speed) * 0.45f); }
        putT -= dt; if (putT <= 0 && Mathf.Abs(speed) > 0.5f) { putT = Mathf.Lerp(0.34f, 0.14f, Mathf.Abs(speed) / MaxSpd); Sfx.Play("click_002", 0.12f, 0.6f + Mathf.Abs(speed) * 0.05f); }
        if (Mathf.Abs(speed) > 6f)
            foreach (var pet in Pet.All)
                if (pet && (pet.st == Pet.St.Chase || pet.st == Pet.St.Notice) && (pet.transform.position - p.transform.position).magnitude > 3f)
                { pet.LoseTrack(); Escapes++; if (Game.I && Time.time > escT) { escT = Time.time + 4f; Game.I.Toast("咻～騎太快，" + (pet.dog ? "狗狗" : "貓咪") + "追不上了！"); } }
        steerVis = Mathf.Lerp(steerVis, st * Mathf.Clamp01(Mathf.Abs(speed) / 4f), dt * 6);
    }

    void LateUpdate()
    {
        if (!Riding) return; var p = Player.I; if (!p) return;
        transform.position = p.transform.position - Vector3.up * 0.08f; transform.rotation = Quaternion.Euler(0, yaw, 0);
        model.localRotation = Quaternion.Euler(0, 0, -steerVis * 12f); rider.localRotation = model.localRotation;
        rider.localScale = Vector3.one; riderHead.localScale = FirstPerson ? Vector3.zero : Vector3.one;
        var cam = p.cam; float sp01 = Mathf.Clamp01(Mathf.Abs(speed) / MaxSpd);
        cam.transform.localPosition = Vector3.zero; cam.transform.localRotation = Quaternion.identity;
        if (FirstPerson)
        {
            p.camRig.localPosition = new Vector3(0, 2.0f, -0.25f); p.camRig.localRotation = Quaternion.Euler(p.pitch + 6, 0, -steerVis * 6);
        }
        else
        {   // chase cam behind + above; pulled in if a wall is in the way
            var piv = p.transform.position + Vector3.up * 1.7f; var rot = Quaternion.Euler(14 + p.pitch * 0.5f, yaw, 0);
            var want = piv - rot * Vector3.forward * (3.6f + sp01 * 0.6f) + Vector3.up * 0.3f; var d = want - piv; float dist = d.magnitude;
            if (Physics.SphereCast(piv, 0.22f, d.normalized, out var h, dist, Art.ShotMask, QueryTriggerInteraction.Ignore) && !h.collider.transform.IsChildOf(transform) && !h.collider.transform.IsChildOf(p.transform)) dist = Mathf.Max(0.4f, h.distance - 0.1f);
            p.camRig.position = piv + d.normalized * dist; p.camRig.rotation = Quaternion.LookRotation(piv + rot * Vector3.forward * 2f - p.camRig.position);
        }
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, 72 + sp01 * 14, Time.deltaTime * 5);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        if (Riding) { Cur = null; Player.Riding = false; var p = Player.I; if (p) { p.cc.radius = origR > 0 ? origR : 0.3f; p.cc.enabled = true; ResetCam(p); } }
    }
}
