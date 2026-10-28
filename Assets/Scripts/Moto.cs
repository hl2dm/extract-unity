using UnityEngine;
using static SoftKit;

/// v0.4.2 rideable toy scooter (小綿羊). [E] near it -> ride (faster, noisy putt-putt; chasing pets lose you at speed),
/// can't ride into houses/shops and can't loot while riding -> [E] to hop off. V toggles third/first person.
public class Moto : Interactable
{
    public static Moto Cur; public static int Mounts, Escapes; public static float AutoThrottle, AutoSteer; public static bool Auto, FirstPerson, CamFree;
    public const float MaxSpd = 9.5f;
    public float speed; Transform model, rider, riderHead; BoxCollider box; float escT, yaw, vy, steerVis, noiseT, toastT, putT, origR; Color col;
    public bool Riding => Cur == this;
    public override string Prompt => Riding ? "[E] 下車（下車才能翻東西、進屋）　V 換視角" : "[E] 騎上小綿羊";
    public override Vector3 Point => transform.position + Vector3.up * 0.8f;

    static Quaternion Q(float x, float y, float z) => Quaternion.Euler(x, y, z);
    static Color Dk(Color c, float k = 0.8f) => new Color(c.r * k, c.g * k, c.b * k, 1);
    static Color Lt(Color c, float k = 0.35f) => Color.Lerp(c, Color.white, k);
    static Color H(string hex) { ColorUtility.TryParseHtmlString("#" + hex, out var c); return c; }
    static GameObject Seg(Transform p, Vector3 a, Vector3 b, float r, Color c)
    { var d = b - a; return Cyl(p, (a + b) / 2, r, d.magnitude + r, c, r * 0.95f, Quaternion.FromToRotation(Vector3.up, d), 12); }
    static System.Collections.Generic.List<Vector2> Dome(float R)
    { var l = new System.Collections.Generic.List<Vector2>(); l.Add(new Vector2(R * 1.02f, -0.02f)); for (int i = 0; i <= 8; i++) { float a = Mathf.PI * 0.5f * i / 8; l.Add(new Vector2(R * Mathf.Cos(a), R * Mathf.Sin(a) + 0.02f)); } l[l.Count - 1] = new Vector2(0, R + 0.02f); return l; }
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
        Ball(md, new Vector3(0, 0.6f, 0.76f), new Vector3(0.42f, 0.34f, 0.55f), Cream, 14);                     // front fender (separate cream form)
        Ring(md, new Vector3(0, 1.42f, 0.58f), Q(80, 0, 0), 0.2f, 0.035f, Cream, 24);                              // seam between shield and nacelle
        Cyl(md, new Vector3(0, 1.38f, 0.58f), 0.075f, 0.4f, Lt(c, 0.5f), 0.03f, null, 14);                       // chunky stem
        Cyl(md, new Vector3(0, 1.6f, 0.56f), 0.06f, 0.78f, Cream, 0.03f, Q(0, 0, 90), 14);                         // handlebar
        foreach (int s in new[] { -1, 1 })
        {
            Cyl(md, new Vector3(s * 0.43f, 1.6f, 0.56f), 0.085f, 0.2f, dark, 0.04f, Q(0, 0, 90), 14);             // fat rubber grips
            Ball(md, new Vector3(s * 0.54f, 1.6f, 0.56f), Vector3.one * 0.11f, Rose, 10);                          // grip caps
            Lath(md, new Vector3(s * 0.22f, 1.62f, 0.54f), new[] { new Vector2(0, 0), new Vector2(0.06f, 0.01f), new Vector2(0.045f, 0.06f), new Vector2(0.025f, 0.16f), new Vector2(0, 0.17f) }, Lt(c, 0.5f), 12, false, Q(-8, 0, s * -18), "mirror_stalk"); // tapered cone stalk
            Ring(md, new Vector3(s * 0.28f, 1.84f, 0.52f), Q(0, 0, 0), 0.065f, 0.028f, Lt(c, 0.2f), 20);          // mirror rim
            Ball(md, new Vector3(s * 0.28f, 1.84f, 0.51f), new Vector3(0.13f, 0.13f, 0.05f), Sky, 12);             // mirror glass
        }
        Ball(md, new Vector3(0, 1.52f, 0.66f), new Vector3(0.46f, 0.36f, 0.3f), c, 18);                         // head nacelle
        Ring(md, new Vector3(0, 1.52f, 0.8f), Q(0, 0, 0), 0.13f, 0.035f, Cream, 28);                             // headlight bezel
        Ball(md, new Vector3(0, 1.52f, 0.8f), new Vector3(0.24f, 0.24f, 0.07f), Butter, 14);                     // headlight
        Ball(md, new Vector3(0.05f, 1.56f, 0.835f), new Vector3(0.07f, 0.05f, 0.02f), White, 8);                // headlight glint
        foreach (int s in new[] { -1, 1 }) Ball(md, new Vector3(s * 0.2f, 1.45f, 0.78f), new Vector3(0.08f, 0.06f, 0.04f), ButterD, 8); // blinkers
        { var bk = Lath(md, new Vector3(0, 0.88f, 0.98f), new[] { new Vector2(0, 0), new Vector2(0.14f, 0.0f), new Vector2(0.2f, 0.04f), new Vector2(0.22f, 0.14f), new Vector2(0.23f, 0.26f), new Vector2(0.2f, 0.27f), new Vector2(0.19f, 0.06f), new Vector2(0, 0.05f) }, Wood, 20, false, null, "basket"); bk.transform.localScale = new Vector3(1, 1, 0.75f);
          var rim = Ring(md, new Vector3(0, 1.15f, 0.98f), Q(90, 0, 0), 0.215f, 0.035f, WoodD, 24); rim.transform.localScale = new Vector3(1, 0.75f, 1);
          Ball(md, new Vector3(0.05f, 1.16f, 0.98f), new Vector3(0.18f, 0.14f, 0.14f), Leaf, 10); Ball(md, new Vector3(-0.08f, 1.2f, 0.94f), Vector3.one * 0.1f, Coral, 8); }   // round wicker hamper with veggies
        Ring(md, new Vector3(0, 0.88f, -1.0f), Q(0, 0, 0), 0.13f, 0.035f, Cream, 18);                             // tail light bezel
        Ball(md, new Vector3(0, 0.88f, -1.02f), new Vector3(0.24f, 0.24f, 0.1f), H("ff5f73"), 12);                  // round red tail light
        Ball(md, new Vector3(0.04f, 0.92f, -1.07f), Vector3.one * 0.05f, White, 6);
        { var rg = Ring(md, new Vector3(0, 0.7f, -0.5f), Q(90, 0, 0), 1f, 0.03f, Cream, 36); rg.transform.localScale = new Vector3(0.355f, 0.5f, 1f); } // side ridge line
        Block(md, new Vector3(0, 0.62f, -1.06f), new Vector3(0.3f, 0.16f, 0.04f), Cream, 0.03f, false);          // plate
        foreach (int s in new[] { -1, 1 })
        {   // side flower decals + chrome-free bumper rail
            Ball(md, new Vector3(s * 0.355f, 0.8f, -0.5f), new Vector3(0.05f, 0.2f, 0.2f), White, 12);
            Ball(md, new Vector3(s * 0.37f, 0.8f, -0.5f), new Vector3(0.04f, 0.08f, 0.08f), ButterD, 8);
        }
        Ring(md, new Vector3(0, 1.05f, -0.92f), Q(90, 0, 0), 0.16f, 0.025f, Cream, 20);                          // grab rail
        // rider: chibi kid — big head, round hoodie, backpack, mittens, chunky shoes. Scaled to 0 while parked.
        var rd = Group(g, "rider", Vector3.zero); m.rider = rd; var skin = H("ffd6b8"); var ink = H("4a3540");
        Ball(rd, new Vector3(0, 1.2f, -0.42f), new Vector3(0.4f, 0.24f, 0.36f), LavD, 14);                      // shorts
        Ball(rd, new Vector3(0, 1.46f, -0.33f), new Vector3(0.46f, 0.5f, 0.38f), Sky, 18, false, Q(10, 0, 0));   // hoodie torso
        Ball(rd, new Vector3(0, 1.38f, -0.15f), new Vector3(0.26f, 0.12f, 0.06f), SkyD, 10);                     // pocket
        Ring(rd, new Vector3(0, 1.68f, -0.3f), Q(90, 0, 0), 0.12f, 0.045f, SkyD, 20);                             // hood collar
        Block(rd, new Vector3(0, 1.52f, -0.62f), new Vector3(0.46f, 0.5f, 0.26f), Coral, 0.2f, false);             // backpack upper lobe
        Ball(rd, new Vector3(0, 1.27f, -0.66f), new Vector3(0.4f, 0.22f, 0.26f), Dk(Coral, 0.9f), 12);              // lower lobe
        Ring(rd, new Vector3(0, 1.8f, -0.62f), Q(0, 90, 0), 0.07f, 0.025f, ButterD, 14);                           // top strap loop
        Block(rd, new Vector3(0, 1.66f, -0.74f), new Vector3(0.48f, 0.2f, 0.1f), Butter, 0.06f, false);           // flap
        Ball(rd, new Vector3(0, 1.4f, -0.77f), new Vector3(0.22f, 0.22f, 0.05f), Mint, 12);                       // round pocket badge
        Ball(rd, new Vector3(0, 1.4f, -0.8f), new Vector3(0.09f, 0.09f, 0.03f), White, 8);
        foreach (int s in new[] { -1, 1 }) Ball(rd, new Vector3(s * 0.25f, 1.48f, -0.62f), new Vector3(0.06f, 0.3f, 0.18f), ButterD, 8); // side pockets
        foreach (int s in new[] { -1, 1 })
        {
            Seg(rd, new Vector3(s * 0.22f, 1.6f, -0.28f), new Vector3(s * 0.4f, 1.6f, 0.5f), 0.095f, Sky); Ball(rd, new Vector3(s * 0.31f, 1.6f, 0.1f), Vector3.one * 0.2f, Sky, 10); // elbow         // sleeve
            Ring(rd, new Vector3(s * 0.38f, 1.6f, 0.4f), Q(0, s * 12, 0), 0.07f, 0.03f, SkyD, 16);                 // cuff
            Ball(rd, new Vector3(s * 0.42f, 1.64f, 0.56f), new Vector3(0.21f, 0.19f, 0.2f), Butter, 12); Ball(rd, new Vector3(s * 0.34f, 1.7f, 0.6f), new Vector3(0.09f, 0.11f, 0.09f), Butter, 8); Ring(rd, new Vector3(s * 0.41f, 1.63f, 0.46f), Q(0, 0, 0), 0.085f, 0.03f, ButterD, 14); // mitten + thumb + cuff         // mittens on grips
            Seg(rd, new Vector3(s * 0.13f, 1.17f, -0.36f), new Vector3(s * 0.16f, 1.12f, 0.02f), 0.11f, LavD); Ball(rd, new Vector3(s * 0.16f, 1.12f, 0.02f), Vector3.one * 0.21f, LavD, 10); // knee    // thigh
            Seg(rd, new Vector3(s * 0.16f, 1.12f, 0.02f), new Vector3(s * 0.16f, 0.74f, 0.12f), 0.085f, skin);      // shin
            Ball(rd, new Vector3(s * 0.16f, 0.68f, 0.18f), new Vector3(0.17f, 0.13f, 0.27f), Coral, 12);          // shoe
            Ball(rd, new Vector3(s * 0.16f, 0.63f, 0.18f), new Vector3(0.18f, 0.05f, 0.28f), White, 12);          // sole
        }
        var head = Group(rd, "head", new Vector3(0, 2.0f, -0.26f)); m.riderHead = head;
        Ball(head, Vector3.zero, new Vector3(0.56f, 0.52f, 0.52f), skin, 20);                                     // big round head
        Lath(head, Vector3.zero, Dome(0.31f), Butter, 24, false, Q(-12, 0, 0), "helmet");                          // helmet dome
        Ring(head, new Vector3(0, 0.05f, 0), Q(78, 0, 0), 0.3f, 0.035f, Coral, 32);                                // helmet stripe
        Ball(head, new Vector3(0, 0.29f, -0.04f), new Vector3(0.12f, 0.08f, 0.12f), Coral, 10);                   // helmet button
        Ball(head, new Vector3(0, 0.12f, 0.25f), new Vector3(0.34f, 0.04f, 0.12f), ButterD, 14, false, Q(-10, 0, 0)); // visor brim
        foreach (int s in new[] { -1, 1 })
        {
            Ball(head, new Vector3(s * 0.105f, 0.0f, 0.24f), new Vector3(0.07f, 0.1f, 0.035f), ink, 10);       // eyes
            Ball(head, new Vector3(s * 0.105f + 0.015f, 0.025f, 0.262f), Vector3.one * 0.028f, White, 6);
            Ball(head, new Vector3(s * 0.175f, -0.07f, 0.21f), new Vector3(0.08f, 0.045f, 0.03f), H("ffaabb"), 8, false, Q(0, s * 30, 0)); // blush
            Ball(head, new Vector3(s * 0.28f, -0.02f, -0.02f), new Vector3(0.06f, 0.1f, 0.1f), skin, 8);          // ears
        }
        var hair = H("7a5040");
        Ball(head, new Vector3(0, -0.06f, -0.17f), new Vector3(0.5f, 0.34f, 0.3f), hair, 16);                     // bob hair at the back
        for (int i = -2; i <= 2; i++) Ball(head, new Vector3(i * 0.075f, 0.09f, 0.21f - Mathf.Abs(i) * 0.02f), new Vector3(0.11f, 0.08f, 0.07f), hair, 8); // bangs under the visor
        foreach (int s in new[] { -1, 1 }) Ball(head, new Vector3(s * 0.25f, -0.12f, -0.12f), new Vector3(0.12f, 0.16f, 0.12f), hair, 8); // side tufts
        Ball(head, new Vector3(0, -0.04f, 0.27f), Vector3.one * 0.03f, H("f0a08a"), 6); Ring(head, new Vector3(0, -0.1f, 0.25f), Q(0, 0, 0), 0.045f, 0.013f, H("c95468"), 14).transform.localScale = new Vector3(1, 0.7f, 1); Ball(head, new Vector3(0, -0.125f, 0.25f), new Vector3(0.06f, 0.03f, 0.02f), H("d9667a"), 8); // open smile
        foreach (int s in new[] { -1, 1 }) Ball(head, new Vector3(s * 0.105f, 0.075f, 0.25f), new Vector3(0.07f, 0.018f, 0.02f), H("7a5040"), 6, false, Q(0, 0, s * -10)); // brows            // smile
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
        if (CamFree) return;
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
