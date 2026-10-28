using System.Collections.Generic;
using UnityEngine;

public class Level
{
    public Transform root; public ThemeDef theme; public int seed; public float vmult; public Vector3 spawn; public float spawnYaw; public Bus bus;
    public List<LootContainer> containers = new List<LootContainer>(); public List<Vector3> spots = new List<Vector3>();
}

public static partial class LevelGen
{
    const float WH = 3.0f, WT = 0.2f, DW = 1.15f, DH = 2.2f;
    static System.Random rng; static Level L; static int luck;
    static float R() => (float)rng.NextDouble();
    static float RR(float a, float b) => a + R() * (b - a);
    static T Pick<T>(IList<T> a) => a[rng.Next(a.Count)];

    public static Level BuildHouse(int seed, ThemeDef th, float vmult, int luckLv)
    {
        if (th.id == "aunt") return BuildAunt(seed, th, vmult, luckLv);
        rng = new System.Random(seed); luck = luckLv;
        L = new Level { theme = th, seed = seed, vmult = vmult, root = new GameObject("Level").transform };
        var root = L.root;
        // ground
        Art.Box(root, new Vector3(0, -0.1f, 0), new Vector3(130, 0.2f, 130), Art.Lit(new Color(0.72f, 0.95f, 0.55f), "grass", 0.05f), true, 0.25f, "grass_ground");
        Art.Box(root, new Vector3(0, 0.005f, 0), new Vector3(130, 0.02f, 8), Art.Lit(new Color(0.85f, 0.85f, 0.88f), "asphalt", 0.25f), false, 0.25f, "road");
        for (int s = -1; s <= 1; s += 2) Art.Box(root, new Vector3(0, 0.06f, s * 5f), new Vector3(130, 0.12f, 2f), Art.Lit(Color.white, "concrete", 0.1f), true, 0.5f, "sidewalk");
        // road stripes
        for (float x = -60; x < 60; x += 6) Art.Box(root, new Vector3(x, 0.017f, 0), new Vector3(2.5f, 0.01f, 0.18f), Art.Lit(new Color(0.95f, 0.92f, 0.7f), null, 0.3f), false);
        // bounds
        foreach (var (p, s) in new[] { (new Vector3(0, 2, 60), new Vector3(130, 6, 1)), (new Vector3(0, 2, -60), new Vector3(130, 6, 1)), (new Vector3(60, 2, 0), new Vector3(1, 6, 130)), (new Vector3(-60, 2, 0), new Vector3(1, 6, 130)) })
        { var b = new GameObject("bound"); b.transform.SetParent(root, false); b.transform.localPosition = p; b.AddComponent<BoxCollider>().size = s; }
        string style = th.style ?? "house"; blocks.Clear();
        var allRooms = new List<Room>(); var openSpots = new List<Vector3>();
        var pastel = new[] { new Color(0.95f, 0.9f, 0.8f), new Color(0.86f, 0.9f, 0.93f), new Color(0.93f, 0.85f, 0.82f), new Color(0.88f, 0.92f, 0.84f), new Color(0.97f, 0.93f, 0.72f), new Color(0.84f, 0.84f, 0.86f) };
        var lots = new List<(Vector2 pos, bool north)>();
        if (style == "house")
        {
            // shuffle a larger lot pool -> 3..5 houses per raid
            var north = new List<float> { -40, -22, -4, 14 }; var south = new List<float> { -42, -24, -6 };
            Shuffle(north); Shuffle(south);
            int nn = rng.Next(2, 4), ns = rng.Next(1, 3);
            for (int i = 0; i < nn; i++) lots.Add((new Vector2(north[i], 18), true));
            for (int i = 0; i < ns; i++) lots.Add((new Vector2(south[i], -18), false));
        }
        else
        {
            bool mart = style == "mart", wh = style == "base"; int cols = mart || wh ? 5 : 4, rows = mart || wh ? 3 : 4; float cell = wh ? 6.4f : 6f;
            var bpos = new Vector2(-6, 13 + rows * cell / 2 + 0.3f);
            var rms = BuildHouse(root, bpos, true, cols, rows, cell, mart ? new Color(0.96f, 0.96f, 0.94f) : wh ? new Color(0.55f, 0.6f, 0.52f) : style == "office" ? new Color(1f, 0.86f, 0.62f) : new Color(0.72f, 0.76f, 0.8f), true, style, openSpots);
            allRooms.AddRange(rms);
            // parking lot with cars across the road
            Art.Box(root, new Vector3(-6, 0.012f, -19), new Vector3(30, 0.02f, 12), Art.Lit(new Color(0.7f, 0.7f, 0.72f), "asphalt", 0.25f), false, 0.25f, "parking");
            for (int i = 0; i < 6; i++) Art.Box(root, new Vector3(-18 + i * 4.6f, 0.025f, -19), new Vector3(0.12f, 0.01f, 5f), Art.Lit(Color.white, null, 0.3f), false);
            for (int i = 0; i < 4; i++) if (wh) { if (R() < 0.7f) Art.Spawn("PH/covered_car", root, new Vector3(-15.7f + i * 4.6f + (i > 1 ? 4.6f : 0), 0.02f, -19), RR(-4, 4) + 90, new Vector3(0, 0, 4.2f)); } else if (R() < 0.75f) Art.Spawn("Ext/sedan", root, new Vector3(-15.7f + i * 4.6f + (i > 1 ? 4.6f : 0), 0.02f, -19 + RR(-0.3f, 0.3f)), RR(-4, 4) + (R() < 0.5f ? 0 : 180), new Vector3(0, 1.45f, 0));
            blocks.Add(new Rect(-21, -25, 30, 12));
            if (wh) BaseYard(root);
        }
        for (int i = 0; i < lots.Count; i++)
        {
            float cell = RR(4.8f, 5.6f); int cols = rng.Next(2, 4), rows = i == 0 ? 2 : rng.Next(1, 3);
            var h = BuildHouse(root, lots[i].pos, lots[i].north, cols, rows, cell, pastel[rng.Next(pastel.Length)], i == 0 && style == "house", "house", null);
            allRooms.AddRange(h);
        }
        // key: put in random non-locked container
        var keyable = L.containers.FindAll(c => !c.special);
        if (keyable.Count > 0) { var kc = Pick(keyable); kc.slots.Insert(rng.Next(kc.slots.Count + 1), new LootSlot { it = new Item { r = 2, ic = "🔑", name = "神秘鑰匙", v = 0, key = true } }); }
        // ground loot inside rooms + yard
        int gN = rng.Next(5, 9) + (style == "house" ? 0 : 3);
        for (int i = 0; i < gN; i++)
        {
            var rm = Pick(allRooms); if (rm.locked && R() < 0.5f) continue;
            var p = rm.center + new Vector3(RR(-1.2f, 1.2f), 0.06f, RR(-1.2f, 1.2f));
            var it = LootContainer.Roll(R() < 0.7f ? "low" : "mid", rng, th, vmult, luck);
            var g = new GameObject("GroundLoot"); g.transform.SetParent(root, false); g.transform.position = p; g.AddComponent<GroundLoot>().Init(it);
        }
        // yard props
        Yard(root, style);
        Vegetation(root);
        // bus stop (south sidewalk, east end); bus drives toward -x
        L.bus = Bus.Build(root, new Vector3(34, 0.12f, -5.4f), Vector3.left);
        // pets
        SpawnPets(Mathf.Max(1, th.enemies), openSpots);
        // spawn
        L.spawn = new Vector3(-52, 0.2f, 1.5f); L.spawnYaw = 90;
        Art.Toonify(root.gameObject, 0.012f);
        return L;
    }

    static readonly List<Rect> blocks = new List<Rect>();
    static void Shuffle<T>(IList<T> a) { for (int i = a.Count - 1; i > 0; i--) { int j = rng.Next(i + 1); (a[i], a[j]) = (a[j], a[i]); } }
    static readonly string[] CatNames = { "小咪", "咪咪", "橘子", "胖虎", "麻糬" }, DogNames = { "旺財", "小黑", "豆豆", "阿福", "皮皮" };
    static void SpawnPets(int n, List<Vector3> inside)
    {
        var road = new[] { new Vector3(-30, 0.1f, 7.5f), new Vector3(12, 0.1f, 7.5f), new Vector3(20, 0.1f, -7.5f), new Vector3(-20, 0.1f, -7.5f) };
        var pool = new List<Vector3>(inside); Shuffle(pool);
        string id = L.theme.id;
        for (int i = 0; i < n; i++)
        {
            bool dog = id == "school" ? i != 2 : id == "mart" ? i == 2 : i % 2 == 1;
            List<Vector3> route;
            if (i == 0 && pool.Count > 0) { route = new List<Vector3>(); int k = Mathf.Min(pool.Count, 3); for (int j = 0; j < k; j++) route.Add(pool[j]); }
            else if (i == 1 || pool.Count < 4) route = new List<Vector3>(road);
            else { route = new List<Vector3>(); int k = Mathf.Min(pool.Count, 3); for (int j = 0; j < k; j++) route.Add(pool[(i * 3 + j) % pool.Count]); }
            int wi = rng.Next(route.Count);
            var nm = dog ? DogNames[((i + L.seed) % DogNames.Length + DogNames.Length) % DogNames.Length] : CatNames[((i + L.seed) % CatNames.Length + CatNames.Length) % CatNames.Length];
            var pet = Pet.Create(L.root, route[wi] + Vector3.up * 0.05f, dog, nm);
            pet.patrol.AddRange(route);
            if ((id == "mart" && i == 0) || (id == "grandma" && i == 1) || (id == "aunt" && i == 0 && R() < 0.5f)) { pet.sleeper = true; pet.patrol.Clear(); pet.ForceState(Pet.St.Sleep); }
        }
    }
    static bool Blocked(Vector3 p, float pad) { foreach (var r in blocks) if (p.x > r.xMin - pad && p.x < r.xMax + pad && p.z > r.yMin - pad && p.z < r.yMax + pad) return true; return false; }

    public class Room { public Vector3 center; public int cx, cz; public bool locked; public string type; }

    static List<Room> BuildHouse(Transform parent, Vector2 pos, bool north, int cols, int rows, float cell, Color wallC, bool lockRoom, string style, List<Vector3> openSpots)
    {
        bool big = style != "house";
        { float hw = cols * cell / 2 + 1.2f, hd = rows * cell / 2 + 1.2f; blocks.Add(new Rect(pos.x - hw, pos.y - hd, hw * 2, hd * 2)); }
        bool Open(int id) => big && id / cols < rows - 1;
        var hroot = new GameObject("House").transform; hroot.SetParent(parent, false); hroot.localPosition = new Vector3(pos.x, 0, pos.y);
        if (!north) hroot.localRotation = Quaternion.Euler(0, 180, 0);
        // local: front faces -z (towards road for north houses)
        float W = cols * cell, D = rows * cell; float x0 = -W / 2, z0 = -D / 2;
        bool office = style == "office", wh = style == "base";
        var wallM = wh ? Art.Lit(wallC, "corrugated", 0.3f, 0.4f) : office ? Art.Lit(wallC, "concrete", 0.15f) : Art.Lit(wallC, "plaster", 0.08f);
        var inM = wh ? Art.Lit(new Color(0.78f, 0.78f, 0.76f), "concrete", 0.1f) : office ? Art.Lit(new Color(0.86f, 0.89f, 0.92f), "plaster", 0.1f) : Art.Lit(new Color(0.96f, 0.94f, 0.9f), "plaster", 0.08f);
        var floorM = Art.Lit(Color.white, "woodfloor", 0.35f); var tileM = Art.Lit(Color.white, "tiles", 0.5f);
        // foundation + floor
        Art.Box(hroot, new Vector3(0, 0.06f, 0), new Vector3(W + 0.4f, 0.12f, D + 0.4f), Art.Lit(new Color(0.6f, 0.6f, 0.6f), "concrete"), true, 0.5f, "foundation");
        var rooms = new List<Room>(); var types = new[] { "living", "bedroom", "kitchen", "bedroom", "living", "bath" };
        var backTypes = wh ? new[] { "armory", "barracks", "garage", "armory", "barracks" } : style == "mart" ? new[] { "stock", "coolroom", "staff", "coolroom", "stock" } : new[] { "meeting", "server", "pantry", "meeting" };
        for (int cz = 0; cz < rows; cz++) for (int cx = 0; cx < cols; cx++)
            {
                var rm = new Room { cx = cx, cz = cz, center = new Vector3(x0 + (cx + 0.5f) * cell, 0.13f, z0 + (cz + 0.5f) * cell), type = big ? (cz < rows - 1 ? (style == "mart" ? "aisle" : wh ? "storage" : "cubicle") : backTypes[(cx + rng.Next(2)) % backTypes.Length]) : types[(cx + cz * cols + rng.Next(3)) % types.Length] };
                rooms.Add(rm);
                var fm = wh ? Art.Lit(new Color(0.72f, 0.72f, 0.7f), "concrete", 0.2f) : office && rm.type != "server" && rm.type != "pantry" ? Art.Lit(new Color(0.62f, 0.66f, 0.74f), "carpet", 0.05f) : rm.type == "kitchen" || rm.type == "bath" || style == "mart" || rm.type == "server" || rm.type == "pantry" ? tileM : floorM;
                Art.Box(hroot, rm.center + Vector3.down * 0.0f, new Vector3(cell, 0.02f, cell), fm, false, wh ? 0.25f : rm.type == "kitchen" || rm.type == "bath" ? 0.5f : 0.35f, "wood_floor");
            }
        // spanning tree connections between cells
        var conn = new HashSet<(int, int)>(); var visited = new HashSet<int> { 0 }; var frontier = new List<(int, int)>();
        int Id(int x, int z) => z * cols + x;
        void AddF(int a) { int ax = a % cols, az = a / cols; foreach (var (dx, dz) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) }) { int bx = ax + dx, bz = az + dz; if (bx < 0 || bz < 0 || bx >= cols || bz >= rows) continue; frontier.Add((a, Id(bx, bz))); } }
        AddF(0);
        while (visited.Count < cols * rows && frontier.Count > 0) { int k = rng.Next(frontier.Count); var (a, b) = frontier[k]; frontier.RemoveAt(k); if (visited.Contains(b)) continue; visited.Add(b); conn.Add((Mathf.Min(a, b), Mathf.Max(a, b))); AddF(b); }
        // locked leaf room
        int lockedId = -1; int lockedPartner = -1;
        if (lockRoom && cols * rows >= 3)
        {
            for (int a = cols * rows - 1; a >= 1; a--) { int deg = 0, partner = -1; foreach (var (p, q) in conn) { if (p == a) { deg++; partner = q; } if (q == a) { deg++; partner = p; } } if (deg == 1 && a / cols == rows - 1) { lockedId = a; lockedPartner = partner; break; } }
            if (lockedId < 0) for (int a = cols * rows - 1; a >= 1; a--) { int deg = 0, partner = -1; foreach (var (p, q) in conn) { if (p == a) { deg++; partner = q; } if (q == a) { deg++; partner = p; } } if (deg == 1) { lockedId = a; lockedPartner = partner; break; } }
        }
        if (big)
        {
            // big building: one locked back room, every other back room gets a door onto the open floor
            lockedId = (rows - 1) * cols + rng.Next(cols); conn.RemoveWhere(c => c.Item1 == lockedId || c.Item2 == lockedId); conn.Add((lockedId - cols, lockedId));
            for (int x = 0; x < cols; x++) { int bb = (rows - 1) * cols + x; if (bb != lockedId) conn.Add((bb - cols, bb)); }
        }
        // extra loop (after leaf selection; never touch the locked room)
        for (int a = 0; a < cols * rows; a++) for (int b = a + 1; b < cols * rows; b++) { if (a == lockedId || b == lockedId) continue; int ax = a % cols, az = a / cols, bx = b % cols, bz = b / cols; if (Mathf.Abs(ax - bx) + Mathf.Abs(az - bz) == 1 && R() < 0.2f) conn.Add((a, b)); }
        int frontCell = rng.Next(cols); if (lockedId == frontCell) frontCell = (frontCell + 1) % cols; int frontCell2 = big ? (frontCell + cols / 2) % cols : -1;
        // walls: vertical edges (along z) at x lines, horizontal edges (along x) at z lines
        for (int cz = 0; cz < rows; cz++) for (int xi = 0; xi <= cols; xi++)
            {
                var a = new Vector3(x0 + xi * cell, 0, z0 + cz * cell); bool ext = xi == 0 || xi == cols;
                string op = "none"; if (ext) op = R() < 0.6f ? "window" : "none";
                else { int c1 = Id(xi - 1, cz), c2 = Id(xi, cz); if (Open(c1) && Open(c2)) continue; if (conn.Contains((Mathf.Min(c1, c2), Mathf.Max(c1, c2)))) op = (c1 == lockedId || c2 == lockedId) ? "locked" : "door"; }
                Wall(hroot, a, a + new Vector3(0, 0, cell), op, ext ? wallM : inM, inM);
            }
        for (int zi = 0; zi <= rows; zi++) for (int cx = 0; cx < cols; cx++)
            {
                var a = new Vector3(x0 + cx * cell, 0, z0 + zi * cell); bool ext = zi == 0 || zi == rows;
                string op = "none"; if (ext) op = zi == 0 && (cx == frontCell || cx == frontCell2) ? "front" : (R() < (wh ? 0.35f : big && zi == 0 ? 0.95f : 0.6f) ? "window" : "none");
                else { int c1 = Id(cx, zi - 1), c2 = Id(cx, zi); if (Open(c1) && Open(c2)) continue; if (conn.Contains((Mathf.Min(c1, c2), Mathf.Max(c1, c2)))) op = (c1 == lockedId || c2 == lockedId) ? "locked" : "door"; }
                Wall(hroot, a, a + new Vector3(cell, 0, 0), op, ext ? wallM : inM, inM);
            }
        // ceiling + roof
        Art.Box(hroot, new Vector3(0, WH + 0.06f, 0), new Vector3(W + 0.2f, 0.12f, D + 0.2f), inM, true, 0.5f, "ceiling");
        if (big) FlatRoof(hroot, W, D, style, x0, z0); else Roof(hroot, W + 0.9f, D + 0.9f, WH + 0.12f, RoofTints[rng.Next(RoofTints.Length)]);
        var trimM = Art.Lit(new Color(0.93f, 0.92f, 0.88f), "plaster", 0.3f);
        foreach (var k in corners) Art.Box(hroot, new Vector3(k.x * (W / 2 + 0.02f), 0.12f + WH / 2, k.y * (D / 2 + 0.02f)), new Vector3(WT + 0.12f, WH, WT + 0.12f), trimM, false);
        // porch light + front step
        foreach (int fcx in new[] { frontCell, frontCell2 })
        {
            if (fcx < 0) continue; var fc = new Vector3(x0 + (fcx + 0.5f) * cell, 0, z0);
            Art.Box(hroot, fc + new Vector3(0, 0.06f, -0.9f), new Vector3(2.4f, 0.12f, 1.4f), Art.Lit(Color.white, "concrete"), true, 0.5f, "step");
            Art.Box(hroot, fc + new Vector3(0, 0.13f, -0.75f), new Vector3(1.0f, 0.015f, 0.6f), Art.Lit(new Color(0.33f, 0.25f, 0.18f), "fabric", 0.05f), false, 1, "doormat");
        }
        // lights + furniture + containers
        foreach (var rm in rooms)
        {
            int id = Id(rm.cx, rm.cz); rm.locked = id == lockedId;
            var lg = new GameObject("RoomLight"); lg.transform.SetParent(hroot, false); lg.transform.localPosition = rm.center + Vector3.up * 2.6f;
            var l = lg.AddComponent<Light>(); l.type = LightType.Point; l.range = big ? 9f : 7.5f; l.intensity = rm.locked ? 2.4f : RR(1.1f, 2.3f); l.color = rm.locked ? new Color(1f, 0.55f, 0.35f) : big ? (rm.type == "server" ? new Color(0.6f, 0.8f, 1f) : new Color(0.95f, 0.97f, 1f)) : Color.Lerp(new Color(1f, 0.66f, 0.38f), new Color(1f, 0.86f, 0.7f), R()); l.shadows = LightShadows.None;
            if (wh) { l.intensity = rm.locked ? 2.4f : RR(1.8f, 2.4f); l.color = rm.locked ? l.color : new Color(1f, 0.9f, 0.72f); Art.Spawn("PH/hanging_industrial_lamp", hroot, rm.center + Vector3.up * (WH - 0.75f), 0, new Vector3(0, 0.75f, 0), false); }
            else if (big) { l.intensity = rm.locked ? 2.4f : RR(1.6f, 2.2f); Art.Box(hroot, rm.center + Vector3.up * (WH - 0.04f), new Vector3(1.6f, 0.04f, 0.5f), Art.Emissive(new Color(0.95f, 0.97f, 1f), 2.5f), false); }
            else Art.Prim(PrimitiveType.Cylinder, hroot, rm.center + Vector3.up * (WH - 0.04f), new Vector3(0.5f, 0.03f, 0.5f), Art.Emissive(new Color(1f, 0.92f, 0.8f), 3f));
            Furnish(hroot, rm, cell, rm.locked, style, rows);
            L.spots.Add(hroot.TransformPoint(rm.center)); if (openSpots != null && Open(id)) openSpots.Add(hroot.TransformPoint(rm.center + new Vector3(cell * 0.32f, 0, cell * 0.32f)));
        }
        // store world-space room centers
        foreach (var rm in rooms) rm.center = hroot.TransformPoint(rm.center);
        return rooms;
    }

    static void Wall(Transform p, Vector3 a, Vector3 b, string op, Material outM, Material inM)
    {
        var dir = (b - a); float len = dir.magnitude; var u = dir / len; var mid = (a + b) / 2;
        bool alongX = Mathf.Abs(u.x) > 0.5f;
        Vector3 Sz(float l, float h) => alongX ? new Vector3(l, h, WT) : new Vector3(WT, h, l);
        void Seg(float from, float to, float y0, float y1, Material m)
        {
            if (to - from < 0.01f || y1 - y0 < 0.01f) return; var c = a + u * ((from + to) / 2); c.y = y0 + (y1 - y0) / 2 + 0.12f;
            Art.Box(p, c, Sz(to - from + (alongX ? WT : 0), y1 - y0), m, true, 0.5f, "wall");
        }
        var m0 = outM;
        if (op == "none") { Seg(0, len, 0, WH, m0); return; }
        float w = op == "window" ? 1.5f : DW; float s0 = len / 2 - w / 2, s1 = len / 2 + w / 2;
        if (op == "window")
        {
            Seg(0, s0, 0, WH, m0); Seg(s1, len, 0, WH, m0); Seg(s0, s1, 0, 0.95f, m0); Seg(s0, s1, 2.15f, WH, m0);
            var gc = mid; gc.y = 0.12f + 1.55f; Art.Box(p, gc, Sz(w, 1.2f) - (alongX ? new Vector3(0, 0, 0.14f) : new Vector3(0.14f, 0, 0)), Art.Lit(new Color(0.35f, 0.5f, 0.6f), null, 0.95f, 0.2f), true, 1, "glass");
            var fm = Art.Lit(new Color(0.93f, 0.92f, 0.88f), "plaster", 0.35f);
            Vector3 D3(float l, float h, float d) => alongX ? new Vector3(l, h, d) : new Vector3(d, h, l);
            float fw = 0.08f, fd = WT + 0.07f;
            gc = mid; gc.y = 0.12f + 0.95f - 0.03f; Art.Box(p, gc, D3(w + 0.3f, 0.07f, WT + 0.2f), fm, false); // sill (protrudes)
            gc.y = 0.12f + 2.15f + fw / 2; Art.Box(p, gc, D3(w + 2 * fw, fw, fd), fm, false); // header
            foreach (float e in new[] { s0 - fw / 2, s1 + fw / 2 }) { var sc = a + u * e; sc.y = 0.12f + 1.55f; Art.Box(p, sc, D3(fw, 1.2f + fw, fd), fm, false); }
            gc.y = 0.12f + 1.55f; Art.Box(p, gc, D3(0.05f, 1.2f, WT - 0.06f), fm, false); // mullion
            gc.y = 0.12f + 1.8f; Art.Box(p, gc, D3(w, 0.045f, WT - 0.06f), fm, false); // transom
            // daylight spilling in: spot just inside the window, aimed into the room and down
            var n = new Vector3(-u.z, 0, u.x); if (Vector3.Dot(n, -mid) < 0) n = -n;
            var sl = new GameObject("winlight").AddComponent<Light>(); sl.transform.SetParent(p, false);
            sl.transform.localPosition = mid + n * 0.22f + Vector3.up * 2.05f; sl.transform.localRotation = Quaternion.LookRotation((n * 1.0f + Vector3.down * 0.75f).normalized);
            sl.type = LightType.Spot; sl.spotAngle = 85; sl.innerSpotAngle = 30; sl.range = 6.5f; sl.intensity = 3.2f; sl.color = new Color(0.82f, 0.9f, 1f); sl.shadows = LightShadows.None;
            return;
        }
        Seg(0, s0, 0, WH, m0); Seg(s1, len, 0, WH, m0); Seg(s0, s1, DH, WH, m0);
        if (op == "front" || op == "locked")
        {
            var dg = new GameObject(op == "locked" ? "LockedDoor" : "Door"); dg.transform.SetParent(p, false);
            var hingePos = a + u * s0; hingePos.y = 0.12f; dg.transform.localPosition = hingePos;
            dg.transform.localRotation = Quaternion.LookRotation(alongX ? Vector3.forward : Vector3.right) * Quaternion.Euler(0, alongX ? 0 : 0, 0);
            var hinge = new GameObject("hinge").transform; hinge.SetParent(dg.transform, false);
            var leafDir = alongX ? Vector3.right : Vector3.forward; // in parent space
            var leaf = Art.Box(hinge, Vector3.zero, new Vector3(DW - 0.04f, DH - 0.03f, 0.06f), op == "locked" ? Art.Lit(new Color(0.45f, 0.18f, 0.12f), null, 0.3f) : Art.Lit(new Color(0.55f, 0.36f, 0.22f), "woodfloor", 0.3f), true, 1, "doorleaf");
            // orient leaf: hinge local x should point along u
            hinge.localRotation = Quaternion.identity; dg.transform.rotation = p.rotation * Quaternion.FromToRotation(Vector3.right, u);
            leaf.transform.localPosition = new Vector3((DW - 0.04f) / 2, DH / 2, 0);
            Art.Prim(PrimitiveType.Sphere, leaf.transform, new Vector3(0.4f, 0, 0.06f), new Vector3(0.07f, 0.07f, 0.07f), Art.Lit(new Color(0.85f, 0.75f, 0.4f), null, 0.8f, 1f));
            var d = dg.AddComponent<Door>(); d.hinge = hinge; d.locked = op == "locked"; d.col = leaf.GetComponent<Collider>(); d.reach = 2.4f;
            if (d.locked)
            {
                var steel = Art.Lit(new Color(0.55f, 0.56f, 0.58f), "metal", 0.6f, 0.9f); var brass = Art.Lit(new Color(0.78f, 0.62f, 0.28f), null, 0.7f, 1f);
                foreach (float sd in new[] { 1f, -1f })
                {
                    Art.Box(leaf.transform, new Vector3(0.33f, 0.02f, sd * 0.05f), new Vector3(0.16f, 0.05f, 0.03f), steel, false); // hasp
                    Art.Box(leaf.transform, new Vector3(0.33f, -0.07f, sd * 0.075f), new Vector3(0.09f, 0.1f, 0.035f), brass, false); // padlock body
                    var sh = Art.Prim(PrimitiveType.Cylinder, leaf.transform, new Vector3(0.33f, 0.0f, sd * 0.075f), new Vector3(0.05f, 0.03f, 0.05f), steel); // shackle
                    Art.Box(leaf.transform, new Vector3(0.33f, -0.075f, sd * 0.094f), new Vector3(0.012f, 0.03f, 0.004f), Art.Emissive(new Color(1f, 0.3f, 0.2f), 1.2f), false); // tiny indicator
                }
            }
        }
    }

    static readonly Color[] RoofTints = { Color.white, new Color(0.78f, 0.62f, 0.55f), new Color(0.62f, 0.68f, 0.76f), new Color(0.58f, 0.52f, 0.5f), new Color(0.85f, 0.8f, 0.72f) };
    static void Roof(Transform p, float W, float D, float y, Color tint)
    {
        bool alongX = W >= D; float span = alongX ? D : W, len = alongX ? W : D; float rise = span * 0.32f;
        var m = Art.Lit(tint, "roof", 0.2f); float slope = Mathf.Sqrt(rise * rise + span * span / 4);
        float ang = Mathf.Atan2(rise, span / 2) * Mathf.Rad2Deg;
        for (int s = -1; s <= 1; s += 2)
        {
            var g = Art.Box(p, Vector3.zero, alongX ? new Vector3(len, 0.3f, slope + 0.1f) : new Vector3(slope + 0.1f, 0.3f, len), m, false, 0.5f, "roof");
            if (alongX) { g.transform.localPosition = new Vector3(0, y + rise / 2, s * span / 4); g.transform.localRotation = Quaternion.Euler(s * ang, 0, 0); }
            else { g.transform.localPosition = new Vector3(s * span / 4, y + rise / 2, 0); g.transform.localRotation = Quaternion.Euler(0, 0, -s * ang); }
        }
        // fascia boards, gutters and downpipes along both eaves
        var fasc = Art.Lit(new Color(0.92f, 0.91f, 0.88f), "plaster", 0.3f); var gut = Art.Lit(new Color(0.42f, 0.44f, 0.45f), "metal", 0.45f, 0.7f);
        for (int s = -1; s <= 1; s += 2)
        {
            float e = span / 2 + 0.06f; float ey = y - 0.08f;
            Vector3 P(float along, float h, float off) => alongX ? new Vector3(along, h, s * (e + off)) : new Vector3(s * (e + off), h, along);
            Vector3 S(float l, float h, float d) => alongX ? new Vector3(l, h, d) : new Vector3(d, h, l);
            Art.Box(p, P(0, ey, 0), S(len + 0.1f, 0.32f, 0.05f), fasc, false);
            Art.Box(p, P(0, ey - 0.08f, 0.1f), S(len + 0.1f, 0.12f, 0.14f), gut, false);
            foreach (float k in new[] { -1f, 1f }) { Art.Box(p, P(k * (len / 2 - 0.62f), (ey - 0.1f) / 2, -0.45f + 0.12f), S(0.09f, ey - 0.1f, 0.09f), gut, false); Art.Box(p, P(k * (len / 2 - 0.62f), ey - 0.12f, -0.17f), S(0.08f, 0.08f, 0.5f), gut, false); }
        }
        // gable ends (triangular prisms)
        var gm = Art.Lit(new Color(0.9f, 0.86f, 0.78f), "plaster", 0.1f);
        for (int s = -1; s <= 1; s += 2)
        {
            var g = new GameObject("gable"); g.transform.SetParent(p, false);
            var mesh = new Mesh(); float hs = span / 2 - 0.25f, r2 = rise - 0.1f;
            var v = new[] { new Vector3(-hs, 0, 0), new Vector3(hs, 0, 0), new Vector3(0, r2, 0) };
            mesh.vertices = new[] { v[0], v[1], v[2], v[0], v[2], v[1] }; mesh.triangles = new[] { 0, 2, 1, 3, 5, 4 };
            mesh.uv = new[] { new Vector2(0, 0), new Vector2(span / 2, 0), new Vector2(span / 4, rise / 2), new Vector2(0, 0), new Vector2(span / 4, rise / 2), new Vector2(span / 2, 0) };
            mesh.RecalculateNormals(); g.AddComponent<MeshFilter>().sharedMesh = mesh; g.AddComponent<MeshRenderer>().sharedMaterial = gm;
            if (alongX) { g.transform.localPosition = new Vector3(s * (len / 2 - 0.45f), y, 0); g.transform.localRotation = Quaternion.Euler(0, 90, 0); }
            else { g.transform.localPosition = new Vector3(0, y, s * (len / 2 - 0.45f)); }
        }
    }

    static readonly Vector2[] corners = { new Vector2(-1, -1), new Vector2(1, -1), new Vector2(1, 1), new Vector2(-1, 1) };
    static void Furnish(Transform h, Room rm, float cell, bool locked, string style, int rows)
    {
        float e = cell / 2 - 0.25f; var c = rm.center; var th = L.theme;
        // corner slots: item placed at corner, back against the z-wall (rot faces room center along z)
        var slots = new List<int> { 0, 1, 2, 3 }; for (int i = 0; i < 4; i++) { int j = rng.Next(4); (slots[i], slots[j]) = (slots[j], slots[i]); }
        int si = 0;
        Vector3 Slot(Vector3 size, out float rot)
        {
            var k = corners[slots[si++ % 4]]; rot = k.y > 0 ? 180 : 0; // front faces toward center (+z if at -z wall)
            return c + new Vector3(k.x * (e - size.x / 2), 0, k.y * (e - size.z / 2));
        }
        void Cont(string kind, string label = null, bool special = false) { var ck = Data.CK[kind]; var pos = Slot(ck.size, out float rot); ContAt(kind, pos, rot, label, special); }
        void ContAt(string kind, Vector3 pos, float rot, string label = null, bool special = false)
        {
            var ck = Data.CK[kind]; GameObject g;
            if (ck.model != null) g = Art.Spawn(ck.model, h, pos, rot, ck.size, true);
            else
            {
                g = new GameObject(kind); g.transform.SetParent(h, false); g.transform.localPosition = pos; g.transform.localRotation = Quaternion.Euler(0, rot, 0);
                if (kind == "register") Props.Register(g.transform);
                else if (kind == "desk") Props.Desk(g.transform, rng);
                else if (kind == "rack") Props.ServerRack(g.transform, rng);
                else if (kind == "cooler") Props.Cooler(g.transform, rng);
                else
                {
                    var mm = Art.Lit(new Color(0.35f, 0.37f, 0.4f), "metal", 0.55f, 0.6f);
                    Art.Box(g.transform, new Vector3(0, ck.size.y / 2, 0), ck.size, mm, true, 1, "safe");
                    Art.Prim(PrimitiveType.Cylinder, g.transform, new Vector3(0, ck.size.y / 2, ck.size.z / 2 + 0.02f), new Vector3(0.18f, 0.02f, 0.18f), Art.Lit(new Color(0.8f, 0.7f, 0.3f), null, 0.8f, 1f)).transform.localRotation = Quaternion.Euler(90, 0, 0);
                    Art.Box(g.transform, new Vector3(ck.size.x * 0.3f, ck.size.y * 0.75f, ck.size.z / 2 + 0.01f), new Vector3(0.08f, 0.05f, 0.01f), Art.Emissive(new Color(0.2f, 1f, 0.4f), 3f), false);
                }
            }
            var lc = g.AddComponent<LootContainer>(); lc.Init(kind, rng, th, L.vmult, luck, label); lc.special = special; L.containers.Add(lc);
        }
        void Deco(string model, Vector3 size, bool coll = true) { var pos = Slot(size, out float rot); Art.Spawn(model, h, pos, rot, size, coll); }
        void DecoAt(string model, Vector3 pos, float rot, Vector3 size, bool coll = true) => Art.Spawn(model, h, pos, rot, size, coll);
        if (style == "mart" || style == "office" || style == "base") { FurnishBig(); return; }
        if (locked)
        {
            Cont("safe", "床頭保險箱", true); Cont("wardrobe", null, true);
            Deco("Furn/bedDouble", new Vector3(1.7f, 0, 2.1f));
            Art.Spawn("Env/Health", h, c + new Vector3(0, 0, 0), 30, new Vector3(0.5f, 0, 0), false);
            return;
        }
        switch (rm.type)
        {
            case "bedroom":
                Deco(R() < 0.5f ? "Furn/bedDouble" : "Furn/bedSingle", new Vector3(1.7f, 0, 2.1f)); Cont("wardrobe"); if (R() < 0.6f) Cont("drawer"); else Deco("Furn/lampRoundFloor", new Vector3(0.4f, 1.6f, 0.4f));
                Art.Spawn("Furn/rugRound", h, c, 0, new Vector3(2.2f, 0, 0), false); break;
            case "kitchen":
                Cont("fridge"); Deco("Furn/kitchenStove", new Vector3(0.8f, 0, 0.7f)); Deco("Furn/kitchenSink", new Vector3(0.9f, 0, 0.7f));
                Art.Spawn("Furn/table", h, c, R() * 90, new Vector3(1.4f, 0, 0)); if (R() < 0.6f) Cont("box"); break;
            case "bath":
                Deco("Furn/bathtub", new Vector3(1.7f, 0, 0.8f)); Deco("Furn/toilet", new Vector3(0.5f, 0, 0.7f)); Deco("Furn/washer", new Vector3(0.7f, 0, 0.7f)); Cont("box"); break;
            default:
                Deco("Furn/loungeSofa", new Vector3(2.0f, 0, 0.9f)); Deco("Furn/cabinetTelevision", new Vector3(1.6f, 0, 0.5f));
                Art.Spawn("Furn/rugRectangle", h, c, 0, new Vector3(2.6f, 0, 0), false); Art.Spawn("Furn/tableCoffee", h, c + new Vector3(0, 0.02f, 0), 0, new Vector3(1.1f, 0, 0));
                if (R() < 0.7f) Cont("box"); else Deco("Furn/pottedPlant", new Vector3(0.5f, 0, 0.5f)); break;
        }
        void FurnishBig()
        {
            bool mart = style == "mart";
            Vector3 shelf = new Vector3(1.1f, 2.1f, 0.52f), disp = new Vector3(1.08f, 1.56f, 0.4f), desk = new Vector3(1.6f, 0.9f, 0.8f), rack = new Vector3(0.92f, 1.9f, 0.6f), fr = new Vector3(0.8f, 1.85f, 0.85f);
            if (locked)
            {
                if (mart) { Cont("safe", "店長保險箱", true); Cont("shelf", "店長私藏貨架", true); Deco("PH/cardboard_box_01", new Vector3(0.6f, 0, 0.6f)); }
                else if (style == "base") { Cont("safe", "軍火庫保險箱", true); Cont("milcrate", "機密武器箱", true); Deco("PH/ammo_box", new Vector3(0, 0.26f, 0)); }
                else { Cont("safe", "總座保險箱", true); Cont("desk", "總座辦公桌", true); Deco("PH/potted_plant_01", new Vector3(0, 1.2f, 0)); }
                Art.Spawn("Env/Health", h, c + new Vector3(0.6f, 0, 0), 30, new Vector3(0.5f, 0, 0), false);
                return;
            }
            switch (rm.type)
            {
                case "aisle":
                {
                    if (rm.cz == 0 && rm.cx % 2 == 0 && R() < 0.7f)
                    {
                        // checkout counter with a register container on top
                        var cp = c + new Vector3(0, 0, 1.0f);
                        Art.Box(h, cp + new Vector3(0, 0.475f, 0), new Vector3(2.4f, 0.95f, 0.7f), Art.Lit(new Color(0.85f, 0.83f, 0.8f), "plaster", 0.3f), true, 1, "counter");
                        ContAt("register", cp + new Vector3(0.5f, 0.95f, 0), 180);
                        Art.Box(h, cp + new Vector3(-0.6f, 1.1f, 0), new Vector3(0.6f, 0.3f, 0.4f), Art.Lit(new Color(0.9f, 0.9f, 0.85f), null, 0.3f), false);
                        break;
                    }
                    // gondola: 2x2 shelf units back to back
                    int ci = rng.Next(4); string ck = R() < 0.6f ? "shelf" : "display";
                    for (int i = 0; i < 4; i++)
                    {
                        float sx = (i % 2 == 0 ? -0.56f : 0.56f), sz = i < 2 ? -0.28f : 0.28f; float rot = i < 2 ? 180 : 0;
                        var pos = c + new Vector3(sx, 0, sz); bool isShelf = ck == "shelf";
                        if (i == ci) ContAt(ck, pos, rot); else DecoAt(isShelf ? "PH/steel_frame_shelves_01" : "PH/wooden_display_shelves_01", pos, rot, isShelf ? shelf : disp);
                    }
                    if (R() < 0.6f) DecoAt("PH/cardboard_box_01", c + new Vector3(RR(-2.2f, 2.2f), 0, -2.2f), R() * 360, new Vector3(0.5f, 0, 0.5f));
                    break;
                }
                case "coolroom":
                    for (int i = 0; i < 3; i++) { var pos = c + new Vector3(-1.7f + i * 1.7f, 0, e - 0.45f); if (i != 1) ContAt("cooler", pos, 180); else { var dg = new GameObject("coolerDeco"); dg.transform.SetParent(h, false); dg.transform.localPosition = pos; dg.transform.localRotation = Quaternion.Euler(0, 180, 0); Props.Cooler(dg.transform, rng); } }
                    DecoAt("PH/plastic_crate_01", c + new Vector3(-1.5f, 0, -1.6f), 20, new Vector3(0.5f, 0, 0.5f)); break;
                case "stock":
                    Cont("box"); Cont("shelf"); Deco("PH/steel_frame_shelves_01", shelf);
                    for (int i = 0; i < 4; i++) DecoAt("PH/cardboard_box_01", c + new Vector3(RR(-1.2f, 1.2f), 0, RR(-1.2f, 1.2f)), R() * 360, new Vector3(0.55f, 0, 0.55f));
                    break;
                case "staff":
                    Cont("drawer"); Deco("PH/metal_trash_can", new Vector3(0, 0.7f, 0)); Art.Spawn("Furn/table", h, c, 0, new Vector3(1.4f, 0, 0)); Cont("box"); break;
                case "cubicle":
                    ContAt("desk", c + new Vector3(-1.2f, 0, -1.1f), 0); { var dg = new GameObject("deskDeco"); dg.transform.SetParent(h, false); dg.transform.localPosition = c + new Vector3(1.2f, 0, 1.1f); dg.transform.localRotation = Quaternion.Euler(0, 180, 0); Props.Desk(dg.transform, rng); }
                    Props.Partition(h, c + new Vector3(0, 0, 0.0f), 2.8f);
                    DecoAt("PH/potted_plant_01", c + new Vector3(2.3f, 0, -2.3f), R() * 360, new Vector3(0, 1.1f, 0));
                    DecoAt("PH/metal_trash_can", c + new Vector3(-0.1f, 0, -1.4f), R() * 360, new Vector3(0, 0.6f, 0));
                    if (R() < 0.4f) Cont("drawer"); break;
                case "server":
                    for (int i = 0; i < 4; i++) { var pos = c + new Vector3(-1.5f + i * 1.0f, 0, e - 0.55f); if (i == 1 || i == 3) ContAt("rack", pos, 180); else { var dg = new GameObject("rackDeco"); dg.transform.SetParent(h, false); dg.transform.localPosition = pos; dg.transform.localRotation = Quaternion.Euler(0, 180, 0); Props.ServerRack(dg.transform, rng); } }
                    Art.Box(h, c + new Vector3(0, WH - 0.35f, e - 0.55f), new Vector3(4.2f, 0.08f, 0.4f), Art.Lit(new Color(0.25f, 0.27f, 0.3f), "metal", 0.4f, 0.6f), false); // cable tray
                    break;
                case "pantry":
                    Cont("fridge"); Deco("Furn/kitchenSink", new Vector3(0.9f, 0, 0.7f)); Deco("PH/metal_trash_can", new Vector3(0, 0.7f, 0)); Art.Spawn("Furn/table", h, c, 0, new Vector3(1.2f, 0, 0)); break;
                case "storage":
                {
                    // pallet racking: orange uprights, blue beams, 3 levels of crates; one crate on the floor is lootable
                    Props.PalletRack(h, c + new Vector3(0, 0, -0.9f), 0, rng); Props.PalletRack(h, c + new Vector3(0, 0, 0.9f), 180, rng);
                    if (R() < 0.75f) ContAt(R() < 0.6f ? "crate" : "toolchest", c + new Vector3(RR(-1.6f, 1.6f), 0, -2.3f), 0);
                    if (R() < 0.4f) DecoAt("PH/wooden_crate_02", c + new Vector3(RR(-2f, 2f), 0, 2.3f), R() * 30, new Vector3(1.16f, 0, 0));
                    break;
                }
                case "armory":
                    Cont("toolchest"); Cont("crate"); Deco("PH/metal_tool_chest", new Vector3(0.69f, 0, 0));
                    Art.Spawn("Furn/table", h, c, 0, new Vector3(1.6f, 0, 0));
                    for (int i = 0; i < 3; i++) DecoAt("PH/ammo_box", c + new Vector3(-0.4f + i * 0.3f, 0.78f, RR(-0.1f, 0.1f)), R() * 40, new Vector3(0, 0.26f, 0), false);
                    break;
                case "barracks":
                    Deco("Furn/bedSingle", new Vector3(1.0f, 0, 2.0f)); Deco("Furn/bedSingle", new Vector3(1.0f, 0, 2.0f)); Cont("drawer"); Cont("crate");
                    DecoAt("PH/metal_jerrycan_green", c + new Vector3(0.3f, 0, 0.2f), R() * 360, new Vector3(0, 0.5f, 0)); break;
                case "garage":
                    DecoAt("PH/covered_car", c + new Vector3(0, 0, -0.3f), 90, new Vector3(0, 0, 4.0f)); Cont("toolchest");
                    for (int i = 0; i < 3; i++) DecoAt("PH/metal_jerrycan_green", c + new Vector3(2.3f, 0, -2.2f + i * 0.45f), 90, new Vector3(0, 0.5f, 0));
                    DecoAt("PH/Barrel_01", c + new Vector3(-2.3f, 0, 2.2f), 0, new Vector3(0, 0.9f, 0)); break;
                default: // meeting
                    Art.Spawn("Furn/table", h, c, 90, new Vector3(2.6f, 0, 0)); Cont("drawer"); Deco("PH/potted_plant_01", new Vector3(0, 1.2f, 0)); if (R() < 0.5f) Cont("box"); break;
            }
        }
    }

    static void FlatRoof(Transform h, float W, float D, string style, float x0, float z0)
    {
        bool mart = style == "mart"; float y = WH + 0.12f;
        var rm = Art.Lit(new Color(0.55f, 0.55f, 0.56f), "concrete", 0.2f); var pm = Art.Lit(mart ? new Color(0.95f, 0.95f, 0.93f) : new Color(0.62f, 0.66f, 0.7f), "plaster", 0.2f);
        Art.Box(h, new Vector3(0, y + 0.1f, 0), new Vector3(W + 0.6f, 0.2f, D + 0.6f), rm, true, 0.5f, "roof_slab");
        Art.Box(h, new Vector3(0, y + 0.55f, -D / 2 - 0.2f), new Vector3(W + 0.6f, 0.7f, 0.2f), pm, false); Art.Box(h, new Vector3(0, y + 0.55f, D / 2 + 0.2f), new Vector3(W + 0.6f, 0.7f, 0.2f), pm, false);
        Art.Box(h, new Vector3(-W / 2 - 0.2f, y + 0.55f, 0), new Vector3(0.2f, 0.7f, D + 0.6f), pm, false); Art.Box(h, new Vector3(W / 2 + 0.2f, y + 0.55f, 0), new Vector3(0.2f, 0.7f, D + 0.6f), pm, false);
        if (style == "base")
        {
            // warehouse: low ridge vents + stencil band + floodlights
            var mm = Art.Lit(new Color(0.45f, 0.48f, 0.44f), "metal", 0.4f, 0.6f);
            for (int i = -1; i <= 1; i++) Art.Box(h, new Vector3(i * W / 3.2f, y + 0.55f, 0), new Vector3(1.4f, 0.7f, 1.4f), mm, false);
            Art.Box(h, new Vector3(0, WH - 0.35f, -D / 2 - 0.13f), new Vector3(W + 0.3f, 0.5f, 0.05f), Art.Lit(new Color(0.85f, 0.7f, 0.15f), null, 0.3f), false);
            for (int i = 0; i < 14; i++) Art.Box(h, new Vector3(-W / 2 + 0.4f + i * (W / 14f), WH - 0.35f, -D / 2 - 0.165f), new Vector3(0.35f, 0.5f, 0.02f), Art.Lit(new Color(0.08f, 0.08f, 0.08f), null, 0.3f), false).transform.localRotation = Quaternion.Euler(0, 0, 35);
            foreach (float fx in new[] { -W / 2 + 1.5f, W / 2 - 1.5f }) { Art.Box(h, new Vector3(fx, WH + 0.95f, -D / 2 - 0.3f), new Vector3(0.6f, 0.35f, 0.25f), mm, false); Art.Box(h, new Vector3(fx, WH + 0.95f, -D / 2 - 0.43f), new Vector3(0.5f, 0.25f, 0.02f), Art.Emissive(new Color(1f, 0.95f, 0.8f), 3f), false); }
            return;
        }
        if (mart)
        {
            // convenience-store colour band + lit sign
            // thick tri-colour fascia across the whole front (visible from the street)
            Art.Box(h, new Vector3(0, WH - 0.12f, -D / 2 - 0.2f), new Vector3(W + 0.6f, 0.34f, 0.12f), Art.Emissive(new Color(1f, 0.45f, 0.1f), 2.2f), false);
            Art.Box(h, new Vector3(0, WH + 0.25f, -D / 2 - 0.2f), new Vector3(W + 0.6f, 0.4f, 0.12f), Art.Emissive(new Color(0.1f, 0.75f, 0.35f), 2.2f), false);
            Art.Box(h, new Vector3(0, WH + 0.62f, -D / 2 - 0.2f), new Vector3(W + 0.6f, 0.34f, 0.12f), Art.Emissive(new Color(0.9f, 0.12f, 0.12f), 2.2f), false);
            // big rooftop sign box
            Art.Box(h, new Vector3(0, y + 1.9f, -D / 2 + 0.1f), new Vector3(9f, 2.2f, 0.4f), Art.Lit(Color.white, null, 0.4f), false, 1, "sign");
            Art.Box(h, new Vector3(0, y + 1.9f, -D / 2 - 0.12f), new Vector3(8.4f, 1.7f, 0.04f), Art.Emissive(new Color(0.15f, 0.65f, 0.3f), 2.4f), false);
            Art.Box(h, new Vector3(-2.6f, y + 1.9f, -D / 2 - 0.15f), new Vector3(1.6f, 1.2f, 0.03f), Art.Emissive(new Color(1f, 0.5f, 0.1f), 3f), false);
            Art.Box(h, new Vector3(1.2f, y + 1.9f, -D / 2 - 0.15f), new Vector3(4.2f, 0.36f, 0.03f), Art.Emissive(Color.white, 3f), false);
            Art.Box(h, new Vector3(1.2f, y + 1.45f, -D / 2 - 0.15f), new Vector3(4.2f, 0.22f, 0.03f), Art.Emissive(new Color(0.9f, 0.12f, 0.12f), 3f), false);
            foreach (float sx in new[] { -3.5f, 3.5f }) Art.Box(h, new Vector3(sx, y + 0.5f, -D / 2 + 0.25f), new Vector3(0.18f, 1.0f, 0.18f), Art.Lit(new Color(0.4f, 0.4f, 0.42f), "metal", 0.4f, 0.7f), false);
            // roadside pylon sign at the parking corner
            var pl = new Vector3(W / 2 + 1.5f, 0, -D / 2 - 3.2f);
            Art.Box(h, pl + new Vector3(0, 2.5f, 0), new Vector3(0.3f, 5f, 0.3f), Art.Lit(new Color(0.4f, 0.4f, 0.42f), "metal", 0.4f, 0.7f), true, 1, "pylon");
            Art.Box(h, pl + new Vector3(0, 5.6f, 0), new Vector3(2.2f, 2.2f, 0.4f), Art.Lit(Color.white, null, 0.4f), false);
            foreach (float sz in new[] { -0.21f, 0.21f }) { Art.Box(h, pl + new Vector3(0, 6.1f, sz), new Vector3(2.0f, 0.5f, 0.02f), Art.Emissive(new Color(1f, 0.45f, 0.1f), 3f), false); Art.Box(h, pl + new Vector3(0, 5.55f, sz), new Vector3(2.0f, 0.5f, 0.02f), Art.Emissive(new Color(0.1f, 0.75f, 0.35f), 3f), false); Art.Box(h, pl + new Vector3(0, 5.0f, sz), new Vector3(2.0f, 0.5f, 0.02f), Art.Emissive(new Color(0.9f, 0.12f, 0.12f), 3f), false); }
        }
        else
        {
            // office: rooftop HVAC units + vertical fins on facade
            var mm = Art.Lit(new Color(0.7f, 0.72f, 0.74f), "metal", 0.4f, 0.6f);
            // real AC condenser units on steel stands near the front edge so they read from the street
            var st = Art.Lit(new Color(0.3f, 0.32f, 0.34f), "metal", 0.4f, 0.7f);
            for (int i = 0; i < 4; i++) { var ap = new Vector3(-W / 2 + W * (i + 0.5f) / 4f, y + 0.2f, -D / 2 + 1.4f); Art.Box(h, ap + new Vector3(0, 0.3f, 0), new Vector3(2.0f, 0.6f, 1.0f), st, false); Art.Spawn("PH/exterior_aircon_unit", h, ap + new Vector3(0, 0.6f, 0), 180, new Vector3(2.0f, 0, 0), false); }
            Art.Box(h, new Vector3(-W / 4, y + 0.8f, D / 4), new Vector3(2.4f, 1.2f, 1.6f), mm, false);
            // side-wall mounted units at street level
            foreach (float sx in new[] { -1f, 1f }) Art.Spawn("PH/exterior_aircon_unit", h, new Vector3(sx * (W / 2 + 0.55f), 1.8f, -D / 2 + 3f), sx * 90, new Vector3(1.6f, 0, 0), false);
            for (float x = x0; x <= -x0 + 0.01f; x += 3f) Art.Box(h, new Vector3(x, (WH + 1f) / 2, z0 - 0.2f), new Vector3(0.18f, WH + 1f, 0.25f), Art.Lit(new Color(0.35f, 0.38f, 0.42f), null, 0.5f, 0.4f), false);
        }
    }

    static void BaseYard(Transform root)
    {
        // sandbag posts, barrels, jerrycans and crate stacks around the depot
        Art.Spawn("Env/SackTrench", root, new Vector3(-26, 0, 11f), 0, new Vector3(3.2f, 0, 0)); Art.Spawn("Env/SackTrench", root, new Vector3(14, 0, 11f), 0, new Vector3(3.2f, 0, 0));
        for (int i = 0; i < 5; i++) { var p = new Vector3(RR(-36, 24), 0, RR(-29, -27)); if ((p - new Vector3(34, 0, -24)).magnitude < 12) continue; Art.Spawn(R() < 0.5f ? "PH/Barrel_01" : "PH/metal_jerrycan_green", root, p, R() * 360, new Vector3(0, R() < 0.5f ? 0.9f : 0.5f, 0)); }
        for (int i = 0; i < 3; i++) { var p = new Vector3(-30 + i * 22, 0, -11.5f); Art.Spawn("PH/wooden_military_crate", root, p, R() * 20, new Vector3(1.24f, 0, 0)); Art.Spawn("PH/wooden_military_crate", root, p + Vector3.up * 0.47f, R() * 20, new Vector3(1.24f, 0, 0), false); }
        Art.Spawn("PH/utility_box_01", root, new Vector3(-24, 0, 11.5f), 180, new Vector3(0, 1.1f, 0));
    }

    static void Vegetation(Transform root)
    {
        Physics.SyncTransforms(); var vr = new System.Random(rng.Next()); var grp = new GameObject("Vegetation").transform; grp.SetParent(root, false);
        int placed = 0;
        for (int i = 0; i < 5000 && placed < 2100; i++)
        {
            float x = (float)vr.NextDouble() * 112 - 56, z = (float)vr.NextDouble() * 112 - 56; if (Mathf.Abs(z) < 6.4f) continue;
            if (!Physics.Raycast(new Vector3(x, 30, z), Vector3.down, out var hit, 40) || hit.collider.name != "grass_ground") continue;
            // cluster: a few clumps around each anchor
            int n = 1 + vr.Next(4);
            for (int j = 0; j < n; j++)
            {
                var pp = new Vector3(x + (float)vr.NextDouble() * 1.6f - 0.8f, 0, z + (float)vr.NextDouble() * 1.6f - 0.8f);
                double t = vr.NextDouble(); string id = t < 0.72 ? "PH/grass_medium_01" : t < 0.93 ? "PH/weed_plant_02" : "PH/shrub_02";
                float h = id.EndsWith("shrub_02") ? 0.6f + (float)vr.NextDouble() * 0.6f : id.EndsWith("weed_plant_02") ? 0.35f + (float)vr.NextDouble() * 0.35f : 0.35f + (float)vr.NextDouble() * 0.4f;
                if (Art.Clump(id, grp, pp, (float)vr.NextDouble() * 360, h, vr)) placed++;
            }
        }
        Debug.Log("[veg] clumps " + placed);
    }

    static void Yard(Transform root, string style)
    {
        // street lights along road
        for (float x = -50; x <= 50; x += 20)
        {
            for (int s = -1; s <= 1; s += 2)
            {
                if ((x / 20 + s) % 2 == 0) continue;
                var sl = Art.Spawn("Env/StreetLight", root, new Vector3(x, 0, s * 6.2f), s > 0 ? 180 : 0, new Vector3(0, 5.5f, 0));
                var bc = sl.GetComponent<BoxCollider>(); if (bc) { bc.size = new Vector3(0.3f, bc.size.y, 0.3f); }
            }
        }
        // trees
        int trees = rng.Next(16, 24);
        for (int i = 0; i < trees; i++)
        {
            var p = new Vector3(RR(-56, 56), 0, (R() < 0.5f ? 1 : -1) * RR(28, 55)); if (R() < 0.3f) p = new Vector3(RR(-56, -35) * (R() < 0.5f ? 1 : -1), 0, RR(-50, 50));
            if ((p - new Vector3(34, 0, -24)).magnitude < 12 || Blocked(p, 2.5f)) continue;
            var t = Art.Spawn("Env/Tree_" + rng.Next(1, 5), root, p, R() * 360, new Vector3(0, RR(4.5f, 7.5f), 0));
            var bc = t.GetComponent<BoxCollider>(); if (bc) { bc.size = new Vector3(0.6f, bc.size.y, 0.6f); }
        }
        // fences around north/south lots
        if (style == "house") for (float x = -38; x < 22; x += 2.1f) { Art.Spawn("Env/Fence", root, new Vector3(x, 0, 29.5f), 0, new Vector3(2.1f, 1.9f, 0), true, false); }
        if (style == "house") for (float x = -50; x < 6; x += 2.1f) { Art.Spawn("Env/Fence", root, new Vector3(x, 0, -30f), 0, new Vector3(2.1f, 1.9f, 0), true, false); }
        // street clutter
        Art.Spawn("Ext/sedan", root, new Vector3(RR(-30, 10), 0.02f, R() < 0.5f ? -2.6f : 2.6f), RR(85, 95) + (R() < 0.5f ? 0 : 180), new Vector3(0, 1.45f, 0));
        Art.Spawn("Env/TrashContainer", root, new Vector3(-36, 0, 7.6f), 0, new Vector3(1.8f, 0, 0));
        Art.Spawn("Env/TrashContainer", root, new Vector3(18, 0, -7.6f), 180, new Vector3(1.8f, 0, 0));
        for (int i = 0; i < 6; i++) Art.Spawn("Env/TrafficCone", root, new Vector3(RR(-45, 45), 0, RR(-3.5f, 3.5f)), R() * 360, new Vector3(0, 0.7f, 0), false);
        for (int i = 0; i < 5; i++) { var bp = new Vector3(RR(-50, 50), 0, (R() < 0.5f ? 1 : -1) * RR(9, 26)); if (Blocked(bp, 0.8f)) continue; Art.Spawn(R() < 0.5f ? "Env/ExplodingBarrel" : "Env/GasCan", root, bp, R() * 360, new Vector3(0, 1.0f, 0)); }
        for (int i = 0; i < 8; i++) Art.Spawn("Env/Debris_Papers_1", root, new Vector3(RR(-55, 55), 0.02f, RR(-6, 6)), R() * 360, new Vector3(1.2f, 0, 0), false);
        Art.Spawn("Env/Pallet", root, new Vector3(26, 0, -10), 20, new Vector3(1.4f, 0, 0));
        Art.Spawn("Env/CardboardBoxes_3", root, new Vector3(27, 0, -11.5f), 10, new Vector3(1.4f, 0, 0));
        Art.Spawn("Env/Debris_Tires", root, new Vector3(-46, 0, -9), 0, new Vector3(1.4f, 0, 0));
        Art.Spawn("Env/WaterTank_Floor", root, new Vector3(-50, 0, 22), 0, new Vector3(3f, 0, 0));
    }
}
