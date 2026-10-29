using System.Collections.Generic;
using UnityEngine;

/// SoftKit: procedural toy-like modular pieces. Every piece is bevelled/rounded, slightly wobbly (hand-made),
/// vertex-coloured with a faint crayon mottle, and rendered with the shared pastel crayon toon material.
/// No textures, no scanned/realistic assets.
public static class SoftKit
{
    public static Color H(string s) => Data.H(s);
    // ---- palette (art bible §3)
    public static readonly Color Pink = H("f3e3d0"), PinkD = H("dfb596"), Rose = H("e8a294"), Mint = H("c8ddc0"), MintD = H("98b98e"), Butter = H("ffe9a3"), ButterD = H("ffd06a"),
        Lav = H("d2e2ea"), LavD = H("b4c6a6"), Sky = H("c6e0f0"), SkyD = H("85c6f0"), Peach = H("ffd5b8"), Coral = H("ff9d8c"), Cream = H("fff3df"), White = H("fffaf2"),
        Wood = H("f2c995"), WoodD = H("cf9a68"), Leaf = H("a6db7c"), LeafD = H("78c25e"), Plum = H("6e4d5e"), Grey = H("ddd6ca"), GreyD = H("b3a998"), Roof = H("d9876a"), Teal = H("8fd8cf");

    // ---- materials
    static readonly Dictionary<int, Material> mats = new Dictionary<int, Material>();
    public static Material Mat(float outline = 0.014f)
    {
        int k = Mathf.RoundToInt(outline * 1000); if (mats.TryGetValue(k, out var m) && m) return m;
        var sh = Art.ToonShader;
        if (!sh) { m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Universal Render Pipeline/Lit")); }
        else
        {
            m = new Material(sh); m.SetColor("_BaseColor", Color.white); m.SetFloat("_UseVC", 1); m.SetFloat("_UseAvg", 0); m.SetFloat("_TexAmt", 0); m.SetFloat("_PaletteAmt", 0);
            m.SetFloat("_OutlineWidth", outline); m.SetColor("_ShadeColor", new Color(0.82f, 0.78f, 0.72f)); // v0.5: lighter warm lilac shade so north-facing facades stay pastel m.SetColor("_OutlineColor", new Color(0.42f, 0.29f, 0.32f));
        }
        m.name = "SoftKit_" + k; m.enableInstancing = true; mats[k] = m; return m;
    }

    // ---- noise
    static float Hs(int x, int y, int z, int s) { unchecked { int h = x * 374761393 + y * 668265263 + z * 1274126177 + s * 1442695041; h = (h ^ (h >> 13)) * 1274126177; return ((h ^ (h >> 16)) & 0xffff) / 65535f; } }
    public static float VN(Vector3 p, int s)
    {
        int x = Mathf.FloorToInt(p.x), y = Mathf.FloorToInt(p.y), z = Mathf.FloorToInt(p.z); float fx = p.x - x, fy = p.y - y, fz = p.z - z;
        fx = fx * fx * (3 - 2 * fx); fy = fy * fy * (3 - 2 * fy); fz = fz * fz * (3 - 2 * fz);
        float a = Mathf.Lerp(Hs(x, y, z, s), Hs(x + 1, y, z, s), fx), b = Mathf.Lerp(Hs(x, y + 1, z, s), Hs(x + 1, y + 1, z, s), fx);
        float c = Mathf.Lerp(Hs(x, y, z + 1, s), Hs(x + 1, y, z + 1, s), fx), d = Mathf.Lerp(Hs(x, y + 1, z + 1, s), Hs(x + 1, y + 1, z + 1, s), fx);
        return Mathf.Lerp(Mathf.Lerp(a, b, fy), Mathf.Lerp(c, d, fy), fz);
    }
    static Vector3 Wob(Vector3 p, int s, float amp, float f) { var q = p * f; return new Vector3(VN(q, s) - .5f, VN(q, s + 17) - .5f, VN(q, s + 41) - .5f) * 2 * amp; }
    static Color Mottle(Color c, Vector3 p, int s) { float k = 0.985f + 0.025f * VN(p * 0.6f, s + 5); return new Color(c.r * k, c.g * k, c.b * k, MatA); }
    /// material class in vertex alpha: 1 matte clay, 0.5 glossy plastic, 0.1 felt
    public static float MatA = 1f; public static float PushM(float a) { var o = MatA; MatA = a; return o; }
    static float PF(float u) { return Mathf.Pow(Mathf.Max(0, 1 - Mathf.Pow(Mathf.Abs(u), 4)), 0.35f); }
    /// inflated wall panel in XY, back at z=0, bulging toward -Z by b
    public static Mesh PillowMesh(float w, float h, float b, Color c, int seed)
    {
        var mb = new MB(); int nx = GeoQ.Seg(22), ny = GeoQ.Seg(18); float e = 0.002f;
        System.Func<float, float, Vector3> P = (u, v) => new Vector3(u * w / 2, v * h / 2, -b * PF(u) * PF(v));
        for (int j = 0; j <= ny; j++) for (int i = 0; i <= nx; i++)
        {
            float u = -1 + 2f * i / nx, v = -1 + 2f * j / ny; u = Mathf.Sign(u) * Mathf.Pow(Mathf.Abs(u), 0.6f); v = Mathf.Sign(v) * Mathf.Pow(Mathf.Abs(v), 0.6f);
            var p = P(u, v); var du = P(Mathf.Clamp(u + e, -1, 1), v) - P(Mathf.Clamp(u - e, -1, 1), v); var dv = P(u, Mathf.Clamp(v + e, -1, 1)) - P(u, Mathf.Clamp(v - e, -1, 1));
            var n = Vector3.Cross(dv, du).normalized; if (n.z > 0) n = -n; if (n.sqrMagnitude < 0.5f) n = new Vector3(Mathf.Sign(u) * Mathf.Abs(u), Mathf.Sign(v) * Mathf.Abs(v), 0).normalized;
            mb.Add(p, n, Mottle(c, p, seed));
        }
        for (int j = 0; j < ny; j++) for (int i = 0; i < nx; i++) { int q = j * (nx + 1) + i; mb.Quad(q, q + 1, q + nx + 2, q + nx + 1); }
        return mb.Build("sk_pillow");
    }
    public static GameObject Pillow(Transform p, Vector3 pos, float w, float h, float b, Color c) { return Obj(p, "sk_pillow", PillowMesh(w, h, b, c, Seed(pos, new Vector3(w, h, b))), pos, Quaternion.identity); }
    static int Seed(Vector3 a, Vector3 b) { unchecked { return (int)(a.x * 73.1f) * 31 + (int)(a.y * 91.7f) * 17 + (int)(a.z * 53.3f) * 7 + (int)(b.x * 11.3f + b.y * 7.1f + b.z * 3.7f); } }
    public static int GlobalSeed;

    // ---- mesh builder
    class MB
    {
        public readonly List<Vector3> V = new List<Vector3>(), N = new List<Vector3>(); public readonly List<Color> C = new List<Color>(); public readonly List<int> T = new List<int>();
        public int Add(Vector3 v, Vector3 n, Color c) { V.Add(v); N.Add(n); C.Add(c); return V.Count - 1; }
        public void Tri(int a, int b, int c)
        {
            var cr = Vector3.Cross(V[b] - V[a], V[c] - V[a]);
            if (cr.sqrMagnitude < 1e-14f) { T.Add(a); T.Add(b); T.Add(c); return; }
            if (Vector3.Dot(cr, N[a] + N[b] + N[c]) < 0) { T.Add(a); T.Add(c); T.Add(b); } else { T.Add(a); T.Add(b); T.Add(c); }
        }
        public void Quad(int a, int b, int c, int d) { Tri(a, b, c); Tri(a, c, d); }
        public Mesh Build(string n)
        {
            var m = new Mesh { name = n }; if (V.Count > 65000) m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            m.SetVertices(V); m.SetNormals(N); m.SetColors(C); m.SetTriangles(T, 0); m.RecalculateBounds(); return m;
        }
    }

    static float[] Axis(float hh, float r, int mid)
    {
        var l = new List<float>(); float t22 = 0.41421356f; l.Add(-hh);
        int bs = GeoQ.Bev; // v0.8: bevel steps per 45° (low 2 = v0.7)
        if (r > 0.004f) for (int k = 1; k <= bs; k++) l.Add(-hh + r * (1 - (k == bs ? 0 : (bs == 2 ? t22 : Mathf.Tan(Mathf.PI / 4 * (1 - k / (float)bs))))));
        for (int i = 1; i < mid; i++) l.Add(Mathf.Lerp(-hh + r, hh - r, i / (float)mid));
        if (r > 0.004f) for (int k = bs; k >= 1; k--) l.Add(hh - r * (1 - (k == bs ? 0 : (bs == 2 ? t22 : Mathf.Tan(Mathf.PI / 4 * (1 - k / (float)bs))))));
        l.Add(hh); return l.ToArray();
    }
    /// Rounded box centred at origin. taper: top shrinks by this fraction. top: optional top-face colour.
    public static Mesh RoundBox(Vector3 size, float r, Color c, float wob, int seed, float taper = 0, Color? top = null)
    {
        var h = size * 0.5f; r = Mathf.Clamp(r, 0, Mathf.Min(h.x, Mathf.Min(h.y, h.z)) * 0.98f); var inner = h - Vector3.one * r;
        var mb = new MB();
        float[][] ax = { Axis(h.x, r, Mathf.Clamp(Mathf.CeilToInt(size.x / 1.6f), 1, 10)), Axis(h.y, r, Mathf.Clamp(Mathf.CeilToInt(size.y / 1.6f), 1, 10)), Axis(h.z, r, Mathf.Clamp(Mathf.CeilToInt(size.z / 1.6f), 1, 10)) };
        float freq = 1f / Mathf.Max(0.35f, Mathf.Min(size.x, Mathf.Min(size.y, size.z)) * 0.9f);
        for (int a = 0; a < 3; a++) for (int sg = -1; sg <= 1; sg += 2)
        {
            int u = (a + 1) % 3, w = (a + 2) % 3; var U = ax[u]; var W = ax[w]; int b0 = mb.V.Count;
            for (int i = 0; i < U.Length; i++) for (int j = 0; j < W.Length; j++)
            {
                Vector3 p0 = Vector3.zero; p0[a] = sg * h[a]; p0[u] = U[i]; p0[w] = W[j];
                var inn = new Vector3(Mathf.Clamp(p0.x, -inner.x, inner.x), Mathf.Clamp(p0.y, -inner.y, inner.y), Mathf.Clamp(p0.z, -inner.z, inner.z));
                var d = p0 - inn; Vector3 n, pos;
                if (d.sqrMagnitude < 1e-10f) { n = Vector3.zero; n[a] = sg; pos = p0; } else { n = d.normalized; pos = inn + n * r; }
                var col = top.HasValue && n.y > 0.6f ? top.Value : c; if (n.y < -0.6f) col *= 0.88f;
                col = Mottle(col, pos, seed);
                if (taper != 0) { float k = 1 - taper * (pos.y + h.y) / (2 * h.y); pos.x *= k; pos.z *= k; }
                pos += Wob(pos, seed, wob, freq);
                mb.Add(pos, n, col);
            }
            for (int i = 0; i < U.Length - 1; i++) for (int j = 0; j < W.Length - 1; j++)
            { int q = b0 + i * W.Length + j; mb.Quad(q, q + W.Length, q + W.Length + 1, q + 1); }
        }
        return mb.Build("sk_box");
    }
    /// Surface of revolution around Y from a (radius, y) profile listed bottom -> top. Normals from the profile tangent (soft).
    public static Mesh Lathe(IList<Vector2> prof, Color c, int seg, float wob, int seed, IList<Color> ringCols = null)
    {
        seg = GeoQ.Seg(seg); var mb = new MB(); int n = prof.Count; float span = 0; for (int i = 0; i < n; i++) span = Mathf.Max(span, prof[i].x);
        float freq = 1f / Mathf.Max(0.3f, span * 1.2f);
        for (int k = 0; k < n; k++)
        {
            var t = prof[Mathf.Min(n - 1, k + 1)] - prof[Mathf.Max(0, k - 1)]; var nn = new Vector2(t.y, -t.x).normalized; if (prof[k].x < 1e-4f) nn = new Vector2(0, k == 0 ? -1 : 1);
            var rc = ringCols != null ? ringCols[k] : c;
            for (int i = 0; i <= seg; i++)
            {
                float a = i * Mathf.PI * 2 / seg; float ca = Mathf.Cos(a), sa = Mathf.Sin(a);
                var p = new Vector3(prof[k].x * ca, prof[k].y, prof[k].x * sa); var nv = new Vector3(nn.x * ca, nn.y, nn.x * sa);
                var pw = i == seg ? new Vector3(prof[k].x, prof[k].y, 0) : p; // seam-consistent wobble
                mb.Add(p + Wob(pw, seed, wob * Mathf.Min(1, prof[k].x * 4), freq), nv, Mottle(nv.y < -0.7f ? rc * 0.88f : rc, pw, seed));
            }
        }
        for (int k = 0; k < n - 1; k++) for (int i = 0; i < seg; i++) { int a = k * (seg + 1) + i, b = a + seg + 1; mb.Quad(a, b, b + 1, a + 1); }
        return mb.Build("sk_lathe");
    }
    public static List<Vector2> CylProf(float R, float Hh, float r)
    {
        var l = new List<Vector2>(); r = Mathf.Min(r, Mathf.Min(R, Hh / 2) * 0.98f); float y0 = -Hh / 2, y1 = Hh / 2;
        l.Add(new Vector2(0, y0));
        int ar = GeoQ.Arc; float da = 90f / ar;
        for (int i = 0; i <= ar; i++) { float a = Mathf.Deg2Rad * (-90 + da * i); l.Add(new Vector2(R - r + r * Mathf.Cos(a), y0 + r + r * Mathf.Sin(a))); }
        for (int i = 0; i <= ar; i++) { float a = Mathf.Deg2Rad * (da * i); l.Add(new Vector2(R - r + r * Mathf.Cos(a), y1 - r + r * Mathf.Sin(a))); }
        l.Add(new Vector2(0, y1)); return l;
    }
    public static List<Vector2> SphereProf(float R, int st = 10) { var l = new List<Vector2>(); for (int i = 0; i <= st; i++) { float a = Mathf.PI * (-0.5f + i / (float)st); l.Add(new Vector2(R * Mathf.Cos(a), R * Mathf.Sin(a))); } l[0] = new Vector2(0, -R); l[st] = new Vector2(0, R); return l; }
    /// Torus in the XY plane (axis = Z).
    public static Mesh Torus(float R, float r, Color c, int su, int sv, int seed)
    {
        su = GeoQ.Seg(su); sv = GeoQ.Seg(sv); var mb = new MB();
        for (int i = 0; i <= su; i++) for (int j = 0; j <= sv; j++)
        {
            float u = i * Mathf.PI * 2 / su, v = j * Mathf.PI * 2 / sv;
            var n = new Vector3(Mathf.Cos(v) * Mathf.Cos(u), Mathf.Cos(v) * Mathf.Sin(u), Mathf.Sin(v));
            var p = new Vector3((R + r * Mathf.Cos(v)) * Mathf.Cos(u), (R + r * Mathf.Cos(v)) * Mathf.Sin(u), r * Mathf.Sin(v));
            mb.Add(p, n, Mottle(c, p, seed));
        }
        for (int i = 0; i < su; i++) for (int j = 0; j < sv; j++) { int a = i * (sv + 1) + j, b = a + sv + 1; mb.Quad(a, b, b + 1, a + 1); }
        return mb.Build("sk_torus");
    }

    // ---- GameObject helpers
    public static GameObject Obj(Transform p, string name, Mesh m, Vector3 pos, Quaternion rot, float outline = 0.014f)
    {
        var g = new GameObject(name); g.transform.SetParent(p, false); g.transform.localPosition = pos; g.transform.localRotation = rot;
        g.AddComponent<MeshFilter>().sharedMesh = m; var mr = g.AddComponent<MeshRenderer>(); mr.sharedMaterial = Mat(outline); return g;
    }
    static float OL(float big) => Mathf.Clamp(big * 0.006f, 0.006f, 0.016f);
    public static GameObject Block(Transform p, Vector3 pos, Vector3 size, Color c, float r = 0.1f, bool col = true, float yaw = 0, float wob = -1, float taper = 0, Color? top = null, string name = "sk_block")
    {
        r = Mathf.Max(r, Mathf.Min(Mathf.Min(size.x, Mathf.Min(size.y, size.z)) * 0.3f, 0.25f)); // toy bevel floor
        int seed = Seed(pos, size) + GlobalSeed; if (wob < 0) wob = Mathf.Clamp(Mathf.Min(size.x, Mathf.Min(size.y, size.z)) * 0.06f, 0.004f, 0.04f);
        var g = Obj(p, name, RoundBox(size, r, c, wob, seed, taper, top), pos, Quaternion.Euler(0, yaw, 0), OL(Mathf.Max(size.x, Mathf.Max(size.y, size.z))));
        if (col) g.AddComponent<BoxCollider>().size = size; return g;
    }
    public static GameObject Cyl(Transform p, Vector3 pos, float R, float Hh, Color c, float r = -1, Quaternion? rot = null, int seg = 20, bool col = false, string name = "sk_cyl")
    {
        if (r < 0) r = Mathf.Min(R, Hh / 2) * 0.35f; int seed = Seed(pos, new Vector3(R, Hh, 0)) + GlobalSeed;
        var g = Obj(p, name, Lathe(CylProf(R, Hh, r), c, seg, Mathf.Min(0.03f, R * 0.06f), seed), pos, rot ?? Quaternion.identity, OL(Mathf.Max(R * 2, Hh)));
        if (col) { var bc = g.AddComponent<BoxCollider>(); bc.size = new Vector3(R * 2, Hh, R * 2); }
        return g;
    }
    public static GameObject Ball(Transform p, Vector3 pos, Vector3 size, Color c, int seg = 16, bool col = false, Quaternion? rot = null, string name = "sk_ball", float wob = 0.05f)
    {
        int seed = Seed(pos, size) + GlobalSeed; var g = Obj(p, name, Lathe(SphereProf(0.5f, Mathf.Max(6, GeoQ.Seg(seg) / 2)), c, seg, wob, seed), pos, rot ?? Quaternion.identity, OL(Mathf.Max(size.x, Mathf.Max(size.y, size.z))));
        g.transform.localScale = size; if (col) g.AddComponent<SphereCollider>().radius = 0.5f; return g;
    }
    public static GameObject Lath(Transform p, Vector3 pos, IList<Vector2> prof, Color c, int seg = 20, bool col = false, Quaternion? rot = null, string name = "sk_lathe", IList<Color> ringCols = null)
    {
        float R = 0, y0 = 1e9f, y1 = -1e9f; foreach (var v in prof) { R = Mathf.Max(R, v.x); y0 = Mathf.Min(y0, v.y); y1 = Mathf.Max(y1, v.y); }
        var g = Obj(p, name, Lathe(prof, c, seg, Mathf.Min(0.03f, R * 0.05f), Seed(pos, new Vector3(R, y1, y0)) + GlobalSeed, ringCols), pos, rot ?? Quaternion.identity, OL(Mathf.Max(R * 2, y1 - y0)));
        if (col) { var bc = g.AddComponent<BoxCollider>(); bc.center = new Vector3(0, (y0 + y1) / 2, 0); bc.size = new Vector3(R * 2, y1 - y0, R * 2); }
        return g;
    }
    /// Ring (torus) facing local +Z of rot.
    public static GameObject Ring(Transform p, Vector3 pos, Quaternion rot, float R, float r, Color c, int su = 28, string name = "sk_ring")
        => Obj(p, name, Torus(R, r, c, su, 8, Seed(pos, new Vector3(R, r, 1)) + GlobalSeed), pos, rot, Mathf.Clamp(r * 0.12f, 0.004f, 0.012f));
    /// Flat vertex-coloured field (top plane at y=0 local) with colour from a function of local xz; optional thin collider.
    public static GameObject Field(Transform p, Vector3 pos, float sx, float sz, float cell, System.Func<float, float, Color> colf, bool col = true, string name = "sk_field", float ol = 0f)
    {
        var mb = new MB(); int nx = Mathf.Max(1, Mathf.RoundToInt(sx / cell)), nz = Mathf.Max(1, Mathf.RoundToInt(sz / cell)); float cx = sx / nx, cz = sz / nz;
        for (int i = 0; i < nx; i++) for (int j = 0; j < nz; j++)
        {
            float x0 = -sx / 2 + i * cx, z0 = -sz / 2 + j * cz; var c = colf(x0 + cx / 2, z0 + cz / 2);
            int a = mb.Add(new Vector3(x0, 0, z0), Vector3.up, c), b = mb.Add(new Vector3(x0 + cx, 0, z0), Vector3.up, c), d = mb.Add(new Vector3(x0 + cx, 0, z0 + cz), Vector3.up, c), e = mb.Add(new Vector3(x0, 0, z0 + cz), Vector3.up, c);
            mb.Quad(a, b, d, e);
        }
        var g = Obj(p, name, mb.Build(name), pos, Quaternion.identity, ol);
        if (ol <= 0) g.GetComponent<MeshRenderer>().sharedMaterial = Mat(0.0001f);
        if (col) { var bc = g.AddComponent<BoxCollider>(); bc.center = new Vector3(0, -0.1f, 0); bc.size = new Vector3(sx, 0.2f, sz); }
        return g;
    }
    public static TextMesh Text(Transform p, Vector3 pos, Quaternion rot, string s, float size, Color c)
    {
        var g = new GameObject("sk_text"); g.transform.SetParent(p, false); g.transform.localPosition = pos; g.transform.localRotation = rot;
        var tm = g.AddComponent<TextMesh>(); tm.font = Art.FontBold; if (Art.FontBold) g.GetComponent<MeshRenderer>().sharedMaterial = Art.WorldFontMat;
        tm.text = s; tm.characterSize = size; tm.fontSize = 80; tm.color = c; tm.anchor = TextAnchor.MiddleCenter; return tm;
    }
    public static Transform Group(Transform p, string name, Vector3 pos, float yaw = 0) { var g = new GameObject(name).transform; g.SetParent(p, false); g.localPosition = pos; g.localRotation = Quaternion.Euler(0, yaw, 0); return g; }
    public static Quaternion FaceZ => Quaternion.identity;
    public static Quaternion AxisX => Quaternion.Euler(0, 0, 90);
    public static Quaternion AxisZ => Quaternion.Euler(90, 0, 0);
}

/// v0.8 geometry tiers (低/中/高) for SoftKit tessellation. Tied to the F8 quality level (AAQuality, applied on next map build)
/// and capped by platform: Windows 高, WebGL 中, mobile 低. "-geo N" overrides (autotest before/after). Low = v0.7 geometry.
public static class GeoQ
{
    public static int Force = -1;
    public static int Tier
    {
        get
        {
            if (Force >= 0) return Mathf.Clamp(Force, 0, 2);
            int cap = Application.isMobilePlatform ? 0 : (Application.platform == RuntimePlatform.WebGLPlayer ? 1 : 2);
            int lv = AAQuality.Level >= 0 ? AAQuality.Level : PlayerPrefs.GetInt("aa", cap);
            return Mathf.Clamp(Mathf.Min(cap, lv), 0, 2);
        }
    }
    static readonly float[] Mul = { 1f, 1.6f, 2.2f };
    static readonly int[] Bevs = { 2, 3, 5 }, Arcs = { 3, 5, 8 };
    public static int Seg(int s) => Mathf.Max(3, Mathf.RoundToInt(s * Mul[Tier]));
    public static int Bev => Bevs[Tier];
    public static int Arc => Arcs[Tier];
}
