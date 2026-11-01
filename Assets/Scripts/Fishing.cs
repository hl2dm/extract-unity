using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// v0.9.21 grandma's farm pond: shin-deep water you can wade into, fish shadows you can catch with a small timing press.
public static class Pond
{
    public static bool On; public static Vector3 C; public static float RX = 7, RZ = 5;
    public const float Surface = 0.2f;
    public static readonly Fish.Kind[] Kinds = {
        new Fish.Kind("溪蝦", "🦐", 0, 22, 0.35f, 0.55f), new Fish.Kind("吳郭魚", "🐡", 0, 35, 0.30f, 0.95f),
        new Fish.Kind("孔雀魚", "🐠", 1, 80, 0.18f, 0.6f), new Fish.Kind("泥鰍", "🐍", 2, 300, 0.12f, 1.0f), new Fish.Kind("錦鯉", "🎏", 3, 850, 0.05f, 1.35f) };
    /// 0 = centre, 1 = water edge (wobbly ellipse, same shape as the water mesh)
    public static float R01(Vector3 p)
    {
        float x = (p.x - C.x) / RX, z = (p.z - C.z) / RZ; float an = Mathf.Atan2(z, x); float w = 1 + 0.06f * Mathf.Sin(an * 3 + 1);
        return Mathf.Sqrt(x * x + z * z) / w;
    }
    public static bool In(Vector3 p, float edge = 0.97f) => On && R01(p) < edge && p.y < 0.6f;
    public static float Wade(Vector3 p) => In(p) ? 0.55f : 1f;
    public static Vector3 RandIn(System.Random r, float k)
    {
        float an = (float)r.NextDouble() * 6.283f, rr = Mathf.Sqrt((float)r.NextDouble()) * k; float w = 1 + 0.06f * Mathf.Sin(an * 3 + 1);
        return C + new Vector3(Mathf.Cos(an) * RX * rr * w, 0, Mathf.Sin(an) * RZ * rr * w);
    }
    public static int Splashes, Catches, Misses;
    /// a splash is loud: geese within r hear it and turn to look (feeds the same alert the eyes use)
    public static void Splash(Vector3 at, float r, bool big)
    {
        Splashes++; Ripple(at, big ? 1.4f : 0.8f); if (big) Ripple(at, 0.9f, 0.15f);
        foreach (var g in Goose.All) if (g && (g.transform.position - at).sqrMagnitude < r * r) g.ForceSee(at);
    }
    // ---- ripple rings (small pool of unlit line loops on the water surface)
    static readonly List<(LineRenderer lr, float t, float max, Vector3 c, float d)> rings = new List<(LineRenderer, float, float, Vector3, float)>();
    static Material mRip; static Transform ripRoot;
    public static void Ripple(Vector3 at, float max, float delay = 0)
    {
        if (!On) return; if (!ripRoot) { ripRoot = new GameObject("pond_ripples").transform; rings.Clear(); ripRoot.gameObject.AddComponent<RippleTick>(); }
        if (!mRip) mRip = Art.Unlit(Data.H("dcefeb"));
        LineRenderer lr = null; int idx = -1;
        for (int i = 0; i < rings.Count; i++) if (rings[i].t >= 1) { lr = rings[i].lr; idx = i; break; }
        if (!lr) { if (rings.Count >= 10) { idx = 0; lr = rings[0].lr; } else {
            var g = new GameObject("ripple"); g.transform.SetParent(ripRoot, false); lr = g.AddComponent<LineRenderer>(); lr.useWorldSpace = true; lr.loop = true; lr.positionCount = 28; lr.sharedMaterial = mRip; lr.shadowCastingMode = ShadowCastingMode.Off; lr.receiveShadows = false; lr.numCornerVertices = 1; } }
        var e = (lr, -delay, max, new Vector3(at.x, Surface + 0.012f, at.z), 0.9f); if (idx >= 0) rings[idx] = e; else rings.Add(e); lr.enabled = false;
    }
    public static void TickRipples(float dt)
    {
        for (int i = 0; i < rings.Count; i++)
        {
            var r = rings[i]; if (r.t >= 1 || !r.lr) continue; r.t += dt / r.d; rings[i] = r;
            if (r.t < 0) continue; if (r.t >= 1) { r.lr.enabled = false; continue; }
            float k = 1 - (1 - r.t) * (1 - r.t); float rad = 0.15f + r.max * k; r.lr.enabled = true;
            r.lr.widthMultiplier = 0.032f * (1 - r.t * 0.8f);
            for (int q = 0; q < 28; q++) { float a = q * Mathf.PI * 2 / 28; r.lr.SetPosition(q, r.c + new Vector3(Mathf.Cos(a) * rad, 0, Mathf.Sin(a) * rad * 0.92f)); }
        }
    }
    public static void Reset() { On = false; rings.Clear(); if (ripRoot) Object.Destroy(ripRoot.gameObject); ripRoot = null; Splashes = Catches = Misses = 0; }
    /// called by the farm builder right after the pond exists
    public static void Build(Transform pond, Vector3 c, float rx, float rz, System.Random r)
    {
        Reset(); On = true; C = c; RX = rx; RZ = rz;
        for (int i = 0; i < 6; i++) Fish.Create(pond, r, i);
    }
    public static Fish.Kind RollKind(System.Random r)
    {
        double x = r.NextDouble(); foreach (var k in Kinds) { x -= k.w; if (x < 0) return k; } return Kinds[0];
    }
}

public class RippleTick : MonoBehaviour { void Update() { Pond.TickRipples(Time.deltaTime); } }

public class Fish : Interactable
{
    public class Kind { public string name, ic; public int r, v; public float w, size; public Kind(string n, string i, int r, int v, float w, float s) { name = n; ic = i; this.r = r; this.v = v; this.w = w; size = s; } }
    public Kind kind; System.Random rng; Vector3 goal; float speed = 0.5f, dartT, coolT, hideT, tailT; Transform body, tail;
    // timing press
    public bool Timing; float tT, marker, dir = 1, zone0, zone1; Transform bar, barZone, barMark; int useFrame;
    public static readonly List<Fish> AllFish = new List<Fish>();
    public static int TimeoutsRun;
    public override string Prompt => hideT > 0 ? "" : Timing ? "<color=#2f7a4a>綠色時按 [E]！</color>" : coolT > 0 ? "魚嚇跑了…等一下" : "水裡有影子（" + Size + "）　[E] 抓魚";
    string Size => kind.size > 1.2f ? "好大一隻" : kind.size > 0.8f ? "中等" : "小小的";
    public override Vector3 Point => transform.position + Vector3.up * 0.1f;

    public static Fish Create(Transform pond, System.Random r, int i)
    {
        var g = new GameObject("fish_" + i); g.transform.SetParent(pond, false);
        var f = g.AddComponent<Fish>(); f.rng = new System.Random(r.Next()); f.reach = 1.9f;
        var sc = g.AddComponent<SphereCollider>(); sc.isTrigger = true; sc.radius = 0.32f; sc.center = new Vector3(0, 0.12f, 0);
        f.Spawn(true); return f;
    }
    void Build()
    {
        if (body) Destroy(body.gameObject);
        float s = kind.size; var sh = Data.H(kind.r >= 3 ? "5d5462" : "3f525c"); var col = Color.Lerp(sh, Data.H("86bccb"), 0.12f); s *= 1.35f;
        body = new GameObject("shadow").transform; body.SetParent(transform, false); body.localPosition = new Vector3(0, Pond.Surface + 0.006f - transform.localPosition.y, 0);
        SoftKit.Ball(body, Vector3.zero, new Vector3(0.16f, 0.012f, 0.42f) * s, col, 12, false, null, "fish_body", 0.02f);
        tail = new GameObject("tail").transform; tail.SetParent(body, false); tail.localPosition = new Vector3(0, 0, -0.22f * s);
        SoftKit.Ball(tail, new Vector3(0, 0, -0.06f * s), new Vector3(0.14f, 0.01f, 0.13f) * s, col, 8, false, null, "fish_tail", 0.02f);
        foreach (int sx in new[] { -1, 1 }) { var fin = SoftKit.Ball(body, new Vector3(sx * 0.09f * s, 0, 0.04f * s), new Vector3(0.1f, 0.008f, 0.05f) * s, col, 8, false, null, "fish_fin", 0.02f); fin.transform.localRotation = Quaternion.Euler(0, sx * 35, 0); }   // pectoral fins: reads as a fish, not a blob
        SoftKit.Ball(body, new Vector3(0, 0.002f, 0.17f * s), new Vector3(0.11f, 0.01f, 0.1f) * s, col, 8, false, null, "fish_head", 0.02f);
        if (kind.r >= 3) SoftKit.Ball(body, new Vector3(0, 0.004f, 0.05f * s), new Vector3(0.07f, 0.006f, 0.14f) * s, Data.H("d9926a"), 8, false, null, "fish_glint", 0.02f);
        foreach (var mr in body.GetComponentsInChildren<MeshRenderer>()) { mr.shadowCastingMode = ShadowCastingMode.Off; mr.receiveShadows = false; }
    }
    void Spawn(bool first)
    {
        kind = Pond.RollKind(rng); var p = Pond.RandIn(rng, 0.8f); transform.position = new Vector3(p.x, 0, p.z);
        goal = Pond.RandIn(rng, 0.85f); Build(); hideT = first ? 0 : 0; coolT = 0; Timing = false; HideBar();
        foreach (var c in GetComponents<Collider>()) c.enabled = true; body.gameObject.SetActive(true);
    }
    protected override void OnEnable() { base.OnEnable(); AllFish.Add(this); }
    protected override void OnDisable() { base.OnDisable(); AllFish.Remove(this); }

    void Update()
    {
        float dt = Time.deltaTime;
        if (hideT > 0) { hideT -= dt; if (hideT <= 0) Spawn(false); return; }
        coolT -= dt; dartT -= dt;
        var pl = Game.I ? Game.I.player : null; Vector3 pos = transform.position;
        if (pl && !Timing) { var d = pos - pl.transform.position; d.y = 0; if (d.magnitude < 2.4f && pl.Sprinting) { goal = pos + d.normalized * 3; dartT = 0.8f; } }
        float sp = Timing ? 0.12f : dartT > 0 ? 3.2f : speed;
        var to = goal - pos; to.y = 0;
        if (to.magnitude < 0.3f || Pond.R01(goal) > 0.88f) goal = Pond.RandIn(rng, 0.85f);
        else
        {
            var step = to.normalized * sp * dt; var np = pos + step; if (Pond.R01(np) < 0.88f) transform.position = np; else goal = Pond.RandIn(rng, 0.6f);
            var want = Quaternion.LookRotation(to.normalized); transform.rotation = Quaternion.Slerp(transform.rotation, want, dt * 4);
        }
        tailT += dt * (3 + sp * 6); if (tail) tail.localRotation = Quaternion.Euler(0, Mathf.Sin(tailT) * 24, 0);
        if (Timing)
        {
            tT += dt; float spd = 1.05f + kind.r * 0.3f; marker += dir * spd * dt; if (marker > 1) { marker = 1; dir = -1; } if (marker < 0) { marker = 0; dir = 1; }
            UpdateBar(); if (tT > 3.2f) { TimeoutsRun++; Miss(); }
        }
    }
    public override void Use()
    {
        if (hideT > 0 || coolT > 0 || Time.frameCount == useFrame) return; useFrame = Time.frameCount;
        if (!Timing) { StartTiming(); return; }
        Resolve(marker >= zone0 && marker <= zone1);
    }
    public void StartTiming()
    {
        Timing = true; tT = 0; marker = 0; dir = 1; float w = Mathf.Max(0.12f, 0.32f - kind.r * 0.05f); float c = 0.3f + (float)rng.NextDouble() * 0.45f; zone0 = c - w / 2; zone1 = c + w / 2;
        MakeBar(); Sfx.Play("water_step" + rng.Next(4), 0.25f, 1.1f);
    }
    /// for the autotest: press at the right/wrong moment
    public void ForceResolve(bool hit) { if (!Timing) StartTiming(); marker = hit ? (zone0 + zone1) / 2 : (zone0 > 0.3f ? 0 : 1); Resolve(hit); }
    void Resolve(bool hit)
    {
        if (hit)
        {
            var it = new Item { r = kind.r, ic = kind.ic, name = kind.name, v = kind.v };
            if (Game.I && !Game.I.Pickup(it)) { Timing = false; HideBar(); coolT = 1; return; }
            Pond.Catches++; Sfx.Play("water_splash0", 0.5f, 1.05f); StartCoroutine(Jump(kind)); Pond.Ripple(transform.position, 1.1f);
            if (Game.I) Game.I.Toast("抓到了！<color=#3a78c9>" + kind.name + "</color>（" + Data.RarName[kind.r] + "）", 2.2f);
            Pond.Splash(transform.position, 4.5f, false);
            Timing = false; HideBar(); hideT = 18 + (float)rng.NextDouble() * 10; body.gameObject.SetActive(false); foreach (var c in GetComponents<Collider>()) c.enabled = false;
        }
        else Miss();
    }
    System.Collections.IEnumerator Jump(Kind k)
    {   // payoff: the caught fish flips out of the water in its real colours, then lands in the bag
        var j = new GameObject("fish_jump").transform; j.SetParent(null, false); var p0 = transform.position + Vector3.up * Pond.Surface; float s = Mathf.Max(0.8f, k.size) * 1.2f;
        var c = Data.H(k.r >= 3 ? "f08a4b" : k.r == 2 ? "9a8a6a" : k.r == 1 ? "6cc0d8" : "a7b6b0");
        SoftKit.Ball(j, Vector3.zero, new Vector3(0.12f, 0.16f, 0.36f) * s, c, 12, false, null, "jump_body", 0.02f);
        SoftKit.Ball(j, new Vector3(0, 0, -0.22f * s), new Vector3(0.03f, 0.16f, 0.12f) * s, Color.Lerp(c, Color.white, 0.25f), 8, false, null, "jump_tail", 0.02f);
        foreach (int sx in new[] { -1, 1 }) SoftKit.Ball(j, new Vector3(sx * 0.055f * s, 0.03f, 0.12f * s), Vector3.one * 0.03f * s, Data.H("2a2630"), 6, false, null, "jump_eye", 0.01f);
        foreach (var mr in j.GetComponentsInChildren<MeshRenderer>()) mr.shadowCastingMode = ShadowCastingMode.Off;
        for (float t = 0; t < 1.4f; t += Time.deltaTime) { float u = t / 1.4f; j.position = p0 + Vector3.up * (Mathf.Sin(u * Mathf.PI) * 0.9f); j.rotation = Quaternion.Euler(-60 + u * 140, 30, Mathf.Sin(u * 20) * 15); yield return null; }
        Pond.Ripple(p0, 0.8f); Destroy(j.gameObject);
    }
    void Miss()
    {
        Pond.Misses++; Timing = false; HideBar(); coolT = 2.5f; dartT = 1.0f;
        var away = transform.position - (Game.I ? Game.I.player.transform.position : transform.position); away.y = 0; if (away.sqrMagnitude < 0.01f) away = transform.forward;
        goal = transform.position + away.normalized * 3.5f;
        Sfx.Play("water_splash1", 0.55f, 0.95f + (float)rng.NextDouble() * 0.1f); Pond.Splash(transform.position, 7f, true);
        if (Game.I) Game.I.Toast("啪！魚溜走了，水花好大聲…", 1.6f);
    }
    // ---- little world-space timing bar floating over the fish
    void MakeBar()
    {
        if (bar) { bar.gameObject.SetActive(true); }
        else
        {
            bar = new GameObject("fish_bar").transform; bar.SetParent(null, false);   // not under the fish: its scale is flattened
            SoftKit.Block(bar, Vector3.zero, new Vector3(0.95f, 0.15f, 0.03f), Data.H("fbf1de"), 0.06f, false, 0, -1, 0, null, "bar_bg");
            barZone = SoftKit.Block(bar, new Vector3(0, 0, -0.012f), new Vector3(1, 0.11f, 0.012f), Data.H("6fbf7e"), 0.02f, false, 0, -1, 0, null, "bar_zone").transform;
            barMark = SoftKit.Block(bar, new Vector3(0, 0, -0.024f), new Vector3(0.022f, 0.24f, 0.014f), Data.H("3a2a34"), 0.01f, false, 0, -1, 0, null, "bar_mark").transform;
            foreach (var mr in bar.GetComponentsInChildren<MeshRenderer>()) { mr.shadowCastingMode = ShadowCastingMode.Off; mr.receiveShadows = false; }
        }
        barZone.localScale = new Vector3((zone1 - zone0) * 0.88f, 1, 1); barZone.localPosition = new Vector3(((zone0 + zone1) / 2 - 0.5f) * 0.88f, 0, -0.012f); UpdateBar();
    }
    void UpdateBar()
    {
        if (!bar) return; var cam = Game.I && Game.I.player ? Game.I.player.cam : Camera.main; bar.position = transform.position + Vector3.up * 0.42f; if (cam) { var tc = cam.transform.position - bar.position; tc.y = 0; bar.position += tc.normalized * 0.35f; }   // low and in front of the fish so the HUD panels never cover it
        if (cam) bar.rotation = cam.transform.rotation;   // screen-parallel: no perspective skew on the timing bar
        barMark.localPosition = new Vector3((marker - 0.5f) * 0.88f, 0, -0.024f);
    }
    void OnDestroy() { if (bar) Destroy(bar.gameObject); }   // bar is unparented now
    public string BarDbg() { var cam = Camera.main; return bar ? ("bar " + bar.position.ToString("F2") + " act " + bar.gameObject.activeInHierarchy + " vp " + (cam ? cam.WorldToViewportPoint(bar.position).ToString("F2") : "-") + " fish " + transform.position.ToString("F2")) : "no bar"; }
    void HideBar() { if (bar) bar.gameObject.SetActive(false); }
}
