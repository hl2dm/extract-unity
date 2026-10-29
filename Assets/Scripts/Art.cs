using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class Art
{
    static Material pbrT, litT, emT, unlitT, cutT;
    static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();
    static readonly Dictionary<string, GameObject> models = new Dictionary<string, GameObject>();
    static readonly Dictionary<string, Texture2D> texs = new Dictionary<string, Texture2D>();
    static readonly Dictionary<string, Sprite> icons = new Dictionary<string, Sprite>();
    public static Font Font, FontBold;
    static Material wfm;
    /// depth-tested world text (signs, posters): walls occlude it. Font default GUI/Text shader ignores depth.
    // v0.8.1: dynamic font atlases rebuild (and briefly flash every glyph) when a new char/size is first requested mid-play; request everything up front
    public static int AtlasRebuilds;
    static bool rbHook;
    public static void PrewarmText()
    {
        if (!rbHook) { rbHook = true; Font.textureRebuilt += f => AtlasRebuilds++; }
        const string common = "0123456789:：/%×x+-.,!！?？()（）「」、。 ★♥▶◀▲▼←→ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
        var bySize = new System.Collections.Generic.Dictionary<(Font, int, FontStyle), System.Text.StringBuilder>();
        void Add(Font f, int sz, FontStyle st, string s) { if (!f || f.dynamic == false || string.IsNullOrEmpty(s)) return; var k = (f, sz, st); if (!bySize.TryGetValue(k, out var sb)) bySize[k] = sb = new System.Text.StringBuilder(common); sb.Append(s); }
        foreach (var t in Object.FindObjectsByType<UnityEngine.UI.Text>(FindObjectsInactive.Include)) { Add(t.font, t.fontSize, t.fontStyle, t.text); if (t.resizeTextForBestFit) for (int z = t.resizeTextMinSize; z <= t.resizeTextMaxSize; z += 2) Add(t.font, z, t.fontStyle, t.text); }
        foreach (var t in Object.FindObjectsByType<TextMesh>(FindObjectsInactive.Include)) Add(t.font, t.fontSize, t.fontStyle, t.text);
        foreach (var kv in bySize) kv.Key.Item1.RequestCharactersInTexture(kv.Value.ToString(), kv.Key.Item2, kv.Key.Item3);
    }
    public static Material WorldFontMat { get { if (!wfm && FontBold) { var sh = Resources.Load<Shader>("Shaders/WorldText") ?? Shader.Find("Soft/WorldText"); wfm = new Material(FontBold.material); if (sh) wfm.shader = sh; else Debug.LogError("[text] WorldText shader missing"); wfm.mainTexture = FontBold.material.mainTexture; wfm.name = "FontWorld"; Font.textureRebuilt += f => { if (wfm && f == FontBold) wfm.mainTexture = f.material.mainTexture; }; wfm.renderQueue = 3000; } return wfm; } }
    static Material ovl;
    /// world-space label material that draws on top of geometry (no clipping into walls)
    public static Material OverlayFontMat { get { if (!ovl && FontBold) { ovl = new Material(FontBold.material); ovl.name = "FontOverlay"; ovl.SetInt("unity_GUIZTestMode", (int)UnityEngine.Rendering.CompareFunction.Always); ovl.renderQueue = 4000; } return ovl; } }
    public static bool Casual = true; public static int VMLayer = 8; public static int ShotMask => ~((1 << 8) | (1 << 9));

    public static void Init()
    {
        litT = Resources.Load<Material>("Mat/Lit"); pbrT = Resources.Load<Material>("Mat/LitPBR") ?? Resources.Load<Material>("Mat/Lit"); emT = Resources.Load<Material>("Mat/LitEmissive");
        unlitT = Resources.Load<Material>("Mat/Unlit"); cutT = Resources.Load<Material>("Mat/UnlitCutout");
        Font = Resources.Load<Font>("Fonts/NotoTC-Medium"); FontBold = Resources.Load<Font>("Fonts/NotoTC-Black") ?? Font;
        if (Casual) { var rb = Resources.Load<Font>("Fonts/GenSenRounded-B"); var rh = Resources.Load<Font>("Fonts/GenSenRounded-H"); if (rb) Font = rb; if (rh) FontBold = rh; else if (rb) FontBold = rb; }
    }
    public static Texture2D Tex(string n) { if (!texs.TryGetValue(n, out var t)) { t = Resources.Load<Texture2D>("Tex/" + n); texs[n] = t; } return t; }

    public static Material Lit(Color c, string tex = null, float smooth = 0.15f, float metal = 0f)
    {
        string k = "L" + c + tex + smooth + metal;
        if (cache.TryGetValue(k, out var m)) return m;
        var nrm = tex != null ? Tex(tex + "_n") : null;
        if (nrm) { m = PBR(c, Tex(tex), nrm, Tex(tex + "_m"), smooth, metal); cache[k] = m; return m; }
        m = new Material(litT); m.SetColor("_BaseColor", c); if (tex != null) m.SetTexture("_BaseMap", Tex(tex));
        m.SetFloat("_Smoothness", smooth); m.SetFloat("_Metallic", metal); cache[k] = m; return m;
    }
    public static Material PBR(Color c, Texture d, Texture n, Texture ms, float smooth = 1f, float metal = 0f)
    {
        var m = new Material(pbrT); m.SetColor("_BaseColor", c); m.SetTexture("_BaseMap", d);
        m.SetTexture("_BumpMap", n); m.SetFloat("_BumpScale", 1f); m.EnableKeyword("_NORMALMAP");
        if (ms) { m.SetTexture("_MetallicGlossMap", ms); m.EnableKeyword("_METALLICSPECGLOSSMAP"); m.SetTexture("_OcclusionMap", ms); m.EnableKeyword("_OCCLUSIONMAP"); m.SetFloat("_OcclusionStrength", 1f);
            m.SetFloat("_Smoothness", Mathf.Clamp(0.55f + smooth, 0.6f, 1f)); m.SetFloat("_Metallic", 1f); }
        else { m.SetFloat("_Smoothness", smooth); m.SetFloat("_Metallic", metal); }
        return m;
    }
    static readonly Dictionary<string, string> phMap = new Dictionary<string, string> { { "Env/Crate", "PH/wooden_crate_02" }, { "Env/ExplodingBarrel", "PH/Barrel_01" }, { "Furn/table", "PH/wooden_table_02" }, { "Furn/cabinetTelevision", "PH/painted_wooden_cabinet" }, { "Env/StreetLight", "PH/street_lamp_01" }, { "Env/Tree_1", "PH/tree_small_02" }, { "Env/Tree_2", "PH/island_tree_01" }, { "Env/Tree_3", "PH/fir_tree_01" }, { "Env/Tree_4", "PH/tree_small_02" }, { "Env/Fence", "PH/chainlink_fence" }, { "Env/Sofa", "PH/sofa_02" }, { "Furn/loungeSofa", "PH/sofa_02" }, { "Env/Debris_BrokenCar", "PH/covered_car" }, { "Furn/bookcaseClosedDoors", "PH/vintage_cabinet_01" }, { "Furn/sideTableDrawers", "PH/drawer_cabinet" } };
    // multi-material Poly Haven models: texture set per material-name key
    static readonly Dictionary<string, string[]> phSets = new Dictionary<string, string[]> { { "PH/bolt_action_rifle_7_62", new[] { "accesories", "" } }, { "PH/fir_tree_01", new[] { "trunk_a", "trunk_b", "trunk_c", "twig", "bark" } }, { "PH/tree_small_02", new[] { "branch", "leaves", "" } }, { "PH/island_tree_01", new[] { "branches", "leaves", "" } }, { "PH/chainlink_fence", new[] { "wire", "posts" } }, { "PH/vintage_cabinet_01", new[] { "b", "a" } }, { "PH/potted_plant_01", new[] { "leaves", "pot" } }, { "PH/exterior_aircon_unit", new[] { "01", "rusted_01", "rusted_02", "02" } }, { "PH/Camera_01", new[] { "body", "strap", "lens_body" } }, { "PH/potted_plant_02", new[] { "leaves", "pot" } }, { "PH/fancy_picture_frame_01", new[] { "canvas", "" } }, { "PH/vintage_radio_transceiver", new[] { "accessories", "" } }, { "PH/CashRegister_01", new[] { "glass", "" } } };
    // per-model orientation fix so the model's front faces local +Z (from step6 audit)
    public static readonly Dictionary<string, Vector3> FrontFix = new Dictionary<string, Vector3> { { "PH/painted_wooden_cabinet", new Vector3(0, 180, 0) }, { "PH/metal_office_desk", new Vector3(0, 180, 0) }, { "PH/sofa_02", new Vector3(0, 180, 0) }, { "Ext/fridge", new Vector3(0, 180, 0) }, { "PH/vintage_cabinet_01", new Vector3(0, 180, 0) } };
    public static Material PHMat(string id)
    {
        string k = "PH" + id; if (cache.TryGetValue(k, out var m)) return m;
        var d = Resources.Load<Texture2D>(id + "_d"); var n = Resources.Load<Texture2D>(id + "_n"); var ms = Resources.Load<Texture2D>(id + "_m");
        m = d && n ? PBR(Color.white, d, n, ms, 0.45f) : Lit(Color.gray);
        if (d && UnityEngine.Experimental.Rendering.GraphicsFormatUtility.HasAlphaChannel(d.graphicsFormat) && true)
        { m.SetFloat("_AlphaClip", 1); m.SetFloat("_Cutoff", 0.45f); m.EnableKeyword("_ALPHATEST_ON"); m.SetFloat("_Cull", 0); m.SetOverrideTag("RenderType", "TransparentCutout"); m.renderQueue = 2450; m.doubleSidedGI = true; m.SetFloat("_Smoothness", 0.6f); }
        m.enableInstancing = true; cache[k] = m; return m;
    }
    static string PHKey(string id, string[] keys, string mat)
    {
        if (keys == null) return id;
        string best = null; var tail = mat.StartsWith(id.Substring(3)) ? mat.Substring(id.Length - 3) : mat;
        foreach (var k in keys) if (k.Length > 0 && (tail == "_" + k || tail.StartsWith("_" + k + "_") || tail.StartsWith("_" + k + " ") || (tail.Contains(k) && k.Length > 2)) && (best == null || k.Length > best.Length)) best = k;
        if (best == null) best = System.Array.IndexOf(keys, "") >= 0 ? "" : keys[0];
        return best == "" ? id : id + "__" + best;
    }
    public static void ApplyPH(GameObject g, string id)
    {
        phSets.TryGetValue(id, out var keys);
        foreach (var r in g.GetComponentsInChildren<Renderer>()) { var a = r.sharedMaterials; for (int i = 0; i < a.Length; i++) a[i] = PHMat(PHKey(id, keys, a[i] ? a[i].name : "")); r.sharedMaterials = a; }
    }
    // ---- vegetation clump: one random sub-mesh of a Poly Haven plant model
    static readonly Dictionary<string, List<MeshFilter>> clumpMeshes = new Dictionary<string, List<MeshFilter>>();
    public static GameObject Clump(string id, Transform parent, Vector3 pos, float yaw, float height, System.Random rng)
    {
        if (!clumpMeshes.TryGetValue(id, out var list)) { list = new List<MeshFilter>(); var pf = Model(id); if (pf) foreach (var mf in pf.GetComponentsInChildren<MeshFilter>(true)) if (mf.sharedMesh) list.Add(mf); clumpMeshes[id] = list; }
        if (list.Count == 0) return null;
        var src = list[rng.Next(list.Count)]; var pr = src.transform.root;
        var g = new GameObject("clump"); g.transform.SetParent(parent, false); g.transform.position = pos; g.transform.rotation = Quaternion.Euler(0, yaw, 0);
        var c = new GameObject("m"); c.transform.SetParent(g.transform, false);
        c.transform.localRotation = Quaternion.Inverse(pr.rotation) * src.transform.rotation; c.transform.localScale = src.transform.lossyScale;
        c.AddComponent<MeshFilter>().sharedMesh = src.sharedMesh; var r = c.AddComponent<MeshRenderer>(); r.sharedMaterial = PHMat(id);
        r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = true;
        var b = r.bounds; float k = height / Mathf.Max(0.02f, b.size.y); g.transform.localScale = Vector3.one * k;
        b = r.bounds; var off = new Vector3(pos.x - b.center.x, pos.y - b.min.y, pos.z - b.center.z); g.transform.position += off;
        return g;
    }
    // ---- CASUAL toon shading
    static Shader toonS; static bool toonTried;
    public static Shader ToonShader { get { if (!toonTried) { toonTried = true; toonS = Resources.Load<Shader>("Shaders/Toon"); if (!toonS) toonS = Shader.Find("Casual/Toon"); if (toonS && !toonS.isSupported) toonS = null; Debug.Log("[toon] shader " + (toonS ? "ok" : "MISSING -> Lit fallback")); } return toonS; } }
    public static Color Brighten(Color c) { Color.RGBToHSV(c, out var h, out var s, out var v); return Color.HSVToRGB(h, Mathf.Clamp01(s * 1.25f + 0.05f), Mathf.Clamp01(v * 0.8f + 0.22f)); }
    /// realistic photo textures -> a flat pastel crayon colour by material kind (alpha 0 = keep texture average)
    public static Color PaletteFor(string tn)
    {
        tn = tn.ToLower(); int hsh = Mathf.Abs(tn.GetHashCode());
        Color[] walls = { Data.H("f1e2cf"), Data.H("cfe0d0"), Data.H("ffe9a3"), Data.H("aee0ff"), Data.H("b8ecd0"), Data.H("ffd0b0") };
        if (tn.Contains("brick") || tn.Contains("plaster") || tn.Contains("wall") || tn.Contains("concrete") || tn.Contains("corrugated") || tn.Contains("container")) return walls[hsh % walls.Length];
        if (tn.Contains("asphalt") || tn.Contains("road")) return Data.H("cfc8bc");
        if (tn.Contains("grass")) return Data.H("bfe8a0");
        if (tn.Contains("dirt") || tn.Contains("sand") || tn.Contains("gravel")) return Data.H("f2dcb8");
        if (tn.Contains("wood") || tn.Contains("plank") || tn.Contains("floor")) return Data.H("f7d9a8");
        if (tn.Contains("carpet") || tn.Contains("fabric")) return Data.H("e8c9b0");
        if (tn.Contains("metal") || tn.Contains("steel")) return Data.H("cbd8de");
        if (tn.Contains("tile") || tn.Contains("roof")) return Data.H("f5a9a0");
        return new Color(0, 0, 0, 0);
    }
    public static readonly Color PastelShade = new Color(0.74f, 0.7f, 0.66f);
    /// warm dark brown-plum crayon line, faintly tinted by the fill colour (never pure black)
    public static Color WarmLine(Color c) => Color.Lerp(new Color(0.38f, 0.25f, 0.28f), c * 0.45f, 0.18f);
    public static Material Toon(Color c, Texture tex = null, float outline = 0.012f)
    {
        string k = "T" + c + (tex ? tex.GetInstanceID() : 0) + outline; if (cache.TryGetValue(k, out var m)) return m;
        var sh = ToonShader; if (!sh) { m = new Material(litT); m.SetColor("_BaseColor", c); if (tex) m.SetTexture("_BaseMap", tex); m.SetFloat("_Smoothness", 0.1f); cache[k] = m; return m; }
        m = new Material(sh); m.SetColor("_BaseColor", c); if (tex) m.SetTexture("_BaseMap", tex); m.SetFloat("_OutlineWidth", outline);
        if (tex && PaletteFor(tex.name).a > 0) { var pc = PaletteFor(tex.name); bool tinted = c.r + c.g + c.b < 2.9f; m.SetColor("_BaseColor", tinted ? Color.Lerp(c, Color.white, 0.3f) : pc); m.SetFloat("_UseAvg", 0f); m.SetFloat("_TexAmt", 0.15f); }
        m.SetColor("_ShadeColor", PastelShade); m.SetColor("_OutlineColor", WarmLine(c)); m.enableInstancing = true; cache[k] = m; return m;
    }
    static readonly Dictionary<Material, Material> toonOf = new Dictionary<Material, Material>();
    /// Swap every lit material under root for a toon version (keeps colour + albedo texture). Skips emissive, unlit and cutout/transparent materials.
    public static void Toonify(GameObject root, float outline = 0.012f)
    {
        if (!root || !ToonShader) return;
        foreach (var r in root.GetComponentsInChildren<Renderer>(true))
        {
            if (r is ParticleSystemRenderer || r is LineRenderer || r is TrailRenderer || r.gameObject.layer == VMLayer || r.GetComponentInParent<HintRing>() || r.GetComponent<Billboard>()) continue;
            var ms = r.sharedMaterials; bool ch = false;
            for (int i = 0; i < ms.Length; i++)
            {
                var m = ms[i]; if (!m || m.shader == toonS) continue;
                if (!toonOf.TryGetValue(m, out var t))
                {
                    t = null; var sn = m.shader ? m.shader.name : "";
                    bool skip = m.IsKeywordEnabled("_EMISSION") || sn.Contains("Unlit") || sn.Contains("Sky") || m.renderQueue >= 2450 || (m.HasProperty("_Surface") && m.GetFloat("_Surface") > 0.5f);
                    if (!skip)
                    {
                        Color c = m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : m.HasProperty("_Color") ? m.GetColor("_Color") : Color.white;
                        Texture tx = m.HasProperty("_BaseMap") ? m.GetTexture("_BaseMap") : m.HasProperty("_MainTex") ? m.GetTexture("_MainTex") : null;
                        t = new Material(toonS); t.SetColor("_BaseColor", Brighten(c)); if (tx) { t.SetTexture("_BaseMap", tx); if (m.HasProperty("_BaseMap")) { t.SetTextureScale("_BaseMap", m.GetTextureScale("_BaseMap")); t.SetTextureOffset("_BaseMap", m.GetTextureOffset("_BaseMap")); } }
                        t.SetFloat("_OutlineWidth", outline); Color.RGBToHSV(c, out var hh, out var ss, out _); t.SetColor("_OutlineColor", WarmLine(c)); t.SetColor("_ShadeColor", PastelShade); if (tx) { t.SetFloat("_TexAmt", 0.15f); var pc = PaletteFor(tx.name); if (pc.a > 0) { t.SetColor("_BaseColor", pc); t.SetFloat("_UseAvg", 0f); t.SetFloat("_PaletteAmt", 0f); } } t.enableInstancing = true;
                    }
                    toonOf[m] = t;
                }
                if (t) { ms[i] = t; ch = true; }
            }
            if (ch) r.sharedMaterials = ms;
        }
    }
    public static Material Emissive(Color c, float intensity)
    {
        string k = "E" + c + intensity; if (cache.TryGetValue(k, out var m)) return m;
        m = new Material(emT); m.SetColor("_BaseColor", c * 0.6f); m.SetColor("_EmissionColor", c * intensity); m.EnableKeyword("_EMISSION"); cache[k] = m; return m;
    }
    public static Material Unlit(Color c) { string k = "U" + c; if (cache.TryGetValue(k, out var m)) return m; m = new Material(unlitT); m.SetColor("_BaseColor", c); cache[k] = m; return m; }
    public static Material Cutout(Texture t) { var m = new Material(cutT); m.SetTexture("_BaseMap", t); m.SetColor("_BaseColor", Color.white); return m; }

    public static Sprite Icon(string ic)
    {
        if (icons.TryGetValue(ic, out var s)) return s;
        var t = Resources.Load<Texture2D>("Icons/" + Data.IconKey(ic));
        s = t ? Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f)) : null; icons[ic] = s; return s;
    }

    public static GameObject Model(string path) { if (!models.TryGetValue(path, out var g)) { g = Resources.Load<GameObject>(path); models[path] = g; if (!g) Debug.LogWarning("missing model " + path); } return g; }

    public static Bounds RenderBounds(GameObject g)
    {
        var rs = g.GetComponentsInChildren<Renderer>(); Bounds b = new Bounds(g.transform.position, Vector3.zero); bool f = false;
        foreach (var r in rs)
        {
            if (r is ParticleSystemRenderer || r is LineRenderer) continue;
            Bounds rb = r.bounds;
            if (r is SkinnedMeshRenderer sm && sm.sharedMesh)
            {
                var tmp = new Mesh(); sm.BakeMesh(tmp, true); var v = tmp.vertices; if (v.Length > 0) { rb = new Bounds(sm.transform.position + sm.transform.rotation * v[0], Vector3.zero); for (int i = 1; i < v.Length; i += 3) rb.Encapsulate(sm.transform.position + sm.transform.rotation * v[i]); }
                Object.Destroy(tmp);
            }
            if (!f) { b = rb; f = true; } else b.Encapsulate(rb);
        }
        return b;
    }
    /// Spawn a model, scaled uniformly so its largest horizontal dim (or height if fitH) matches, sitting on y.
    static readonly Dictionary<string, GameObject> lod1s = new Dictionary<string, GameObject>();
    static GameObject Lod1(string path) { if (!lod1s.TryGetValue(path, out var g)) { g = Resources.Load<GameObject>(path + "_lod1"); lod1s[path] = g; } return g; }
    public class PlacedRec { public string path; public GameObject holder, model, lod1; }
    public static readonly List<PlacedRec> Placed = new List<PlacedRec>();
    public static readonly Dictionary<string, Vector3> UpFix = new Dictionary<string, Vector3>();
    /// runtime sanity check over every spawned prop: tilt, LOD0/LOD1 mismatch, sunk/floating. Returns issue lines.
    public static List<string> CheckPlaced(string tag)
    {
        var res = new List<string>(); Placed.RemoveAll(p => !p.holder); var allR = Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
        foreach (var p in Placed)
        {
            if (!p.holder.activeInHierarchy) continue; var h = p.holder.transform;
            float tilt = Vector3.Angle(h.up, Vector3.up); if (tilt > 2f) res.Add("tilt " + tilt.ToString("0") + "deg " + p.path + " @" + h.position);
            if (p.lod1)
            {
                var r0 = new List<Renderer>(); foreach (var r in p.model.GetComponentsInChildren<Renderer>(true)) if (!r.transform.IsChildOf(p.lod1.transform)) r0.Add(r);
                Bounds a = new Bounds(), c = new Bounds(); bool fa = false, fc = false;
                foreach (var r in r0) { if (!fa) { a = r.bounds; fa = true; } else a.Encapsulate(r.bounds); }
                foreach (var r in p.lod1.GetComponentsInChildren<Renderer>(true)) { if (!fc) { c = r.bounds; fc = true; } else c.Encapsulate(r.bounds); }
                if (fa && fc && ((a.size - c.size).magnitude > 0.2f * a.size.magnitude || (a.center - c.center).magnitude > 0.15f * a.size.magnitude)) res.Add("lod1 mismatch " + p.path + " lod0 " + a.size + " lod1 " + c.size);
            }
            var b = RenderBounds(p.model); if (b.size.y < 0.05f) continue;
            // ground under the footprint centre, ignoring the prop itself
            var hits = Physics.RaycastAll(new Vector3(b.center.x, b.min.y + 0.6f, b.center.z), Vector3.down, 3f, ~0, QueryTriggerInteraction.Ignore);
            float gy = float.NegativeInfinity; foreach (var hh in hits) { if (hh.collider.transform.IsChildOf(h)) continue; if (hh.point.y > gy) gy = hh.point.y; }
            if (p.path.Contains("hanging") || p.path.Contains("rollershutter") || p.path.Contains("aircon") || p.path.Contains("StreetLight") || p.path.Contains("street_lamp")) continue; // mounted by design
            if (float.IsNegativeInfinity(gy)) gy = -100; float gap = b.min.y - gy;
            if (gap > 0.08f && allR != null)
            {   // supported by a collider-less mesh (shelf board, table top)?
                var ctr = new Vector3(b.center.x, 0, b.center.z);
                foreach (var r in allR) { if (!r || r.transform.IsChildOf(h) || r.GetComponentInParent<HintRing>()) continue; var rb = r.bounds; if (rb.max.y < b.min.y - 0.1f || rb.max.y > b.min.y + 0.06f) continue; if (ctr.x < rb.min.x || ctr.x > rb.max.x || ctr.z < rb.min.z || ctr.z > rb.max.z) continue; gap = 0; break; }
            }
            if (gap > 0.08f) res.Add("floating " + gap.ToString("0.00") + "m " + p.path + " @" + h.position);
            else if (gap < -0.12f && gy - b.min.y < b.size.y * 0.9f) res.Add("sunk " + (-gap).ToString("0.00") + "m " + p.path + " @" + h.position);
        }
        foreach (var r in res) Debug.LogWarning("[orient] " + tag + " " + r);
        Debug.Log("[orient] " + tag + " checked " + Placed.Count + " props, issues " + res.Count);
        return res;
    }
    public static GameObject Spawn(string path, Transform parent, Vector3 pos, float rotY, Vector3 fit, bool collider = true, bool uniform = true)
    {
        string ph = null; if (phMap.TryGetValue(path, out var phId) && Model(phId)) { ph = phId; path = phId; }
        else if (path.StartsWith("PH/")) ph = path;
        var pf = Model(path); GameObject g;
        if (!pf) { g = GameObject.CreatePrimitive(PrimitiveType.Cube); g.GetComponent<Renderer>().sharedMaterial = Lit(Color.magenta); }
        else g = Object.Instantiate(pf);
        if (ph != null) ApplyPH(g, ph);
        GameObject l1 = null; var l1pf = ph != null ? Lod1(path) : null;
        if (l1pf) { l1 = Object.Instantiate(l1pf); ApplyPH(l1, ph); l1.transform.SetParent(g.transform, false); var t0 = pf.transform; var t1 = l1pf.transform;
            // keep LOD1's own import transform relative to LOD0 (prefabs can differ, e.g. -90 X vs 0)
            l1.transform.localRotation = Quaternion.Inverse(t0.localRotation) * t1.localRotation; l1.transform.localScale = new Vector3(t1.localScale.x / Mathf.Max(1e-5f, t0.localScale.x), t1.localScale.y / Mathf.Max(1e-5f, t0.localScale.y), t1.localScale.z / Mathf.Max(1e-5f, t0.localScale.z)); l1.transform.localPosition = Quaternion.Inverse(t0.localRotation) * (t1.localPosition - t0.localPosition);
            if (Quaternion.Angle(t0.localRotation, t1.localRotation) > 1f || (t0.localScale - t1.localScale).sqrMagnitude > 1e-4f) Debug.Log("[orient] lod1 transform differs " + path + " rot " + t0.localEulerAngles + " vs " + t1.localEulerAngles + " scl " + t0.localScale + " vs " + t1.localScale);
            l1.SetActive(true); }
        g.name = path; var holder = new GameObject(path.Substring(path.LastIndexOf('/') + 1));
        holder.transform.SetParent(parent, false);
        g.transform.SetParent(holder.transform, false); g.transform.localPosition = Vector3.zero;
        foreach (var c in g.GetComponentsInChildren<Collider>()) Object.Destroy(c);
        if (FrontFix.TryGetValue(path, out var ff)) g.transform.localRotation = Quaternion.Euler(ff) * g.transform.localRotation;
        if (UpFix.TryGetValue(path, out var uf)) g.transform.localRotation = Quaternion.Euler(uf) * g.transform.localRotation;
        var b = RenderBounds(g); var sz = b.size;
        Vector3 sc;
        if (uniform)
        {
            float s = float.MaxValue;
            if (fit.x > 0 && sz.x > 1e-4f) s = Mathf.Min(s, fit.x / sz.x);
            if (fit.y > 0 && sz.y > 1e-4f) s = Mathf.Min(s, fit.y / sz.y);
            if (fit.z > 0 && sz.z > 1e-4f) s = Mathf.Min(s, fit.z / sz.z);
            if (s == float.MaxValue) s = 1; sc = Vector3.one * s;
        }
        else sc = new Vector3(fit.x > 0 ? fit.x / Mathf.Max(sz.x, 1e-4f) : 1, fit.y > 0 ? fit.y / Mathf.Max(sz.y, 1e-4f) : 1, fit.z > 0 ? fit.z / Mathf.Max(sz.z, 1e-4f) : 1);
        g.transform.localScale = Vector3.Scale(g.transform.localScale, sc);
        b = RenderBounds(g);
        g.transform.position -= new Vector3(b.center.x, b.min.y, b.center.z) - holder.transform.position;
        holder.transform.localPosition = pos; holder.transform.localRotation = Quaternion.Euler(0, rotY, 0);
        foreach (var r in g.GetComponentsInChildren<Renderer>()) { r.shadowCastingMode = ShadowCastingMode.On; r.receiveShadows = true; }
        if (l1)
        {
            var r0 = new List<Renderer>(); foreach (var r in g.GetComponentsInChildren<Renderer>()) if (!r.transform.IsChildOf(l1.transform)) r0.Add(r);
            var lg = holder.AddComponent<LODGroup>(); lg.SetLODs(new[] { new LOD(0.16f, r0.ToArray()), new LOD(0.006f, l1.GetComponentsInChildren<Renderer>()) }); lg.RecalculateBounds();
        }
        Placed.Add(new PlacedRec { path = path, holder = holder, model = g, lod1 = l1 });
        if (collider)
        {
            var bc = holder.AddComponent<BoxCollider>(); var lb = new Bounds(new Vector3(0, b.size.y / 2, 0), b.size);
            bc.center = lb.center; bc.size = lb.size;
        }
        return holder;
    }

    // ---------- meshes ----------
    public static GameObject Box(Transform parent, Vector3 center, Vector3 size, Material m, bool collide = true, float uvScale = 1f, string name = "box")
    {
        var g = new GameObject(name); g.transform.SetParent(parent, false); g.transform.localPosition = center;
        g.AddComponent<MeshFilter>().sharedMesh = BoxMesh(size, uvScale); var mr = g.AddComponent<MeshRenderer>(); mr.sharedMaterial = m;
        if (collide) { var bc = g.AddComponent<BoxCollider>(); bc.size = size; }
        return g;
    }
    static readonly Dictionary<string, Mesh> meshCache = new Dictionary<string, Mesh>();
    public static Mesh BoxMesh(Vector3 s, float uvs)
    {
        string k = s.ToString("F3") + uvs; if (meshCache.TryGetValue(k, out var cm)) return cm;
        var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new List<int>();
        Vector3 h = s * 0.5f;
        void Face(Vector3 nrm, Vector3 u, Vector3 w, float su, float sw)
        {
            int i = v.Count; Vector3 c = Vector3.Scale(nrm, h);
            v.Add(c - u * su / 2 - w * sw / 2); v.Add(c + u * su / 2 - w * sw / 2); v.Add(c + u * su / 2 + w * sw / 2); v.Add(c - u * su / 2 + w * sw / 2);
            for (int j = 0; j < 4; j++) n.Add(nrm);
            uv.Add(new Vector2(0, 0)); uv.Add(new Vector2(su * uvs, 0)); uv.Add(new Vector2(su * uvs, sw * uvs)); uv.Add(new Vector2(0, sw * uvs));
            tri.AddRange(new[] { i, i + 2, i + 1, i, i + 3, i + 2 });
        }
        Face(Vector3.forward, Vector3.left, Vector3.up, s.x, s.y); Face(Vector3.back, Vector3.right, Vector3.up, s.x, s.y);
        Face(Vector3.right, Vector3.forward, Vector3.up, s.z, s.y); Face(Vector3.left, Vector3.back, Vector3.up, s.z, s.y);
        Face(Vector3.up, Vector3.right, Vector3.forward, s.x, s.z); Face(Vector3.down, Vector3.left, Vector3.forward, s.x, s.z);
        var m = new Mesh(); m.SetVertices(v); m.SetNormals(n); m.SetUVs(0, uv); m.SetTriangles(tri, 0); m.RecalculateTangents(); m.RecalculateBounds();
        meshCache[k] = m; return m;
    }
    public static GameObject Prim(PrimitiveType t, Transform parent, Vector3 pos, Vector3 scale, Material m, bool keepCollider = false)
    {
        var g = GameObject.CreatePrimitive(t); g.transform.SetParent(parent, false); g.transform.localPosition = pos; g.transform.localScale = scale;
        g.GetComponent<Renderer>().sharedMaterial = m; if (!keepCollider) Object.Destroy(g.GetComponent<Collider>()); return g;
    }

    // ---------- UI sprites ----------
    static Sprite rr, ring, disc, soft;
    public static Sprite Rounded { get { if (!rr) rr = MakeRounded(64, 14); return rr; } }
    public static Sprite Ring { get { if (!ring) ring = MakeRing(128, 0.78f, 1f); return ring; } }
    public static Sprite Disc { get { if (!disc) disc = MakeRing(128, 0f, 1f); return disc; } }
    public static Sprite Soft { get { if (!soft) soft = MakeSoft(128); return soft; } }
    static Sprite MakeRounded(int s, int r)
    {
        var t = new Texture2D(s, s, TextureFormat.RGBA32, false); t.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
            {
                float dx = Mathf.Max(0, Mathf.Max(r - x - 0.5f, x + 0.5f - (s - r))), dy = Mathf.Max(0, Mathf.Max(r - y - 0.5f, y + 0.5f - (s - r)));
                float d = Mathf.Sqrt(dx * dx + dy * dy); t.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(r - d + 0.5f)));
            }
        t.Apply(); return Sprite.Create(t, new Rect(0, 0, s, s), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect, new Vector4(r, r, r, r));
    }
    static Sprite MakeRing(int s, float inner, float outer)
    {
        var t = new Texture2D(s, s, TextureFormat.RGBA32, false); t.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
            {
                float d = new Vector2(x + 0.5f - s / 2f, y + 0.5f - s / 2f).magnitude / (s / 2f); float px = 2f / s;
                float a = Mathf.Clamp01((outer - d) / px) * (inner <= 0 ? 1 : Mathf.Clamp01((d - inner) / px));
                t.SetPixel(x, y, new Color(1, 1, 1, a));
            }
        t.Apply(); return Sprite.Create(t, new Rect(0, 0, s, s), new Vector2(.5f, .5f));
    }
    static Sprite MakeSoft(int s)
    {
        var t = new Texture2D(s, s, TextureFormat.RGBA32, false); t.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < s; y++) for (int x = 0; x < s; x++) { float d = new Vector2(x + 0.5f - s / 2f, y + 0.5f - s / 2f).magnitude / (s / 2f); float a = Mathf.Clamp01(1 - d); t.SetPixel(x, y, new Color(1, 1, 1, a * a)); }
        t.Apply(); return Sprite.Create(t, new Rect(0, 0, s, s), new Vector2(.5f, .5f));
    }

    // ---------- lighting / post ----------
    static float baseVig; public static ReflectionProbe Probe; public static Light Sun; public static Volume Vol; static VolumeProfile prof;
    public static void SetupEnvironment(Color sky, Color ground, float sunI, Color sunC, Vector3 sunEuler, float fogStart, float fogEnd, float exposure, float bloom, float vignette, float saturation, Color filter)
    {
        if (!Sun) { var g = new GameObject("Sun"); Sun = g.AddComponent<Light>(); Sun.type = LightType.Directional; Sun.shadows = LightShadows.Soft; Sun.shadowStrength = 0.85f; }
        Sun.color = sunC; Sun.intensity = sunI; Sun.transform.rotation = Quaternion.Euler(sunEuler);
        RenderSettings.sun = Sun;
        bool web = Application.platform == RuntimePlatform.WebGLPlayer;
        var cubeM = Resources.Load<Material>("Mat/SkyHDRI"); var cube = Resources.Load<Cubemap>("Sky/sky");
        if (cubeM && cube && !Casual)
        {
            var sm = new Material(cubeM); sm.SetTexture("_Tex", cube); sm.SetFloat("_Exposure", 0.9f); sm.SetFloat("_Rotation", 40f); sm.SetColor("_Tint", Color.Lerp(Color.gray, sky, 0.25f)); RenderSettings.skybox = sm;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom; RenderSettings.customReflectionTexture = cube;
        }
        else if (Casual && Resources.Load<Shader>("Shaders/PastelSky") is Shader ps && ps.isSupported)
        { var sm = new Material(ps); sm.SetColor("_Top", Color.Lerp(sky, new Color(0.62f, 0.8f, 0.98f), 0.6f)); sm.SetColor("_Ground", Color.Lerp(ground, new Color(1f, 0.9f, 0.82f), 0.6f)); RenderSettings.skybox = sm; }
        else { var skyM = Resources.Load<Material>("Mat/Sky"); if (skyM) { skyM = new Material(skyM); skyM.SetColor("_SkyTint", sky); skyM.SetColor("_GroundColor", ground); skyM.SetFloat("_Exposure", 1.15f); RenderSettings.skybox = skyM; } }
        // ambient sampled from the HDRI (upper / horizon / lower hemisphere means), tinted by preset
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.62f, 0.66f, 0.74f) * 1.25f; RenderSettings.ambientEquatorColor = Color.Lerp(new Color(0.52f, 0.54f, 0.58f), ground, 0.3f); RenderSettings.ambientGroundColor = Color.Lerp(new Color(0.24f, 0.23f, 0.21f), ground, 0.4f);
        if (Casual) { RenderSettings.ambientSkyColor = Color.Lerp(sky, Color.white, 0.5f) * 1.05f; RenderSettings.ambientEquatorColor = Color.Lerp(sky, ground, 0.5f) * 0.95f + new Color(0.15f, 0.15f, 0.15f); RenderSettings.ambientGroundColor = Color.Lerp(ground, Color.white, 0.35f) * 0.7f; }
        RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared; RenderSettings.fogColor = Casual ? Color.Lerp(sky, Color.white, 0.45f) : new Color(0.68f, 0.73f, 0.8f); RenderSettings.fogDensity = 1.35f / fogEnd;
        RenderSettings.reflectionIntensity = 1f;
        QualitySettings.shadowDistance = web ? 45 : 90; QualitySettings.shadowCascades = web ? 2 : 4;
        if (!Probe) { var pg = new GameObject("ReflProbe"); Probe = pg.AddComponent<ReflectionProbe>(); Probe.mode = ReflectionProbeMode.Realtime; Probe.refreshMode = ReflectionProbeRefreshMode.ViaScripting; Probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.NoTimeSlicing; Probe.resolution = web ? 64 : 256; Probe.hdr = true; Probe.importance = 0; pg.AddComponent<ProbeKick>(); }
        Probe.transform.position = new Vector3(0, 1.6f, 0); Probe.size = new Vector3(140, 30, 140); Probe.boxProjection = false; Probe.GetComponent<ProbeKick>().Kick();
        if (!Vol)
        {
            var vg = new GameObject("PostFX"); Vol = vg.AddComponent<Volume>(); Vol.isGlobal = true; prof = ScriptableObject.CreateInstance<VolumeProfile>(); Vol.sharedProfile = prof;
            prof.Add<Tonemapping>(true).mode.Override(TonemappingMode.Neutral);
            var b = prof.Add<Bloom>(true); b.threshold.Override(1.05f); b.scatter.Override(0.65f);
            prof.Add<ColorAdjustments>(true); prof.Add<Vignette>(true); prof.Add<WhiteBalance>(true);
            var lgg = prof.Add<ShadowsMidtonesHighlights>(true);
        }
        prof.TryGet<Bloom>(out var bl); bl.intensity.Override(bloom);
        prof.TryGet<ColorAdjustments>(out var c); c.postExposure.Override(exposure); c.contrast.Override(6f); c.saturation.Override(saturation); c.colorFilter.Override(filter);
        prof.TryGet<Vignette>(out var v); baseVig = vignette; v.intensity.Override(vignette); v.smoothness.Override(0.45f);
        if (Casual)
        {   // pastel crayon grade: soft bloom, low contrast, warm white balance, faint lilac vignette, lighter purple-ish shadows
            Sun.shadowStrength = 0.4f; Sun.color = Color.Lerp(sunC, new Color(1f, 0.93f, 0.84f), 0.5f);
            bl.threshold.Override(1.0f); bl.scatter.Override(0.75f); bl.intensity.Override(0.25f); bl.tint.Override(new Color(1f, 0.95f, 0.88f));
            c.contrast.Override(14f); c.saturation.Override(24f); c.postExposure.Override(-0.05f);
            baseVig = 0.16f; v.intensity.Override(baseVig); v.color.Override(new Color(0.36f, 0.3f, 0.24f));
            if (prof.TryGet<WhiteBalance>(out var wb)) { wb.temperature.Override(8f); wb.tint.Override(-2f); }
            if (prof.TryGet<ShadowsMidtonesHighlights>(out var smh)) smh.shadows.Override(new Vector4(0.96f, 0.98f, 1.02f, -0.02f));
            RenderSettings.ambientSkyColor = new Color(0.76f, 0.8f, 0.84f); RenderSettings.ambientEquatorColor = new Color(0.84f, 0.8f, 0.72f); RenderSettings.ambientGroundColor = new Color(0.62f, 0.6f, 0.54f);
            RenderSettings.fogColor = new Color(0.97f, 0.94f, 0.88f); RenderSettings.fogDensity *= 0.3f; Sun.intensity *= 0.85f;
        }
    }
    /// v0.9 knit UI: blur the world behind modal panels (URP Gaussian DoF, WebGL-safe), instead of a cream whitewash
    public static void UiBlur(bool on)
    {
        if (!prof) return;
        if (!prof.TryGet<DepthOfField>(out var d)) { d = prof.Add<DepthOfField>(true); d.mode.Override(DepthOfFieldMode.Gaussian); d.gaussianStart.Override(0f); d.gaussianEnd.Override(0.6f); d.gaussianMaxRadius.Override(1.5f); d.highQualitySampling.Override(true); }
        d.active = on;
    }
    public static void Hurt(float k) { if (prof && prof.TryGet<Vignette>(out var v)) { v.color.Override(Color.Lerp(Casual ? new Color(0.36f, 0.3f, 0.24f) : Color.black, new Color(0.15f, 0.35f, 0.9f), k)); v.intensity.Override(Mathf.Max(baseVig, k * 0.55f)); } }
}

public class ProbeKick : MonoBehaviour
{
    int n = -1; public void Kick() { n = 3; }
    void Update() { if (n > 0 && --n == 0) { var p = GetComponent<ReflectionProbe>(); var cam = Camera.main; if (cam) p.transform.position = cam.transform.position; p.RenderProbe(); } }
}
