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
        var r = Q(tilt, yaw, 0); var g = Ball(p, at + r * new Vector3(0, 0, len * 0.5f), new Vector3(len * 0.34f, len * 0.07f, len), c, 8, false, r, "sk_plant", 0.02f); T(g, MatK.Foliage);
    }
    public static void Flower(Transform p, Vector3 at, float s, Color petal)
    {
        int n = Lo ? 4 : 6; for (int i = 0; i < n; i++) { float a = i * 360f / n; var r = Q(-15, a, 0); var g = Ball(p, at + r * new Vector3(0, 0, s * 0.55f), new Vector3(s * 0.55f, s * 0.14f, s * 0.8f), petal, 6, false, r, "sk_petal", 0.02f); T(g, MatK.Fabric); }
        Ball(p, at + Vector3.up * s * 0.06f, Vector3.one * s * 0.45f, ButterD, 6, false, null, "sk_bud");
    }
    /// leafy shrub: clustered leaf blades around a soft core (no big lump silhouette)
    public static void Shrub(Transform p, Vector3 b, float rad, float h, Color leaf, Color? flower, int seed)
    {
        var rng = new System.Random(seed); float F() => (float)rng.NextDouble();
        var core = Ball(p, b + Vector3.up * h * 0.4f, new Vector3(rad * 1.2f, h * 0.6f, rad * 1.2f), Dk(leaf, 0.7f), 8, false, null, "sk_plant"); T(core, MatK.Foliage);
        int n = Lo ? 9 : 18;
        for (int i = 0; i < n; i++)
        {
            float a = i * 137.5f + F() * 20, ring = i / (float)n; var c = Color.Lerp(leaf, Dk(leaf, 0.82f), F());
            Blade(p, b + Vector3.up * (h * (0.2f + 0.55f * ring)) + Q(0, a, 0) * new Vector3(0, 0, rad * 0.25f), rad * (0.75f + 0.3f * F()), a, -20 - 45 * ring, c);
        }
        if (flower.HasValue) { int m = Lo ? 2 : 4; for (int i = 0; i < m; i++) { float a = i * 97f + 30; Flower(p, b + Vector3.up * h * (0.7f + 0.15f * F()) + Q(0, a, 0) * new Vector3(0, 0, rad * 0.55f), rad * 0.32f, i % 2 == 0 ? flower.Value : Lt(flower.Value, 0.3f)); } }
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
    static void Book(Transform p, Vector3 b, float h, float th, float yaw, Color c) { Block(p, b + Vector3.up * h / 2, new Vector3(th, h, h * 0.72f), c, 0.008f, false, yaw); }
    static void Jar(Transform p, Vector3 b, float s, Color content)
    {
        var g = Cyl(p, b + Vector3.up * s * 0.5f, s * 0.3f, s, Lt(Glassy, 0.2f), s * 0.08f, null, 12, false, "sk_jar"); T(g, MatK.Glass);
        Cyl(p, b + Vector3.up * s * 0.38f, s * 0.26f, s * 0.7f, content, s * 0.05f, null, 10, false, "sk_jarfill");
        var l = Cyl(p, b + Vector3.up * s * 1.04f, s * 0.31f, s * 0.12f, Coral, 0.01f, null, 12, false, "sk_lid"); T(l, MatK.Metal);
    }
    static void Can(Transform p, Vector3 b, float s, Color label)
    {
        var g = Cyl(p, b + Vector3.up * s * 0.5f, s * 0.28f, s, Grey, 0.01f, null, 12, false, "sk_can"); T(g, MatK.Metal);
        Cyl(p, b + Vector3.up * s * 0.5f, s * 0.285f, s * 0.6f, label, 0.005f, null, 12, false, "sk_label");
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
}
