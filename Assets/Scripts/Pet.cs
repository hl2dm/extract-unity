using System.Linq;
using System.Collections.Generic;
using UnityEngine;

/// CASUAL: cats & dogs. Patrol / Sleep / Investigate (noise) / Notice / Chase / Flee (with stolen loot) / Lured (eating).
public class Pet : MonoBehaviour
{
    public enum St { Patrol, Sleep, Investigate, Notice, Chase, Flee, Lured }
    public static readonly List<Pet> All = new List<Pet>();
    public bool dog; public string pname; public St st = St.Patrol; public float speed, chaseSpeed, sight; public List<Vector3> patrol = new List<Vector3>();
    public bool sleeper; public Item carried; public int catches;
    CharacterController cc; Animation anim; readonly Dictionary<string, string> clips = new Dictionary<string, string>(); string cur;
    int wp; float stT, lostT, cool, idleT, barkT, stuckT; Vector3 target, lastPos; Lure lure; Transform icon; TextMesh mark; GameObject zzz;
    public static float FreezeAll; // autotest: >0 pets ignore the player

    Transform model; float fitH;
    // chibi proportions: bigger head, shorter legs (re-applied after animation every frame)
    Transform headB; Vector3 headS; readonly System.Collections.Generic.List<Transform> legB = new System.Collections.Generic.List<Transform>(); readonly System.Collections.Generic.List<Vector3> legS = new System.Collections.Generic.List<Vector3>();
    float HeadK => dog ? 1.6f : 1.5f; float LegK => dog ? 0.5f : 0.68f;
    void Chibify()
    {
        var bones = model.GetComponentsInChildren<Transform>().Where(t => t != model && !t.name.ToLower().EndsWith("_end") && !t.name.ToLower().StartsWith("ik") && t.GetComponent<Renderer>() == null).ToList();
        if (bones.Count < 4) { Debug.Log("[pet] " + name + " chibi: no rig"); return; }
        headB = bones.FirstOrDefault(t => t.name.ToLower().Contains("head") && !t.name.ToLower().Contains("ik"));
        var lp = bones.Select(t => transform.InverseTransformPoint(t.position)).ToList();
        float minY = lp.Min(v => v.y), maxY = lp.Max(v => v.y), hipY = minY + (maxY - minY) * 0.45f;
        if (!headB) { int hi = 0; for (int i = 1; i < bones.Count; i++) if (lp[i].z + lp[i].y > lp[hi].z + lp[hi].y) hi = i; headB = bones[hi].parent && bones.Contains(bones[hi].parent) ? bones[hi].parent : bones[hi]; }
        if (headB) headS = headB.localScale;
        foreach (var f in bones.Where((t, i) => lp[i].y < minY + (maxY - minY) * 0.12f))
        {
            var r = f; while (r.parent && bones.Contains(r.parent) && transform.InverseTransformPoint(r.parent.position).y < hipY) r = r.parent;
            if (r != f && !legB.Contains(r) && !legB.Any(l => r.IsChildOf(l) || l.IsChildOf(r))) { legB.Add(r); legS.Add(r.localScale); }
        }
        Debug.Log("[pet] " + name + " chibi head=" + (headB ? headB.name : "none") + " legs=" + legB.Count + " headK=" + HeadK + " legK=" + LegK);
        ApplyChibi();
    }
    void ApplyChibi() { if (headB) headB.localScale = headS * HeadK; for (int i = 0; i < legB.Count; i++) if (legB[i]) legB[i].localScale = legS[i] * LegK; }
    void LateUpdate() { if (model) ApplyChibi(); }
    Bounds RB() { bool f = false; var b = new Bounds(transform.position, Vector3.zero); foreach (var r in model.GetComponentsInChildren<Renderer>()) { if (!f) { b = r.bounds; f = true; } else b.Encapsulate(r.bounds); } return b; }
    System.Collections.IEnumerator Fit()
    {
        yield return null; yield return null; if (!model) yield break;
        // facing: the head bone must sit on the root's +Z side (walk direction = transform.forward)
        // Quaternius rigs have generic "Bone.0xx" names but IKFront*/IKBack* leg targets: front-minus-back gives the body axis.
        Vector3 fs = Vector3.zero, bs = Vector3.zero; int fn = 0, bn = 0;
        foreach (var t in model.GetComponentsInChildren<Transform>()) { var n = t.name.ToLower(); if (n.EndsWith("_end")) continue;
            if (n.Contains("front") || n.Contains("head")) { fs += transform.InverseTransformPoint(t.position); fn++; }
            else if (n.Contains("back") || n.Contains("hip") || n.Contains("tail")) { bs += transform.InverseTransformPoint(t.position); bn++; } }
        if (fn > 0 && bn > 0)
        {
            var dl = fs / fn - bs / bn; float yaw = Mathf.Atan2(dl.x, dl.z) * Mathf.Rad2Deg;
            if (Mathf.Abs(yaw) > 30f) model.RotateAround(transform.position, Vector3.up, -yaw);
            Debug.Log("[pet] " + name + " facing front/back=" + fn + "/" + bn + " yawErr=" + yaw.ToString("F0") + (Mathf.Abs(yaw) > 30f ? " corrected" : " ok"));
        }
        else Debug.Log("[pet] " + name + " facing: no front/back markers");
        Chibify(); yield return null; if (!model) yield break;
        var b = RB(); if (b.size.y > 1e-4f) model.localScale *= fitH / b.size.y;
        yield return null; yield return null; if (!model) yield break; b = RB();
        model.position += new Vector3(transform.position.x - b.center.x, transform.position.y - b.min.y, transform.position.z - b.center.z);
        Debug.Log("[pet] " + name + " fit size " + b.size.ToString("F2"));
    }
    static Bounds SkinBounds(GameObject g)
    {
        bool f = false; Bounds b = new Bounds(g.transform.position, Vector3.zero); var tmp = new Mesh();
        foreach (var r in g.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            if (!r.sharedMesh) continue; r.BakeMesh(tmp, false); var M = r.transform.localToWorldMatrix;
            foreach (var v in tmp.vertices) { var w = M.MultiplyPoint3x4(v); if (!f) { b = new Bounds(w, Vector3.zero); f = true; } else b.Encapsulate(w); }
        }
        Destroy(tmp); return f ? b : Art.RenderBounds(g);
    }
    public static Pet Create(Transform parent, Vector3 pos, bool dog, string name)
    {
        var root = new GameObject(dog ? "Dog" : "Cat"); root.transform.SetParent(parent, false); root.transform.position = pos; root.layer = 9;
        var p = root.AddComponent<Pet>(); p.dog = dog; p.pname = name;
        p.speed = dog ? 2.1f : 1.7f; p.chaseSpeed = dog ? 5.2f : 5.6f; p.sight = dog ? 11f : 9f;
        var pf = Art.Model(dog ? "Pets/Dog" : "Pets/Cat"); float h = dog ? 0.75f : 0.55f;
        if (pf)
        {
            var m = Instantiate(pf, root.transform); m.transform.localPosition = Vector3.zero; m.transform.localRotation = Quaternion.identity; foreach (var smr0 in m.GetComponentsInChildren<SkinnedMeshRenderer>()) { smr0.updateWhenOffscreen = true; if (smr0.sharedMesh) smr0.sharedMesh = PetSmooth.Smooth(smr0.sharedMesh); }
            { var an0 = m.GetComponentInChildren<Animation>(); if (an0) { foreach (AnimationState a0 in an0) { an0.Play(a0.name); break; } an0.Sample(); } }
            var b = Art.RenderBounds(m); float s = h / Mathf.Max(0.01f, b.size.y); m.transform.localScale *= s; b = Art.RenderBounds(m);
            // models face +Z? if the longest horizontal axis is X, turn it.
            m.transform.localRotation = Quaternion.Euler(0, 180, 0); // model faces -Z
            m.transform.position += new Vector3(pos.x - b.center.x, pos.y - b.min.y, pos.z - b.center.z); p.model = m.transform; p.fitH = h; p.StartCoroutine(p.Fit());
            p.anim = m.GetComponentInChildren<Animation>();
            if (p.anim) { var names = new List<string>(); foreach (AnimationState a in p.anim) { names.Add(a.name); var k = a.name.Contains("|") ? a.name.Substring(a.name.LastIndexOf('|') + 1) : a.name; p.clips[k.ToLower()] = a.name; } Debug.Log("[pet] " + root.name + " clips: " + string.Join(",", names)); }
            else Debug.Log("[pet] " + root.name + " has no Animation component");
            foreach (var smr in m.GetComponentsInChildren<SkinnedMeshRenderer>()) smr.updateWhenOffscreen = true;
            Art.Toonify(m, 0.006f); { var tint = dog ? Data.H("c98b4a") : Data.H("f2a65a"); foreach (var r in m.GetComponentsInChildren<Renderer>()) { var ms = r.materials; foreach (var mm in ms) foreach (var pn in new[] { "_BaseColor", "_Color" }) if (mm.HasProperty(pn)) { var c = mm.GetColor(pn); if (c.grayscale > 0.8f) mm.SetColor(pn, tint); } r.materials = ms; } }
            foreach (var c in m.GetComponentsInChildren<Collider>()) Destroy(c);
        }
        else
        {
            var col = dog ? Data.H("c98b4a") : Data.H("f2a65a");
            Art.Prim(PrimitiveType.Capsule, root.transform, new Vector3(0, h * 0.45f, 0), new Vector3(h * 0.5f, h * 0.45f, h * 0.5f), Art.Toon(col)).transform.localRotation = Quaternion.Euler(90, 0, 0);
            Art.Prim(PrimitiveType.Sphere, root.transform, new Vector3(0, h * 0.8f, h * 0.45f), Vector3.one * h * 0.45f, Art.Toon(col));
        }
        p.cc = root.AddComponent<CharacterController>(); p.cc.height = h; p.cc.radius = 0.25f; p.cc.center = Vector3.up * (h * 0.5f + 0.02f); p.cc.stepOffset = 0.2f;
        // floating status mark ("?" / "!" / zzz)
        var mk = new GameObject("mark"); mk.transform.SetParent(root.transform, false); mk.transform.localPosition = Vector3.up * (h + 0.45f);
        p.mark = mk.AddComponent<TextMesh>(); p.mark.font = Art.FontBold; p.mark.GetComponent<MeshRenderer>().sharedMaterial = Art.WorldFontMat; p.mark.characterSize = 0.06f; p.mark.fontSize = 64; p.mark.anchor = TextAnchor.MiddleCenter; p.mark.text = "";
        mk.AddComponent<Billboard2>();
        All.Add(p); return p;
    }
    void OnDestroy() { All.Remove(this); }

    void Play(params string[] keys)
    {
        if (!anim) return;
        foreach (var k in keys) foreach (var kv in clips) if (kv.Key.Contains(k)) { if (cur == kv.Value) return; cur = kv.Value; anim[kv.Value].wrapMode = WrapMode.Loop; anim.CrossFade(kv.Value, 0.2f); return; }
    }
    public static void Noise(Vector3 pos, float radius)
    {
        foreach (var p in All) if (p && p.isActiveAndEnabled) p.Hear(pos, radius * (p.dog ? 1.3f : 1f));
    }
    void Hear(Vector3 pos, float r)
    {
        if ((pos - transform.position).magnitude > r) return;
        if (st == St.Patrol || st == St.Sleep || st == St.Investigate) { if (st == St.Sleep) Say(dog ? "woof" : "meow"); Set(St.Investigate); target = pos; }
    }
    public void LureTo(Lure l)
    {
        if (st == St.Flee || st == St.Lured && lure) return;
        lure = l; Set(St.Lured); target = l.transform.position;
    }
    public void LoseTrack() { if (st == St.Chase || st == St.Notice || st == St.Investigate) Set(St.Patrol); }
    void Set(St s) { if (st == s) return; st = s; stT = 0; lostT = 0; }
    void Say(string n) { Sfx.At(n, transform.position + Vector3.up * 0.4f, 0.9f, 3, 30); }
    Vector3 Eye => transform.position + Vector3.up * (dog ? 0.6f : 0.45f);
    bool Sees(Player p)
    {
        var d = p.cam.transform.position - Eye; float dist = d.magnitude; if (dist > sight) return false;
        var f = transform.forward; f.y = 0; var dd = d; dd.y = 0;
        if (Vector3.Angle(f, dd) > 50 && dist > 2.2f) return false;
        if (Physics.Raycast(Eye, d.normalized, out var h, dist, Art.ShotMask, QueryTriggerInteraction.Ignore)) { if (!h.collider.GetComponentInParent<Player>()) return false; }
        return true;
    }

    void Update()
    {
        float dt = Time.deltaTime; stT += dt; cool -= dt;
        var p = Player.I; bool active = p != null && p.inRaid && Game.I.RaidActive && !Game.I.InputLocked;
        Vector3 move = Vector3.zero; Vector3 face = transform.forward; float spd = 0;
        bool sees = active && FreezeAll <= 0 && p.alive && cool <= 0 && st != St.Flee && st != St.Lured && st != St.Sleep && Sees(p);
        if (sees && (st == St.Patrol || st == St.Investigate)) { Set(St.Notice); Say(dog ? "woof" : "meow"); }
        switch (st)
        {
            case St.Sleep:
                Play("sleep", "lie", "rest", "idle"); if (stT > 25 + Random.value * 10 && !sleeper) Set(St.Patrol); break;
            case St.Patrol:
                if (patrol.Count == 0) { Play("idle", "sit"); face = transform.forward; idleT += dt; if (sleeper && idleT > 6) { Set(St.Sleep); idleT = 0; } break; }
                { var to = patrol[wp] - transform.position; to.y = 0; if (to.magnitude < 0.6f) { idleT += dt; Play("idle", "sit"); if (idleT > 1.5f) { idleT = 0; wp = (wp + 1) % patrol.Count; } } else { move = to.normalized; spd = speed; face = to; Play("walk"); } }
                break;
            case St.Investigate:
                { var to = target - transform.position; to.y = 0; if (to.magnitude < 0.8f || stT > 7) { Play("idle", "sit"); if (stT > 2.5f && to.magnitude < 0.8f || stT > 7) Set(St.Patrol); } else { move = to.normalized; spd = speed * 1.3f; face = to; Play("walk"); } }
                break;
            case St.Notice:
                { var to = p.transform.position - transform.position; to.y = 0; face = to; Play("idle", "sit"); if (stT > 0.55f) Set(St.Chase); }
                break;
            case St.Chase:
                {
                    if (!active || FreezeAll > 0) { Set(St.Patrol); break; }
                    var to = p.transform.position - transform.position; to.y = 0; face = to;
                    if (Sees(p) || to.magnitude < 2.5f) lostT = 0; else lostT += dt;
                    if (lostT > 3 || stT > 8) { Set(St.Patrol); Game.I.Toast((dog ? "狗狗" : "貓咪") + pname + " 跟丟你了～"); break; }
                    move = to.normalized; spd = chaseSpeed; Play("run", "gallop", "walk");
                    barkT -= dt; if (barkT <= 0) { barkT = dog ? 1.4f : 2.2f; Say(dog ? "woof" : "meow"); }
                    if (to.magnitude < 0.9f) Catch(p);
                }
                break;
            case St.Flee:
                {
                    var to = transform.position - p.transform.position; to.y = 0; move = to.normalized; spd = chaseSpeed * 0.9f; face = to; Play("run", "gallop", "walk");
                    if (stT > 3) { DropLoot(); cool = 8; Set(St.Patrol); }
                }
                break;
            case St.Lured:
                {
                    if (!lure) { cool = 2; Set(St.Patrol); break; }
                    var to = lure.transform.position - transform.position; to.y = 0;
                    if (to.magnitude > 0.55f) { move = to.normalized; spd = chaseSpeed * 0.8f; face = to; Play("run", "walk"); }
                    else { face = to; Play("eat", "idle", "sit"); lure.Eating(this); }
                }
                break;
        }
        // movement w/ simple stuck handling
        if (cc && cc.enabled)
        {
            var v = move * spd; v.y = -4; cc.Move(v * dt);
            if (spd > 0.5f) { if ((transform.position - lastPos).magnitude < spd * dt * 0.2f) { stuckT += dt; if (stuckT > 1.2f) { stuckT = 0; if (st == St.Patrol && patrol.Count > 0) wp = (wp + 1) % patrol.Count; else if (st == St.Investigate) Set(St.Patrol); else transform.position += Quaternion.Euler(0, 90, 0) * move * 0.3f; } } else stuckT = 0; }
            lastPos = transform.position;
        }
        face.y = 0; if (face.sqrMagnitude > 0.001f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(face), dt * 8);
        // status mark
        if (mark)
        {
            string s = st == St.Sleep ? "<color=#4a7fb5>z z z</color>" : st == St.Investigate ? "<color=#b8861a>?</color>" : st == St.Notice || st == St.Chase ? "<color=#ff5a5a>!</color>" : st == St.Lured ? "<color=#c2508e>♥</color>" : st == St.Flee && carried != null ? "<color=#d97a2a>嘿嘿</color>" : "";
            mark.richText = true; mark.text = s; mark.transform.localPosition = Vector3.up * ((dog ? 0.75f : 0.55f) + 0.45f + 0.05f * Mathf.Sin(Time.time * 4));
        }
    }

    void Catch(Player p)
    {
        catches++; cool = 8; carried = Game.I.StealOne();
        p.Caught(this);
        Say(dog ? "woof" : "meow");
        Game.I.Toast(carried != null ? (dog ? "狗狗" : "貓咪") + pname + " 叼走了你的「" + carried.name + "」！快追！" : (dog ? "狗狗" : "貓咪") + pname + " 撲了你一下，好險背包是空的！");
        Set(St.Flee);
    }
    void DropLoot()
    {
        if (carried == null) return;
        var g = new GameObject("Loot"); g.transform.SetParent(transform.parent, false); g.transform.position = transform.position + Vector3.up * 0.02f; g.AddComponent<GroundLoot>().Init(carried);
        Game.I.Toast("「" + carried.name + "」被丟在地上了，快去撿回來！"); carried = null;
    }
    public void ForceState(St s) { Set(s); }
}

public class Billboard2 : MonoBehaviour { void LateUpdate() { var c = Camera.main; if (c) transform.rotation = Quaternion.LookRotation(transform.position - c.transform.position); } }

/// thrown can (cats) / meat (dogs); lasts ~3.5–4 s once it lands
public class Lure : MonoBehaviour
{
    public static readonly List<Lure> All = new List<Lure>();
    public int kind; public bool landed; float life = 8f, t; bool eaten; Rigidbody rb;
    public static Lure Spawn(int kind, Vector3 pos, Vector3 vel)
    {
        var g = new GameObject(kind == 0 ? "LureCan" : "LureMeat"); g.transform.position = pos; g.layer = 9;
        var l = g.AddComponent<Lure>(); l.kind = kind;
        if (kind == 0) { Art.Prim(PrimitiveType.Cylinder, g.transform, Vector3.zero, new Vector3(0.14f, 0.06f, 0.14f), Art.Toon(Data.H("f2c14e"))); Art.Prim(PrimitiveType.Cylinder, g.transform, Vector3.zero, new Vector3(0.145f, 0.035f, 0.145f), Art.Toon(Data.H("ff7a8a"))); }
        else { Art.Prim(PrimitiveType.Sphere, g.transform, Vector3.zero, new Vector3(0.18f, 0.13f, 0.22f), Art.Toon(Data.H("d9553c"))); Art.Prim(PrimitiveType.Cylinder, g.transform, new Vector3(0, 0, 0.13f), new Vector3(0.04f, 0.06f, 0.04f), Art.Toon(Data.H("fff5e6"))).transform.localRotation = Quaternion.Euler(90, 0, 0); }
        var sc = g.AddComponent<SphereCollider>(); sc.radius = 0.09f;
        l.rb = g.AddComponent<Rigidbody>(); l.rb.mass = 0.3f; l.rb.linearVelocity = vel; l.rb.angularVelocity = Random.insideUnitSphere * 8; l.rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        All.Add(l); return l;
    }
    void OnDestroy() { All.Remove(this); }
    void OnCollisionEnter(Collision c)
    {
        if (landed) return; landed = true;
        // treats should plop and stay put (cans used to roll 15m+ down the road, out-running the pet's lure timer)
        if (rb) { rb.linearVelocity *= 0.25f; rb.linearDamping = 6f; rb.angularDamping = 8f; }
        Sfx.At("boing", transform.position, 0.7f, 2, 25);
        Game.I.OnLureLanded(this);
        float best = 12f; Pet who = null;
        foreach (var p in Pet.All) { if (!p) continue; float d = (p.transform.position - transform.position).magnitude; if (d < best) { best = d; who = p; } }
        foreach (var p in Pet.All) if (p && (p.transform.position - transform.position).magnitude < 12f && (p == who || (p.dog == (kind == 1)))) p.LureTo(this);
    }
    public void Eating(Pet p) { if (!eaten) { eaten = true; life = Mathf.Min(life, 2.5f); Sfx.At("munch", transform.position, 0.8f, 2, 20); Game.I.OnLureEaten(); } }
    void Update()
    {
        if (!landed) { if (transform.position.y < -20) Destroy(gameObject); t += Time.deltaTime; if (t > 4) { landed = true; } return; }
        life -= Time.deltaTime; if (eaten) transform.localScale = Vector3.one * Mathf.Clamp01(life / 2.5f + 0.2f);
        if (life <= 0) Destroy(gameObject);
    }
}

public static class PetSmooth
{
    static readonly System.Collections.Generic.Dictionary<Mesh, Mesh> smoothCache = new System.Collections.Generic.Dictionary<Mesh, Mesh>();
    /// weld-by-position smooth normals so the low-poly pet reads as a soft vinyl toy, not papercraft
    public static Mesh Smooth(Mesh src)
    {
        if (smoothCache.TryGetValue(src, out var c) && c) return c; if (!src.isReadable) { Debug.Log("[pet] mesh not readable, no smooth"); return src; }
        var m = Object.Instantiate(src); m.name = src.name + "_smooth"; var v = m.vertices; var n = m.normals; if (n == null || n.Length != v.Length) { m.RecalculateNormals(); n = m.normals; }
        var acc = new System.Collections.Generic.Dictionary<Vector3Int, Vector3>();
        System.Func<Vector3, Vector3Int> K = p => new Vector3Int(Mathf.RoundToInt(p.x * 2000), Mathf.RoundToInt(p.y * 2000), Mathf.RoundToInt(p.z * 2000));
        var t = m.triangles;
        for (int i = 0; i < t.Length; i += 3) { var fn = Vector3.Cross(v[t[i + 1]] - v[t[i]], v[t[i + 2]] - v[t[i]]); for (int k = 0; k < 3; k++) { var key = K(v[t[i + k]]); acc.TryGetValue(key, out var a); acc[key] = a + fn; } }
        for (int i = 0; i < v.Length; i++) if (acc.TryGetValue(K(v[i]), out var a) && a.sqrMagnitude > 1e-12f) n[i] = a.normalized;
        m.normals = n; smoothCache[src] = m; Debug.Log("[pet] smoothed " + src.name + " verts=" + v.Length); return m;
    }
}
