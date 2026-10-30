using System.Collections.Generic;
using UnityEngine;
using static SoftKit;

/// v0.9.4: recognisable building dressing (shop-window goods per shop type, potted plants, table settings)
/// replacing the old colour-ball placeholders. Positions are bottom-centre on the supporting surface.
/// Segment counts go through GeoQ.Seg (cheaper on WebGL/mobile); tier 0 also drops the smallest detail pieces.
public static class Goods
{
    static bool Lo => GeoQ.Tier == 0;
    static Quaternion Q(float x, float y, float z) => Quaternion.Euler(x, y, z);
    static void T(GameObject g, float k) { if (g) SoftKit.Tag(g.transform, k); }
    static Color Dk(Color c, float k) => new Color(c.r * k, c.g * k, c.b * k, c.a);
    static Color Lt(Color c, float k) => Color.Lerp(c, Color.white, k);
    static readonly Color Terra = H("d98c62"), Soil = H("7a5640"), Crust = H("d99a52"), CrustD = H("b8733a"), Ice = H("f4f6ff"), Glassy = H("dff0f4");

    // ---------- plants ----------
    public static void Blade(Transform p, Vector3 at, float len, float yaw, float tilt, Color c)
    {   // flattened leaf blade, base at 'at', pointing outward/upward
        var r = Q(tilt, yaw, 0); var g = Ball(p, at + r * new Vector3(0, 0, len * 0.5f), new Vector3(len * 0.4f, len * 0.06f, len), c, 8, false, r, "sk_leaf", 0.02f); T(g, MatK.Foliage);
    }
    public static void Flower(Transform p, Vector3 at, float s, Color petal)
    {
        int n = Lo ? 4 : 6; for (int i = 0; i < n; i++) { float a = i * 360f / n; var r = Q(-15, a, 0); var g = Ball(p, at + r * new Vector3(0, 0, s * 0.55f), new Vector3(s * 0.55f, s * 0.14f, s * 0.8f), petal, 6, false, r, "sk_petal", 0.02f); T(g, MatK.Fabric); }
        Ball(p, at + Vector3.up * s * 0.06f, Vector3.one * s * 0.45f, ButterD, 6, false, null, "sk_bud");
    }
    /// leafy shrub: clustered leaf blades around a soft core (no big lump silhouette)
    public static void Shrub(Transform p, Vector3 b, float rad, float h, Color leaf, Color? flower, int seed)
    {   // v0.9.6: stems radiate up/out from the crown; every leaf is attached to a stem point (no core blob, no floating cards)
        var rng = new System.Random(seed); float F() => (float)rng.NextDouble();
        int ns = Lo ? 5 : 8; float sr = Mathf.Max(0.006f, rad * 0.035f); int fl = 0;
        for (int s = 0; s < ns; s++)
        {
            float a = s * 360f / ns + F() * 25f, el = 40f + F() * 30f, L = Mathf.Max(h, rad) * (0.6f + 0.35f * F());
            var rot = Q(-el, a, 0); var dir = rot * Vector3.forward; var tip = b + dir * L;
            var st = Cyl(p, b + dir * L * 0.5f, sr, L, Dk(leaf, 0.6f), sr * 0.5f, Quaternion.FromToRotation(Vector3.up, dir), 5, false, "sk_stem"); T(st, MatK.Foliage);
            var c = Color.Lerp(leaf, Dk(leaf, 0.8f), F());
            foreach (float t in new[] { 0.35f, 0.68f })
                foreach (int sd in new[] { -1, 1 }) Blade(p, b + dir * L * t, rad * (0.62f - 0.25f * Mathf.Abs(t - 0.55f) * 2f) * (0.8f + 0.35f * F()), a + sd * (40f + 15f * F()), -(el * 0.5f + 5f), sd > 0 ? c : Lt(c, 0.06f));
            Blade(p, tip, rad * (0.42f + 0.15f * F()), a, -(el * 0.7f), Lt(c, 0.08f));
            if (!Lo) {   // forked side branch: thinner, shorter, its own tapering leaves
                var fp = b + dir * L * 0.5f; float fa = a + (s % 2 == 0 ? 38f : -38f); var fd = Q(-(el - 12f), fa, 0) * Vector3.forward; float fl2 = L * 0.45f;
                T(Cyl(p, fp + fd * fl2 * 0.5f, sr * 0.7f, fl2, Dk(leaf, 0.6f), sr * 0.35f, Quaternion.FromToRotation(Vector3.up, fd), 5, false, "sk_stem"), MatK.Foliage);
                Blade(p, fp + fd * fl2, rad * (0.36f + 0.12f * F()), fa, -(el * 0.6f), Color.Lerp(c, Lt(leaf, 0.1f), F()));
                Blade(p, fp + fd * fl2 * 0.6f, rad * 0.3f, fa + 50f, -(el * 0.4f), c); }
            if (flower.HasValue && fl < (Lo ? 2 : 4) && s % 2 == 0) { Flower(p, tip + Vector3.up * rad * 0.08f, rad * 0.3f, fl % 2 == 0 ? flower.Value : Lt(flower.Value, 0.3f)); fl++; }
        }
        for (int k = 0; k < 2; k++) Blade(p, b, rad * 0.7f, k * 180f + F() * 40f, -70f, Lt(leaf, 0.05f));   // upright heart leaves
    }
    public static void PotPlant(Transform p, Vector3 b, float s, Color leaf, Color? flower, int seed)
    {   // terracotta pot (tapered lathe with rim) + soil + leaves
        var pot = Lath(p, b, new[] { new Vector2(0, 0), new Vector2(s * 0.36f, 0), new Vector2(s * 0.46f, s * 0.62f), new Vector2(s * 0.52f, s * 0.64f), new Vector2(s * 0.52f, s * 0.78f), new Vector2(s * 0.44f, s * 0.78f), new Vector2(0, s * 0.72f) }, Terra, 14, false, null, "sk_pot"); T(pot, MatK.Plastic);
        Shrub(p, b + Vector3.up * s * 0.7f, s * 0.55f, s * 0.9f, leaf, flower, seed);
    }
    // ---------- shop goods ----------
    static void Bowl(Transform p, Vector3 b, float R, Color c, Color fill, Color? top)
    {
        var g = Lath(p, b, new[] { new Vector2(0, 0), new Vector2(R * 0.5f, 0), new Vector2(R * 0.55f, R * 0.12f), new Vector2(R, R * 0.6f), new Vector2(R * 1.04f, R * 0.66f), new Vector2(0, R * 0.62f) }, c, 14, false, null, "sk_bowl"); T(g, MatK.Plastic);
        var m = Lath(p, b + Vector3.up * R * 0.55f, new[] { new Vector2(0, 0), new Vector2(R * 0.98f, 0), new Vector2(R * 0.6f, R * 0.45f), new Vector2(0, R * 0.75f) }, fill, 12, false, null, "sk_ice"); T(m, MatK.Fabric);
        if (top.HasValue) Ball(p, b + Vector3.up * R * 1.22f, new Vector3(R * 0.8f, R * 0.22f, R * 0.8f), top.Value, 8, false, null, "sk_syrup");
    }
    static void Cone(Transform p, Vector3 b, float s, Color scoop)
    {
        var g = Lath(p, b, new[] { new Vector2(0, 0), new Vector2(s * 0.22f, s * 0.75f), new Vector2(s * 0.26f, s * 0.8f), new Vector2(0, s * 0.8f) }, Crust, 10, false, null, "sk_cone"); T(g, MatK.Wood);
        Ball(p, b + Vector3.up * s * 0.95f, Vector3.one * s * 0.48f, scoop, 10, false, null, "sk_scoop");
    }
    static void Loaf(Transform p, Vector3 b, float L, float yaw)
    {
        var r = Q(0, yaw, 0); Block(p, b + Vector3.up * L * 0.2f, new Vector3(L * 0.5f, L * 0.4f, L), Crust, L * 0.19f, false, yaw);
        if (!Lo) for (int i = -1; i <= 1; i++) Block(p, b + Vector3.up * L * 0.4f + r * new Vector3(0, 0, i * L * 0.26f), new Vector3(L * 0.36f, 0.015f, 0.03f), CrustD, 0.006f, false, yaw + 25);
    }
    static void Croissant(Transform p, Vector3 b, float s) { var g = Ring(p, b + Vector3.up * s * 0.16f, Q(90, 0, 0), s * 0.32f, s * 0.15f, Crust, 10); g.transform.localScale = new Vector3(1, 1, 0.75f); }
    static void Book(Transform p, Vector3 b, float h, float th, float yaw, Color c)
    {   // v0.9.9 plain block + illustrated spine on both narrow faces (was a bare coloured block)
        Block(p, b + Vector3.up * h / 2, new Vector3(th, h, h * 0.72f), c, 0.008f, false, yaw);
        var r = Q(0, yaw, 0); string art = "art_sp" + (Mathf.Abs(Mathf.RoundToInt(b.x * 37 + b.z * 53 + h * 100)) % 8);
        SoftProps.Picture(p, b + Vector3.up * h / 2 + r * new Vector3(0, 0, -h * 0.36f - 0.002f), r, th * 0.96f, h * 0.96f, art);
        SoftProps.Picture(p, b + Vector3.up * h / 2 + r * new Vector3(0, 0, h * 0.36f + 0.002f), r * Q(0, 180, 0), th * 0.96f, h * 0.96f, art);
    }
    static void Jar(Transform p, Vector3 b, float s, Color content)
    {
        var g = Cyl(p, b + Vector3.up * s * 0.5f, s * 0.3f, s, Lt(Glassy, 0.2f), s * 0.08f, null, 12, false, "sk_jar"); T(g, MatK.Glass);
        Cyl(p, b + Vector3.up * s * 0.38f, s * 0.26f, s * 0.7f, content, s * 0.05f, null, 10, false, "sk_jarfill");
        var l = Cyl(p, b + Vector3.up * s * 1.04f, s * 0.31f, s * 0.12f, Coral, 0.01f, null, 12, false, "sk_lid"); T(l, MatK.Metal);
    }
    static void Can(Transform p, Vector3 b, float s, Color label)
    {
        var g = Cyl(p, b + Vector3.up * s * 0.5f, s * 0.28f, s, Grey, 0.01f, null, 12, false, "sk_can"); T(g, MatK.Metal);
        Cyl(p, b + Vector3.up * s * 0.5f, s * 0.285f, s * 0.6f, Lt(label, 0.5f), 0.005f, null, 12, false, "sk_label");
        SoftProps.Picture(p, b + Vector3.up * s * 0.5f + new Vector3(0, 0, s * 0.29f), Q(0, 180, 0), s * 0.42f, s * 0.58f, "art_pk3");   // v0.9.9 illustrated can label
    }
    static void Bottle(Transform p, Vector3 b, float s, Color c)
    {
        var g = Lath(p, b, new[] { new Vector2(0, 0), new Vector2(s * 0.16f, 0), new Vector2(s * 0.16f, s * 0.62f), new Vector2(s * 0.06f, s * 0.8f), new Vector2(s * 0.06f, s * 0.95f), new Vector2(0, s * 0.95f) }, c, 10, false, null, "sk_bottle"); T(g, MatK.Plastic);
        Cyl(p, b + Vector3.up * s, s * 0.07f, s * 0.1f, White, 0.01f, null, 8);
    }
    static void Bouquet(Transform p, Vector3 b, float s, Color c)
    {   // wrapped paper cone with flowers
        var g = Lath(p, b, new[] { new Vector2(0, 0), new Vector2(s * 0.1f, 0), new Vector2(s * 0.3f, s * 0.7f), new Vector2(s * 0.26f, s * 0.72f), new Vector2(0, s * 0.5f) }, Cream, 10, false, null, "sk_wrap"); T(g, MatK.Fabric);
        int n = Lo ? 2 : 3; for (int i = 0; i < n; i++) Flower(p, b + Vector3.up * s * 0.74f + Q(-20, i * 120, 0) * new Vector3(0, 0, s * 0.12f), s * 0.3f, i == 1 ? Lt(c, 0.35f) : c);
        Blade(p, b + Vector3.up * s * 0.6f, s * 0.3f, 60, -40, LeafD); Blade(p, b + Vector3.up * s * 0.6f, s * 0.3f, 240, -40, Leaf);
    }
    static void Toy(Transform p, Vector3 b, float s, Color c, int k)
    {
        if (k % 2 == 0) { Block(p, b + Vector3.up * s * 0.3f, new Vector3(s * 0.7f, s * 0.6f, s * 0.6f), c, 0.03f, false); Block(p, b + Vector3.up * s * 0.75f, new Vector3(s * 0.5f, s * 0.3f, s * 0.5f), Lt(c, 0.4f), 0.03f, false); }   // stacking blocks
        else { Ball(p, b + Vector3.up * s * 0.3f, new Vector3(s * 0.55f, s * 0.6f, s * 0.5f), c, 10); Ball(p, b + Vector3.up * s * 0.78f, Vector3.one * s * 0.42f, c, 10); foreach (int sx in new[] { -1, 1 }) Ball(p, b + new Vector3(sx * s * 0.16f, s * 0.98f, 0), Vector3.one * s * 0.15f, c, 6); }   // teddy
    }
    /// fills a storefront: shelves at local y {0.6,1.2} (+0.03 board), front z ~ -0.2, width w (fr = facade frame)
    public static readonly List<(string shop, Vector3 at, Vector3 outN)> Windows = new List<(string, Vector3, Vector3)>();
    public static int ShopWindow(Transform fr, string shop, float w, int seed)
    {
        Windows.Add((shop, fr.TransformPoint(new Vector3(0, 0.95f, -0.22f)), -fr.forward));
        var rng = new System.Random(seed); int n = 0; float span = w * 0.6f; int per = Lo ? 4 : 6;
        Color[] pc = { Rose, PinkD, ButterD, Coral, H("c58fd6"), White };
        string s = shop ?? "";
        for (int row = 0; row < 2; row++)
        {
            float y = 1.23f + row * 0.6f; /* shelves above the counter; the 0.6 m shelf hides behind it */ var b0 = new Vector3(-span / 2, y, -0.22f);
            for (int i = 0; i < per; i++, n++)
            {
                var b = b0 + new Vector3(span * (i + 0.5f) / per, 0, ((i + row) % 2) * 0.04f - 0.02f); var c = pc[rng.Next(pc.Length)];
                if (s.Contains("花")) { if ((i + row) % 2 == 0) PotPlant(fr, b, 0.3f, i % 3 == 0 ? Leaf : LeafD, c, seed + n); else Bouquet(fr, b, 0.46f, c); }
                else if (s.Contains("冰") || s.Contains("豆花")) { if (row == 0) Bowl(fr, b, 0.15f, i % 2 == 0 ? White : Sky, s.Contains("豆花") ? H("f3e6c8") : Ice, s.Contains("豆花") ? (Color?)H("c9874a") : c); else Cone(fr, b, 0.36f, c); }
                else if (s.Contains("早餐") || s.Contains("麵包")) { if (i % 3 == 2) Croissant(fr, b, 0.36f); else Loaf(fr, b, 0.36f, 90 + rng.Next(-15, 15)); }
                else if (s.Contains("書") || s.Contains("文具")) { for (int j = 0; j < (Lo ? 3 : 5); j++) Book(fr, b + new Vector3(-0.08f + j * 0.04f, 0, 0), 0.22f + 0.06f * (float)rng.NextDouble(), 0.035f, 90, pc[(j + i) % pc.Length]); }
                else if (s.Contains("理髮")) { Bottle(fr, b, 0.3f, i % 2 == 0 ? Sky : Rose); }
                else if (s.Contains("玩具")) Toy(fr, b, 0.3f, c, i + row);
                else { if (i % 2 == 0) Jar(fr, b, 0.26f, c); else Can(fr, b, 0.22f, c); }
            }
        }
        return n;
    }
    // ---------- table settings ----------
    public static void FruitBowl(Transform p, Vector3 b)
    {
        Bowl(p, b, 0.2f, White, Butter, null);
        Color[] fc = { H("e2574c"), H("f2a33a"), H("e2574c") };
        for (int i = 0; i < 3; i++) { var a = b + new Vector3(-0.08f + i * 0.08f, 0.2f, (i % 2) * 0.05f - 0.02f); Ball(p, a, new Vector3(0.11f, 0.1f, 0.11f), fc[i], 10, false, null, "sk_fruit"); Cyl(p, a + Vector3.up * 0.055f, 0.006f, 0.03f, WoodD, 0.002f, null, 4); if (!Lo) Blade(p, a + Vector3.up * 0.06f, 0.04f, i * 90, -30, LeafD); }
        var ban = Ring(p, b + new Vector3(0.02f, 0.25f, -0.05f), Q(0, 20, 0), 0.08f, 0.025f, ButterD, 10); ban.transform.localScale = new Vector3(1, 0.55f, 1);
    }
    public static void Meal(Transform p, Vector3 b)
    {   // plate with rice bowl, side dish and chopsticks
        var pl = Cyl(p, b + Vector3.up * 0.01f, 0.22f, 0.02f, White, 0.008f, null, 18, false, "sk_plate"); T(pl, MatK.Plastic);
        Bowl(p, b + new Vector3(-0.07f, 0.02f, 0), 0.07f, Sky, White, null);
        var d = Cyl(p, b + new Vector3(0.09f, 0.03f, 0.04f), 0.08f, 0.02f, Cream, 0.006f, null, 14); T(d, MatK.Plastic); Ball(p, b + new Vector3(0.09f, 0.05f, 0.04f), new Vector3(0.1f, 0.03f, 0.08f), H("7fb35a"), 8);
        foreach (int s in new[] { -1, 1 }) Block(p, b + new Vector3(0.18f + s * 0.012f, 0.03f, -0.02f), new Vector3(0.01f, 0.01f, 0.22f), WoodD, 0.003f, false);
    }

    // ---------- v0.9.5: loot-shelf goods (upright, resting on the board top; heights fit under the next shelf) ----------
    static readonly Color Cookie = H("d9a25e"), CookieD = H("a8692f"), Choco = H("6b4130"), Paper = H("f6efe2");
    public static void BiscuitBox(Transform p, Vector3 b, float h, Color c, float yaw)
    {   // tall carton: body, white label band, cookie picture, darker top flap seam
        float w = h * 0.62f, d = h * 0.3f; var r = Q(0, yaw, 0);
        var g = Block(p, b + Vector3.up * h / 2, new Vector3(w, h, d), c, 0.012f, false, yaw); T(g, MatK.Plastic);
        SoftProps.Picture(p, b + Vector3.up * h * 0.5f + r * new Vector3(0, 0, d / 2 + 0.003f), r * Q(0, 180, 0), w * 0.94f, h * 0.94f, "art_pk0");   // v0.9.9 illustrated box front (was label band + primitive cookie)
        if (!Lo) Block(p, b + Vector3.up * (h - 0.01f), new Vector3(w * 1.01f, 0.012f, d * 1.01f), Dk(c, 0.8f), 0.003f, false, yaw);
    }
    public static void ChipCookie(Transform p, Vector3 c, float R, Quaternion face, bool choc = false)
    {   // v0.9.5 one recognisable cookie: golden disc, darker rim, choc chips on the visible face (local +y of face)
        Cyl(p, c, R, R * 0.28f, choc ? Choco : Cookie, R * 0.08f, face, 16, false, "sk_cookie");
        if (Lo) return; var up = face * Vector3.up;
        for (int k = 0; k < 6; k++) { float a = k * 2.4f, rr = R * (k == 0 ? 0f : 0.3f + 0.35f * ((k * 7) % 3) / 2f);
            var o = face * new Vector3(Mathf.Cos(a) * rr, 0, Mathf.Sin(a) * rr);
            Block(p, c + o + up * R * 0.15f, Vector3.one * R * 0.18f, choc ? Cookie : Choco, R * 0.04f, false, k * 37f); }
    }
    public static void CookieStack(Transform p, Vector3 b, float s)
    {   // v0.9.5 plate with stacked chip cookies + one standing cookie facing the shopper (front = -z of shelf local)
        float R = s * 0.42f;
        Cyl(p, b + Vector3.up * 0.01f, R * 1.15f, 0.02f, White, 0.008f, null, 18);
        for (int i = 0; i < 3; i++) ChipCookie(p, b + new Vector3(((i % 2) - 0.5f) * R * 0.08f, 0.03f + R * 0.14f + i * R * 0.3f, 0), R * 0.9f, Quaternion.identity, i == 1);
        ChipCookie(p, b + new Vector3(0, R * 0.95f + 0.02f, R * 0.55f), R * 0.85f, Q(80, 0, 0));
    }
    public static void Tin(Transform p, Vector3 b, float R, float h, Color c)
    {   // round biscuit tin: body, label band, slightly wider lid with rim
        var g = Cyl(p, b + Vector3.up * h * 0.45f, R, h * 0.9f, c, 0.01f, null, 16, false, "sk_tin"); T(g, MatK.Metal);
        Cyl(p, b + Vector3.up * h * 0.45f, R * 1.005f, h * 0.32f, Paper, 0.004f, null, 16);
        var l = Cyl(p, b + Vector3.up * h * 0.94f, R * 1.05f, h * 0.12f, Dk(c, 0.85f), 0.012f, null, 16); T(l, MatK.Metal);
        SoftProps.Picture(p, b + Vector3.up * (h + 0.002f), Q(90, 0, 0), R * 2.0f, R * 2.0f, "art_pk2");   // v0.9.9 illustrated lid (was a cookie disc)
    }
    public static void CookieJar(Transform p, Vector3 b, float s)
    {   // clear jar with a visible stack of cookies + lid knob
        float R = s * 0.32f;
        var g = Cyl(p, b + Vector3.up * s * 0.45f, R, s * 0.9f, Lt(Glassy, 0.35f), s * 0.06f, null, 14, false, "sk_cjar"); T(g, MatK.Glass);
        int n = Lo ? 3 : 6; for (int i = 0; i < n; i++) Cyl(p, b + new Vector3(((i % 2) - 0.5f) * R * 0.15f, s * 0.05f + i * s * 0.12f, 0), R * 0.8f, s * 0.08f, i % 3 == 2 ? Choco : (i % 2 == 0 ? Cookie : CookieD), 0.01f, Q(0, 0, (i % 2) * 6 - 3), 12);
        var l = Cyl(p, b + Vector3.up * s * 0.94f, R * 1.04f, s * 0.08f, Coral, 0.01f, null, 14); T(l, MatK.Plastic);
        Ball(p, b + Vector3.up * s * 1.01f, new Vector3(R * 0.45f, s * 0.1f, R * 0.45f), Dk(Coral, 0.85f), 8, false, null, "sk_knob");
    }
    public static void Packet(Transform p, Vector3 b, float h, Color c, float yaw)
    {   // pillow-pack snack bag standing upright: puffy body, crimped top + bottom seals, window
        float w = h * 0.7f; var r = Q(0, yaw, 0);
        var g = Block(p, b + Vector3.up * h / 2, new Vector3(w, h * 0.86f, h * 0.24f), c, h * 0.1f, false, yaw); T(g, MatK.Plastic);
        foreach (float y in new[] { 0.03f, 0.97f }) Block(p, b + Vector3.up * h * y, new Vector3(w * 1.02f, h * 0.07f, 0.012f), Dk(c, 0.85f), 0.003f, false, yaw);
        SoftProps.Picture(p, b + Vector3.up * h * 0.5f + r * new Vector3(0, 0, h * 0.125f), r * Q(0, 180, 0), w * 0.9f, h * 0.84f, "art_pk1");   // v0.9.9 illustrated bag front
    }
    public static void Crackers(Transform p, Vector3 b, float s, Color wrap)
    {   // sleeve of square crackers lying flat, stacked two rolls
        for (int k = 0; k < 2; k++) { var g = Block(p, b + new Vector3(0, s * 0.11f + k * s * 0.21f, 0), new Vector3(s * 0.95f, s * 0.2f, s * 0.4f), k == 0 ? wrap : Lt(wrap, 0.2f), s * 0.05f, false); T(g, MatK.Plastic);
            Block(p, b + new Vector3(s * 0.49f, s * 0.11f + k * s * 0.21f, 0), new Vector3(0.008f, s * 0.18f, s * 0.36f), Cookie, 0.002f, false); }
    }
    public static void DrinkBottle(Transform p, Vector3 b, float s, Color liquid)
    {   // PET bottle: clear body tinted by drink, label band, cap
        var g = Lath(p, b, new[] { new Vector2(0, 0), new Vector2(s * 0.15f, 0), new Vector2(s * 0.16f, s * 0.05f), new Vector2(s * 0.16f, s * 0.6f), new Vector2(s * 0.07f, s * 0.82f), new Vector2(s * 0.05f, s * 0.9f), new Vector2(0, s * 0.9f) }, Lt(liquid, 0.25f), 12, false, null, "sk_pet"); T(g, MatK.Glass);
        Cyl(p, b + Vector3.up * s * 0.38f, s * 0.165f, s * 0.2f, Paper, 0.004f, null, 12);
        Cyl(p, b + Vector3.up * s * 0.93f, s * 0.055f, s * 0.07f, White, 0.008f, null, 8);
    }
    public static void Figure(Transform p, Vector3 b, float s, Color c)
    {   // small collectible on a round base (body, head, ears)
        Cyl(p, b + Vector3.up * s * 0.04f, s * 0.28f, s * 0.08f, WoodD, 0.01f, null, 12);
        Ball(p, b + Vector3.up * s * 0.33f, new Vector3(s * 0.36f, s * 0.46f, s * 0.32f), c, 10);
        Ball(p, b + Vector3.up * s * 0.72f, Vector3.one * s * 0.34f, Lt(c, 0.2f), 10);
        foreach (int sx in new[] { -1, 1 }) Ball(p, b + new Vector3(sx * s * 0.13f, s * 0.88f, 0), new Vector3(s * 0.1f, s * 0.14f, s * 0.06f), c, 6);
    }
    /// fills one shelf board with goods that match the container label; bottom-centre of the board top at b, usable width w, clear height h
    public static int ShelfRow(Transform p, string label, Vector3 b, float w, float h, int row, int seed)
    {
        var rng = new System.Random(seed); string s = label ?? ""; int n = 0;
        Color[] pc = { Coral, MintD, ButterD, SkyD, Rose, LavD };
        int per = s.Contains("書") ? 1 : (s.Contains("餅乾") ? 3 : (Lo ? 3 : 4));
        for (int i = 0; i < per; i++)
        {
            float x = -w / 2 + w * (i + 0.5f) / per; var at = b + new Vector3(x, 0, 0); var c = pc[(i + row * 2 + rng.Next(2)) % pc.Length]; float hh = h * (0.78f + 0.12f * (float)rng.NextDouble());
            if (s.Contains("餅乾")) { float cw = w / per; switch ((i + row) % 3) { case 0: BiscuitBox(p, at, Mathf.Min(hh, cw * 1.35f), c, 0); break; case 1: CookieStack(p, at, Mathf.Min(hh * 0.8f, cw * 0.95f)); break; default: CookieJar(p, at, Mathf.Min(hh, cw * 1.0f)); break; } }
            else if (s.Contains("零食")) { if ((i + row) % 3 == 2) Tin(p, at, w / per * 0.32f, hh * 0.6f, c); else Packet(p, at, Mathf.Min(hh, w / per * 1.2f), c, rng.Next(-8, 8)); }
            else if (s.Contains("飲料")) { DrinkBottle(p, at, hh, c); }
            else if (s.Contains("玩具") || s.Contains("收藏")) { if ((i + row) % 2 == 0) Figure(p, at, Mathf.Min(hh, w / per), c); else Toy(p, at, Mathf.Min(hh, w / per) * 0.85f, c, i + row); }
            else if (s.Contains("書")) { int nb = Lo ? 5 : 9; float bw = w * 0.9f / nb; for (int j = 0; j < nb; j++) Book(p, at + new Vector3(-w * 0.45f + (j + 0.5f) * bw, 0, 0), hh * (0.8f + 0.2f * (float)rng.NextDouble()), bw * 0.9f, 90, pc[(j + row) % pc.Length]); }
            else { switch ((i + row) % 3) { case 0: Jar(p, at, Mathf.Min(hh * 0.85f, w / per), c); break; case 1: BiscuitBox(p, at, Mathf.Min(hh * 0.8f, w / per * 1.3f), c, 0); break; default: Can(p, at, Mathf.Min(hh * 0.8f, w / per), c); break; } }
            n++;
        }
        return n;
    }
    /// v0.9.5 bush: 5-7 leaf clusters (dark AO core + leaf blades) on short stems, irregular silhouette, petalled flowers
    public static void Bush(Transform p, Vector3 b, float s, Color leaf, Color? flower, System.Random rng)
    {
        int nc = Lo ? 5 : 8; var r2 = new System.Random(Mathf.RoundToInt(b.x * 31 + b.z * 17));
        float F() => (float)r2.NextDouble();
        for (int k = 0; k < nc; k++)
        {
            float a = k * 6.283f / nc + F() * 0.6f, rr = (0.22f + 0.14f * F()) * s, hy = (0.1f + 0.2f * F()) * s;   // v0.9.9 clumps sit low and overlap, no bare stick stems showing
            var c = b + new Vector3(Mathf.Cos(a) * rr, hy, Mathf.Sin(a) * rr);
            Shrub(p, c, (0.34f + 0.1f * F()) * s, (0.3f + 0.1f * F()) * s, k % 3 == 0 ? Lt(leaf, 0.08f) : Color.Lerp(leaf, Dk(leaf, 0.8f), F()), null, Mathf.RoundToInt(b.x * 7 + b.z * 3) + k);
        }
        Shrub(p, b + Vector3.up * 0.42f * s, 0.42f * s, 0.36f * s, Lt(leaf, 0.05f), null, Mathf.RoundToInt(b.z * 11) + 99);
        if (!Lo) for (int k = 0; k < 5; k++) { float a = k * 1.2566f + 0.6f; Shrub(p, b + new Vector3(Mathf.Cos(a) * 0.3f * s, 0.02f * s, Mathf.Sin(a) * 0.3f * s), 0.3f * s, 0.22f * s, Dk(leaf, 0.82f), null, Mathf.RoundToInt(b.x * 5) + 200 + k); }   // v0.9.9 dark skirt hides the base
        if (flower.HasValue) for (int i = 0; i < 7; i++)
        {   // keep the caller's rng draw count identical (2 per flower)
            float a = (float)rng.NextDouble() * 6.28f, y = (0.45f + (float)rng.NextDouble() * 0.3f) * s;
            if (Lo && i % 2 == 1) continue;
            Flower(p, b + new Vector3(Mathf.Cos(a) * 0.42f * s, y + 0.12f * s, Mathf.Sin(a) * 0.42f * s), 0.11f * s, i % 2 == 0 ? flower.Value : Lt(flower.Value, 0.3f));
        }
    }
}
