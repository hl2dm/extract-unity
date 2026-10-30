using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using static SoftKit;

/// 阿姨家 — the one fully hand-designed SoftKit map (art_bible + concepts 01/02/07).
public static partial class LevelGen
{
    struct Op { public bool ax; public float fc, c, w, y0, y1; }
    public const float AWH = 3.0f, AWT = 0.24f, FY = 0.12f;
    static Color Lite(Color c, float k) => Color.Lerp(c, Color.white, k);
    static Color k2c(float x) => new[] { Rose, ButterD, Coral }[Mathf.Abs(Mathf.RoundToInt(x)) % 3];
    static void NoShadow(GameObject g) { foreach (var r in g.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = ShadowCastingMode.Off; }

    static void SWall(Transform h, bool ax, float wallFc, float off, float a0, float a1, float th, Color c, List<Op> ops, bool col, float H = AWH)
    {
        var holes = ops.FindAll(o => o.ax == ax && Mathf.Abs(o.fc - wallFc) < 0.01f && o.c + o.w / 2 > a0 && o.c - o.w / 2 < a1);
        holes.Sort((a, b) => a.c.CompareTo(b.c));
        float fixedC = wallFc + off, cur = a0;
        void Piece(float s, float e, float ya, float yb)
        {
            if (e - s < 0.01f || yb - ya < 0.01f) return; float mid = (s + e) / 2;
            var pos = ax ? new Vector3(mid, FY + (ya + yb) / 2, fixedC) : new Vector3(fixedC, FY + (ya + yb) / 2, mid);
            var sz = ax ? new Vector3(e - s, yb - ya, th) : new Vector3(th, yb - ya, e - s);
            Block(h, pos, sz, c, Mathf.Min(0.05f, th * 0.3f), col, 0, 0.01f, 0, null, "sk_wall");
        }
        foreach (var o in holes)
        {
            float hs = Mathf.Max(a0, o.c - o.w / 2), he = Mathf.Min(a1, o.c + o.w / 2);
            Piece(cur, hs, 0, H); if (o.y0 > 0) Piece(hs, he, 0, o.y0); if (o.y1 < H) Piece(hs, he, o.y1, H); cur = he;
        }
        Piece(cur, a1, 0, H);
    }


    public static bool LegacyTrim;              // -legacytrim: rebuild the pre-0.4.2 unsplit band (before/after shots only)
    public static int TrimCrossBefore, TrimPieces;
    public static readonly List<Bounds> Openings = new List<Bounds>();
    public static readonly List<Bounds> NoRide = new List<Bounds>();
    /// horizontal wainscot/trim band along a wall, split around every door/window opening on that wall line
    static void Band(Transform h, bool ax, float wallFc, float fixedC, float a0, float a1, float y, Color c, List<Op> ops)
    {
        const float bh = 0.08f, bt = 0.05f, gap = 0.18f;
        var holes = ops.FindAll(o => o.ax == ax && Mathf.Abs(o.fc - wallFc) < 0.01f && o.y0 <= y + bh && o.y1 >= y - bh && o.c + o.w / 2 + gap > a0 && o.c - o.w / 2 - gap < a1);
        holes.Sort((a, b) => a.c.CompareTo(b.c));
        TrimCrossBefore += holes.Count;
        void Piece(float s0, float e0)
        {
            if (e0 - s0 < 0.06f) return; float mid = (s0 + e0) / 2; TrimPieces++;
            Block(h, ax ? new Vector3(mid, FY + y, fixedC) : new Vector3(fixedC, FY + y, mid), ax ? new Vector3(e0 - s0, bh, bt) : new Vector3(bt, bh, e0 - s0), c, 0.02f, false, 0, -1, 0, null, "sk_trimbar");
        }
        if (LegacyTrim) { Piece(a0, a1); return; }
        float cur = a0;
        foreach (var o in holes) { Piece(cur, o.c - o.w / 2 - gap); cur = Mathf.Max(cur, o.c + o.w / 2 + gap); }
        Piece(cur, a1);
    }
    /// counts thin horizontal bar/trim renderers whose bounds cross a door/window opening (should be 0)
    public static int TrimDoorHits(Transform root, bool log = true)
    {
        int n = 0; if (!root) return 0;
        foreach (var r in root.GetComponentsInChildren<Renderer>())
        {
            if (!r.enabled || !r.gameObject.activeInHierarchy) continue;
            var b = r.bounds; float len = Mathf.Max(b.size.x, b.size.z);
            if (b.size.y > 0.35f || len < 0.5f || len < b.size.y * 3) continue;
            bool skip = false;
            for (var q = r.transform; q && q != root; q = q.parent) { var nm = q.name; if (nm == "Door" || nm == "LockedDoor" || nm == "sk_grille" || nm == "sk_curtain" || nm == "flowerbox" || q.GetComponent<Interactable>()) { skip = true; break; } }
            if (skip) continue;
            foreach (var o in Openings) if (o.Intersects(b)) { n++; if (log) Debug.Log("[trimcheck] " + r.name + " " + b.ToString("F2") + " crosses opening " + o.ToString("F2")); break; }
        }
        return n;
    }

    class ARoom { public string name; public float x0, x1, z0, z1; public Color wall, trim; public System.Func<float, float, Color> floor; public Vector3 C => new Vector3((x0 + x1) / 2, FY, (z0 + z1) / 2); }

    static LootContainer SCont(Transform parent, string kind, Vector3 lp, float yaw, Color accent, string label = null, bool special = false, List<LootContainer> bucket = null)
    {
        var g = SoftProps.ContainerBody(parent, kind, lp, yaw, accent, label);
        var lc = g.AddComponent<LootContainer>(); lc.Init(kind, rng, L.theme, L.vmult, luck, label); lc.special = special; L.containers.Add(lc); bucket?.Add(lc); return lc;
    }

    static Door SDoor(Transform h, float fc, float c, float w, float hgt, Color col, Color trim, bool locked)
    {
        var dg = new GameObject(locked ? "LockedDoor" : "Door"); dg.transform.SetParent(h, false); dg.transform.localPosition = new Vector3(c - w / 2, FY, fc);
        var hinge = new GameObject("hinge").transform; hinge.SetParent(dg.transform, false);
        var leaf = Block(hinge, new Vector3(w / 2, hgt / 2, 0), new Vector3(w - 0.05f, hgt - 0.04f, 0.1f), col, 0.05f, true, 0, 0.01f, 0, null, "doorleaf");
        foreach (int s in new[] { -1, 1 })
        {
            Block(hinge, new Vector3(w / 2, hgt * 0.32f, s * 0.06f), new Vector3(w * 0.62f, hgt * 0.32f, 0.03f), Lite(col, 0.3f), 0.06f, false);
            Ring(hinge, new Vector3(w / 2, hgt * 0.72f, s * 0.06f), Quaternion.identity, w * 0.2f, 0.035f, trim, 20);
            Ball(hinge, new Vector3(w / 2, hgt * 0.72f, s * 0.055f), new Vector3(w * 0.36f, w * 0.36f, 0.02f), Sky, 14);
            Ball(hinge, new Vector3(w - 0.18f, hgt * 0.47f, s * 0.08f), Vector3.one * 0.11f, ButterD, 8);
        }
        if (locked)
        {   // big heart padlock + butter bands so the locked room reads from afar
            foreach (int s in new[] { -1, 1 }) { Block(hinge, new Vector3(w / 2, hgt * 0.5f, s * 0.07f), new Vector3(w - 0.1f, 0.12f, 0.03f), ButterD, 0.03f, false); Ball(hinge, new Vector3(w / 2, hgt * 0.5f, s * 0.11f), new Vector3(0.32f, 0.3f, 0.1f), Rose, 12); Ring(hinge, new Vector3(w / 2, hgt * 0.5f + 0.18f, s * 0.11f), Quaternion.identity, 0.1f, 0.03f, ButterD, 14); }
        }
        var d = dg.AddComponent<Door>(); d.hinge = hinge; d.locked = locked; d.col = leaf.GetComponent<Collider>(); d.reach = 2.4f;
        return d;
    }

    /// frame trim around an opening (both faces)
    static void Trim(Transform h, Op o, Color c, bool arch)
    {
        float d = AWT + 0.08f, ym = (o.y0 + o.y1) / 2;
        System.Func<float, float, Vector3> P = (a, y) => o.ax ? new Vector3(a, FY + y, o.fc) : new Vector3(o.fc, FY + y, a);
        System.Func<float, float, Vector3> S = (a, y) => o.ax ? new Vector3(a, y, d) : new Vector3(d, y, a);
        foreach (int s in new[] { -1, 1 }) Block(h, P(o.c + s * (o.w / 2 + 0.06f), ym), S(0.14f, o.y1 - o.y0 + 0.1f), c, 0.05f, false);
        Block(h, P(o.c, o.y1 + 0.06f), S(o.w + 0.26f, 0.14f), c, 0.05f, false);
        if (o.y0 > 0) Block(h, P(o.c, o.y0 - 0.05f), S(o.w + 0.36f, 0.12f) + (o.ax ? new Vector3(0, 0, 0.12f) : new Vector3(0.12f, 0, 0)), c, 0.05f, false);
        foreach (int s in new[] { -1, 1 }) Ball(h, P(o.c + s * (o.w / 2 + 0.06f), o.y1 + 0.06f), Vector3.one * 0.24f, c, 10);   // v0.5 soft corner rosettes
        if (arch)
        {   // scalloped crown over arches
            var r = Ring(h, P(o.c, o.y1 + 0.06f), o.ax ? Quaternion.identity : Quaternion.Euler(0, 90, 0), o.w * 0.3f, 0.07f, c, 22);
            r.transform.localScale = new Vector3(1, 0.55f, 1);
        }
    }

    static void Window(Transform h, List<Op> ops, bool ax, float fc, float c, float w, float y0, float y1, float outYaw, Vector3 outDir, Color curtain, bool flowers)
    {
        var o = new Op { ax = ax, fc = fc, c = c, w = w, y0 = y0, y1 = y1 }; ops.Add(o);
        var ctr = ax ? new Vector3(c, FY + (y0 + y1) / 2, fc) : new Vector3(fc, FY + (y0 + y1) / 2, c);
        var gr = SoftProps.Grille(h, ctr + outDir * (AWT / 2 + 0.03f), w + 0.1f, y1 - y0 + 0.1f, White); gr.localRotation = Quaternion.Euler(0, outYaw, 0);
        var cu = SoftProps.Curtains(h, ctr - outDir * (AWT / 2 + 0.1f) + Vector3.up * 0.05f, w, y1 - y0 + 0.2f, curtain); cu.localRotation = Quaternion.Euler(0, outYaw, 0);
        var blk = new GameObject("winblock"); blk.transform.SetParent(h, false); blk.transform.localPosition = ctr; blk.AddComponent<BoxCollider>().size = ax ? new Vector3(w, y1 - y0, AWT) : new Vector3(AWT, y1 - y0, w);
        if (flowers)
        {
            var fb = Group(h, "flowerbox", ctr + outDir * (AWT / 2 + 0.22f) - Vector3.up * ((y1 - y0) / 2 + 0.14f), outYaw);
            Block(fb, Vector3.zero, new Vector3(w + 0.2f, 0.26f, 0.32f), WoodD, 0.08f, false);
            for (int i = 0; i < 4; i++) Goods.Shrub(fb, new Vector3(-w / 2 + 0.12f + i * (w - 0.24f) / 3f, 0.12f, 0), 0.16f, 0.24f, i % 2 == 0 ? LeafD : Leaf, i % 2 == 0 ? Rose : ButterD, 31 + i);   // v0.9.4: real flower box
        }
    }

    static Transform AuntHouse(Transform root, Vector3 origin, List<ARoom> rooms, List<LootContainer> lockedC, List<Vector3> petSpots)
    {
        var h = new GameObject("House").transform; h.SetParent(root, false); h.localPosition = origin;
        var wallOut = Pink; var trim = White; var ops = new List<Op>();
        System.Func<float, float, Color> wood = (x, z) => Color.Lerp((Mathf.FloorToInt(x / 0.55f) & 1) == 0 ? Wood : Lite(Wood, 0.1f), WoodD, 0.28f * VN(new Vector3(x * 0.5f, 0, z * 0.5f), 3));
        System.Func<float, float, Color> Tiles(Color a, Color b) => (x, z) => ((Mathf.FloorToInt(x / 0.6f) + Mathf.FloorToInt(z / 0.6f)) & 1) == 0 ? a : b;
        rooms.Add(new ARoom { name = "garage", x0 = -9, x1 = -3, z0 = 0, z1 = 6, wall = Lite(Sky, 0.45f), trim = SkyD, floor = Tiles(Lite(Grey, 0.55f), Lite(Grey, 0.4f)) });
        rooms.Add(new ARoom { name = "living", x0 = -3, x1 = 3, z0 = 0, z1 = 6, wall = Lite(Peach, 0.3f), trim = Rose, floor = wood });
        rooms.Add(new ARoom { name = "kitchen", x0 = 3, x1 = 9, z0 = 0, z1 = 6, wall = Lite(Mint, 0.35f), trim = MintD, floor = Tiles(Mint, White) });
        rooms.Add(new ARoom { name = "bath", x0 = -9, x1 = -3, z0 = 6, z1 = 12, wall = Lite(Sky, 0.25f), trim = SkyD, floor = Tiles(Sky, White) });
        rooms.Add(new ARoom { name = "bedroom", x0 = -3, x1 = 3, z0 = 6, z1 = 12, wall = Lite(Lav, 0.35f), trim = LavD, floor = (x, z) => (Mathf.FloorToInt(z / 0.55f) & 1) == 0 ? Lite(Wood, 0.22f) : Lite(Wood, 0.32f) });
        rooms.Add(new ARoom { name = "aunt", x0 = 3, x1 = 9, z0 = 6, z1 = 12, wall = Lite(Pink, 0.35f), trim = PinkD, floor = Tiles(Lav, Lite(Lav, 0.5f)) });

        // ---- openings
        ops.Add(new Op { ax = true, fc = 0, c = -6, w = 4.4f, y0 = 0, y1 = 2.5f });            // garage mouth
        ops.Add(new Op { ax = true, fc = 0, c = 0, w = 1.3f, y0 = 0, y1 = 2.4f });            // front door
        ops.Add(new Op { ax = false, fc = -3, c = 1.6f, w = 1.3f, y0 = 0, y1 = 2.3f });         // garage<->living
        ops.Add(new Op { ax = false, fc = 3, c = 1.6f, w = 1.3f, y0 = 0, y1 = 2.3f });          // living<->kitchen
        ops.Add(new Op { ax = true, fc = 6, c = -6, w = 1.4f, y0 = 0, y1 = 2.3f });            // garage->bath
        ops.Add(new Op { ax = true, fc = 6, c = 1.8f, w = 1.3f, y0 = 0, y1 = 2.3f });          // living->bedroom
        ops.Add(new Op { ax = true, fc = 6, c = 6, w = 1.3f, y0 = 0, y1 = 2.3f });             // kitchen->aunt (locked)
        Window(h, ops, true, 0, 6, 2.0f, 1.0f, 2.4f, 0, Vector3.back, PinkD, true);            // kitchen front
        Window(h, ops, true, 12, -6, 1.2f, 1.6f, 2.5f, 180, Vector3.forward, Sky, false);       // bath back
        Window(h, ops, true, 12, 0, 2.0f, 1.0f, 2.4f, 180, Vector3.forward, Pink, false);       // bedroom back
        Window(h, ops, true, 12, 6, 2.0f, 1.0f, 2.4f, 180, Vector3.forward, Rose, false);       // aunt back
        Window(h, ops, false, -9, 3, 1.6f, 1.2f, 2.4f, 90, Vector3.left, Butter, false);       // garage side
        Window(h, ops, false, 9, 3, 1.6f, 1.05f, 2.4f, -90, Vector3.right, PinkD, false);      // kitchen side
        Window(h, ops, false, 9, 9, 1.6f, 1.0f, 2.4f, -90, Vector3.right, Rose, false);        // aunt side

        // ---- plinth, floors, ceiling
        Block(h, new Vector3(0, 0.055f, 6), new Vector3(18.5f, 0.11f, 12.5f), LavD, 0.04f, true, 0, 0.01f);
        Block(h, new Vector3(0, 0.06f, -0.55f), new Vector3(2.6f, 0.12f, 0.9f), Lite(LavD, 0.3f), 0.05f, true);
        foreach (var r in rooms) Field(h, new Vector3((r.x0 + r.x1) / 2, FY, (r.z0 + r.z1) / 2), r.x1 - r.x0, r.z1 - r.z0, 0.55f, r.floor, true, "floor_" + r.name);
        var ceil = Block(h, new Vector3(0, FY + AWH + 0.07f, 6), new Vector3(18.4f, 0.14f, 12.4f), Cream, 0.05f, true, 0, 0.01f); NoShadow(ceil);
        // toy-diorama massing: fat rounded corner pillars + pillowy cornice + chunky base bumper (visual only)
        foreach (var cx in new[] { -9.2f, 9.2f }) foreach (var cz in new[] { -0.2f, 12.2f }) { Cyl(h, new Vector3(cx, FY + AWH / 2 + 0.2f, cz), 0.8f, AWH + 0.6f, Butter, 0.35f, null, 24); Ball(h, new Vector3(cx, FY + AWH + 0.6f, cz), new Vector3(1.5f, 0.8f, 1.5f), Rose, 16); }
        Block(h, new Vector3(0, FY + AWH + 0.35f, -0.35f), new Vector3(18.6f, 0.6f, 0.7f), White, 0.3f, false);
        foreach (var px in new[] { -3.05f, 3.05f }) { Cyl(h, new Vector3(px, FY + AWH / 2, -0.3f), 0.5f, AWH, Butter, 0.25f, null, 20); Ball(h, new Vector3(px, FY + AWH + 0.15f, -0.3f), new Vector3(1.1f, 0.6f, 1.1f), Mint, 14); }
        Block(h, new Vector3(4.6f, 0.3f, -0.35f), new Vector3(8.2f, 0.6f, 0.6f), LavD, 0.28f, false);

        // ---- structural walls (outer pink, partitions cream) + room skins
        float e = AWT / 2;
        SWall(h, true, 0, 0, -9 - e, 9 + e, AWT, wallOut, ops, true);
        SWall(h, true, 12, 0, -9 - e, 9 + e, AWT, wallOut, ops, true);
        SWall(h, false, -9, 0, 0, 12, AWT, wallOut, ops, true);
        SWall(h, false, 9, 0, 0, 12, AWT, wallOut, ops, true);
        SWall(h, false, -3, 0, e, 12 - e, 0.18f, Cream, ops, true);
        SWall(h, false, 3, 0, e, 12 - e, 0.18f, Cream, ops, true);
        SWall(h, true, 6, 0, -9 + e, 9 - e, 0.18f, Cream, ops, true);
        foreach (var r in rooms)
        {
            float so = 0.11f + 0.03f, ins = 0.13f;
            SWall(h, true, r.z0, so, r.x0 + ins, r.x1 - ins, 0.04f, r.wall, ops, false);
            SWall(h, true, r.z1, -so, r.x0 + ins, r.x1 - ins, 0.04f, r.wall, ops, false);
            SWall(h, false, r.x0, so, r.z0 + ins, r.z1 - ins, 0.04f, r.wall, ops, false);
            SWall(h, false, r.x1, -so, r.z0 + ins, r.z1 - ins, 0.04f, r.wall, ops, false);
            // two-tone wainscot band
            // (v0.4.2: split around every opening on that wall instead of running through doorways)
            Band(h, false, r.x0, r.x0 + 0.17f, r.z0 + 0.15f, r.z1 - 0.15f, 1.0f, r.trim, ops);
            Band(h, false, r.x1, r.x1 - 0.17f, r.z0 + 0.15f, r.z1 - 0.15f, 1.0f, r.trim, ops);
            // v0.5 baseboards (split around doorways like the wainscot)
            Band(h, false, r.x0, r.x0 + 0.17f, r.z0 + 0.15f, r.z1 - 0.15f, 0.07f, new Color(r.trim.r * 0.9f, r.trim.g * 0.9f, r.trim.b * 0.9f, 1), ops);
            Band(h, false, r.x1, r.x1 - 0.17f, r.z0 + 0.15f, r.z1 - 0.15f, 0.07f, new Color(r.trim.r * 0.9f, r.trim.g * 0.9f, r.trim.b * 0.9f, 1), ops);
            Band(h, true, r.z0, r.z0 + 0.17f, r.x0 + 0.15f, r.x1 - 0.15f, 0.07f, new Color(r.trim.r * 0.9f, r.trim.g * 0.9f, r.trim.b * 0.9f, 1), ops);
            Band(h, true, r.z1, r.z1 - 0.17f, r.x0 + 0.15f, r.x1 - 0.15f, 0.07f, new Color(r.trim.r * 0.9f, r.trim.g * 0.9f, r.trim.b * 0.9f, 1), ops);
        }
        foreach (var o in ops) Trim(h, o, trim, o.y0 == 0 && o.w < 2);
        foreach (var o in ops)
        {   // world-space opening volumes (inset so the frame trim / sill touching the edge doesn't count) for the trim-crossing check
            var ctr = o.ax ? new Vector3(o.c, FY + (o.y0 + o.y1) / 2, o.fc) : new Vector3(o.fc, FY + (o.y0 + o.y1) / 2, o.c);
            var sz = o.ax ? new Vector3(o.w - 0.1f, o.y1 - o.y0 - 0.1f, 0.6f) : new Vector3(0.6f, o.y1 - o.y0 - 0.1f, o.w - 0.1f);
            Openings.Add(new Bounds(h.TransformPoint(ctr), sz));
        }

        // ---- doors
        SDoor(h, 0, 0, 1.3f, 2.4f, Mint, trim, false);
        SDoor(h, 6, 6, 1.3f, 2.3f, Lav, trim, true);

        // ---- upper storeys (dollhouse shell: no shadows so rooms get sun)
        var up = Group(h, "upper", Vector3.zero);
        float y2 = FY + AWH + 0.14f;
        Block(up, new Vector3(0, y2 + 0.05f, 6), new Vector3(19.2f, 0.45f, 12.9f), PinkD, 0.2f, true, 0, 0.02f);              // chunky band
        Block(up, new Vector3(0, y2 + 1.6f, 6.1f), new Vector3(18.4f, 3.1f, 12.2f), wallOut, 0.35f, true, 0, 0.04f);
        Block(up, new Vector3(0, y2 + 3.2f, 6.1f), new Vector3(18.8f, 0.3f, 12.5f), Cream, 0.14f, true, 0, 0.02f);
        Block(up, new Vector3(-1.25f, y2 + 4.75f, 6.6f), new Vector3(13.5f, 2.9f, 10.8f), Lite(Pink, 0.15f), 0.35f, true, 0, 0.04f);
        foreach (float x in new[] { -4.6f, 2.1f })
        {
            var hump = Cyl(up, new Vector3(x, y2 + 6.15f, 6.6f), 3.55f, 11.4f, Rose, 0.35f, Quaternion.Euler(90, 0, 0), 30); hump.transform.localScale = new Vector3(1, 1, 0.6f);
            var cap = Cyl(up, new Vector3(x, y2 + 6.15f, 0.95f), 3.2f, 0.3f, Cream, 0.12f, Quaternion.Euler(90, 0, 0), 30); cap.transform.localScale = new Vector3(1, 1, 0.55f);
        }
        Cyl(up, new Vector3(4.2f, y2 + 7.6f, 9f), 0.32f, 1.2f, Coral, 0.1f);
        SoftProps.RoundWin(up, new Vector3(-4.6f, y2 + 7.0f, 0.75f), 0.55f, MintD, true);
        SoftProps.RoundWin(up, new Vector3(2.1f, y2 + 7.0f, 0.75f), 0.55f, MintD, false);
        // 2F facade: inflated panels, windows pushed onto the bulge
        Pillow(up, new Vector3(0, y2 + 1.65f, -0.02f), 17.6f, 2.8f, 0.5f, Lite(wallOut, 0.06f));
        Pillow(up, new Vector3(-1.25f, y2 + 4.75f, 1.18f), 13f, 2.6f, 0.5f, Lite(Pink, 0.2f));
        SoftProps.RoundWin(up, new Vector3(-4.5f, y2 + 1.65f, -0.55f), 1.3f, White, true);
        SoftProps.Win(up, new Vector3(1.6f, y2 + 1.6f, -0.55f), 2.4f, 1.9f, White, true, Rose, true);
        SoftProps.Win(up, new Vector3(-7.9f, y2 + 1.65f, -0.5f), 1.2f, 1.6f, White, false, null, true);
        // 3F facade
        SoftProps.RoundWin(up, new Vector3(-1.25f, y2 + 4.75f, 0.65f), 1.15f, Mint, true);
        SoftProps.Win(up, new Vector3(-5.8f, y2 + 4.7f, 0.7f), 1.6f, 1.6f, White, true, ButterD);
        SoftProps.Win(up, new Vector3(3.4f, y2 + 4.7f, 0.7f), 1.6f, 1.6f, White, true, PinkD);
        // 3F balcony on the right
        SoftProps.Grille(up, new Vector3(7.4f, y2 + 3.85f, -0.55f), 3.4f, 1.1f, White, false);
        foreach (float x in new[] { 5.7f, 9.1f }) Block(up, new Vector3(x, y2 + 3.85f, 1.5f), new Vector3(0.08f, 0.95f, 3f), White, 0.03f, false);
        SoftProps.FlowerPot(up, new Vector3(6.3f, y2 + 3.35f, 0.6f), Coral, Rose, 1f, false);
        SoftProps.FlowerPot(up, new Vector3(8.4f, y2 + 3.35f, 0.6f), Sky, ButterD, 0.9f, false);
        SoftProps.WaterTank(up, new Vector3(7.4f, y2 + 3.35f, 9.5f), Sky);
        // sign on the band
        var sign = Group(up, "aunt_sign", new Vector3(0, y2 + 0.05f, -0.5f));
        Block(sign, Vector3.zero, new Vector3(2.8f, 0.85f, 0.16f), White, 0.2f, false);
        Ring(sign, Vector3.zero, Quaternion.identity, 0.0f, 0.0f, White, 3);
        SoftProps.Text3D(sign, new Vector3(0, 0.05f, -0.95f), "阿姨家", 0.15f, Rose);
        Ball(sign, new Vector3(-1.6f, 0.2f, -0.05f), new Vector3(0.3f, 0.3f, 0.1f), Rose, 10);
        Ball(sign, new Vector3(1.6f, 0.2f, -0.05f), new Vector3(0.3f, 0.3f, 0.1f), ButterD, 10);
        NoShadow(up.gameObject);

        // ---- front: garage awning + rolled shutter, planters, mailbox
        SoftProps.Awning(h, new Vector3(-6, FY + 3.15f, -0.15f), 4.9f, 1.0f, Sky, White);
        Cyl(h, new Vector3(-6, FY + 2.88f, -0.05f), 0.16f, 4.4f, Lite(Grey, 0.4f), 0.05f, Quaternion.Euler(0, 0, 90), 14);
        SoftProps.Awning(h, new Vector3(0, FY + 3.2f, -0.15f), 2.4f, 0.8f, Mint, White);
        foreach (float x in new[] { -2.3f, -1.4f, 1.4f, 2.3f, 7.7f, 8.6f }) SoftProps.FlowerPot(h, new Vector3(x, FY, -0.42f), x < 0 ? Coral : Sky, x > 5 ? ButterD : Rose, 0.9f);
        SoftProps.Mailbox(h, new Vector3(-2.9f, FY, -0.55f), 180);
        SoftProps.Scooter(h, new Vector3(3.9f, FY, -0.8f), 90, Mint);
        SoftProps.Bush(h, new Vector3(-9.4f, FY, -0.6f), 0.9f, Leaf, Rose);
        SoftProps.Bush(h, new Vector3(9.4f, FY, -0.6f), 0.9f, Leaf, ButterD);
        SoftProps.WaterJar(h, new Vector3(-9.6f, FY, 2.5f), Coral);

        // ---- GARAGE
        var gC = rooms[0]; 
        SCont(h, "toolchest", new Vector3(-8.55f, FY, 1.0f), 90, Coral, "工具箱");
        SCont(h, "box", new Vector3(-8.3f, FY, 5.25f), 0, Wood, "神秘紙箱");
        SCont(h, "shelf", new Vector3(-4.0f, FY, 5.62f), 180, Lite(Sky, 0.1f), "雜物架");
        SoftProps.Scooter(h, new Vector3(-7.7f, FY, 3.3f), 0, Pink);
        SoftProps.Cone(h, new Vector3(-3.6f, FY, 0.6f));
        SoftProps.WaterJar(h, new Vector3(-5.2f, FY, 5.5f), Butter);
        // ---- LIVING
        SoftProps.Rug(h, new Vector3(0, FY + 0.004f, 3.2f), 1.1f, PinkD, Cream); Ring(h, new Vector3(0, FY + 0.03f, 3.2f), Quaternion.Euler(90, 0, 0), 1.12f, 0.05f, Rose, 40);
        SoftProps.Sofa(h, new Vector3(-2.35f, FY, 4.0f), 90, Rose, Mint);
        SoftProps.RoundTable(h, new Vector3(0, FY, 3.2f), 0.55f, 0.45f, White, WoodD); Cyl(h, new Vector3(0, FY + 0.22f, 3.2f), 0.14f, 0.42f, WoodD, 0.05f, null, 16); Cyl(h, new Vector3(0, FY + 0.05f, 3.2f), 0.34f, 0.1f, WoodD, 0.05f, null, 20); // chunky pedestal
        Goods.FruitBowl(h, new Vector3(0, FY + 0.44f, 3.2f));   // v0.9.4: fruit bowl (was 3 balls)

        SCont(h, "drawer", new Vector3(2.5f, FY, 4.0f), -90, Wood, "電視櫃");
        SoftProps.TVStand(h, new Vector3(2.5f, FY + 0.46f, 4.0f), -90);
        SCont(h, "display", new Vector3(-1.4f, FY, 5.7f), 180, Lite(Wood, 0.2f), "阿姨的收藏櫃");
        SoftProps.PetBed(h, new Vector3(1.6f, FY, 1.3f), Lav);
        petSpots.Add(h.TransformPoint(new Vector3(1.6f, FY, 1.3f)));
        SoftProps.FloorLamp(h, new Vector3(-2.5f, FY, 0.55f));
        SoftProps.Plant(h, new Vector3(2.45f, FY, 0.5f));
        SoftProps.Clock(h, new Vector3(0.4f, FY + 2.7f, 5.82f), 180);
        SoftProps.Frame(h, new Vector3(-2.82f, FY + 2.0f, 4.0f), 90, 1.1f, 0.8f, ButterD, Sky);
        { var mr = Ring(h, new Vector3(-2.8f, FY + 2.15f, 5.05f), Quaternion.Euler(0, 90, 0), 0.32f, 0.07f, Coral, 24); Ball(h, new Vector3(-2.83f, FY + 2.15f, 5.05f), new Vector3(0.04f, 0.6f, 0.6f), Lite(Sky, 0.5f), 14); Ball(h, new Vector3(-2.79f, FY + 2.25f, 4.97f), new Vector3(0.02f, 0.12f, 0.08f), White, 6); // round mirror
          Block(h, new Vector3(-2.7f, FY + 1.95f, 2.85f), new Vector3(0.3f, 0.1f, 0.9f), Lite(Wood, 0.2f), 0.05f, false); foreach (int sx in new[] { -1, 1 }) Ball(h, new Vector3(-2.76f, FY + 1.85f, 2.85f + sx * 0.35f), new Vector3(0.1f, 0.14f, 0.1f), WoodD, 6);
          Cyl(h, new Vector3(-2.7f, FY + 2.12f, 2.6f), 0.09f, 0.22f, Lite(Mint, 0.2f), 0.04f, null, 12); Goods.Shrub(h, new Vector3(-2.7f, FY + 2.22f, 2.6f), 0.13f, 0.2f, Leaf, null, 7); Goods.PotPlant(h, new Vector3(-2.7f, FY + 2.0f, 3.08f), 0.16f, LeafD, Rose, 9); { var v = Cyl(h, new Vector3(-2.7f, FY + 2.06f, 3.28f), 0.05f, 0.16f, ButterD, 0.03f, null, 12, false, "sk_vase"); SoftKit.Tag(v.transform, MatK.Plastic); } } // little wall shelf
        // ---- KITCHEN
        SCont(h, "fridge", new Vector3(8.45f, FY, 5.35f), -90, Sky, "冰箱");
        SoftProps.Counter(h, new Vector3(8.5f, FY, 3.0f), -90, 1.8f, Lite(Mint, 0.1f), White, "sink");
        SoftProps.Counter(h, new Vector3(7.6f, FY, 0.5f), 0, 1.4f, Lite(Mint, 0.1f), White, "stove");
        SCont(h, "drawer", new Vector3(3.55f, FY, 4.6f), 90, Mint, "碗櫃");
        SoftProps.RoundTable(h, new Vector3(5.6f, FY, 3.0f), 0.62f, 0.75f, Butter, WoodD);
        for (int i = 0; i < 3; i++) { float a = i * 2.1f + 0.4f; SoftProps.Stool(h, new Vector3(5.6f + Mathf.Cos(a) * 1.05f, FY, 3.0f + Mathf.Sin(a) * 1.05f), i == 0 ? Rose : i == 1 ? Mint : Lav); }
        Goods.Meal(h, new Vector3(5.6f, FY + 0.76f, 3.0f));   // v0.9.4: plate + rice bowl + dish (was balls)

        SoftProps.Clock(h, new Vector3(5.0f, FY + 2.6f, 0.17f), 0);
        // ---- BATH
        SoftProps.Bathtub(h, new Vector3(-7.6f, FY, 11.35f), 180);
        SoftProps.Toilet(h, new Vector3(-3.7f, FY, 11.45f), 180);
        SoftProps.Counter(h, new Vector3(-8.5f, FY, 8.3f), 90, 0.9f, White, Lite(Sky, 0.3f), "sink");
        SCont(h, "box", new Vector3(-4.0f, FY, 7.0f), -90, Lite(Lav, 0.2f), "洗衣籃");
        Ball(h, new Vector3(-6.5f, FY + 0.02f, 8.8f), new Vector3(1.1f, 0.04f, 0.7f), Mint, 12);
        // ---- BEDROOM
        SoftProps.Rug(h, new Vector3(0.4f, FY + 0.004f, 8.6f), 1.2f, Lav, Butter);
        SoftProps.Bed(h, new Vector3(-1.83f, FY, 9.9f), 90, 1.3f, Wood, Lav, ButterD);
        SCont(h, "wardrobe", new Vector3(1.7f, FY, 11.55f), 180, Lite(Wood, 0.1f), "衣櫃");
        SCont(h, "drawer", new Vector3(-2.45f, FY, 11.5f), 180, Pink, "床頭小抽屜");
        SCont(h, "box", new Vector3(2.45f, FY, 7.3f), -90, Lite(Butter, 0.1f), "玩具箱");
        SoftProps.Teddy(h, new Vector3(-1.4f, FY + 0.75f, 10.3f), 90, 0.8f);
        SoftProps.ToyBlocks(h, new Vector3(0.6f, FY, 7.3f), 6, rng);
        // ---- AUNT'S LOCKED ROOM
        SoftProps.Rug(h, new Vector3(6.0f, FY + 0.004f, 8.4f), 1.1f, PinkD, Cream);
        SoftProps.Bed(h, new Vector3(7.83f, FY, 9.6f), -90, 1.7f, PinkD, Rose, White, false);
        SCont(h, "safe", new Vector3(8.4f, FY, 11.45f), 180, LavD, "床頭保險箱", true, lockedC);
        SCont(h, "wardrobe", new Vector3(4.0f, FY, 11.6f), 180, Lav, "阿姨的大衣櫃", true, lockedC);
        SCont(h, "drawer", new Vector3(3.55f, FY, 8.4f), 90, PinkD, "梳妝台", false, lockedC);
        Ball(h, new Vector3(3.3f, FY + 1.7f, 8.4f), new Vector3(0.05f, 0.7f, 0.55f), Lite(Sky, 0.5f), 14);
        SoftProps.Plant(h, new Vector3(8.5f, FY, 6.6f), 0.9f);

        foreach (var r in rooms) L.spots.Add(h.TransformPoint(r.C));
        return h;
    }

    static Transform CandyShop(Transform root, Vector3 pos, float yaw)
    {
        var g = Group(root, "Shop", pos, yaw);
        Field(g, new Vector3(0, FY, 3), 9, 6, 0.6f, (x, z) => ((Mathf.FloorToInt(x / 0.6f) + Mathf.FloorToInt(z / 0.6f)) & 1) == 0 ? Cream : Lite(Peach, 0.1f), true, "floor_shop");
        Block(g, new Vector3(0, 0.055f, 3), new Vector3(9.4f, 0.11f, 6.4f), LavD, 0.04f, true);
        Block(g, new Vector3(0, FY + 1.75f, 6), new Vector3(9.2f, 3.5f, 0.24f), Butter, 0.05f, true, 0, 0.01f);
        foreach (int s in new[] { -1, 1 }) Block(g, new Vector3(s * 4.5f, FY + 1.75f, 3), new Vector3(0.24f, 3.5f, 6.2f), Butter, 0.05f, true, 0, 0.01f);
        foreach (int s in new[] { -1, 1 }) Block(g, new Vector3(s * 4.3f, FY + 1.75f, 0), new Vector3(0.5f, 3.5f, 0.3f), Lite(Butter, 0.3f), 0.1f, true);
        Block(g, new Vector3(0, FY + 3.15f, 0), new Vector3(9.2f, 0.7f, 0.3f), Lite(Butter, 0.3f), 0.1f, true);
        var up = Group(g, "upper", Vector3.zero);
        Block(up, new Vector3(0, FY + 3.6f, 3), new Vector3(9.5f, 0.3f, 6.5f), Cream, 0.1f, true);
        Block(up, new Vector3(0, FY + 5.2f, 3.1f), new Vector3(9.2f, 3.0f, 6.2f), Mint, 0.3f, true, 0, 0.04f);
        var hump = Cyl(up, new Vector3(0, FY + 6.7f, 3.1f), 4.7f, 6.4f, Lite(Pink, 0.1f), 0.3f, Quaternion.Euler(90, 0, 0), 30); hump.transform.localScale = new Vector3(1, 1, 0.45f);
        var capc = Cyl(up, new Vector3(0, FY + 6.7f, -0.05f), 4.3f, 0.2f, Mint, 0.1f, Quaternion.Euler(90, 0, 0), 30); capc.transform.localScale = new Vector3(1, 1, 0.42f);
        SoftProps.RoundWin(up, new Vector3(0, FY + 7.2f, -0.2f), 0.5f, White, true);
        foreach (int s in new[] { -1, 1 })
        {   // candy-jar display windows instead of ring grilles (shop motif differs from homes)
            Block(up, new Vector3(s * 2.3f, FY + 5.2f, -0.05f), new Vector3(1.7f, 1.6f, 0.2f), White, 0.2f, false);
            Block(up, new Vector3(s * 2.3f, FY + 5.2f, -0.12f), new Vector3(1.4f, 1.3f, 0.1f), Lite(Sky, 0.3f), 0.15f, false);
            Block(up, new Vector3(s * 2.3f, FY + 4.45f, -0.3f), new Vector3(1.8f, 0.14f, 0.45f), Coral, 0.06f, false);
            for (int j = -1; j <= 1; j++) { var jc = j == 0 ? Rose : (j < 0 ? ButterD : MintD); Cyl(up, new Vector3(s * 2.3f + j * 0.42f, FY + 4.72f, -0.32f), 0.16f, 0.4f, Lite(jc, 0.45f), 0.08f, null, 14); Ball(up, new Vector3(s * 2.3f + j * 0.42f, FY + 4.68f, -0.32f), Vector3.one * 0.2f, jc, 8); Cyl(up, new Vector3(s * 2.3f + j * 0.42f, FY + 4.96f, -0.32f), 0.13f, 0.08f, Coral, 0.03f, null, 12); }
        }
        for (int j = -2; j <= 2; j++) Ball(up, new Vector3(j * 1.6f, FY + 8.25f - Mathf.Abs(j) * 0.35f, 3.1f), new Vector3(0.5f, 0.3f, 0.5f), j % 2 == 0 ? Butter : Cream, 10); // dome skylight bumps
        NoShadow(up.gameObject);
        SoftProps.Awning(g, new Vector3(0, FY + 2.8f, -0.2f), 9.4f, 1.3f, Mint, White);
        var sg = Group(g, "shop_sign", new Vector3(0, FY + 3.75f, -0.35f));
        Block(sg, Vector3.zero, new Vector3(4.2f, 0.9f, 0.16f), White, 0.25f, false);
        SoftProps.Text3D(sg, new Vector3(0, 0, -0.3f), "柑仔店", 0.15f, Coral);
        foreach (int s in new[] { -1, 1 }) Ball(sg, new Vector3(s * 1.75f, 0, -0.06f), new Vector3(0.4f, 0.4f, 0.1f), s < 0 ? Rose : ButterD, 10);
        SCont(g, "shelf", new Vector3(-3.0f, FY, 5.6f), 180, Rose, "零食貨架");
        SCont(g, "shelf", new Vector3(-1.6f, FY, 5.6f), 180, Sky, "餅乾貨架");
        SCont(g, "display", new Vector3(1.0f, FY, 5.7f), 180, Butter, "玩具展示櫃");
        SCont(g, "cooler", new Vector3(3.85f, FY, 4.6f), -90, Sky, "飲料冰箱");
        SoftProps.Counter(g, new Vector3(2.6f, FY, 1.7f), 180, 1.6f, Rose, White, "");
        SCont(g, "register", new Vector3(2.9f, FY + 1.0f, 1.7f), 180, Coral, "收銀機");
        SCont(g, "box", new Vector3(-3.5f, FY, 1.4f), 30, Wood, "零食箱");
        for (int i = 0; i < 3; i++) { var jar = SoftProps.WaterJar(g, new Vector3(1.9f + i * 0.35f, FY + 0.97f, 1.6f), new[] { Lite(Sky, 0.4f), Lite(Pink, 0.3f), Lite(Mint, 0.3f) }[i]); jar.localScale = Vector3.one * 0.45f; }
        SoftProps.Cone(g, new Vector3(-4.8f, 0.12f, -0.6f));
        L.spots.Add(g.TransformPoint(new Vector3(0, FY, 3)));
        return g;
    }

    static void Park(Transform root, System.Random r)
    {
        var g = Group(root, "Park", new Vector3(6, 0, -19));
        Field(g, new Vector3(0, 0.03f, 0), 30, 18, 1f, (x, z) => Color.Lerp(H("c9efa6"), H("b9e898"), VN(new Vector3(x * 0.2f, 0, z * 0.2f), 4)), false, "park_lawn");
        // path from sidewalk + plaza
        Field(g, new Vector3(0, 0.06f, 5.5f), 2.4f, 7f, 0.6f, (x, z) => ((Mathf.FloorToInt(x / 0.6f) + Mathf.FloorToInt(z / 0.6f)) & 1) == 0 ? Lite(Butter, 0.3f) : Lite(Peach, 0.3f), false, "park_path");
        Lath(g, new Vector3(0, 0.0f, 0), new[] { new Vector2(4.2f, 0f), new Vector2(4.0f, 0.07f), new Vector2(0, 0.07f) }, Lite(Butter, 0.25f), 36, false, null, "plaza");
        // trees
        SoftProps.Tree(g, new Vector3(-12, 0, 2), 0, 1.15f); SoftProps.Tree(g, new Vector3(-8, 0, -5), 1, 1.0f); SoftProps.Tree(g, new Vector3(11, 0, 3), 1, 1.1f, Lite(Leaf, 0.1f));
        SoftProps.Tree(g, new Vector3(13, 0, -5), 2, 1.0f); SoftProps.Tree(g, new Vector3(-2, 0, -7.5f), 0, 1.25f, Mint); SoftProps.Tree(g, new Vector3(6, 0, -7), 0, 0.95f, Lite(Pink, 0.1f));
        // flower bushes along the sidewalk edge (gap for the path)
        foreach (float x in new[] { -14f, -10.5f, -7f, -3.5f, 3.5f, 7f, 10.5f, 14f }) SoftProps.Bush(g, new Vector3(x, 0, 8.4f), 0.95f, Leaf, x < 0 ? (x < -8 ? Rose : ButterD) : (x > 8 ? Lav : Rose), r);
        // playground: slide + spring duck + sandbox
        var sl = Group(g, "slide", new Vector3(-6, 0, 0), 30);
        foreach (int s in new[] { -1, 1 }) Cyl(sl, new Vector3(s * 0.4f, 1.0f, -1.2f), 0.06f, 2.0f, Sky);
        for (int i = 0; i < 5; i++) Cyl(sl, new Vector3(0, 0.3f + i * 0.35f, -1.2f), 0.04f, 0.8f, White, 0.02f, Quaternion.Euler(0, 0, 90), 8);
        Block(sl, new Vector3(0, 2.0f, -0.9f), new Vector3(1.0f, 0.12f, 0.8f), Butter, 0.05f, false);
        var ramp = Block(sl, new Vector3(0, 1.05f, 0.75f), new Vector3(0.9f, 0.12f, 3.0f), Coral, 0.06f, false); ramp.transform.localRotation = Quaternion.Euler(38, 0, 0);
        var sc = sl.gameObject.AddComponent<BoxCollider>(); sc.size = new Vector3(1.1f, 2.1f, 3.4f); sc.center = new Vector3(0, 1.05f, 0);
        var duck = Group(g, "springduck", new Vector3(2.5f, 0, -2.5f), -30);
        Cyl(duck, new Vector3(0, 0.25f, 0), 0.12f, 0.5f, Grey); Ball(duck, new Vector3(0, 0.7f, 0), new Vector3(0.7f, 0.5f, 0.9f), Butter); Ball(duck, new Vector3(0, 1.05f, 0.3f), Vector3.one * 0.38f, Butter); Ball(duck, new Vector3(0, 1.0f, 0.52f), new Vector3(0.2f, 0.1f, 0.2f), Coral, 8);
        foreach (int s in new[] { -1, 1 }) Ball(duck, new Vector3(s * 0.12f, 1.1f, 0.45f), Vector3.one * 0.06f, Plum, 6);
        var dc = duck.gameObject.AddComponent<BoxCollider>(); dc.size = new Vector3(0.7f, 1.2f, 1f); dc.center = new Vector3(0, 0.6f, 0);
        var sb = Group(g, "sandbox", new Vector3(8, 0, -2));
        Field(sb, new Vector3(0, 0.1f, 0), 3, 3, 0.5f, (x, z) => Lite(ButterD, 0.3f), false);
        foreach (int s in new[] { -1, 1 }) { Block(sb, new Vector3(s * 1.6f, 0.15f, 0), new Vector3(0.25f, 0.3f, 3.4f), Wood, 0.08f, true); Block(sb, new Vector3(0, 0.15f, s * 1.6f), new Vector3(3.4f, 0.3f, 0.25f), Wood, 0.08f, true); }
        Lath(sb, new Vector3(0.4f, 0.1f, 0.3f), new[] { new Vector2(0, 0), new Vector2(0.4f, 0), new Vector2(0.25f, 0.3f), new Vector2(0, 0.35f) }, Lite(ButterD, 0.2f), 14);
        Ball(sb, new Vector3(-0.6f, 0.2f, -0.4f), new Vector3(0.3f, 0.25f, 0.3f), Coral, 10);
        SoftProps.Bench(g, new Vector3(-3.5f, 0, 2.5f), 120, Rose, MintD);
        SoftProps.Bench(g, new Vector3(3.8f, 0, 2.6f), -120, Sky, MintD);
        SoftProps.TrashBin(g, new Vector3(1.8f, 0, 4.2f), Mint);
        SCont(g, "box", new Vector3(-2.2f, 0.07f, 3.6f), 20, Wood, "野餐籃");
        L.spots.Add(g.TransformPoint(new Vector3(0, 0.1f, 0)));
    }

    static void TownRow(Transform root, float x0, float x1, float zFront, float yaw, System.Random r, ref int k)
    {
        Color[] walls = { Mint, Butter, Lav, Sky, Peach, Lite(Pink, 0.2f), Lite(Teal, 0.45f), Lite(Coral, 0.4f) };
        Color[] trims = { White, Cream, White, Lite(Butter, 0.4f) };
        Color[] roofs = { Rose, Coral, LavD, SkyD, MintD, PinkD };
        string[] shops = { "早餐店", "冰店", "書局", "花店", "麵包", "豆花", "文具", "理髮" };
        int n = Mathf.Max(1, Mathf.RoundToInt((x1 - x0) / 7.4f)); float w = (x1 - x0) / n;
        for (int i = 0; i < n; i++, k++)
        {
            float x = x0 + (i + 0.5f) * w; var wall = walls[(k * 3 + r.Next(2)) % walls.Length];
            int fl = r.Next(10) < 7 ? 2 : 3;
            SoftProps.TownHouse(root, new Vector3(x, 0, zFront), yaw, w - 0.8f, 10, fl, wall, trims[r.Next(trims.Length)], roofs[r.Next(roofs.Length)], r, r.Next(3) == 0 ? shops[k % shops.Length] : null);
        }
    }

    static Level BuildAunt(int seed, ThemeDef th, float vmult, int luckLv)
    {
        rng = new System.Random(seed); luck = luckLv; GlobalSeed = seed;
        L = new Level { theme = th, seed = seed, vmult = vmult, root = new GameObject("Level").transform };
        Openings.Clear(); NoRide.Clear(); TrimCrossBefore = TrimPieces = 0;
        var root = L.root; blocks.Clear(); var deco = new System.Random(20260424);   // fixed layout; loot varies by seed
        // ---- ground, road, sidewalks
        Field(root, Vector3.zero, 130, 130, 2f, (x, z) => Color.Lerp(H("b2e58a"), H("8fd06a"), VN(new Vector3(x * 0.06f, 0, z * 0.06f), 9)), true, "grass_ground");
        Field(root, new Vector3(0, 0.02f, 0), 130, 8, 1f, (x, z) => Color.Lerp(H("d2bea6"), H("8e7c6a"), Mathf.Clamp01(0.35f * VN(new Vector3(x * 0.3f, 0, z * 0.3f), 5) + 0.65f * VN(new Vector3(x * 0.05f, 0, z * 0.05f), 11) + (Mathf.Abs(z) > 2.6f ? 0.18f : 0f))), false, "road");
        for (float x = -60; x < 60; x += 6) if (Mathf.Abs(x + 6) > 3) Block(root, new Vector3(x, 0.035f, 0), new Vector3(2.5f, 0.03f, 0.22f), ButterD, 0.01f, false, 0, 0);
        for (float z = -3.3f; z <= 3.31f; z += 0.85f) Block(root, new Vector3(-6, 0.035f, z), new Vector3(2.8f, 0.03f, 0.45f), White, 0.01f, false, 0, 0);
        System.Func<float, float, Color> tiles = (x, z) => ((Mathf.FloorToInt(x) + Mathf.FloorToInt(z)) & 1) == 0 ? Cream : Color.Lerp(Cream, H("e9e2d4"), 0.5f);
        Field(root, new Vector3(0, 0.12f, 6.5f), 130, 5f, 1f, tiles, true, "sidewalk");
        Field(root, new Vector3(0, 0.12f, -6.75f), 130, 5.5f, 1f, tiles, true, "sidewalk");
        foreach (int s in new[] { -1, 1 }) Block(root, new Vector3(0, 0.08f, s * 4.06f), new Vector3(130, 0.17f, 0.24f), H("e4ded2"), 0.06f, false, 0, 0.01f);
        foreach (int s in new[] { -1, 1 }) Block(root, new Vector3(0, 0.035f, s * 3.5f), new Vector3(130, 0.04f, 0.9f), H("cfc8bc"), 0.015f, false, 0, 0);   // v0.5 gutter band
        for (float x = -54; x < 58; x += 18) { Cyl(root, new Vector3(x + 3, 0.05f, 1.9f), 0.6f, 0.08f, H("b3aca0"), 0.035f, null, 24); Ring(root, new Vector3(x + 3, 0.095f, 1.9f), Quaternion.Euler(90, 0, 0), 0.38f, 0.04f, H("9a9488"), 24); Ball(root, new Vector3(x + 3, 0.095f, 1.9f), new Vector3(0.22f, 0.04f, 0.22f), H("8f80ab"), 12); }   // manhole covers
        for (float x = -57; x < 58; x += 12) foreach (int s in new[] { -1, 1 }) Block(root, new Vector3(x, 0.04f, s * 3.62f), new Vector3(0.8f, 0.05f, 0.5f), H("9a9488"), 0.01f, false, 0, 0);   // drain grates
        foreach (var (p, s) in new[] { (new Vector3(0, 2, 60), new Vector3(130, 6, 1)), (new Vector3(0, 2, -60), new Vector3(130, 6, 1)), (new Vector3(60, 2, 0), new Vector3(1, 6, 130)), (new Vector3(-60, 2, 0), new Vector3(1, 6, 130)) })
        { var b = new GameObject("bound"); b.transform.SetParent(root, false); b.transform.localPosition = p; b.AddComponent<BoxCollider>().size = s; }

        // ---- the house + street
        var rooms = new List<ARoom>(); var lockedC = new List<LootContainer>(); var petSpots = new List<Vector3>();
        var house = AuntHouse(root, new Vector3(-6, 0, 9), rooms, lockedC, petSpots);
        int k = 0;
        TownRow(root, -58f, -15.4f, 9f, 0, deco, ref k);
        TownRow(root, 3.4f, 58f, 9f, 0, deco, ref k);
        TownRow(root, -58f, -28.8f, -9.5f, 180, deco, ref k);
        TownRow(root, -18.4f, -10.6f, -9.5f, 180, deco, ref k);
        TownRow(root, 22.6f, 58f, -9.5f, 180, deco, ref k);
        // back row silhouettes (seen from the high shots)
        for (float x = -54; x < 56; x += 9.5f) { SoftProps.TownHouse(root, new Vector3(x, 0, 24f), 0, 9f, 9, 3 + deco.Next(2), new[] { Lite(Lav, 0.2f), Lite(Sky, 0.2f), Lite(Mint, 0.2f), Lite(Butter, 0.2f) }[deco.Next(4)], White, new[] { Rose, LavD, SkyD }[deco.Next(3)], deco); }
        var shop = CandyShop(root, new Vector3(-23.6f, 0, -9.5f), 180);
        // no-ride zones (scooter must be parked outside; dismount to go in)
        { Bounds Z(Transform t, Vector3 a, Vector3 b) { var bb = new Bounds(t.TransformPoint(a), Vector3.zero); bb.Encapsulate(t.TransformPoint(b)); bb.Expand(new Vector3(0.6f, 10, 0.6f)); return bb; }
          NoRide.Add(Z(house, new Vector3(-9.3f, 0, -0.2f), new Vector3(9.3f, 4, 12.3f))); NoRide.Add(Z(shop, new Vector3(-4.7f, 0, -0.2f), new Vector3(4.7f, 4, 6.2f))); }
        Debug.Log("[trimcheck] bands crossing openings if unsplit: " + TrimCrossBefore + ", pieces now " + TrimPieces + (LegacyTrim ? " (LEGACY unsplit mode)" : ""));
        Park(root, deco);
        foreach (var st in root.GetComponentsInChildren<Transform>()) if (st.name == "slide") { st.gameObject.AddComponent<SlideRide>(); break; }
        // rideable toy scooter, parked on the sidewalk near the spawn + one in front of the house
        Moto.Build(root, new Vector3(-47.5f, 0.12f, 5.3f), 90, Pink); Moto.Build(root, new Vector3(-13.5f, 0.12f, 5.2f), 90, Sky);

        // street furniture
        for (float x = -50; x <= 50; x += 16) SoftProps.StreetLamp(root, new Vector3(x, 0.12f, 4.6f), 180);
        for (float x = -46; x <= 46; x += 16) foreach (int sd in new[] { -1, 1 }) { if (sd < 0 && x > 25 && x < 43) continue; float px = x + (sd < 0 ? 4 : 0); if (px > 6 && px < 13) continue; if (sd > 0 && (Mathf.Abs(px + 47.5f) < 3 || Mathf.Abs(px + 13.5f) < 3)) continue;   // sidewalk planters
            Block(root, new Vector3(px, 0.42f, sd * 4.75f), new Vector3(0.9f, 0.6f, 0.6f), sd > 0 ? Coral : Lite(Sky, 0.1f), 0.2f);
            Goods.Shrub(root, new Vector3(px - 0.2f, 0.72f, sd * 4.75f), 0.28f, 0.45f, Leaf, null, (int)px * 3 + sd); Goods.Shrub(root, new Vector3(px + 0.18f, 0.72f, sd * 4.75f), 0.26f, 0.5f, MintD, k2c(px), (int)px * 5 + sd);
            }
        for (float x = -42; x <= 50; x += 16) if (x < 27 || x > 41) SoftProps.StreetLamp(root, new Vector3(x, 0.12f, -4.6f), 0);
        Color[] sc = { Mint, Pink, Butter, Sky, Lav, Coral };
        for (int i = 0; i < 4; i++) SoftProps.Scooter(root, new Vector3(8.5f + i * 1.0f, 0.12f, 5.35f), 180 + (i % 2) * 8, sc[i]);
        for (int i = 0; i < 3; i++) SoftProps.Scooter(root, new Vector3(-41f + i * 1.0f, 0.12f, -5.35f), (i % 2) * -8, sc[i + 2]);
        for (int i = 0; i < 2; i++) SoftProps.Scooter(root, new Vector3(44f + i * 1.0f, 0.12f, 5.35f), 180, sc[(i + 4) % 6]);
        foreach (float x in new[] { -46f, -34f, 46f, 54f }) SoftProps.Tree(root, new Vector3(x, 0.12f, -5.9f), x < 0 ? 0 : 1, 0.85f);
        foreach (float x in new[] { -52f, 24f, 52f }) SoftProps.Tree(root, new Vector3(x, 0.12f, 5.9f), 1, 0.8f, Lite(Pink, 0.05f));
        SoftProps.Hydrant(root, new Vector3(16f, 0.12f, 4.7f)); SoftProps.Hydrant(root, new Vector3(-30f, 0.12f, -4.7f));
        SoftProps.TrashBin(root, new Vector3(-17.5f, 0.12f, -9.0f), Mint); SoftProps.TrashBin(root, new Vector3(-16.9f, 0.12f, -9.0f), Butter);
        SoftProps.Mailbox(root, new Vector3(30.5f, 0.12f, -5.6f), 0);
        foreach (float x in new[] { -44f, -36f, 18f, 30f }) SoftProps.FlowerPot(root, new Vector3(x, 0.12f, 5.6f), Coral, Rose, 1.1f);
        SCont(root, "box", new Vector3(-42f, 0.12f, 8.2f), 0, Wood, "回收箱");
        ClearStreet(root);
        // backdrop: hills + clouds
        for (int i = 0; i < 9; i++) { float x = -64 + i * 16; SoftProps.Hill(root, new Vector3(x, -4, 62 + (i % 2) * 4), new Vector3(30 + (i % 3) * 8, 26 + (i % 2) * 10, 18), i % 2 == 0 ? H("a8dc8f") : H("bfe6a2")); SoftProps.Hill(root, new Vector3(x + 8, -4, -64), new Vector3(30, 22 + (i % 3) * 6, 18), i % 2 == 0 ? H("b5e09a") : H("a3d48c")); }
        for (int i = 0; i < 5; i++) { SoftProps.Hill(root, new Vector3(68, -4, -48 + i * 24), new Vector3(18, 22, 30), H("b2df98")); SoftProps.Hill(root, new Vector3(-68, -4, -48 + i * 24), new Vector3(18, 22, 30), H("aedb93")); }
        for (int i = 0; i < 10; i++) SoftProps.Cloud(root, new Vector3(-55 + i * 12 + deco.Next(6), 24 + deco.Next(10), (i % 2 == 0 ? 48 : -48) + deco.Next(10)), 1.2f + (float)deco.NextDouble());

        // ---- key (never inside the locked room), ground loot
        var keyable = L.containers.FindAll(c => !c.special && !lockedC.Contains(c));
        if (keyable.Count > 0) { var kc = Pick(keyable); kc.slots.Insert(rng.Next(kc.slots.Count + 1), new LootSlot { it = new Item { r = 2, ic = "🔑", name = "神秘鑰匙", v = 0, key = true } }); }
        int gN = rng.Next(6, 9);
        var gSpots = new List<Vector3>(); foreach (var r in rooms) gSpots.Add(house.TransformPoint(r.C)); gSpots.Add(shop.TransformPoint(new Vector3(0, FY, 2.5f))); gSpots.Add(new Vector3(6, 0.1f, -16f));
        for (int i = 0; i < gN; i++)
        {
            int ri = rng.Next(gSpots.Count); if (ri == 5 && R() < 0.5f) continue;
            var p = gSpots[ri] + new Vector3(RR(-1.0f, 1.0f), 0.06f, RR(-1.0f, 1.0f));
            var it = LootContainer.Roll(R() < 0.7f ? "low" : "mid", rng, th, vmult, luck);
            var g = new GameObject("GroundLoot"); g.transform.SetParent(root, false); g.transform.position = p; g.AddComponent<GroundLoot>().Init(it);
        }
        // ---- bus + pets + spawn
        L.bus = Bus.Build(root, new Vector3(34, 0.12f, -5.4f), Vector3.left);
        SpawnPets(Mathf.Max(1, th.enemies), petSpots);
        L.spawn = new Vector3(-52, 0.2f, 1.5f); L.spawnYaw = 90;
        Art.Toonify(root.gameObject, 0.012f);
        Debug.Log("[aunt] containers " + L.containers.Count + " locked " + lockedC.Count);
        return L;
    }

    public static int SignBlocked, OverlapShrunk;
    public static int OverlapMoved, OverlapHidden, OverlapLeft, OverlapProps;
    static readonly string[] PropNames = { "sk_tree", "sk_lamp", "sk_scooter", "sk_hydrant", "sk_bin", "sk_mailbox", "sk_pot" };
    static Bounds RB(Transform t)
    {
        var b = new Bounds(t.position, Vector3.zero); bool any = false;
        foreach (var r in t.GetComponentsInChildren<Renderer>()) { if (!r.enabled) continue; if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds); }
        return b;
    }
    static bool InBuilding(Transform t)
    {
        for (var q = t.parent; q; q = q.parent) if (q.name == "TownHouse" || q.name == "House" || q.name == "Shop" || System.Array.IndexOf(PropNames, q.name) >= 0) return true;
        return false;
    }
    static Bounds Shrink(Bounds b, float m) { b.Expand(new Vector3(-2 * m, 0, -2 * m)); return b; }
    /// keeps street dressing clear of building footprints/facades (+0.25 m) and of each other.
    public static int ClearStreet(Transform root, bool fix = true)
    {
        var bld = new List<Bounds>(); var props = new List<Transform>();
        foreach (var t in root.GetComponentsInChildren<Transform>())
        {
            if (t.name == "TownHouse" || t.name == "House" || t.name == "Shop" || t.name == "box") { var b = RB(t); b.Expand(new Vector3(0.5f, 0, 0.5f)); bld.Add(b); }
            else if (System.Array.IndexOf(PropNames, t.name) >= 0 && t.gameObject.activeInHierarchy && !InBuilding(t)) props.Add(t);
        }
        var rays = new List<(Ray r, float d)>();
        foreach (var s in root.GetComponentsInChildren<Transform>())
        {
            if (s.name != "sk_text3d" || !s.gameObject.activeInHierarchy) continue; var p0 = s.position; if (Mathf.Abs(p0.z) < 5 || Mathf.Abs(p0.z) > 13) continue;
            var sb = RB(s);
            foreach (float ex in new[] { -3f, 0f, 3f })
            {
                var eye = new Vector3(p0.x + ex, p0.y, 0);
                foreach (var tp in new[] { sb.center, new Vector3(sb.min.x, sb.center.y, sb.center.z), new Vector3(sb.max.x, sb.center.y, sb.center.z), new Vector3(sb.center.x, sb.min.y, sb.center.z), new Vector3(sb.center.x, sb.max.y, sb.center.z) })
                { var d = tp - eye; rays.Add((new Ray(eye, d.normalized), d.magnitude - 0.3f)); }
            }
        }
        bool Blocks(Bounds b) { foreach (var (r, d) in rays) if (b.IntersectRay(r, out float h) && h < d) return true; return false; }
        var placed = new List<Bounds>(); int left = 0; OverlapProps = props.Count; SignBlocked = 0;
        bool HitP(Transform p, Bounds b) => Hit(b) || (p.name == "sk_tree" && Blocks(b));
        bool Hit(Bounds b) { foreach (var o in bld) if (o.Intersects(b)) return true; foreach (var o in placed) if (o.Intersects(b)) return true; return false; }
        foreach (var p in props)
        {
            var b0 = Shrink(RB(p), 0.08f);
            if (!HitP(p, b0)) { placed.Add(b0); continue; }
            if (!fix && p.name == "sk_tree" && !Hit(b0)) { SignBlocked++; Debug.Log("[overlap] tree blocks sign view @" + p.position); continue; }
            if (!fix) { left++; string w = ""; foreach (var o in bld) if (o.Intersects(b0)) w += " B" + o.ToString("F1"); foreach (var o in placed) if (o.Intersects(b0)) w += " P" + o.ToString("F1"); Debug.Log("[overlap] " + p.name + " " + b0.ToString("F1") + " hits" + w); continue; }
            bool ok = false; float zs = p.position.z > 0 ? -1 : 1;
            for (int ring = 1; ring <= 16 && !ok; ring++)
                for (int dz = 0; dz <= 3 && !ok; dz++)
                    foreach (int sx in new[] { 1, -1 })
                    {
                        var off = new Vector3(sx * ring * 0.5f, 0, zs * dz * 0.6f); if (dz == 0 && false) continue;
                        if (Mathf.Abs(p.position.z + off.z) < 4.5f) continue;
                        var bb = b0; bb.center += off; if (HitP(p, bb)) continue;
                        p.position += off; placed.Add(bb); ok = true; OverlapMoved++;
                        Debug.Log("[overlap] moved " + p.name + " by " + off); break;
                    }
            if (!ok && p.name == "sk_tree" && p.localScale.x > 0.7f)
            {   // smaller, lower crown where there is no gap
                p.localScale *= 0.6f; var bs = Shrink(RB(p), 0.08f);
                for (int ring = 0; ring <= 16 && !ok; ring++) foreach (int sx in new[] { 1, -1 })
                {
                    var off = new Vector3(sx * ring * 0.5f, 0, 0); var bb = bs; bb.center += off; if (HitP(p, bb)) continue;
                    p.position += off; placed.Add(bb); ok = true; OverlapShrunk++; Debug.Log("[overlap] shrank tree, moved " + off); break;
                }
                if (!ok) p.localScale /= 0.6f;
            }
            if (!ok) { p.gameObject.SetActive(false); OverlapHidden++; Debug.Log("[overlap] hid " + p.name + " @" + p.position); }
        }
        if (!fix) OverlapLeft = left;
        return left;
    }
}
