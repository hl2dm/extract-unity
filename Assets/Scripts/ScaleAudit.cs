using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// v0.7 world-scale audit: measures every prop family in kid-relative units and checks it against the scale bible (review/scale_bible.md).
public static class ScaleAudit
{
    public const float Kid = 1.30f;               // reference kid height (m)
    public struct Rule { public string key, label; public float hMin, hMax; public Rule(string k, string l, float a, float b) { key = k; label = l; hMin = a; hMax = b; } }
    // height ranges in metres (derived from Kid = 1.30; see scale_bible.md)
    public static Rule[] Rules = {
        new Rule("sk_sofa", "sofa", 0.75f, 1.00f), new Rule("sk_rtable", "table", 0.42f, 0.80f), new Rule("sk_stool", "stool/chair", 0.38f, 0.55f),
        new Rule("sk_bed", "bed (headboard)", 0.45f, 1.30f), new Rule("sk_counter", "counter (+tap)", 0.85f, 1.40f), new Rule("sk_tub", "bathtub", 0.45f, 0.72f),
        new Rule("sk_toilet", "toilet", 0.55f, 0.90f), new Rule("sk_tv", "tv on stand", 0.9f, 1.6f), new Rule("sk_flamp", "floor lamp", 1.3f, 1.8f),
        new Rule("Cont_fridge", "fridge", 1.6f, 2.0f), new Rule("Cont_cooler", "drink cooler", 1.6f, 2.0f), new Rule("Cont_wardrobe", "wardrobe", 1.7f, 2.25f),
        new Rule("Cont_drawer", "drawer chest", 0.6f, 1.1f), new Rule("Cont_box", "box/crate", 0.3f, 0.9f), new Rule("Cont_shelf", "store shelf", 1.6f, 2.6f),
        new Rule("Cont_display", "display case", 1.4f, 2.1f), new Rule("Cont_safe", "safe", 0.4f, 0.9f), new Rule("Cont_register", "till (lid open)", 0.2f, 0.7f),
        new Rule("Door", "interior door", 2.0f, 2.45f), new Rule("LockedDoor", "locked door", 2.0f, 2.45f), new Rule("sk_doorvis", "shop/house door", 2.0f, 2.45f),
        new Rule("sk_lamp", "street lamp", 3.2f, 4.6f), new Rule("sk_tree", "tree", 3.0f, 7.0f),
        new Rule("sk_bench", "bench", 0.7f, 1.1f), new Rule("sk_bin", "trash bin", 0.7f, 1.1f), new Rule("sk_mailbox", "mailbox", 1.0f, 1.5f),
        new Rule("sk_scooter", "parked scooter", 1.0f, 1.40f), new Rule("RideScooter", "ride scooter", 1.2f, 1.6f), new Rule("slide", "slide", 1.6f, 2.6f),
        new Rule("Bus", "bus", 2.8f, 3.8f), new Rule("Cat", "cat (toy)", 0.35f, 0.75f), new Rule("Dog", "dog (toy)", 0.45f, 0.95f),
        new Rule("TownHouse", "town house", 6f, 16f), new Rule("sk_text3d", "sign text", 0.3f, 1.4f), new Rule("GroundLoot", "ground loot", 0.05f, 0.7f),
    };
    public struct M { public int n; public float h, w, d, hMin, hMax; }
    public static int Fails;
    public static readonly List<string> Lines = new List<string>();

    static Bounds LocalB(Transform g, out bool any)
    {
        any = false; var b = new Bounds();
        foreach (var r in g.GetComponentsInChildren<Renderer>())
        {
            if (!r.enabled || r is ParticleSystemRenderer || r.gameObject.layer == Art.VMLayer) continue;
            var mf = r.GetComponent<MeshFilter>(); Bounds mb; Transform t = r.transform;
            if (mf && mf.sharedMesh) mb = mf.sharedMesh.bounds; else if (r is SkinnedMeshRenderer s && s.sharedMesh) mb = s.sharedMesh.bounds; else continue;
            if (mb.size.sqrMagnitude < 1e-8f || (mf && mf.sharedMesh && mf.sharedMesh.vertexCount == 0)) continue;   // v0.9.5: empty text meshes (pet "!" mark) are not geometry
            for (int i = 0; i < 8; i++)
            {
                var c = mb.center + Vector3.Scale(mb.extents, new Vector3((i & 1) * 2 - 1, ((i >> 1) & 1) * 2 - 1, ((i >> 2) & 1) * 2 - 1));
                var p = g.InverseTransformPoint(t.TransformPoint(c));
                if (!any) { b = new Bounds(p, Vector3.zero); any = true; } else b.Encapsulate(p);
            }
        }
        return b;
    }
    public static M Measure(IEnumerable<Transform> ts)
    {
        var hs = new List<float>(); var ws = new List<float>(); var ds = new List<float>();
        foreach (var t in ts)
        {
            var b = LocalB(t, out bool any); if (!any) continue; float s = t.lossyScale.y;
            // height = top of geometry above the floor it stands on
            float top = t.TransformPoint(new Vector3(0, b.max.y, 0)).y; float floor = t.position.y;
            hs.Add(top - floor); ws.Add(b.size.x * t.lossyScale.x); ds.Add(b.size.z * t.lossyScale.z);
        }
        var m = new M { n = hs.Count }; if (m.n == 0) return m;
        float Med(List<float> l) { l.Sort(); return l[l.Count / 2]; }
        m.hMin = hs.Min(); m.hMax = hs.Max(); m.h = Med(hs); m.w = Med(ws); m.d = Med(ds); return m;
    }
    public static int Run(Transform root, Player p)
    {
        Fails = 0; Lines.Clear();
        var all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Where(t => t.gameObject.activeInHierarchy).ToList();
        float eye = p.camRig.localPosition.y, h = p.cc.height;
        Log($"player kid capsule {h:0.00} m, eye {eye:0.00} m ({eye / Kid:0.00} K), fov {p.cam.fieldOfView:0}, walk {Player.Walk:0.0} m/s ({Player.Walk / Kid:0.0} K/s), run {Player.Run:0.0} m/s");
        Chk(Mathf.Abs(h - Kid) < 0.08f, $"kid capsule {h:0.00} ~ {Kid}");
        Chk(eye > 1.05f && eye < 1.22f, $"eye height {eye:0.00} in 1.05-1.22");
        Chk(Player.Walk / Kid > 2.0f && Player.Walk / Kid < 3.0f, $"walk {Player.Walk / Kid:0.0} K/s in 2-3");
        Chk(p.cam.fieldOfView >= 64 && p.cam.fieldOfView <= 74, $"fov {p.cam.fieldOfView:0} in 64-74");
        Chk(LevelGen.AWH >= 2.7f && LevelGen.AWH <= 3.1f, $"interior ceiling {LevelGen.AWH:0.00} m ({LevelGen.AWH / Kid:0.00} K) in 2.7-3.1");
        foreach (var nm in new[] { "road", "sidewalk" }) { var t = all.FirstOrDefault(x => x.name == nm); var r0 = t ? t.GetComponent<Renderer>() : null; if (r0) Log($"{nm,-16} width {r0.bounds.size.z:0.00} m = {r0.bounds.size.z / Kid:0.0} K (info)"); }
        foreach (var r in Rules)
        {
            var set = all.Where(t => t.name == r.key || (r.key.StartsWith("Cont_") && t.name == r.key)).ToList();
            var m = Measure(set); if (m.n == 0) { Log($"{r.label,-16} n=0"); continue; }
            bool ok = m.hMin >= r.hMin - 0.001f && m.hMax <= r.hMax + 0.001f;
            Log($"{r.label,-16} n={m.n,3} h={m.h:0.00} ({m.hMin:0.00}-{m.hMax:0.00}) w={m.w:0.00} d={m.d:0.00} = {m.h / Kid:0.00} K  rule {r.hMin:0.00}-{r.hMax:0.00} {(ok ? "ok" : "OUT")}");
            if (!ok) Fails++;
        }
        // all container kinds, informational
        foreach (var gk in all.Where(t => t.name.StartsWith("Cont_")).GroupBy(t => t.name)) { var m = Measure(gk); Log($"  {gk.Key,-18} n={m.n,3} h={m.h:0.00} w={m.w:0.00} d={m.d:0.00}"); }
        Log($"scale fails {Fails}");
        return Fails;
    }
    static void Chk(bool ok, string s) { Log((ok ? "ok  " : "OUT ") + s); if (!ok) Fails++; }
    static void Log(string s) { Lines.Add(s); Debug.Log("[scale] " + s); }
}
