using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// v0.8 lighting pass on top of the pastel storybook grade. Tier follows GeoQ (F8 quality + platform cap: Win 高, WebGL 中, mobile 低).
/// "-lite 0" (or ?lite=0) keeps the v0.7 lighting for before/after shots.
public static class LightQ
{
    public static bool Legacy, NoSoft, NoSoft2, NoVivid, NoDusk;   // NoDusk = v0.9.2 look   // NoVivid = v0.9.1 look   // NoSoft2 = v0.9 look (before v0.9.1 overcast-soft pass)   // NoSoft = v0.8 look (before v0.8.1 softening)
    static Light fill;
    public static void Apply(Transform root)
    {
        int t = GeoQ.Tier; var sun = Art.Sun;
        var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        SetSSAO(urp, Legacy || t >= 1);
        if (Legacy || !sun) { Shader.SetGlobalVector("_ToonLight", Vector4.zero); Shader.SetGlobalVector("_ToonSoft", Vector4.zero); return; }
        float S = NoSoft ? 0f : 1f; float S2 = (NoSoft || NoSoft2) ? 0f : K_S2; Shader.SetGlobalVector("_ToonSoft", new Vector4(S, S2, (NoSoft || NoSoft2) ? 0f : 1f, (NoSoft || NoSoft2 || NoVivid) ? 0f : 1f));
        // soft shadows: resolution / cascades / filter quality per tier
        if (urp)
        {
            urp.mainLightShadowmapResolution = t == 2 ? 4096 : (t == 1 ? 2048 : 1024);
            urp.shadowCascadeCount = t == 2 ? 4 : (t == 1 ? 2 : 1);
            urp.shadowDistance = t == 2 ? 90 : (t == 1 ? 55 : 35);
        }
        QualitySettings.shadowDistance = t == 2 ? 90 : (t == 1 ? 55 : 35);
        sun.shadows = LightShadows.Soft; sun.shadowStrength = Mathf.Lerp(0.55f, 0.3f, S); sun.intensity *= Mathf.Lerp(1f, 0.86f, S);
        var ad = sun.GetComponent<UniversalAdditionalLightData>() ?? sun.gameObject.AddComponent<UniversalAdditionalLightData>();
        ad.softShadowQuality = t == 2 ? SoftShadowQuality.High : (t == 1 ? SoftShadowQuality.Medium : SoftShadowQuality.Low);
        // warm key, cool fill (shader-side, no extra light cost), lamp pools, SSAO, rim
        sun.color = new Color(1f, 0.94f, 0.86f); 
        var fd = Quaternion.Euler(25, sun.transform.eulerAngles.y + 160, 0) * Vector3.back;
        Shader.SetGlobalVector("_ToonFillDir", fd.normalized);
        Shader.SetGlobalColor("_ToonFillCol", Color.Lerp(new Color(0.16f, 0.21f, 0.28f), new Color(0.22f, 0.24f, 0.26f), S));
        if (S > 0) { RenderSettings.ambientMode = AmbientMode.Trilight; RenderSettings.ambientSkyColor = Color.Lerp(RenderSettings.ambientSkyColor, new Color(0.9f, 0.92f, 0.94f), 0.5f); RenderSettings.ambientEquatorColor = Color.Lerp(RenderSettings.ambientEquatorColor, new Color(0.92f, 0.88f, 0.8f), 0.5f); RenderSettings.ambientGroundColor = Color.Lerp(RenderSettings.ambientGroundColor, new Color(0.72f, 0.68f, 0.6f), 0.5f); }
        Shader.SetGlobalColor("_ToonSkyCol", new Color(0.08f, 0.08f, 0.07f));
        Shader.SetGlobalVector("_ToonLight", new Vector4(t == 0 ? 0.6f : 1f, t >= 1 ? (t == 2 ? 0.9f : 0.7f) : 0f, t >= 1 ? 1f : 0f, t == 0 ? 0.5f : 1f));
        // gentle bloom + grade + atmosphere
        var prof = Art.Vol ? Art.Vol.sharedProfile : null;
        if (prof)
        {
            if (prof.TryGet<Bloom>(out var bl)) { bl.active = t >= 1; bl.threshold.Override(Mathf.Lerp(1.25f, 0.85f, S)); bl.intensity.Override((t == 2 ? 0.3f : 0.22f) * Mathf.Lerp(1f, 0.45f, S)); bl.scatter.Override(Mathf.Lerp(0.72f, 0.82f, S)); bl.highQualityFiltering.Override(t == 2); }
            if (prof.TryGet<ShadowsMidtonesHighlights>(out var smh)) { smh.shadows.Override(new Vector4(0.94f, 0.98f, 1.04f, -0.03f)); smh.highlights.Override(new Vector4(1.03f, 0.99f, 1.0f, Mathf.Lerp(0.03f, -0.06f, S)));
                if (S > 0) smh.shadows.Override(new Vector4(0.98f, 0.97f, 0.96f, 0.05f)); }
            if (prof.TryGet<ColorAdjustments>(out var c)) { c.contrast.Override(Mathf.Lerp(18f, 2f, S)); c.postExposure.Override(Mathf.Lerp(0f, 0.04f, S)); c.saturation.Override(10f); }
            if (!prof.TryGet<SplitToning>(out var st)) st = prof.Add<SplitToning>(true);
            st.shadows.Override(new Color(0.62f, 0.72f, 0.76f)); st.highlights.Override(new Color(1f, 0.9f, 0.78f)); st.balance.Override(20f);
        }
        RenderSettings.fogColor = new Color(0.95f, 0.93f, 0.87f); RenderSettings.fogDensity *= 1.25f;
        Shader.SetGlobalVector("_ToonDusk", Vector4.zero); Shader.SetGlobalVector("_SkySunCol", Vector4.zero);
        if (S2 > 0) Soft2(root, prof, sun);
        PoolK = 1f; if (S2 > 0 && !NoVivid && !NoDusk) { Dusk(prof, sun); PoolK = 2.1f; }
        if (root && t >= 1) Pools(root, t);
    }
    // v0.9.1 "overcast-bright afternoon": very light shadows, no glare, matte materials, low contrast/saturation, gentle warm haze. Same on every tier (only cost-free params).
    public static bool K_Aces = true;
    public static float K_Shadow = 0.26f, K_Bloom = 0.04f, K_Exposure = -0.3f, K_Contrast = 16f, K_Sat = 46f, K_S2 = 0.3f, K_Vig = 0.12f, K_Fog = 0.03f;
    static void Soft2(Transform root, VolumeProfile prof, Light sun)
    {
        sun.shadowStrength = K_Shadow; sun.color = new Color(1f, 0.96f, 0.9f); sun.intensity *= 0.85f;
        var ad = sun.GetComponent<UniversalAdditionalLightData>(); if (ad && GeoQ.Tier >= 1) ad.softShadowQuality = SoftShadowQuality.High;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.62f, 0.72f, 0.88f); RenderSettings.ambientEquatorColor = new Color(0.86f, 0.72f, 0.62f); RenderSettings.ambientGroundColor = new Color(0.62f, 0.5f, 0.42f);
        Shader.SetGlobalColor("_ToonFillCol", new Color(0.26f, 0.25f, 0.23f)); Shader.SetGlobalColor("_ToonSkyCol", new Color(0.03f, 0.03f, 0.03f));
        RenderSettings.fogColor = new Color(0.78f, 0.84f, 0.92f); RenderSettings.fogDensity *= K_Fog;
        var sk = RenderSettings.skybox;
        if (sk && sk.HasProperty("_Horizon")) { sk.SetColor("_Top", new Color(0.36f, 0.6f, 0.92f)); sk.SetColor("_Mid", new Color(0.54f, 0.74f, 0.95f)); sk.SetColor("_Horizon", new Color(0.8f, 0.85f, 0.95f)); sk.SetColor("_Ground", new Color(0.86f, 0.82f, 0.76f)); sk.SetColor("_Cloud", new Color(0.92f, 0.91f, 0.89f)); }
        if (prof)
        {
            if (prof.TryGet<Tonemapping>(out var tm)) tm.mode.Override(K_Aces ? TonemappingMode.ACES : TonemappingMode.Neutral);
            if (prof.TryGet<Bloom>(out var bl)) { bl.threshold.Override(1.4f); bl.intensity.Override(K_Bloom); bl.scatter.Override(0.6f); }
            if (prof.TryGet<ColorAdjustments>(out var c)) { c.contrast.Override(K_Contrast); c.postExposure.Override(K_Exposure); c.saturation.Override(K_Sat); c.colorFilter.Override(Color.white); }
            if (prof.TryGet<Vignette>(out var v)) v.intensity.Override(K_Vig);
            if (prof.TryGet<WhiteBalance>(out var wb)) { wb.temperature.Override(7f); wb.tint.Override(0f); }
            if (prof.TryGet<ShadowsMidtonesHighlights>(out var smh)) { smh.shadows.Override(new Vector4(0.97f, 0.96f, 1f, 0f)); smh.highlights.Override(new Vector4(1f, 1f, 1f, -0.12f)); }
            if (prof.TryGet<SplitToning>(out var st)) { st.shadows.Override(new Color(0.5f, 0.5f, 0.5f)); st.highlights.Override(new Color(0.5f, 0.5f, 0.5f)); st.balance.Override(0f); }
        }
        Art.SetSoftBaseVig(K_Vig);
        if (root) Matte(root);
        if (root) Materials(root); if (root && !NoDusk && !NoVivid) Glows(root);
    }
    // v0.9.3 nostalgic golden-hour dusk: low warm sun, orange->peach->lavender sky, blue-violet soft shadows, warm haze, gentle glow. Cost-free params, same on every tier.
    public static float D_Elev = 20f, D_Yaw = 72f, D_Sun = 1.08f, D_Temp = 4f, D_Exp = -0.2f, D_Fog = 0.42f, D_Bloom = 0.09f, D_Shadow = 0.7f, D_Sat = 56f, D_Con = 32f;
    static void Dusk(VolumeProfile prof, Light sun)
    {
        var e = sun.transform.eulerAngles; sun.transform.rotation = Quaternion.Euler(D_Elev, D_Yaw, 0);
        sun.color = new Color(1f, 0.82f, 0.62f); sun.intensity *= D_Sun; sun.shadowStrength = D_Shadow;
        RenderSettings.ambientSkyColor = new Color(0.54f, 0.47f, 0.5f); RenderSettings.ambientEquatorColor = new Color(0.8f, 0.57f, 0.42f); RenderSettings.ambientGroundColor = new Color(0.56f, 0.44f, 0.42f);
        Shader.SetGlobalColor("_ToonFillCol", new Color(0.22f, 0.15f, 0.14f));
        Shader.SetGlobalVector("_ToonDusk", new Vector4(0.72f, 0.6f, 0.66f, 1f)   /* v0.9.11 warm rose shade (was blue-violet: interiors read lavender) */);
        RenderSettings.fogColor = new Color(0.97f, 0.78f, 0.68f); RenderSettings.fogDensity *= D_Fog;
        var sk = RenderSettings.skybox;
        if (sk && sk.HasProperty("_Horizon")) { sk.SetColor("_Top", new Color(0.62f, 0.6f, 0.86f)); sk.SetColor("_Mid", new Color(1f, 0.78f, 0.6f)); sk.SetColor("_Horizon", new Color(1f, 0.62f, 0.34f)); sk.SetColor("_Ground", new Color(0.86f, 0.66f, 0.58f)); sk.SetColor("_Cloud", new Color(1f, 0.8f, 0.72f)); }
        Shader.SetGlobalVector("_SkySunDir", -sun.transform.forward); Shader.SetGlobalVector("_SkySunCol", new Vector4(1f, 0.84f, 0.6f, 0.55f));
        if (prof)
        {
            if (prof.TryGet<Bloom>(out var bl)) { bl.threshold.Override(1.0f); bl.intensity.Override(D_Bloom); bl.scatter.Override(0.68f); bl.tint.Override(new Color(1f, 0.85f, 0.7f)); }
            if (prof.TryGet<ColorAdjustments>(out var c)) { c.postExposure.Override(D_Exp); c.saturation.Override(D_Sat); c.contrast.Override(D_Con); }
            if (prof.TryGet<WhiteBalance>(out var wb)) { wb.temperature.Override(D_Temp); wb.tint.Override(2f); }
            if (prof.TryGet<SplitToning>(out var st)) { st.shadows.Override(new Color(0.42f, 0.44f, 0.62f)); st.highlights.Override(new Color(0.62f, 0.52f, 0.42f)); st.balance.Override(-35f); }
            if (prof.TryGet<ShadowsMidtonesHighlights>(out var smh)) smh.highlights.Override(new Vector4(1f, 1f, 1f, -0.16f));
        }
    }
    // v0.9.3 per-material response: tag procedural meshes by what they are (walls stay matte plaster by default)
    static readonly (string k, float a)[] MatMap = {
        ("floor_shop", SoftKit.MatK.Tile), ("floor", SoftKit.MatK.Floor), ("sidewalk", SoftKit.MatK.Tile), ("park_path", SoftKit.MatK.Tile), ("road", SoftKit.MatK.Asphalt),
        ("grass", SoftKit.MatK.Foliage), ("lawn", SoftKit.MatK.Foliage), ("sk_tree", SoftKit.MatK.Foliage), ("sk_bush", SoftKit.MatK.Foliage), ("sk_plant", SoftKit.MatK.Foliage), ("clump", SoftKit.MatK.Foliage), ("Vegetation", SoftKit.MatK.Foliage),
        ("hinge", SoftKit.MatK.Wood), ("sk_bench", SoftKit.MatK.Wood), ("bookcase", SoftKit.MatK.Wood), ("desk", SoftKit.MatK.Wood), ("chair", SoftKit.MatK.Wood), ("sidetable", SoftKit.MatK.Wood), ("crafttable", SoftKit.MatK.Wood), ("sk_counter", SoftKit.MatK.Wood),
        ("sk_scooter", SoftKit.MatK.Metal), ("shop_sign", SoftKit.MatK.Metal), ("sk_ac", SoftKit.MatK.Metal), ("sk_tank", SoftKit.MatK.Metal), ("sk_mailbox", SoftKit.MatK.Metal), ("sk_hydrant", SoftKit.MatK.Metal), ("sk_bin", SoftKit.MatK.Metal), ("PalletRack", SoftKit.MatK.Metal), ("sk_lamp", SoftKit.MatK.Metal),
        ("sk_rug", SoftKit.MatK.Fabric), ("sk_sofa", SoftKit.MatK.Fabric), ("sk_bed", SoftKit.MatK.Fabric), ("sk_curtain", SoftKit.MatK.Fabric), ("sk_petbed", SoftKit.MatK.Fabric), ("sk_awning", SoftKit.MatK.Fabric),
        ("glass", SoftKit.MatK.Glass) };
    public static int Materials(Transform root)
    {
        int n = 0;
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            foreach (var (k, a) in MatMap) if (t.name.StartsWith(k)) { SoftKit.Tag(t, a); n++; break; }
        Debug.Log("[light] mat tagged " + n); return n;
    }
    public static int Matte(Transform root)
    {   // standard-lit materials: kill gloss/metal glare (toon shader handles its own via _ToonSoft.y)
        int n = 0; var seen = new System.Collections.Generic.HashSet<Material>();
        foreach (var r in root.GetComponentsInChildren<Renderer>(true)) foreach (var m in r.sharedMaterials)
            {
                if (!m || !seen.Add(m)) continue;
                if (m.HasProperty("_Smoothness")) { m.SetFloat("_Smoothness", Mathf.Min(m.GetFloat("_Smoothness"), 0.18f)); n++; }
                if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", Mathf.Min(m.GetFloat("_Metallic"), 0.2f));
                if (m.HasProperty("_SpecularHighlights")) { m.SetFloat("_SpecularHighlights", 0f); m.EnableKeyword("_SPECULARHIGHLIGHTS_OFF"); }
                if (m.HasProperty("_EnvironmentReflections")) { m.SetFloat("_EnvironmentReflections", 0f); m.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF"); }
            }
        return n;
    }
    static void SetSSAO(UniversalRenderPipelineAsset urp, bool on)
    {
        if (!urp) return;
        var f = typeof(UniversalRenderPipelineAsset).GetField("m_RendererDataList", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (f?.GetValue(urp) is ScriptableRendererData[] list)
            foreach (var rd in list) if (rd) foreach (var ft in rd.rendererFeatures) if (ft && ft.GetType().Name.Contains("AmbientOcclusion")) ft.SetActive(on);
    }
    public static float PoolK = 1f;
    static Light Pt(Transform at, Vector3 local, Color c, float range, float inten)
    {   inten *= PoolK; if (PoolK > 1) { c = Color.Lerp(c, new Color(1f, 0.7f, 0.42f), 0.45f); range *= 1.2f; }
        var g = new GameObject("lq_pool"); g.transform.SetParent(at, false); g.transform.localPosition = local;
        var l = g.AddComponent<Light>(); l.type = LightType.Point; l.color = c; l.range = range; l.intensity = inten; l.shadows = LightShadows.None; l.renderMode = LightRenderMode.ForcePixel; l.intensity *= (NoSoft2 ? 1f : 0.7f); return l;
    }
    static Material glowB, glowF, lampM; static Transform glowRoot; static readonly HashSet<Transform> glowed = new HashSet<Transform>();
    static Material GlowM(bool bill, Color c, float a)
    {
        var sh = Resources.Load<Shader>("Shaders/Glow") ?? Shader.Find("Soft/Glow"); if (!sh) return null;
        var m = new Material(sh); m.SetColor("_Color", new Color(c.r, c.g, c.b, a)); m.SetFloat("_Bill", bill ? 1 : 0); return m;
    }
    static void G(Transform p, Vector3 lp, float size, Material m, bool flat)
    {
        // parented to a separate root (not the prop) so prop bounds / placement + scale audits are unaffected
        if (!glowRoot) { glowRoot = new GameObject("lq_glows").transform; glowRoot.SetParent(p.root, false); }
        var q = Art.Prim(PrimitiveType.Quad, glowRoot, Vector3.zero, Vector3.one * size, m); q.name = "lq_glow"; q.transform.position = p.TransformPoint(lp);
        if (flat) q.transform.rotation = Quaternion.Euler(90, 0, 0);
        var r = q.GetComponent<Renderer>(); r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
    }
    // v0.9.3 visible motivated light sources: lamp bulb halo + ground pool, warm window glows (all tiers; cheap quads, static, no flicker)
    public static int Glows(Transform root)
    {
        glowRoot = null;
        if (!glowB) glowB = GlowM(true, new Color(1f, 0.72f, 0.4f), 0.85f);
        if (!glowF) glowF = GlowM(false, new Color(1f, 0.66f, 0.36f), 0.36f);
        if (!glowB) return 0; var wg = GlowM(true, new Color(1f, 0.68f, 0.36f), 0.3f);
        int n = 0, w = 0;
        foreach (var tr in root.GetComponentsInChildren<Transform>().Where(x => x.name == "sk_lamp" || x.name == "sk_flamp").ToList())
        {
            if (glowed.Contains(tr)) continue; glowed.Add(tr);
            if (tr.name == "sk_lamp") { G(tr, new Vector3(0, 3.45f, 0), 3.2f, glowB, false); if (!lampM) lampM = Art.Emissive(new Color(1f, 0.86f, 0.6f), 1.35f);
                foreach (var rr in tr.GetComponentsInChildren<Renderer>()) { var ly = tr.InverseTransformPoint(rr.bounds.center).y; if (ly > 3.2f && ly < 3.7f && rr.name != "lq_glow" && rr.bounds.size.y < 1.2f) rr.sharedMaterial = lampM; } G(tr, new Vector3(0, 0.04f, 0), 7f, glowF, true); }
            else G(tr, new Vector3(0, 1.5f, 0), 1.4f, glowB, false);
            n++;
        }
        foreach (var r in root.GetComponentsInChildren<Renderer>().Where(x => x.name.StartsWith("glass")).ToList())
        {
            break; var b = r.bounds;   // v0.9.6: window glow quads read as flat stickers -> off (interiors are modelled now)
             if (b.size.y < 0.3f) continue;
            if (!glowRoot) { glowRoot = new GameObject("lq_glows").transform; glowRoot.SetParent(root.root, false); }
            if (glowed.Contains(r.transform)) continue; glowed.Add(r.transform);
            var q = Art.Prim(PrimitiveType.Quad, glowRoot, Vector3.zero, Vector3.one * Mathf.Max(b.size.x, b.size.y, b.size.z) * 1.5f, wg); q.name = "lq_glow"; q.transform.position = b.center;
            q.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; w++;
        }
        Debug.Log("[light] glows lamps " + n + " windows " + w); return n + w;
    }
    public static int Pools(Transform root, int t)
    {
        int n = 0, cap = t == 2 ? 60 : 24;
        foreach (var tr in root.GetComponentsInChildren<Transform>().Where(x => x.name == "sk_lamp" || x.name == "sk_flamp" || x.name == "sk_curtain").ToList())
        {
            if (n >= cap) break; if (tr.Find("lq_pool")) continue;
            if (tr.name == "sk_lamp") Pt(tr, new Vector3(0, 3.1f, 0), new Color(1f, 0.82f, 0.55f), 6.5f, 0.9f);
            else if (tr.name == "sk_flamp") Pt(tr, new Vector3(0, 1.5f, 0), new Color(1f, 0.74f, 0.45f), 3.2f, 1.5f);
            else Pt(tr, new Vector3(0, -0.3f, 0), new Color(1f, 0.93f, 0.8f), 3.0f, PoolK > 1 ? 0.75f : 0.55f);
            n++;
        }
        if (PoolK > 1) foreach (var r in root.GetComponentsInChildren<Renderer>().Where(x => x.name.StartsWith("glass")).ToList())
            {   // v0.9.4 dusk: warm amber light spilling in through windows (motivated interior light)
                if (n >= cap) break; var b = r.bounds; if (b.size.y < 0.3f || r.transform.Find("lq_pool")) continue;
                Pt(r.transform, r.transform.InverseTransformPoint(b.center), new Color(1f, 0.64f, 0.34f), 5.5f, 0.8f); n++;
            }
        Debug.Log("[light] tier " + t + " pools " + n); return n;
    }
}
