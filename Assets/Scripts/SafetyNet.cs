using UnityEngine;
using System.Collections.Generic;

/// v0.9.24: global "never fall through the world" net for the player and pets, on every map.
/// Tracks the last grounded spot; if a body drops below the kill plane or ends up under a floor
/// (surface above the feet, nothing below), it is put back on that spot.
public static class SafetyNet
{
    public static string Why = ""; public static float KillY = -8f; public static int Restores; public static readonly List<Transform> Movers = new List<Transform>();   // moving bodies (bus) are never "safe ground"
    static readonly RaycastHit[] hb = new RaycastHit[12]; static readonly Collider[] ob = new Collider[12];
    static bool Skip(Collider c, Collider self)
    {
        if (!c || c.isTrigger || c == self || c is CharacterController) return true;
        foreach (var m in Movers) if (m && c.transform.IsChildOf(m)) return true;
        return false;
    }
    /// nearest valid hit along a ray (ignores triggers, character controllers, self, moving bodies)
    public static bool Ray(Vector3 o, Vector3 d, float len, Collider self, out RaycastHit hit)
    {
        int n = Physics.RaycastNonAlloc(o, d, hb, len, ~0, QueryTriggerInteraction.Ignore); hit = default; float best = 1e9f; bool ok = false;
        for (int i = 0; i < n; i++) { if (Skip(hb[i].collider, self) || hb[i].distance >= best) continue; best = hb[i].distance; hit = hb[i]; ok = true; }
        return ok;
    }
    /// standing on static ground right now
    public static bool OnGround(Vector3 feet, Collider self) => Ray(feet + Vector3.up * 0.3f, Vector3.down, 0.75f, self, out _);
    /// below the kill plane, or under a floor: a surface above the feet and nothing below them
    public static bool Lost(Vector3 feet, Collider self)
    {
        if (feet.y < KillY) return true;
        if (Embedded(feet + Vector3.up * 0.18f, 0.1f, self) && Ray(feet + Vector3.up * 30f, Vector3.down, 29.6f, self, out var top) && top.point.y > feet.y + 0.3f && top.point.y < feet.y + 1.6f) return true;   // sunk into a ground/floor slab (a roof metres above = standing next to a house, not buried)
        if (Ray(feet + Vector3.up * 0.3f, Vector3.down, 3.3f, self, out _)) return false;
        bool l = Ray(feet + Vector3.up * 30f, Vector3.down, 29.7f, self, out var tp); if (l) Why = "no floor, roof " + tp.collider.name; return l;
    }
    public static bool Embedded(Vector3 c, float r, Collider self)
    {
        int n = Physics.OverlapSphereNonAlloc(c, r, ob, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < n; i++)
        {
            var o = ob[i]; if (Skip(o, self)) continue;
            if (o is MeshCollider mc && !mc.convex) continue;   // only solid volumes can be "inside"
            if ((o.ClosestPoint(c) - c).sqrMagnitude < 1e-6f) { Why = "inside " + o.name + "/" + (o.transform.parent ? o.transform.parent.name : ""); return true; }   // point is inside the volume
        }
        return false;
    }
    /// a free standing spot at/under `p` (ground-snapped, capsule clear)
    public static bool FreeSpot(Vector3 p, float radius, float height, Collider self, out Vector3 spot)
    {
        spot = p; if (!Ray(p + Vector3.up * 2f, Vector3.down, 4f, self, out var h)) return false;
        spot = h.point + Vector3.up * 0.03f;
        int n = Physics.OverlapCapsuleNonAlloc(spot + Vector3.up * (radius + 0.05f), spot + Vector3.up * Mathf.Max(radius + 0.06f, height - radius), radius, ob, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < n; i++) if (!Skip(ob[i], self) && !(self && ob[i].transform.IsChildOf(self.transform))) return false;
        return true;
    }
}
