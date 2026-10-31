using System.Collections.Generic;
using UnityEngine;
using static SoftKit;

/// v0.9.17 ② 外婆家農場「鵝叫警報」 — dusk paddies, farmhouse, pond geese, haystacks, gate bus stop.
public static partial class LevelGen
{
    public static readonly List<Bounds> Rice = new List<Bounds>();
    public static bool InRice(Vector3 p) { foreach (var b in Rice) if (p.x > b.min.x && p.x < b.max.x && p.z > b.min.z && p.z < b.max.z) return true; return false; }
    public static readonly List<Vector3> FarmHay = new List<Vector3>();
    public static Vector3 PondC = new Vector3(14, 0, 20);

    static Level BuildFarm(int seed, ThemeDef th, float vmult, int luckLv) { Level r = null; var it = BuildFarmCo(seed, th, vmult, luckLv, l => r = l); while (it.MoveNext()) { } return r; }
    public static System.Collections.IEnumerator BuildFarmCo(int seed, ThemeDef th, float vmult, int luckLv, System.Action<Level> done)
    {
        rng = new System.Random(seed); luck = luckLv; GlobalSeed = seed; SoftProps.ResetSigns();
        L = new Level { theme = th, seed = seed, vmult = vmult, root = new GameObject("Level").transform };
        Openings.Clear(); NoRide.Clear(); TrimCrossBefore = TrimPieces = 0; Rice.Clear(); FarmHay.Clear();
        var root = L.root; blocks.Clear(); var deco = new System.Random(20261101);
        LoadScreen.Set(LoadFrom + (LoadTo - LoadFrom) * 0 / 13f, "鋪草地和馬路…"); yield return null;
        // ---- ground: dusk grass, road + concrete shoulder by the gate
        Field(root, Vector3.zero, 130, 130, 2f, (x, z) => Color.Lerp(H("98b872"), H("7c9f5a"), VN(new Vector3(x * 0.06f, 0, z * 0.06f), 9)), true, "grass_ground");
        Field(root, new Vector3(0, 0.02f, -30.25f), 130, 7.5f, 1f, (x, z) => Color.Lerp(H("b9a996"), H("8e8070"), Mathf.Clamp01(0.4f * VN(new Vector3(x * 0.3f, 0, z * 0.3f), 5) + 0.6f * VN(new Vector3(x * 0.05f, 0, z * 0.05f), 11))), true, "road");
        for (float x = -60; x < 60; x += 6) Block(root, new Vector3(x, 0.035f, -30.25f), new Vector3(2.5f, 0.03f, 0.2f), H("f2e6c4"), 0.01f, false, 0, 0);
        Field(root, new Vector3(0, 0.12f, -24.75f), 130, 3.5f, 1f, (x, z) => ((Mathf.FloorToInt(x) + Mathf.FloorToInt(z)) & 1) == 0 ? H("e8dcc8") : H("ddd0bc"), true, "sidewalk");
        Block(root, new Vector3(0, 0.08f, -26.5f), new Vector3(130, 0.17f, 0.22f), H("d8cfc0"), 0.06f, false, 0, 0.01f);
        foreach (var (p, s) in new[] { (new Vector3(0, 2, 60), new Vector3(130, 6, 1)), (new Vector3(0, 2, -60), new Vector3(130, 6, 1)), (new Vector3(60, 2, 0), new Vector3(1, 6, 130)), (new Vector3(-60, 2, 0), new Vector3(1, 6, 130)) })
        { var b = new GameObject("bound"); b.transform.SetParent(root, false); b.transform.localPosition = p; b.AddComponent<BoxCollider>().size = s; }
        // keep the road side of the farm walkable but stop kids wandering off south across the road
        { var b = new GameObject("bound"); b.transform.SetParent(root, false); b.transform.localPosition = new Vector3(0, 2, -34.5f); b.AddComponent<BoxCollider>().size = new Vector3(130, 6, 1); }

        LoadScreen.Set(LoadFrom + (LoadTo - LoadFrom) * 1 / 13f, "蓋外婆的紅磚厝…"); yield return null;
        // ---- farmhouse (the aunt kit, red brick + tiled roof)
        var rooms = new List<ARoom>(); var lockedC = new List<LootContainer>(); var petSpots = new List<Vector3>();
        FarmHouse = true; Transform house;
        try { house = AuntHouse(root, new Vector3(-24, 0, 10), rooms, lockedC, petSpots); } finally { FarmHouse = false; }
        house.name = "House";
        foreach (var r in rooms)
        {   // v0.9.18 warm lamp glow in every farmhouse room (tungsten bulb, no shadows)
            var lg = new GameObject("room_glow"); lg.transform.SetParent(house, false); lg.transform.localPosition = new Vector3((r.x0 + r.x1) / 2, FY + 2.3f, (r.z0 + r.z1) / 2);
            var li = lg.AddComponent<Light>(); li.type = LightType.Point; li.color = new Color(1f, 0.78f, 0.52f); li.range = 7.5f; li.intensity = 1.1f; li.shadows = LightShadows.None;
        }
        {   // wooden name board under the eave
            var sg = Group(house, "farm_sign", new Vector3(0, FY + AWH + 0.62f, -1.45f));
            Block(sg, Vector3.zero, new Vector3(2.6f, 0.78f, 0.12f), H("8a5a3c"), 0.05f, false); Block(sg, new Vector3(0, 0, -0.05f), new Vector3(2.35f, 0.58f, 0.04f), H("f1e2c2"), 0.03f, false);
            SoftProps.Text3D(sg, new Vector3(0, 0.02f, -0.95f), "外婆家", 0.15f, H("b8402e"));
            foreach (int sx in new[] { -1, 1 }) Cyl(sg, new Vector3(sx * 1.0f, -0.5f, 0.1f), 0.03f, 0.5f, H("6a4430"), 0.01f, null, 8);
        }
        foreach (int sx in new[] { -1, 1 })
        {   // warm porch lanterns
            var lg = new GameObject("porch_light"); lg.transform.SetParent(house, false); lg.transform.localPosition = new Vector3(sx * 3.6f, FY + 2.6f, -1.0f);
            var li = lg.AddComponent<Light>(); li.type = LightType.Point; li.color = new Color(1f, 0.74f, 0.42f); li.range = 9; li.intensity = 1.6f; li.shadows = LightShadows.None;
            Lath(house, new Vector3(sx * 3.6f, FY + 2.35f, -1.0f), new List<Vector2> { new Vector2(0, 0), new Vector2(0.12f, 0.04f), new Vector2(0.16f, 0.2f), new Vector2(0.12f, 0.36f), new Vector2(0, 0.4f) }, H("e8503a"), 14, false, null, "lantern");
            Cyl(house, new Vector3(sx * 3.6f, FY + 2.8f, -1.0f), 0.012f, 0.1f, H("3a2a20"), 0.004f, null, 6);
        }
        LoadScreen.Set(LoadFrom + (LoadTo - LoadFrom) * 2 / 13f, "整理曬穀場…"); yield return null;
        // ---- 曬穀場 concrete drying yard with golden grain patches, rake, baskets
        Field(root, new Vector3(-24, 0.05f, 3.5f), 26, 11, 1f, (x, z) => Color.Lerp(H("d9d0c0"), H("c8bead"), VN(new Vector3(x * 0.4f, 0, z * 0.4f), 21)), false, "yard");
        foreach (var (cx, cz, sx2, sz2) in new[] { (-30f, 3.5f, 6f, 4.5f), (-21f, 2.5f, 5f, 4f) })
            Field(root, new Vector3(cx, 0.065f, cz), sx2, sz2, 0.25f, (x, z) => Color.Lerp(FarmArt.Straw, FarmArt.StrawL, VN(new Vector3(x * 3, 0, z * 3), 31)) * (0.96f + 0.08f * Mathf.Sin(x * 6f)), false, "grain");
        {   // wooden grain rake leaning by the patch
            var rk = Group(root, "sk_rake", new Vector3(-26.4f, 0.07f, 1.0f), 20);
            Cyl(rk, new Vector3(0, 0.03f, 0.6f), 0.025f, 1.6f, H("a87a50"), 0.01f, Quaternion.Euler(90, 0, 0), 8); Block(rk, new Vector3(0, 0.06f, 1.42f), new Vector3(0.7f, 0.08f, 0.06f), H("8a6040"), 0.02f, false);
            for (int i = 0; i < 7; i++) Block(rk, new Vector3(-0.3f + i * 0.1f, 0.03f, 1.48f), new Vector3(0.025f, 0.05f, 0.1f), H("8a6040"), 0.008f, false);
        }
        SCont(root, "basket", new Vector3(-33.8f, 0.07f, 6.2f), 0, FarmArt.Straw, "竹籃子");
        SCont(root, "ricejar", new Vector3(-15.6f, 0.07f, 7.6f), 0, H("7a4a36"), "外婆的米缸");
        SCont(root, "basket", new Vector3(-17.2f, 0.07f, 1.0f), 30, FarmArt.Straw, "菜籃子");

        LoadScreen.Set(LoadFrom + (LoadTo - LoadFrom) * 3 / 13f, "插秧、做田埂…"); yield return null;
        // ---- rice paddies (4 x 2) with raised 田埂 ridges
        float px0 = -7f, pz0 = -20.6f, pw = 10.1f, pd = 9.4f, rg = 1.4f;
        for (int i = 0; i < 4; i++) for (int j = 0; j < 2; j++)
        {
            var c = new Vector3(px0 + rg + pw / 2 + i * (pw + rg), 0, pz0 + rg + pd / 2 + j * (pd + rg));
            FarmArt.Paddy(root, c, pw, pd, 101 + i * 7 + j * 3);
            Rice.Add(new Bounds(c + Vector3.up, new Vector3(pw - 0.2f, 2, pd - 0.2f)));
        }
        float W = 4 * pw + 5 * rg, D = 2 * pd + 3 * rg;
        for (int i = 0; i <= 4; i++) Ridge(root, new Vector3(px0 + rg / 2 + i * (pw + rg), 0, pz0 + D / 2), new Vector3(rg, 0.16f, D));
        for (int j = 0; j <= 2; j++) Ridge(root, new Vector3(px0 + W / 2, 0, pz0 + rg / 2 + j * (pd + rg)), new Vector3(W, 0.16f, rg));
        FarmArt.Scarecrow(root, new Vector3(px0 + rg + pw * 1.5f + rg, 0.05f, pz0 + D / 2 + 0.0f), 200);
        FarmArt.Scarecrow(root, new Vector3(px0 + 3 * rg + pw * 2.6f + rg, 0.05f, pz0 + rg + pd * 0.5f), 160);

        LoadScreen.Set(LoadFrom + (LoadTo - LoadFrom) * 4 / 13f, "挖池塘、放鵝…"); yield return null;
        // ---- pond with lily pads, reeds, stones and a little dock (geese live here)
        {
            var pg = Group(root, "pond", PondC);
            var mb = new FMB { a = MatK.Tile }; int N = 48; float rx = 7, rz = 5;
            int ci = mb.V(new Vector3(0, 0.06f, 0), Vector3.up, H("5f8fa6"));
            for (int k = 0; k <= N; k++) { float an = k * Mathf.PI * 2 / N; float w = 1 + 0.06f * Mathf.Sin(an * 3 + 1); mb.V(new Vector3(Mathf.Cos(an) * rx * w, 0.06f, Mathf.Sin(an) * rz * w), Vector3.up, Color.Lerp(H("7aa6b4"), H("c9b4b8"), 0.25f + 0.2f * Mathf.Sin(an))); if (k > 0) { mb.t.Add(ci); mb.t.Add(ci + k + 1); mb.t.Add(ci + k); } }
            Obj(pg, "pond_water", mb.Build("pond"), Vector3.zero, Quaternion.identity, 0.0001f);
            // muddy bank ring
            var bk = new FMB { a = MatK.Plaster };
            for (int k = 0; k < N; k++) { float a0 = k * Mathf.PI * 2 / N, a1 = (k + 1) * Mathf.PI * 2 / N; float w0 = 1 + 0.06f * Mathf.Sin(a0 * 3 + 1), w1 = 1 + 0.06f * Mathf.Sin(a1 * 3 + 1);
                Vector3 P(float a, float w, float o, float y) => new Vector3(Mathf.Cos(a) * (rx * w + o), y, Mathf.Sin(a) * (rz * w + o));
                bk.Quad(P(a0, w0, -0.1f, 0.07f), P(a0, w0, 0.9f, 0.03f), P(a1, w1, 0.9f, 0.03f), P(a1, w1, -0.1f, 0.07f), Color.Lerp(H("a48c6c"), H("b9a27c"), (k % 3) / 3f)); }
            Obj(pg, "pond_bank", bk.Build("bank"), Vector3.zero, Quaternion.identity, 0.0001f);
            for (int k = 0; k < 14; k++) { float an = k * 0.45f + (float)deco.NextDouble() * 0.3f; var sp2 = new Vector3(Mathf.Cos(an) * (rx + 0.5f), 0.05f, Mathf.Sin(an) * (rz + 0.5f)); Ball(pg, sp2, new Vector3(0.5f + (float)deco.NextDouble() * 0.4f, 0.25f, 0.45f), Color.Lerp(H("b7aa98"), H("9a8e80"), (float)deco.NextDouble()), 10, false, null, "pond_stone", 0.12f); }
            for (int k = 0; k < 9; k++) { float an = (float)deco.NextDouble() * 6.28f; float rr = (float)deco.NextDouble() * 0.7f; var lp = new Vector3(Mathf.Cos(an) * rx * rr, 0.075f, Mathf.Sin(an) * rz * rr);
                var pad = new FMB { a = MatK.Foliage }; int nn = 14; int c0 = pad.V(Vector3.zero, Vector3.up, H("7fae5a")); for (int q = 0; q <= nn; q++) { float aa = 0.5f + q * (Mathf.PI * 2 - 1f) / nn; pad.V(new Vector3(Mathf.Cos(aa), 0, Mathf.Sin(aa)) * 0.45f, Vector3.up, H("95c06a")); if (q > 0) { pad.t.Add(c0); pad.t.Add(c0 + q + 1); pad.t.Add(c0 + q); } }
                var po = Obj(pg, "lilypad", pad.Build("pad"), lp, Quaternion.Euler(0, deco.Next(360), 0), 0.004f); po.transform.localScale = Vector3.one * (0.7f + (float)deco.NextDouble() * 0.6f);
                if (k % 3 == 0) Lath(pg, lp + Vector3.up * 0.01f, new List<Vector2> { new Vector2(0, 0), new Vector2(0.1f, 0.03f), new Vector2(0.13f, 0.1f), new Vector2(0.05f, 0.16f), new Vector2(0, 0.12f) }, H("f2b8c6"), 10, false, null, "lotus"); }
            var reeds = new FMB { a = MatK.Foliage };
            for (int k = 0; k < 90; k++) { float an = 3.4f + (float)deco.NextDouble() * 1.8f; float rr = 0.92f + (float)deco.NextDouble() * 0.12f; var b0 = new Vector3(Mathf.Cos(an) * rx * rr, 0.05f, Mathf.Sin(an) * rz * rr); float hh = 0.8f + (float)deco.NextDouble() * 0.7f; var lean = new Vector3((float)deco.NextDouble() - 0.5f, 0, (float)deco.NextDouble() - 0.5f) * 0.3f;
                reeds.Ribbon(new[] { b0, b0 + Vector3.up * hh * 0.5f + lean * 0.3f, b0 + Vector3.up * hh + lean }, Vector3.Cross(lean.normalized + Vector3.forward * 0.01f, Vector3.up).normalized, new[] { 0.04f, 0.03f, 0.005f }, new[] { H("6f9248"), H("94ad58"), H("c8c070") });
                if (k % 5 == 0) Lath(pg, b0 + Vector3.up * hh * 0.85f + lean * 0.8f, new List<Vector2> { new Vector2(0, 0), new Vector2(0.03f, 0.02f), new Vector2(0.032f, 0.14f), new Vector2(0, 0.17f) }, H("8a5a3a"), 8, false, null, "cattail"); }
            Obj(pg, "reeds", reeds.Build("reeds"), Vector3.zero, Quaternion.identity, 0.0001f);
            // little plank dock on the south bank
            var dk = Group(pg, "dock", new Vector3(-1.5f, 0, -rz + 0.2f));
            for (int k = 0; k < 6; k++) Block(dk, new Vector3(0, 0.2f, -0.9f + k * 0.38f), new Vector3(1.4f, 0.06f, 0.34f), Color.Lerp(H("b08660"), H("9a7350"), (k % 2) * 0.6f), 0.02f, k < 3);
            foreach (int sx in new[] { -1, 1 }) foreach (float z in new[] { -0.9f, 1.0f }) Cyl(dk, new Vector3(sx * 0.62f, 0.12f, z), 0.06f, 0.36f, H("7a5a40"), 0.02f, null, 8);
            // invisible wall so kids walk around the water, not into it
            var wc = new GameObject("pond_wall"); wc.transform.SetParent(pg, false); wc.transform.localPosition = new Vector3(0, 0.8f, 0.4f); var cap = wc.AddComponent<CapsuleCollider>(); cap.direction = 0; cap.radius = rz - 0.6f; cap.height = rx * 2 - 0.4f;
        }
        LoadScreen.Set(LoadFrom + (LoadTo - LoadFrom) * 5 / 13f, "蓋雞舍…"); yield return null;
        // ---- chicken coop with nest boxes + 2 hens
        {
            var cp = Group(root, "Coop", new Vector3(34, 0, 18), -90);
            Block(cp, new Vector3(0, 0.05f, 0), new Vector3(5.6f, 0.1f, 4.0f), H("b9a888"), 0.03f, false);
            // hut
            Block(cp, new Vector3(0, 1.0f, 1.4f), new Vector3(3.0f, 2.0f, 0.12f), H("b98a5e"), 0.03f, true);
            foreach (int sx in new[] { -1, 1 }) Block(cp, new Vector3(sx * 1.5f, 1.0f, 0.8f), new Vector3(0.12f, 2.0f, 1.3f), H("a87a50"), 0.03f, true);
            for (int k = 0; k < 6; k++) Block(cp, new Vector3(0, 2.12f + k * 0.0f, 0.2f + k * 0.26f), new Vector3(3.4f, 0.05f, 0.32f), Color.Lerp(H("8c8478"), H("a9a091"), k % 2), 0.01f, false).transform.localRotation = Quaternion.Euler(-12, 0, 0);
            // wire pen (posts + mesh lines)
            var pen = new FMB { a = MatK.Metal };
            foreach (var (a, b) in new[] { (new Vector3(-2.6f, 0, -1.8f), new Vector3(2.6f, 0, -1.8f)), (new Vector3(-2.6f, 0, -1.8f), new Vector3(-2.6f, 0, 1.4f)), (new Vector3(2.6f, 0, -1.8f), new Vector3(2.6f, 0, 1.4f)) })
            {
                var d = b - a; float LL = d.magnitude; var side = Vector3.Cross(d.normalized, Vector3.up);
                for (float y = 0.15f; y < 1.2f; y += 0.18f) pen.Tube(new List<Vector3> { a + Vector3.up * y, b + Vector3.up * y }, new List<float> { 0.006f, 0.006f }, new List<Color> { H("c8c4bc"), H("c8c4bc") }, 4);
                for (float t = 0; t <= LL + 0.01f; t += 0.18f) pen.Tube(new List<Vector3> { a + d.normalized * t + Vector3.up * 0.1f, a + d.normalized * t + Vector3.up * 1.2f }, new List<float> { 0.005f, 0.005f }, new List<Color> { H("c8c4bc"), H("c8c4bc") }, 4);
                for (float t = 0; t <= LL + 0.01f; t += 1.3f) Cyl(cp, a + d.normalized * t + Vector3.up * 0.65f, 0.05f, 1.3f, H("8a6a44"), 0.015f, null, 8);
                var bc = new GameObject("pen_col"); bc.transform.SetParent(cp, false); bc.transform.localPosition = (a + b) / 2 + Vector3.up * 0.6f; bc.transform.localRotation = Quaternion.LookRotation(d); bc.AddComponent<BoxCollider>().size = new Vector3(0.1f, 1.2f, LL);
            }
            Obj(cp, "pen_wire", pen.Build("pen"), Vector3.zero, Quaternion.identity, 0.0001f);
            SCont(cp, "nest", new Vector3(-0.8f, 0.1f, 1.0f), 180, H("a77a50"), "雞窩");
            SCont(cp, "nest", new Vector3(0.6f, 0.1f, 1.0f), 180, H("a77a50"), "雞窩");
            FarmArt.Goose(cp, new Vector3(-1.4f, 0.1f, -0.6f), 30, 1); FarmArt.Goose(cp, new Vector3(1.2f, 0.1f, -1.0f), -60, 1);
            for (int k = 0; k < 20; k++) Ball(cp, new Vector3((float)deco.NextDouble() * 4 - 2, 0.11f, (float)deco.NextDouble() * 2.6f - 1.6f), new Vector3(0.05f, 0.01f, 0.03f), FarmArt.Straw, 6, false, null, "grain", 0f);
            L.spots.Add(cp.TransformPoint(new Vector3(0, 0.1f, 0)));
        }
        LoadScreen.Set(LoadFrom + (LoadTo - LoadFrom) * 6 / 13f, "整理工具間…"); yield return null;
        // ---- tool shed (open front) with tool chest + crate
        {
            var sh = Group(root, "Shed", new Vector3(-38, 0, -12), 90);
            Block(sh, new Vector3(0, 0.06f, 0), new Vector3(4.4f, 0.12f, 3.4f), H("b8ad9c"), 0.03f, false);
            Block(sh, new Vector3(0, 1.25f, 1.6f), new Vector3(4.4f, 2.5f, 0.14f), H("9a6a48"), 0.03f, true);
            foreach (int sx in new[] { -1, 1 }) Block(sh, new Vector3(sx * 2.15f, 1.25f, 0.2f), new Vector3(0.14f, 2.5f, 2.9f), H("8a5e40"), 0.03f, true);
            for (int k = 0; k < 9; k++) Block(sh, new Vector3(-2.0f + k * 0.5f, 1.25f, 1.52f), new Vector3(0.02f, 2.4f, 0.02f), H("7a5038"), 0.005f, false);
            var rf = Block(sh, new Vector3(0, 2.75f, 0.2f), new Vector3(4.9f, 0.08f, 3.8f), H("8c94a0"), 0.02f, false); rf.transform.localRotation = Quaternion.Euler(-8, 0, 0);
            for (int k = 0; k < 10; k++) Block(sh, new Vector3(-2.3f + k * 0.5f, 2.8f, 0.2f), new Vector3(0.06f, 0.04f, 3.8f), H("a4acb6"), 0.01f, false).transform.localRotation = Quaternion.Euler(-8, 0, 0);
            SCont(sh, "toolchest", new Vector3(-1.2f, 0.12f, 1.1f), 180, Mint, "外公的工具櫃");
            SCont(sh, "crate", new Vector3(0.9f, 0.12f, 1.0f), 180, Wood, "農具箱");
            // hoe + sickle on the wall
            Cyl(sh, new Vector3(0.0f, 1.3f, 1.48f), 0.025f, 1.5f, H("b08a5c"), 0.01f, Quaternion.Euler(0, 0, 12), 8); Block(sh, new Vector3(0.16f, 2.02f, 1.44f), new Vector3(0.3f, 0.06f, 0.08f), H("8a8f98"), 0.01f, false);
            L.spots.Add(sh.TransformPoint(new Vector3(0, 0.12f, 0)));
        }
        LoadScreen.Set(LoadFrom + (LoadTo - LoadFrom) * 7 / 13f, "種菜園…"); yield return null;
        // ---- vegetable rows (cabbages + onion-like sprouts)
        for (int r = 0; r < 4; r++)
        {
            var row = new Vector3(-35, 0, -6.5f + r * 1.6f); Block(root, row + Vector3.up * 0.08f, new Vector3(9, 0.18f, 0.9f), H("8c6a4a"), 0.08f, false, 0, 0.03f, 0.2f, H("9e7a56"));
            for (int k = 0; k < 8; k++) Cabbage(root, row + new Vector3(-3.9f + k * 1.1f, 0.17f, 0), deco.Next(1000), r % 2 == 0);
        }
        LoadScreen.Set(LoadFrom + (LoadTo - LoadFrom) * 8 / 13f, "堆稻草…"); yield return null;
        // ---- haystacks: hide spots
        foreach (var hp in new[] { new Vector2(-37.5f, 2.5f), new Vector2(-10.4f, 5.0f), new Vector2(-10.2f, -13.5f), new Vector2(24f, 8.5f), new Vector2(28.5f, 25.5f), new Vector2(5.5f, 27.5f), new Vector2(-4.5f, 9.5f) })
        { FarmArt.Haystack(root, new Vector3(hp.x, 0.02f, hp.y), 1f, Mathf.RoundToInt(hp.x * 7 + hp.y * 3)); FarmHay.Add(new Vector3(hp.x, 0, hp.y)); }
        LoadScreen.Set(LoadFrom + (LoadTo - LoadFrom) * 9 / 13f, "圍籬笆…"); yield return null;
        // ---- fences: front along the road with west + main gates, sides, back
        float fz = -22.6f;
        FarmArt.BambooFence(root, new Vector3(-44, 0, fz), new Vector3(-42, 0, fz)); FarmArt.BambooFence(root, new Vector3(-38, 0, fz), new Vector3(17.5f, 0, fz)); FarmArt.BambooFence(root, new Vector3(26.5f, 0, fz), new Vector3(44, 0, fz));
        FarmArt.BambooFence(root, new Vector3(-44, 0, fz), new Vector3(-44, 0, 34)); FarmArt.BambooFence(root, new Vector3(44, 0, fz), new Vector3(44, 0, 34)); FarmArt.BambooFence(root, new Vector3(-44, 0, 34), new Vector3(44, 0, 34));
        GateArch(root, new Vector3(22, 0, fz), "外婆家農場"); GateArch(root, new Vector3(-40, 0, fz), null);
        LoadScreen.Set(LoadFrom + (LoadTo - LoadFrom) * 10 / 13f, "種竹林…"); yield return null;
        // ---- bamboo grove behind the back fence + along the sides
        for (int k = 0; k < 16; k++) FarmArt.BambooClump(root, new Vector3(-42 + k * 5.6f + (float)deco.NextDouble() * 1.5f, 0, 37 + (float)deco.NextDouble() * 3), 500 + k, 1f);
        for (int k = 0; k < 6; k++) { FarmArt.BambooClump(root, new Vector3(48 + (float)deco.NextDouble() * 3, 0, -14 + k * 9), 600 + k, 0.9f); FarmArt.BambooClump(root, new Vector3(-48 - (float)deco.NextDouble() * 3, 0, -8 + k * 8), 700 + k, 0.9f); }
        // utility poles along the road
        for (float x = -52; x <= 52; x += 13) if (Mathf.Abs(x - 22) > 4 && Mathf.Abs(x + 40) > 3)
        { Cyl(root, new Vector3(x, 3.62f, -23.5f), 0.13f, 7f, H("9a9288"), 0.04f, null, 10, true, "sk_pole"); Block(root, new Vector3(x, 6.7f, -23.5f), new Vector3(1.8f, 0.12f, 0.12f), H("6a6258"), 0.03f, false); }
        // distant lilac mountains + warm hills (no clouds)
        for (int i = 0; i < 9; i++) { float x = -64 + i * 16; SoftProps.Hill(root, new Vector3(x, -6, 74 + (i % 2) * 6), new Vector3(36 + (i % 3) * 8, 34 + (i % 2) * 12, 20), Color.Lerp(H("b8a8c8"), H("c9b4c4"), (i % 3) / 2f)); SoftProps.Hill(root, new Vector3(x + 8, -4, 62), new Vector3(28, 18 + (i % 3) * 5, 14), H("9fb070")); SoftProps.Hill(root, new Vector3(x + 8, -4, -66), new Vector3(30, 20, 16), H("a8b47a")); }
        for (int i = 0; i < 5; i++) { SoftProps.Hill(root, new Vector3(70, -4, -48 + i * 24), new Vector3(18, 22, 30), H("a4b276")); SoftProps.Hill(root, new Vector3(-70, -4, -48 + i * 24), new Vector3(18, 22, 30), H("a0ae72")); }
        // water jars by the house + a tricycle-cart of hay
        SoftProps.WaterJar(root, new Vector3(-12.6f, 0.07f, 9.2f), H("8a5a3c")); SoftProps.WaterJar(root, new Vector3(-13.5f, 0.07f, 9.6f), H("6a7a8a"));

        LoadScreen.Set(LoadFrom + (LoadTo - LoadFrom) * 11 / 13f, "藏寶物和鑰匙…"); yield return null;
        // ---- key + ground loot
        var keyable = L.containers.FindAll(c => !c.special && !lockedC.Contains(c));
        if (keyable.Count > 0) { var kc = Pick(keyable); kc.slots.Insert(rng.Next(kc.slots.Count + 1), new LootSlot { it = new Item { r = 2, ic = "🔑", name = "神秘鑰匙", v = 0, key = true } }); }
        var gSpots = new List<Vector3>(); foreach (var r in rooms) gSpots.Add(house.TransformPoint(r.C));
        gSpots.Add(new Vector3(-24, 0.1f, 4)); gSpots.Add(new Vector3(px0 + rg / 2 + 2 * (pw + rg), 0.2f, -9.1f)); gSpots.Add(new Vector3(12.5f, 0.3f, 15.4f)); gSpots.Add(new Vector3(-36, 0.1f, -12));
        int gN = rng.Next(7, 10);
        for (int i = 0; i < gN; i++)
        {
            var p = gSpots[rng.Next(gSpots.Count)] + new Vector3(RR(-1.0f, 1.0f), 0.06f, RR(-1.0f, 1.0f));
            var it = LootContainer.Roll(R() < 0.7f ? "low" : "mid", rng, th, vmult, luck);
            var g = new GameObject("GroundLoot"); g.transform.SetParent(root, false); g.transform.position = p; g.AddComponent<GroundLoot>().Init(it);
        }
        L.spots.Add(new Vector3(-24, 0.1f, 4)); L.spots.Add(PondC + new Vector3(0, 0.1f, -6.5f));
        LoadScreen.Set(LoadFrom + (LoadTo - LoadFrom) * 12 / 13f, "公車、狗狗和鵝就位…"); yield return null;
        // ---- bus at the main gate, pets, geese, spawn
        L.bus = Bus.Build(root, new Vector3(22, 0.12f, -25.4f), Vector3.right);
        {
            var cat = Pet.Create(root, (petSpots.Count > 0 ? petSpots[0] : house.TransformPoint(new Vector3(0, FY, 3))) + Vector3.up * 0.05f, false, CatNames[((L.seed) % CatNames.Length + CatNames.Length) % CatNames.Length]);
            var inside = new List<Vector3>(); foreach (var r in rooms) if (inside.Count < 3) inside.Add(house.TransformPoint(r.C)); cat.patrol.AddRange(inside);
            // v0.9.18: route keeps ~4 m off the paddies (was walking the 田埂 right beside the rice where kids hide)
            var dogRoute = new List<Vector3> { new Vector3(-24, 0.1f, 3), new Vector3(-12, 0.1f, 0.5f), new Vector3(px0 + rg / 2 + 2 * (pw + rg), 0.1f, 6.2f), new Vector3(14, 0.1f, 12.5f), new Vector3(29, 0.1f, 12), new Vector3(px0 + rg / 2 + 3 * (pw + rg), 0.1f, 6.2f) };
            var dog = Pet.Create(root, dogRoute[0] + Vector3.up * 0.05f, true, DogNames[((1 + L.seed) % DogNames.Length + DogNames.Length) % DogNames.Length]); dog.patrol.AddRange(dogRoute);
        }
        Goose.All.Clear();
        for (int i = 0; i < 3; i++) { float an = -1.2f + i * 0.9f; var gp = PondC + new Vector3(Mathf.Cos(an) * 8.2f, 0.05f, Mathf.Sin(an) * 6.2f); Goose.Create(root, gp, PondC + new Vector3(Mathf.Cos(an) * 8.3f, 0.05f, Mathf.Sin(an) * 6.3f), an * Mathf.Rad2Deg + 90); }
        L.spawn = new Vector3(-50, 0.2f, -25.0f); L.spawnYaw = 75;
        Art.Toonify(root.gameObject, 0.012f);
        // toonify gives everything outlines; keep rice/straw/foliage ribbons outline-free (would double the blades)
        foreach (var mr in root.GetComponentsInChildren<MeshRenderer>()) if (mr.name == "rice" || mr.name == "hay_straws" || mr.name == "bamboo_leaf" || mr.name == "reeds" || mr.name == "sc_straw" || mr.name == "pond_water" || mr.name == "pen_wire") mr.sharedMaterial = Mat(0.0001f);
        Debug.Log("[farm] containers " + L.containers.Count + " locked " + lockedC.Count + " hay " + FarmHay.Count + " geese " + Goose.All.Count + " paddies " + Rice.Count);
        done(L); yield break;
    }
    static void Ridge(Transform root, Vector3 c, Vector3 s)
    {   // 田埂: packed mud with a grassy top, walkable (low step)
        Block(root, c + Vector3.up * s.y / 2, s, H("8e7050"), 0.05f, true, 0, 0.02f, 0.15f, H("98b066"), "ridge");
    }
    static void Cabbage(Transform p, Vector3 pos, int seed, bool round)
    {
        var rnd = new System.Random(seed); var mb = new FMB { a = MatK.Foliage }; var g = Group(p, "sk_cabbage", pos, rnd.Next(360));
        if (round)
        {
            Ball(g, new Vector3(0, 0.12f, 0), new Vector3(0.28f, 0.24f, 0.28f), H("b7d68a"), 12, false, null, "cab_head", 0.04f);
            for (int i = 0; i < 7; i++) { float an = i * 0.9f; var d = new Vector3(Mathf.Cos(an), 0, Mathf.Sin(an)); mb.Ribbon(new[] { d * 0.06f + Vector3.up * 0.04f, d * 0.2f + Vector3.up * 0.16f, d * 0.3f + Vector3.up * 0.12f }, Vector3.Cross(d, Vector3.up), new[] { 0.06f, 0.2f, 0.12f }, new[] { H("8fbf66"), H("7aae58"), H("6a9a4c") }, true, 0.7f); }
        }
        else for (int i = 0; i < 9; i++) { float an = i * 0.7f; var d = new Vector3(Mathf.Cos(an), 0, Mathf.Sin(an)); float h = 0.3f + (float)rnd.NextDouble() * 0.2f; mb.Ribbon(new[] { Vector3.zero, d * 0.04f + Vector3.up * h * 0.6f, d * 0.1f + Vector3.up * h }, Vector3.Cross(d, Vector3.up), new[] { 0.03f, 0.025f, 0.005f }, new[] { H("e8e2c8"), H("7aae58"), H("9cc46a") }); }
        Obj(g, "cab_leaf", mb.Build("cab"), Vector3.zero, Quaternion.identity, 0.004f);
    }
    static void GateArch(Transform root, Vector3 c, string text)
    {
        var g = Group(root, "sk_gate", c); float hw = text != null ? 4.5f : 2f;
        foreach (int sx in new[] { -1, 1 }) { Cyl(g, new Vector3(sx * hw, 1.5f, 0), 0.14f, 3.0f, H("8a5e40"), 0.04f, null, 12, true, "gate_post"); Cyl(g, new Vector3(sx * hw, 3.05f, 0), 0.2f, 0.12f, H("6a4430"), 0.04f, null, 12); }
        if (text == null) return;
        Cyl(g, new Vector3(0, 2.9f, 0), 0.1f, hw * 2 + 0.6f, H("8a5e40"), 0.03f, Quaternion.Euler(0, 0, 90), 10);
        var sg = Group(g, "gate_sign", new Vector3(0, 3.35f, 0)); Block(sg, Vector3.zero, new Vector3(4.2f, 0.85f, 0.14f), H("9a6a48"), 0.06f, false); Block(sg, new Vector3(0, 0, -0.06f), new Vector3(3.9f, 0.62f, 0.04f), H("f4e6c6"), 0.03f, false);
        SoftProps.Text3D(sg, new Vector3(0, 0.02f, -0.95f), text, 0.13f, H("b8402e"));
        var sg2 = Group(g, "gate_sign_b", new Vector3(0, 3.35f, 0)); sg2.localRotation = Quaternion.Euler(0, 180, 0); SoftProps.Text3D(sg2, new Vector3(0, 0.02f, -0.95f), text, 0.13f, H("b8402e"));
        foreach (int sx in new[] { -1, 1 }) Lath(g, new Vector3(sx * 1.7f, 2.4f, -0.1f), new List<Vector2> { new Vector2(0, 0), new Vector2(0.16f, 0.05f), new Vector2(0.2f, 0.25f), new Vector2(0.16f, 0.45f), new Vector2(0, 0.5f) }, H("e04a36"), 14, false, null, "lantern");
    }
}
