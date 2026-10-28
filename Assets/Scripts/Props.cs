using UnityEngine;
// procedural props for STEP 5: register, desk, server rack, drinks cooler, cubicle partition, pallet racking
public static class Props
{
    static Material M(Color c, string tex = null, float sm = 0.3f, float me = 0f) => Art.Lit(c, tex, sm, me);
    static GameObject B(Transform p, Vector3 c, Vector3 s, Material m, bool col = false, float uv = 1f) => Art.Box(p, c, s, m, col, uv);

    public static void Register(Transform g)
    {
        var body = M(new Color(0.16f, 0.17f, 0.19f), null, 0.55f, 0.2f); var light = M(new Color(0.82f, 0.83f, 0.84f), null, 0.4f);
        B(g, new Vector3(0, 0.06f, 0), new Vector3(0.48f, 0.12f, 0.42f), light, true);                       // cash drawer
        B(g, new Vector3(0, 0.121f, 0.211f), new Vector3(0.3f, 0.02f, 0.005f), body);                        // drawer slot
        var kb = B(g, new Vector3(0, 0.16f, 0.06f), new Vector3(0.36f, 0.06f, 0.26f), body); kb.transform.localRotation = Quaternion.Euler(-12, 0, 0);
        var keyM = M(new Color(0.9f, 0.9f, 0.88f), null, 0.3f); var keyR = M(new Color(0.85f, 0.2f, 0.2f), null, 0.3f); var keyG = M(new Color(0.2f, 0.7f, 0.3f), null, 0.3f);
        for (int r = 0; r < 4; r++) for (int c = 0; c < 4; c++) { var k = B(kb.transform, new Vector3(-0.11f + c * 0.06f, 0.034f, -0.08f + r * 0.05f), new Vector3(0.04f, 0.012f, 0.035f), c == 3 ? (r == 0 ? keyG : keyR) : keyM); }
        B(g, new Vector3(0.0f, 0.24f, -0.12f), new Vector3(0.05f, 0.14f, 0.05f), body);                      // screen post
        var scr = B(g, new Vector3(0, 0.36f, -0.12f), new Vector3(0.34f, 0.22f, 0.03f), body); scr.transform.localRotation = Quaternion.Euler(-15, 0, 0);
        B(scr.transform, new Vector3(0, 0.005f, 0.016f), new Vector3(0.3f, 0.18f, 0.004f), Art.Emissive(new Color(0.3f, 0.75f, 1f), 1.6f));
        B(scr.transform, new Vector3(0, 0, -0.016f), new Vector3(0.26f, 0.08f, 0.004f), Art.Emissive(new Color(0.3f, 1f, 0.5f), 1.4f)); // customer-facing price display
        B(g, new Vector3(0.18f, 0.17f, -0.1f), new Vector3(0.12f, 0.1f, 0.14f), light);                       // receipt printer
        B(g, new Vector3(0.18f, 0.225f, -0.07f), new Vector3(0.07f, 0.004f, 0.06f), M(Color.white, null, 0.1f));
    }

    public static void Desk(Transform g, System.Random rng)
    {
        var wood = M(new Color(0.9f, 0.82f, 0.7f), "deskwood", 0.35f); var grey = M(new Color(0.42f, 0.44f, 0.47f), "metal", 0.45f, 0.5f); var dark = M(new Color(0.08f, 0.08f, 0.09f), null, 0.5f);
        B(g, new Vector3(0, 0.74f, 0), new Vector3(1.5f, 0.04f, 0.75f), wood, true, 0.6f);
        B(g, new Vector3(0.5f, 0.36f, -0.02f), new Vector3(0.42f, 0.7f, 0.66f), grey, true);                 // drawer pedestal
        for (int i = 0; i < 3; i++) { B(g, new Vector3(0.5f, 0.15f + i * 0.22f, 0.315f), new Vector3(0.38f, 0.19f, 0.01f), grey); B(g, new Vector3(0.5f, 0.2f + i * 0.22f, 0.323f), new Vector3(0.12f, 0.015f, 0.012f), dark); }
        foreach (float z in new[] { -0.3f, 0.3f }) B(g, new Vector3(-0.68f, 0.36f, z), new Vector3(0.05f, 0.72f, 0.05f), grey);
        B(g, new Vector3(0, 0.45f, -0.35f), new Vector3(1.4f, 0.5f, 0.02f), wood, false, 0.6f);               // modesty panel
        // monitor + keyboard + mug
        B(g, new Vector3(-0.15f, 0.77f, -0.2f), new Vector3(0.2f, 0.02f, 0.16f), dark); B(g, new Vector3(-0.15f, 0.86f, -0.22f), new Vector3(0.04f, 0.18f, 0.04f), dark);
        var mon = B(g, new Vector3(-0.15f, 1.03f, -0.2f), new Vector3(0.58f, 0.34f, 0.03f), dark);
        bool on = rng.NextDouble() < 0.7; B(mon.transform, new Vector3(0, 0.005f, 0.016f), new Vector3(0.54f, 0.3f, 0.004f), on ? Art.Emissive(rng.NextDouble() < 0.5 ? new Color(0.35f, 0.6f, 1f) : new Color(0.85f, 0.9f, 1f), 1.3f) : M(new Color(0.03f, 0.03f, 0.04f), null, 0.9f));
        B(g, new Vector3(-0.15f, 0.77f, 0.12f), new Vector3(0.44f, 0.02f, 0.14f), M(new Color(0.2f, 0.2f, 0.22f), null, 0.3f));
        B(g, new Vector3(0.3f, 0.77f, 0.1f), new Vector3(0.07f, 0.02f, 0.1f), M(new Color(0.2f, 0.2f, 0.22f), null, 0.3f));
        if (rng.NextDouble() < 0.6) Art.Prim(PrimitiveType.Cylinder, g, new Vector3(0.5f, 0.81f, -0.1f), new Vector3(0.08f, 0.05f, 0.08f), M(rng.NextDouble() < 0.5 ? Color.white : new Color(0.8f, 0.3f, 0.2f), null, 0.6f));
        // paper stack
        B(g, new Vector3(0.45f, 0.775f, -0.2f), new Vector3(0.22f, 0.03f, 0.3f), M(new Color(0.96f, 0.96f, 0.94f), null, 0.1f));
        // chair
        var ch = M(new Color(0.15f, 0.16f, 0.2f), null, 0.3f); float cz = 0.62f;
        B(g, new Vector3(-0.1f, 0.47f, cz), new Vector3(0.48f, 0.08f, 0.46f), ch); B(g, new Vector3(-0.1f, 0.82f, cz + 0.22f), new Vector3(0.46f, 0.6f, 0.06f), ch);
        B(g, new Vector3(-0.1f, 0.24f, cz), new Vector3(0.05f, 0.42f, 0.05f), grey); B(g, new Vector3(-0.1f, 0.04f, cz), new Vector3(0.55f, 0.04f, 0.06f), grey); B(g, new Vector3(-0.1f, 0.04f, cz), new Vector3(0.06f, 0.04f, 0.55f), grey);
    }

    public static void ServerRack(Transform g, System.Random rng)
    {
        var body = M(new Color(0.06f, 0.065f, 0.075f), "metal", 0.45f, 0.6f); var unit = M(new Color(0.18f, 0.19f, 0.21f), "metal", 0.5f, 0.6f); var vent = M(new Color(0.02f, 0.02f, 0.025f), null, 0.2f);
        B(g, new Vector3(0, 1.0f, 0), new Vector3(0.62f, 2.0f, 0.95f), body, true);
        B(g, new Vector3(0, 2.01f, 0), new Vector3(0.64f, 0.03f, 0.97f), M(new Color(0.25f, 0.27f, 0.3f), "metal", 0.5f, 0.7f));
        Color[] led = { new Color(0.2f, 1f, 0.4f), new Color(0.3f, 0.6f, 1f), new Color(1f, 0.65f, 0.1f) };
        for (int i = 0; i < 14; i++)
        {
            float y = 0.18f + i * 0.12f; bool blank = rng.NextDouble() < 0.18;
            B(g, new Vector3(0, y, 0.48f), new Vector3(0.54f, 0.1f, 0.02f), blank ? vent : unit);
            if (blank) continue;
            B(g, new Vector3(-0.08f, y, 0.492f), new Vector3(0.26f, 0.05f, 0.005f), vent);   // drive bays / vent
            int n = 1 + rng.Next(3); for (int k = 0; k < n; k++) B(g, new Vector3(0.15f + k * 0.035f, y + 0.02f, 0.492f), new Vector3(0.018f, 0.012f, 0.004f), Art.Emissive(led[rng.Next(led.Length)], 3.5f));
        }
        B(g, new Vector3(0, 1.0f, 0.5f), new Vector3(0.6f, 0.03f, 0.01f), vent);
        foreach (float sx in new[] { -0.29f, 0.29f }) B(g, new Vector3(sx, 1.0f, 0.49f), new Vector3(0.03f, 1.96f, 0.03f), M(new Color(0.3f, 0.32f, 0.36f), "metal", 0.5f, 0.7f));
    }

    static readonly Color[] brand = { new Color(0.85f, 0.12f, 0.12f), new Color(0.1f, 0.35f, 0.8f), new Color(0.12f, 0.55f, 0.25f) };
    public static void Cooler(Transform g, System.Random rng)
    {
        var bc = brand[rng.Next(brand.Length)]; var side = M(bc, "metal", 0.6f, 0.3f); var frame = M(new Color(0.12f, 0.12f, 0.13f), "metal", 0.6f, 0.5f);
        B(g, new Vector3(0, 0.975f, -0.05f), new Vector3(0.8f, 1.95f, 0.75f), side, true);
        B(g, new Vector3(0, 0.95f, 0.31f), new Vector3(0.68f, 1.5f, 0.01f), Art.Emissive(new Color(0.9f, 0.95f, 1f), 1.2f)); // lit back wall
        Color[] can = { new Color(0.9f, 0.1f, 0.1f), new Color(0.95f, 0.8f, 0.1f), new Color(0.1f, 0.6f, 0.95f), new Color(0.2f, 0.75f, 0.3f), new Color(0.95f, 0.5f, 0.1f), Color.white, new Color(0.5f, 0.2f, 0.6f) };
        var shelfM = M(new Color(0.7f, 0.72f, 0.74f), "metal", 0.5f, 0.6f);
        for (int s = 0; s < 5; s++)
        {
            float y = 0.25f + s * 0.32f; B(g, new Vector3(0, y, 0.33f), new Vector3(0.68f, 0.015f, 0.04f), shelfM);
            var cm = M(can[rng.Next(can.Length)], null, 0.75f, 0.4f); bool tall = rng.NextDouble() < 0.5; float h = tall ? 0.24f : 0.13f; float w = tall ? 0.075f : 0.065f;
            for (int k = 0; k < 8; k++) if (rng.NextDouble() < 0.88) B(g, new Vector3(-0.3f + k * 0.085f, y + h / 2 + 0.01f, 0.36f), new Vector3(w, h, w), k % 3 == 2 ? M(can[rng.Next(can.Length)], null, 0.75f, 0.4f) : cm);
        }
        // door frames (glass is left open so the stock reads) + handles
        foreach (float sx in new[] { -0.38f, 0f, 0.38f }) B(g, new Vector3(sx, 0.95f, 0.36f), new Vector3(0.04f, 1.6f, 0.04f), frame);
        B(g, new Vector3(0, 0.16f, 0.36f), new Vector3(0.8f, 0.1f, 0.05f), frame); B(g, new Vector3(0, 1.74f, 0.36f), new Vector3(0.8f, 0.05f, 0.05f), frame);
        foreach (float sx in new[] { -0.05f, 0.05f }) B(g, new Vector3(sx, 1.0f, 0.4f), new Vector3(0.02f, 0.4f, 0.03f), M(new Color(0.8f, 0.8f, 0.82f), null, 0.8f, 1f));
        B(g, new Vector3(0, 1.85f, 0.31f), new Vector3(0.8f, 0.2f, 0.04f), Art.Emissive(bc, 2.2f));                 // brand header
        B(g, new Vector3(0, 1.85f, 0.335f), new Vector3(0.4f, 0.07f, 0.01f), Art.Emissive(Color.white, 2.5f));
    }

    public static void Partition(Transform h, Vector3 c, float len)
    {
        B(h, c + new Vector3(0, 0.65f, 0), new Vector3(len, 1.3f, 0.06f), M(new Color(0.42f, 0.48f, 0.56f), "carpet", 0.05f), true, 1f);
        B(h, c + new Vector3(0, 1.31f, 0), new Vector3(len + 0.02f, 0.03f, 0.08f), M(new Color(0.75f, 0.76f, 0.78f), "metal", 0.5f, 0.6f));
    }

    public static void PalletRack(Transform h, Vector3 pos, float rot, System.Random rng)
    {
        var g = new GameObject("PalletRack").transform; g.SetParent(h, false); g.localPosition = pos; g.localRotation = Quaternion.Euler(0, rot, 0);
        var up = M(new Color(0.95f, 0.45f, 0.08f), "metal", 0.4f, 0.5f); var beam = M(new Color(0.12f, 0.3f, 0.7f), "metal", 0.4f, 0.5f); var deck = M(new Color(0.55f, 0.56f, 0.58f), "metal", 0.4f, 0.6f);
        float L = 2.7f, D = 1.0f, H = 2.65f;
        foreach (float x in new[] { -L / 2, L / 2 }) foreach (float z in new[] { -D / 2, D / 2 }) B(g, new Vector3(x, H / 2, z), new Vector3(0.08f, H, 0.08f), up);
        foreach (float x in new[] { -L / 2, L / 2 }) for (int k = 0; k < 4; k++) { var br = B(g, new Vector3(x, 0.35f + k * 0.62f, 0), new Vector3(0.03f, 0.03f, D * 1.2f), up); br.transform.localRotation = Quaternion.Euler((k % 2 == 0 ? 1 : -1) * 32, 0, 0); }
        float[] lv = { 0.12f, 1.0f, 1.88f };
        foreach (float y in lv)
        {
            foreach (float z in new[] { -D / 2, D / 2 }) B(g, new Vector3(0, y, z), new Vector3(L, 0.1f, 0.06f), beam);
            B(g, new Vector3(0, y + 0.04f, 0), new Vector3(L - 0.05f, 0.02f, D), deck);
            // load this level
            float x = -L / 2 + 0.1f;
            while (x < L / 2 - 0.4f)
            {
                double t = rng.NextDouble(); var p = new Vector3(0, y + 0.06f, 0);
                if (t < 0.4 && x + 1.26f < L / 2) { Art.Spawn("PH/wooden_military_crate", g, p + new Vector3(x + 0.63f, 0, 0), (float)rng.NextDouble() * 6 - 3, new Vector3(1.24f, 0, 0), false); if (y < 1.5f && rng.NextDouble() < 0.5) Art.Spawn("PH/wooden_military_crate", g, p + new Vector3(x + 0.63f, 0.47f, 0), (float)rng.NextDouble() * 8 - 4, new Vector3(1.24f, 0, 0), false); x += 1.32f; }
                else if (t < 0.6 && x + 1.18f < L / 2) { Art.Spawn("PH/wooden_crate_02", g, p + new Vector3(x + 0.59f, 0, 0), 0, new Vector3(1.16f, 0, 0), false); x += 1.24f; }
                else if (t < 0.85) { for (int k = 0; k < 4; k++) Art.Spawn("PH/ammo_box", g, p + new Vector3(x + 0.08f + k * 0.13f, 0, (float)rng.NextDouble() * 0.3f - 0.15f), 90, new Vector3(0, 0.26f, 0), false); x += 0.6f; }
                else x += 0.5f;
            }
        }
        var col = new GameObject("rackCol"); col.transform.SetParent(g, false); col.transform.localPosition = new Vector3(0, H / 2, 0); col.AddComponent<BoxCollider>().size = new Vector3(L + 0.1f, H, D + 0.1f);
    }
}
