using System.Collections.Generic;
using UnityEngine;
using static SoftKit;

/// SoftKit props: street furniture, plants, vehicles, building parts, chunky furniture and loot-container bodies.
/// Convention: a prop is a Group at pos/yaw; its "front" faces local +Z.
public static class SoftProps
{
    static Quaternion Q(float x, float y, float z) => Quaternion.Euler(x, y, z);
    static Color Dk(Color c, float k = 0.8f) => new Color(c.r * k, c.g * k, c.b * k, 1);
    static Color Lt(Color c, float k = 0.35f) => Color.Lerp(c, Color.white, k);

    // ---------------- nature
    public static Transform Tree(Transform p, Vector3 pos, int kind, float s = 1f, Color? crown = null)
    {
        var g = Group(p, "sk_tree", pos); var cr = crown ?? (kind == 1 ? Mint : Leaf);
        // v0.9.7: tapered trunk with root flare, forked branches and leafy clusters at the tips (was a trunk stick + pom-pom ball)
        int tseed = Mathf.RoundToInt(pos.x * 17.3f + pos.z * 7.1f); var tr = new System.Random(tseed); float F() => (float)tr.NextDouble();
        if (kind == 2) Cyl(g, new Vector3(0, 1.1f * s, 0), 0.17f * s, 2.2f * s, WoodD, 0.06f);
        else
        {
            var om = PushM(MatK.Wood);
            Lath(g, Vector3.zero, new[] { new Vector2(0, 0), new Vector2(0.3f, 0), new Vector2(0.2f, 0.12f), new Vector2(0.16f, 0.6f), new Vector2(0.13f, 2.2f), new Vector2(0.11f, 2.85f), new Vector2(0, 2.9f) }.Scale(s), WoodD, 10);
            for (int k = 0; k < 3; k++) { float a = k * 2.1f + F(); var rt = Block(g, new Vector3(Mathf.Cos(a) * 0.22f, 0.05f, Mathf.Sin(a) * 0.22f) * s, new Vector3(0.09f, 0.08f, 0.3f) * s, Dk(WoodD, 0.9f), 0.03f, false); rt.transform.localRotation = Q(12, -a * Mathf.Rad2Deg + 90, 0); }   // root flare
            int nb = kind == 0 ? 4 : 5; var tips = new List<Vector3>();
            for (int k = 0; k < nb; k++)
            {
                float a = k * 6.283f / nb + F() * 0.7f, y0 = (2.45f + 0.3f * F()) * s, L = (0.9f + 0.35f * F()) * s, el = 45f + 15f * F();
                var dir = new Vector3(Mathf.Cos(a) * Mathf.Sin(el * Mathf.Deg2Rad), Mathf.Cos(el * Mathf.Deg2Rad), Mathf.Sin(a) * Mathf.Sin(el * Mathf.Deg2Rad));
                var b0 = new Vector3(0, y0, 0); var b1 = b0 + dir * L; tips.Add(b1);
                Cyl(g, (b0 + b1) / 2, 0.07f * s, L, WoodD, 0.02f, Quaternion.FromToRotation(Vector3.up, dir), 7);
                var tw = b1 + (dir + new Vector3(-dir.z, 0.3f, dir.x) * 0.6f).normalized * 0.4f * s; Cyl(g, (b1 + tw) / 2, 0.035f * s, 0.4f * s, WoodD, 0.01f, Quaternion.FromToRotation(Vector3.up, (tw - b1).normalized), 6);   // twig fork
            }
            tips.Add(new Vector3(0, 3.5f * s, 0)); Cyl(g, new Vector3(0, 3.15f * s, 0), 0.09f * s, 0.7f * s, WoodD, 0.02f, null, 7);
            MatA = MatK.Foliage;
            foreach (var t in tips)
            {
                float cs = (kind == 0 ? 1.5f : 1.35f) * s * (0.85f + 0.3f * F());
                int sub = 2 + tr.Next(2);   // v0.9.8: irregular sub-clusters of leaf sprays, no smooth core blob
                for (int q = 0; q < sub; q++) { var off = new Vector3(F() - 0.5f, F() * 0.5f, F() - 0.5f) * cs * 0.9f; Goods.Bush(g, t + off - Vector3.up * 0.35f * cs, cs * (0.9f + 0.6f * F()), Color.Lerp(cr, q == 0 ? Dk(cr, 0.8f) : Lt(cr, 0.06f), F()), null, tr); }
            }
            MatA = om; Merge(g, 0, "treemesh");
        }
        if (kind == 2) new GameObject("pine_tag").transform.SetParent(g, false);
        if (kind == 2)
        {   // v0.9.8 conifer: tapered trunk, dark inner cone, 7 tiers of drooping needle branches (were 3 stacked cones)
            var om2 = PushM(MatK.Foliage);
            Lath(g, new Vector3(0, 1.0f * s, 0), new[] { new Vector2(0, 0), new Vector2(0.75f, 0.1f), new Vector2(0.12f, 3.0f), new Vector2(0, 3.1f) }.Scale(s), Dk(LeafD, 0.6f), 12);
            for (int i = 0; i < 7; i++)
            {
                float y = (1.15f + i * 0.45f) * s, R = (1.45f - i * 0.18f) * s; int nb = 11 - i;
                for (int k = 0; k < nb; k++)
                {
                    float a = (k + (i % 2) * 0.5f) * 360f / nb + F() * 12f; var rot = Q(0, a, 0) * Q(14 + F() * 10, 0, 0);
                    var col = Color.Lerp(LeafD, Dk(LeafD, 0.75f), F()); if (i > 4) col = Color.Lerp(col, Lt(LeafD, 0.12f), 0.5f);
                    Ball(g, new Vector3(0, y, 0) + rot * new Vector3(0, 0, R * 0.5f), new Vector3(R * 0.42f, 0.16f * s, R * 1.0f), col, 8, false, rot, "sk_leaf", 0.03f);
                    Ball(g, new Vector3(0, y + 0.08f * s, 0) + rot * new Vector3(0, 0, R * 0.45f), new Vector3(R * 0.22f, 0.1f * s, R * 0.8f), Lt(col, 0.08f), 6, false, rot, "sk_leaf", 0.02f);   // sunlit upper sprig
                }
            }
            Cyl(g, new Vector3(0, 4.35f * s, 0), 0.05f * s, 0.5f * s, LeafD, 0.02f, null, 6);   // leader shoot
            MatA = om2; Merge(g, 0, "treemesh");
        }
        var cc = g.gameObject.AddComponent<CapsuleCollider>(); cc.radius = 0.25f * s; cc.height = 3f * s; cc.center = new Vector3(0, 1.5f * s, 0);
        return g;
    }
    public static Transform Bush(Transform p, Vector3 pos, float s, Color c, Color? flower = null, System.Random rng = null)
    {
        var g = Group(p, "sk_bush", pos); rng = rng ?? new System.Random((int)(pos.x * 13 + pos.z * 7));
        Goods.Bush(g, Vector3.zero, s, c, flower, rng); Merge(g, 0, "sk_bush");   // v0.9.5: real foliage (was 3 balls + ball flowers)
        var bc = g.gameObject.AddComponent<BoxCollider>(); var rb = Art.RenderBounds(g.gameObject); var lc0 = g.InverseTransformPoint(rb.center); bc.size = new Vector3(rb.size.x, rb.size.y, rb.size.z) * 0.92f; bc.center = lc0;   // v0.9.5 collider = leaf footprint so the overlap audit sees the real bush
        return g;
    }
    public static Transform FlowerPot(Transform p, Vector3 pos, Color pot, Color flower, float s = 1f, bool col = true)
    {
        var g = Group(p, "sk_pot", pos);
        Lath(g, Vector3.zero, new[] { new Vector2(0, 0), new Vector2(0.2f, 0), new Vector2(0.27f, 0.36f), new Vector2(0.3f, 0.4f), new Vector2(0.24f, 0.42f), new Vector2(0, 0.42f) }.Scale(s), pot, 16);
        Goods.Shrub(g, new Vector3(0, 0.4f, 0) * s, 0.26f * s, 0.32f * s, LeafD, flower, (int)(pos.x * 13 + pos.z * 7));   // v0.9.4: leaves + petalled flowers (was balls)

        if (col) { var bc = g.gameObject.AddComponent<BoxCollider>(); bc.size = new Vector3(0.6f, 0.7f, 0.6f) * s; bc.center = new Vector3(0, 0.35f * s, 0); }
        return g;
    }
    static Vector2[] Scale(this Vector2[] a, float s) { for (int i = 0; i < a.Length; i++) a[i] *= s; return a; }
    public static void Hill(Transform p, Vector3 pos, Vector3 size, Color c) => Ball(p, pos, size, c, 24, false, null, "sk_hill", 0.03f);
    public static void Cloud(Transform p, Vector3 pos, float s)
    {
        // v0.9.7 layered cumulus: wide flat-bottomed base deck, a row of mid puffs, smaller sunlit top puffs, cool shaded underside
        var g = Group(p, "sk_cloud", pos); var r = new System.Random(Mathf.RoundToInt(pos.x * 7 + pos.z * 3)); float F() => (float)r.NextDouble();
        Color top = White, mid = Color.Lerp(White, new Color(1f, 0.93f, 0.95f), 0.5f), und = new Color(0.86f, 0.87f, 0.96f);
        Ball(g, new Vector3(0, -0.5f, 0) * s, new Vector3(9f, 1.1f, 4f) * s, und, 20, false, null, "sk_cloud", 0.02f);   // flat base deck
        int n = 6; for (int i = 0; i < n; i++) { float x = (-3.4f + i * 6.8f / (n - 1)) + (F() - 0.5f) * 0.6f, ht = 1.4f + 1.2f * Mathf.Sin((i + 0.5f) / n * Mathf.PI) + F() * 0.4f; Ball(g, new Vector3(x, ht * 0.35f - 0.2f, (F() - 0.5f) * 0.8f) * s, new Vector3(2.4f + F(), ht, 2.4f + F()) * s, mid, 16, false, null, "sk_cloud", 0.03f); }
        for (int i = 0; i < 3; i++) { float x = -1.4f + i * 1.4f + (F() - 0.5f) * 0.5f; Ball(g, new Vector3(x, 1.3f + F() * 0.4f, -0.2f) * s, new Vector3(1.8f, 1.5f, 1.8f) * s * (0.8f + 0.3f * F()), top, 16, false, null, "sk_cloud", 0.03f); }
    }

    // ---------------- street
    public static Transform StreetLamp(Transform p, Vector3 pos, float yaw)
    {
        var g = Group(p, "sk_lamp", pos, yaw);
        Cyl(g, new Vector3(0, 0.05f, 0), 0.34f, 0.1f, H("5e4330"), 0.03f, null, 16); Lath(g, new Vector3(0, 0.1f, 0), new[] { new Vector2(0, 0), new Vector2(0.28f, 0), new Vector2(0.28f, 0.06f), new Vector2(0.22f, 0.1f), new Vector2(0.24f, 0.16f), new Vector2(0.17f, 0.36f), new Vector2(0, 0.36f) }, H("4f8a84"), 20); for (int k = 0; k < 4; k++) { float a = k * Mathf.PI / 2 + 0.785f; Ball(g, new Vector3(Mathf.Cos(a) * 0.3f, 0.12f, Mathf.Sin(a) * 0.3f), new Vector3(0.06f, 0.04f, 0.06f), Butter, 8); }   // v0.9.9 r3 dark stepped plinth + bolts Cyl(g, new Vector3(0, 1.5f, 0), 0.12f, 2.8f, Mint, 0.05f); Ring(g, new Vector3(0, 0.46f, 0), Q(90, 0, 0), 0.15f, 0.04f, Butter, 18);   // v0.9.6 plinth base (was a big ball) Ring(g, new Vector3(0, 1.6f, 0), Q(90, 0, 0), 0.17f, 0.045f, Lt(Mint, 0.4f), 16);
        Ball(g, new Vector3(0, 2.95f, 0), Vector3.one * 0.38f, H("7d5a3c"), 12); for (int k = 0; k < 8; k++) { float a = k * Mathf.PI / 4; Block(g, new Vector3(Mathf.Cos(a) * 0.115f, 1.6f, Mathf.Sin(a) * 0.115f), new Vector3(0.025f, 2.1f, 0.025f), H("7d5a3c"), 0.01f, false); } foreach (float ry in new[] { 0.55f, 2.62f, 2.78f }) Ring(g, new Vector3(0, ry, 0), Q(90, 0, 0), 0.14f, 0.03f, ry > 2.7f ? Butter : H("7d5a3c"), 18);   // v0.9.9 r2 fluted shaft + collars
        Tag(Cyl(g, new Vector3(0, 3.42f, 0), 0.34f, 0.62f, Data.H("fff3c4"), 0.06f, null, 12).transform, MatK.Glass); for (int k = 0; k < 4; k++) Block(g, new Vector3(Mathf.Cos(k * 1.5708f + 0.785f) * 0.33f, 3.42f, Mathf.Sin(k * 1.5708f + 0.785f) * 0.33f), new Vector3(0.05f, 0.64f, 0.05f), H("7d5a3c"), 0.01f, false); Cyl(g, new Vector3(0, 3.1f, 0), 0.38f, 0.06f, H("7d5a3c"), 0.02f, null, 12);   // v0.9.5 lantern (was a globe ball)
        Lath(g, new Vector3(0, 3.75f, 0), new[] { new Vector2(0, 0), new Vector2(0.62f, 0), new Vector2(0.5f, 0.18f), new Vector2(0.15f, 0.36f), new Vector2(0, 0.42f) }, new[] { Rose, Sky, ButterD }[Mathf.Abs(Mathf.RoundToInt(pos.x / 16f)) % 3], 18);
        var b = Art.Prim(PrimitiveType.Sphere, g, new Vector3(0, 3.42f, 0), Vector3.one * 0.4f, Art.Emissive(Data.H("ffe9a8"), 1.6f));
        var bc = g.gameObject.AddComponent<BoxCollider>(); bc.size = new Vector3(0.4f, 3.6f, 0.4f); bc.center = new Vector3(0, 1.8f, 0);
        return g;
    }
    public static Transform Scooter(Transform p, Vector3 pos, float yaw, Color c)
    {
        var g = Group(p, "sk_scooter", pos, yaw); g.localScale = new Vector3(1.0f, 0.88f, 0.88f); var dark = Plum; var hub = Grey;
        foreach (float z in new[] { -0.62f, 0.62f }) { Cyl(g, new Vector3(0, 0.3f, z), 0.3f, 0.16f, H("3a3236"), 0.07f, Q(0, 0, 90), 22); foreach (int s in new[] { -1, 1 }) { Ring(g, new Vector3(s * 0.082f, 0.3f, z), Q(0, 90, 0), 0.2f, 0.025f, Lt(Grey, 0.3f), 20); Cyl(g, new Vector3(s * 0.085f, 0.3f, z), 0.08f, 0.03f, Butter, 0.01f, Q(0, 0, 90), 12); for (int sp = 0; sp < 5; sp++) { var spk = Block(g, new Vector3(s * 0.083f, 0.3f, z), new Vector3(0.012f, 0.3f, 0.03f), Lt(Grey, 0.2f), 0.005f, false); spk.transform.localRotation = Q(sp * 36, 0, 0); } } for (int t = 0; t < 14; t++) { float a = t * Mathf.PI * 2 / 14; var tr = Block(g, new Vector3(0, 0.3f + Mathf.Cos(a) * 0.3f, z + Mathf.Sin(a) * 0.3f), new Vector3(0.15f, 0.03f, 0.05f), Dk(H("3a3236"), 0.7f), 0.008f, false); tr.transform.localRotation = Q(a * Mathf.Rad2Deg, 0, 0); } }   // v0.9.9 r3 tyre tread, rim, spokes, hub
        Block(g, new Vector3(0, 0.42f, 0.02f), new Vector3(0.36f, 0.16f, 0.9f), c, 0.07f, false);        // floor board
        Block(g, new Vector3(0, 0.62f, -0.48f), new Vector3(0.5f, 0.5f, 0.75f), c, 0.22f, false, 0, -1, 0.15f); // rear body
        Block(g, new Vector3(0, 0.92f, -0.42f), new Vector3(0.38f, 0.14f, 0.66f), dark, 0.07f, false);   // seat
        Block(g, new Vector3(0, 0.82f, 0.5f), new Vector3(0.44f, 0.95f, 0.2f), c, 0.1f, false, 0, -1, 0.25f); // leg shield
        Block(g, new Vector3(0, 0.5f, 0.66f), new Vector3(0.3f, 0.22f, 0.45f), c, 0.1f, false);          // front fender
        Cyl(g, new Vector3(0, 1.3f, 0.52f), 0.045f, 0.35f, Grey); foreach (float ny in new[] { 1.15f, 1.3f }) Ring(g, new Vector3(0, ny, 0.52f), Q(90, 0, 0), 0.055f, 0.014f, Dk(c, 0.75f), 12); Block(g, new Vector3(0, 1.22f, 0.6f), new Vector3(0.12f, 0.08f, 0.04f), Cream, 0.01f, false); // v0.9.9 r3 neck collars + badge
        Cyl(g, new Vector3(0, 1.45f, 0.5f), 0.04f, 0.72f, Grey, 0.02f, Q(0, 0, 90), 10);
        Block(g, new Vector3(0, 1.38f, 0.6f), new Vector3(0.3f, 0.2f, 0.16f), c, 0.07f, false);
        Ball(g, new Vector3(0, 1.38f, 0.69f), new Vector3(0.14f, 0.14f, 0.06f), Butter, 10);
        foreach (int s in new[] { -1, 1 }) { Cyl(g, new Vector3(s * 0.36f, 1.45f, 0.5f), 0.035f, 0.13f, dark, 0.012f, Q(0, 0, 90), 12); for (int gr = -2; gr <= 2; gr++) Ring(g, new Vector3(s * 0.36f + gr * 0.022f, 1.45f, 0.5f), Q(0, 90, 0), 0.036f, 0.007f, Dk(dark, 0.8f), 12); Cyl(g, new Vector3(s * 0.435f, 1.45f, 0.5f), 0.04f, 0.018f, hub, 0.006f, Q(0, 0, 90), 12);   // grips
            Cyl(g, new Vector3(s * 0.25f, 1.58f, 0.5f), 0.01f, 0.24f, Grey, 0.003f, Q(0, 0, s * 15), 6); Cyl(g, new Vector3(s * 0.28f, 1.71f, 0.5f), 0.065f, 0.02f, Grey, 0.006f, Q(90, 0, 0), 12); Cyl(g, new Vector3(s * 0.28f, 1.71f, 0.508f), 0.05f, 0.005f, Lt(Sky, 0.4f), 0.002f, Q(90, 0, 0), 12); }   // v0.9.9 mirrors
        Block(g, new Vector3(0, 0.93f, -0.42f), new Vector3(0.39f, 0.012f, 0.67f), Dk(dark, 0.7f), 0.004f, false);   // seat piping
        Block(g, new Vector3(0, 0.62f, -0.87f), new Vector3(0.26f, 0.14f, 0.02f), Cream, 0.01f, false); Block(g, new Vector3(0, 0.75f, -0.86f), new Vector3(0.2f, 0.06f, 0.03f), Coral, 0.01f, false);   // plate + tail light
        Cyl(g, new Vector3(0, 1.38f, 0.695f), 0.08f, 0.012f, Grey, 0.004f, Q(90, 0, 0), 14);   // headlight rim
        var bc = g.gameObject.AddComponent<BoxCollider>(); bc.size = new Vector3(0.7f, 1.3f, 1.8f); bc.center = new Vector3(0, 0.65f, 0);
        return g;
    }
    public static Transform Bench(Transform p, Vector3 pos, float yaw, Color seat, Color leg)
    {
        var g = Group(p, "sk_bench", pos, yaw);
        // v0.9.9 slatted park bench with curled iron side frames + armrests (was two slabs on two blocks)
        for (int i = 0; i < 4; i++) Block(g, new Vector3(0, 0.45f, 0.18f - i * 0.12f), new Vector3(1.8f, 0.045f, 0.095f), Color.Lerp(seat, Dk(seat, 0.85f), i % 2 * 0.5f), 0.018f, false);
        for (int i = 0; i < 3; i++) Block(g, new Vector3(0, 0.66f + i * 0.13f, -0.25f - i * 0.025f), new Vector3(1.8f, 0.09f, 0.04f), Color.Lerp(seat, Dk(seat, 0.85f), i % 2 * 0.5f), 0.015f, false).transform.localRotation = Q(-12, 0, 0);
        foreach (int s in new[] { -1, 1 })
        {
            float x = s * 0.72f;
            Block(g, new Vector3(x, 0.21f, 0.16f), new Vector3(0.05f, 0.42f, 0.05f), leg, 0.015f, false); Block(g, new Vector3(x, 0.5f, -0.24f), new Vector3(0.05f, 1.0f, 0.05f), leg, 0.015f, false).transform.localRotation = Q(-8, 0, 0);
            Block(g, new Vector3(x, 0.41f, -0.04f), new Vector3(0.05f, 0.04f, 0.48f), leg, 0.012f, false);
            Block(g, new Vector3(x + s * 0.03f, 0.64f, 0.0f), new Vector3(0.08f, 0.04f, 0.42f), Dk(seat, 0.8f), 0.015f, false); Block(g, new Vector3(x, 0.55f, 0.17f), new Vector3(0.045f, 0.2f, 0.045f), leg, 0.012f, false);
            Cyl(g, new Vector3(x, 0.03f, 0.16f), 0.045f, 0.03f, Dk(leg, 0.8f), 0.008f, null, 10); Cyl(g, new Vector3(x, 0.03f, -0.28f), 0.045f, 0.03f, Dk(leg, 0.8f), 0.008f, null, 10);
        }
        var bc = g.gameObject.AddComponent<BoxCollider>(); bc.size = new Vector3(1.8f, 0.6f, 0.5f); bc.center = new Vector3(0, 0.3f, 0);
        return g;
    }
    public static Transform Mailbox(Transform p, Vector3 pos, float yaw)
    {
        var g = Group(p, "sk_mailbox", pos, yaw);
        Cyl(g, new Vector3(0, 0.5f, 0), 0.07f, 1f, GreyD); Block(g, new Vector3(0, 1.15f, 0), new Vector3(0.5f, 0.4f, 0.35f), Coral, 0.15f, false);
        Cyl(g, new Vector3(0, 1.35f, 0), 0.175f, 0.5f, Coral, 0.05f, Q(0, 0, 90), 16);   // v0.9.9 rounded mail-box roof
        Block(g, new Vector3(0, 1.22f, 0.176f), new Vector3(0.34f, 0.1f, 0.012f), Dk(Coral, 0.8f), 0.02f, false); Block(g, new Vector3(0, 1.22f, 0.182f), new Vector3(0.26f, 0.025f, 0.01f), Plum, 0.008f, false);   // flap + slot
        Picture(g, new Vector3(0, 1.07f, 0.184f), Q(0, 180, 0), 0.46f, 0.3f, "art_mail");   // v0.9.9 r3 illustrated MAIL plate
        { var fp = Block(g, new Vector3(0.27f, 1.3f, -0.05f), new Vector3(0.02f, 0.32f, 0.03f), GreyD, 0.006f, false); Block(g, new Vector3(0.27f, 1.42f, 0.04f), new Vector3(0.025f, 0.11f, 0.16f), H("e8473f"), 0.01f, false); }   // side flag
        foreach (float ry in new[] { 0.94f, 1.6f }) Block(g, new Vector3(0, ry, 0), new Vector3(0.53f, 0.035f, 0.39f), Dk(Coral, 0.6f), 0.015f, false);   // dark trim bands
        Block(g, new Vector3(0, 0.85f, 0), new Vector3(0.2f, 0.12f, 0.2f), GreyD, 0.03f, false); Cyl(g, new Vector3(0, 0.3f, 0), 0.085f, 0.04f, Dk(GreyD, 0.8f), 0.01f, null, 14);   // post bracket + collar
        Cyl(g, new Vector3(0, 0.02f, 0), 0.16f, 0.04f, GreyD, 0.01f, null, 14);
        var bc = g.gameObject.AddComponent<BoxCollider>(); bc.size = new Vector3(0.5f, 1.4f, 0.4f); bc.center = new Vector3(0, 0.7f, 0); return g;
    }
    public static Transform TrashBin(Transform p, Vector3 pos, Color c)
    {
        var g = Group(p, "sk_bin", pos); bool rec = c == Mint; c = rec ? H("3f9a62") : H("3f74c4"); var lid = rec ? H("f5c03a") : H("f08a3a");   // v0.9.11 saturated sorting-bin colours + contrasting lid
        Lath(g, Vector3.zero, new[] { new Vector2(0, 0), new Vector2(0.28f, 0), new Vector2(0.33f, 0.8f), new Vector2(0.36f, 0.86f), new Vector2(0, 0.9f) }, c, 18, true);
        Lath(g, new Vector3(0, 0.9f, 0), new[] { new Vector2(0, -0.02f), new Vector2(0.38f, 0), new Vector2(0.3f, 0.12f), new Vector2(0, 0.16f) }, lid, 18);
        // v0.9.9 r2: rim lip, lid seam, vertical ribs, foot ring, handle and recycle badge
        Ring(g, new Vector3(0, 0.87f, 0), Q(90, 0, 0), 0.36f, 0.025f, Dk(c, 0.7f), 20); Ring(g, new Vector3(0, 0.04f, 0), Q(90, 0, 0), 0.285f, 0.03f, Dk(c, 0.65f), 20);
        for (int k = 0; k < 8; k++) { if (k == 6) continue; float a = k * Mathf.PI / 4; float rr = 0.315f; var rib = Block(g, new Vector3(Mathf.Cos(a) * rr, 0.45f, Mathf.Sin(a) * rr), new Vector3(0.035f, 0.62f, 0.03f), Lt(c, 0.12f), 0.012f, false); rib.transform.localRotation = Q(0, -a * Mathf.Rad2Deg + 90, 4f); }
        Ring(g, new Vector3(0, 1.08f, 0), Q(0, 0, 0), 0.07f, 0.018f, Dk(lid, 0.7f), 12); Lath(g, new Vector3(0, 0.6f, 0), new[] { new Vector2(0.322f, 0), new Vector2(0.336f, 0.012f), new Vector2(0.336f, 0.1f), new Vector2(0.322f, 0.11f) }, Cream, 20); Lath(g, new Vector3(0, 0.92f, 0), new[] { new Vector2(0, -0.02f), new Vector2(0.39f, 0), new Vector2(0.31f, 0.13f), new Vector2(0, 0.17f) }, H("3f6e6a"), 20);   // r3 contrast band + dark lid
        Picture(g, new Vector3(0, 0.4f, -0.314f), Q(-3.6f, 0, 0), 0.4f, 0.2f, rec ? "art_bin0" : "art_bin1");   // v0.9.11 painted sorting label (was a tiny white disc)
        Block(g, new Vector3(0, 0.05f, -0.34f), new Vector3(0.2f, 0.035f, 0.12f), GreyD, 0.012f, false); Block(g, new Vector3(0, 0.1f, -0.29f), new Vector3(0.04f, 0.1f, 0.03f), GreyD, 0.01f, false);   // foot pedal
        return g;
    }
    public static Transform Cone(Transform p, Vector3 pos)
    {
        var g = Group(p, "sk_cone", pos); var oc = H("f26a2e"); var od = Dk(oc, 0.75f);
        Block(g, new Vector3(0, 0.03f, 0), new Vector3(0.6f, 0.06f, 0.6f), H("3a3236"), 0.05f, false);   // square rubber base w/ rounded corners
        Block(g, new Vector3(0, 0.065f, 0), new Vector3(0.48f, 0.015f, 0.48f), Dk(H("3a3236"), 0.8f), 0.02f, false);
        Lath(g, new Vector3(0, 0.06f, 0), new[] { new Vector2(0, 0), new Vector2(0.21f, 0), new Vector2(0.2f, 0.03f), new Vector2(0.06f, 0.6f), new Vector2(0.045f, 0.62f), new Vector2(0, 0.63f) }, oc, 16, true, null, "sk_conebody");
        for (int ri = 0; ri < 8; ri++) { float y = 0.12f + ri * 0.06f, r = Mathf.Lerp(0.198f, 0.065f, (y - 0.06f) / 0.6f); if (ri == 3 || ri == 5) continue; Ring(g, new Vector3(0, y, 0), Q(90, 0, 0), r + 0.004f, 0.006f, od, 20); }   // moulded ridges
        foreach (var (y0, y1) in new[] { (0.27f, 0.36f), (0.45f, 0.51f) }) { float r0 = Mathf.Lerp(0.198f, 0.065f, (y0 - 0.06f) / 0.6f) + 0.006f, r1 = Mathf.Lerp(0.198f, 0.065f, (y1 - 0.06f) / 0.6f) + 0.006f;
            var bd = Lath(g, Vector3.zero, new[] { new Vector2(0, y0), new Vector2(r0, y0), new Vector2(r1, y1), new Vector2(0, y1) }, H("f4f6f8"), 16, false, null, "sk_coneband"); Tag(bd.transform, MatK.Glass); }   // reflective bands
        return g;
    }
    public static Transform Hydrant(Transform p, Vector3 pos)
    {
        var g = Group(p, "sk_hydrant", pos); var hc = H("e03b33"); var hd = Dk(hc, 0.7f); var brass = H("e8b84a"); var om = PushM(MatK.Metal);
        Cyl(g, new Vector3(0, 0.03f, 0), 0.22f, 0.06f, hd, 0.012f, null, 16);   // ground flange
        for (int bi = 0; bi < 6; bi++) { float a = bi * Mathf.PI / 3; Cyl(g, new Vector3(Mathf.Cos(a) * 0.185f, 0.07f, Mathf.Sin(a) * 0.185f), 0.018f, 0.025f, brass, 0.004f, null, 6); }   // flange bolts
        Lath(g, new Vector3(0, 0.06f, 0), new[] { new Vector2(0, 0), new Vector2(0.16f, 0), new Vector2(0.15f, 0.2f), new Vector2(0.145f, 0.44f), new Vector2(0, 0.44f) }, hc, 16);   // barrel
        Cyl(g, new Vector3(0, 0.5f, 0), 0.19f, 0.05f, hd, 0.012f, null, 16);   // top collar
        for (int bi = 0; bi < 6; bi++) { float a = bi * Mathf.PI / 3 + 0.5f; Cyl(g, new Vector3(Mathf.Cos(a) * 0.165f, 0.53f, Mathf.Sin(a) * 0.165f), 0.014f, 0.02f, brass, 0.003f, null, 6); }
        Lath(g, new Vector3(0, 0.52f, 0), new[] { new Vector2(0, 0), new Vector2(0.17f, 0), new Vector2(0.16f, 0.05f), new Vector2(0.1f, 0.14f), new Vector2(0.05f, 0.17f), new Vector2(0, 0.18f) }, hc, 16);   // dome bonnet
        Cyl(g, new Vector3(0, 0.72f, 0), 0.035f, 0.05f, brass, 0.006f, null, 5);   // pentagon operating nut
        foreach (int sd in new[] { -1, 1 }) { Cyl(g, new Vector3(sd * 0.18f, 0.34f, 0), 0.065f, 0.07f, hc, 0.01f, Q(0, 0, 90), 14); Cyl(g, new Vector3(sd * 0.225f, 0.34f, 0), 0.075f, 0.03f, brass, 0.008f, Q(0, 0, 90), 6); Cyl(g, new Vector3(sd * 0.245f, 0.34f, 0), 0.025f, 0.02f, hd, 0.004f, Q(0, 0, 90), 5); }   // hose nozzles + hex caps
        Cyl(g, new Vector3(0, 0.34f, 0.17f), 0.085f, 0.07f, hc, 0.01f, Q(90, 0, 0), 14); Cyl(g, new Vector3(0, 0.34f, 0.215f), 0.095f, 0.035f, brass, 0.008f, Q(90, 0, 0), 6);   // pumper nozzle
        for (int ci = 0; ci < 7; ci++) { float t = ci / 6f; var cp = new Vector3(Mathf.Lerp(0.23f, 0.08f, t), Mathf.Lerp(0.3f, 0.27f, t) - Mathf.Sin(t * Mathf.PI) * 0.07f, Mathf.Lerp(0.03f, 0.2f, t));
            Ring(g, cp, Q(ci % 2 == 0 ? 0 : 90, 30, 0), 0.012f, 0.004f, H("9aa3aa"), 8); }   // cap chain
        foreach (int sd in new[] { -1, 1 }) for (int ci = 0; ci < 6; ci++) { float t = ci / 5f; Ring(g, new Vector3(sd * Mathf.Lerp(0.25f, 0.15f, t), 0.3f - Mathf.Sin(t * Mathf.PI) * 0.06f, Mathf.Lerp(0.02f, 0.12f, t)), Q(ci % 2 == 0 ? 0 : 90, 60, 0), 0.011f, 0.004f, H("9aa3aa"), 8); }   // v0.9.10 side cap chains
        Cyl(g, new Vector3(0, 0.44f, 0), 0.152f, 0.05f, H("f4efe6"), 0.006f, null, 16);   // reflective band
        Cyl(g, new Vector3(0, 0.12f, 0), 0.162f, 0.018f, hd, 0.004f, null, 16); Cyl(g, new Vector3(0, 0.28f, 0), 0.157f, 0.014f, hd, 0.004f, null, 16);   // casting seams
        for (int ri = 0; ri < 4; ri++) { float a = ri * Mathf.PI / 2 + Mathf.PI / 4; var rb2 = Block(g, new Vector3(Mathf.Cos(a) * 0.15f, 0.28f, Mathf.Sin(a) * 0.15f), new Vector3(0.018f, 0.4f, 0.018f), hd, 0.006f, false); rb2.transform.localRotation = Q(0, -a * Mathf.Rad2Deg, -2); }   // mould ribs
        Cyl(g, new Vector3(0, 0.665f, 0), 0.06f, 0.035f, hd, 0.006f, null, 12); for (int bi = 0; bi < 5; bi++) { float a = bi * Mathf.PI * 0.4f; Ball(g, new Vector3(Mathf.Cos(a) * 0.05f, 0.69f, Mathf.Sin(a) * 0.05f), Vector3.one * 0.012f, brass, 5); }   // bonnet bolt ring
        Block(g, new Vector3(0, 0.44f, 0.152f), new Vector3(0.09f, 0.03f, 0.004f), hd, 0.002f, false);   // stencil plate
        Cyl(g, new Vector3(0, 0.075f, 0), 0.163f, 0.03f, H("8a4a3a"), 0.008f, null, 16); Cyl(g, new Vector3(0, 0.004f, 0), 0.3f, 0.008f, H("9c8f7c"), 0.004f, null, 18);   // v0.9.10 rust/grime ring + damp ground stain
        for (int ch = 0; ch < 7; ch++) { float a = ch * 2.4f; Ball(g, new Vector3(Mathf.Cos(a) * 0.148f, 0.15f + (ch * 0.053f) % 0.3f, Mathf.Sin(a) * 0.148f), new Vector3(0.03f, 0.02f, 0.03f), H("f0d9c8"), 6); }   // chipped paint flecks
        MatA = om;
        var bc = g.gameObject.AddComponent<BoxCollider>(); bc.size = new Vector3(0.4f, 0.8f, 0.4f); bc.center = new Vector3(0, 0.4f, 0); return g;
    }
    public static Transform WaterJar(Transform p, Vector3 pos, Color c)
    {
        var g = Group(p, "sk_jar", pos);
        Lath(g, Vector3.zero, new[] { new Vector2(0, 0), new Vector2(0.25f, 0), new Vector2(0.38f, 0.3f), new Vector2(0.32f, 0.62f), new Vector2(0.24f, 0.68f), new Vector2(0.27f, 0.72f), new Vector2(0, 0.72f) }, c, 18, true);
        {   // v0.9.10: glazed band, rim lip, wooden lid with knob, ladle - reads as a real water urn
            var om5 = PushM(MatK.Tile); Cyl(g, new Vector3(0, 0.36f, 0), 0.378f, 0.05f, Dk(c, 0.72f), 0.01f, null, 22); Cyl(g, new Vector3(0, 0.44f, 0), 0.366f, 0.018f, H("f4efe6"), 0.005f, null, 22);
            Cyl(g, new Vector3(0, 0.715f, 0), 0.285f, 0.035f, Dk(c, 0.8f), 0.012f, null, 22); MatA = om5; var om6 = PushM(MatK.Wood);
            Cyl(g, new Vector3(0, 0.745f, 0), 0.3f, 0.03f, H("b07a4f"), 0.01f, null, 20); for (int pi = -2; pi <= 2; pi++) Block(g, new Vector3(pi * 0.11f, 0.762f, 0), new Vector3(0.006f, 0.004f, 0.5f), H("8a5a38"), 0.001f, false);
            Ball(g, new Vector3(0, 0.79f, 0), new Vector3(0.07f, 0.05f, 0.07f), H("8a5a38"), 8);
            var la = Cyl(g, new Vector3(0.16f, 0.82f, 0.08f), 0.012f, 0.32f, H("c9965f"), 0.005f, Q(0, 0, -62), 6); Lath(g, new Vector3(0.02f, 0.76f, 0.08f), new[] { new Vector2(0, 0), new Vector2(0.05f, 0.005f), new Vector2(0.055f, 0.05f), new Vector2(0, 0.05f) }, H("c9965f"), 10);
            MatA = om6;
        }
        return g;
    }

    // ---------------- building parts (all facing local -Z = outward from a facade at z=0)
    /// v0.9.7 鐵窗花 wrought-iron grille: slim frame + bars, and a fine forged motif inset inside every pane cell
    /// (outline petals / diamond lattice / sunrise fan chosen per window) - no fat balls, nothing over a bar.
    public static Transform Grille(Transform p, Vector3 center, float w, float h, Color c, bool bulge = true)
    {
        var g = Group(p, "sk_grille", center);
        float t = 0.09f, bt = 0.06f, tu = 0.016f; float d = bulge ? -0.14f : 0;
        var om = PushM(0.5f);
        Block(g, new Vector3(0, h / 2, d), new Vector3(w, t, t), c, 0.02f, false); Block(g, new Vector3(0, -h / 2, d), new Vector3(w, t, t), c, 0.02f, false);
        Block(g, new Vector3(-w / 2, 0, d), new Vector3(t, h, t), c, 0.02f, false); Block(g, new Vector3(w / 2, 0, d), new Vector3(t, h, t), c, 0.02f, false);
        int nx = Mathf.Max(1, Mathf.RoundToInt(w / 1.2f)), ny = Mathf.Max(1, Mathf.RoundToInt(h / 1.2f)); float cx = w / nx, cy = h / ny;
        for (int i = 1; i < nx; i++) Block(g, new Vector3(-w / 2 + i * cx, 0, d), new Vector3(bt, h, bt), c, 0.015f, false);
        for (int j = 1; j < ny; j++) Block(g, new Vector3(0, -h / 2 + j * cy, d), new Vector3(w, bt, bt), c, 0.015f, false);
        Vector3 wc = p.TransformPoint(center); int kind = Mathf.Abs(Mathf.RoundToInt(wc.x * 3.7f + wc.z * 2.3f + wc.y)) % 3;
        for (int i = 0; i < nx; i++) for (int j = 0; j < ny; j++)
        {
            var m = new Vector3(-w / 2 + (i + 0.5f) * cx, -h / 2 + (j + 0.5f) * cy, d);
            float hx = cx / 2 - bt / 2 - 0.05f, hy = cy / 2 - bt / 2 - 0.05f, R = Mathf.Min(hx, hy);   // usable half-extent inside the bars
            if (kind == 0)
            {   // forged lotus: 6 outline petals + ring centre + 4 corner tendrils
                for (int k = 0; k < 6; k++)
                {
                    float a = k * Mathf.PI / 3 + Mathf.PI / 2;
                    var pe = Ring(g, m + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0) * R * 0.42f, Q(0, 0, a * Mathf.Rad2Deg), R * 0.4f, tu, c, 16, "sk_iron");
                    pe.transform.localScale = new Vector3(1f, 0.42f, 1f);
                }
                Ring(g, m, Quaternion.identity, R * 0.13f, tu * 1.2f, Lt(ButterD, 0.1f), 14, "sk_iron");
                foreach (int sx in new[] { -1, 1 }) foreach (int sy in new[] { -1, 1 })
                    Ring(g, m + new Vector3(sx * (hx - R * 0.16f), sy * (hy - R * 0.16f), 0), Quaternion.identity, R * 0.14f, tu, c, 12, "sk_iron");
            }
            else if (kind == 1)
            {   // diamond lattice: diamond touching the bar mid-points, inner circle, small knot
                foreach (float k in new[] { 0.98f, 0.55f })
                {
                    var q = new[] { new Vector3(hx * k, 0, 0), new Vector3(0, hy * k, 0), new Vector3(-hx * k, 0, 0), new Vector3(0, -hy * k, 0) };
                    for (int e = 0; e < 4; e++) Bar(g, m + q[e], m + q[(e + 1) % 4], tu, c);
                }
                Arc(g, m, R * 0.3f, 0, 2 * Mathf.PI, tu, c, 14);
                Ball(g, m + new Vector3(0, 0, -0.01f), new Vector3(0.05f, 0.05f, 0.04f), c, 8);
            }
            else
            {   // sunrise fan: half rings from the bottom rail + spokes
                var b0 = m + new Vector3(0, -hy, 0); float rm = Mathf.Min(hx, 2 * hy) * 0.95f;
                foreach (float rf in new[] { 0.32f, 0.66f, 1f }) Arc(g, b0, rm * rf, 0, Mathf.PI, tu, c, 10);
                for (int k = 1; k < 6; k++) { float a = k * Mathf.PI / 6; Block(g, b0 + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0) * rm * 0.66f, new Vector3(tu * 2, rm * 0.66f, tu * 2), c, 0.005f, false).transform.localRotation = Q(0, 0, a * Mathf.Rad2Deg - 90); }
            }
        }
        MatA = om;
        if (bulge) foreach (int sx in new[] { -1, 1 }) foreach (int sy in new[] { -1, 1 }) Block(g, new Vector3(sx * w / 2, sy * h / 2, d / 2), new Vector3(0.04f, 0.04f, Mathf.Abs(d)), c, 0.015f, false);
        return g;
    }
    /// thin iron bar between two local points (XY plane)
    public static void Bar(Transform g, Vector3 a, Vector3 b, float tu, Color c)
    {
        var dlt = b - a; Block(g, (a + b) / 2, new Vector3(tu * 2, dlt.magnitude + tu, tu * 2), c, 0.004f, false).transform.localRotation = Q(0, 0, Mathf.Atan2(dlt.y, dlt.x) * Mathf.Rad2Deg - 90);
    }
    /// thin forged arc from block segments (XY plane), angles in radians
    public static void Arc(Transform g, Vector3 c0, float R, float a0, float a1, float tu, Color c, int n)
    {
        for (int k = 0; k < n; k++)
        {
            float u0 = a0 + (a1 - a0) * k / n, u1 = a0 + (a1 - a0) * (k + 1) / n, um = (u0 + u1) / 2;
            Block(g, c0 + new Vector3(Mathf.Cos(um), Mathf.Sin(um), 0) * R, new Vector3(tu * 2, 2 * R * Mathf.Sin((u1 - u0) / 2) + tu, tu * 2), c, 0.004f, false).transform.localRotation = Q(0, 0, um * Mathf.Rad2Deg);
        }
    }
    /// round window (quatrefoil if petals) in the XY plane; glass sits at z=0, frame proud toward -Z
    /// v0.9.7 oculus window: dim recess + glass, moulded frame, fine wheel/quatrefoil tracery (no fat rings, no glint sticker ball)
    public static Transform RoundWin(Transform p, Vector3 center, float R, Color frame, bool petals)
    {
        var g = Group(p, "sk_roundwin", center); var om = PushM(0.5f);
        Cyl(g, new Vector3(0, 0, 0.05f), R, 0.03f, new Color(0.42f, 0.34f, 0.38f), 0.005f, Q(90, 0, 0), 24);   // dim room behind
        Tag(Cyl(g, new Vector3(0, 0, 0.0f), R, 0.02f, Color.Lerp(Sky, new Color(0.42f, 0.34f, 0.38f), 0.55f), 0.005f, Q(90, 0, 0), 24).transform, MatK.Glass);
        float tu = Mathf.Max(0.018f, R * 0.035f);
        Ring(g, new Vector3(0, 0, -0.06f), Quaternion.identity, R, Mathf.Max(0.07f, R * 0.11f), frame, 32);
        Ring(g, new Vector3(0, 0, -0.1f), Quaternion.identity, R + 0.08f, Mathf.Max(0.05f, R * 0.07f), Lt(frame, 0.3f), 32);
        if (petals) { int pv = Mathf.Abs(Mathf.RoundToInt(center.x * 7.3f + center.y * 3.1f + center.z * 5.7f + R * 11f)) % 3; int pn = pv == 0 ? 4 : pv == 1 ? 6 : 8; float pr = pv == 0 ? 0.36f : pv == 1 ? 0.27f : 0.2f, pd = pv == 0 ? 0.42f : pv == 1 ? 0.5f : 0.6f;   // v0.9.9 medallion varies per window (4/6/8 petals)
            for (int k = 0; k < pn; k++) { float a = k * Mathf.PI * 2 / pn + Mathf.PI / 4; Ring(g, new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0) * R * pd + new Vector3(0, 0, -0.05f), Quaternion.identity, R * pr, tu, frame, 24); } Ring(g, new Vector3(0, 0, -0.05f), Quaternion.identity, R * 0.16f, tu, frame, 18); }
        else { for (int k = 0; k < 8; k++) { float a = k * Mathf.PI / 4; Bar(g, new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0) * R * 0.18f + new Vector3(0, 0, -0.05f), new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0) * R * 0.92f + new Vector3(0, 0, -0.05f), tu, frame); } Ring(g, new Vector3(0, 0, -0.05f), Quaternion.identity, R * 0.18f, tu * 1.3f, frame, 18); }
        MatA = om;
        return g;
    }
    /// rectangular window with optional arched top + sill + flower box; glass at z=0 facing -Z
    static Material WinLampM;
    public static readonly Dictionary<Transform, Vector2> WinDim = new Dictionary<Transform, Vector2>();
    public static readonly List<(Vector3 at, Vector3 outN, Vector3 rt)> WinSpots = new List<(Vector3, Vector3, Vector3)>();
    public static Transform Win(Transform p, Vector3 center, float w, float h, Color frame, bool arch, Color? flowers = null, bool grille = false)
    {   // v0.9.6: real window: recessed reveal, thin frame with depth, mullions, sill, dim interior + curtains, soft glass streaks (no opaque sky slab, no glow sticker)
        var g = Group(p, "sk_win", center); WinSpots.Add((g.position, -g.forward, g.right)); WinDim[g] = new Vector2(w, h); var om = PushM(0.5f);
        int hs = Mathf.Abs(Mathf.RoundToInt(p.TransformPoint(center).x * 7.3f + p.TransformPoint(center).y * 3.1f + p.TransformPoint(center).z * 5.7f));
        float rise = arch ? Mathf.Min(w / 2, 0.42f) : 0f, fb = 0.1f, dep = 0.13f;
        Color inside = Color.Lerp(new Color(0.36f, 0.28f, 0.32f), new Color(0.5f, 0.4f, 0.34f), (hs % 5) / 4f);
        Block(g, new Vector3(0, 0, 0.03f), new Vector3(w, h, 0.04f), inside, 0.01f, false);                                // room back wall (recess)
        Block(g, new Vector3(0, h / 2 - 0.12f, 0.0f), new Vector3(w * 0.9f, 0.06f, 0.03f), new Color(1f, 0.82f, 0.6f), 0.01f, false);   // warm lit ceiling edge
        foreach (int sx in new[] { -1, 1 }) Block(g, new Vector3(sx * w / 2, 0, -0.07f), new Vector3(0.03f, h, 0.2f), Dk(frame, 0.7f), 0.005f, false);   // reveal sides
        Block(g, new Vector3(0, -h / 2, -0.07f), new Vector3(w, 0.03f, 0.2f), Dk(frame, 0.7f), 0.005f, false);
        Color[] cc = { new Color(0.98f, 0.93f, 0.86f), new Color(0.95f, 0.82f, 0.86f), new Color(0.86f, 0.9f, 0.96f), new Color(0.95f, 0.9f, 0.72f) };
        Color cu = cc[hs % 4]; float open = 0.26f + (hs % 3) * 0.05f;
        if (hs % 5 == 4) { var bl = Drape(g, new Vector3(0, h / 2 - h * 0.2f, -0.05f), Quaternion.identity, w * 0.94f, h * 0.36f, Mathf.Max(4, Mathf.RoundToInt(w * 5)), 0.035f, cu); bl.name = "sk_curtainp"; }   // v0.9.12 gathered half-drawn cafe curtain (was a slab)
        else foreach (int sx in new[] { -1, 1 }) { var c = Drape(g, new Vector3(sx * w * (0.5f - open / 2), -0.01f, -0.055f), Quaternion.identity, w * open, h * 0.9f, Mathf.Max(3, Mathf.RoundToInt(w * open * 9)), 0.04f, cu, (hs % 2 == 0) ? 0.45f : -1, sx); c.name = "sk_curtainp"; }   // v0.9.12 pleated cloth panels (were boxes)
        if (hs % 2 == 0) { float ix = w * 0.22f * ((hs % 4) - 1.5f) / 1.5f; Block(g, new Vector3(ix, -h / 2 + 0.12f, 0.0f), new Vector3(0.16f, 0.2f, 0.12f), Color.Lerp(inside, Color.black, 0.35f), 0.03f, false); Ball(g, new Vector3(ix, -h / 2 + 0.32f, 0.0f), new Vector3(0.26f, 0.22f, 0.1f), Color.Lerp(inside, new Color(0.2f, 0.3f, 0.2f), 0.6f), 8); }   // pot-plant silhouette on the inner sill
        else Block(g, new Vector3(w * 0.25f, -h * 0.05f, 0.015f), new Vector3(w * 0.3f, h * 0.5f, 0.02f), Color.Lerp(inside, Color.black, 0.25f), 0.02f, false);   // bookcase silhouette
        // frame: four bars with depth (not a pillow slab)
        foreach (int sx in new[] { -1, 1 }) Block(g, new Vector3(sx * (w / 2 + fb / 2), 0, -0.15f), new Vector3(fb, h + fb * 2, dep), frame, 0.025f, false);
        if (!arch) Block(g, new Vector3(0, h / 2 + fb / 2, -0.15f), new Vector3(w + fb * 2, fb, dep), frame, 0.025f, false);
        Block(g, new Vector3(0, -h / 2 - fb / 2, -0.15f), new Vector3(w + fb * 2, fb, dep), frame, 0.025f, false);
        // mullions + transom (a grille window uses the grille's own bar grid, so the two never cross)
        if (!grille)
        {
            Block(g, new Vector3(0, 0, -0.11f), new Vector3(0.05f, h, 0.06f), Lt(frame, 0.2f), 0.01f, false);
            Block(g, new Vector3(0, h * 0.18f, -0.11f), new Vector3(w, 0.05f, 0.06f), Lt(frame, 0.2f), 0.01f, false);
        }
        // glass hint: one faint horizontal sky sheen across the upper pane (no diagonal sticks)
        Block(g, new Vector3(0, h * 0.36f, -0.09f), new Vector3(w * 0.96f, h * 0.2f, 0.004f), Color.Lerp(inside, new Color(0.88f, 0.86f, 0.98f), 0.35f), 0.002f, false);
        Drape(g, new Vector3(0, h / 2 - 0.07f, -0.075f), Quaternion.identity, w * 0.96f, 0.13f, Mathf.Max(6, Mathf.RoundToInt(w * 10)), 0.025f, Dk(cu, 0.9f));   // v0.9.12 ruffled cloth valance (was a bar)
        Cyl(g, new Vector3(0, h / 2 - 0.005f, -0.075f), 0.012f, w * 0.98f, Dk(frame, 0.6f), 0.004f, Q(0, 0, 90), 10);   // curtain rod
        if (hs % 3 != 2) { var lp = Art.Prim(PrimitiveType.Sphere, g, new Vector3(w * 0.15f * ((hs % 2) * 2 - 1), h * 0.22f, 0.0f), Vector3.one * Mathf.Min(0.16f, w * 0.08f), WinLampM ?? (WinLampM = Art.Emissive(new Color(1f, 0.8f, 0.55f), 0.9f))); lp.name = "sk_winlamp"; }   // dim pendant inside
        // sill + lintel
        Block(g, new Vector3(0, -h / 2 - fb - 0.04f, -0.16f), new Vector3(w + 0.3f, 0.08f, 0.22f), Lt(frame, 0.15f), 0.02f, false);
        Block(g, new Vector3(0, -h / 2 - fb - 0.11f, -0.2f), new Vector3(w + 0.2f, 0.06f, 0.16f), Dk(frame, 0.85f), 0.01f, false);
        if (arch)
        {   // shallow segmental arch top: recess + glass hint + frame ring, flattened so it never climbs into the floor above
            var a1 = Cyl(g, new Vector3(0, h / 2, 0.03f), w / 2, 0.04f, inside, 0.01f, Q(90, 0, 0), 20); a1.transform.localScale = new Vector3(1, 1, rise / (w / 2));
            var rg = Ring(g, new Vector3(0, h / 2, -0.15f), Quaternion.identity, w / 2 + fb / 2, fb * 0.6f, frame, 24); rg.transform.localScale = new Vector3(1, rise / (w / 2), 1);
            Block(g, new Vector3(0, h / 2, -0.15f), new Vector3(w + fb * 2, 0.05f, dep * 0.8f), frame, 0.01f, false);
        }
        else Block(g, new Vector3(0, h / 2 + fb + 0.07f, -0.14f), new Vector3(w + 0.36f, 0.14f, 0.18f), Dk(frame, 0.92f), 0.03f, false);
        if (flowers.HasValue) { Block(g, new Vector3(0, -h / 2 - 0.36f, -0.36f), new Vector3(w * 0.9f, 0.3f, 0.22f), WoodD, 0.03f, false); for (int i = 0; i < 4; i++) Goods.Shrub(g, new Vector3(-w * 0.45f + 0.2f + i * (w * 0.9f - 0.4f) / 3, -h / 2 - 0.21f, -0.36f), 0.17f, 0.24f, i % 2 == 0 ? Leaf : LeafD, flowers.Value, 41 + i + hs % 7); }
        if (grille) Grille(g, new Vector3(0, 0, -0.08f), w + 0.1f, h + 0.1f, White);
        MatA = om; return g;
    }
    public static Transform Awning(Transform p, Vector3 center, float w, float depth, Color a, Color b)
    {
        var om = PushM(MatK.Fabric); var g = Group(p, "sk_awning", center); int n = Mathf.Max(3, Mathf.RoundToInt(w / 0.9f)); float sw = w / n;
        for (int i = 0; i < n; i++)
        {
            var s = Block(g, new Vector3(-w / 2 + (i + 0.5f) * sw, 0, -depth / 2), new Vector3(sw + 0.01f, 0.2f, depth), i % 2 == 0 ? a : b, 0.09f, false, 0, 0.01f);
            s.transform.localRotation = Q(-22, 0, 0);
            Ball(g, new Vector3(-w / 2 + (i + 0.5f) * sw, -depth * 0.37f - 0.08f, -depth * 0.93f), new Vector3(sw, 0.42f, 0.24f), i % 2 == 0 ? a : b, 12);
            if (GeoQ.Tier > 0) foreach (int k in new[] { -1, 1 }) { var sc = Cyl(g, new Vector3(-w / 2 + (i + 0.5f) * sw + k * sw * 0.25f, -depth * 0.37f - 0.3f, -depth * 0.98f), sw * 0.24f, 0.04f, i % 2 == 0 ? b : a, 0.01f, Q(90, 0, 0), 10); sc.transform.localScale = new Vector3(1, 1, 0.75f); }   // v0.8.1 scallops
        }
        Block(g, new Vector3(0, -depth * 0.37f + 0.12f, -depth * 0.98f), new Vector3(w + 0.04f, 0.05f, 0.06f), Dk(a, 0.8f), 0.01f, false);   // piping
        MatA = om; return g;
    }
    public static Transform Shutter(Transform p, Vector3 center, float w, float h, Color c)
    {
        var g = Group(p, "sk_shutter", center);
        Block(g, Vector3.zero, new Vector3(w, h, 0.2f), c, 0.1f, false);
        int n = 3; for (int i = 0; i < n; i++) Cyl(g, new Vector3(0, -h / 2 + (i + 0.5f) * h / n, -0.06f), 0.09f, w - 0.3f, Dk(c, 0.9f), 0.045f, Q(0, 0, 90), 12);
        Block(g, new Vector3(0, -h / 2 + 0.06f, -0.06f), new Vector3(w, 0.12f, 0.08f), Dk(c, 0.85f), 0.03f, false);
        Cyl(g, new Vector3(0, h / 2 + 0.18f, -0.05f), 0.2f, w + 0.1f, Dk(c, 0.95f), 0.06f, Q(0, 0, 90), 14);
        return g;
    }
    public static Transform ACUnit(Transform p, Vector3 pos)
    {
        var g = Group(p, "sk_ac", pos); Block(g, Vector3.zero, new Vector3(0.9f, 0.6f, 0.4f), White, 0.12f, false);
        Ring(g, new Vector3(0.18f, 0, -0.2f), Quaternion.identity, 0.2f, 0.03f, GreyD, 18); Ball(g, new Vector3(0.18f, 0, -0.19f), new Vector3(0.12f, 0.12f, 0.04f), GreyD, 8); Tag(g, MatK.Metal); return g;
    }
    public static Transform WaterTank(Transform p, Vector3 pos, Color c)
    {
        var g = Group(p, "sk_tank", pos);
        foreach (int sx in new[] { -1, 1 }) foreach (int sz in new[] { -1, 1 }) Cyl(g, new Vector3(sx * 0.45f, 0.35f, sz * 0.45f), 0.05f, 0.7f, GreyD, 0.02f);
        Block(g, new Vector3(0, 0.72f, 0), new Vector3(1.2f, 0.08f, 1.2f), GreyD, 0.03f, false);
        Cyl(g, new Vector3(0, 1.35f, 0), 0.6f, 1.2f, c, 0.25f); Lath(g, new Vector3(0, 1.95f, 0), new[] { new Vector2(0, 0), new Vector2(0.58f, 0), new Vector2(0.35f, 0.15f), new Vector2(0, 0.2f) }, Dk(c, 0.9f));
        return g;
    }
    /// big friendly arched door slab (visual), centred on its bottom at pos, facing -Z
    public static Transform DoorVisual(Transform p, Vector3 pos, float w, float h, Color c, Color frame)
    {
        var g = Group(p, "sk_doorvis", pos); var om = PushM(MatK.Wood); frame = Dk(frame, 0.78f);   // v0.9.5 frame darker than wall so the arch reads
        // v0.9.5: real arched door - slab, frame that follows the arch (no full ring crossing the leaf), 4 raised panels, arched fanlight, hinges, lever handle, kick plate
        float hs = h - w / 2, fz = -0.07f;
        Block(g, new Vector3(0, hs / 2, 0), new Vector3(w, hs, 0.1f), c, 0.02f, false);
        var top = Cyl(g, new Vector3(0, hs, 0), w / 2, 0.1f, c, 0.02f, Q(90, 0, 0), 20); top.transform.localScale = new Vector3(1, 1, 1);
        foreach (int sx in new[] { -1, 1 }) Block(g, new Vector3(sx * (w / 2 + 0.07f), hs / 2, fz), new Vector3(0.14f, hs, 0.14f), frame, 0.02f, false);   // jambs
        int na = GeoQ.Tier == 0 ? 7 : 11;
        for (int k = 0; k < na; k++)
        {   // arch frame as segments over the top half only
            float a0 = Mathf.PI * k / na, a1 = Mathf.PI * (k + 1) / na, am = (a0 + a1) / 2, R = w / 2 + 0.07f;
            var sg = Block(g, new Vector3(Mathf.Cos(am) * R, hs + Mathf.Sin(am) * R, fz), new Vector3(0.14f, 2 * R * Mathf.Sin((a1 - a0) / 2) + 0.03f, 0.14f), frame, 0.01f, false); sg.transform.localRotation = Q(0, 0, am * Mathf.Rad2Deg);
        }
        // arched fanlight glass with a mullion + transom bar
        // v0.9.7 fanlight: true half-round pane (strips, no full disc + covering plank) with a sunburst of glazing bars
        float fr0 = w / 2 - 0.12f; int ns = 9; Color gc = Data.H("6f8796");
        for (int k = 0; k < ns; k++) { float y0 = fr0 * k / ns, y1 = fr0 * (k + 1) / ns, hw = Mathf.Sqrt(Mathf.Max(0, fr0 * fr0 - y0 * y0)); var st = Block(g, new Vector3(0, hs + (y0 + y1) / 2, -0.055f), new Vector3(2 * hw, y1 - y0 + 0.002f, 0.015f), Color.Lerp(gc, new Color(0.86f, 0.88f, 0.96f), 0.25f * k / ns), 0.001f, false); if (k == 0) st.name = "glass_door"; }
        Block(g, new Vector3(0, hs, -0.068f), new Vector3(w - 0.1f, 0.07f, 0.03f), Dk(c, 0.85f), 0.008f, false);   // transom
        Arc(g, new Vector3(0, hs, -0.068f), fr0 * 0.3f, 0, Mathf.PI, 0.015f, Dk(c, 0.85f), 8);
        for (int k = 1; k < 4; k++) { float a = k * Mathf.PI / 4; Block(g, new Vector3(0, hs, -0.068f) + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0) * fr0 * 0.65f, new Vector3(0.03f, fr0 * 0.7f, 0.025f), Dk(c, 0.85f), 0.004f, false).transform.localRotation = Q(0, 0, a * Mathf.Rad2Deg - 90); }
        // four raised panels (rail + stile layout)
        float pw = w * 0.34f, ph1 = hs * 0.38f, ph2 = hs * 0.3f;
        foreach (int sx in new[] { -1, 1 })
        {
            foreach (var (py, ph) in new[] { (hs * 0.25f, ph1), (hs * 0.68f, ph2) })
            {
                Block(g, new Vector3(sx * w * 0.22f, py, -0.055f), new Vector3(pw, ph, 0.02f), Dk(c, 0.9f), 0.004f, false);   // recess shadow
                Block(g, new Vector3(sx * w * 0.22f, py, -0.065f), new Vector3(pw - 0.07f, ph - 0.07f, 0.025f), Lt(c, 0.08f), 0.01f, false);   // raised field
            }
        }
        Block(g, new Vector3(0, 0.1f, -0.06f), new Vector3(w - 0.08f, 0.14f, 0.02f), Dk(c, 0.75f), 0.004f, false);   // kick plate
        int mh0 = g.childCount;
        foreach (float hy in new[] { 0.3f, hs * 0.55f, hs - 0.2f }) Block(g, new Vector3(-w / 2 + 0.03f, hy, -0.065f), new Vector3(0.05f, 0.14f, 0.03f), GreyD, 0.005f, false);   // hinges
        Block(g, new Vector3(w * 0.36f, h * 0.42f, -0.065f), new Vector3(0.07f, 0.26f, 0.025f), ButterD, 0.006f, false);   // back plate
        Block(g, new Vector3(w * 0.32f, h * 0.44f, -0.11f), new Vector3(0.16f, 0.035f, 0.035f), ButterD, 0.008f, false);   // lever
        Block(g, new Vector3(w * 0.36f, h * 0.39f, -0.08f), new Vector3(0.025f, 0.045f, 0.01f), GreyD, 0f, false);   // keyhole
        for (int k = mh0; k < g.childCount; k++) Tag(g.GetChild(k), MatK.Metal);
        MatA = om; return g;
    }

    /// 透天厝 townhouse (solid, not enterable). Front at local z=0 facing -Z, body extends +Z.
    public static Transform TownHouse(Transform p, Vector3 pos, float yaw, float w, float d, int floors, Color wall, Color trim, Color roof, System.Random rng, string shop = null)
    {
        GeoQ.ArchLow = true;
        var g = Group(p, "TownHouse", pos, yaw); float gh = 3.2f, fh = 2.5f, H = gh + (floors - 1) * fh;
        Block(g, new Vector3(0, H / 2, d / 2), new Vector3(w, H, d), wall, Mathf.Min(1.3f, w * 0.17f), true, 0, 0.06f);
        Block(g, new Vector3(0, 0.3f, d / 2), new Vector3(w + 0.3f, 0.6f, d + 0.3f), Dk(wall, 0.88f), 0.3f, false);
        for (int f = 1; f < floors; f++) Block(g, new Vector3(0, gh + (f - 1) * fh, d / 2), new Vector3(w + 0.3f, 0.42f, d + 0.3f), trim, 0.21f, false);
        // inflated storeys: each floor is a puffy marshmallow panel + fat rolled band
        Pillow(g, new Vector3(0, gh / 2 + 0.1f, 0.02f), w - 0.4f, gh - 0.3f, 0.75f, Lt(wall, 0.06f));
        for (int f = 1; f < floors; f++) { Pillow(g, new Vector3(0, gh + (f - 0.5f) * fh, 0.02f), w - 0.5f, fh - 0.35f, 0.75f, Lt(wall, 0.06f)); var om0 = PushM(0.5f); Cyl(g, new Vector3(0, gh + (f - 1) * fh, -0.45f), 0.4f, w + 0.1f, trim, 0.32f, Q(0, 0, 90), 16); MatA = om0; }
        var fr = Group(g, "front", new Vector3(0, 0, -0.75f));
        // roof style
        int rs = rng.Next(3);
        if (rs == 0)
        {   // round hump gable with tile cap
            var cap = Cyl(g, new Vector3(0, H - 0.1f, d / 2), w * 0.5f + 0.1f, d + 0.25f, roof, 0.25f, Q(90, 0, 0), 28); cap.transform.localScale = new Vector3(1, 1, 0.55f);
            var gab = Cyl(g, new Vector3(0, H - 0.1f, 0.0f), w * 0.5f - 0.15f, 0.4f, wall, 0.12f, Q(90, 0, 0), 28); gab.transform.localScale = new Vector3(1, 1, 0.5f);
            RoundWin(g, new Vector3(0, H + w * 0.1f, -0.25f), Mathf.Min(0.55f, w * 0.09f), trim, rng.Next(2) == 0);
        }
        else if (rs == 1)
        {   // parapet + rooftop water tank
            Block(g, new Vector3(0, H + 0.35f, d / 2), new Vector3(w + 0.2f, 0.8f, d + 0.2f), trim, 0.4f, false);
            WaterTank(g, new Vector3(w * 0.22f, H + 0.6f, d * 0.6f), rng.Next(2) == 0 ? Sky : Butter);
            var shed = Cyl(g, new Vector3(-w * 0.18f, H + 0.6f, d * 0.55f), 1.2f, d * 0.5f, Lav, 0.15f, Q(90, 0, 0), 20); shed.transform.localScale = new Vector3(1, 1, 0.8f);
        }
        else
        {   // double wave top
            for (int s = -1; s <= 1; s += 2) { var c = Cyl(g, new Vector3(s * w * 0.25f, H - 0.05f, d / 2), w * 0.26f, d + 0.2f, roof, 0.2f, Q(90, 0, 0), 22); c.transform.localScale = new Vector3(1, 1, 0.6f); }
            Block(g, new Vector3(0, H + 0.12f, d * 0.5f), new Vector3(0.5f, 0.45f, d + 0.1f), trim, 0.15f, false);
        }
        // ground floor
        int gt = shop != null ? 0 : rng.Next(3); Color acc = new[] { Mint, Sky, Butter, Lav, PinkD }[rng.Next(5)];
        if (gt == 0)
        {   // little shop: awning + display + sign
            Block(fr, new Vector3(0, 1.25f, -0.03f), new Vector3(w * 0.66f, 2.1f, 0.14f), Lt(Sky, 0.3f), 0.5f, false);
            for (int i = 0; i < 3; i++) Block(fr, new Vector3(0, 0.6f + i * 0.6f, -0.2f), new Vector3(w * 0.66f, 0.06f, 0.35f), WoodD, 0.03f, false);
            // v0.9.4: goods built after the sign (below) to match the shop type
            var awn = Awning(fr, new Vector3(0, 2.5f, -0.05f), w * 0.85f, 1.1f, acc, White);
            // v0.8.1: sign stands on brackets at the awning's front edge - out in front of balconies/windows, nothing crosses it from the street
            var ab = Art.RenderBounds(awn.gameObject); float atop = fr.InverseTransformPoint(ab.max).y; float sz = 0.14f, hb = sz * 9f;
            string sn = shop ?? new[] { "早餐", "冰店", "書店", "花店" }[rng.Next(4)];
            var st = Text3D(fr, new Vector3(0, atop + 0.12f + hb / 2f, -1.02f), sn, sz, new[] { Rose, Coral, LavD, MintD }[rng.Next(4)]);
            // v0.9.10: icon is painted into the gouache sign board itself
            foreach (float bx in new[] { -0.45f, 0.45f }) Block(fr, new Vector3(bx, atop - 0.03f, -0.72f), new Vector3(0.06f, 0.3f, 0.06f), GreyD, 0.015f, false);   // brackets down to the awning
            { int si = ShopIcon(sn); if (si == 0 || si == 1 || si == 4 || si == 5) { var mb = Group(fr, "sk_menuboard", new Vector3(w * 0.36f, 0, -1.35f), 0); var om2 = PushM(MatK.Wood);   // v0.9.9 sidewalk menu easel with illustrated chalkboard
                foreach (int sx in new[] { -1, 1 }) Block(mb, new Vector3(sx * 0.36f, 0.62f, 0), new Vector3(0.06f, 1.24f, 0.06f), WoodD, 0.012f, false);
                Block(mb, new Vector3(0, 0.82f, 0.03f), new Vector3(0.78f, 1.02f, 0.04f), WoodD, 0.015f, false); MatA = om2;
                Picture(mb, new Vector3(0, 0.82f, 0.005f), Quaternion.identity, 0.74f, 0.98f, "art_menu"); } }
            Goods.ShopWindow(fr, sn, w, Mathf.RoundToInt(fr.position.x * 13 + fr.position.z * 7));
        }
        else if (gt == 1)
        {   // rolling shutter + side door
            Shutter(fr, new Vector3(-w * 0.15f, 1.15f, -0.05f), w * 0.5f, 2.1f, Lt(acc, 0.4f));
            DoorVisual(fr, new Vector3(w * 0.3f, 0, -0.07f), 1.2f, 2.2f, acc, trim);
        }
        else
        {   // arched door + window with grille
            DoorVisual(fr, new Vector3(-w * 0.2f, 0, -0.07f), 1.3f, 2.25f, acc, trim);
            Win(fr, new Vector3(w * 0.2f, 1.55f, -0.05f), 2.0f, 1.8f, trim, true, PinkD, true);
        }
        // upper floors
        for (int f = 1; f < floors; f++)
        {
            float y = gh + (f - 1) * fh + fh * 0.5f;
            bool bal = f == 1 && rng.Next(2) == 0;
            if (bal && gt == 0)
            {   // shops skip the balcony (it boxed the sign) - no extra rng draws so the street layout stays identical to v0.8
                Win(fr, new Vector3(0, y, -0.05f), Mathf.Min(2.8f, w * 0.48f), 1.75f, trim, false);
                Block(fr, new Vector3(0, y - 1.05f, -0.2f), new Vector3(Mathf.Min(2.4f, w * 0.42f), 0.16f, 0.42f), White, 0.07f, false);
            }
            else if (bal)
            {   // balcony with flower grille railing
                Block(fr, new Vector3(0, y - 0.95f, -0.6f), new Vector3(w * 0.72f, 0.36f, 1.2f), trim, 0.18f, false);
                Block(fr, new Vector3(0, y - 0.25f, -1.12f), new Vector3(w * 0.72f, 0.3f, 0.3f), White, 0.15f, false);
                for (int s = -2; s <= 2; s++) Cyl(fr, new Vector3(s * w * 0.17f, y - 0.55f, -1.12f), 0.11f, 0.5f, White, 0.05f, null, 10);
                Win(fr, new Vector3(0, y + 0.15f, -0.05f), w * 0.4f, 1.4f, trim, false);
                FlowerPot(fr, new Vector3(w * 0.28f, y - 0.77f, -0.75f), Coral, PinkD, 1.1f, false);
            }
            else
            {
                if (rng.Next(2) == 0 && f == floors - 1) RoundWin(fr, new Vector3(0, y, -0.05f), Mathf.Min(1.1f, w * 0.2f), trim, Mathf.Abs(Mathf.RoundToInt(pos.x * 0.37f + pos.z)) % 3 == 0);
                else Win(fr, new Vector3(0, y, -0.05f), Mathf.Min(2.2f, w * 0.42f), 1.6f, trim, f % 2 == 0, rng.Next(2) == 0 ? (Color?)new[] { PinkD, ButterD, LavD }[rng.Next(3)] : null);   // v0.9.6: own sill (old extra slab overlapped it)
            }
        }
        Craft(g, fr, pos, w, d, H, floors, gh, fh, wall, trim, acc, gt);
        ArchDetail(g, fr, pos, w, d, H, floors, gh, fh, wall, trim, roof, acc, gt, rs);
        GeoQ.ArchLow = false;
        Merge(fr, 0, "sk_house"); Merge(g, 0, "sk_house");   // v0.8.1: static dressing -> few draws
        return g;
    }

    /// medium/small-scale craft pass for townhouses: corner pilasters, plinth, step, canopy, mailbox, house number,
    /// lamp, AC units, laundry pole, planters, rooftop rail/antenna. Uses its own rng so map layout stays identical.
    static void Craft(Transform g, Transform fr, Vector3 pos, float w, float d, float H, int floors, float gh, float fh, Color wall, Color trim, Color acc, int gt)
    {
        var r = new System.Random(Mathf.RoundToInt(pos.x * 73.1f + pos.z * 131.7f) + floors * 7);
        var om = PushM(1f);
        foreach (int s in new[] { -1, 1 }) Block(fr, new Vector3(s * (w / 2 - 0.12f), H / 2 + 0.3f, 0.45f), new Vector3(0.34f, H - 0.6f, 0.34f), Lt(trim, 0.15f), 0.12f, false);   // accent corner pilasters
        Block(fr, new Vector3(0, 0.09f, -0.35f), new Vector3(w + 0.1f, 0.18f, 0.7f), Dk(wall, 0.8f), 0.08f, false);   // ground plinth / curb contact
        Block(fr, new Vector3(0, 0.015f, -1.0f), new Vector3(w + 0.3f, 0.03f, 0.9f), Dk(wall, 0.62f), 0.01f, false);   // AO contact strip
        float dx = gt == 1 ? w * 0.3f : (gt == 2 ? -w * 0.2f : 0f);
        if (gt != 0)
        {
            Block(fr, new Vector3(dx, 0.12f, -0.5f), new Vector3(2.3f, 0.24f, 0.8f), Lt(wall, 0.25f), 0.08f, false);   // door step
            var om2 = PushM(0.5f); var cn = Block(fr, new Vector3(dx, 3.25f, -0.45f), new Vector3(2.5f, 0.14f, 0.9f), acc, 0.06f, false); cn.transform.localRotation = Q(-12, 0, 0); MatA = om2;   // door canopy
            foreach (int s in new[] { -1, 1 }) Cyl(fr, new Vector3(dx + s * 1.05f, 3.0f, -0.2f), 0.04f, 0.5f, White, 0.02f, Q(40, 0, 0), 8);   // canopy brackets
            // v0.9.7: townhouse (gt 2) mailbox goes on the far side of the door and the number plaque above the arch, so the window grille never covers them
            float mx = gt == 2 ? dx - 1.15f : dx + (dx > 0 ? -1.45f : 1.45f);
            Color mbc = new[] { Coral, MintD, Sky }[r.Next(3)];
            Block(fr, new Vector3(mx, 1.25f, -0.17f), new Vector3(0.42f, 0.52f, 0.24f), mbc, 0.04f, false);   // wall mailbox body
            Block(fr, new Vector3(mx, 1.54f, -0.2f), new Vector3(0.48f, 0.07f, 0.32f), Dk(mbc, 0.85f), 0.02f, false);   // lid
            Block(fr, new Vector3(mx, 1.38f, -0.3f), new Vector3(0.28f, 0.035f, 0.02f), Dk(mbc, 0.45f), 0.005f, false);   // letter slot
            Block(fr, new Vector3(mx, 1.1f, -0.3f), new Vector3(0.16f, 0.1f, 0.02f), White, 0.01f, false);   // name card
            Block(fr, new Vector3(mx + 0.23f, 1.32f, -0.17f), new Vector3(0.03f, 0.24f, 0.05f), Coral, 0.008f, false);   // flag
            float py = gt == 2 ? 2.72f : 2.15f, ppx = gt == 2 ? dx : mx;
            Block(fr, new Vector3(ppx, py, -0.08f), new Vector3(0.56f, 0.32f, 0.04f), Dk(trim, 0.6f), 0.02f, false);   // plaque back
            Block(fr, new Vector3(ppx, py, -0.11f), new Vector3(0.5f, 0.26f, 0.03f), Lt(Butter, 0.2f), 0.03f, false);   // enamel face
            Text(fr, new Vector3(ppx, py, -0.135f), Quaternion.identity, (r.Next(2, 98)).ToString(), 0.035f, Dk(trim, 0.45f));
        }
        // wall lamp next to the entrance
        float lx = gt == 0 ? w * 0.42f : dx + (dx > 0 ? 1.4f : -1.4f);
        Block(fr, new Vector3(lx, 2.6f, -0.1f), new Vector3(0.1f, 0.1f, 0.3f), GreyD, 0.03f, false);
        var om3 = PushM(0.5f); Lath(fr, new Vector3(lx, 2.36f, -0.34f), new[] { new Vector2(0, 0), new Vector2(0.1f, 0), new Vector2(0.13f, 0.26f), new Vector2(0.17f, 0.28f), new Vector2(0.06f, 0.36f), new Vector2(0, 0.37f) }, Lt(ButterD, 0.4f), 12); Cyl(fr, new Vector3(lx, 2.66f, -0.34f), 0.19f, 0.03f, GreyD, 0.01f, null, 12); Cyl(fr, new Vector3(lx, 2.36f, -0.34f), 0.11f, 0.03f, GreyD, 0.01f, null, 10); MatA = om3;   // v0.9.5 lantern shade (was a ball)
        // planters hugging the base
        foreach (int s in new[] { -1, 1 }) if (r.Next(3) > 0) { Block(fr, new Vector3(s * (w / 2 - 0.65f), 0.32f, -0.45f), new Vector3(0.8f, 0.45f, 0.55f), Lt(trim, 0.3f), 0.04f, false); { var lc = new[] { LeafD, Leaf }[r.Next(2)]; float px = s * (w / 2 - 0.65f); Cyl(fr, new Vector3(px, 0.555f, -0.45f), 0.36f, 0.02f, Dk(trim, 0.6f), 0.005f, Q(0, 0, 0), 8); Goods.Shrub(fr, new Vector3(px - 0.17f, 0.56f, -0.45f), 0.24f, 0.4f, lc, null, 11 + s); Goods.Shrub(fr, new Vector3(px + 0.17f, 0.56f, -0.45f), 0.22f, 0.36f, Dk(lc, 0.92f), Rose, 17 + s); } }
        // AC units + laundry pole on upper floors
        int side = r.Next(2) == 0 ? -1 : 1; bool laundry = false;
        for (int f = 1; f < floors; f++)
        {
            float y0 = gh + (f - 1) * fh;
            if (r.Next(3) > 0)
            {
                float ax = side * w * 0.36f;
                Block(fr, new Vector3(ax, y0 + 0.55f, -0.25f), new Vector3(0.8f, 0.55f, 0.45f), new Color(0.95f, 0.94f, 0.92f), 0.14f, false);
                Cyl(fr, new Vector3(ax + 0.08f, y0 + 0.55f, -0.48f), 0.19f, 0.03f, new Color(0.78f, 0.79f, 0.8f), 0.01f, Q(90, 0, 0), 16);
                Block(fr, new Vector3(ax, y0 + 0.24f, -0.25f), new Vector3(0.7f, 0.06f, 0.4f), GreyD, 0.02f, false);
            }
            if (!laundry && f == floors - 1 && r.Next(2) == 0)
            {
                laundry = true; float ly = y0 + fh - 0.25f, lw = w * 0.5f;
                foreach (int s in new[] { -1, 1 }) Cyl(fr, new Vector3(s * lw / 2, ly, -0.35f), 0.035f, 0.6f, GreyD, 0.015f, Q(90, 0, 0), 8);
                Cyl(fr, new Vector3(0, ly, -0.62f), 0.035f, lw, White, 0.015f, Q(0, 0, 90), 8);
                for (int i = 0; i < 3; i++) { float cx = -lw * 0.3f + i * lw * 0.3f; var cl = Block(fr, new Vector3(cx, ly - 0.32f, -0.62f), new Vector3(0.42f, 0.55f, 0.05f), new[] { PinkD, Sky, Butter, Lav }[r.Next(4)], 0.04f, false); cl.transform.localRotation = Q(0, 0, (float)(r.NextDouble() * 8 - 4)); }
            }
        }
        // v0.5: rounded gutter + downpipe on one side, eave rail under the roof line
        { float gx = -side * (w / 2 - 0.42f); Cyl(fr, new Vector3(gx, H / 2, 0.12f), 0.11f, H - 0.2f, Lt(trim, 0.2f), 0.08f, null, 12);
          for (int f = 0; f < floors; f++) Ring(fr, new Vector3(gx, gh * 0.5f + f * fh, 0.12f), Q(90, 0, 0), 0.14f, 0.035f, Dk(trim, 0.85f), 14);
          Ball(fr, new Vector3(gx, 0.2f, 0.0f), new Vector3(0.32f, 0.16f, 0.4f), Lt(trim, 0.2f), 10);
          Cyl(g, new Vector3(0, H - 0.05f, -0.82f), 0.13f, w - 0.3f, Lt(trim, 0.2f), 0.1f, Q(0, 0, 90), 14); }
        // rooftop rim rail + antenna
        if (r.Next(2) == 0)
        {
            Cyl(g, new Vector3(w * 0.3f, H + 0.9f, d * 0.7f), 0.03f, 1.8f, GreyD, 0.01f, null, 6);
            Cyl(g, new Vector3(w * 0.3f, H + 1.5f, d * 0.7f), 0.02f, 0.9f, GreyD, 0.01f, Q(0, 0, 90), 6);
        }
        MatA = om;
    }



    /// v0.8.1: fold freshly-built detail children (index >= from) into one mesh per material, so hundreds of tiles/slats cost one draw
    public static void Merge(Transform root, int from, string name)
    {
        var groups = new Dictionary<Material, List<CombineInstance>>(); var kill = new List<GameObject>();
        var inv = root.worldToLocalMatrix;
        for (int i = from; i < root.childCount; i++)
        {
            var c = root.GetChild(i);
            if (c.name == "front" || c.name == "sk_doorvis" || c.name == "sk_text3d" || c.GetComponentInChildren<TextMesh>() || c.GetComponentInChildren<Collider>()) continue;
            foreach (var mf in c.GetComponentsInChildren<MeshFilter>())
            {
                var mr = mf.GetComponent<MeshRenderer>(); if (!mr || !mf.sharedMesh) continue;
                if (!groups.TryGetValue(mr.sharedMaterial, out var l)) groups[mr.sharedMaterial] = l = new List<CombineInstance>();
                l.Add(new CombineInstance { mesh = mf.sharedMesh, transform = inv * mf.transform.localToWorldMatrix });
            }
            PlaceAudit.Record(root, c); kill.Add(c.gameObject);
        }
        foreach (var kv in groups)
        {
            var m = new Mesh { name = name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 }; m.CombineMeshes(kv.Value.ToArray(), true, true); m.RecalculateBounds();
            var go = new GameObject(name); go.transform.SetParent(root, false); go.AddComponent<MeshFilter>().sharedMesh = m; go.AddComponent<MeshRenderer>().sharedMaterial = kv.Key;
        }
        foreach (var k in kill) { k.transform.SetParent(null); Object.Destroy(k); }
    }
    /// v0.8.1 architectural detail pass: crisp, low-radius pieces where the eye reads "real building":
    /// louvred shutters + window reveals, front tiled pent roof with gutter/brackets, corner quoins, shop counter,
    /// blade-sign bracket, chimney, roof-coping tiles, scalloped awning valance. Own rng -> layout unchanged.
    static void ArchDetail(Transform g, Transform fr, Vector3 pos, float w, float d, float H, int floors, float gh, float fh, Color wall, Color trim, Color roof, Color acc, int gt, int rs)
    {
        var r = new System.Random(Mathf.RoundToInt(pos.x * 51.3f + pos.z * 97.1f) + floors * 13);
        int T = GeoQ.Tier; var om = PushM(1f);
        int n0f = fr.childCount, n0g = g.childCount;
        Color shut = new[] { Data.H("8fae9a"), Data.H("7fa3b8"), Data.H("c99a6a"), Data.H("a7b98a") }[r.Next(4)];
        var doorRects = new List<Vector3>();   // x centre, top y, half width (fr-local)
        foreach (Transform dv in fr) if (dv.name == "sk_doorvis") { var db = Art.RenderBounds(dv.gameObject); var c0 = fr.InverseTransformPoint(db.min); var c1 = fr.InverseTransformPoint(db.max); doorRects.Add(new Vector3((c0.x + c1.x) / 2, Mathf.Max(c0.y, c1.y), Mathf.Abs(c1.x - c0.x) / 2)); }
        // 1) windows: reveals (depth) + louvred shutters + hinges
        foreach (Transform wn in fr) if (wn.name == "sk_win")
        {
            if (!WinDim.TryGetValue(wn, out var wd)) continue; var lc = wn.localPosition; float ww = wd.x + 0.2f, wh = wd.y; if (ww < 0.6f || wh < 0.6f) continue;   // v0.9.6: true opening size; Win() has its own frame/reveal now
            if (T == 0 && r.Next(2) == 0) continue;
            float sw = Mathf.Min(0.75f, ww * 0.42f);
            foreach (int s in new[] { -1, 1 })
            {
                float sx = lc.x + s * (ww / 2 + 0.03f + sw / 2), sy = lc.y, sh = wh + 0.2f;   // hugs the frame, hinged on its inner edge
                bool hit = false;   // v0.9.5: no shutter where it would overlap a door / its frame / the building edge
                foreach (var dr in doorRects) if (Mathf.Abs(sx - dr.x) < sw / 2 + dr.z + 0.08f && sy - sh / 2 < dr.y) hit = true;
                if (Mathf.Abs(sx) + sw / 2 > w / 2 - 0.4f) hit = true;
                if (hit) continue;
                Block(fr, new Vector3(sx, sy, -0.12f), new Vector3(sw, sh, 0.06f), Dk(shut, 0.9f), 0.015f, false);
                foreach (int t in new[] { -1, 1 }) Block(fr, new Vector3(sx + t * (sw / 2 - 0.05f), sy, -0.17f), new Vector3(0.09f, sh, 0.07f), shut, 0.015f, false);   // stiles
                int ns = T == 2 ? 9 : 6;
                for (int i = 0; i < ns; i++) { var sl = Block(fr, new Vector3(sx, sy - sh / 2 + 0.12f + i * (sh - 0.24f) / (ns - 1), -0.17f), new Vector3(sw - 0.14f, 0.05f, 0.09f), Lt(shut, 0.08f), 0.01f, false); sl.transform.localRotation = Q(28, 0, 0); }
                Block(fr, new Vector3(sx, sy, -0.18f), new Vector3(sw - 0.1f, 0.08f, 0.08f), shut, 0.01f, false);   // mid rail
                foreach (int t in new[] { -1, 1 }) Block(fr, new Vector3(sx - s * (sw / 2 + 0.02f), sy + t * sh * 0.32f, -0.15f), new Vector3(0.05f, 0.12f, 0.06f), GreyD, 0.01f, false);   // hinges
            }
        }
        // 2) corner quoins on the pilasters
        int nq = Mathf.FloorToInt((H - 0.6f) / 0.5f);
        foreach (int s in new[] { -1, 1 }) for (int i = 0; i < nq; i++)
        { bool wide = i % 2 == 0; Block(fr, new Vector3(s * (w / 2 - (wide ? 0.2f : 0.14f)), 0.6f + i * 0.5f + 0.24f, 0.2f), new Vector3(wide ? 0.5f : 0.36f, 0.42f, 0.12f), Lt(trim, wide ? 0.25f : 0.1f), 0.02f, false); }
        // 3) tiled pent roof across the front between ground and first floor (Taiwanese street eave)
        if (floors > 1 && gt != 0)   // shops keep their awning as the eave: no pent roof crowding the sign
        {
            float ey = gh - 0.15f, ed = 0.85f, ew = w - 0.2f; Color tc = Dk(roof, 0.95f);
            var eb = Block(fr, new Vector3(0, ey, -ed / 2), new Vector3(ew, 0.08f, ed), Dk(roof, 0.75f), 0.01f, false); eb.transform.localRotation = Q(-18, 0, 0);
            int rows = T == 0 ? 2 : 4; float tw = T == 2 ? 0.3f : 0.42f; int nt = Mathf.Max(4, Mathf.RoundToInt(ew / tw)); tw = ew / nt;
            for (int rr = 0; rr < rows; rr++)
            {
                float zz = -0.12f - rr * (ed - 0.15f) / rows, yy = ey + 0.07f - Mathf.Tan(18f * Mathf.Deg2Rad) * (-zz - ed / 2) - ed / 2 * 0f;
                yy = ey + 0.06f + Mathf.Tan(18f * Mathf.Deg2Rad) * (zz + ed / 2);
                for (int i = 0; i < nt; i++)
                {
                    float xx = -ew / 2 + (i + 0.5f + (rr % 2) * 0.5f) * tw; if (xx > ew / 2 - tw * 0.3f) continue;
                    var tl = Block(fr, new Vector3(xx, yy, zz), new Vector3(tw * 0.94f, 0.07f, (ed - 0.1f) / rows + 0.08f), (i + rr) % 5 == 0 ? Lt(tc, 0.12f) : tc, 0.025f, false); tl.transform.localRotation = Q(-18, 0, 0);
                }
            }
            float gyy = ey - Mathf.Tan(18f * Mathf.Deg2Rad) * ed / 2 - 0.02f;
            Block(fr, new Vector3(0, gyy, -ed - 0.02f), new Vector3(ew + 0.1f, 0.14f, 0.14f), Lt(trim, 0.15f), 0.03f, false);   // gutter
            Block(fr, new Vector3(0, gyy + 0.08f, -ed - 0.08f), new Vector3(ew + 0.1f, 0.03f, 0.04f), Dk(trim, 0.85f), 0.01f, false);   // gutter lip
            float px = (r.Next(2) == 0 ? -1 : 1) * (ew / 2 - 0.05f);
            Block(fr, new Vector3(px, gyy / 2, -ed - 0.02f), new Vector3(0.1f, gyy, 0.1f), Lt(trim, 0.15f), 0.03f, false);   // downpipe
            for (int i = 1; i <= 2; i++) Block(fr, new Vector3(px, gyy * i / 3f, -ed / 2), new Vector3(0.04f, 0.04f, ed), GreyD, 0.01f, false);   // pipe stays
            int nb = Mathf.Max(2, Mathf.RoundToInt(ew / 1.6f));
            for (int i = 0; i <= nb; i++)
            {   // v0.9.7 knee brackets: wall post + arm under the eave + diagonal brace (was a lone floating diagonal peg)
                float bx = -ew / 2 + 0.15f + i * (ew - 0.3f) / nb; Color bc = Dk(trim, 0.7f);
                Block(fr, new Vector3(bx, ey - 0.3f, -0.04f), new Vector3(0.09f, 0.5f, 0.08f), bc, 0.01f, false);
                Block(fr, new Vector3(bx, ey - 0.08f, -0.24f), new Vector3(0.08f, 0.07f, 0.44f), bc, 0.01f, false);
                var br = Block(fr, new Vector3(bx, ey - 0.3f, -0.22f), new Vector3(0.06f, 0.5f, 0.06f), bc, 0.01f, false); br.transform.localRotation = Q(-45, 0, 0);
            }
        }
        // 4) shops: counter with panels + blade sign on an iron bracket
        if (gt == 0)
        {
            float cw = w * 0.6f;
            Block(fr, new Vector3(0, 0.5f, -0.75f), new Vector3(cw, 1.0f, 0.5f), Lt(acc, 0.2f), 0.03f, false);
            Block(fr, new Vector3(0, 1.04f, -0.75f), new Vector3(cw + 0.14f, 0.08f, 0.62f), WoodD, 0.02f, false);
            int np = Mathf.Max(2, Mathf.RoundToInt(cw / 0.8f));
            for (int i = 0; i < np; i++) Block(fr, new Vector3(-cw / 2 + (i + 0.5f) * cw / np, 0.5f, -1.01f), new Vector3(cw / np - 0.14f, 0.7f, 0.03f), Dk(acc, 0.92f), 0.01f, false);
            float bx = w / 2 - 0.5f, by = gh + 0.9f;
            Block(fr, new Vector3(bx, by + 0.55f, -0.55f), new Vector3(0.05f, 0.05f, 1.1f), GreyD, 0.01f, false);
            var dg = Block(fr, new Vector3(bx, by + 0.3f, -0.35f), new Vector3(0.04f, 0.04f, 0.7f), GreyD, 0.01f, false); dg.transform.localRotation = Q(-40, 0, 0);
            Ring(fr, new Vector3(bx, by + 0.42f, -0.25f), Q(0, 90, 0), 0.12f, 0.02f, GreyD, 10);
            Block(fr, new Vector3(bx, by - 0.1f, -0.85f), new Vector3(0.08f, 1.0f, 0.62f), Lt(acc, 0.3f), 0.04f, false);
            Block(fr, new Vector3(bx, by - 0.1f, -0.85f), new Vector3(0.1f, 0.84f, 0.46f), White, 0.03f, false);
            foreach (int s in new[] { -1, 1 }) Block(fr, new Vector3(bx, by + 0.45f, -0.85f + s * 0.22f), new Vector3(0.02f, 0.12f, 0.02f), GreyD, 0f, false);
        }
        // 5) chimney on the hump roof, tile coping on parapet roofs
        if (rs == 0 && T > 0)
        {
            float cx = w * 0.3f * (r.Next(2) == 0 ? -1 : 1), cz = d * 0.6f;
            for (int i = 0; i < 6; i++) Block(g, new Vector3(cx, H + 0.2f + i * 0.22f, cz), new Vector3(i % 2 == 0 ? 0.62f : 0.58f, 0.2f, i % 2 == 0 ? 0.62f : 0.58f), i % 2 == 0 ? Lt(trim, 0.1f) : Dk(wall, 0.85f), 0.02f, false);
            Block(g, new Vector3(cx, H + 1.55f, cz), new Vector3(0.8f, 0.1f, 0.8f), Dk(roof, 0.8f), 0.02f, false);
            Block(g, new Vector3(cx, H + 1.75f, cz), new Vector3(0.5f, 0.06f, 0.5f), Dk(roof, 0.8f), 0.02f, false);
        }
        else if (rs == 1)
        {
            int nc = Mathf.RoundToInt((w + 0.2f) / (T == 2 ? 0.35f : 0.5f));
            for (int i = 0; i < nc; i++) Block(g, new Vector3(-(w + 0.2f) / 2 + (i + 0.5f) * (w + 0.2f) / nc, H + 0.8f, -0.05f), new Vector3((w + 0.2f) / nc * 0.92f, 0.12f, 0.38f), i % 2 == 0 ? roof : Dk(roof, 0.9f), 0.02f, false);
        }
        // 6) v0.8.1b: stepped cornice + dentil course under the roof line, string course with dentils between storeys, sills on corbels + keystones on windows
        {
            float cy = H - 0.42f;
            for (int k = 0; k < 3; k++) Block(fr, new Vector3(0, cy + k * 0.11f, -0.12f - k * 0.06f), new Vector3(w - 0.5f + k * 0.12f, 0.1f, 0.2f + k * 0.12f), k == 1 ? Dk(trim, 0.9f) : Lt(trim, 0.2f), 0.02f, false);
            if (T > 0) { int nd = Mathf.RoundToInt((w - 0.7f) / 0.28f); for (int i = 0; i < nd; i++) Block(fr, new Vector3(-(w - 0.7f) / 2 + (i + 0.5f) * (w - 0.7f) / nd, cy - 0.11f, -0.1f), new Vector3(0.12f, 0.12f, 0.14f), Lt(trim, 0.3f), 0.01f, false); }
            for (int f = 2; f < floors; f++)
            {
                float sy = gh + (f - 1) * fh + 0.05f;
                Block(fr, new Vector3(0, sy, -0.1f), new Vector3(w - 0.6f, 0.08f, 0.16f), Dk(trim, 0.92f), 0.01f, false);
            }
            foreach (Transform wn in fr) if (wn.name == "sk_win")
            {
                var b = Art.RenderBounds(wn.gameObject); var lc = fr.InverseTransformPoint(b.center); float ww = Mathf.Min(b.size.x, 3.2f) - 0.5f, wh = b.size.y - 0.75f; if (ww < 0.6f || wh < 0.6f) continue;
                if (lc.y > gh) foreach (int s in new[] { -1, 1 }) { var co = Block(fr, new Vector3(lc.x + s * (ww / 2 - 0.15f), lc.y - wh / 2 - 0.32f, -0.12f), new Vector3(0.12f, 0.22f, 0.18f), Lt(trim, 0.25f), 0.02f, false); }   // corbels
                Block(fr, new Vector3(lc.x, lc.y + wh / 2 + 0.1f, -0.12f), new Vector3(0.22f, 0.26f, 0.12f), Lt(trim, 0.35f), 0.02f, false);   // keystone
            }
        }
        MatA = om;
    }
    /// chunky extruded 3D letters: stacked darker layers behind a bright face, on a puffy pill
    public static float Lum(Color c) { float f(float v) => v <= 0.03928f ? v / 12.92f : Mathf.Pow((v + 0.055f) / 1.055f, 2.4f); return 0.2126f * f(c.r) + 0.7152f * f(c.g) + 0.0722f * f(c.b); }
    public static float Contrast(Color a, Color b) { float x = Lum(a), y = Lum(b); return (Mathf.Max(x, y) + 0.05f) / (Mathf.Min(x, y) + 0.05f); }
    /// keep hue, push lightness until text vs board reaches ratio (dark text on light board, cream on dark board)
    public static Color Readable(Color text, Color board, float ratio = 4.5f)
    {
        bool lightBoard = Lum(board) > 0.18f; var c = text;
        for (int i = 0; i < 40 && Contrast(c, board) < ratio; i++) c = lightBoard ? Dk(c, 0.92f) : Color.Lerp(c, new Color(1f, 0.98f, 0.93f), 0.15f);
        if (Contrast(c, board) < ratio) c = lightBoard ? new Color(0.25f, 0.16f, 0.2f) : new Color(1f, 0.98f, 0.93f);
        return c;
    }
    public static int SignCount, SignFails;
    public static readonly string[] SignNames = { "早餐","冰店","書店","花店","早餐店","書局","麵包","豆花","文具","理髮","阿姨家","柑仔店" };
    /// chunky extruded 3D letters on a puffy pill board. Board height fixed by `size`; glyphs fill ~68% of it,
    /// names longer than 4 chars wrap to two lines; text colour forced to >= 4.5:1 contrast with the board.
    public static Transform Text3D(Transform p, Vector3 pos, string s, float size, Color c)
    {
        var g = Group(p, "sk_text3d", pos);
        int sgi = System.Array.IndexOf(SignNames, s);
        if (sgi >= 0)
        {   // v0.9.10 B: one gouache sign system - painted arched board w/ icon + real-font CJK lettering
            var omp = PushM(MatK.Wood); float Hp = size * 9f * 1.25f, Wp = Hp * (300f + s.Length * 210f) / 300f;
            Block(g, new Vector3(0, -Hp * 0.06f, 0.3f), new Vector3(Wp * 0.97f, Hp * 0.78f, 0.22f), H("7a5236"), 0.06f, false);
            Picture(g, new Vector3(0, 0, 0.185f), Quaternion.identity, Wp, Hp, "art_sg" + sgi); MatA = omp; SignCount++;
            var ht = Text(g, new Vector3(0, 0, 0.3f), Quaternion.identity, s, size / 8f, c); ht.GetComponent<MeshRenderer>().enabled = false;   // keeps name for tests
            return g;
        }
        var om = PushM(0.5f);
        Color board = new Color(1f, 0.98f, 0.94f);
        int n = s.Length, lines = n > 4 ? 2 : 1; int per = (n + lines - 1) / lines;
        string txt = lines == 2 ? s.Substring(0, per) + "\n" + s.Substring(per) : s;
        float Hb = size * 9f * (lines == 2 ? 1.35f : 1f);
        float em = Hb * 0.56f / 0.92f / lines / (lines == 2 ? 0.95f : 1f);   // CJK ink ~0.92em
        float wd = per * em + Hb * 0.5f;
        Block(g, new Vector3(0, 0, 0.32f), new Vector3(wd, Hb, 0.3f), board, Mathf.Min(Hb * 0.47f, 0.14f), false);
        var face = Readable(c, board, 7f);
        Block(g, new Vector3(0, 0, 0.4f), new Vector3(wd + 0.16f, Hb + 0.16f, 0.26f), Lt(face, 0.15f), Mathf.Min(Hb * 0.47f, 0.14f) + 0.06f, false);   // coloured rim frames the plaque
        MatA = om; float cr = Contrast(face, board); SignCount++;
        if (cr < 4.5f) { SignFails++; Debug.Log("[contrast] FAIL sign '" + s + "' ratio " + cr.ToString("0.00")); }
        float cs = em / 8f;
        for (int k = 2; k >= 0; k--) { var t = Text(g, new Vector3(0, 0, 0.12f - k * 0.05f), Quaternion.identity, txt, cs, k == 0 ? face : (k == 1 ? Lt(face, 0.35f) : Dk(face, 0.7f))); t.lineSpacing = 0.95f; t.GetComponent<MeshRenderer>().sortingOrder = 20 - k * 2; }   // v0.8.1: 3 layers, 5 cm apart (was 5 @ 3 cm: sort/z shimmer)
        return g;
    }

    // ---------------- furniture (front faces +Z)
    public static Transform Sofa(Transform p, Vector3 pos, float yaw, Color c, Color cushion)
    {
        var g = Group(p, "sk_sofa", pos, yaw);
        Block(g, new Vector3(0, 0.25f, 0), new Vector3(2.2f, 0.42f, 0.95f), Dk(c, 0.92f), 0.18f, false);
        Block(g, new Vector3(0, 0.66f, -0.35f), new Vector3(2.2f, 0.62f, 0.32f), c, 0.16f, false);
        foreach (int s in new[] { -1, 1 }) Block(g, new Vector3(s * 1.0f, 0.55f, 0), new Vector3(0.32f, 0.5f, 0.95f), c, 0.15f, false);
        foreach (int s in new[] { -1, 1 }) Block(g, new Vector3(s * 0.43f, 0.53f, 0.08f), new Vector3(0.82f, 0.2f, 0.72f), Lt(c, 0.2f), 0.09f, false);
        Block(g, new Vector3(-0.55f, 0.72f, -0.12f), new Vector3(0.5f, 0.42f, 0.18f), cushion, 0.12f, false).transform.localRotation = Q(-10, 8, 6);
        Block(g, new Vector3(0.55f, 0.72f, -0.12f), new Vector3(0.48f, 0.42f, 0.18f), Butter, 0.12f, false).transform.localRotation = Q(-10, -8, -5);
        var bc = g.gameObject.AddComponent<BoxCollider>(); bc.size = new Vector3(2.2f, 0.97f, 0.95f); bc.center = new Vector3(0, 0.485f, 0); return g;
    }
    public static Transform RoundTable(Transform p, Vector3 pos, float R, float h, Color top, Color leg, bool col = true)
    {
        var g = Group(p, "sk_rtable", pos);
        Cyl(g, new Vector3(0, h - 0.06f, 0), R, 0.12f, top, 0.05f, null, 24);
        if (R < 0.6f) Cyl(g, new Vector3(0, (h - 0.1f) / 2, 0), 0.08f, h - 0.1f, leg); else foreach (int i in new[] { 0, 1, 2, 3 }) { float a = i * Mathf.PI / 2 + 0.78f; Cyl(g, new Vector3(Mathf.Cos(a) * R * 0.6f, (h - 0.1f) / 2, Mathf.Sin(a) * R * 0.6f), 0.07f, h - 0.1f, leg, 0.03f); }
        if (col) { var bc = g.gameObject.AddComponent<BoxCollider>(); bc.size = new Vector3(R * 1.8f, h, R * 1.8f); bc.center = new Vector3(0, h / 2, 0); }
        return g;
    }
    public static Transform Stool(Transform p, Vector3 pos, Color c)
    {
        var g = Group(p, "sk_stool", pos); Cyl(g, new Vector3(0, 0.45f, 0), 0.22f, 0.12f, c, 0.05f); foreach (int i in new[] { 0, 1, 2 }) { float a = i * 2.09f; Cyl(g, new Vector3(Mathf.Cos(a) * 0.13f, 0.2f, Mathf.Sin(a) * 0.13f), 0.035f, 0.4f, WoodD, 0.015f); }
        return g;
    }
    /// v0.9.7 woven rug: rectangular, border bands + diamond medallion field + fringe tassels (was a flat striped disc)
    public static Transform Rug(Transform p, Vector3 pos, float R, Color a, Color b)
    {
        var g = Group(p, "sk_rug", pos); float sx = R * 2f, sz = R * 1.4f; var om = PushM(MatK.Fabric);
        Color c3 = Color.Lerp(a, b, 0.5f), dk = Dk(a, 0.75f);
        System.Func<float, float, Color> pat = (x, z) =>
        {
            float ex = sx / 2 - Mathf.Abs(x), ez = sz / 2 - Mathf.Abs(z), e = Mathf.Min(ex, ez);
            if (e < 0.08f) return dk; if (e < 0.14f) return b; if (e < 0.2f) return a;
            if (e < 0.26f) return ((Mathf.FloorToInt((x + z) / 0.08f) & 1) == 0) ? b : c3;   // braided band
            float dmd = Mathf.Abs(x) / (sx * 0.32f) + Mathf.Abs(z) / (sz * 0.32f);
            if (dmd < 0.3f) return dk; if (dmd < 0.55f) return b; if (dmd < 0.62f) return a; if (dmd < 1f) return c3;
            float u = Mathf.Repeat(x / 0.3f, 1f) - 0.5f, v = Mathf.Repeat(z / 0.3f, 1f) - 0.5f;
            return Mathf.Abs(u) + Mathf.Abs(v) < 0.18f ? a : b;   // small diamond repeat in the corners
        };
        Field(g, new Vector3(0, 0.012f, 0), sx, sz, 0.04f, pat, false, "sk_rugtop", 0.004f);
        Block(g, new Vector3(0, 0.003f, 0), new Vector3(sx + 0.02f, 0.006f, sz + 0.02f), dk, 0.002f, false);   // v0.9.8: was 0.012 thick and poked through the field (the "pink diamond")   // backing / edge binding
        foreach (int s2 in new[] { -1, 1 }) for (int i = 0; i < Mathf.RoundToInt(sz / 0.07f); i++) Block(g, new Vector3(s2 * (sx / 2 + 0.05f), 0.006f, -sz / 2 + 0.04f + i * 0.07f), new Vector3(0.09f, 0.01f, 0.025f), Lt(b, 0.3f), 0.003f, false);   // fringe
        MatA = om; return g;
    }
    public static Transform TVStand(Transform p, Vector3 pos, float yaw)
    {
        var g = Group(p, "sk_tv", pos, yaw); var om = PushM(0.5f);
        Block(g, new Vector3(0, 0.73f, -0.05f), new Vector3(1.25f, 0.92f, 0.5f), H("b9784a"), 0.34f, false);                    // chunky rounded bezel body
        Block(g, new Vector3(0, 0.75f, 0.2f), new Vector3(0.95f, 0.64f, 0.1f), Dk(SkyD, 0.75f), 0.16f, false);           // screen
        Block(g, new Vector3(0, 0.75f, 0.17f), new Vector3(1.08f, 0.76f, 0.08f), H("f1e2c6"), 0.12f, false);   // v0.9.11 cream screen surround
        var dl = Cyl(g, new Vector3(0, 1.205f, -0.05f), 0.36f, 0.006f, White, 0.002f, Quaternion.identity, 20);    // crocheted lace doily on top
        for (int k = 0; k < 16; k++) { float a = k * Mathf.PI / 8; Ball(g, new Vector3(Mathf.Cos(a) * 0.36f, 1.203f, -0.05f + Mathf.Sin(a) * 0.36f), new Vector3(0.07f, 0.012f, 0.07f), White, 6); }
        for (int k = 0; k < 3; k++) Block(g, new Vector3(0.55f, 0.75f - k * 0.1f, 0.21f), new Vector3(0.006f, 0.05f, 0.004f), H("7a4a2c"), 0.002f, false);
        // v0.9.8: retro CRT details — reflection strip, speaker slots + dials on a control strip, antenna base with telescoping rabbit ears, tapered legs (were balls)
        Picture(g, new Vector3(0, 0.75f, 0.2525f), Q(0, 180, 0), 0.84f, 0.54f, "art_tv");   // v0.9.9 cartoon on the screen (was a blank dark slab)
        var gl = Block(g, new Vector3(-0.22f, 0.9f, 0.255f), new Vector3(0.3f, 0.035f, 0.005f), Lt(Sky, 0.35f), 0.01f, false); gl.transform.localRotation = Q(0, 0, -18);
        Block(g, new Vector3(0, 0.37f, 0.2f), new Vector3(1.0f, 0.13f, 0.02f), Dk(H("b9784a"), 0.88f), 0.03f, false);   // control strip
        for (int i = 0; i < 6; i++) Block(g, new Vector3(-0.42f + i * 0.05f, 0.37f, 0.212f), new Vector3(0.018f, 0.08f, 0.006f), Dk(H("7a4a2c"), 0.7f), 0.004f, false);   // speaker slots
        foreach (var (x, c) in new[] { (0.28f, Coral), (0.42f, Mint) }) { Cyl(g, new Vector3(x, 0.37f, 0.225f), 0.045f, 0.035f, c, 0.01f, Q(90, 0, 0), 14); Block(g, new Vector3(x, 0.395f, 0.244f), new Vector3(0.008f, 0.03f, 0.004f), White, 0.002f, false); }   // dials with pointer
        Block(g, new Vector3(0, 1.215f, -0.08f), new Vector3(0.26f, 0.05f, 0.14f), Dk(H("7a4a2c"), 0.85f), 0.012f, false);   // v0.9.9 flat antenna base block (was a dome)
        Block(g, new Vector3(0, 1.245f, -0.08f), new Vector3(0.12f, 0.025f, 0.08f), GreyD, 0.006f, false);   // swivel plate
        foreach (int sd in new[] { -1, 1 })
        {
            var dir = Q(0, 0, -sd * 32) * Vector3.up; var b0 = new Vector3(sd * 0.03f, 1.26f, -0.08f); float[] rad = { 0.014f, 0.01f, 0.007f }; float[] len = { 0.12f, 0.11f, 0.09f };   // collapsed telescopes: keeps tv-on-stand under the 1.6 m scale rule
            for (int k = 0; k < 3; k++) { var b1 = b0 + dir * len[k]; Cyl(g, (b0 + b1) / 2, rad[k], len[k], Lt(GreyD, 0.55f), 0.002f, Quaternion.FromToRotation(Vector3.up, dir), 8); if (k < 2) Cyl(g, b1, rad[k] * 1.4f, 0.015f, GreyD, 0.002f, Quaternion.FromToRotation(Vector3.up, dir), 8); b0 = b1; }
            Cyl(g, b0 + dir * 0.025f, 0.0035f, 0.05f, Lt(GreyD, 0.7f), 0.0005f, Quaternion.FromToRotation(Vector3.up, dir), 6);   // v0.9.9 thin whip tip, no knob
        }
        foreach (int sd in new[] { -1, 1 }) foreach (float z in new[] { -0.22f, 0.12f }) { var lg = Cyl(g, new Vector3(sd * 0.48f, 0.14f, z), 0.025f, 0.28f, WoodD, 0.01f, Q(z < 0 ? -8 : 8, 0, sd * 8), 8); }   // splayed legs
        MatA = om;
        return g;
    }
    /// v0.9.7 bed: frame on legs, slatted headboard between posts, mattress with piping, fitted sheet, pillow with seam,
    /// quilt with stitched patchwork + turned-down band and a folded throw at the foot (no flat-disc "stars")
    public static Transform Bed(Transform p, Vector3 pos, float yaw, float w, Color frame, Color quilt, Color star, bool stars = true)
    {
        var g = Group(p, "sk_bed", pos, yaw); float L = 2.1f; var om = PushM(MatK.Wood);
        foreach (int sx in new[] { -1, 1 }) foreach (int sz in new[] { -1, 1 }) Block(g, new Vector3(sx * (w / 2 - 0.05f), 0.09f, sz * (L / 2 - 0.05f)), new Vector3(0.1f, 0.18f, 0.1f), Dk(frame, 0.85f), 0.02f, false);   // legs
        foreach (int sx in new[] { -1, 1 }) Block(g, new Vector3(sx * (w / 2 - 0.03f), 0.27f, 0), new Vector3(0.06f, 0.18f, L - 0.1f), frame, 0.02f, false);   // side rails
        Block(g, new Vector3(0, 0.22f, 0), new Vector3(w - 0.1f, 0.06f, L - 0.1f), Dk(frame, 0.7f), 0.01f, false);   // slat deck
        float hy = 1.0f;   // headboard: two turned posts + top rail + slats
        foreach (int sx in new[] { -1, 1 }) { Block(g, new Vector3(sx * (w / 2 - 0.02f), hy / 2, -L / 2 - 0.02f), new Vector3(0.1f, hy, 0.1f), frame, 0.025f, false); Lath(g, new Vector3(sx * (w / 2 - 0.02f), hy, -L / 2 - 0.02f), new[] { new Vector2(0, 0), new Vector2(0.06f, 0), new Vector2(0.065f, 0.04f), new Vector2(0.03f, 0.1f), new Vector2(0, 0.11f) }, Lt(frame, 0.1f), 10); }
        Block(g, new Vector3(0, hy - 0.06f, -L / 2 - 0.02f), new Vector3(w - 0.06f, 0.1f, 0.07f), frame, 0.02f, false);
        Block(g, new Vector3(0, 0.44f, -L / 2 - 0.02f), new Vector3(w - 0.06f, 0.08f, 0.06f), frame, 0.02f, false);
        int nsl = Mathf.Max(3, Mathf.RoundToInt(w / 0.2f)); for (int i = 0; i < nsl; i++) Block(g, new Vector3(-w / 2 + 0.12f + i * (w - 0.24f) / (nsl - 1), 0.67f, -L / 2 - 0.02f), new Vector3(0.05f, 0.42f, 0.035f), Lt(frame, 0.08f), 0.01f, false);
        Block(g, new Vector3(0, 0.36f, L / 2 - 0.02f), new Vector3(w, 0.32f, 0.06f), frame, 0.02f, false);   // low footboard
        MatA = MatK.Fabric;
        Block(g, new Vector3(0, 0.37f, 0), new Vector3(w - 0.12f, 0.24f, L - 0.14f), White, 0.06f, false);   // mattress
        foreach (float yy in new[] { 0.255f, 0.485f }) foreach (int sx in new[] { -1, 1 }) Block(g, new Vector3(sx * (w / 2 - 0.06f), yy, 0), new Vector3(0.02f, 0.02f, L - 0.18f), Lt(star, 0.3f), 0.005f, false);   // piping
        Block(g, new Vector3(0, 0.5f, -L / 2 + 0.3f), new Vector3(w - 0.14f, 0.03f, 0.5f), Lt(Sky, 0.6f), 0.01f, false);   // fitted sheet at the head
        var pl = Block(g, new Vector3(0, 0.58f, -L / 2 + 0.3f), new Vector3(w * 0.62f, 0.13f, 0.36f), White, 0.06f, false); pl.transform.localRotation = Q(-8, 0, 0);   // pillow
        Block(g, new Vector3(0, 0.58f, -L / 2 + 0.3f), new Vector3(w * 0.64f, 0.015f, 0.38f), Lt(quilt, 0.5f), 0.004f, false).transform.localRotation = Q(-8, 0, 0);   // pillow seam
        float qz0 = -L / 2 + 0.6f, qz1 = L / 2 - 0.08f, ql = qz1 - qz0, qw = w - 0.06f;
        Block(g, new Vector3(0, 0.53f, (qz0 + qz1) / 2), new Vector3(qw, 0.06f, ql), quilt, 0.03f, false);   // quilt slab
        foreach (int sx in new[] { -1, 1 }) Block(g, new Vector3(sx * (qw / 2 + 0.01f), 0.41f, (qz0 + qz1) / 2), new Vector3(0.03f, 0.2f, ql), quilt, 0.015f, false);   // drop over the sides
        int px = 4, pz = 5; Color pa = stars ? star : Lt(quilt, 0.25f);
        for (int i = 0; i < px; i++) for (int j = 0; j < pz; j++) if (((i + j) & 1) == 0) Block(g, new Vector3(-qw / 2 + (i + 0.5f) * qw / px, 0.563f, qz0 + 0.2f + (j + 0.5f) * (ql - 0.2f) / pz), new Vector3(qw / px - 0.04f, 0.006f, (ql - 0.2f) / pz - 0.04f), Color.Lerp(pa, White, 0.35f), 0.002f, false);   // patchwork
        for (int i = 1; i < px; i++) Block(g, new Vector3(-qw / 2 + i * qw / px, 0.565f, qz0 + 0.1f + ql / 2), new Vector3(0.012f, 0.006f, ql - 0.2f), Dk(quilt, 0.7f), 0.001f, false);   // stitch lines
        for (int j = 1; j < pz; j++) Block(g, new Vector3(0, 0.565f, qz0 + 0.2f + j * (ql - 0.2f) / pz), new Vector3(qw, 0.006f, 0.012f), Dk(quilt, 0.7f), 0.001f, false);
        Block(g, new Vector3(0, 0.575f, qz0 + 0.1f), new Vector3(qw + 0.01f, 0.05f, 0.22f), Lt(quilt, 0.45f), 0.02f, false);   // turned-down band
        Block(g, new Vector3(0, 0.6f, qz1 - 0.3f), new Vector3(qw * 0.9f, 0.06f, 0.36f), Lt(star, 0.2f), 0.025f, false);   // folded throw
        Block(g, new Vector3(0, 0.632f, qz1 - 0.3f), new Vector3(qw * 0.9f, 0.006f, 0.02f), Dk(star, 0.8f), 0.001f, false);
        MatA = om;
        var bc = g.gameObject.AddComponent<BoxCollider>(); bc.size = new Vector3(w, 0.8f, L); bc.center = new Vector3(0, 0.4f, 0); return g;
    }
    /// v0.9.7 nightstand with a drawer, knob and a shaded table lamp that actually lights the corner
    public static Transform Nightstand(Transform p, Vector3 pos, float yaw, Color body)
    {
        var g = Group(p, "sk_nightstand", pos, yaw); var om = PushM(MatK.Wood);
        foreach (int sx in new[] { -1, 1 }) foreach (int sz in new[] { -1, 1 }) Block(g, new Vector3(sx * 0.19f, 0.05f, sz * 0.15f), new Vector3(0.05f, 0.1f, 0.05f), Dk(body, 0.8f), 0.01f, false);
        Block(g, new Vector3(0, 0.3f, 0), new Vector3(0.46f, 0.4f, 0.38f), body, 0.03f, false);
        Block(g, new Vector3(0, 0.515f, 0), new Vector3(0.5f, 0.03f, 0.42f), Lt(body, 0.15f), 0.01f, false);
        Block(g, new Vector3(0, 0.36f, 0.19f), new Vector3(0.4f, 0.16f, 0.02f), Lt(body, 0.1f), 0.01f, false);
        Cyl(g, new Vector3(0, 0.36f, 0.205f), 0.02f, 0.02f, ButterD, 0.005f, Q(90, 0, 0), 8);
        Block(g, new Vector3(0, 0.18f, 0.19f), new Vector3(0.4f, 0.02f, 0.01f), Dk(body, 0.8f), 0.002f, false);
        MatA = MatK.Plastic;
        Lath(g, new Vector3(-0.05f, 0.53f, -0.02f), new[] { new Vector2(0, 0), new Vector2(0.07f, 0), new Vector2(0.075f, 0.02f), new Vector2(0.03f, 0.05f), new Vector2(0.015f, 0.08f), new Vector2(0.015f, 0.24f), new Vector2(0, 0.24f) }, Lt(Mint, 0.2f), 12);
        MatA = MatK.Fabric;
        var sh = Lath(g, new Vector3(-0.05f, 0.7f, -0.02f), new[] { new Vector2(0, 0.0f), new Vector2(0.14f, 0f), new Vector2(0.09f, 0.16f), new Vector2(0, 0.16f) }, new Color(1f, 0.93f, 0.8f), 16);
        var lo = new GameObject("sk_lamp_light"); lo.transform.SetParent(g, false); lo.transform.localPosition = new Vector3(-0.05f, 0.76f, -0.02f);
        var lt = lo.AddComponent<Light>(); lt.type = LightType.Point; lt.color = new Color(1f, 0.78f, 0.52f); lt.range = 3.2f; lt.intensity = 1.1f; lt.shadows = LightShadows.None;
        var bu = Art.Prim(PrimitiveType.Sphere, g, new Vector3(-0.05f, 0.75f, -0.02f), Vector3.one * 0.06f, WinLampM ?? (WinLampM = Art.Emissive(new Color(1f, 0.8f, 0.55f), 0.9f))); bu.name = "sk_bulb";
        Block(g, new Vector3(0.13f, 0.56f, 0.04f), new Vector3(0.14f, 0.05f, 0.1f), SkyD, 0.01f, false); Block(g, new Vector3(0.13f, 0.6f, 0.04f), new Vector3(0.12f, 0.03f, 0.09f), Coral, 0.01f, false);   // two books
        MatA = om; var bc = g.gameObject.AddComponent<BoxCollider>(); bc.size = new Vector3(0.5f, 0.55f, 0.42f); bc.center = new Vector3(0, 0.27f, 0); return g;
    }
    /// v0.9.7 low open bookshelf with books of varied height/width/colour, a leaning book, bookend and toy bin
    public static Transform LowShelf(Transform p, Vector3 pos, float yaw, float w, Color body, int seed)
    {
        var g = Group(p, "sk_lowshelf", pos, yaw); var r = new System.Random(seed * 7919 + Mathf.RoundToInt(pos.x * 13 + pos.z * 31 + yaw)); var om = PushM(MatK.Wood); float h = 0.95f, d = 0.32f;
        foreach (int sx in new[] { -1, 1 }) Block(g, new Vector3(sx * (w / 2 - 0.025f), h / 2, 0), new Vector3(0.05f, h, d), body, 0.01f, false);
        foreach (float y in new[] { 0.04f, 0.47f, h - 0.02f }) Block(g, new Vector3(0, y, 0), new Vector3(w, 0.04f, d), Lt(body, 0.1f), 0.01f, false);
        Block(g, new Vector3(0, h / 2, -d / 2 + 0.01f), new Vector3(w - 0.08f, h - 0.06f, 0.02f), Dk(body, 0.85f), 0.004f, false);
        Color[] bk = { Coral, SkyD, ButterD, MintD, LavD, PinkD, Data.H("3f6e8c"), Data.H("8c4a3f"), Data.H("e8d6b0") };
        MatA = MatK.Plastic;
        foreach (float y0 in new[] { 0.06f, 0.49f })
        {
            float x = -w / 2 + 0.07f, lim = y0 < 0.2f ? w * 0.55f : w / 2 - 0.07f;
            while (true)
            {
                float bw = 0.035f + (float)r.NextDouble() * 0.04f, bh = 0.24f + (float)r.NextDouble() * 0.14f, bd = 0.19f + (float)r.NextDouble() * 0.07f;
                if (x + bw > lim) break; Color c = bk[r.Next(bk.Length)];
                Block(g, new Vector3(x + bw / 2, y0 + bh / 2, 0.02f), new Vector3(bw, bh, bd), c, 0.004f, false);
                Picture(g, new Vector3(x + bw / 2, y0 + bh / 2, 0.02f + bd / 2 + 0.002f), Q(0, 180, 0), bw * 0.96f, bh * 0.97f, "art_sp" + r.Next(8));   // v0.9.9 illustrated spine
                x += bw + 0.004f;
            }
            if (y0 > 0.2f) { var lb = Block(g, new Vector3(x + 0.11f, y0 + 0.14f, 0.02f), new Vector3(0.03f, 0.3f, 0.22f), bk[r.Next(bk.Length)], 0.004f, false); lb.transform.localRotation = Q(0, 0, -25); }
            else
            {   // fabric toy bin in the lower right bay
                MatA = MatK.Fabric; float bx = w * 0.55f + (w / 2 - 0.05f - w * 0.55f) / 2, bwid = (w / 2 - 0.05f - w * 0.55f) - 0.04f;
                Block(g, new Vector3(bx, y0 + 0.17f, 0.02f), new Vector3(bwid, 0.34f, 0.26f), Lt(Mint, 0.15f), 0.03f, false);
                Block(g, new Vector3(bx, y0 + 0.24f, 0.152f), new Vector3(0.1f, 0.025f, 0.01f), Dk(Mint, 0.6f), 0.005f, false);   // handle slot
                MatA = MatK.Plastic;
            }
        }
        MatA = om; var bc = g.gameObject.AddComponent<BoxCollider>(); bc.size = new Vector3(w, h, d); bc.center = new Vector3(0, h / 2, 0); return g;
    }
    /// v0.9.7 picture frame: moulded frame, mat, simple painted scene
    public static int ShopIcon(string n)
    {   string[][] k = { new[]{"早餐"}, new[]{"冰"}, new[]{"書"}, new[]{"花"}, new[]{"麵包","烘焙"}, new[]{"豆花"}, new[]{"文具"}, new[]{"理髮","髮"} };
        if (n.Contains("豆花")) return 5; for (int i = 0; i < k.Length; i++) foreach (var t in k[i]) if (n.Contains(t)) return i; return 8; }
    static readonly Dictionary<string, Material> PicMats = new Dictionary<string, Material>();
    // v0.9.12 wrap-around printed label hugging a cylinder (bottles, jars, tins) - faces local +z
    public static Transform CurvedLabel(Transform p, Vector3 axisPos, float R, float h, float arcDeg, string art)
    {
        var t = Picture(p, axisPos, Quaternion.identity, 1, 1, art); t.localScale = Vector3.one; t.name = "sk_label";
        int N = 24; var V = new List<Vector3>(); var Nn = new List<Vector3>(); var U = new List<Vector2>(); var T = new List<int>();
        for (int i = 0; i <= N; i++) { float u = (float)i / N, a = Mathf.Deg2Rad * arcDeg * (0.5f - u); var d = new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a));
            V.Add(d * R + Vector3.down * h / 2); V.Add(d * R + Vector3.up * h / 2); Nn.Add(d); Nn.Add(d); U.Add(new Vector2(u, 0)); U.Add(new Vector2(u, 1)); }
        for (int i = 0; i < N; i++) { int a = i * 2; T.AddRange(new[] { a, a + 1, a + 2, a + 2, a + 1, a + 3, a, a + 2, a + 1, a + 2, a + 3, a + 1 }); }
        var m = new Mesh { name = "sk_label" }; m.SetVertices(V); m.SetNormals(Nn); m.SetUVs(0, U); m.SetTriangles(T, 0); m.RecalculateBounds();
        t.GetComponent<MeshFilter>().sharedMesh = m; return t;
    }
    public static Transform Picture(Transform p, Vector3 lpos, Quaternion lrot, float w, float h, string art)
    {   
        // v0.9.8: flat artwork is a single illustrated texture on a quad inside a modelled frame
        var q = GameObject.CreatePrimitive(PrimitiveType.Quad); Object.Destroy(q.GetComponent<Collider>()); q.name = "sk_art";
        q.transform.SetParent(p, false); q.transform.localPosition = lpos; q.transform.localRotation = lrot; q.transform.localScale = new Vector3(w, h, 1);
        var r = q.GetComponent<MeshRenderer>(); if (!PicMats.TryGetValue(art, out var am) || am == null) { am = Art.Cutout(Resources.Load<Texture2D>("Art/" + art)); am.SetColor("_BaseColor", new Color(0.86f, 0.82f, 0.78f)); if (art.StartsWith("art_menu") || art.StartsWith("art_sg") || art.StartsWith("art_plate") || art.StartsWith("art_lb") || art.StartsWith("art_ic") || art.StartsWith("art_cv") || art.StartsWith("art_tv") || art.StartsWith("art_blk") || art.StartsWith("art_bin") || art.StartsWith("art_clock")) am.SetColor("_BaseColor", new Color(1f, 0.97f, 0.92f)); PicMats[art] = am; } r.sharedMaterial = am; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;   // v0.9.9 one shared material per artwork   // unlit, toned to sit in the warm room light r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return q.transform;
    }
    public static Transform WallFrame(Transform p, Vector3 pos, float yaw, float w, float h, Color frame, int kind)
    {
        var g = Group(p, "sk_frame", pos, yaw); var om = PushM(MatK.Wood);
        Block(g, Vector3.zero, new Vector3(w, h, 0.04f), frame, 0.01f, false);
        Block(g, new Vector3(0, 0, 0.022f), new Vector3(w - 0.08f, h - 0.08f, 0.01f), White, 0.002f, false);
        float iw = w - 0.18f, ih = h - 0.18f; Color sky = kind == 0 ? Lt(Sky, 0.3f) : Lt(Butter, 0.3f);
        Block(g, new Vector3(0, 0, 0.03f), new Vector3(iw, ih, 0.005f), sky, 0.001f, false);
        Picture(g, new Vector3(0, 0, 0.034f), Q(0, 180, 0), iw, ih, kind == 0 ? "art_hills" : "art_kid");   // v0.9.8 painted illustration (was primitive hills / house shapes)
        MatA = om; return g;
    }
    public static Transform Counter(Transform p, Vector3 pos, float yaw, float w, Color body, Color top, string kind)
    {
        var g = Group(p, "sk_counter", pos, yaw);
        Block(g, new Vector3(0, 0.45f, 0), new Vector3(w, 0.9f, 0.7f), body, 0.1f, false); Block(g, new Vector3(0, 0.93f, 0), new Vector3(w + 0.06f, 0.08f, 0.76f), top, 0.03f, false);
        int n = Mathf.Max(1, Mathf.RoundToInt(w / 0.7f)); for (int i = 0; i < n; i++) { float x = -w / 2 + (i + 0.5f) * w / n; Block(g, new Vector3(x, 0.45f, 0.35f), new Vector3(w / n - 0.08f, 0.75f, 0.04f), Lt(body, 0.2f), 0.04f, false); Ball(g, new Vector3(x + 0.18f, 0.7f, 0.38f), Vector3.one * 0.06f, ButterD, 6); }
        if (kind == "stove") { foreach (int s in new[] { -1, 1 }) Ring(g, new Vector3(s * 0.25f, 0.98f, 0), Q(90, 0, 0), 0.13f, 0.025f, Plum, 16); Lath(g, new Vector3(0.25f, 0.98f, 0), new[] { new Vector2(0, 0), new Vector2(0.17f, 0), new Vector2(0.2f, 0.22f), new Vector2(0, 0.24f) }, Coral, 14); }
        if (kind == "sink") { Block(g, new Vector3(0, 0.95f, 0), new Vector3(0.6f, 0.04f, 0.45f), Lt(SkyD, 0.3f), 0.03f, false); Cyl(g, new Vector3(0, 1.15f, -0.3f), 0.03f, 0.4f, Grey, 0.01f); Cyl(g, new Vector3(0, 1.33f, -0.18f), 0.03f, 0.25f, Grey, 0.01f, Q(90, 0, 0), 8); }
        var bc = g.gameObject.AddComponent<BoxCollider>(); bc.size = new Vector3(w, 1f, 0.72f); bc.center = new Vector3(0, 0.5f, 0); return g;
    }
    public static Transform Bathtub(Transform p, Vector3 pos, float yaw)
    {
        var g = Group(p, "sk_tub", pos, yaw);
        Block(g, new Vector3(0, 0.3f, 0), new Vector3(1.7f, 0.5f, 0.85f), White, 0.25f, false);
        Block(g, new Vector3(0, 0.5f, 0), new Vector3(1.45f, 0.08f, 0.6f), Sky, 0.04f, false);
        // v0.9.5 real tub details: rim lip, faucet + spout, drain, block feet, a bath mat — no ball bubbles
        Block(g, new Vector3(0, 0.56f, 0), new Vector3(1.74f, 0.05f, 0.89f), White, 0.03f, false);
        Cyl(g, new Vector3(0.78f, 0.62f, 0), 0.035f, 0.14f, Grey, 0.01f, null, 10);
        Block(g, new Vector3(0.72f, 0.69f, 0), new Vector3(0.16f, 0.035f, 0.04f), Grey, 0.01f, false);
        Cyl(g, new Vector3(0.6f, 0.545f, 0), 0.04f, 0.01f, Grey, 0f, null, 10);
        foreach (int sx in new[] { -1, 1 }) foreach (int sz in new[] { -1, 1 }) Block(g, new Vector3(sx * 0.72f, 0.03f, sz * 0.32f), new Vector3(0.1f, 0.06f, 0.1f), ButterD, 0.02f, false);
        var bc = g.gameObject.AddComponent<BoxCollider>(); bc.size = new Vector3(1.7f, 0.7f, 0.85f); bc.center = new Vector3(0, 0.35f, 0); return g;
    }
    public static Transform Toilet(Transform p, Vector3 pos, float yaw)
    {
        var g = Group(p, "sk_toilet", pos, yaw);
        Lath(g, Vector3.zero, new[] { new Vector2(0, 0), new Vector2(0.17f, 0), new Vector2(0.21f, 0.3f), new Vector2(0.24f, 0.42f), new Vector2(0, 0.44f) }, White, 18);
        Block(g, new Vector3(0, 0.65f, -0.22f), new Vector3(0.45f, 0.45f, 0.2f), White, 0.1f, false);
        var bc = g.gameObject.AddComponent<BoxCollider>(); bc.size = new Vector3(0.5f, 0.85f, 0.6f); bc.center = new Vector3(0, 0.42f, -0.05f); return g;
    }
    public static Transform Plant(Transform p, Vector3 pos, float s = 1f)
    {
        var g = Group(p, "sk_plant", pos);
        Lath(g, Vector3.zero, new[] { new Vector2(0, 0), new Vector2(0.18f, 0), new Vector2(0.24f, 0.35f), new Vector2(0, 0.35f) }.Scale(s), Coral, 14);
        for (int i = 0; i < 6; i++) { float a = i * 1.05f; var l = Ball(g, new Vector3(Mathf.Cos(a) * 0.18f, 0.6f + (i % 3) * 0.12f, Mathf.Sin(a) * 0.18f) * s, new Vector3(0.14f, 0.42f, 0.04f) * s, i % 2 == 0 ? Leaf : LeafD, 8, false, null, "sk_leaf", 0.02f); l.transform.localRotation = Q(0, -a * Mathf.Rad2Deg, 35); }   // v0.9.4: thin blades
        var bc = g.gameObject.AddComponent<BoxCollider>(); bc.size = new Vector3(0.5f, 1f, 0.5f) * s; bc.center = new Vector3(0, 0.5f * s, 0); return g;
    }
    public static Transform FloorLamp(Transform p, Vector3 pos)
    {
        var g = Group(p, "sk_flamp", pos); Cyl(g, new Vector3(0, 0.04f, 0), 0.2f, 0.08f, WoodD); Cyl(g, new Vector3(0, 0.75f, 0), 0.03f, 1.4f, WoodD, 0.01f);
        Lath(g, new Vector3(0, 1.4f, 0), new[] { new Vector2(0, 0), new Vector2(0.3f, 0), new Vector2(0.18f, 0.35f), new Vector2(0, 0.35f) }, Butter, 16); return g;
    }
    public static Transform Frame(Transform p, Vector3 pos, float yaw, float w, float h, Color frame, Color art)
    {
        var g = Group(p, "sk_frame", pos, yaw); Block(g, Vector3.zero, new Vector3(w, h, 0.06f), frame, 0.03f, false); Block(g, new Vector3(0, 0, 0.03f), new Vector3(w - 0.08f, h - 0.08f, 0.02f), H("f3e6cf"), 0.005f, false);
        Picture(g, new Vector3(0, 0, 0.045f), Q(0, 180, 0), w - 0.18f, h - 0.18f, "art_frame"); return g;   // v0.9.10 gouache sunflower painting
    }
    public static Transform Clock(Transform p, Vector3 pos, float yaw)
    {   // v0.9.11: carved wooden pendulum wall clock - pediment, brass bezel + glass, painted wreath face, 3D hands, swinging pendulum window
        var g = Group(p, "sk_clock", pos, yaw); var wd = H("8a5534"); var wl = H("b5774a"); var br = H("d9a43c");
        var omw = PushM(MatK.Wood);
        Block(g, new Vector3(0, -0.2f, -0.02f), new Vector3(0.62f, 1.05f, 0.1f), wd, 0.04f, false);            // case
        Block(g, new Vector3(0, -0.2f, 0.035f), new Vector3(0.56f, 0.99f, 0.02f), wl, 0.02f, false);           // raised panel
        var pd = Block(g, new Vector3(0, 0.38f, -0.01f), new Vector3(0.5f, 0.5f, 0.09f), wd, 0.04f, false); pd.transform.localRotation = Q(0, 0, 45);   // gabled pediment
        Ball(g, new Vector3(0, 0.74f, 0.0f), Vector3.one * 0.08f, br, 10);                                     // finial
        foreach (int sd in new[] { -1, 1 }) { Cyl(g, new Vector3(sd * 0.33f, -0.2f, 0.02f), 0.025f, 1.0f, wl, 0.01f, Quaternion.identity, 10); Ball(g, new Vector3(sd * 0.33f, 0.31f, 0.02f), Vector3.one * 0.06f, br, 8); }   // turned side columns
        Block(g, new Vector3(0, -0.74f, -0.01f), new Vector3(0.7f, 0.06f, 0.14f), wd, 0.02f, false);           // base moulding
        MatA = omw;
        Ring(g, new Vector3(0, 0, 0.06f), Quaternion.identity, 0.255f, 0.03f, br, 28);                         // brass bezel
        Picture(g, new Vector3(0, 0, 0.052f), Q(0, 180, 0), 0.47f, 0.47f, "art_clock");                       // painted wreath face
        Block(g, new Vector3(-0.08f, 0.1f, 0.079f), new Vector3(0.11f, 0.02f, 0.002f), White, 0.002f, false).transform.localRotation = Q(0, 0, 40);   // glass glint
        var hh = Block(g, new Vector3(0.0f, 0.05f, 0.066f), new Vector3(0.036f, 0.12f, 0.008f), H("2a1c16"), 0.004f, false); hh.transform.localRotation = Q(0, 0, -60); hh.transform.localPosition = new Vector3(0.048f, 0.028f, 0.066f);
        var mh = Block(g, new Vector3(0, 0.08f, 0.068f), new Vector3(0.024f, 0.18f, 0.008f), H("2a1c16"), 0.004f, false); mh.transform.localRotation = Q(0, 0, 10); mh.transform.localPosition = new Vector3(-0.015f, 0.084f, 0.068f);
        Ball(g, new Vector3(0, 0, 0.072f), Vector3.one * 0.045f, br, 8);                                        // centre cap
        Block(g, new Vector3(0, -0.47f, 0.047f), new Vector3(0.36f, 0.44f, 0.01f), H("2e2230"), 0.02f, false);  // pendulum window
        var pv = new GameObject("pendulum").transform; pv.SetParent(g, false); pv.localPosition = new Vector3(0, -0.27f, 0.055f);
        Block(pv, new Vector3(0, -0.15f, 0), new Vector3(0.015f, 0.3f, 0.006f), br, 0.003f, false); Cyl(pv, new Vector3(0, -0.32f, 0.004f), 0.06f, 0.012f, br, 0.004f, Q(90, 0, 0), 18);
        pv.gameObject.AddComponent<Swing>();
        return g;
    }
    public static Transform Curtains(Transform p, Vector3 center, float w, float h, Color c)
    {   // v0.9.12: two pleated cloth panels gathered on a turned rod with rings + finials, tied back with sashes (was two rounded boxes)
        var g = Group(p, "sk_curtain", center); float rodY = h / 2 + 0.1f, pw = Mathf.Max(0.55f, w * 0.34f), ph = h + 0.05f;
        var omw = PushM(MatK.Wood); Cyl(g, new Vector3(0, rodY, -0.02f), 0.022f, w + 0.75f, WoodD, 0.008f, Q(0, 0, 90), 16);
        foreach (int s in new[] { -1, 1 })
        {
            float ex = s * (w / 2 + 0.38f);
            Lath(g, new Vector3(ex, rodY, -0.02f), new[] { new Vector2(0, 0), new Vector2(0.035f, 0.005f), new Vector2(0.05f, 0.03f), new Vector2(0.03f, 0.06f), new Vector2(0.045f, 0.085f), new Vector2(0, 0.11f) }, Lt(WoodD, 0.1f), 16, false, Q(0, 0, -s * 90));   // turned finial
            Block(g, new Vector3(s * (w / 2 + 0.22f), rodY + 0.0f, 0.03f), new Vector3(0.03f, 0.05f, 0.08f), Dk(WoodD, 0.85f), 0.012f, false);   // wall bracket
        }
        MatA = omw;
        foreach (int s in new[] { -1, 1 })
        {
            float cx = s * (w / 2 + 0.1f);
            Drape(g, new Vector3(cx, rodY - 0.04f - ph / 2, -0.02f), Quaternion.identity, pw, ph, 5, 0.07f, c, 0.42f, s);
            for (int k = 0; k < 6; k++) Ring(g, new Vector3(cx - pw / 2 + 0.05f + k * (pw - 0.1f) / 5f, rodY, -0.02f), Q(0, 90, 0), 0.032f, 0.006f, Lt(WoodD, 0.25f), 14);   // curtain rings
            float tx = s * (w / 2 + 0.1f + pw * 0.3f), ty = rodY - 0.04f - ph * 0.58f;
            var sash = Obj(g, "sk_sash", Torus(pw * 0.24f, 0.02f, Dk(c, 0.78f), 24, 10, 3), new Vector3(tx, ty, -0.08f), Q(90, 0, 0) * Quaternion.Euler(0, 0, 0)); sash.transform.localScale = new Vector3(1f, 1f, 0.55f);   // fabric tie-back band
            Ball(g, new Vector3(tx - s * 0.04f, ty - 0.02f, -0.14f), new Vector3(0.07f, 0.05f, 0.04f), Dk(c, 0.72f), 10);   // sash knot
            Cyl(g, new Vector3(s * (w / 2 + 0.3f), ty, 0.02f), 0.018f, 0.06f, Dk(WoodD, 0.9f), 0.006f, Q(90, 0, 0), 12);   // wall hook
        }
        return g;
    }
    public static Transform PetBed(Transform p, Vector3 pos, Color c)
    {
        var g = Group(p, "sk_petbed", pos); var t = Ring(g, new Vector3(0, 0.12f, 0), Q(90, 0, 0), 0.38f, 0.14f, c, 24); Cyl(g, new Vector3(0, 0.05f, 0), 0.4f, 0.1f, Lt(PinkD, 0.3f), 0.04f); return g;
    }
    public static Transform Teddy(Transform p, Vector3 pos, float yaw, float s = 1f)
    {   // v0.9.9 plush bear: pear body, tapered limbs with paw pads, muzzle w/ stitched mouth, seam stitching, ribbon
        var g = Group(p, "sk_bear", pos, yaw); var fur = H("c98a55"); var lite = H("f2d6b0"); var st = H("6b4630"); var om = PushM(MatK.Felt);
        Ball(g, new Vector3(0, 0.22f, 0) * s, new Vector3(0.4f, 0.44f, 0.34f) * s, fur, 18);
        Ball(g, new Vector3(0, 0.2f, 0.1f) * s, new Vector3(0.26f, 0.28f, 0.16f) * s, lite, 14);   // belly patch
        for (int i = 0; i < 6; i++) Block(g, new Vector3(0, 0.1f + i * 0.04f, 0.18f) * s, new Vector3(0.012f, 0.022f, 0.008f) * s, st, 0.002f, false);   // seam stitches
        Ball(g, new Vector3(0, 0.6f, 0) * s, new Vector3(0.4f, 0.37f, 0.36f) * s, fur, 18);
        Ball(g, new Vector3(0, 0.56f, 0.15f) * s, new Vector3(0.17f, 0.13f, 0.12f) * s, lite, 12);   // muzzle
        Ball(g, new Vector3(0, 0.59f, 0.215f) * s, new Vector3(0.06f, 0.045f, 0.04f) * s, st, 8);   // nose
        Block(g, new Vector3(0, 0.55f, 0.21f) * s, new Vector3(0.008f, 0.03f, 0.006f) * s, st, 0.002f, false);
        foreach (int k in new[] { -1, 1 })
        {
            Block(g, new Vector3(k * 0.02f, 0.53f, 0.205f) * s, new Vector3(0.03f, 0.008f, 0.006f) * s, st, 0.002f, false).transform.localRotation = Q(0, 0, k * -25);   // smile
            Ball(g, new Vector3(k * 0.075f, 0.65f, 0.165f) * s, new Vector3(0.045f, 0.05f, 0.03f) * s, H("2a1d18"), 8); Ball(g, new Vector3(k * 0.068f, 0.665f, 0.18f) * s, Vector3.one * 0.012f * s, White, 6);   // eyes + glint
            Ball(g, new Vector3(k * 0.15f, 0.78f, 0) * s, new Vector3(0.14f, 0.14f, 0.08f) * s, fur, 12); Ball(g, new Vector3(k * 0.15f, 0.78f, 0.03f) * s, new Vector3(0.08f, 0.08f, 0.03f) * s, lite, 8);   // ears
            var arm = Ball(g, new Vector3(k * 0.2f, 0.27f, 0.07f) * s, new Vector3(0.1f, 0.22f, 0.11f) * s, fur, 12); arm.transform.localRotation = Q(-35, 0, k * 30);   // tapered arm
            Ball(g, new Vector3(k * 0.26f, 0.18f, 0.13f) * s, new Vector3(0.06f, 0.06f, 0.03f) * s, lite, 8);
            var leg = Ball(g, new Vector3(k * 0.11f, 0.06f, 0.12f) * s, new Vector3(0.14f, 0.12f, 0.24f) * s, fur, 12); leg.transform.localRotation = Q(0, k * -10, 0);
            Ball(g, new Vector3(k * 0.12f, 0.065f, 0.24f) * s, new Vector3(0.1f, 0.1f, 0.02f) * s, lite, 10);   // foot pad
            for (int t = 0; t < 3; t++) Ball(g, new Vector3(k * 0.12f + (t - 1) * 0.028f, 0.1f, 0.248f) * s, Vector3.one * 0.022f * s, lite, 6);   // toe beans
        }
        foreach (int k in new[] { -1, 1 }) Ball(g, new Vector3(k * 0.05f, 0.43f, 0.15f) * s, new Vector3(0.09f, 0.06f, 0.04f) * s, H("e8473f"), 8);   // ribbon bow
        Ball(g, new Vector3(0, 0.43f, 0.165f) * s, Vector3.one * 0.035f * s, H("b3302a"), 6);
        MatA = om; return g;
    }
    public static Transform Piggy(Transform p, Vector3 pos, float yaw, float s = 1f)
    {
        var g = Group(p, "sk_piggy", pos, yaw); var c = Rose;
        Ball(g, Vector3.zero, new Vector3(0.38f, 0.32f, 0.46f) * s, Lt(c, 0.25f), 16); Cyl(g, new Vector3(0, 0, 0.24f) * s, 0.08f * s, 0.06f * s, c, 0.02f, Q(90, 0, 0), 12);
        foreach (int k in new[] { -1, 1 })
        {
            Ball(g, new Vector3(k * 0.1f, 0.16f, 0.1f) * s, new Vector3(0.09f, 0.1f, 0.04f) * s, c, 8); Ball(g, new Vector3(k * 0.08f, 0.05f, 0.21f) * s, Vector3.one * 0.04f * s, Plum, 6);
            foreach (float z in new[] { -0.12f, 0.12f }) Cyl(g, new Vector3(k * 0.1f, -0.15f, z) * s, 0.04f * s, 0.1f * s, c, 0.02f);
        }
        Block(g, new Vector3(0, 0.16f, 0) * s, new Vector3(0.12f, 0.01f, 0.03f) * s, Plum, 0.004f, false);
        return g;
    }
    /// v0.9.7 wooden toy blocks: bevelled painted cubes with letters/numbers, cylinders, an arch and triangles;
    /// a small stacked tower plus pieces lying flat - every piece rests on the floor or on the piece under it
    public static Transform ToyBlocks(Transform p, Vector3 pos, int n, System.Random rng)
    {
        var g = Group(p, "sk_toys", pos); Color[] tc = { H("e8473f"), H("2fa84f"), H("2f7fe0"), H("ffc21a"), H("9b4fd8"), H("ff7fb0") }; var om = PushM(MatK.Plastic);   // v0.9.9 saturated toy paint (muted house palette read grey)   // v0.9.9 painted pieces use glossy paint (wood/plaster material greyed them out)
        string[] gl = { "A", "B", "C", "1", "2", "3", "★", "D", "4" }; float s = 0.16f; int li = 0;
        void Cube(Vector3 at, float yaw, Color c)
        {
            var cb = Group(g, "sk_toyblock", at, yaw);
            c = Color.Lerp(Dk(c, 0.12f), c, (float)rng.NextDouble()); var wood = Color.Lerp(Lt(Wood, 0.2f), Lt(Wood, 0.35f), (float)rng.NextDouble());
            var omw = PushM(MatK.Wood); Block(cb, new Vector3(0, s / 2, 0), Vector3.one * s, wood, 0.032f, false); MatA = omw;   // v0.9.8 deep bevel: raw beech core shows at the rounded edges
            foreach (var (fp, fr) in new[] { (new Vector3(0, s / 2, -s / 2 - 0.001f), Q(0, 0, 0)), (new Vector3(0, s / 2, s / 2 + 0.001f), Q(0, 180, 0)), (new Vector3(-s / 2 - 0.001f, s / 2, 0), Q(0, 90, 0)), (new Vector3(s / 2 + 0.001f, s / 2, 0), Q(0, -90, 0)) })
            {
                Picture(cb, fp, fr, s * 0.86f, s * 0.86f, "art_blk" + (li++ % 9));   // v0.9.9 painted letter tile flush on the face (were floating TextMesh letters + panel slabs)
            }
            Block(cb, new Vector3(0, s + 0.001f, 0), new Vector3(s * 0.8f, 0.004f, s * 0.8f), Lt(c, 0.25f), 0.002f, false);
        }
        int k0 = rng.Next(6);
        // tower: two cubes, a plank, an arch, a triangle roof
        Cube(new Vector3(-0.2f, 0, 0), 0, tc[k0 % 6]); Cube(new Vector3(0.0f, 0, 0), 0, tc[(k0 + 1) % 6]);
        Block(g, new Vector3(-0.1f, s + 0.025f, 0), new Vector3(0.42f, 0.05f, 0.14f), tc[(k0 + 2) % 6], 0.012f, false, 0, -1, 0, null, "sk_toyplank");
        float ay = s + 0.05f;
        foreach (int sx in new[] { -1, 1 }) Block(g, new Vector3(-0.1f + sx * 0.1f, ay + 0.06f, 0), new Vector3(0.06f, 0.12f, 0.12f), tc[(k0 + 3) % 6], 0.01f, false);   // arch legs
        Arc(g, new Vector3(-0.1f, ay + 0.12f, 0), 0.1f, 0, Mathf.PI, 0.03f, tc[(k0 + 3) % 6], 8);
        Block(g, new Vector3(-0.1f, ay + 0.2f, 0), new Vector3(0.26f, 0.05f, 0.12f), tc[(k0 + 3) % 6], 0.01f, false);   // arch top bridge
        var tri = Cyl(g, new Vector3(-0.1f, ay + 0.225f, 0), 0.12f, 0.12f, tc[(k0 + 4) % 6], 0.01f, Q(90, 0, 0), 3); tri.transform.localRotation = Q(90, 0, 0) * Q(0, 90, 0); tri.transform.localPosition = new Vector3(-0.1f, ay + 0.225f + 0.06f, 0);   // triangular prism roof (flat side down)
        // pieces lying around
        Cube(new Vector3(0.38f, 0, 0.22f), 23, tc[(k0 + 4) % 6]);
        Cube(new Vector3(-0.42f, 0, 0.3f), -12, tc[(k0 + 5) % 6]);
        if (n > 5) Cube(new Vector3(0.12f, 0, -0.34f), 40, tc[(k0 + 2) % 6]);
        // v0.9.9 real toys instead of bare cylinders/prisms: wooden train, panelled beach ball, tin robot
        {   var tr = Group(g, "sk_toytrain", new Vector3(0.32f, 0, -0.2f), 35);
            for (int ci = 0; ci < 3; ci++) { float x = -ci * 0.15f; var cc = tc[(k0 + ci + 1) % 6];
                Block(tr, new Vector3(x, 0.055f, 0), new Vector3(0.12f, 0.06f, 0.08f), cc, 0.012f, false);
                if (ci == 0) { Block(tr, new Vector3(x - 0.025f, 0.11f, 0), new Vector3(0.06f, 0.06f, 0.08f), cc, 0.01f, false); Block(tr, new Vector3(x - 0.025f, 0.145f, 0), new Vector3(0.08f, 0.012f, 0.1f), H("3a2a24"), 0.004f, false); Cyl(tr, new Vector3(x + 0.035f, 0.105f, 0), 0.016f, 0.05f, H("3a2a24"), 0.004f, null, 10); Cyl(tr, new Vector3(x + 0.062f, 0.055f, 0), 0.026f, 0.012f, H("ffe07a"), 0.003f, Q(0, 0, 90), 12); }
                else Block(tr, new Vector3(x, 0.095f, 0), new Vector3(0.09f, 0.03f, 0.06f), tc[(k0 + ci + 3) % 6], 0.012f, false);
                foreach (float wx in new[] { -0.035f, 0.035f }) foreach (float wz in new[] { -0.045f, 0.045f }) { Cyl(tr, new Vector3(x + wx, 0.024f, wz), 0.022f, 0.012f, H("3a2a24"), 0.004f, Q(90, 0, 0), 12); Cyl(tr, new Vector3(x + wx, 0.024f, wz * 1.15f), 0.008f, 0.006f, H("ffc21a"), 0.002f, Q(90, 0, 0), 8); }
                if (ci > 0) Block(tr, new Vector3(x + 0.075f, 0.04f, 0), new Vector3(0.04f, 0.01f, 0.012f), H("3a2a24"), 0.003f, false); }
        }
        {   var bb = Group(g, "sk_beachball", new Vector3(-0.05f, 0.09f, 0.36f), 20); float br = 0.09f;
            { var bm = Art.Cutout(Resources.Load<Texture2D>("Art/art_beachball")); var sp = Art.Prim(PrimitiveType.Sphere, bb, Vector3.zero, Vector3.one * br * 2, bm); sp.name = "sk_ballpanels"; sp.transform.localRotation = Q(0, 0, 18); }   // v0.9.9 r3 textured 6-panel ball
        }
        {   var rb = Group(g, "sk_toyrobot", new Vector3(0.58f, 0, 0.05f), -30); rb.localScale = Vector3.one * 1.25f; var mc = H("d9553f"); var md = H("3f6fa6");   // v0.9.10 painted tin toy: red body, blue limbs
            foreach (int lx in new[] { -1, 1 }) { Cyl(rb, new Vector3(lx * 0.03f, 0.04f, 0), 0.017f, 0.06f, md, 0.006f, null, 16); Obj(rb, "sk_knee", SoftKit.Torus(0.018f, 0.005f, md, 16, 6, 2), new Vector3(lx * 0.03f, 0.05f, 0), Q(90, 0, 0), 0.002f); Ball(rb, new Vector3(lx * 0.03f, 0.012f, 0.01f), new Vector3(0.05f, 0.026f, 0.066f), tc[(k0 + 2) % 6], 14); }
            Block(rb, new Vector3(0, 0.115f, 0), new Vector3(0.11f, 0.09f, 0.07f), mc, 0.03f, false, 0, -1, 0.12f);   /* v0.9.12 rounded tin barrel, tapered */
            Block(rb, new Vector3(0, 0.12f, 0.036f), new Vector3(0.06f, 0.045f, 0.006f), H("2b3440"), 0.012f, false);
            for (int di = 0; di < 3; di++) Ball(rb, new Vector3(-0.018f + di * 0.018f, 0.12f, 0.04f), Vector3.one * 0.011f, tc[(k0 + di) % 6], 6);
            foreach (int ax in new[] { -1, 1 }) { Cyl(rb, new Vector3(ax * 0.07f, 0.12f, 0), 0.014f, 0.06f, md, 0.004f, Q(0, 0, ax * 20), 10); Ball(rb, new Vector3(ax * 0.08f, 0.085f, 0), Vector3.one * 0.026f, tc[(k0 + 3) % 6], 8); }
            Block(rb, new Vector3(0, 0.2f, 0), new Vector3(0.085f, 0.065f, 0.07f), mc, 0.028f, false); Ball(rb, new Vector3(0, 0.232f, 0), new Vector3(0.07f, 0.03f, 0.058f), Dk(mc, 0.92f), 16);   /* domed cap */
            foreach (int ex in new[] { -1, 1 }) { Ball(rb, new Vector3(ex * 0.02f, 0.205f, 0.036f), new Vector3(0.022f, 0.022f, 0.008f), H("fff3a0"), 8); Ball(rb, new Vector3(ex * 0.02f, 0.205f, 0.04f), new Vector3(0.009f, 0.009f, 0.004f), H("2b3440"), 6); }
            Block(rb, new Vector3(0, 0.183f, 0.036f), new Vector3(0.04f, 0.006f, 0.004f), H("2b3440"), 0.002f, false);
            Cyl(rb, new Vector3(0, 0.245f, 0), 0.004f, 0.03f, md, 0.002f, null, 6); Ball(rb, new Vector3(0, 0.265f, 0), Vector3.one * 0.016f, tc[k0 % 6], 8);
            {   // v0.9.10 detail: ear bolts, chest grille, rivets, belt, wind-up key, claw hands
                foreach (int ex in new[] { -1, 1 }) { Cyl(rb, new Vector3(ex * 0.046f, 0.205f, 0), 0.018f, 0.012f, tc[(k0 + 4) % 6], 0.003f, Q(0, 0, 90), 10); Cyl(rb, new Vector3(ex * 0.053f, 0.205f, 0), 0.007f, 0.01f, md, 0.002f, Q(0, 0, 90), 6); }
                for (int gi = 0; gi < 4; gi++) Block(rb, new Vector3(0, 0.1f + gi * 0.0f - 0.0f, 0.036f), new Vector3(0.05f, 0.003f, 0.003f), H("9aa6b2"), 0.001f, false).transform.localPosition = new Vector3(0, 0.086f + gi * 0.007f, 0.037f);
                foreach (int rx in new[] { -1, 1 }) foreach (int ry in new[] { -1, 1 }) Ball(rb, new Vector3(rx * 0.046f, 0.115f + ry * 0.036f, 0.036f), Vector3.one * 0.007f, md, 5);
                Block(rb, new Vector3(0, 0.073f, 0), new Vector3(0.115f, 0.012f, 0.075f), tc[(k0 + 1) % 6], 0.004f, false); Block(rb, new Vector3(0, 0.162f, 0), new Vector3(0.04f, 0.014f, 0.04f), md, 0.004f, false);   // belt + neck
                Cyl(rb, new Vector3(0, 0.12f, -0.045f), 0.005f, 0.03f, H("e8b84a"), 0.002f, Q(90, 0, 0), 6); foreach (int kx in new[] { -1, 1 }) Ball(rb, new Vector3(kx * 0.014f, 0.12f, -0.062f), new Vector3(0.018f, 0.012f, 0.005f), H("e8b84a"), 6);   // wind-up key
                foreach (int ax in new[] { -1, 1 }) foreach (int cz in new[] { -1, 1 }) Block(rb, new Vector3(ax * 0.085f, 0.064f, cz * 0.008f), new Vector3(0.008f, 0.02f, 0.006f), md, 0.002f, false);   // claws
                // v0.9.12 r2 tin-toy finish: shoulder caps, chest gauge with needle, mouth grille, cream racing stripe, foot treads
                foreach (int ax in new[] { -1, 1 }) Ball(rb, new Vector3(ax * 0.06f, 0.148f, 0), new Vector3(0.036f, 0.03f, 0.05f), tc[(k0 + 2) % 6], 12);
                Cyl(rb, new Vector3(0.0f, 0.138f, 0.04f), 0.013f, 0.004f, H("fbf1d6"), 0.001f, Q(90, 0, 0), 16); Block(rb, new Vector3(0.004f, 0.141f, 0.043f), new Vector3(0.002f, 0.011f, 0.002f), H("c8402e"), 0.0005f, false).transform.localRotation = Q(0, 0, -35);
                for (int mi = 0; mi < 4; mi++) Block(rb, new Vector3(-0.012f + mi * 0.008f, 0.184f, 0.039f), new Vector3(0.003f, 0.008f, 0.002f), H("9aa6b2"), 0.0005f, false);
                Block(rb, new Vector3(0, 0.115f, -0.036f), new Vector3(0.11f, 0.012f, 0.004f), H("fbf1d6"), 0.002f, false);
                foreach (int lx in new[] { -1, 1 }) for (int ti = 0; ti < 3; ti++) Block(rb, new Vector3(lx * 0.03f, 0.002f, -0.006f + ti * 0.012f), new Vector3(0.05f, 0.004f, 0.004f), H("2b3440"), 0.001f, false);
            }
        }
        MatA = om; return g;
    }

    // ---------------- loot container bodies (collider on the returned root, sized to Data.CK)
    public static GameObject ContainerBody(Transform p, string kind, Vector3 pos, float yaw, Color accent, string label = null)
    {
        var ck = Data.CK[kind]; var sz = ck.size; var g = Group(p, "Cont_" + kind, pos, yaw);
        switch (kind)
        {
            case "fridge":
                Block(g, new Vector3(0, 0.95f, 0), new Vector3(0.85f, 1.8f, 0.8f), Lt(Sky, 0.25f), 0.3f, false);
                Block(g, new Vector3(0, 1.35f, 0.4f), new Vector3(0.75f, 0.03f, 0.05f), White, 0.01f, false);
                foreach (float y in new[] { 1.6f, 0.75f }) Block(g, new Vector3(0.3f, y, 0.44f), new Vector3(0.06f, 0.3f, 0.06f), GreyD, 0.03f, false);
                Ball(g, new Vector3(-0.15f, 1.6f, 0.41f), new Vector3(0.12f, 0.12f, 0.03f), Coral, 8); Ball(g, new Vector3(0.0f, 1.0f, 0.41f), new Vector3(0.1f, 0.1f, 0.03f), ButterD, 8);
                foreach (int s in new[] { -1, 1 }) Cyl(g, new Vector3(s * 0.3f, 0.04f, 0.25f), 0.05f, 0.08f, GreyD); break;
            case "wardrobe":
                Block(g, new Vector3(0, 1.02f, 0), new Vector3(1.4f, 1.86f, 0.62f), accent, 0.28f, false, 0, -1, 0, Lt(accent, 0.2f));
                foreach (int s in new[] { -1, 1 }) foreach (float z in new[] { -0.2f, 0.2f }) Lath(g, new Vector3(s * 0.5f, 0f, z), new[] { new Vector2(0, 0), new Vector2(0.05f, 0), new Vector2(0.07f, 0.06f), new Vector2(0.09f, 0.1f), new Vector2(0.09f, 0.13f), new Vector2(0, 0.14f) }, WoodD, 10);   // v0.9.7 turned feet (were balls)
                var crown = Cyl(g, new Vector3(0, 1.95f, 0), 0.7f, 0.6f, accent, 0.08f, Q(90, 0, 0), 22); crown.transform.localScale = new Vector3(1, 1, 0.35f);
                foreach (int s in new[] { -1, 1 }) { Block(g, new Vector3(s * 0.34f, 1.0f, 0.31f), new Vector3(0.62f, 1.6f, 0.05f), Lt(accent, 0.15f), 0.08f, false); Block(g, new Vector3(s * 0.1f, 1.05f, 0.36f), new Vector3(0.035f, 0.3f, 0.035f), ButterD, 0.012f, false); foreach (float hy in new[] { 0.93f, 1.17f }) Block(g, new Vector3(s * 0.1f, hy, 0.34f), new Vector3(0.03f, 0.03f, 0.04f), ButterD, 0.008f, false); }   // v0.9.7 bar pulls (were ball knobs)
                break;
            case "drawer":
                Block(g, new Vector3(0, 0.47f, 0), new Vector3(0.9f, 0.8f, 0.54f), accent, 0.22f, false, 0, -1, 0, Lt(accent, 0.25f));
                foreach (int s in new[] { -1, 1 }) foreach (float z in new[] { -0.15f, 0.15f }) Ball(g, new Vector3(s * 0.32f, 0.06f, z), new Vector3(0.15f, 0.12f, 0.15f), WoodD, 8); // bun feet
                for (int i = 0; i < 3; i++) { Block(g, new Vector3(0, 0.24f + i * 0.23f, 0.27f), new Vector3(0.7f, 0.18f, 0.05f), Lt(accent, 0.2f), 0.08f, false); Ball(g, new Vector3(0, 0.24f + i * 0.23f, 0.31f), Vector3.one * 0.09f, i == 1 ? Coral : ButterD, 8); }
                break;
            case "box":
            {   // v0.9.9 r3 cardboard box: kraft body, open flaps, tape, edge seams, shipping label
                var kr = H("c9965a"); var kd = Dk(kr, 0.72f); var om2 = PushM(MatK.Fabric);
                Block(g, new Vector3(0, 0.31f, 0), new Vector3(0.82f, 0.6f, 0.82f), kr, 0.025f, false, 0, 0.004f);
                foreach (int sx in new[] { -1, 1 }) foreach (int sz2 in new[] { -1, 1 }) Block(g, new Vector3(sx * 0.411f, 0.31f, sz2 * 0.411f), new Vector3(0.02f, 0.6f, 0.02f), kd, 0.004f, false);
                foreach (float y in new[] { 0.015f, 0.605f }) { Block(g, new Vector3(0, y, 0.411f), new Vector3(0.82f, 0.018f, 0.02f), kd, 0.004f, false); Block(g, new Vector3(0, y, -0.411f), new Vector3(0.82f, 0.018f, 0.02f), kd, 0.004f, false); }
                foreach (int s in new[] { -1, 1 }) { var fl = Block(g, new Vector3(s * 0.41f, 0.61f, 0), new Vector3(0.4f, 0.015f, 0.8f), Lt(kr, 0.06f), 0.005f, false); fl.transform.localPosition = new Vector3(s * 0.5f, 0.68f, 0); fl.transform.localRotation = Q(0, 0, s * 35); }
                foreach (int s in new[] { -1, 1 }) { var fl = Block(g, new Vector3(0, 0.61f, s * 0.41f), new Vector3(0.8f, 0.015f, 0.36f), Dk(kr, 0.9f), 0.005f, false); fl.transform.localPosition = new Vector3(0, 0.66f, s * 0.47f); fl.transform.localRotation = Q(s * -28, 0, 0); }
                Block(g, new Vector3(0, 0.45f, 0.415f), new Vector3(0.16f, 0.32f, 0.008f), H("e9dcb0"), 0.003f, false);   // tape strip
                Picture(g, new Vector3(0.12f, 0.24f, 0.418f), Q(0, 180, 0), 0.4f, 0.27f, "art_lbl");   // v0.9.10 painted fragile label
                Ball(g, new Vector3(0.05f, 0.6f, 0.05f), new Vector3(0.22f, 0.16f, 0.22f), Coral, 10); Ball(g, new Vector3(-0.15f, 0.62f, -0.05f), new Vector3(0.18f, 0.14f, 0.18f), Butter, 10);   // goodies peeking out
                MatA = om2; break;
            }
            case "safe":
                Block(g, new Vector3(0, 0.3f, 0), new Vector3(0.58f, 0.58f, 0.58f), LavD, 0.12f, false);
                Ring(g, new Vector3(0, 0.32f, 0.3f), Quaternion.identity, 0.12f, 0.03f, ButterD, 18); Ball(g, new Vector3(0, 0.32f, 0.3f), new Vector3(0.1f, 0.1f, 0.05f), Butter, 8);
                for (int i = 0; i < 3; i++) Ball(g, new Vector3(-0.15f + i * 0.15f, 0.62f, 0), Vector3.one * 0.1f, new[] { PinkD, ButterD, MintD }[i], 8); break;
            case "shelf": case "display":
                Block(g, new Vector3(0, sz.y / 2, -sz.z / 2 + 0.04f), new Vector3(sz.x, sz.y, 0.08f), accent, 0.04f, false);
                foreach (int s in new[] { -1, 1 }) Block(g, new Vector3(s * (sz.x / 2 - 0.07f), sz.y / 2, 0), new Vector3(0.16f, sz.y, sz.z + 0.04f), accent, 0.075f, false);
                { var arc = Cyl(g, new Vector3(0, sz.y + 0.02f, 0), sz.x / 2, sz.z + 0.04f, accent, 0.07f, Q(90, 0, 0), 22); arc.transform.localScale = new Vector3(1, 1, 0.75f); Ball(g, new Vector3(0, sz.y + 0.1f, sz.z / 2 + 0.05f), new Vector3(0.2f, 0.2f, 0.06f), ButterD, 10); } // arched crown
                int nsh = kind == "shelf" ? 4 : 3;
                for (int i = 0; i < nsh; i++)
                {
                    float y = 0.1f + i * (sz.y - 0.15f) / (nsh - 0.6f); Block(g, new Vector3(0, y, 0), new Vector3(sz.x - 0.04f, 0.06f, sz.z), Lt(accent, 0.25f), 0.025f, false);
                    float gap = (sz.y - 0.15f) / (nsh - 0.6f) - 0.1f; if (i == nsh - 1) gap = Mathf.Min(gap, sz.y - y - 0.06f);   // v0.9.5: goods matching the label, standing on the board, clear of the board above
                    Goods.ShelfRow(g, label ?? (kind == "display" ? "收藏" : "雜物"), new Vector3(0, y + 0.03f, 0.02f), sz.x - 0.4f, Mathf.Min(gap, 0.42f), i, Mathf.RoundToInt(pos.x * 17 + pos.z * 5) + i);
                }
                break;
            case "cooler":   // v0.9.5: open-front shop cooler - shell, wire racks, real drink bottles visible from the front
                Block(g, new Vector3(0, 0.93f, -0.2f), new Vector3(0.8f, 1.85f, 0.4f), Lt(SkyD, 0.1f), 0.12f, false);
                foreach (int s in new[] { -1, 1 }) Block(g, new Vector3(s * 0.37f, 0.93f, 0.2f), new Vector3(0.06f, 1.85f, 0.42f), Lt(SkyD, 0.1f), 0.02f, false);
                Block(g, new Vector3(0, 1.78f, 0.2f), new Vector3(0.8f, 0.14f, 0.42f), Lt(SkyD, 0.1f), 0.03f, false);
                Block(g, new Vector3(0, 0.15f, 0.2f), new Vector3(0.8f, 0.3f, 0.42f), Dk(SkyD, 0.9f), 0.03f, false);
                for (int i = 0; i < 4; i++) { Block(g, new Vector3(0, 0.32f + i * 0.34f, 0.18f), new Vector3(0.66f, 0.02f, 0.38f), Lt(Grey, 0.4f), 0.005f, false); for (int k = 0; k < 3; k++) Goods.DrinkBottle(g, new Vector3(-0.2f + k * 0.2f, 0.33f + i * 0.34f, 0.2f), 0.27f, new[] { Coral, MintD, ButterD, PinkD }[(i + k) % 4]); }
                Block(g, new Vector3(0, 1.78f, 0.42f), new Vector3(0.6f, 0.1f, 0.02f), Coral, 0.02f, false); break;
            case "register":
                Block(g, new Vector3(0, 0.13f, 0), new Vector3(0.52f, 0.26f, 0.42f), Coral, 0.07f, false); Block(g, new Vector3(0, 0.3f, -0.1f), new Vector3(0.36f, 0.18f, 0.1f), Butter, 0.04f, false).transform.localRotation = Q(-25, 0, 0);
                for (int i = 0; i < 6; i++) Ball(g, new Vector3(-0.12f + (i % 3) * 0.12f, 0.28f, 0.03f + (i / 3) * 0.1f), new Vector3(0.07f, 0.03f, 0.07f), White, 6); break;
            case "toolchest":
            {   // v0.9.10: rolling metal tool chest - drawers, pulls, lid handle, casters, rivets
                var om3 = PushM(MatK.Metal); var tcC = H("c8443a"); var tcD = Dk(tcC, 0.75f); var st = H("b9c2c9");
                Block(g, new Vector3(0, 0.36f, 0), new Vector3(0.66f, 0.56f, 0.4f), tcC, 0.03f, false);
                Block(g, new Vector3(0, 0.66f, 0), new Vector3(0.69f, 0.06f, 0.42f), tcD, 0.02f, false);
                for (int i = 0; i < 4; i++) { float y = 0.17f + i * 0.13f; Block(g, new Vector3(0, y, 0.2f), new Vector3(0.6f, 0.11f, 0.02f), Lt(tcC, 0.08f), 0.01f, false); Block(g, new Vector3(0, y + 0.02f, 0.215f), new Vector3(0.3f, 0.018f, 0.02f), st, 0.006f, false); Block(g, new Vector3(0.22f, y, 0.212f), new Vector3(0.07f, 0.04f, 0.006f), White, 0.003f, false); }
                foreach (int s in new[] { -1, 1 }) { Cyl(g, new Vector3(s * 0.36f, 0.5f, 0), 0.012f, 0.2f, st, 0.005f, Q(90, 0, 0), 8); foreach (int z in new[] { -1, 1 }) { Cyl(g, new Vector3(s * 0.25f, 0.05f, z * 0.15f), 0.04f, 0.035f, H("333a40"), 0.01f, Q(0, 0, 90), 10); Block(g, new Vector3(s * 0.25f, 0.085f, z * 0.15f), new Vector3(0.05f, 0.02f, 0.05f), st, 0.005f, false); } }
                for (int i = 0; i < 4; i++) Ball(g, new Vector3(-0.3f + (i % 2) * 0.6f, i < 2 ? 0.62f : 0.1f, 0.205f), Vector3.one * 0.015f, st, 6);
                Block(g, new Vector3(-0.1f, 0.71f, 0.05f), new Vector3(0.22f, 0.03f, 0.08f), H("e8b84a"), 0.01f, false);   // wrench on top
                MatA = om3; break;
            }
            default:
                Block(g, new Vector3(0, sz.y / 2, 0), sz, accent, Mathf.Min(0.12f, sz.y * 0.3f), false); break;
        }
        var bc = g.gameObject.AddComponent<BoxCollider>(); bc.size = sz; bc.center = new Vector3(0, sz.y / 2, 0);
        return g.gameObject;
    }
}

/// v0.9.11 gentle clock pendulum swing
public class Swing : MonoBehaviour { float ph; void Start() { ph = transform.position.x; } void Update() { transform.localRotation = Quaternion.Euler(0, 0, 9f * Mathf.Sin(Time.time * 3.1f + ph)); } }
