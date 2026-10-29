using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// v0.8 lighting pass on top of the pastel storybook grade. Tier follows GeoQ (F8 quality + platform cap: Win 高, WebGL 中, mobile 低).
/// "-lite 0" (or ?lite=0) keeps the v0.7 lighting for before/after shots.
public static class LightQ
{
    public static bool Legacy;
    static Light fill;
    public static void Apply(Transform root)
    {
        int t = GeoQ.Tier; var sun = Art.Sun;
        var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        SetSSAO(urp, Legacy || t >= 1);
        if (Legacy || !sun) { Shader.SetGlobalVector("_ToonLight", Vector4.zero); return; }
        // soft shadows: resolution / cascades / filter quality per tier
        if (urp)
        {
            urp.mainLightShadowmapResolution = t == 2 ? 4096 : (t == 1 ? 2048 : 1024);
            urp.shadowCascadeCount = t == 2 ? 4 : (t == 1 ? 2 : 1);
            urp.shadowDistance = t == 2 ? 90 : (t == 1 ? 55 : 35);
        }
        QualitySettings.shadowDistance = t == 2 ? 90 : (t == 1 ? 55 : 35);
        sun.shadows = LightShadows.Soft; sun.shadowStrength = 0.55f;
        var ad = sun.GetComponent<UniversalAdditionalLightData>() ?? sun.gameObject.AddComponent<UniversalAdditionalLightData>();
        ad.softShadowQuality = t == 2 ? SoftShadowQuality.High : (t == 1 ? SoftShadowQuality.Medium : SoftShadowQuality.Low);
        // warm key, cool fill (shader-side, no extra light cost), lamp pools, SSAO, rim
        sun.color = new Color(1f, 0.94f, 0.86f); 
        var fd = Quaternion.Euler(25, sun.transform.eulerAngles.y + 160, 0) * Vector3.back;
        Shader.SetGlobalVector("_ToonFillDir", fd.normalized);
        Shader.SetGlobalColor("_ToonFillCol", new Color(0.16f, 0.21f, 0.28f));
        Shader.SetGlobalColor("_ToonSkyCol", new Color(0.08f, 0.08f, 0.07f));
        Shader.SetGlobalVector("_ToonLight", new Vector4(t == 0 ? 0.6f : 1f, t >= 1 ? (t == 2 ? 0.9f : 0.7f) : 0f, t >= 1 ? 1f : 0f, t == 0 ? 0.5f : 1f));
        // gentle bloom + grade + atmosphere
        var prof = Art.Vol ? Art.Vol.sharedProfile : null;
        if (prof)
        {
            if (prof.TryGet<Bloom>(out var bl)) { bl.active = t >= 1; bl.threshold.Override(1.25f); bl.intensity.Override(t == 2 ? 0.3f : 0.22f); bl.scatter.Override(0.72f); bl.highQualityFiltering.Override(t == 2); }
            if (prof.TryGet<ShadowsMidtonesHighlights>(out var smh)) { smh.shadows.Override(new Vector4(0.94f, 0.98f, 1.04f, -0.03f)); smh.highlights.Override(new Vector4(1.03f, 0.99f, 1.0f, 0.03f)); }
            if (prof.TryGet<ColorAdjustments>(out var c)) { c.contrast.Override(18f); c.postExposure.Override(0f); c.saturation.Override(10f); }
            if (!prof.TryGet<SplitToning>(out var st)) st = prof.Add<SplitToning>(true);
            st.shadows.Override(new Color(0.62f, 0.72f, 0.76f)); st.highlights.Override(new Color(1f, 0.9f, 0.78f)); st.balance.Override(20f);
        }
        RenderSettings.fogColor = new Color(0.95f, 0.93f, 0.87f); RenderSettings.fogDensity *= 1.25f;
        if (root && t >= 1) Pools(root, t);
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
        var l = g.AddComponent<Light>(); l.type = LightType.Point; l.color = c; l.range = range; l.intensity = inten; l.shadows = LightShadows.None; l.renderMode = LightRenderMode.ForcePixel; return l;
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
