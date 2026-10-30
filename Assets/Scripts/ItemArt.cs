using System.Collections.Generic;
using UnityEngine;

/// CASUAL: flat sticker icons (Resources/Icons) for UI, and billboard sticker "toys" for ground loot.
public static class ItemArt
{
    public static Sprite Pic(Item it) => it == null ? null : Art.Icon(it.ic);
    public static Sprite Pic(string name, string ic) => Art.Icon(ic);
    static readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();
    static Material blobM;
    public static Transform Build(Transform parent, Item it, out bool fallback)
    {
        fallback = false; var root = new GameObject("model").transform; root.SetParent(parent, false);
        var sp = Art.Icon(it.ic);
        if (!sp) { fallback = true; Art.Prim(PrimitiveType.Cube, root, new Vector3(0, 0.15f, 0), Vector3.one * 0.3f, Art.Lit(Data.RarCol[it.r])); return root; }
        if (!mats.TryGetValue(it.ic, out var m) || !m) { m = Art.Cutout(sp.texture); mats[it.ic] = m; }
        var q = GameObject.CreatePrimitive(PrimitiveType.Quad); Object.Destroy(q.GetComponent<Collider>()); q.name = "sticker";
        q.transform.SetParent(root, false); q.transform.localPosition = new Vector3(0, 0.28f, 0); q.transform.localScale = Vector3.one * 0.48f;
        var r = q.GetComponent<Renderer>(); r.sharedMaterial = m; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        {   // v0.9.12 a chunky enamel token (rarity rim, cream face, icon both sides) that spins, instead of a flat sticker
            var tk = new GameObject("token").transform; tk.SetParent(root, false); tk.localPosition = new Vector3(0, 0.28f, 0); tk.gameObject.AddComponent<TokenSpin>();
            var rc = Data.RarCol[Mathf.Clamp(it.r, 0, Data.RarCol.Length - 1)];
            SoftKit.Cyl(tk, Vector3.zero, 0.25f, 0.05f, new Color(0.99f, 0.95f, 0.86f), 0.02f, Quaternion.Euler(90, 0, 0), 40);
            SoftKit.Obj(tk, "sk_tokenrim", SoftKit.Torus(0.25f, 0.026f, Color.Lerp(rc, Color.white, 0.15f), 48, 12, 1), Vector3.zero, Quaternion.identity, 0.006f);
            q.transform.SetParent(tk, false); q.transform.localPosition = new Vector3(0, 0, -0.027f); q.transform.localScale = Vector3.one * 0.4f;
            var q2 = Object.Instantiate(q, tk); q2.transform.localPosition = new Vector3(0, 0, 0.027f); q2.transform.localRotation = Quaternion.Euler(0, 180, 0);
        }
        if (!blobM) { var bs = Resources.Load<Shader>("Shaders/Blob") ?? Shader.Find("Soft/Blob"); if (bs) { blobM = new Material(bs) { name = "ItemBlob" }; blobM.SetColor("_BaseColor", new Color(0.28f, 0.2f, 0.14f, 0.3f)); } }
        if (blobM) { var bq = GameObject.CreatePrimitive(PrimitiveType.Quad); Object.Destroy(bq.GetComponent<Collider>()); bq.name = "item_blob"; bq.transform.SetParent(root, false); bq.transform.localPosition = new Vector3(0, 0.012f, 0); bq.transform.localRotation = Quaternion.Euler(90, 0, 0); bq.transform.localScale = Vector3.one * 0.36f; var br = bq.GetComponent<Renderer>(); br.sharedMaterial = blobM; br.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; br.receiveShadows = false; }   // v0.9.9 soft blob (opaque black cylinder read as a hole in the rug)
        if (!glowM) { var gs = Resources.Load<Shader>("Shaders/Blob") ?? Shader.Find("Soft/Blob"); if (gs) { glowM = new Material(gs) { name = "ItemGlow" }; glowM.SetColor("_BaseColor", new Color(1f, 0.86f, 0.42f, 0.55f)); glowM.renderQueue = 3001; } }
        if (glowM) { var gq = GameObject.CreatePrimitive(PrimitiveType.Quad); Object.Destroy(gq.GetComponent<Collider>()); gq.name = "item_glow"; gq.transform.SetParent(root, false); gq.transform.localPosition = new Vector3(0, 0.008f, 0); gq.transform.localRotation = Quaternion.Euler(90, 0, 0); gq.transform.localScale = Vector3.one * 0.7f; var gr = gq.GetComponent<Renderer>(); gr.sharedMaterial = glowM; gr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; gq.AddComponent<GlowPulse>(); }   // v0.9.11 soft gold loot ring so floating pickups read as markers
        return root;
    }
    static Material glowM;
}

/// v0.9.11 slow breathing scale for the loot glow
public class GlowPulse : MonoBehaviour { void Update() { transform.localScale = Vector3.one * (0.7f + 0.06f * Mathf.Sin(Time.time * 2f + transform.parent.GetInstanceID())); } }

/// v0.8.1: steady, gentle item feedback (off = v0.8 pulsing/blinking, kept for before/after tests)
public static class Calm { public static bool On = true; }

public class TokenSpin : MonoBehaviour { void Update() { float k = transform.parent ? transform.parent.GetInstanceID() : 0; transform.localRotation = Quaternion.Euler(0, Time.time * 55f + k, 0); transform.localPosition = new Vector3(0, 0.3f + 0.025f * Mathf.Sin(Time.time * 1.6f + k), 0); } }

public class Billboard : MonoBehaviour
{
    void LateUpdate() { var c = Camera.main; if (!c) return; var d = transform.position - c.transform.position; d.y = 0; if (d.sqrMagnitude > 1e-4f) transform.rotation = Quaternion.LookRotation(d) ; transform.localPosition = new Vector3(0, 0.28f + (Calm.On ? 0.025f * Mathf.Sin(Time.time * 1.6f + transform.parent.GetInstanceID()) : 0.05f * Mathf.Sin(Time.time * 3 + transform.parent.GetInstanceID())), 0); }
}

/// Glowing floor ring that tells the player "this can be searched". Fades with distance, hides when empty.
public class HintRing : MonoBehaviour
{
    public static Interactable focused;
    public Interactable owner; public Color col = new Color(1f, 0.85f, 0.45f); public float radius = 0.6f, aspect = 1f;
    public float Level; MeshRenderer mr, mk; Transform marker; float fi; MaterialPropertyBlock mpb; float t0; static Mesh ring;
    static Mesh gem;
    static Mesh GemMesh()
    {   // flat-shaded octagonal bipyramid: short crown, long pavilion pointing at the prop
        if (gem) return gem; int n = 8; var v = new List<Vector3>(); var tr = new List<int>();
        Vector3 top = new Vector3(0, 0.55f, 0), bot = new Vector3(0, -1.1f, 0); Vector3 R(int i) { float a = i * Mathf.PI * 2 / n; return new Vector3(Mathf.Cos(a) * 0.5f, 0.15f, Mathf.Sin(a) * 0.5f); }
        Vector3 T(int i) { float a = (i + 0.5f) * Mathf.PI * 2 / n; return new Vector3(Mathf.Cos(a) * 0.28f, 0.55f, Mathf.Sin(a) * 0.28f); }
        void Tri(Vector3 a, Vector3 b, Vector3 c) { int k = v.Count; v.Add(a); v.Add(b); v.Add(c); tr.Add(k); tr.Add(k + 1); tr.Add(k + 2); k = v.Count; v.Add(a); v.Add(c); v.Add(b); tr.Add(k); tr.Add(k + 1); tr.Add(k + 2); }
        for (int i = 0; i < n; i++) { int j = (i + 1) % n; Tri(R(i), bot, R(j)); Tri(R(j), T(i), R(i)); Tri(T(i), R(j), T(j)); Tri(T(i), T(j), top); }
        gem = new Mesh(); gem.SetVertices(v); gem.SetTriangles(tr, 0); gem.RecalculateNormals(); gem.RecalculateBounds(); return gem;
    }
    static Mesh RingMesh()
    {
        if (ring) return ring; int n = 48; var v = new Vector3[n * 2]; var tr = new int[n * 6];
        for (int i = 0; i < n; i++) { float a = i * Mathf.PI * 2 / n; var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)); v[i * 2] = d * 0.94f; v[i * 2 + 1] = d; int j = (i + 1) % n; tr[i * 6] = i * 2; tr[i * 6 + 1] = j * 2; tr[i * 6 + 2] = i * 2 + 1; tr[i * 6 + 3] = i * 2 + 1; tr[i * 6 + 4] = j * 2; tr[i * 6 + 5] = j * 2 + 1; }
        ring = new Mesh { vertices = v, triangles = tr }; ring.RecalculateNormals(); ring.RecalculateBounds(); return ring;
    }
    public static HintRing Add(Interactable owner, Vector3 worldFloor, float radius, Color c)
    {
        var g = new GameObject("hintRing"); g.transform.SetParent(owner.transform, true); g.transform.position = worldFloor + Vector3.up * 0.025f; g.transform.rotation = Quaternion.identity;
        var h = g.AddComponent<HintRing>(); h.owner = owner; h.radius = radius; h.col = c; return h;
    }
    void Start()
    {
        t0 = Random.value * 5; mpb = new MaterialPropertyBlock(); FitClear();
        var rg = new GameObject("ring"); rg.transform.SetParent(transform, false); rg.AddComponent<MeshFilter>().sharedMesh = RingMesh();
        mr = rg.AddComponent<MeshRenderer>(); mr.sharedMaterial = Art.Emissive(Color.white, 1f); mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; mr.receiveShadows = false;
        var lossy = transform.lossyScale; rg.transform.localScale = new Vector3(radius / Mathf.Max(lossy.x, 1e-3f), 1, radius * aspect / Mathf.Max(lossy.z, 1e-3f));
        var m = new GameObject("focusGem"); m.AddComponent<MeshFilter>().sharedMesh = GemMesh(); m.AddComponent<MeshRenderer>(); marker = m.transform;   // v0.9.8 faceted down-pointing gem (was a spinning cube)
        marker.localScale = Vector3.one * 0.11f;   // world-space, unparented: owner scale (flat rugs etc.) no longer stretches it
        mk = m.GetComponent<MeshRenderer>(); mk.sharedMaterial = Art.Emissive(Color.white, 1f); mk.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }
    // shrink the ring until its outline no longer passes through neighbouring props / walls
    void OnDestroy() { if (marker) Destroy(marker.gameObject); }
    void OnDisable() { if (mk) mk.enabled = false; }
    public int shrinks;
    void FitClear()
    {
        var c = transform.position; float ox = radius, oz = radius * aspect;
        for (int k = 0; k < 12; k++)
        {
            bool hit = false;
            for (int i = 0; i < 16 && !hit; i++)
            {
                float a = i * Mathf.PI / 8; var p = c + new Vector3(Mathf.Cos(a) * radius, 0.12f, Mathf.Sin(a) * radius * aspect);
                foreach (var col in Physics.OverlapSphere(p, 0.05f, ~0, QueryTriggerInteraction.Ignore))
                { if (owner && (col.transform.IsChildOf(owner.transform) || col.GetComponentInParent<Interactable>() == owner)) continue; var cb = col.bounds; if (c.x > cb.min.x - 0.05f && c.x < cb.max.x + 0.05f && c.z > cb.min.z - 0.05f && c.z < cb.max.z + 0.05f) continue; /* the body this ring surrounds */ if (col.GetComponentInParent<Player>()) continue; if (col.bounds.max.y < 0.1f + c.y) continue; hit = true; break; }
            }
            if (!hit) break; radius *= 0.9f; shrinks++; if (radius < 0.32f) { radius = 0.32f; break; }
        }
        if (shrinks > 0) Debug.Log("[ring] " + (owner ? owner.name : "?") + " shrunk " + ox.ToString("0.00") + "->" + radius.ToString("0.00"));
    }
    void LateUpdate()
    {
        if (!owner) { Destroy(gameObject); return; }
        var lc = owner as LootContainer; bool empty = lc && lc.Empty; bool searched = lc && lc.opened;
        var cam = Camera.main; float d = cam ? Vector3.Distance(cam.transform.position, transform.position) : 0;
        bool foc = focused == owner;
        float vis = empty ? 0 : Mathf.Clamp01((14f - d) / 5f); if (foc) vis = 1; if (owner is Station) vis = 0;   /* v0.9.8: hub stations (desk map, bed...) use their prompt only: no floor ring / floating gem */
        mr.enabled = vis > 0.01f; float t = Time.time + t0;
        if (mr.enabled)
        {
            float pulse = Calm.On ? 0.92f + 0.08f * Mathf.Sin(t * 1.4f) : 0.75f + 0.25f * Mathf.Sin(t * 3.2f);
            var c = searched ? Color.Lerp(col, Color.gray, 0.55f) : col; float tgt = (foc ? (Calm.On ? 0.75f : 1.6f) : (Calm.On ? 0.4f : 0.7f))   /* v0.9.5: subtler hint ring */ * pulse * vis * (searched ? 0.6f : 1f);
            float inten = Calm.On ? (fi = Mathf.MoveTowards(fi, tgt, Time.deltaTime * 3f)) : tgt;   // below bloom threshold, eased focus change
            Level = c.grayscale * inten; mpb.SetColor("_EmissionColor", c * inten); mpb.SetColor("_BaseColor", c * 0.35f * vis); mr.SetPropertyBlock(mpb);
            float s = foc ? (Calm.On ? 1.06f : 1.08f + 0.04f * Mathf.Sin(t * 6)) : 1f; mr.transform.localScale = new Vector3(radius * s / Mathf.Max(transform.lossyScale.x, 1e-3f), 1, radius * aspect * s / Mathf.Max(transform.lossyScale.z, 1e-3f));
        }
        mk.enabled = foc && !empty && !(owner is Station);
        if (mk.enabled)
        {
            var b = owner.TopPoint; marker.position = b + Vector3.up * (Calm.On ? 0.28f + 0.03f * Mathf.Sin(t * 1.6f) : 0.28f + 0.06f * Mathf.Sin(t * 4)); marker.rotation = Quaternion.Euler(0, t * (Calm.On ? 25 : 90), 0);
            mpb.SetColor("_EmissionColor", col * (Calm.On ? 1.2f : 5f)); mpb.SetColor("_BaseColor", col); mk.SetPropertyBlock(mpb);
        }
    }
}
