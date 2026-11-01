using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// v0.9.20: the aunt map had ~28k separate static MeshRenderers (every leaf, petal, stem and trim block its own
/// object, ~25k of them also drawn into 4 shadow cascades) -> CPU-bound frames + long hitches when the view turned.
/// Merge static pieces per (material, shadow mode, layer, vertex layout, 12 m cell) into one mesh each. Same meshes,
/// same materials, same transforms -> identical look. Originals keep their GameObjects/colliders/bounds and are only
/// switched off with forceRenderingOff, so gameplay code and audits that read renderer bounds still work.
public static class MeshMerge
{
    public static int Merged, Groups, Skipped, Freed;
    const float Cell = 12f;
    static bool Dynamic(Transform t, Transform root)
    {
        for (var q = t; q && q != root; q = q.parent)
            foreach (var c in q.GetComponents<Component>())
            {
                if (c is Rigidbody || c is Animator || c is Animation || c is TextMesh || c is ParticleSystem || c is Light || c is LODGroup) return true;
                if (c is MonoBehaviour && !(c is HideSpot)) return true;   // any gameplay script (doors, loot, interactables, pulses, spinners, pets)
            }
        return false;
    }
    public static void Run(Transform root)
    {
        Merged = Groups = Skipped = Freed = 0; if (!root) return;
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var groups = new Dictionary<string, List<(MeshRenderer r, Mesh m)>>();
        foreach (var r in root.GetComponentsInChildren<MeshRenderer>())
        {
            if (!r.enabled || !r.gameObject.activeInHierarchy || r.forceRenderingOff || r.sharedMaterials.Length != 1 || !r.sharedMaterial || r.HasPropertyBlock()) { Skipped++; continue; }
            var f = r.GetComponent<MeshFilter>(); var m = f ? f.sharedMesh : null;
            if (!m || !m.isReadable || m.subMeshCount != 1 || m.vertexCount > 60000 || Dynamic(r.transform, root)) { Skipped++; continue; }
            var c = r.bounds.center; string lay = (m.HasVertexAttribute(VertexAttribute.Color) ? "c" : "") + (m.HasVertexAttribute(VertexAttribute.TexCoord0) ? "u" : "") + (m.HasVertexAttribute(VertexAttribute.Normal) ? "n" : "") + (m.HasVertexAttribute(VertexAttribute.Tangent) ? "t" : "");
            string k = r.sharedMaterial.GetInstanceID() + "|" + (int)r.shadowCastingMode + "|" + r.receiveShadows + "|" + r.gameObject.layer + "|" + lay + "|" + Mathf.FloorToInt(c.x / Cell) + "," + Mathf.FloorToInt(c.y / Cell) + "," + Mathf.FloorToInt(c.z / Cell);
            if (!groups.TryGetValue(k, out var l)) groups[k] = l = new List<(MeshRenderer, Mesh)>(); l.Add((r, m));
        }
        var tn = new Dictionary<string, long>(); var cn = new Dictionary<string, int>();
        foreach (var r in root.GetComponentsInChildren<MeshRenderer>()) { var f = r.GetComponent<MeshFilter>(); if (!f || !f.sharedMesh || !r.enabled) continue; long t = 0; for (int i = 0; i < f.sharedMesh.subMeshCount; i++) t += (long)f.sharedMesh.GetIndexCount(i) / 3; if (r.shadowCastingMode != ShadowCastingMode.Off) t *= 1; tn[r.name] = (tn.TryGetValue(r.name, out var a) ? a : 0) + t; cn[r.name] = (cn.TryGetValue(r.name, out var b2) ? b2 : 0) + 1; }
        var sb = new System.Text.StringBuilder(); foreach (var kv in System.Linq.Enumerable.Take(System.Linq.Enumerable.OrderByDescending(tn, x => x.Value), 14)) sb.Append(" " + kv.Key + "=" + (kv.Value / 1000) + "k/" + cn[kv.Key]);
        Debug.Log("[merge] tris by name (k/count):" + sb);
        var uses = new Dictionary<Mesh, int>();
        foreach (var f in root.GetComponentsInChildren<MeshFilter>(true)) if (f.sharedMesh) uses[f.sharedMesh] = uses.TryGetValue(f.sharedMesh, out var u) ? u + 1 : 1;
        foreach (var mc in root.GetComponentsInChildren<MeshCollider>(true)) if (mc.sharedMesh) uses[mc.sharedMesh] = 99;
        var host = new GameObject("merged_static").transform; host.SetParent(root, false); var w2l = root.worldToLocalMatrix;
        foreach (var kv in groups)
        {
            var l = kv.Value; if (l.Count < 2) continue;
            var ci = new CombineInstance[l.Count]; for (int i = 0; i < l.Count; i++) ci[i] = new CombineInstance { mesh = l[i].m, transform = w2l * l[i].r.localToWorldMatrix };
            var mesh = new Mesh { name = "merged", indexFormat = IndexFormat.UInt32 }; mesh.CombineMeshes(ci, true, true); mesh.RecalculateBounds(); mesh.UploadMeshData(true);
            var g = new GameObject("merged"); g.transform.SetParent(host, false); g.layer = l[0].r.gameObject.layer;
            g.AddComponent<MeshFilter>().sharedMesh = mesh; var mr = g.AddComponent<MeshRenderer>(); var s = l[0].r;
            mr.sharedMaterial = s.sharedMaterial; mr.shadowCastingMode = s.shadowCastingMode; mr.receiveShadows = s.receiveShadows; mr.lightProbeUsage = s.lightProbeUsage; mr.reflectionProbeUsage = s.reflectionProbeUsage;
            foreach (var (r, m0) in l)
            {
                r.forceRenderingOff = true;
                // free the original geometry (otherwise every vertex lives on the GPU twice: ~100M tris on the aunt map);
                // pin the renderer's world bounds first so audits that read r.bounds still see the same box.
                if (uses.TryGetValue(m0, out var n) && n == 1) { var b = m0.bounds; r.GetComponent<MeshFilter>().sharedMesh = null; r.localBounds = b; Object.Destroy(m0); Freed++; }
            }
            Merged += l.Count; Groups++;
        }
        Debug.Log("[merge] " + root.name + " merged " + Merged + " renderers into " + Groups + " meshes, freed " + Freed + ", skipped " + Skipped + " in " + sw.ElapsedMilliseconds + " ms, gfxMB " + (UnityEngine.Profiling.Profiler.GetAllocatedMemoryForGraphicsDriver() >> 20));
    }
}
