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
        Cyl(g, new Vector3(0, 1.1f * s, 0), 0.17f * s, 2.2f * s, WoodD, 0.06f);
        if (kind == 0) { Ball(g, new Vector3(0, 2.9f * s, 0), new Vector3(2.4f, 2.3f, 2.4f) * s, cr); Ball(g, new Vector3(0.45f, 3.3f, -0.5f) * s, Vector3.one * 0.5f * s, Lt(cr)); }
        else if (kind == 1) { Ball(g, new Vector3(0, 2.7f, 0) * s, new Vector3(2.1f, 1.7f, 2.1f) * s, cr); Ball(g, new Vector3(0.8f, 2.4f, 0.2f) * s, new Vector3(1.5f, 1.3f, 1.5f) * s, cr); Ball(g, new Vector3(-0.7f, 2.5f, -0.3f) * s, new Vector3(1.5f, 1.3f, 1.5f) * s, cr); Ball(g, new Vector3(0.1f, 3.4f, 0.1f) * s, new Vector3(1.4f, 1.2f, 1.4f) * s, Lt(cr, 0.2f)); }
        else { for (int i = 0; i < 3; i++) Lath(g, new Vector3(0, (2.0f + i * 0.9f) * s, 0), new[] { new Vector2(0, 0), new Vector2(1.3f - i * 0.3f, 0.05f) * s, new Vector2(1.1f - i * 0.28f, 0.35f * s), new Vector2(0.15f, 1.2f * s), new Vector2(0, 1.25f * s) }, LeafD); }
        var cc = g.gameObject.AddComponent<CapsuleCollider>(); cc.radius = 0.25f * s; cc.height = 3f * s; cc.center = new Vector3(0, 1.5f * s, 0);
        return g;
    }
    public static Transform Bush(Transform p, Vector3 pos, float s, Color c, Color? flower = null, System.Random rng = null)
    {
        var g = Group(p, "sk_bush", pos); rng = rng ?? new System.Random((int)(pos.x * 13 + pos.z * 7));
        int n = 3; for (int i = 0; i < n; i++) { float a = i * 2.1f; Ball(g, new Vector3(Mathf.Cos(a) * 0.3f * s, 0.35f * s, Mathf.Sin(a) * 0.3f * s), new Vector3(0.8f, 0.7f, 0.8f) * s, i == 2 ? Lt(c, 0.15f) : c); }
        if (flower.HasValue) for (int i = 0; i < 7; i++) { float a = (float)rng.NextDouble() * 6.28f, r = 0.35f * s; Ball(g, new Vector3(Mathf.Cos(a) * r, (0.45f + (float)rng.NextDouble() * 0.3f) * s, Mathf.Sin(a) * r), Vector3.one * 0.14f * s, flower.Value, 8); }
        var bc = g.gameObject.AddComponent<BoxCollider>(); bc.size = new Vector3(1.1f, 0.8f, 1.1f) * s; bc.center = new Vector3(0, 0.4f * s, 0);
        return g;
    }
    public static Transform FlowerPot(Transform p, Vector3 pos, Color pot, Color flower, float s = 1f, bool col = true)
    {
        var g = Group(p, "sk_pot", pos);
        Lath(g, Vector3.zero, new[] { new Vector2(0, 0), new Vector2(0.2f, 0), new Vector2(0.27f, 0.36f), new Vector2(0.3f, 0.4f), new Vector2(0.24f, 0.42f), new Vector2(0, 0.42f) }.Scale(s), pot, 16);
        Ball(g, new Vector3(0, 0.5f, 0) * s, new Vector3(0.5f, 0.32f, 0.5f) * s, LeafD, 12);
        for (int i = 0; i < 5; i++) { float a = i * 1.26f; Ball(g, new Vector3(Mathf.Cos(a) * 0.15f, 0.62f + (i % 2) * 0.05f, Mathf.Sin(a) * 0.15f) * s, Vector3.one * 0.12f * s, flower, 8); }
        if (col) { var bc = g.gameObject.AddComponent<BoxCollider>(); bc.size = new Vector3(0.6f, 0.7f, 0.6f) * s; bc.center = new Vector3(0, 0.35f * s, 0); }
        return g;
    }
    static Vector2[] Scale(this Vector2[] a, float s) { for (int i = 0; i < a.Length; i++) a[i] *= s; return a; }
    public static void Hill(Transform p, Vector3 pos, Vector3 size, Color c) => Ball(p, pos, size, c, 24, false, null, "sk_hill", 0.03f);
    public static void Cloud(Transform p, Vector3 pos, float s)
    {
        var g = Group(p, "sk_cloud", pos); var c = White;
        Ball(g, Vector3.zero, new Vector3(5, 2.6f, 3) * s, c); Ball(g, new Vector3(2.4f, -0.3f, 0) * s, new Vector3(3.5f, 2, 2.6f) * s, c); Ball(g, new Vector3(-2.3f, -0.4f, 0.2f) * s, new Vector3(3.2f, 1.8f, 2.4f) * s, Lt(Pink, 0.6f)); Ball(g, new Vector3(0.6f, 1.1f, 0) * s, new Vector3(2.8f, 2.2f, 2.2f) * s, c);
    }

    // ---------------- street
    public static Transform StreetLamp(Transform p, Vector3 pos, float yaw)
    {
        var g = Group(p, "sk_lamp", pos, yaw);
        Ball(g, new Vector3(0, 0.2f, 0), new Vector3(0.75f, 0.45f, 0.75f), MintD, 16); Cyl(g, new Vector3(0, 1.5f, 0), 0.16f, 2.8f, Mint, 0.08f);
        Ball(g, new Vector3(0, 2.95f, 0), Vector3.one * 0.38f, MintD, 12);
        Ball(g, new Vector3(0, 3.45f, 0), Vector3.one * 0.85f, Data.H("fff3c4"), 16);
        Lath(g, new Vector3(0, 3.75f, 0), new[] { new Vector2(0, 0), new Vector2(0.62f, 0), new Vector2(0.5f, 0.18f), new Vector2(0.15f, 0.36f), new Vector2(0, 0.42f) }, Rose, 18);
        var b = Art.Prim(PrimitiveType.Sphere, g, new Vector3(0, 3.42f, 0), Vector3.one * 0.55f, Art.Emissive(Data.H("ffe9a8"), 1.6f));
        var bc = g.gameObject.AddComponent<BoxCollider>(); bc.size = new Vector3(0.4f, 3.6f, 0.4f); bc.center = new Vector3(0, 1.8f, 0);
        return g;
    }
    public static Transform Scooter(Transform p, Vector3 pos, float yaw, Color c)
    {
        var g = Group(p, "sk_scooter", pos, yaw); g.localScale = new Vector3(1.25f, 1.12f, 1.12f); var dark = Plum; var hub = Grey;
        foreach (float z in new[] { -0.62f, 0.62f }) { Cyl(g, new Vector3(0, 0.3f, z), 0.3f, 0.16f, Dk(dark, 1.1f), 0.07f, Q(0, 0, 90), 18); Cyl(g, new Vector3(0, 0.3f, z), 0.13f, 0.2f, hub, 0.04f, Q(0, 0, 90), 12); }
        Block(g, new Vector3(0, 0.42f, 0.02f), new Vector3(0.36f, 0.16f, 0.9f), c, 0.07f, false);        // floor board
        Block(g, new Vector3(0, 0.62f, -0.48f), new Vector3(0.5f, 0.5f, 0.75f), c, 0.22f, false, 0, -1, 0.15f); // rear body
        Block(g, new Vector3(0, 0.92f, -0.42f), new Vector3(0.38f, 0.14f, 0.66f), dark, 0.07f, false);   // seat
        Block(g, new Vector3(0, 0.82f, 0.5f), new Vector3(0.44f, 0.95f, 0.2f), c, 0.1f, false, 0, -1, 0.25f); // leg shield
        Block(g, new Vector3(0, 0.5f, 0.66f), new Vector3(0.3f, 0.22f, 0.45f), c, 0.1f, false);          // front fender
        Cyl(g, new Vector3(0, 1.3f, 0.52f), 0.04f, 0.35f, Grey);
        Cyl(g, new Vector3(0, 1.45f, 0.5f), 0.04f, 0.72f, Grey, 0.02f, Q(0, 0, 90), 10);
        Block(g, new Vector3(0, 1.38f, 0.6f), new Vector3(0.3f, 0.2f, 0.16f), c, 0.07f, false);
        Ball(g, new Vector3(0, 1.38f, 0.69f), new Vector3(0.14f, 0.14f, 0.06f), Butter, 10);
        foreach (int s in new[] { -1, 1 }) Ball(g, new Vector3(s * 0.38f, 1.45f, 0.5f), new Vector3(0.09f, 0.09f, 0.12f), dark, 8);
        var bc = g.gameObject.AddComponent<BoxCollider>(); bc.size = new Vector3(0.7f, 1.3f, 1.8f); bc.center = new Vector3(0, 0.65f, 0);
        return g;
    }
    public static Transform Bench(Transform p, Vector3 pos, float yaw, Color seat, Color leg)
    {
        var g = Group(p, "sk_bench", pos, yaw);
        Block(g, new Vector3(0, 0.45f, 0), new Vector3(1.8f, 0.14f, 0.5f), seat, 0.06f, false);
        Block(g, new Vector3(0, 0.82f, -0.22f), new Vector3(1.8f, 0.4f, 0.1f), seat, 0.05f, false);
        foreach (int s in new[] { -1, 1 }) Block(g, new Vector3(s * 0.75f, 0.2f, 0), new Vector3(0.14f, 0.4f, 0.44f), leg, 0.05f, false);
        var bc = g.gameObject.AddComponent<BoxCollider>(); bc.size = new Vector3(1.8f, 0.6f, 0.5f); bc.center = new Vector3(0, 0.3f, 0);
        return g;
    }
    public static Transform Mailbox(Transform p, Vector3 pos, float yaw)
    {
        var g = Group(p, "sk_mailbox", pos, yaw);
        Cyl(g, new Vector3(0, 0.5f, 0), 0.07f, 1f, GreyD); Block(g, new Vector3(0, 1.15f, 0), new Vector3(0.5f, 0.4f, 0.35f), Coral, 0.15f, false);
        Block(g, new Vector3(0, 1.2f, 0.18f), new Vector3(0.3f, 0.05f, 0.02f), Plum, 0.01f, false);
        var bc = g.gameObject.AddComponent<BoxCollider>(); bc.size = new Vector3(0.5f, 1.4f, 0.4f); bc.center = new Vector3(0, 0.7f, 0); return g;
    }
    public static Transform TrashBin(Transform p, Vector3 pos, Color c)
    {
        var g = Group(p, "sk_bin", pos);
        Lath(g, Vector3.zero, new[] { new Vector2(0, 0), new Vector2(0.28f, 0), new Vector2(0.33f, 0.8f), new Vector2(0.36f, 0.86f), new Vector2(0, 0.9f) }, c, 18, true);
        Lath(g, new Vector3(0, 0.9f, 0), new[] { new Vector2(0, -0.02f), new Vector2(0.38f, 0), new Vector2(0.3f, 0.12f), new Vector2(0, 0.16f) }, Dk(c, 0.85f), 18);
        return g;
    }
    public static Transform Cone(Transform p, Vector3 pos)
    {
        var g = Group(p, "sk_cone", pos);
        Lath(g, Vector3.zero, new[] { new Vector2(0, 0), new Vector2(0.3f, 0), new Vector2(0.3f, 0.06f), new Vector2(0.2f, 0.08f), new Vector2(0.06f, 0.65f), new Vector2(0, 0.68f) }, Coral, 14, true,
            null, "sk_cone", new[] { Coral, Coral, Coral, White, Coral, Coral });
        return g;
    }
    public static Transform Hydrant(Transform p, Vector3 pos)
    {
        var g = Group(p, "sk_hydrant", pos); Cyl(g, new Vector3(0, 0.3f, 0), 0.16f, 0.6f, Coral); Ball(g, new Vector3(0, 0.62f, 0), Vector3.one * 0.3f, Coral); Cyl(g, new Vector3(0, 0.38f, 0), 0.07f, 0.45f, Coral, 0.03f, Q(0, 0, 90), 10);
        var bc = g.gameObject.AddComponent<BoxCollider>(); bc.size = new Vector3(0.4f, 0.8f, 0.4f); bc.center = new Vector3(0, 0.4f, 0); return g;
    }
    public static Transform WaterJar(Transform p, Vector3 pos, Color c)
    {
        var g = Group(p, "sk_jar", pos);
        Lath(g, Vector3.zero, new[] { new Vector2(0, 0), new Vector2(0.25f, 0), new Vector2(0.38f, 0.3f), new Vector2(0.32f, 0.62f), new Vector2(0.24f, 0.68f), new Vector2(0.27f, 0.72f), new Vector2(0, 0.72f) }, c, 18, true);
        return g;
    }

    // ---------------- building parts (all facing local -Z = outward from a facade at z=0)
    /// 鐵窗花 iron flower grille: frame + bars + flower motifs, in the XY plane at local z.
    public static Transform Grille(Transform p, Vector3 center, float w, float h, Color c, bool bulge = true)
    {
        var g = Group(p, "sk_grille", center);
        float t = 0.2f; float d = bulge ? -0.14f : 0;
        Block(g, new Vector3(0, h / 2, d), new Vector3(w, t, t), c, 0.02f, false); Block(g, new Vector3(0, -h / 2, d), new Vector3(w, t, t), c, 0.02f, false);
        Block(g, new Vector3(-w / 2, 0, d), new Vector3(t, h, t), c, 0.02f, false); Block(g, new Vector3(w / 2, 0, d), new Vector3(t, h, t), c, 0.02f, false);
        int nx = Mathf.Max(1, Mathf.RoundToInt(w / 1.2f)), ny = Mathf.Max(1, Mathf.RoundToInt(h / 1.2f)); float cx = w / nx, cy = h / ny;
        for (int i = 1; i < nx; i++) Block(g, new Vector3(-w / 2 + i * cx, 0, d), new Vector3(0.16f, h, 0.16f), c, 0.08f, false);
        for (int j = 1; j < ny; j++) Block(g, new Vector3(0, -h / 2 + j * cy, d), new Vector3(w, 0.16f, 0.16f), c, 0.08f, false);
        float pr = Mathf.Min(cx, cy) * 0.17f;
        for (int i = 0; i < nx; i++) for (int j = 0; j < ny; j++)
        {
            var m = new Vector3(-w / 2 + (i + 0.5f) * cx, -h / 2 + (j + 0.5f) * cy, d);
            // chunky toy flower: 4 fat petals + butter centre (moulded-plastic, not ironwork)
            // clean: single fat ring per cell (front layer)
        }
        var om = PushM(0.5f);
        for (int i = 0; i < nx; i++) for (int j = 0; j < ny; j++)
        {   // front layer: fat scroll rings + corner knobs (layered 3D ironwork)
            var m = new Vector3(-w / 2 + (i + 0.5f) * cx, -h / 2 + (j + 0.5f) * cy, d - 0.2f); float rr = Mathf.Min(cx, cy) * 0.36f;
            Ring(g, m, Quaternion.identity, rr, 0.12f, c, 22);
        }
        MatA = om;
        if (bulge) foreach (int sx in new[] { -1, 1 }) foreach (int sy in new[] { -1, 1 }) Block(g, new Vector3(sx * w / 2, sy * h / 2, d / 2), new Vector3(0.04f, 0.04f, Mathf.Abs(d)), c, 0.015f, false);
        return g;
    }
    /// round window (quatrefoil if petals) in the XY plane; glass sits at z=0, frame proud toward -Z
    public static Transform RoundWin(Transform p, Vector3 center, float R, Color frame, bool petals)
    {
        var g = Group(p, "sk_roundwin", center); var om = PushM(0.5f);
        Cyl(g, new Vector3(0, 0, 0.02f), R, 0.06f, Sky, 0.02f, Q(90, 0, 0), 24);
        Ring(g, new Vector3(0, 0, -0.04f), Quaternion.identity, R, Mathf.Max(0.12f, R * 0.2f), frame, 32);
        if (petals) { for (int k = 0; k < 4; k++) { float a = k * Mathf.PI / 2; Ring(g, new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0) * R * 0.36f + new Vector3(0, 0, -0.16f), Quaternion.identity, R * 0.34f, Mathf.Max(0.07f, R * 0.09f), frame, 20); Ball(g, new Vector3(0, 0, -0.2f), Vector3.one * R * 0.28f, frame, 12); } }
        else Ball(g, new Vector3(-R * 0.3f, R * 0.3f, -0.03f), new Vector3(R * 0.35f, R * 0.18f, 0.03f), White, 10, false, Q(0, 0, -40));
        Ring(g, new Vector3(0, 0, -0.12f), Quaternion.identity, R + 0.12f, Mathf.Max(0.1f, R * 0.12f), Lt(frame, 0.3f), 32); MatA = om;
        return g;
    }
    /// rectangular window with optional arched top + sill + flower box; glass at z=0 facing -Z
    public static Transform Win(Transform p, Vector3 center, float w, float h, Color frame, bool arch, Color? flowers = null, bool grille = false)
    {
        var g = Group(p, "sk_win", center); var om = PushM(0.5f);
        float wr = Mathf.Min(w, h) * 0.32f;
        Block(g, new Vector3(0, 0, 0.04f), new Vector3(w + 0.44f, h + 0.44f, 0.2f), frame, wr + 0.2f, false, 0, 0.01f);
        Block(g, new Vector3(0, 0, -0.04f), new Vector3(w, h, 0.12f), Sky, wr, false, 0, 0.005f, 0, null, "glass");
        Ball(g, new Vector3(-w * 0.22f, h * 0.22f, -0.1f), new Vector3(w * 0.18f, h * 0.1f, 0.03f), White, 10, false, Q(0, 0, -30));
        Block(g, new Vector3(0, -h / 2 - 0.2f, -0.12f), new Vector3(w + 0.6f, 0.26f, 0.36f), frame, 0.13f, false);
        if (arch) { Cyl(g, new Vector3(0, h / 2, 0.03f), w / 2, 0.06f, Sky, 0.02f, Q(90, 0, 0), 20); var r = Ring(g, new Vector3(0, h / 2, -0.04f), Quaternion.identity, w / 2 + 0.02f, 0.07f, frame, 24); }
        if (flowers.HasValue) { Block(g, new Vector3(0, -h / 2 - 0.3f, -0.36f), new Vector3(w + 0.2f, 0.4f, 0.42f), WoodD, 0.18f, false); for (int i = 0; i < 4; i++) Ball(g, new Vector3(-w / 2 + 0.25f + i * (w - 0.5f) / 3, -h / 2 + 0.02f, -0.38f), Vector3.one * 0.38f, i % 2 == 0 ? flowers.Value : LeafD, 10); }
        if (grille) Grille(g, new Vector3(0, 0, -0.06f), w + 0.1f, h + 0.1f, White);
        MatA = om; return g;
    }
    public static Transform Awning(Transform p, Vector3 center, float w, float depth, Color a, Color b)
    {
        var om = PushM(0.1f); var g = Group(p, "sk_awning", center); int n = Mathf.Max(3, Mathf.RoundToInt(w / 0.9f)); float sw = w / n;
        for (int i = 0; i < n; i++)
        {
            var s = Block(g, new Vector3(-w / 2 + (i + 0.5f) * sw, 0, -depth / 2), new Vector3(sw + 0.01f, 0.2f, depth), i % 2 == 0 ? a : b, 0.09f, false, 0, 0.01f);
            s.transform.localRotation = Q(-22, 0, 0);
            Ball(g, new Vector3(-w / 2 + (i + 0.5f) * sw, -depth * 0.37f - 0.08f, -depth * 0.93f), new Vector3(sw, 0.42f, 0.24f), i % 2 == 0 ? a : b, 12);
        }
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
        Ring(g, new Vector3(0.18f, 0, -0.2f), Quaternion.identity, 0.2f, 0.03f, GreyD, 18); Ball(g, new Vector3(0.18f, 0, -0.19f), new Vector3(0.12f, 0.12f, 0.04f), GreyD, 8); return g;
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
        var g = Group(p, "sk_doorvis", pos); var om = PushM(0.5f);
        Block(g, new Vector3(0, (h - w / 2) / 2, 0), new Vector3(w, h - w / 2, 0.12f), c, 0.05f, false);
        Cyl(g, new Vector3(0, h - w / 2, 0), w / 2, 0.12f, c, 0.04f, Q(90, 0, 0), 20);
        Ring(g, new Vector3(0, h - w / 2, -0.06f), Quaternion.identity, w / 2 + 0.06f, 0.09f, frame, 24);
        foreach (int s in new[] { -1, 1 }) Block(g, new Vector3(s * (w / 2 + 0.06f), (h - w / 2) / 2, -0.06f), new Vector3(0.18f, h - w / 2, 0.16f), frame, 0.06f, false);
        RoundWin(g, new Vector3(0, h * 0.62f, -0.07f), w * 0.17f, frame, true);
        Ball(g, new Vector3(w * 0.32f, h * 0.42f, -0.1f), Vector3.one * 0.22f, ButterD, 10);
        MatA = om; return g;
    }

    /// 透天厝 townhouse (solid, not enterable). Front at local z=0 facing -Z, body extends +Z.
    public static Transform TownHouse(Transform p, Vector3 pos, float yaw, float w, float d, int floors, Color wall, Color trim, Color roof, System.Random rng, string shop = null)
    {
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
            for (int i = 0; i < 9; i++) Ball(fr, new Vector3(-w * 0.3f + (i % 5) * w * 0.15f, 0.75f + (i / 5) * 0.6f, -0.22f), new Vector3(0.22f, 0.26f, 0.22f), new[] { PinkD, ButterD, MintD, Coral, LavD }[i % 5], 10);
            Awning(fr, new Vector3(0, 2.5f, -0.05f), w * 0.85f, 1.1f, acc, White);
            Text3D(fr, new Vector3(0, 3.0f, -1.0f), shop ?? new[] { "早餐", "冰店", "書店", "花店" }[rng.Next(4)], 0.16f, new[] { Rose, Coral, LavD, MintD }[rng.Next(4)]);
        }
        else if (gt == 1)
        {   // rolling shutter + side door
            Shutter(fr, new Vector3(-w * 0.15f, 1.15f, -0.05f), w * 0.5f, 2.1f, Lt(acc, 0.4f));
            DoorVisual(fr, new Vector3(w * 0.3f, 0, -0.07f), 1.6f, 2.95f, acc, trim);
        }
        else
        {   // arched door + window with grille
            DoorVisual(fr, new Vector3(-w * 0.2f, 0, -0.07f), 1.8f, 3.0f, acc, trim);
            Win(fr, new Vector3(w * 0.2f, 1.55f, -0.05f), 2.0f, 1.8f, trim, true, PinkD, true);
        }
        // upper floors
        for (int f = 1; f < floors; f++)
        {
            float y = gh + (f - 1) * fh + fh * 0.5f;
            if (f == 1 && rng.Next(2) == 0)
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
                else Win(fr, new Vector3(0, y, -0.05f), Mathf.Min(2.8f, w * 0.48f), 1.75f, trim, f % 2 == 0, rng.Next(2) == 0 ? (Color?)new[] { PinkD, ButterD, LavD }[rng.Next(3)] : null);
                Block(fr, new Vector3(0, y - 1.05f, -0.2f), new Vector3(Mathf.Min(2.4f, w * 0.42f), 0.16f, 0.42f), White, 0.07f, false);   // window sill
            }
        }
        Craft(g, fr, pos, w, d, H, floors, gh, fh, wall, trim, acc, gt);
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
            float mx = dx + (dx > 0 ? -1.45f : 1.45f);
            Block(fr, new Vector3(mx, 1.3f, -0.2f), new Vector3(0.5f, 0.6f, 0.3f), new[] { Coral, MintD, Sky }[r.Next(3)], 0.08f, false);   // mailbox
            Block(fr, new Vector3(mx, 1.45f, -0.36f), new Vector3(0.32f, 0.05f, 0.03f), Dk(wall, 0.45f), 0.01f, false);
            var pl = Block(fr, new Vector3(mx, 2.15f, -0.1f), new Vector3(0.62f, 0.4f, 0.1f), Butter, 0.12f, false);   // house-number plaque
            Text(fr, new Vector3(mx, 2.15f, -0.17f), Quaternion.identity, (r.Next(2, 98)).ToString(), 0.045f, Dk(trim, 0.55f));
        }
        // wall lamp next to the entrance
        float lx = gt == 0 ? w * 0.42f : dx + (dx > 0 ? 1.4f : -1.4f);
        Block(fr, new Vector3(lx, 2.6f, -0.1f), new Vector3(0.1f, 0.1f, 0.3f), GreyD, 0.03f, false);
        var om3 = PushM(0.5f); Ball(fr, new Vector3(lx, 2.52f, -0.32f), new Vector3(0.26f, 0.32f, 0.26f), Lt(ButterD, 0.4f), 12); MatA = om3;
        // planters hugging the base
        foreach (int s in new[] { -1, 1 }) if (r.Next(3) > 0) { Block(fr, new Vector3(s * (w / 2 - 0.65f), 0.32f, -0.45f), new Vector3(0.8f, 0.45f, 0.55f), Lt(trim, 0.3f), 0.12f, false); Ball(fr, new Vector3(s * (w / 2 - 0.65f), 0.7f, -0.45f), new Vector3(0.75f, 0.5f, 0.5f), new[] { MintD, Mint }[r.Next(2)], 12); }
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
        // rooftop rim rail + antenna
        if (r.Next(2) == 0)
        {
            Cyl(g, new Vector3(w * 0.3f, H + 0.9f, d * 0.7f), 0.03f, 1.8f, GreyD, 0.01f, null, 6);
            Cyl(g, new Vector3(w * 0.3f, H + 1.5f, d * 0.7f), 0.02f, 0.9f, GreyD, 0.01f, Q(0, 0, 90), 6);
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
    /// chunky extruded 3D letters on a puffy pill board. Board height fixed by `size`; glyphs fill ~68% of it,
    /// names longer than 4 chars wrap to two lines; text colour forced to >= 4.5:1 contrast with the board.
    public static Transform Text3D(Transform p, Vector3 pos, string s, float size, Color c)
    {
        var g = Group(p, "sk_text3d", pos); var om = PushM(0.5f);
        Color board = new Color(1f, 0.98f, 0.94f);
        int n = s.Length, lines = n > 4 ? 2 : 1; int per = (n + lines - 1) / lines;
        string txt = lines == 2 ? s.Substring(0, per) + "\n" + s.Substring(per) : s;
        float Hb = size * 9f * (lines == 2 ? 1.35f : 1f);
        float em = Hb * 0.56f / 0.92f / lines / (lines == 2 ? 0.95f : 1f);   // CJK ink ~0.92em
        float wd = per * em + Hb * 0.5f;
        Block(g, new Vector3(0, 0, 0.32f), new Vector3(wd, Hb, 0.3f), board, Mathf.Min(Hb * 0.47f, 0.14f), false);
        MatA = om;
        var face = Readable(c, board, 6f); float cr = Contrast(face, board); SignCount++;
        if (cr < 4.5f) { SignFails++; Debug.Log("[contrast] FAIL sign '" + s + "' ratio " + cr.ToString("0.00")); }
        float cs = em / 8f;
        for (int k = 4; k >= 0; k--) { var t = Text(g, new Vector3(0, 0, 0.12f - k * 0.03f), Quaternion.identity, txt, cs, k == 0 ? face : (k == 1 ? Lt(face, 0.35f) : Dk(face, 0.7f))); t.lineSpacing = 0.95f; t.GetComponent<MeshRenderer>().sortingOrder = 20 - k * 2; }
        return g;
    }

    // ---------------- furniture (front faces +Z)
    public static Transform Sofa(Transform p, Vector3 pos, float yaw, Color c, Color cushion)
    {
        var g = Group(p, "sk_sofa", pos, yaw);
        Block(g, new Vector3(0, 0.25f, 0), new Vector3(2.2f, 0.42f, 0.95f), Dk(c, 0.92f), 0.18f, false);
        Block(g, new Vector3(0, 0.75f, -0.35f), new Vector3(2.2f, 0.8f, 0.32f), c, 0.16f, false);
        foreach (int s in new[] { -1, 1 }) Block(g, new Vector3(s * 1.0f, 0.55f, 0), new Vector3(0.32f, 0.5f, 0.95f), c, 0.15f, false);
        foreach (int s in new[] { -1, 1 }) Block(g, new Vector3(s * 0.43f, 0.53f, 0.08f), new Vector3(0.82f, 0.2f, 0.72f), Lt(c, 0.2f), 0.09f, false);
        Block(g, new Vector3(-0.55f, 0.82f, -0.12f), new Vector3(0.5f, 0.42f, 0.18f), cushion, 0.12f, false).transform.localRotation = Q(-10, 8, 6);
        Block(g, new Vector3(0.55f, 0.82f, -0.12f), new Vector3(0.48f, 0.42f, 0.18f), Butter, 0.12f, false).transform.localRotation = Q(-10, -8, -5);
        var bc = g.gameObject.AddComponent<BoxCollider>(); bc.size = new Vector3(2.2f, 1.1f, 0.95f); bc.center = new Vector3(0, 0.55f, 0); return g;
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
    public static Transform Rug(Transform p, Vector3 pos, float R, Color a, Color b)
    {
        var g = Group(p, "sk_rug", pos);
        var prof = new List<Vector2> { new Vector2(R + 0.04f, 0f) }; var cols = new List<Color> { b };
        for (int i = 6; i >= 0; i--) { prof.Add(new Vector2(R * i / 6f, 0.035f)); cols.Add(i == 6 ? b : (i % 2 == 0 ? a : Color.Lerp(a, b, 0.5f))); }
        Obj(g, "sk_rugtop", Lathe(prof, a, 36, 0, 3, cols), Vector3.zero, Quaternion.identity, 0.006f);
        return g;
    }
    public static Transform TVStand(Transform p, Vector3 pos, float yaw)
    {
        var g = Group(p, "sk_tv", pos, yaw);
        Block(g, new Vector3(0, 0.95f, -0.05f), new Vector3(1.25f, 0.85f, 0.3f), Plum, 0.14f, false);
        Block(g, new Vector3(0, 0.98f, 0.11f), new Vector3(1.05f, 0.66f, 0.04f), Dk(SkyD, 0.7f), 0.06f, false);
        foreach (int s in new[] { -1, 1 }) Cyl(g, new Vector3(s * 0.2f, 1.55f, -0.05f), 0.02f, 0.5f, GreyD, 0.01f, Q(0, 0, s * 25), 8);
        Ball(g, new Vector3(0.48f, 0.66f, 0.12f), Vector3.one * 0.08f, Coral, 8);
        return g;
    }
    public static Transform Bed(Transform p, Vector3 pos, float yaw, float w, Color frame, Color quilt, Color star, bool stars = true)
    {
        var g = Group(p, "sk_bed", pos, yaw); float L = 2.1f;
        Block(g, new Vector3(0, 0.22f, 0), new Vector3(w, 0.3f, L), frame, 0.1f, false);
        Block(g, new Vector3(0, 0.5f, 0.05f), new Vector3(w - 0.1f, 0.28f, L - 0.15f), White, 0.12f, false);
        Block(g, new Vector3(0, 0.66f, 0.25f), new Vector3(w + 0.06f, 0.14f, L * 0.7f), quilt, 0.07f, false, 0, 0.02f);
        Block(g, new Vector3(0, 0.66f, -L / 2 + 0.38f), new Vector3(w * 0.75f, 0.2f, 0.45f), Lt(quilt, 0.4f), 0.1f, false);
        var hb = Cyl(g, new Vector3(0, 0.62f, -L / 2 - 0.03f), w / 2 + 0.05f, 0.14f, frame, 0.06f, Q(90, 0, 0), 22); hb.transform.localScale = new Vector3(1, 1, 0.9f);
        Block(g, new Vector3(0, 0.35f, L / 2), new Vector3(w + 0.04f, 0.5f, 0.12f), frame, 0.06f, false);
        if (stars) for (int i = 0; i < 7; i++) Ball(g, new Vector3(-w / 2 + 0.25f + (i * 0.37f) % (w - 0.4f), 0.74f, -0.2f + (i * 0.53f) % 1.2f), new Vector3(0.15f, 0.03f, 0.15f), star, 6);
        var bc = g.gameObject.AddComponent<BoxCollider>(); bc.size = new Vector3(w, 0.8f, L); bc.center = new Vector3(0, 0.4f, 0); return g;
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
        Block(g, new Vector3(0, 0.35f, 0), new Vector3(1.7f, 0.6f, 0.85f), White, 0.25f, false);
        Block(g, new Vector3(0, 0.6f, 0), new Vector3(1.45f, 0.08f, 0.6f), Sky, 0.04f, false);
        for (int i = 0; i < 4; i++) Ball(g, new Vector3(-0.4f + i * 0.25f, 0.68f, (i % 2) * 0.12f), Vector3.one * 0.14f, White, 8);
        Ball(g, new Vector3(0.45f, 0.7f, 0.05f), new Vector3(0.18f, 0.15f, 0.2f), ButterD, 8);
        foreach (int sx in new[] { -1, 1 }) Ball(g, new Vector3(sx * 0.7f, 0.05f, 0.3f), Vector3.one * 0.12f, ButterD, 6);
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
        for (int i = 0; i < 6; i++) { float a = i * 1.05f; var l = Ball(g, new Vector3(Mathf.Cos(a) * 0.18f, 0.6f + (i % 3) * 0.12f, Mathf.Sin(a) * 0.18f) * s, new Vector3(0.18f, 0.4f, 0.1f) * s, i % 2 == 0 ? Leaf : LeafD, 8); l.transform.localRotation = Q(0, -a * Mathf.Rad2Deg, 35); }
        var bc = g.gameObject.AddComponent<BoxCollider>(); bc.size = new Vector3(0.5f, 1f, 0.5f) * s; bc.center = new Vector3(0, 0.5f * s, 0); return g;
    }
    public static Transform FloorLamp(Transform p, Vector3 pos)
    {
        var g = Group(p, "sk_flamp", pos); Cyl(g, new Vector3(0, 0.04f, 0), 0.2f, 0.08f, WoodD); Cyl(g, new Vector3(0, 0.75f, 0), 0.03f, 1.4f, WoodD, 0.01f);
        Lath(g, new Vector3(0, 1.4f, 0), new[] { new Vector2(0, 0), new Vector2(0.3f, 0), new Vector2(0.18f, 0.35f), new Vector2(0, 0.35f) }, Butter, 16); return g;
    }
    public static Transform Frame(Transform p, Vector3 pos, float yaw, float w, float h, Color frame, Color art)
    {
        var g = Group(p, "sk_frame", pos, yaw); Block(g, Vector3.zero, new Vector3(w, h, 0.06f), frame, 0.03f, false); Block(g, new Vector3(0, 0, 0.03f), new Vector3(w - 0.12f, h - 0.12f, 0.02f), art, 0.01f, false);
        Ball(g, new Vector3(-w * 0.15f, -h * 0.1f, 0.05f), new Vector3(w * 0.35f, h * 0.3f, 0.02f), LeafD, 8); Ball(g, new Vector3(w * 0.2f, h * 0.18f, 0.05f), Vector3.one * w * 0.15f, ButterD, 8); return g;
    }
    public static Transform Clock(Transform p, Vector3 pos, float yaw)
    {
        var g = Group(p, "sk_clock", pos, yaw); Cyl(g, Vector3.zero, 0.25f, 0.06f, Butter, 0.02f, Q(90, 0, 0), 20); Ring(g, new Vector3(0, 0, 0.03f), Quaternion.identity, 0.25f, 0.03f, Coral, 20);
        Block(g, new Vector3(0, 0.06f, 0.04f), new Vector3(0.025f, 0.14f, 0.01f), Plum, 0.005f, false); Block(g, new Vector3(0.05f, 0, 0.04f), new Vector3(0.11f, 0.025f, 0.01f), Plum, 0.005f, false); return g;
    }
    public static Transform Curtains(Transform p, Vector3 center, float w, float h, Color c)
    {
        var g = Group(p, "sk_curtain", center); Cyl(g, new Vector3(0, h / 2 + 0.1f, 0), 0.03f, w + 0.6f, WoodD, 0.01f, Q(0, 0, 90), 8);
        foreach (int s in new[] { -1, 1 }) { var cu = Block(g, new Vector3(s * (w / 2 + 0.1f), 0, 0), new Vector3(0.38f, h, 0.1f), c, 0.06f, false, 0, 0.03f, -0.3f); Ball(g, new Vector3(s * (w / 2 + 0.1f), -h * 0.15f, 0.03f), new Vector3(0.22f, 0.08f, 0.14f), Dk(c, 0.9f), 8); }
        return g;
    }
    public static Transform PetBed(Transform p, Vector3 pos, Color c)
    {
        var g = Group(p, "sk_petbed", pos); var t = Ring(g, new Vector3(0, 0.12f, 0), Q(90, 0, 0), 0.38f, 0.14f, c, 24); Cyl(g, new Vector3(0, 0.05f, 0), 0.4f, 0.1f, Lt(PinkD, 0.3f), 0.04f); return g;
    }
    public static Transform Teddy(Transform p, Vector3 pos, float yaw, float s = 1f)
    {
        var g = Group(p, "sk_bear", pos, yaw); var fur = WoodD; var lite = Peach;
        Ball(g, new Vector3(0, 0.22f, 0) * s, new Vector3(0.42f, 0.46f, 0.36f) * s, fur); Ball(g, new Vector3(0, 0.24f, 0.12f) * s, new Vector3(0.28f, 0.3f, 0.16f) * s, Sky);
        Ball(g, new Vector3(0, 0.62f, 0) * s, Vector3.one * 0.4f * s, fur); Ball(g, new Vector3(0, 0.57f, 0.17f) * s, new Vector3(0.16f, 0.12f, 0.1f) * s, lite);
        foreach (int k in new[] { -1, 1 })
        {
            Ball(g, new Vector3(k * 0.16f, 0.8f, 0) * s, Vector3.one * 0.14f * s, fur); Ball(g, new Vector3(k * 0.16f, 0.8f, 0.03f) * s, Vector3.one * 0.08f * s, lite, 8);
            Ball(g, new Vector3(k * 0.075f, 0.66f, 0.17f) * s, Vector3.one * 0.05f * s, Plum, 8); Ball(g, new Vector3(k * 0.23f, 0.28f, 0.06f) * s, new Vector3(0.13f, 0.13f, 0.22f) * s, fur, 10); Ball(g, new Vector3(k * 0.13f, 0.04f, 0.13f) * s, new Vector3(0.15f, 0.11f, 0.22f) * s, fur, 10);
        }
        Ball(g, new Vector3(0, 0.6f, 0.22f) * s, Vector3.one * 0.05f * s, Plum, 8);
        return g;
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
    public static Transform ToyBlocks(Transform p, Vector3 pos, int n, System.Random rng)
    {
        var g = Group(p, "sk_toys", pos); Color[] tc = { Coral, MintD, SkyD, ButterD, LavD, PinkD };
        for (int i = 0; i < n; i++) { var b = Block(g, new Vector3(((float)rng.NextDouble() - 0.5f) * 0.9f, 0.1f, ((float)rng.NextDouble() - 0.5f) * 0.9f), Vector3.one * 0.2f, tc[i % tc.Length], 0.05f, false); b.transform.localRotation = Q(0, (float)rng.NextDouble() * 90, 0); }
        Ball(g, new Vector3(0.6f, 0.15f, 0.1f), Vector3.one * 0.3f, Coral); return g;
    }

    // ---------------- loot container bodies (collider on the returned root, sized to Data.CK)
    public static GameObject ContainerBody(Transform p, string kind, Vector3 pos, float yaw, Color accent)
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
                Block(g, new Vector3(0, 1.0f, 0), new Vector3(1.4f, 1.9f, 0.62f), accent, 0.14f, false, 0, -1, 0, Lt(accent, 0.2f));
                var crown = Cyl(g, new Vector3(0, 1.95f, 0), 0.7f, 0.6f, accent, 0.08f, Q(90, 0, 0), 22); crown.transform.localScale = new Vector3(1, 1, 0.35f);
                foreach (int s in new[] { -1, 1 }) { Block(g, new Vector3(s * 0.34f, 1.0f, 0.31f), new Vector3(0.62f, 1.6f, 0.05f), Lt(accent, 0.15f), 0.08f, false); Ball(g, new Vector3(s * 0.08f, 1.05f, 0.35f), Vector3.one * 0.09f, ButterD, 8); }
                foreach (int s in new[] { -1, 1 }) Cyl(g, new Vector3(s * 0.55f, 0.04f, 0), 0.06f, 0.08f, WoodD); break;
            case "drawer":
                Block(g, new Vector3(0, 0.45f, 0), new Vector3(0.88f, 0.82f, 0.52f), accent, 0.12f, false, 0, -1, 0, Lt(accent, 0.25f));
                for (int i = 0; i < 3; i++) { Block(g, new Vector3(0, 0.2f + i * 0.25f, 0.26f), new Vector3(0.74f, 0.2f, 0.04f), Lt(accent, 0.2f), 0.05f, false); Ball(g, new Vector3(0, 0.2f + i * 0.25f, 0.29f), Vector3.one * 0.07f, ButterD, 6); }
                break;
            case "box":
                Block(g, new Vector3(0, 0.33f, 0), new Vector3(0.85f, 0.62f, 0.85f), Wood, 0.08f, false, 0, 0.02f);
                Block(g, new Vector3(0, 0.66f, 0), new Vector3(0.88f, 0.06f, 0.18f), Butter, 0.02f, false);
                Block(g, new Vector3(0.3f, 0.6f, 0.25f), new Vector3(0.4f, 0.12f, 0.35f), Wood, 0.04f, false).transform.localRotation = Q(25, 15, 0); break;
            case "safe":
                Block(g, new Vector3(0, 0.3f, 0), new Vector3(0.58f, 0.58f, 0.58f), LavD, 0.12f, false);
                Ring(g, new Vector3(0, 0.32f, 0.3f), Quaternion.identity, 0.12f, 0.03f, ButterD, 18); Ball(g, new Vector3(0, 0.32f, 0.3f), new Vector3(0.1f, 0.1f, 0.05f), Butter, 8);
                for (int i = 0; i < 3; i++) Ball(g, new Vector3(-0.15f + i * 0.15f, 0.62f, 0), Vector3.one * 0.1f, new[] { PinkD, ButterD, MintD }[i], 8); break;
            case "shelf": case "display":
                Block(g, new Vector3(0, sz.y / 2, -sz.z / 2 + 0.04f), new Vector3(sz.x, sz.y, 0.08f), accent, 0.04f, false);
                foreach (int s in new[] { -1, 1 }) Block(g, new Vector3(s * (sz.x / 2 - 0.04f), sz.y / 2, 0), new Vector3(0.08f, sz.y, sz.z), accent, 0.04f, false);
                int nsh = kind == "shelf" ? 4 : 3;
                for (int i = 0; i < nsh; i++)
                {
                    float y = 0.1f + i * (sz.y - 0.15f) / (nsh - 0.6f); Block(g, new Vector3(0, y, 0), new Vector3(sz.x - 0.04f, 0.06f, sz.z), Lt(accent, 0.25f), 0.025f, false);
                    for (int k = 0; k < 4; k++) { var c = new[] { PinkD, ButterD, MintD, Coral, SkyD, LavD }[(i * 4 + k) % 6]; if ((i + k) % 3 == 0) Ball(g, new Vector3(-sz.x / 2 + 0.2f + k * (sz.x - 0.4f) / 3, y + 0.14f, 0), new Vector3(0.18f, 0.22f, 0.18f), c, 10); else Block(g, new Vector3(-sz.x / 2 + 0.2f + k * (sz.x - 0.4f) / 3, y + 0.14f, 0), new Vector3(0.16f, 0.22f, 0.14f), c, 0.05f, false); }
                }
                Block(g, new Vector3(0, sz.y + 0.03f, 0), new Vector3(sz.x + 0.04f, 0.08f, sz.z + 0.04f), accent, 0.04f, false); break;
            case "cooler":
                Block(g, new Vector3(0, 0.93f, 0), new Vector3(0.8f, 1.85f, 0.8f), Lt(SkyD, 0.1f), 0.18f, false);
                Block(g, new Vector3(0, 0.95f, 0.39f), new Vector3(0.62f, 1.3f, 0.05f), Lt(Sky, 0.5f), 0.05f, false);
                for (int i = 0; i < 4; i++) for (int k = 0; k < 3; k++) Cyl(g, new Vector3(-0.2f + k * 0.2f, 0.5f + i * 0.3f, 0.3f), 0.06f, 0.2f, new[] { Coral, MintD, ButterD, PinkD }[(i + k) % 4], 0.03f, null, 10);
                Block(g, new Vector3(0, 1.75f, 0.41f), new Vector3(0.6f, 0.15f, 0.04f), Coral, 0.04f, false); break;
            case "register":
                Block(g, new Vector3(0, 0.13f, 0), new Vector3(0.52f, 0.26f, 0.42f), Coral, 0.07f, false); Block(g, new Vector3(0, 0.3f, -0.1f), new Vector3(0.36f, 0.18f, 0.1f), Butter, 0.04f, false).transform.localRotation = Q(-25, 0, 0);
                for (int i = 0; i < 6; i++) Ball(g, new Vector3(-0.12f + (i % 3) * 0.12f, 0.28f, 0.03f + (i / 3) * 0.1f), new Vector3(0.07f, 0.03f, 0.07f), White, 6); break;
            default:
                Block(g, new Vector3(0, sz.y / 2, 0), sz, accent, Mathf.Min(0.12f, sz.y * 0.3f), false); break;
        }
        var bc = g.gameObject.AddComponent<BoxCollider>(); bc.size = sz; bc.center = new Vector3(0, sz.y / 2, 0);
        return g.gameObject;
    }
}
