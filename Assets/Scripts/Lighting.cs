using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// v0.8 lighting pass on top of the pastel storybook grade. Tier follows GeoQ (F8 quality + platform cap: Win 高, WebGL 中, mobile 低).
/// "-lite 0" (or ?lite=0) keeps the v0.7 lighting for before/after shots.
public static class LightQ
{
    public static bool Legacy, NoSoft, NoSoft2, NoVivid;   // NoVivid = v0.9.1 look   // NoSoft2 = v0.9 look (before v0.9.1 overcast-soft pass)   // NoSoft = v0.8 look (before v0.8.1 softening)
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
        if (S2 > 0) Soft2(root, prof, sun);
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
    static Light Pt(Transform at, Vector3 local, Color c, float range, float inten)
    {
        var g = new GameObject("lq_pool"); g.transform.SetParent(at, false); g.transform.localPosition = local;
        var l = g.AddComponent<Light>(); l.type = LightType.Point; l.color = c; l.range = range; l.intensity = inten; l.shadows = LightShadows.None; l.renderMode = LightRenderMode.ForcePixel; l.intensity *= (NoSoft2 ? 1f : 0.7f); return l;
    }
    public static int Pools(Transform root, int t)
    {
        int n = 0, cap = t == 2 ? 60 : 24;
        foreach (var tr in root.GetComponentsInChildren<Transform>().Where(x => x.name == "sk_lamp" || x.name == "sk_flamp" || x.name == "sk_curtain").ToList())
        {
            if (n >= cap) break; if (tr.Find("lq_pool")) continue;
            if (tr.name == "sk_lamp") Pt(tr, new Vector3(0, 3.1f, 0), new Color(1f, 0.82f, 0.55f), 6.5f, 0.9f);
            else if (tr.name == "sk_flamp") Pt(tr, new Vector3(0, 1.5f, 0), new Color(1f, 0.74f, 0.45f), 3.2f, 1.5f);
            else Pt(tr, new Vector3(0, -0.3f, 0), new Color(1f, 0.93f, 0.8f), 3.0f, 0.55f);
            n++;
        }
        Debug.Log("[light] tier " + t + " pools " + n); return n;
    }
}
