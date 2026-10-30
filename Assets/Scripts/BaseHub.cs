using UnityEngine;

/// CASUAL: the kid's bedroom is the base.
public static class BaseHub
{
    public static Transform Build(out Vector3 spawn, out float yaw)
    {
        var root = new GameObject("Base").transform; var S = SaveSys.S;
        float W = 9, D = 8, H = 3.0f; SoftKit.GlobalSeed = 606;
        System.Func<Color, float, Color> Lt = (c, k) => Color.Lerp(c, Color.white, k);
        SoftKit.Block(root, new Vector3(0, -0.1f, 0), new Vector3(40, 0.2f, 40), SoftKit.Leaf, 0.05f, true, 0, 0, 0, null, "ground");
        SoftKit.Field(root, new Vector3(0, 0.02f, 0), W, D, 0.5f, (x, z) => (Mathf.FloorToInt(x / 0.6f) & 1) == 0 ? SoftKit.Wood : Lt(SoftKit.Wood, 0.1f), true, "floor");
        var walls = new GameObject("walls").transform; walls.SetParent(root, false);
        void Wall(Vector3 c, Vector3 size, bool alongX)
        {
            SoftKit.Block(walls, new Vector3(c.x, 0.55f, c.z), new Vector3(size.x, 1.1f, size.z), SoftKit.Pink, 0.05f, true, 0, 0.01f, 0, null, "wall");
            SoftKit.Block(walls, new Vector3(c.x, 1.1f + (H - 1.1f) / 2, c.z), new Vector3(size.x, H - 1.1f, size.z), SoftKit.Cream, 0.05f, true, 0, 0.01f, 0, null, "wall");
            SoftKit.Block(walls, new Vector3(c.x, 1.12f, c.z), alongX ? new Vector3(size.x, 0.1f, size.z + 0.1f) : new Vector3(size.x + 0.1f, 0.1f, size.z), SoftKit.ButterD, 0.04f, false);
            // v0.9.8 skirting board + shoe moulding, wainscot battens on the pink dado, two-step crown moulding
            Vector3 th(float t) => alongX ? new Vector3(size.x, 0, size.z + t) : new Vector3(size.x + t, 0, size.z);
            var sk = th(0.05f); SoftKit.Block(walls, new Vector3(c.x, 0.075f, c.z), new Vector3(sk.x, 0.15f, sk.z), SoftKit.White, 0.015f, false);
            var sm = th(0.075f); SoftKit.Block(walls, new Vector3(c.x, 0.02f, c.z), new Vector3(sm.x, 0.035f, sm.z), Lt(SoftKit.Wood, 0.1f), 0.012f, false);
            var cr1 = th(0.06f); SoftKit.Block(walls, new Vector3(c.x, H - 0.05f, c.z), new Vector3(cr1.x, 0.1f, cr1.z), SoftKit.White, 0.02f, false);
            var cr2 = th(0.11f); SoftKit.Block(walls, new Vector3(c.x, H - 0.015f, c.z), new Vector3(cr2.x, 0.035f, cr2.z), SoftKit.White, 0.012f, false);
            float L = alongX ? size.x : size.z; int nb = Mathf.FloorToInt(L / 0.6f);
            for (int i = 1; i < nb; i++) { float u = -L / 2 + i * L / nb; if (!alongX && c.x > 0 && Mathf.Abs(u) < 1.15f) continue; var bp = alongX ? new Vector3(c.x + u, 0.62f, c.z) : new Vector3(c.x, 0.62f, c.z + u); var bs = alongX ? new Vector3(0.04f, 0.9f, size.z + 0.03f) : new Vector3(size.x + 0.03f, 0.9f, 0.04f); SoftKit.Block(walls, bp, bs, Lt(SoftKit.Pink, 0.12f), 0.01f, false); }
        }
        Wall(new Vector3(0, 0, -D / 2), new Vector3(W, 0, 0.2f), true); Wall(new Vector3(0, 0, D / 2), new Vector3(W, 0, 0.2f), true);
        Wall(new Vector3(-W / 2, 0, 0), new Vector3(0.2f, 0, D), false); Wall(new Vector3(W / 2, 0, 0), new Vector3(0.2f, 0, D), false);
        SoftKit.Block(walls, new Vector3(0, H + 0.1f, 0), new Vector3(W + 0.4f, 0.2f, D + 0.4f), SoftKit.Cream, 0.08f, true, 0, 0.01f, 0, null, "ceiling");
        foreach (var r in walls.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        for (int i = 0; i < 9; i++) { float bx = -3.6f + i * 0.9f, by = 2.5f - 0.12f * Mathf.Sin((i % 3 + 0.5f) / 3f * Mathf.PI); SoftKit.Block(root, new Vector3(bx, by - 0.11f, -D / 2 + 0.13f), new Vector3(0.22f, 0.24f, 0.01f), i % 2 == 0 ? SoftKit.ButterD : SoftKit.Rose, 0.004f, false, 0, -1, 0.92f).transform.localRotation = Quaternion.Euler(0, 0, 180); }   // v0.9.5 bunting flags (were wall balls)
        for (int i = 0; i < 8; i++) { float x0 = -3.6f + i * 0.9f; var sg = SoftKit.Cyl(root, new Vector3(x0 + 0.45f, 2.47f, -D / 2 + 0.125f), 0.008f, 0.92f, SoftKit.White, 0.002f, Quaternion.Euler(0, 0, 90), 5); }
        for (int i = 0; i < 4; i++) { var pz = -2.2f + i * 0.75f; float py = 2.1f + (i % 2) * 0.25f; SoftKit.Block(root, new Vector3(-W / 2 + 0.12f, py, pz), new Vector3(0.03f, 0.36f, 0.3f), SoftKit.Wood, 0.008f, false); SoftKit.Block(root, new Vector3(-W / 2 + 0.135f, py, pz), new Vector3(0.02f, 0.3f, 0.24f), SoftKit.White, 0.003f, false); SoftProps.Picture(root, new Vector3(-W / 2 + 0.147f, py, pz), Quaternion.Euler(0, -90, 0), 0.2f, 0.2f, "art_s" + i); }   // v0.9.8 illustrated prints   // v0.9.5 framed drawings (were wall balls)
        { var pl = SoftKit.Group(root, "sk_plate", new Vector3(2.7f, 1.95f, -D / 2 + 0.1f), 0); SoftKit.Ring(pl, Vector3.zero, Quaternion.identity, 0.72f, 0.07f, SoftKit.WoodD, 28); SoftProps.Picture(pl, new Vector3(0, 0, 0.02f), Quaternion.Euler(0, 180, 0), 1.44f, 1.44f, "art_plate"); }   // v0.9.10 painted wreath plate (was concentric medallion)
        var qc = SoftProps.Curtains(root, new Vector3(2.7f, 1.95f, -D / 2 + 0.2f), 1.5f, 1.6f, SoftKit.PinkD); qc.localRotation = Quaternion.Euler(0, 180, 0);
        var rw = SoftProps.Win(root, new Vector3(W / 2 - 0.12f, 1.6f, 0), 2.0f, 1.2f, SoftKit.White, true); rw.localRotation = Quaternion.Euler(0, 90, 0);
        var rc = SoftProps.Curtains(root, new Vector3(W / 2 - 0.22f, 1.85f, 0), 2.0f, 1.7f, SoftKit.PinkD); rc.localRotation = Quaternion.Euler(0, 90, 0);
        SoftKit.Cyl(root, new Vector3(0, H - 0.2f, 0.4f), 0.015f, 0.4f, SoftKit.GreyD, 0.005f);
        SoftKit.Lath(root, new Vector3(0, H - 0.62f, 0.4f), new[] { new Vector2(0.36f, 0), new Vector2(0.38f, 0.02f), new Vector2(0.34f, 0.08f), new Vector2(0.26f, 0.18f), new Vector2(0.16f, 0.26f), new Vector2(0.06f, 0.3f), new Vector2(0, 0.31f) }, SoftKit.Butter, 24);   // v0.9.7 bell shade with rolled rim (was a 4-point cone)
        SoftKit.Ball(root, new Vector3(0, H - 0.6f, 0.4f), Vector3.one * 0.1f, new Color(1f, 0.95f, 0.8f), 10);
        SoftProps.Bed(root, new Vector3(-W / 2 + 1.1f, 0.02f, D / 2 - 1.2f), 180, 1.4f, SoftKit.Wood, SoftKit.Lav, SoftKit.ButterD);
        SoftProps.Rug(root, new Vector3(0, 0.02f, 0.5f), 1.3f, SoftKit.PinkD, SoftKit.Cream);
        SoftProps.Nightstand(root, new Vector3(-W / 2 + 2.15f, 0.02f, D / 2 - 0.35f), 180, SoftKit.Wood);
        SoftProps.LowShelf(root, new Vector3(2.0f, 0.02f, -D / 2 + 0.28f), 0, 1.1f, Lt(SoftKit.Wood, 0.15f), 11);
        SoftProps.Plant(root, new Vector3(2.35f, 0.99f, -D / 2 + 0.28f), 0.55f);
        SoftProps.WallFrame(root, new Vector3(0.3f, 1.85f, D / 2 - 0.12f), 180, 0.7f, 0.55f, SoftKit.WoodD, 0);
        SoftProps.WallFrame(root, new Vector3(1.25f, 1.75f, D / 2 - 0.12f), 180, 0.5f, 0.62f, SoftKit.Coral, 1);
        SoftProps.ContainerBody(root, "drawer", new Vector3(-W / 2 + 3.4f, 0.02f, D / 2 - 0.4f), 180, SoftKit.Pink);
        SoftProps.ContainerBody(root, "wardrobe", new Vector3(-W / 2 + 0.45f, 0.02f, 1.0f), 90, SoftKit.Wood);
        SoftProps.Plant(root, new Vector3(W / 2 - 0.45f, 0.02f, D / 2 - 0.45f));
        var desk = SoftKit.Group(root, "desk", new Vector3(0, 0.02f, -D / 2 + 0.55f));
        SoftKit.Block(desk, new Vector3(0, 0.76f, 0), new Vector3(1.8f, 0.1f, 0.85f), SoftKit.Wood, 0.05f, true);
        foreach (int sx in new[] { -1, 1 }) SoftKit.Block(desk, new Vector3(sx * 0.7f, 0.36f, 0), new Vector3(0.4f, 0.72f, 0.8f), SoftKit.Pink, 0.08f, true);
        foreach (int sx in new[] { -1, 1 }) SoftKit.Block(desk, new Vector3(sx * 0.7f, 0.45f, 0.415f), new Vector3(0.16f, 0.035f, 0.03f), SoftKit.ButterD, 0.008f, false);
        float ty = 0.83f;
        SoftKit.Block(desk, new Vector3(0, ty, 0), new Vector3(1.2f, 0.02f, 0.7f), SoftKit.Mint, 0.01f, false, 0, 0);
        SoftKit.Block(desk, new Vector3(0, ty + 0.005f, 0), new Vector3(1.15f, 0.02f, 0.08f), SoftKit.Lav, 0.01f, false, 0, 0);
        Color[] pinC = { SoftKit.Coral, SoftKit.ButterD, SoftKit.SkyD, SoftKit.LavD };
        for (int i = 0; i < 4; i++) { var cp = new Vector3(-0.45f + i * 0.1f, ty + 0.03f, 0.22f); SoftKit.Cyl(desk, cp, 0.012f, 0.16f, pinC[i], 0.003f, Quaternion.Euler(0, 15 * i, 90), 6); SoftKit.Lath(desk, cp + Quaternion.Euler(0, 15 * i, 0) * new Vector3(0.08f, 0, 0), new[] { new Vector2(0, 0), new Vector2(0.012f, 0), new Vector2(0, 0.03f) }, pinC[i], 6, false, Quaternion.Euler(0, 15 * i, -90)); }   // v0.9.5 crayons (were balls)
        SoftKit.Lath(desk, new Vector3(0.75f, ty, -0.2f), new[] { new Vector2(0, 0), new Vector2(0.12f, 0), new Vector2(0.03f, 0.05f), new Vector2(0.03f, 0.3f), new Vector2(0.15f, 0.32f), new Vector2(0.08f, 0.48f), new Vector2(0, 0.5f) }, SoftKit.Sky, 14);
        var chair = SoftKit.Group(root, "chair", new Vector3(0, 0.02f, -D / 2 + 1.35f), 180);
        SoftKit.Block(chair, new Vector3(0, 0.48f, 0), new Vector3(0.55f, 0.1f, 0.5f), SoftKit.Rose, 0.05f, true);
        SoftKit.Block(chair, new Vector3(0, 0.85f, -0.22f), new Vector3(0.55f, 0.6f, 0.08f), SoftKit.Rose, 0.08f, false);
        foreach (int sx in new[] { -1, 1 }) foreach (int sz in new[] { -1, 1 }) SoftKit.Cyl(chair, new Vector3(sx * 0.22f, 0.22f, sz * 0.2f), 0.035f, 0.44f, SoftKit.White, 0.015f);
        var poster = SoftKit.Group(root, "map_poster", new Vector3(0, 1.95f, -D / 2 + 0.13f), 0);
        SoftKit.Block(poster, Vector3.zero, new Vector3(2.5f, 1.48f, 0.05f), SoftKit.Wood, 0.04f, false);
        SoftKit.Block(poster, new Vector3(0, 0, 0.03f), new Vector3(2.38f, 1.36f, 0.02f), SoftKit.Cream, 0.006f, false);
        { var mq = SoftProps.Picture(poster, new Vector3(0, 0, 0.042f), Quaternion.Euler(0, 180, 0), 2.3f, 1.3f, "art_map"); var mr = mq.GetComponent<MeshRenderer>(); if (mr) mr.material.SetColor("_BaseColor", new Color(1f, 0.97f, 0.92f));   // v0.9.9 bigger + full-brightness town map
          var sl = new GameObject("map_light").AddComponent<Light>(); sl.transform.SetParent(poster, false); sl.transform.localPosition = new Vector3(0, 0.9f, 0.9f); sl.transform.localRotation = Quaternion.Euler(50, 180, 0); sl.type = LightType.Spot; sl.spotAngle = 80; sl.range = 3.5f; sl.intensity = 2.2f; sl.color = new Color(1f, 0.93f, 0.82f); sl.shadows = LightShadows.None; }   // v0.9.8 illustrated town map (was coloured blocks)
        SoftKit.Block(poster, new Vector3(0.45f, 0.05f, 0.05f), new Vector3(0.04f, 0.04f, 0.02f), SoftKit.Rose, 0.003f, false, 0, -1, 0.9f);   // map pin
        
        Station(root, "deploy", "書桌上的地圖：出發去玩", new Vector3(0, 0.6f, -D / 2 + 0.55f), new Vector3(1.8f, 1.2f, 0.9f), 1.8f);
        var chest = SoftKit.Group(root, "toychest", new Vector3(-W / 2 + 0.55f, 0.02f, -1.2f), 90);
        SoftKit.Block(chest, new Vector3(0, 0.33f, 0), new Vector3(1.4f, 0.64f, 0.8f), SoftKit.Coral, 0.14f, true, 0, 0.02f);
        var lid = SoftKit.Cyl(chest, new Vector3(0, 0.66f, 0), 0.42f, 1.46f, SoftKit.Rose, 0.1f, Quaternion.Euler(0, 0, 90), 20); lid.transform.localScale = new Vector3(0.35f, 1, 1);
        SoftKit.Block(chest, new Vector3(0, 0.5f, 0.41f), new Vector3(0.2f, 0.16f, 0.04f), SoftKit.ButterD, 0.02f, false); SoftKit.Cyl(chest, new Vector3(0, 0.5f, 0.435f), 0.025f, 0.01f, SoftKit.GreyD, 0.003f, Quaternion.Euler(90, 0, 0), 8);   // v0.9.5 latch plate
        foreach (int s in new[] { -1, 1 }) SoftKit.Block(chest, new Vector3(s * 0.45f, 0.33f, 0.41f), new Vector3(0.1f, 0.6f, 0.03f), SoftKit.Butter, 0.03f, false);
        Station(root, "stash", "玩具箱：我的寶貝", new Vector3(-W / 2 + 0.55f, 0.4f, -1.2f), new Vector3(0.9f, 0.9f, 1.5f), 1.8f);
        var st = SoftKit.Group(root, "sidetable", new Vector3(W / 2 - 0.5f, 0.02f, -2.6f), -90);
        SoftKit.Block(st, new Vector3(0, 0.3f, 0), new Vector3(0.6f, 0.6f, 0.5f), SoftKit.Mint, 0.1f, true);
        for (int i = 0; i < 2; i++) { SoftKit.Block(st, new Vector3(0, 0.18f + i * 0.26f, 0.25f), new Vector3(0.5f, 0.2f, 0.03f), Lt(SoftKit.Mint, 0.3f), 0.04f, false); SoftKit.Block(st, new Vector3(0, 0.2f + i * 0.26f, 0.27f), new Vector3(0.14f, 0.03f, 0.03f), SoftKit.ButterD, 0.008f, false); }
        SoftProps.Piggy(root, new Vector3(W / 2 - 0.5f, 0.82f, -2.6f), -90);
        Station(root, "shop", "小豬撲滿：買零食和裝備", new Vector3(W / 2 - 0.5f, 0.6f, -2.6f), new Vector3(0.8f, 1.2f, 0.8f), 1.8f);
        var ct = SoftKit.Group(root, "crafttable", new Vector3(W / 2 - 0.9f, 0.02f, 2.2f));
        SoftKit.Block(ct, new Vector3(0, 0.7f, 0), new Vector3(1.0f, 0.1f, 1.5f), SoftKit.Butter, 0.05f, true);
        foreach (int sx in new[] { -1, 1 }) foreach (int sz in new[] { -1, 1 }) SoftKit.Cyl(ct, new Vector3(sx * 0.4f, 0.33f, sz * 0.62f), 0.05f, 0.66f, SoftKit.Wood, 0.02f);
        float cty = 0.77f;
        Color[] cc = { SoftKit.Coral, SoftKit.MintD, SoftKit.SkyD, SoftKit.ButterD };
        int tools = 1 + S.luck + S.secure; for (int i = 0; i < Mathf.Min(6, tools + 2); i++) SoftKit.Block(root, new Vector3(W / 2 - 1.1f + (i % 3) * 0.2f, cty + 0.06f, 1.8f + (i / 3) * 0.35f), new Vector3(0.06f, 0.12f, 0.25f), cc[i % 4], 0.03f, false);
        SoftKit.Block(root, new Vector3(W / 2 - 0.75f, cty + 0.01f, 2.5f), new Vector3(0.5f, 0.01f, 0.4f), SoftKit.White, 0.004f, false, 0, 0);
        SoftKit.Lath(root, new Vector3(W / 2 - 0.6f, cty, 1.75f), new[] { new Vector2(0, 0), new Vector2(0.12f, 0), new Vector2(0.12f, 0.2f), new Vector2(0, 0.2f) }, SoftKit.Lav, 12);
        Station(root, "upg", "勞作桌：做升級", new Vector3(W / 2 - 0.9f, 0.5f, 2.2f), new Vector3(1.0f, 1.0f, 1.5f), 1.8f);
        float cap = Data.Upg[2].eff[S.shelf];
        var bc = SoftKit.Group(root, "bookcase", new Vector3(-2.4f, 0.02f, -D / 2 + 0.33f));
        SoftKit.Block(bc, new Vector3(0, 1.0f, -0.2f), new Vector3(1.4f, 2.0f, 0.06f), Lt(SoftKit.Lav, 0.3f), 0.03f, true);
        foreach (int sx in new[] { -1, 1 }) SoftKit.Block(bc, new Vector3(sx * 0.67f, 1.0f, 0), new Vector3(0.08f, 2.0f, 0.45f), SoftKit.Wood, 0.04f, true);
        for (int r = 0; r < 4; r++) SoftKit.Block(bc, new Vector3(0, 0.405f + r * 0.476f, 0), new Vector3(1.3f, 0.05f, 0.44f), SoftKit.Wood, 0.02f, false);
        SoftKit.Block(bc, new Vector3(0, 2.02f, 0), new Vector3(1.48f, 0.08f, 0.5f), SoftKit.Wood, 0.04f, false);
        SoftKit.Block(bc, new Vector3(0, 0.1f, 0), new Vector3(1.3f, 0.2f, 0.44f), SoftKit.Wood, 0.04f, false);
        for (int i = 0; i < S.trophies.Count; i++)
        {
            var it = S.trophies[i]; int row = i / 3, col = i % 3; var p = new Vector3(-3.1f + 0.25f + col * 0.45f, 0.45f + row * 0.476f, -D / 2 + 0.38f);
            var th = new GameObject("trophy").transform; th.SetParent(root, false); th.localPosition = p; ItemArt.Build(th, it, out _);
        }
        Station(root, "stash", "展示架（" + S.trophies.Count + "/" + cap + "）", new Vector3(-2.4f, 1.0f, -D / 2 + 0.35f), new Vector3(1.4f, 2f, 0.6f), 1.8f);
        SoftProps.Teddy(root, new Vector3(-W / 2 + 1.1f, 0.74f, D / 2 - 0.75f), 160, 1.0f);
        Station(root, "quest", "熊熊：今天的任務" + (S.hasQuest && S.quest != null && S.quest.done ? "（可以領獎！）" : ""), new Vector3(-W / 2 + 1.1f, 0.9f, D / 2 - 0.9f), new Vector3(0.6f, 0.9f, 0.6f), 1.8f);
        SoftProps.ToyBlocks(root, new Vector3(-1.35f, 0.02f, 2.35f), 7, new System.Random(7));
        Art.Toonify(root.gameObject, 0.01f);
        spawn = new Vector3(0.8f, 0.1f, 2.2f); yaw = 200;
        return root;
    }
    static void Station(Transform root, string id, string label, Vector3 c, Vector3 size, float reach)
    {
        var g = new GameObject("station_" + id); g.transform.SetParent(root, false); g.transform.localPosition = c;
        var bc = g.AddComponent<BoxCollider>(); bc.size = size; bc.isTrigger = true;
        var s = g.AddComponent<Station>(); s.id = id; s.label = label; s.reach = reach + Mathf.Max(size.x, size.z) * 0.5f;
    }
}
