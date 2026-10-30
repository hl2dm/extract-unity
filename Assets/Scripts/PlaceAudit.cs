using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// v0.8.1: general placement-overlap audit + fixer.
/// props/vehicles/trees/lamps/benches/furniture vs each other (renderer-level, stacking allowed), vs walls, vs door clearance;
/// sign plaques vs windows/doors/shutters/awnings/AC units/other signs. Fix = re-place with clearance (move, then shrink), hide only as last resort.
public static class PlaceAudit
{
    public static readonly HashSet<string> PropN = new HashSet<string> { "sk_tree", "sk_lamp", "sk_scooter", "RideScooter", "sk_hydrant", "sk_bin", "sk_mailbox", "sk_pot", "sk_bench", "sk_cone", "slide",
        "sk_sofa", "sk_bed", "sk_rtable", "sk_tv", "sk_stool", "sk_petbed", "sk_toys", "sk_tank", "sk_tub", "sk_toilet", "sk_piggy", "sk_bear", "sk_plant", "sk_flamp", "sk_bush", "sk_planter" };
    static readonly HashSet<string> Heavy = new HashSet<string> { "sk_tree", "sk_lamp", "slide", "sk_tub", "sk_toilet", "sk_bed", "sk_sofa" };
    static readonly HashSet<string> FacN = new HashSet<string> { "sk_win", "sk_roundwin", "sk_awning", "sk_ac", "sk_shutter" };
    struct Fac { public Transform root; public string kind; public Bounds b; }
    static readonly List<Fac> Facade = new List<Fac>();
    public class Move { public Transform t; public Vector3 from, to; public Vector3 s0, s1; public string what; }
    public static readonly List<Move> Moves = new List<Move>();
    public static int BeforeProp, BeforeWall, BeforeDoor, BeforeSign, Hidden, Fixed;
    public static int PropHits, WallHits, DoorHits, SignHits;

    /// called by SoftProps.Merge before a facade piece is folded into a merged mesh
    public static void Record(Transform root, Transform c)
    {
        foreach (var t in c.GetComponentsInChildren<Transform>()) if (FacN.Contains(t.name)) Facade.Add(new Fac { root = root, kind = t.name, b = RB(t) });
        if (!FacN.Contains(c.name)) { var b = RB(c); if (b.size.sqrMagnitude > 1e-4f) Facade.Add(new Fac { root = root, kind = "piece", b = b }); }   // balconies, pent roofs, railings, brackets...
    }
    static Bounds RB(Transform t)
    {
        var b = new Bounds(t.position, Vector3.zero); bool any = false;
        foreach (var r in t.GetComponentsInChildren<Renderer>()) { if (!r.enabled || !r.gameObject.activeInHierarchy) continue; if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds); }
        return b;
    }
    static Bounds Sh(Bounds b, float m) { b.Expand(-2 * m); return b; }
    class It { public Transform t; public Bounds b; public List<Bounds> parts = new List<Bounds>(); public bool indoor; }
    static bool Indoor(Transform t) { for (var q = t.parent; q; q = q.parent) if (q.name == "House" || q.name == "room" || q.name.StartsWith("Room")) return true; return false; }
    static bool UnderProp(Transform t) { for (var q = t.parent; q; q = q.parent) if (PropN.Contains(q.name)) return true; return false; }
    static It Make(Transform t)
    {
        var it = new It { t = t, b = RB(t), indoor = Indoor(t) };
        foreach (var r in t.GetComponentsInChildren<Renderer>()) if (r.enabled && r.gameObject.activeInHierarchy && !(r is ParticleSystemRenderer)) it.parts.Add(Sh(r.bounds, it.indoor ? 0.04f : -0.07f));   // outdoors: ~14 cm clear gap
        return it;
    }
    static void Refresh(It it, Vector3 d) { it.b.center += d; for (int i = 0; i < it.parts.Count; i++) { var p = it.parts[i]; p.center += d; it.parts[i] = p; } }
    // stacking (lamp on table, plant on shelf) and parked-under-crown are intended
    static bool Clash(It a, It b)
    {
        if (!a.b.Intersects(b.b)) return false;
        if (a.b.min.y > b.b.max.y - 0.12f || b.b.min.y > a.b.max.y - 0.12f) return false;
        foreach (var p in a.parts) foreach (var q in b.parts) if (p.Intersects(q)) return true;
        return false;
    }
    static List<Bounds> DoorZones(Transform root)
    {
        var l = new List<Bounds>();
        foreach (var d in root.GetComponentsInChildren<Transform>()) if (d.name == "sk_doorvis" && d.gameObject.activeInHierarchy)
        {
            var b = RB(d); var f = d.forward; b.Expand(new Vector3(Mathf.Abs(f.x) * 1.6f + 0.1f, -0.1f, Mathf.Abs(f.z) * 1.6f + 0.1f));
            b.min = new Vector3(b.min.x, b.min.y + 0.1f, b.min.z); l.Add(b);
        }
        return l;
    }
    static Transform Owner(Collider c) { for (var q = c.transform; q; q = q.parent) if (PropN.Contains(q.name)) return q; return null; }
    static bool Wall(It it)
    {
        foreach (var p in it.parts)
        {
            var c = p.center; var e = p.extents; e.y = Mathf.Max(0.01f, e.y - 0.08f); c.y += 0.08f;
            foreach (var col in Physics.OverlapBox(c, e, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
            { if (col.transform.IsChildOf(it.t) || Owner(col) != null) continue; var cb = col.bounds; if (cb.size.y < 1.4f || Mathf.Min(cb.size.x, cb.size.z) > 0.8f) continue; return true; }
        }
        return false;
    }
    static bool InDoor(It it, List<Bounds> dz) { if (it.t.name == "sk_tree" || it.t.name == "sk_lamp") return false; foreach (var z in dz) if (z.Intersects(Sh(it.b, 0.05f))) return true; return false; }

    // ---- signs
    class Sg { public Transform t; public Bounds b; }
    static List<Bounds> SignObstacles(Transform root, Sg s, List<Sg> all)
    {
        var l = new List<Bounds>();
        foreach (var f in Facade) if (f.root && f.root.IsChildOf(root)) l.Add(f.b);
        foreach (var t in root.GetComponentsInChildren<Transform>()) if ((FacN.Contains(t.name) || t.name == "sk_doorvis") && t.gameObject.activeInHierarchy && !s.t.IsChildOf(t) && !t.IsChildOf(s.t)) l.Add(RB(t));
        foreach (var o in all) if (o != s) l.Add(o.b);
        foreach (var it in root.GetComponentsInChildren<Transform>()) if (PropN.Contains(it.name) && it.gameObject.activeInHierarchy && !UnderProp(it) && it.name != "sk_tree") l.Add(RB(it));
        return l;
    }
    static Bounds View(Transform t, Bounds b) { var v = Sh(b, 0.03f); var f = b; f.center -= t.forward * 1.5f; f = Sh(f, 0.03f); v.Encapsulate(f); return v; }
    static bool SignHit(Bounds b, List<Bounds> obs) { var s = b; foreach (var o in obs) if (o.size.x > 0.1f || o.size.z > 0.1f) if (o.Intersects(s)) return true; return false; }   // thin brackets = intended attachment

    public static int Run(Transform root, bool fix)
    {
        Physics.SyncTransforms(); if (fix) Moves.Clear();
        Facade.RemoveAll(f => !f.root);
        var items = root.GetComponentsInChildren<Transform>().Where(t => PropN.Contains(t.name) && t.gameObject.activeInHierarchy && !UnderProp(t)).Select(Make).Where(i => i.parts.Count > 0).ToList();
        var dz = DoorZones(root);
        int pp = 0, pw = 0, pd = 0;
        for (int pass = 0; pass < 2; pass++)
        {
            pp = pw = pd = 0; var seen = new HashSet<(int, int)>();
            for (int i = 0; i < items.Count; i++)
            {
                var a = items[i]; It other = null;
                for (int j = 0; j < items.Count && other == null; j++) if (j != i && Clash(a, items[j])) other = items[j];
                bool w = other == null && Wall(a), d = other == null && !w && InDoor(a, dz);
                if (other == null && !w && !d) continue;
                if (other != null) { var key = a.t.GetInstanceID() < other.t.GetInstanceID() ? (a.t.GetInstanceID(), other.t.GetInstanceID()) : (other.t.GetInstanceID(), a.t.GetInstanceID()); if (!seen.Add(key)) continue; }
                if (other != null) pp++; else if (w) pw++; else pd++;
                if (!fix || pass == 1) { Debug.Log("[placeaudit] " + a.t.name + " @" + a.t.position.ToString("F1") + (other != null ? " overlaps " + other.t.name + " @" + other.t.position.ToString("F1") : w ? " in wall" : " blocks door")); continue; }
                var mv = a; if (other != null && Heavy.Contains(a.t.name) && !Heavy.Contains(other.t.name)) mv = other;
                if (!Relocate(mv, items, dz)) { if (!Heavy.Contains(mv.t.name) || mv.t.name == "sk_tree") { Moves.Add(new Move { t = mv.t, from = mv.t.position, to = mv.t.position, s0 = mv.t.localScale, s1 = mv.t.localScale, what = "hid" }); mv.t.gameObject.SetActive(false); items.Remove(mv); Hidden++; i--; Debug.Log("[placeaudit] hid " + mv.t.name); } }
            }
            if (!fix) break;
        }
        // signs
        var signs = root.GetComponentsInChildren<Transform>().Where(t => t.name == "sk_text3d" && t.gameObject.activeInHierarchy).Select(t => new Sg { t = t, b = RB(t) }).ToList();
        int sh = 0;
        foreach (var s in signs)
        {
            var obs = SignObstacles(root, s, signs);
            if (!SignHit(View(s.t, s.b), obs)) continue; sh++;
            if (!fix) { var vv = View(s.t, s.b); string who = ""; foreach (var o in obs) if ((o.size.x > 0.1f || o.size.z > 0.1f) && o.Intersects(vv)) who += " [" + o.center.ToString("F2") + " sz" + o.size.ToString("F2") + "]"; Debug.Log("[placeaudit] sign @" + s.t.position.ToString("F1") + " fwd" + s.t.forward.ToString("F1") + " b" + s.b.center.ToString("F2") + " sz" + s.b.size.ToString("F2") + " overlaps" + who); continue; }
            var p0 = s.t.position; var s0 = s.t.localScale; bool ok = false; var right = s.t.right; right.y = 0; right.Normalize();
            foreach (float sc in new[] { 1f, 0.85f, 0.72f, 0.6f })
            {
                for (int r = 0; r <= 14 && !ok; r++)
                    foreach (var dir in new[] { Vector3.up, right, -right, Vector3.up + right, Vector3.up - right, -Vector3.up * 0.5f })
                    {
                        var off = dir * (0.12f * r); s.t.position = p0 + off; s.t.localScale = s0 * sc; var nb = RB(s.t);
                        if (!SignHit(View(s.t, nb), obs)) { s.b = nb; ok = true; break; }
                        if (r == 0) break;
                    }
                if (ok) break;
            }
            if (ok) { Fixed++; Moves.Add(new Move { t = s.t, from = p0, to = s.t.position, s0 = s0, s1 = s.t.localScale, what = "sign" }); Debug.Log("[placeaudit] sign moved " + (s.t.position - p0).ToString("F2") + " scale " + (s.t.localScale.x / s0.x).ToString("0.00")); }
            else { s.t.position = p0; s.t.localScale = s0; s.b = RB(s.t); }
        }
        if (fix) { BeforeProp = pp0; BeforeWall = pw0; BeforeDoor = pd0; BeforeSign = sh; }
        PropHits = pp; WallHits = pw; DoorHits = pd; SignHits = sh;
        Physics.SyncTransforms();
        return pp + pw + pd + sh;
    }
    static int pp0, pw0, pd0;
    static bool Relocate(It a, List<It> items, List<Bounds> dz)
    {
        var p0 = a.t.position; float maxR = a.indoor ? 1.2f : 3.5f; bool road = !a.indoor && Mathf.Abs(p0.z) >= 4.5f && Mathf.Abs(p0.z) < 14f;
        for (float r = 0.2f; r <= maxR + 1e-3f; r += 0.2f)
            for (int k = 0; k < 12; k++)
            {
                float an = k * 30f * Mathf.Deg2Rad; var off = new Vector3(Mathf.Cos(an), 0, Mathf.Sin(an)) * r;
                if (road && Mathf.Abs(p0.z + off.z) < 4.5f) continue;
                Refresh(a, off); a.t.position = p0 + off; Physics.SyncTransforms();
                bool bad = Wall(a) || InDoor(a, dz); if (!bad) foreach (var o in items) if (o != a && o.t.gameObject.activeInHierarchy && Clash(a, o)) { bad = true; break; }
                if (!bad) { Fixed++; Moves.Add(new Move { t = a.t, from = p0, to = a.t.position, s0 = a.t.localScale, s1 = a.t.localScale, what = a.t.name }); Debug.Log("[placeaudit] moved " + a.t.name + " by " + off.ToString("F2")); return true; }
                Refresh(a, -off); a.t.position = p0;
            }
        Physics.SyncTransforms(); return false;
    }
    /// count first (before numbers), then fix
    public static void Fix(Transform root)
    {
        Hidden = Fixed = 0; Run(root, false); pp0 = PropHits; pw0 = WallHits; pd0 = DoorHits; int s0 = SignHits;
        Run(root, true); BeforeSign = s0;
        int left = Run(root, false);
        DressBefore = Dress(root, false); Dress(root, true); DressAfter = Dress(root, false); Debug.Log("[dressaudit] door-vs-dressing overlaps before " + DressBefore + " after " + DressAfter);
        Debug.Log("[placeaudit] before props " + pp0 + " wall " + pw0 + " door " + pd0 + " signs " + s0 + " | fixed " + Fixed + " hidden " + Hidden + " | after props " + PropHits + " wall " + WallHits + " door " + DoorHits + " signs " + SignHits);
    }

    /// v0.9.5 dressing audit: facade/interior dressing pieces (shutters, pipes, lamps, signs, planters) must not cut through doors.
    public static int DressBefore, DressAfter;
    public static int Dress(Transform root, bool fix)
    {
        int n = 0;
        foreach (var dv in root.GetComponentsInChildren<Transform>()) if (dv.name == "sk_doorvis" && dv.parent)
        {
            var db = Art.RenderBounds(dv.gameObject); db.Expand(new Vector3(-0.06f, -0.06f, 0.1f));
            foreach (var r in dv.parent.GetComponentsInChildren<Renderer>())
            {
                if (!r.enabled || r.transform.IsChildOf(dv) || r.name.StartsWith("wall") || r.name == "front" || r.name.Contains("glow") || r.name.Contains("pool")) continue;
                var b = r.bounds; if (b.max.y < db.min.y + 0.25f) continue; /* steps / sills */ if (b.size.x > 3f || b.size.y > 3.5f || b.size.x * b.size.y * b.size.z < 1e-5f) continue;
                if (b.Intersects(db)) { n++; if (!fix) Debug.Log("[dressaudit] " + r.name + " @" + b.center.ToString("F1") + " cuts door @" + db.center.ToString("F1")); else r.enabled = false; }
            }
        }
        return n;
    }
    // v0.8.1 contact shadows: one soft radial blob under every street prop / furniture piece (grounds them, no hard shadow acne)
    static Material blobM; public static int Blobs;
    public static void ContactShadows(Transform root)
    {
        if (!blobM)
        {
            var sh = Resources.Load<Shader>("Shaders/Blob") ?? Shader.Find("Soft/Blob");
            if (!sh) { Debug.LogError("[contact] Blob shader missing"); return; }
            blobM = new Material(sh) { name = "ContactBlob" }; blobM.SetColor("_BaseColor", new Color(0.28f, 0.2f, 0.14f, 0.36f));
        }
        Blobs = 0;
        foreach (var t in root.GetComponentsInChildren<Transform>())
        {
            if (!PropN.Contains(t.name) || !t.gameObject.activeInHierarchy || UnderProp(t)) continue;
            var b = RB(t); if (b.size.y < 0.15f) continue;
            float sx = Mathf.Clamp(b.size.x * 1.25f, 0.4f, 4f), sz = Mathf.Clamp(b.size.z * 1.25f, 0.4f, 4f);
            if (t.name == "sk_tree" || t.name == "sk_lamp") { sx = sz = t.name == "sk_tree" ? 1.6f : 0.7f; }
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad); Object.Destroy(q.GetComponent<Collider>()); q.name = "contact_shadow";
            q.transform.SetParent(root, true); q.transform.position = new Vector3(b.center.x, b.min.y + 0.014f, b.center.z); q.transform.rotation = Quaternion.Euler(90, 0, 0);
            var ls = root.lossyScale; q.transform.localScale = new Vector3(sx / Mathf.Max(0.01f, ls.x), sz / Mathf.Max(0.01f, ls.z), 1);
            var r = q.GetComponent<Renderer>(); r.sharedMaterial = blobM; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false; Blobs++;
        }
        Debug.Log("[contact] blobs " + Blobs);
    }
}
