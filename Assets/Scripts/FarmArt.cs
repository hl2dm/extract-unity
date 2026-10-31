using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using static SoftKit;

/// v0.9.17 外婆家農場 — hand-built farm meshes: rice paddies, haystacks, geese, tiled roof, bamboo, fences.
public class FMB
{
    public readonly List<Vector3> v = new List<Vector3>(), n = new List<Vector3>(); public readonly List<Color> c = new List<Color>(); public readonly List<int> t = new List<int>();
    public float a = 1f;
    public int V(Vector3 p, Vector3 nm, Color col) { v.Add(p); n.Add(nm); c.Add(new Color(col.r, col.g, col.b, a)); return v.Count - 1; }
    /// ribbon along spine points; both faces get their own vertices (soft up-biased normals for foliage)
    public void Ribbon(IList<Vector3> sp, Vector3 side, IList<float> w, IList<Color> col, bool two = true, float upN = 0.6f)
    {
        int m = sp.Count; var face = Vector3.Cross(side, (sp[m - 1] - sp[0]).normalized).normalized;
        for (int pass = 0; pass < (two ? 2 : 1); pass++)
        {
            var nm = (Vector3.up * upN + (pass == 0 ? face : -face) * (1 - upN)).normalized; int b = v.Count;
            for (int i = 0; i < m; i++) { V(sp[i] - side * w[i] * 0.5f, nm, col[i]); V(sp[i] + side * w[i] * 0.5f, nm, col[i]); }
            for (int i = 0; i < m - 1; i++) { int q = b + 2 * i; if (pass == 0) { t.Add(q); t.Add(q + 2); t.Add(q + 1); t.Add(q + 1); t.Add(q + 2); t.Add(q + 3); } else { t.Add(q); t.Add(q + 1); t.Add(q + 2); t.Add(q + 1); t.Add(q + 3); t.Add(q + 2); } }
        }
    }
    public void Tri(Vector3 p0, Vector3 p1, Vector3 p2, Color col, bool two = false)
    {
        var nm = Vector3.Cross(p1 - p0, p2 - p0).normalized; int b = v.Count; V(p0, nm, col); V(p1, nm, col); V(p2, nm, col); t.Add(b); t.Add(b + 1); t.Add(b + 2);
        if (two) { b = v.Count; V(p0, -nm, col); V(p1, -nm, col); V(p2, -nm, col); t.Add(b); t.Add(b + 2); t.Add(b + 1); }
    }
    public void Quad(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, Color col, bool two = false) { Tri(p0, p1, p2, col, two); Tri(p0, p2, p3, col, two); }
    /// smooth tube through points (radius per point), closed with round-ish caps
    public void Tube(IList<Vector3> sp, IList<float> r, IList<Color> col, int seg = 10)
    {
        int m = sp.Count; Vector3 prevN = Vector3.right; int b0 = v.Count;
        for (int i = 0; i < m; i++)
        {
            var tg = (sp[Mathf.Min(i + 1, m - 1)] - sp[Mathf.Max(i - 1, 0)]).normalized;
            var nx = Vector3.Cross(tg, Vector3.Cross(prevN, tg)).normalized; if (nx.sqrMagnitude < 0.1f) nx = Vector3.Cross(tg, Vector3.up).normalized; prevN = nx; var ny = Vector3.Cross(tg, nx);
            for (int k = 0; k <= seg; k++) { float an = k * Mathf.PI * 2 / seg; var d = nx * Mathf.Cos(an) + ny * Mathf.Sin(an); V(sp[i] + d * r[i], d, col[i]); }
        }
        for (int i = 0; i < m - 1; i++) for (int k = 0; k < seg; k++) { int q = b0 + i * (seg + 1) + k, q2 = q + seg + 1; t.Add(q); t.Add(q2); t.Add(q + 1); t.Add(q + 1); t.Add(q2); t.Add(q2 + 1); }
    }
    public Mesh Build(string name)
    {
        var m = new Mesh { name = name }; if (v.Count > 65000) m.indexFormat = IndexFormat.UInt32;
        m.SetVertices(v); m.SetNormals(n); m.SetColors(c); m.SetTriangles(t, 0); m.RecalculateBounds(); return m;
    }
}

public static class FarmArt
{
    static Color Lt(Color c, float k) => Color.Lerp(c, Color.white, k);
    static Color Dk(Color c, float k) => new Color(c.r * k, c.g * k, c.b * k, 1);
    public static readonly Color Straw = H("e8c46a"), StrawD = H("c9963e"), StrawL = H("f4dc96"), RiceG = H("6f9a3e"), RiceM = H("a9b84e"), RiceT = H("e4c45e"),
        Mud = H("9c7a56"), MudD = H("7d5f42"), Water = H("b9a98e"), Bamboo = H("a9c46a"), BambooD = H("7f9a48"), Brick = H("c27a5e"), TileR = H("c2593f"), GooseW = H("f8f3e8"), Beak = H("f28a2c");

    // ---------------- rice paddy: ~600 clumps of curved blades with drooping golden panicles, one mesh, water + mud under it
    public static Transform Paddy(Transform p, Vector3 c, float sx, float sz, int seed)
    {
        var g = Group(p, "sk_paddy", c); var rnd = new System.Random(seed);
        float Rn() => (float)rnd.NextDouble();
        // muddy water with sky-tinted sheen
        Field(g, new Vector3(0, 0.035f, 0), sx, sz, 0.8f, (x, z) => Color.Lerp(Color.Lerp(Water, H("d9c3a8"), VN(new Vector3(x * 0.25f, 0, z * 0.25f), seed)), MudD, 0.15f * VN(new Vector3(x, 0, z), seed + 3)), false, "paddy_water");
        var mb = new FMB { a = MatK.Foliage }; float step = 0.5f;
        for (float x = -sx / 2 + 0.35f; x < sx / 2 - 0.2f; x += step)
            for (float z = -sz / 2 + 0.35f; z < sz / 2 - 0.2f; z += step)
            {
                var b = new Vector3(x + (Rn() - 0.5f) * 0.12f, 0.04f, z + (Rn() - 0.5f) * 0.12f); float hk = 0.86f + Rn() * 0.22f;
                float ripe = Mathf.Clamp01(0.55f + 0.5f * VN(new Vector3((c.x + x) * 0.12f, 0, (c.z + z) * 0.12f), seed + 9) - 0.2f);
                int nb = 6;
                for (int i = 0; i < nb; i++)
                {
                    float an = i * Mathf.PI * 2 / nb + Rn() * 0.8f; var dir = new Vector3(Mathf.Cos(an), 0, Mathf.Sin(an)); float lean = 0.12f + Rn() * 0.2f, h = (0.62f + Rn() * 0.3f) * hk;
                    var sp = new Vector3[4]; var w = new float[4]; var cl = new Color[4];
                    for (int k = 0; k < 4; k++) { float tt = k / 3f; sp[k] = b + dir * (lean * tt * tt * h) + Vector3.up * (h * tt); w[k] = Mathf.Lerp(0.045f, 0.006f, tt); cl[k] = k == 0 ? Dk(RiceG, 0.8f) : k == 1 ? RiceG : k == 2 ? Color.Lerp(RiceM, RiceT, ripe * 0.6f) : Color.Lerp(RiceM, RiceT, ripe); }
                    mb.Ribbon(sp, Vector3.Cross(dir, Vector3.up).normalized, w, cl);
                    if (i < 3)
                    {   // panicle: stalk to above the leaves, then droops over in a golden arc of grains
                        float ph = h * 1.05f + 0.06f; var top = b + dir * 0.05f + Vector3.up * ph; var dp = new Vector3[4]; var dw = new float[4]; var dc = new Color[4];
                        for (int k = 0; k < 4; k++) { float tt = k / 3f; dp[k] = top + dir * (0.16f * tt) - Vector3.up * (0.2f * tt * tt); dw[k] = Mathf.Lerp(0.05f, 0.022f, tt); dc[k] = Color.Lerp(RiceT, StrawL, tt * 0.5f) * (0.92f + 0.08f * Rn()); dc[k].a = 1; }
                        mb.Ribbon(new[] { b + Vector3.up * (h * 0.5f), top }, Vector3.Cross(dir, Vector3.up).normalized, new[] { 0.012f, 0.008f }, new[] { RiceM, Color.Lerp(RiceM, RiceT, ripe) });
                        mb.Ribbon(dp, Vector3.Cross(dir, Vector3.up).normalized, dw, dc, true, 0.75f);
                    }
                }
            }
        var go = Obj(g, "rice", mb.Build("rice"), Vector3.zero, Quaternion.identity, 0.0001f); go.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
        return g;
    }

    // ---------------- 稻草堆: straw stack around a pole — lathe with straw bands, rope ring, poking straws, cap knot
    public static Transform Haystack(Transform p, Vector3 pos, float s, int seed)
    {
        var g = Group(p, "sk_haystack", pos, seed * 37 % 360); var rnd = new System.Random(seed); float Rn() => (float)rnd.NextDouble();
        g.localScale = Vector3.one * s;
        var prof = new List<Vector2> { new Vector2(0, 0), new Vector2(0.78f, 0.0f), new Vector2(0.92f, 0.25f), new Vector2(0.98f, 0.6f), new Vector2(0.95f, 0.95f), new Vector2(0.86f, 1.2f), new Vector2(0.66f, 1.55f), new Vector2(0.42f, 1.85f), new Vector2(0.2f, 2.08f), new Vector2(0.06f, 2.2f), new Vector2(0, 2.22f) };
        var rc = new List<Color>(); for (int i = 0; i < prof.Count; i++) rc.Add(i % 2 == 0 ? Straw : Color.Lerp(StrawD, Straw, 0.45f));
        var om = PushM(MatK.Fabric);
        var body = Lath(g, Vector3.zero, prof, Straw, 28, false, null, "hay_body", rc);
        Ring(g, new Vector3(0, 1.22f, 0), Quaternion.Euler(90, 0, 0), 0.87f, 0.035f, StrawD, 30, "hay_rope");
        Ring(g, new Vector3(0, 0.62f, 0), Quaternion.Euler(90, 0, 0), 0.985f, 0.03f, Dk(StrawD, 0.9f), 30, "hay_rope");
        Cyl(g, new Vector3(0, 2.3f, 0), 0.04f, 0.5f, H("8a6a44"), 0.01f, null, 8);   // centre pole
        Lath(g, new Vector3(0, 2.12f, 0), new List<Vector2> { new Vector2(0, 0), new Vector2(0.2f, 0.02f), new Vector2(0.16f, 0.12f), new Vector2(0.05f, 0.22f), new Vector2(0, 0.24f) }, StrawD, 14, false, null, "hay_cap");
        // poking straws all over (the "hairy" read of a real haystack)
        var mb = new FMB { a = MatK.Fabric };
        for (int i = 0; i < 260; i++)
        {
            float y = Rn() * 2.1f, an = Rn() * Mathf.PI * 2; float r = 0; for (int k = 0; k < prof.Count - 1; k++) if (y >= prof[k].y && y <= prof[k + 1].y) r = Mathf.Lerp(prof[k].x, prof[k + 1].x, (y - prof[k].y) / Mathf.Max(0.001f, prof[k + 1].y - prof[k].y));
            var d = new Vector3(Mathf.Cos(an), 0, Mathf.Sin(an)); var b = d * (r - 0.03f) + Vector3.up * y; var o = (d + Vector3.down * (0.3f + Rn() * 0.6f) + new Vector3(Rn() - 0.5f, Rn() - 0.5f, Rn() - 0.5f) * 0.5f).normalized; float L = 0.1f + Rn() * 0.18f;
            var col = Rn() < 0.5f ? StrawL : Straw;
            mb.Ribbon(new[] { b, b + o * L * 0.5f + Vector3.down * 0.01f, b + o * L }, Vector3.Cross(o, Vector3.up).normalized, new[] { 0.018f, 0.012f, 0.004f }, new[] { col, col, Lt(col, 0.2f) }, true, 0.3f);
        }
        // a skirt of loose straw on the ground
        for (int i = 0; i < 70; i++)
        {
            float an = Rn() * Mathf.PI * 2, r = 0.9f + Rn() * 0.5f; var b = new Vector3(Mathf.Cos(an) * r, 0.02f, Mathf.Sin(an) * r); var o = new Vector3(Mathf.Cos(an + Rn() * 3), 0, Mathf.Sin(an + Rn() * 3));
            mb.Ribbon(new[] { b, b + o * 0.22f }, Vector3.Cross(o, Vector3.up).normalized + Vector3.up * 0.0f, new[] { 0.02f, 0.01f }, new[] { Straw, StrawL }, true, 0.9f);
        }
        Obj(g, "hay_straws", mb.Build("hay"), Vector3.zero, Quaternion.identity, 0.0001f);
        MatA = om;
        var bc = g.gameObject.AddComponent<BoxCollider>(); bc.center = new Vector3(0, 1.0f, 0); bc.size = new Vector3(1.5f, 2.0f, 1.5f);
        return g;
    }

    // ---------------- goose (also hen with kind 1). Returns rig parts for animation.
    public class Rig { public Transform root, body, neck, head, jaw, wingL, wingR; }
    public static Rig Goose(Transform p, Vector3 pos, float yaw, int kind = 0)
    {
        var rg = new Rig(); var g = Group(p, kind == 0 ? "Goose" : "Hen", pos, yaw); rg.root = g;
        bool hen = kind == 1; var W = hen ? H("c98a4e") : GooseW; var Wd = hen ? H("a2643a") : H("ded6c8"); var B = hen ? H("e8b050") : Beak; float S = hen ? 0.62f : 1f;
        var bodyG = Group(g, "body", new Vector3(0, 0.34f * S, 0)); bodyG.localScale = Vector3.one * S; rg.body = bodyG;
        var om = PushM(MatK.Fabric);
        // teardrop body lying along +z (tail at -z, chest at +z), rear tilted up
        var prof = new List<Vector2> { new Vector2(0, -0.34f), new Vector2(0.05f, -0.31f), new Vector2(0.11f, -0.23f), new Vector2(0.16f, -0.11f), new Vector2(0.185f, 0.02f), new Vector2(0.18f, 0.12f), new Vector2(0.15f, 0.21f), new Vector2(0.09f, 0.27f), new Vector2(0, 0.29f) };
        var bd = Lath(bodyG, Vector3.zero, prof, W, 24, false, Quaternion.Euler(90, 0, 0) * Quaternion.identity, "goose_body", new List<Color> { Wd, Wd, W, W, W, W, W, Lt(W, 0.2f), Lt(W, 0.2f) });
        bd.transform.localScale = new Vector3(0.86f, 1f, 0.92f); bd.transform.localRotation = Quaternion.Euler(-10, 0, 0) * Quaternion.Euler(90, 0, 0);
        // upturned tail tuft
        Lath(bodyG, new Vector3(0, 0.07f, -0.31f), new List<Vector2> { new Vector2(0, 0), new Vector2(0.06f, 0.03f), new Vector2(0.05f, 0.1f), new Vector2(0, 0.13f) }, Wd, 12, false, Quaternion.Euler(-50, 0, 0), "goose_tail");
        // folded wings with primary feather tips
        foreach (int sd in new[] { -1, 1 })
        {
            var wp = Group(bodyG, sd < 0 ? "wingL" : "wingR", new Vector3(sd * 0.135f, 0.05f, 0.1f)); if (sd < 0) rg.wingL = wp; else rg.wingR = wp;
            var wprof = new List<Vector2> { new Vector2(0, 0), new Vector2(0.07f, 0.04f), new Vector2(0.1f, 0.14f), new Vector2(0.09f, 0.26f), new Vector2(0.05f, 0.36f), new Vector2(0, 0.4f) };
            var w = Lath(wp, Vector3.zero, wprof, Wd, 16, false, Quaternion.Euler(-84, 0, 0), "goose_wing", new List<Color> { W, W, Lt(Wd, 0.3f), Wd, Dk(Wd, 0.92f), Dk(Wd, 0.88f) }); w.transform.localScale = new Vector3(0.32f, 1, 1);
            for (int f = 0; f < 3; f++) { var fe = Lath(wp, new Vector3(sd * 0.005f, 0.06f + f * 0.012f, -0.3f - f * 0.02f), new List<Vector2> { new Vector2(0, 0), new Vector2(0.025f, 0.03f), new Vector2(0.018f, 0.12f), new Vector2(0, 0.14f) }, Dk(Wd, 0.85f - f * 0.04f), 8, false, Quaternion.Euler(-74 + f * 6, sd * (8 + f * 6), 0), "goose_prim"); fe.transform.localScale = new Vector3(0.4f, 1, 1); }
        }
        // S-curved neck in its own pivot (base at chest top)
        var nk = Group(g, "neck", new Vector3(0, (0.34f + 0.12f) * S, 0.2f * S)); nk.localScale = Vector3.one * S; rg.neck = nk;
        {
            var mb = new FMB { a = MatK.Fabric }; var sp = new List<Vector3>(); var rr = new List<float>(); var cc = new List<Color>();
            float L = hen ? 0.12f : 0.36f;
            for (int i = 0; i <= 8; i++) { float tt = i / 8f; sp.Add(new Vector3(0, L * tt, 0.07f * Mathf.Sin(tt * Mathf.PI * 1.1f) - 0.03f * tt)); rr.Add(Mathf.Lerp(hen ? 0.09f : 0.075f, 0.042f, Mathf.Sqrt(tt))); cc.Add(Color.Lerp(W, Lt(W, 0.15f), tt)); }
            mb.Tube(sp, rr, cc, 12); Obj(nk, "goose_neck", mb.Build("neck"), Vector3.zero, Quaternion.identity, 0.006f);
            var hd = Group(nk, "head", sp[8] + new Vector3(0, 0.02f, 0)); rg.head = hd;
            var head = Lath(hd, new Vector3(0, 0, -0.01f), new List<Vector2> { new Vector2(0, -0.06f), new Vector2(0.04f, -0.05f), new Vector2(0.058f, -0.01f), new Vector2(0.052f, 0.04f), new Vector2(0.03f, 0.075f), new Vector2(0, 0.085f) }, Lt(W, 0.1f), 16, false, Quaternion.Euler(90, 0, 0), "goose_head");
            head.transform.localScale = new Vector3(0.9f, 1, 1);
            // beak: upper cone + hinged lower jaw + knob (Chinese goose)
            Lath(hd, new Vector3(0, -0.005f, 0.06f), new List<Vector2> { new Vector2(0, 0), new Vector2(0.03f, 0.0f), new Vector2(0.024f, 0.04f), new Vector2(0.012f, 0.075f), new Vector2(0, 0.085f) }, B, 12, false, Quaternion.Euler(96, 0, 0), "goose_beak").transform.localScale = new Vector3(1, 1, 0.7f);
            var jw = Group(hd, "jaw", new Vector3(0, -0.02f, 0.055f)); rg.jaw = jw;
            Lath(jw, Vector3.zero, new List<Vector2> { new Vector2(0, 0), new Vector2(0.022f, 0.0f), new Vector2(0.016f, 0.04f), new Vector2(0, 0.065f) }, Dk(B, 0.88f), 10, false, Quaternion.Euler(100, 0, 0), "goose_jaw").transform.localScale = new Vector3(1, 1, 0.45f);
            if (!hen) Ball(hd, new Vector3(0, 0.035f, 0.058f), new Vector3(0.036f, 0.032f, 0.036f), Dk(B, 0.9f), 10, false, null, "goose_knob", 0.01f);
            else { for (int i = 0; i < 3; i++) Ball(hd, new Vector3(0, 0.05f + (i == 1 ? 0.012f : 0), -0.02f + i * 0.025f), new Vector3(0.02f, 0.04f, 0.025f), H("d8463a"), 8, false, null, "hen_comb", 0.01f); Ball(hd, new Vector3(0, -0.045f, 0.05f), new Vector3(0.018f, 0.03f, 0.018f), H("d8463a"), 8, false, null, "hen_wattle", 0.01f); }
            foreach (int sd in new[] { -1, 1 }) { Ball(hd, new Vector3(sd * 0.044f, 0.012f, 0.02f), new Vector3(0.022f, 0.024f, 0.018f), H("2a2026"), 10, false, null, "goose_eye", 0.0f); Ball(hd, new Vector3(sd * 0.052f, 0.018f, 0.026f), Vector3.one * 0.007f, White, 6, false, null, "goose_glint", 0f); }
        }
        // legs + webbed feet
        foreach (int sd in new[] { -1, 1 })
        {
            Cyl(g, new Vector3(sd * 0.065f * S, 0.1f * S, 0.0f), 0.017f * S, 0.2f * S, B, 0.006f, null, 8, false, "goose_leg");
            var mb = new FMB { a = MatK.Plastic }; var o = new Vector3(sd * 0.065f * S, 0.012f, 0.0f);
            for (int toe = -1; toe <= 1; toe++) { var tip = o + new Vector3(toe * 0.045f * S, 0, 0.1f * S); var nxt = o + new Vector3((toe + 1) * 0.045f * S, 0, 0.1f * S); if (toe < 1) mb.Tri(o, tip + Vector3.up * 0.001f, nxt, Dk(B, 0.95f), true); }
            Obj(g, "goose_foot", mb.Build("foot"), Vector3.zero, Quaternion.identity, 0.004f);
        }
        MatA = om;
        return rg;
    }

    // ---------------- 傳統紅瓦斜屋頂 for the farmhouse (replaces the aunt's upper storeys). local house coords: x -9.3..9.3, z -0.2..12.3
    public static void TiledRoof(Transform h, float y0, float x0, float x1, float z0, float z1, float rise)
    {
        float zc = (z0 + z1) / 2, half = (z1 - z0) / 2; var mb = new FMB { a = MatK.Roof };
        int rows = 14, cols = Mathf.RoundToInt((x1 - x0) / 0.32f);
        for (int sd = -1; sd <= 1; sd += 2)
        {
            for (int r = 0; r < rows; r++)
            {
                float t0 = r / (float)rows, t1 = (r + 1) / (float)rows;
                for (int cI = 0; cI < cols; cI++)
                {
                    float xa = x0 + (x1 - x0) * cI / cols, xb = x0 + (x1 - x0) * (cI + 1) / cols, xm = (xa + xb) / 2;
                    Vector3 P(float x, float tt, float lift) => new Vector3(x, y0 + rise * tt + lift, zc + sd * half * (1 - tt));
                    var col = Color.Lerp(Dk(TileR, 0.92f), Lt(TileR, 0.08f), Mathf.Repeat(Mathf.Sin(cI * 12.9898f + r * 78.233f) * 43758.5f, 1f)); if (r == 0) col = Dk(col, 0.9f);
                    // a barrel tile: raised middle, low edges, slight overlap step at the lower end
                    var a0 = P(xa, t0, 0.0f); var m0 = P(xm, t0, 0.07f); var b0 = P(xb, t0, 0.0f); var a1 = P(xa, t1, 0.03f); var m1 = P(xm, t1, 0.1f); var b1 = P(xb, t1, 0.03f);
                    if (sd > 0) { mb.Quad(a0, a1, m1, m0, col); mb.Quad(m0, m1, b1, b0, Dk(col, 0.93f)); } else { mb.Quad(a0, m0, m1, a1, col); mb.Quad(m0, b0, b1, m1, Dk(col, 0.93f)); }
                }
            }
            // soffit underside + wooden fascia board
            var s0 = new Vector3(x0, y0 - 0.02f, zc + sd * half); var s1 = new Vector3(x1, y0 - 0.02f, zc + sd * half); var s2 = new Vector3(x1, y0 + rise - 0.02f, zc); var s3 = new Vector3(x0, y0 + rise - 0.02f, zc);
            if (sd > 0) mb.Quad(s0, s1, s2, s3, H("8a5a3c")); else mb.Quad(s0, s3, s2, s1, H("8a5a3c"));
        }
        // gable triangles (wall colour) at both ends
        foreach (float gx in new[] { x0 + 0.5f, x1 - 0.5f })
        {
            var g0 = new Vector3(gx, y0 - 0.02f, z0 + 0.8f); var g1 = new Vector3(gx, y0 - 0.02f, z1 - 0.8f); var g2 = new Vector3(gx, y0 + rise * 0.92f, zc);
            mb.Tri(g0, g1, g2, H("e9dcc4"), true);
        }
        var go = Obj(h, "farm_roof", mb.Build("roof"), Vector3.zero, Quaternion.identity, 0.006f);
        go.AddComponent<MeshCollider>().sharedMesh = go.GetComponent<MeshFilter>().sharedMesh;
        // ridge with swallow-tail (燕尾) upswept ends
        Cyl(h, new Vector3((x0 + x1) / 2, y0 + rise + 0.1f, zc), 0.16f, x1 - x0 - 0.4f, Dk(TileR, 0.85f), 0.05f, Quaternion.Euler(0, 0, 90), 14, false, "roof_ridge");
        foreach (int sx in new[] { -1, 1 })
        {
            float ex = sx < 0 ? x0 + 0.2f : x1 - 0.2f; var sp = new List<Vector3>(); var rr = new List<float>(); var cc = new List<Color>(); var mt = new FMB { a = MatK.Roof };
            for (int i = 0; i <= 6; i++) { float tt = i / 6f; sp.Add(new Vector3(ex + sx * 0.6f * tt, y0 + rise + 0.1f + 0.55f * tt * tt, zc)); rr.Add(Mathf.Lerp(0.16f, 0.05f, tt)); cc.Add(Dk(TileR, 0.85f)); }
            mt.Tube(sp, rr, cc, 10); Obj(h, "roof_tail", mt.Build("tail"), Vector3.zero, Quaternion.identity, 0.006f);
        }
    }

    // ---------------- bamboo clump: segmented culms + leaf sprays (no ball crowns)
    public static Transform BambooClump(Transform p, Vector3 pos, int seed, float s = 1f)
    {
        var g = Group(p, "sk_bamboo", pos); var rnd = new System.Random(seed); float Rn() => (float)rnd.NextDouble();
        var mb = new FMB { a = MatK.Foliage }; var leaves = new FMB { a = MatK.Foliage };
        int n = 7 + rnd.Next(5);
        for (int i = 0; i < n; i++)
        {
            var b = new Vector3((Rn() - 0.5f) * 1.2f, 0, (Rn() - 0.5f) * 1.2f) * s; var lean = new Vector3(b.x, 0, b.z).normalized * (0.1f + Rn() * 0.15f); float H0 = (5 + Rn() * 3) * s; int segs = 9;
            var sp = new List<Vector3>(); var rr = new List<float>(); var cc = new List<Color>();
            for (int k = 0; k <= segs; k++) { float tt = k / (float)segs; sp.Add(b + Vector3.up * H0 * tt + lean * H0 * tt * tt); rr.Add(Mathf.Lerp(0.07f, 0.03f, tt) * s); cc.Add(k % 2 == 0 ? BambooD : Bamboo); }
            mb.Tube(sp, rr, cc, 8);
            for (int k = 3; k <= segs; k++)
            {
                var at = sp[k]; int nl = 7 + rnd.Next(4);
                for (int l = 0; l < nl; l++)
                {
                    float an = Rn() * Mathf.PI * 2; var d = new Vector3(Mathf.Cos(an), -0.25f - Rn() * 0.4f, Mathf.Sin(an)).normalized; float L = (0.75f + Rn() * 0.45f) * s;
                    var lc = Color.Lerp(H("8cb85a"), H("c2d678"), Rn());
                    leaves.Ribbon(new[] { at, at + d * L * 0.5f + Vector3.up * 0.04f, at + d * L }, Vector3.Cross(d, Vector3.up).normalized, new[] { 0.03f, 0.16f * s, 0.006f }, new[] { Dk(lc, 0.85f), lc, Lt(lc, 0.1f) }, true, 0.55f);
                }
            }
        }
        Obj(g, "bamboo_culm", mb.Build("culm"), Vector3.zero, Quaternion.identity, 0.004f);
        Obj(g, "bamboo_leaf", leaves.Build("leaf"), Vector3.zero, Quaternion.identity, 0.0001f);
        var bc = g.gameObject.AddComponent<BoxCollider>(); bc.center = new Vector3(0, 1.5f, 0); bc.size = new Vector3(1.4f * s, 3, 1.4f * s);
        return g;
    }

    // ---------------- 竹籬笆 run from a to b (posts + 3 lashed rails + crossed slats)
    public static void BambooFence(Transform p, Vector3 a, Vector3 b, bool col = true)
    {
        var d = b - a; float L = d.magnitude; if (L < 0.2f) return; var g = Group(p, "sk_fence", a, Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg);
        var mb = new FMB { a = MatK.Wood };
        for (float z = 0; z <= L + 0.01f; z += 2f) { var sp = new List<Vector3> { new Vector3(0, 0, z), new Vector3(0, 1.15f, z) }; mb.Tube(sp, new List<float> { 0.05f, 0.045f }, new List<Color> { BambooD, Bamboo }, 8); }
        foreach (float y in new[] { 0.3f, 0.65f, 1.0f }) mb.Tube(new List<Vector3> { new Vector3(0.05f, y, 0), new Vector3(0.05f, y + 0.01f, L * 0.5f), new Vector3(0.05f, y, L) }, new List<float> { 0.028f, 0.028f, 0.028f }, new List<Color> { Bamboo, Lt(Bamboo, 0.1f), Bamboo }, 6);
        for (float z = 0.25f; z < L; z += 0.5f) { var c0 = Color.Lerp(Bamboo, BambooD, (z * 7.3f) % 1f); mb.Tube(new List<Vector3> { new Vector3(0.09f, 0.12f, z - 0.25f), new Vector3(0.09f, 1.05f, z + 0.25f) }, new List<float> { 0.016f, 0.014f }, new List<Color> { c0, c0 }, 5); mb.Tube(new List<Vector3> { new Vector3(0.1f, 0.12f, z + 0.25f), new Vector3(0.1f, 1.05f, z - 0.25f) }, new List<float> { 0.016f, 0.014f }, new List<Color> { c0, c0 }, 5); }
        Obj(g, "fence", mb.Build("fence"), Vector3.zero, Quaternion.identity, 0.004f);
        if (col) { var bc = g.gameObject.AddComponent<BoxCollider>(); bc.center = new Vector3(0, 0.8f, L / 2); bc.size = new Vector3(0.3f, 1.6f, L); }
    }

    // ---------------- scarecrow 稻草人 with a straw hat
    public static void Scarecrow(Transform p, Vector3 pos, float yaw)
    {
        var g = Group(p, "sk_scarecrow", pos, yaw); var om = PushM(MatK.Fabric);
        Cyl(g, new Vector3(0, 1.0f, 0), 0.04f, 2.0f, H("8a6a44"), 0.01f, null, 8); Cyl(g, new Vector3(0, 1.45f, 0), 0.035f, 1.4f, H("8a6a44"), 0.01f, Quaternion.Euler(0, 0, 90), 8);
        Lath(g, new Vector3(0, 0.95f, 0), new List<Vector2> { new Vector2(0, 0), new Vector2(0.3f, 0.02f), new Vector2(0.24f, 0.3f), new Vector2(0.2f, 0.55f), new Vector2(0, 0.6f) }, H("5f86b8"), 16, false, null, "sc_shirt").transform.localScale = new Vector3(1, 1, 0.6f);
        foreach (int sd in new[] { -1, 1 }) { Cyl(g, new Vector3(sd * 0.42f, 1.45f, 0), 0.09f, 0.5f, H("5f86b8"), 0.04f, Quaternion.Euler(0, 0, 90), 10); var mb = new FMB { a = MatK.Fabric }; for (int i = 0; i < 6; i++) { var o = new Vector3(sd * 0.68f, 1.45f, 0); var dd = new Vector3(sd, -0.3f - i * 0.1f, -0.3f + i * 0.12f).normalized; mb.Ribbon(new[] { o, o + dd * 0.18f }, Vector3.forward, new[] { 0.02f, 0.006f }, new[] { Straw, StrawL }); } Obj(g, "sc_straw", mb.Build("st"), Vector3.zero, Quaternion.identity, 0.0001f); }
        Lath(g, new Vector3(0, 1.62f, 0), new List<Vector2> { new Vector2(0, 0), new Vector2(0.16f, 0.06f), new Vector2(0.18f, 0.2f), new Vector2(0.12f, 0.33f), new Vector2(0, 0.36f) }, H("efe2c2"), 16, false, null, "sc_head");
        Text(g, new Vector3(0, 1.82f, 0.17f), Quaternion.Euler(0, 180, 0), "^ ^", 0.012f, H("4a3a30"));
        Text(g, new Vector3(0, 1.82f, -0.17f), Quaternion.identity, "^ ^", 0.012f, H("4a3a30"));
        Lath(g, new Vector3(0, 1.92f, 0), new List<Vector2> { new Vector2(0, 0), new Vector2(0.4f, 0.0f), new Vector2(0.42f, 0.03f), new Vector2(0.18f, 0.06f), new Vector2(0.16f, 0.2f), new Vector2(0, 0.22f) }, Straw, 20, false, null, "sc_hat", new List<Color> { StrawD, Straw, StrawD, H("c0483a"), Straw, Straw });
        MatA = om;
    }

    // ---------------- 竹籃 / 雞窩 / 米缸 container bodies (called from SoftProps.ContainerBody)
    public static void ContBody(Transform g, string kind, Color accent)
    {
        var om = PushM(MatK.Fabric);
        if (kind == "nest")
        {   // straw nest box on legs with eggs visible
            Block(g, new Vector3(0, 0.3f, 0), new Vector3(0.9f, 0.08f, 0.7f), H("a77a50"), 0.02f, false);
            foreach (int sx in new[] { -1, 1 }) foreach (int sz in new[] { -1, 1 }) Block(g, new Vector3(sx * 0.4f, 0.15f, sz * 0.3f), new Vector3(0.06f, 0.3f, 0.06f), H("8a6040"), 0.01f, false);
            foreach (int sz in new[] { -1, 1 }) Block(g, new Vector3(0, 0.45f, sz * 0.33f), new Vector3(0.9f, 0.22f, 0.04f), H("b8885a"), 0.01f, false);
            foreach (int sx in new[] { -1, 1 }) Block(g, new Vector3(sx * 0.43f, 0.45f, 0), new Vector3(0.04f, 0.22f, 0.66f), H("b8885a"), 0.01f, false);
            Lath(g, new Vector3(0, 0.34f, 0), new List<Vector2> { new Vector2(0, 0.08f), new Vector2(0.2f, 0.04f), new Vector2(0.36f, 0.12f), new Vector2(0.3f, 0.16f), new Vector2(0, 0.06f) }, Straw, 18, false, null, "nest_straw", new List<Color> { StrawD, Straw, StrawL, Straw, StrawD }).transform.localScale = new Vector3(1.15f, 1, 0.85f);
            for (int i = 0; i < 3; i++) Ball(g, new Vector3(-0.1f + i * 0.1f, 0.44f, (i % 2) * 0.05f), new Vector3(0.07f, 0.09f, 0.07f), i == 1 ? H("f6efe0") : H("e8c8a0"), 10, false, null, "egg", 0.01f);
        }
        else if (kind == "ricejar")
        {   // 米缸: glazed clay jar with wooden lid
            Lath(g, Vector3.zero, new List<Vector2> { new Vector2(0, 0), new Vector2(0.26f, 0), new Vector2(0.36f, 0.2f), new Vector2(0.38f, 0.42f), new Vector2(0.3f, 0.62f), new Vector2(0.24f, 0.7f), new Vector2(0.25f, 0.74f), new Vector2(0, 0.74f) }, H("7a4a36"), 22, false, null, "jar", new List<Color> { H("5e3a2c"), H("6e4434"), H("8a5640"), H("9a6248"), H("7a4a36"), H("6a3e30"), H("6a3e30"), H("6a3e30") });
            Cyl(g, new Vector3(0, 0.77f, 0), 0.29f, 0.05f, H("c99a66"), 0.02f, null, 20); Block(g, new Vector3(0, 0.82f, 0), new Vector3(0.22f, 0.04f, 0.05f), H("a87a50"), 0.01f, false);
            Text(g, new Vector3(0, 0.45f, 0.37f), Quaternion.Euler(0, 180, 0), "米", 0.03f, H("f3e3c0"));
        }
        else
        {   // woven bamboo basket with veg
            Lath(g, Vector3.zero, new List<Vector2> { new Vector2(0, 0), new Vector2(0.24f, 0), new Vector2(0.32f, 0.2f), new Vector2(0.36f, 0.4f), new Vector2(0.37f, 0.44f), new Vector2(0.33f, 0.44f), new Vector2(0.3f, 0.1f), new Vector2(0, 0.08f) }, Straw, 22, false, null, "basket", new List<Color> { StrawD, Straw, StrawD, Straw, StrawD, StrawD, StrawD, StrawD });
            Ring(g, new Vector3(0, 0.44f, 0), Quaternion.Euler(90, 0, 0), 0.36f, 0.025f, StrawD, 24);
            for (int i = 0; i < 4; i++) Ball(g, new Vector3(-0.12f + (i % 2) * 0.22f, 0.4f, -0.08f + (i / 2) * 0.16f), new Vector3(0.14f, 0.12f, 0.14f), new[] { H("e0a040"), H("8fbf5a"), H("d0603a"), H("f0d060") }[i], 10, false, null, "veg", 0.03f);
        }
        MatA = om;
    }
}

/// v0.9.17 鵝 — the farm's alarm. Never catches the kid; seeing you means a clear 1 s warning, then a 3 s honk that calls the guard dog.
public class Goose : MonoBehaviour
{
    public enum St { Wander, Alert, Honk, Cool, Eat }
    public static readonly List<Goose> All = new List<Goose>();
    public St st = St.Wander; public float stT; public Vector3 home; public float homeR = 5f; public static int HonksRun, AlertsRun; static bool toldAlert, toldHonk;
    public const float SightR = 7.5f, Cone = 60f, RingR = 8f;
    FarmArt.Rig rig; Vector3 goal; float lostT, noiseT, peckT, phase; TextMesh mark; LineRenderer ring, pulse; Lure lure; public Vector3 seenAt;
    public static Goose Create(Transform p, Vector3 pos, Vector3 home, float yaw)
    {
        var rg = FarmArt.Goose(p, pos, yaw); var gz = rg.root.gameObject.AddComponent<Goose>(); gz.rig = rg; gz.home = home; gz.goal = pos; gz.phase = pos.x * 1.7f;
        var mk = new GameObject("mark"); mk.transform.SetParent(rg.root, false); mk.transform.localPosition = new Vector3(0, 1.05f, 0); mk.AddComponent<Billboard2>();
        gz.mark = mk.AddComponent<TextMesh>(); gz.mark.font = Art.FontBold; mk.GetComponent<MeshRenderer>().sharedMaterial = Art.WorldFontMat; gz.mark.characterSize = 0.045f; gz.mark.fontSize = 64; gz.mark.anchor = TextAnchor.MiddleCenter; gz.mark.text = "";
        gz.ring = MkRing(rg.root, "goose_ring", 64); gz.pulse = MkRing(rg.root, "goose_pulse", 48); gz.ring.enabled = gz.pulse.enabled = false;
        All.Add(gz); return gz;
    }
    static Material mAlert, mHonk, mPulse;
    static LineRenderer MkRing(Transform p, string n, int seg)
    {
        if (!mAlert) { mAlert = Art.Unlit(Data.H("f5c23a")); mHonk = Art.Unlit(Data.H("ef5a3c")); mPulse = Art.Unlit(Data.H("f7a08a")); }
        var g = new GameObject(n); g.transform.SetParent(p, false); var lr = g.AddComponent<LineRenderer>(); lr.useWorldSpace = true; lr.loop = true; lr.positionCount = seg; lr.numCornerVertices = 2;
        lr.sharedMaterial = mAlert; lr.shadowCastingMode = ShadowCastingMode.Off; lr.receiveShadows = false; return lr;
    }
    static void Circle(LineRenderer lr, Vector3 c, float r)
    {
        int n = lr.positionCount; for (int i = 0; i < n; i++) { float a = i * Mathf.PI * 2 / n; lr.SetPosition(i, new Vector3(c.x + Mathf.Cos(a) * r, c.y + 0.07f, c.z + Mathf.Sin(a) * r)); }
    }
    void OnDestroy() { All.Remove(this); }
    public static void ResetRun() { HonksRun = 0; AlertsRun = 0; toldAlert = toldHonk = false; }
    public bool Sees(Player p)
    {
        if (!p || p.hidden) return false; var eye = transform.position + Vector3.up * 0.75f; var d = p.cam.transform.position - eye; float dist = d.magnitude; if (dist > SightR) return false;
        if (dist > 2.5f && (p.Sneaking || p.NoiseR < 0.01f) && LevelGen.InRice(p.transform.position)) return false;
        var f = transform.forward; f.y = 0; var dd = d; dd.y = 0; if (Vector3.Angle(f, dd) > Cone && dist > 1.8f) return false;
        if (Physics.Raycast(eye, d.normalized, out var h, dist, Art.ShotMask, QueryTriggerInteraction.Ignore)) { if (!h.collider.GetComponentInParent<Player>()) return false; }
        return true;
    }
    void Set(St s) { st = s; stT = 0; lostT = 0; }
    public void ForceSee(Vector3 at) { if (st == St.Wander || st == St.Eat) { seenAt = at; Set(St.Alert); AlertsRun++; } }
    void Update()
    {
        float dt = Time.deltaTime; stT += dt; var p = Player.I; bool active = p != null && p.inRaid && Game.I && Game.I.RaidActive && !Game.I.InputLocked && Pet.FreezeAll <= 0;
        bool sees = active && p.alive && Sees(p);
        Vector3 move = Vector3.zero; Vector3 face = transform.forward; float spd = 0; float neckPitch = 0, jawOpen = 0, wing = 0;
        if (st == St.Wander || st == St.Eat) foreach (var l in Lure.All) if (l && l.landed && (l.transform.position - transform.position).magnitude < 7f) { lure = l; if (st != St.Eat) Set(St.Eat); break; }
        switch (st)
        {
            case St.Wander:
                {
                    var to = goal - transform.position; to.y = 0;
                    if (to.magnitude < 0.4f) { peckT += dt; neckPitch = 55 * Mathf.Clamp01(Mathf.Sin(peckT * 2.2f) * 2); if (peckT > 2.5f + (phase % 1.5f)) { peckT = 0; var r = Random.insideUnitCircle * homeR; goal = home + new Vector3(r.x, 0, r.y); } }
                    else { move = to.normalized; spd = 0.7f; face = to; }
                    if (sees) { seenAt = p.transform.position; Set(St.Alert); AlertsRun++; Sfx.At("boing", transform.position + Vector3.up * 0.7f, 0.35f, 3, 25); if (!toldAlert) { toldAlert = true; Game.I.Toast("鵝看到你了！快躲進稻田或草堆！"); } }
                    break;
                }
            case St.Eat:
                if (!lure) { Set(St.Wander); break; }
                { var to = lure.transform.position - transform.position; to.y = 0; if (to.magnitude > 0.5f) { move = to.normalized; spd = 1.3f; face = to; } else { neckPitch = 60 * Mathf.Abs(Mathf.Sin(stT * 6)); if (stT > 6) { Destroy(lure.gameObject); Set(St.Wander); } } }
                break;
            case St.Alert:
                {   // neck up, staring — a fair 1 s heads-up with the honk zone previewed
                    var to = (p ? p.transform.position : seenAt) - transform.position; to.y = 0; face = to; neckPitch = -12;
                    if (!sees) lostT += dt; else { lostT = 0; seenAt = p.transform.position; }
                    if (lostT > 0.25f) { Set(St.Cool); stT = 3.5f; break; }   // lost you: short calm-down
                    if (stT > 1.0f) { Set(St.Honk); HonksRun++; Pet.Alarm(transform.position); Sfx.At("goose", transform.position + Vector3.up * 0.6f, 1f, 4, 45); if (!toldHonk) { toldHonk = true; Game.I.Toast("鵝在叫！看門狗要來了！"); } }
                    break;
                }
            case St.Honk:
                {
                    var to = seenAt - transform.position; to.y = 0; face = to; neckPitch = 38 + 6 * Mathf.Sin(stT * 22); jawOpen = 0.5f + 0.5f * Mathf.Sin(stT * 22); wing = 0.6f + 0.4f * Mathf.Sin(stT * 16);
                    noiseT -= dt; if (noiseT <= 0) { noiseT = 0.5f; Pet.Noise(transform.position, RingR); if (stT > 0.4f) Sfx.At("goose", transform.position + Vector3.up * 0.6f, 0.8f, 4, 45); }
                    if (stT > 3f) Set(St.Cool);
                    break;
                }
            case St.Cool:
                if (stT > 6f) Set(St.Wander); break;
        }
        // move (no collider: geese never block or catch)
        if (spd > 0) { var np = transform.position + move * spd * dt; var off = np - home; off.y = 0; if (off.magnitude > homeR + 2) np = home + off.normalized * (homeR + 2) + Vector3.up * transform.position.y; transform.position = np; }
        if (face.sqrMagnitude > 0.01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(new Vector3(face.x, 0, face.z)), dt * 5);
        // animate
        float wad = spd > 0 ? Mathf.Sin(Time.time * 9 + phase) : 0;
        rig.body.localRotation = Quaternion.Euler(0, 0, wad * 7);
        rig.neck.localRotation = Quaternion.Slerp(rig.neck.localRotation, Quaternion.Euler(neckPitch + (spd > 0 ? 6 : 0), 0, wad * 3), dt * 10);
        rig.head.localRotation = Quaternion.Euler(-neckPitch * 0.4f, 0, 0);
        rig.jaw.localRotation = Quaternion.Euler(jawOpen * 28, 0, 0);
        rig.wingL.localRotation = Quaternion.Euler(-wing * 20, wing * 75, -wing * 30); rig.wingR.localRotation = Quaternion.Euler(-wing * 20, -wing * 75, wing * 30);
        // readable warning: "!" + dashed-feel yellow preview ring while alert; solid red ring + pulse while honking
        string mk = st == St.Alert ? "!" : st == St.Honk ? "嘎嘎!" : st == St.Cool && stT < 1.2f ? "?" : "";
        if (mark.text != mk) mark.text = mk; mark.color = st == St.Honk ? Data.H("e8503a") : Data.H("f2b630");
        mark.transform.localScale = Vector3.one * (st == St.Alert ? 1 + 0.25f * Mathf.Sin(stT * 18) : 1);
        bool showRing = st == St.Honk || st == St.Alert && Mathf.Repeat(stT * 6f, 1f) < 0.7f; ring.enabled = showRing; pulse.enabled = st == St.Honk;
        if (showRing) { ring.sharedMaterial = st == St.Honk ? mHonk : mAlert; ring.widthMultiplier = st == St.Honk ? 0.18f : 0.1f; Circle(ring, transform.position, RingR); }
        if (pulse.enabled) { float k = Mathf.Repeat(stT * 1.6f, 1f); pulse.sharedMaterial = mPulse; pulse.widthMultiplier = 0.12f * (1 - k) + 0.02f; Circle(pulse, transform.position, Mathf.Max(0.3f, RingR * k)); }
    }
}
