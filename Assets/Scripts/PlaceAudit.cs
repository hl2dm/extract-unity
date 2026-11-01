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

    public static void Forget(Transform root) => Facade.RemoveAll(f => f.root == root);   // v0.9.20: throw-away LOD build
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
        DoorSunkBefore = DoorGround(root, false); DoorGround(root, true); DoorSunkAfter = DoorGround(root, false);
        Debug.Log("[dooraudit] doors sunk into step/plinth/ground before " + DoorSunkBefore + " after " + DoorSunkAfter + " (lifted " + DoorLifted + ", max " + DoorMaxLift.ToString("F2") + " m)");
        MailSpots.Clear(); SmallClipBefore = SmallClipAfter = SmallClip(root, true); Debug.Log("[smallclip] small-prop interpenetrations " + SmallClipAfter);
        CurtainOffBefore = CurtainAlign(root, false); CurtainAlign(root, true); CurtainOffAfter = CurtainAlign(root, false);
        Debug.Log("[curtainaudit] curtains off-centre before " + CurtainOffBefore + " after " + CurtainOffAfter + " (max shift " + CurtainMaxOff.ToString("F2") + " m)");
        SignMoves.Clear(); SignFaceMoved = 0; if (!System.Environment.GetCommandLineArgs().Contains("-nosignface")) { SignFaceBefore = SignFace(root, true); SignFaceAfter = SignFace(root, false); }
        Debug.Log("[signface] signs " + SignFaceTotal + " blocked/facing-wall before " + SignFaceBefore + " after " + SignFaceAfter + " (moved " + SignFaceMoved + ")");
        DressBefore = Dress(root, false); Dress(root, true); DressAfter = Dress(root, false); Debug.Log("[dressaudit] door-vs-dressing overlaps before " + DressBefore + " after " + DressAfter);
        Debug.Log("[placeaudit] before props " + pp0 + " wall " + pw0 + " door " + pd0 + " signs " + s0 + " | fixed " + Fixed + " hidden " + Hidden + " | after props " + PropHits + " wall " + WallHits + " door " + DoorHits + " signs " + SignHits);
    }


    /// v0.9.22 sign face audit: every painted sign/label/poster (sk_art quad) is sampled on a 5x3 grid; a ray from 0.8 m in front
    /// of the face back to it must not hit anything (awnings, roofs, windows, planters, lamps, trees, props, walls). Bad signs are
    /// nudged forward / up (or shrunk) until clean. Runs before MeshMerge so meshes are readable for temp colliders.
    public static int SignFaceBefore, SignFaceAfter, SignFaceMoved, SignFaceTotal;
    public static readonly List<(Transform t, Vector3 delta, Vector3 p, Vector3 n, string why)> SignMoves = new List<(Transform, Vector3, Vector3, Vector3, string)>();
    static List<Transform> Arts(Transform root) => root.GetComponentsInChildren<Transform>(true).Where(t => t.name == "sk_art" && t.gameObject.activeInHierarchy && t.GetComponent<MeshRenderer>() && t.GetComponent<MeshRenderer>().enabled && t.lossyScale.x > 0.25f && t.lossyScale.y > 0.2f).ToList();
    static string FaceHit(Transform a)
    {
        var n = -a.forward; int hits = 0; string who = null; var own = a.parent && a.parent.GetComponentsInChildren<Renderer>().Length < 40 ? a.parent : a;   // a clock's own bezel etc. is decoration, not an obstacle
        var ls = a.lossyScale; int nx = Mathf.Clamp(Mathf.CeilToInt(Mathf.Abs(ls.x) / 0.12f), 5, 40), ny = Mathf.Clamp(Mathf.CeilToInt(Mathf.Abs(ls.y) / 0.12f), 3, 12);   // v0.9.22: dense grid (<=12 cm) so a small facet poking through a wide board is caught
        for (int i = 0; i < nx; i++) for (int j = 0; j < ny; j++)
        {
            var p = a.TransformPoint(new Vector3(-0.42f + i * 0.84f / (nx - 1), -0.36f + j * 0.72f / (ny - 1), 0));
            if (Physics.Raycast(p + n * 0.8f, -n, out var h, 0.775f, ~0, QueryTriggerInteraction.Ignore) && !h.collider.transform.IsChildOf(own)) { hits++; who = who ?? h.collider.name + "@" + (h.collider.transform.parent ? h.collider.transform.parent.name : ""); }
        }
        if (hits > 0) return hits + "x " + who;
        // eye-height views (1.6 m) from the walkable side, 2.5-4.5 m out and off to the sides: nothing within 1.2 m of the face may cut across it
        var c = a.position; var rt = Vector3.Cross(Vector3.up, n).normalized; var nh = n; nh.y = 0; if (nh.sqrMagnitude < 0.1f) return null; nh.Normalize();
        foreach (float d in new[] { 2.5f, 4.5f }) foreach (float lat in new[] { -1.5f, 0f, 1.5f })
        {
            var e = c + nh * d + rt * lat; if (!Physics.Raycast(new Vector3(e.x, c.y + 0.5f, e.z), Vector3.down, out var g, 12f, ~0, QueryTriggerInteraction.Ignore)) continue;
            e.y = g.point.y + 1.6f; if (e.y > c.y + 0.3f || Physics.CheckSphere(e, 0.25f, ~0, QueryTriggerInteraction.Ignore)) continue;
            if (Physics.Linecast(e, c + n * 1.25f, ~0, QueryTriggerInteraction.Ignore)) continue;   // viewpoint not open to the sign
            int eh = 0; string ew = null;
            for (int i = 0; i < nx; i++) for (int j = 0; j < ny; j++)
            {
                var p = a.TransformPoint(new Vector3(-0.42f + i * 0.84f / (nx - 1), -0.36f + j * 0.72f / (ny - 1), 0)); var dv = p - e; float L = dv.magnitude;
                if (Physics.Raycast(e, dv / L, out var h, L - 0.025f, ~0, QueryTriggerInteraction.Ignore) && h.distance > L - 1.2f && !h.collider.transform.IsChildOf(own)) { eh++; ew = ew ?? h.collider.name + "@" + (h.collider.transform.parent ? h.collider.transform.parent.name : ""); }
            }
            if (eh >= 2) return "eye " + eh + "x " + ew;
        }
        return null;
    }
    public static int SignFace(Transform root, bool fix)
    {
        var arts = Arts(root); SignFaceTotal = arts.Count; if (arts.Count == 0) return 0;
        var sb = arts.Select(a => { var b = Art.RenderBounds(a.gameObject); b.Expand(0.9f); return b; }).ToList();
        var temp = new List<Collider>();
        foreach (var mr in root.GetComponentsInChildren<MeshRenderer>())
        {
            if (!mr.enabled || mr.name == "sk_art" || mr.GetComponent<Collider>()) continue; var mf = mr.GetComponent<MeshFilter>(); if (!mf || !mf.sharedMesh || !mf.sharedMesh.isReadable) continue;
            var rb = mr.bounds; if (!sb.Any(b => b.Intersects(rb))) continue; var mc = mr.gameObject.AddComponent<MeshCollider>(); mc.sharedMesh = mf.sharedMesh; temp.Add(mc);
        }
        Physics.SyncTransforms(); int bad = 0;
        foreach (var a in arts)
        {
            var why = FaceHit(a); if (why == null) continue; bad++;
            if (!fix) { Debug.Log("[signface] bad " + Path(a) + " " + why); continue; }
            var t = a.parent && a.parent.name.StartsWith("sk_") && a.parent.name != "shelter" ? a.parent : a; var n = -a.forward; var p0 = t.position; var ps = a.position; bool ok = false;
            foreach (float up in new[] { 0f, 0.12f, 0.25f, 0.4f }) { foreach (float fw in new[] { 0.05f, 0.1f, 0.18f, 0.26f, 0.35f }) { t.position = p0 + n * fw + Vector3.up * up; Physics.SyncTransforms(); if (FaceHit(a) == null) { ok = true; break; } } if (ok) break; }
            if (!ok) { var s0 = t.localScale; t.position = p0 + n * 0.12f; t.localScale = s0 * 0.8f; Physics.SyncTransforms(); ok = FaceHit(a) == null; if (!ok) { t.position = p0; t.localScale = s0; } }
            if (ok) { SignFaceMoved++; SignMoves.Add((t, t.position - p0, ps, n, why)); }
            Debug.Log("[signface] " + (ok ? "fixed " : "UNFIXED ") + Path(a) + " " + why + " moved " + (t.position - p0).ToString("F2"));
        }
        foreach (var c in temp) Object.DestroyImmediate(c); Physics.SyncTransforms(); return bad;
    }
    static string Path(Transform t) { var s = t.name; for (var p = t.parent; p && s.Length < 90; p = p.parent) s = p.name + "/" + s; return s; }

    /// v0.9.21 door grounding: a door whose bottom is buried in the step / plinth / sidewalk / terrain in front of it gets lifted
    /// so its sill sits on the top of that surface (every door on every map, before MeshMerge so bounds are exact).
    public static int DoorSunkBefore, DoorSunkAfter, DoorLifted; public static float DoorMaxLift;
    public static readonly List<Transform> LiftedDoors = new List<Transform>(); public static readonly List<float> LiftAmt = new List<float>();
    static float GroundUnder(Transform root, Transform dv, Bounds db, List<Renderer> rs)
    {
        var fwd = -dv.forward; fwd.y = 0; if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.back; fwd.Normalize();
        float baseY = db.min.y, top = baseY; var c = db.center;
        // probe points just in front of the door leaf, across its width
        var right = Vector3.Cross(Vector3.up, fwd).normalized; float hw = Mathf.Min(db.extents.x, db.extents.z) > 0.2f ? 0.3f : 0.3f;
        foreach (float sx in new[] { -hw, 0f, hw }) foreach (float dz in new[] { 0.12f, 0.3f })
        {
            var pt = new Vector3(c.x, baseY, c.z) + right * sx + fwd * (Mathf.Min(db.extents.x, db.extents.z) + dz);
            foreach (var r in rs)
            {
                if (!r || !r.enabled || r.transform.IsChildOf(dv)) continue;
                var b = r.bounds; if (b.size.x < 0.5f && b.size.z < 0.5f) continue;
                if (b.max.y <= baseY + 0.015f || b.max.y > baseY + 0.4f || b.min.y > baseY + 0.06f) continue;
                if (pt.x < b.min.x || pt.x > b.max.x || pt.z < b.min.z || pt.z > b.max.z) continue;
                var nm = r.name; if (nm.StartsWith("merged") || nm.Contains("glow") || nm.Contains("pool") || nm.Contains("shadow") || nm.Contains("blob") || nm.Contains("rug") || nm.Contains("mat")) continue;
                top = Mathf.Max(top, b.max.y);
            }
            if (Physics.Raycast(pt + Vector3.up * 0.6f, Vector3.down, out var hit, 0.75f, ~0, QueryTriggerInteraction.Ignore) && !hit.transform.IsChildOf(dv) && hit.point.y > baseY + 0.015f && hit.point.y < baseY + 0.4f) top = Mathf.Max(top, hit.point.y);
        }
        return top - baseY;
    }
    public static int DoorGround(Transform root, bool fix)
    {
        int n = 0; if (fix) { DoorLifted = 0; DoorMaxLift = 0; LiftedDoors.Clear(); LiftAmt.Clear(); }
        var doors = root.GetComponentsInChildren<Transform>().Where(t => (t.name == "sk_doorvis" || t.name == "Door" || t.name == "LockedDoor") && t.gameObject.activeInHierarchy).ToList();
        var all = root.GetComponentsInChildren<Renderer>().Select(r => (r, r.bounds)).ToList();
        foreach (var dv in doors)
        {
            var db = Art.RenderBounds(dv.gameObject); if (db.size.y < 1f) continue;
            var near = new Bounds(db.center, db.size + new Vector3(2.4f, 0, 2.4f));
            var rs = all.Where(x => x.Item2.Intersects(near)).Select(x => x.r).ToList();
            float lift = GroundUnder(root, dv, db, rs); if (lift <= 0.015f) continue;
            n++;
            if (!fix) Debug.Log("[dooraudit] " + dv.name + " @" + db.center.ToString("F1") + " sunk " + lift.ToString("F2") + " m");
            else { dv.position += Vector3.up * (lift + 0.005f); DoorLifted++; DoorMaxLift = Mathf.Max(DoorMaxLift, lift); LiftedDoors.Add(dv); LiftAmt.Add(lift + 0.005f); }
        }
        return n;
    }

    /// v0.9.21 small-prop interpenetration audit (mailboxes on posts and on walls): in the prop's own frame, a thin face part
    /// (flap, slot, plate, card, rim) must not be cut through / covered by a solid part of the same prop, and loose bits
    /// (letters, newspapers) must not sit inside a solid part. Backing plates against the wall are skipped.
    public static int SmallClipBefore, SmallClipAfter; public static readonly List<(Bounds b, Vector3 face)> MailSpots = new List<(Bounds, Vector3)>();
    static readonly HashSet<string> SmallN = new HashSet<string> { "sk_mailbox", "sk_wallmail" };
    public static int SmallClip(Transform root, bool log)
    {
        int n = 0;
        foreach (var g in root.GetComponentsInChildren<Transform>().Where(t => SmallN.Contains(t.name) && t.gameObject.activeInHierarchy))
        {
            var parts = new List<(string nm, Bounds b)>();
            if (log) { var wb = Art.RenderBounds(g.gameObject); if (wb.size.y > 0.2f) MailSpots.Add((wb, g.name == "sk_wallmail" ? -g.forward : g.forward)); }
            foreach (var mf in g.GetComponentsInChildren<MeshFilter>())
            {
                if (!mf.sharedMesh || mf.GetComponent<TextMesh>() || mf.name == "backing") continue;
                var mb = mf.sharedMesh.bounds; var lb = new Bounds(g.InverseTransformPoint(mf.transform.TransformPoint(mb.center)), Vector3.zero);
                for (int k = 0; k < 8; k++) lb.Encapsulate(g.InverseTransformPoint(mf.transform.TransformPoint(mb.center + Vector3.Scale(mb.extents, new Vector3((k & 1) * 2 - 1, (k & 2) - 1, (k & 4) / 2 - 1)))));
                parts.Add((mf.name, lb));
            }
            for (int i = 0; i < parts.Count; i++)
            {
                var a = parts[i].b; var e = a.size; int ax = e.x < e.y ? (e.x < e.z ? 0 : 2) : (e.y < e.z ? 1 : 2);
                bool thin = e[ax] <= 0.035f, loose = parts[i].nm == "sk_letter" || parts[i].nm == "sk_newspaper";
                if (!thin && !loose) continue;
                for (int j = 0; j < parts.Count; j++)
                {
                    if (i == j) continue; var o = parts[j].b; var oe = o.size; if (Mathf.Min(oe.x, Mathf.Min(oe.y, oe.z)) <= 0.035f) continue;   // decal-on-decal layering is fine
                    bool hit;
                    if (loose) { float ix = Mathf.Min(a.max.x, o.max.x) - Mathf.Max(a.min.x, o.min.x), iy = Mathf.Min(a.max.y, o.max.y) - Mathf.Max(a.min.y, o.min.y), iz = Mathf.Min(a.max.z, o.max.z) - Mathf.Max(a.min.z, o.min.z); hit = ix > 0.005f && iy > 0.005f && iz > 0.005f; }
                    else
                    {
                        float pc = a.center[ax]; if (pc <= o.min[ax] + 0.004f || pc >= o.max[ax] - 0.004f) continue;   // plate plane not inside the solid
                        int u = (ax + 1) % 3, v = (ax + 2) % 3;
                        float iu = Mathf.Min(a.max[u], o.max[u]) - Mathf.Max(a.min[u], o.min[u]), iv = Mathf.Min(a.max[v], o.max[v]) - Mathf.Max(a.min[v], o.min[v]);
                        hit = iu > 0 && iv > 0 && iu * iv > 0.04f * e[u] * e[v];
                    }
                    if (!hit) continue; n++; if (log) Debug.Log("[smallclip] " + g.name + " " + parts[i].nm + " cuts " + parts[j].nm + " @" + g.position.ToString("F1") + " plate " + a.center.ToString("F3") + "/" + a.size.ToString("F3") + " solid " + o.center.ToString("F3") + "/" + o.size.ToString("F3"));
                    break;
                }
            }
        }
        return n;
    }

    /// v0.9.21 curtain alignment: every curtain set is centred (sideways) on the window / round window / wall plate it dresses.
    public static int CurtainOffBefore, CurtainOffAfter; public static float CurtainMaxOff;
    public static int CurtainAlign(Transform root, bool fix)
    {
        int n = 0; var all = root.GetComponentsInChildren<Transform>();
        var wins = all.Where(t => (t.name == "sk_win" || t.name == "sk_roundwin" || t.name == "sk_plate") && t.gameObject.activeInHierarchy).Select(t => (t, Art.RenderBounds(t.gameObject))).ToList();
        foreach (var cu in all.Where(t => t.name == "sk_curtain" && t.gameObject.activeInHierarchy))
        {
            Vector3 r = cu.right, f = cu.forward; float best = 9; Vector3 off = Vector3.zero;
            foreach (var (w, b) in wins)
            {
                var d = b.center - cu.position; float depth = Mathf.Abs(Vector3.Dot(d, f)), lat = Vector3.Dot(d, r);
                if (depth > 0.6f || Mathf.Abs(lat) > 1.0f || Mathf.Abs(d.y) > 1.2f) continue;
                if (Mathf.Abs(lat) < Mathf.Abs(best)) { best = lat; off = r * lat; }
            }
            if (best > 8 || Mathf.Abs(best) < 0.04f) continue;
            n++; if (fix) { cu.position += off; CurtainMaxOff = Mathf.Max(CurtainMaxOff, Mathf.Abs(best)); } else Debug.Log("[curtainaudit] " + cu.parent?.name + " off " + best.ToString("F2") + " m");
        }
        return n;
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
