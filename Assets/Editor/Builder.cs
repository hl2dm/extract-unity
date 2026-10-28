using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class Builder
{
    [MenuItem("Extract/Setup")]
    public static void Setup()
    {
        Directory.CreateDirectory("Assets/Settings"); Directory.CreateDirectory("Assets/Resources/Mat"); Directory.CreateDirectory("Assets/Scenes");
        AssetDatabase.Refresh();
        // URP
        var urp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/URP.asset");
        if (!urp)
        {
            var rd = ScriptableObject.CreateInstance<UniversalRendererData>(); AssetDatabase.CreateAsset(rd, "Assets/Settings/Renderer.asset");
            urp = UniversalRenderPipelineAsset.Create(rd); AssetDatabase.CreateAsset(urp, "Assets/Settings/URP.asset");
            try
            {
                var t = Type.GetType("UnityEngine.Rendering.Universal.ScreenSpaceAmbientOcclusion, Unity.RenderPipelines.Universal.Runtime");
                if (t != null)
                {
                    var ao = (ScriptableRendererFeature)ScriptableObject.CreateInstance(t); ao.name = "SSAO"; AssetDatabase.AddObjectToAsset(ao, rd);
                    rd.rendererFeatures.Add(ao);
                    var so = new SerializedObject(rd); var map = so.FindProperty("m_RendererFeatureMap");
                    if (map != null && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(ao, out string g, out long lid)) { map.arraySize++; map.GetArrayElementAtIndex(map.arraySize - 1).longValue = lid; so.ApplyModifiedProperties(); }
                    var aso = new SerializedObject(ao); var st = aso.FindProperty("m_Settings");
                    if (st != null) { Set(st, "Intensity", 1.2f); Set(st, "Radius", 0.35f); Set(st, "DirectLightingStrength", 0.3f); aso.ApplyModifiedProperties(); }
                    EditorUtility.SetDirty(rd); Debug.Log("[setup] SSAO added");
                }
            }
            catch (Exception e) { Debug.LogWarning("[setup] SSAO failed: " + e.Message); }
        }
        try
        {
            urp.shadowDistance = 75; urp.shadowCascadeCount = 2; urp.supportsHDR = true; urp.msaaSampleCount = 1;
            var so = new SerializedObject(urp);
            SetI(so, "m_MainLightShadowmapResolution", 2048); SetI(so, "m_AdditionalLightsRenderingMode", 1); SetI(so, "m_AdditionalLightsPerObjectLimit", 8);
            SetB(so, "m_MainLightShadowsSupported", true); SetB(so, "m_SoftShadowsSupported", true); SetB(so, "m_AdditionalLightShadowsSupported", true); SetI(so, "m_AdditionalLightsShadowmapResolution", 1024);
            so.ApplyModifiedProperties(); EditorUtility.SetDirty(urp);
        }
        catch (Exception e) { Debug.LogWarning("[setup] urp props: " + e.Message); }
        GraphicsSettings.defaultRenderPipeline = urp;
        for (int i = 0; i < QualitySettings.names.Length; i++) { QualitySettings.SetQualityLevel(i, false); QualitySettings.renderPipeline = urp; }
        // materials
        var lit = Shader.Find("Universal Render Pipeline/Lit"); var unlit = Shader.Find("Universal Render Pipeline/Unlit");
        Mat("Lit", lit, m => { m.SetFloat("_Smoothness", 0.2f); });
        Mat("LitEmissive", lit, m => { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", Color.white); m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None; });
        Mat("Unlit", unlit, m => { });
        Mat("UnlitCutout", unlit, m => { m.SetFloat("_AlphaClip", 1); m.SetFloat("_Cutoff", 0.5f); m.EnableKeyword("_ALPHATEST_ON"); m.renderQueue = 2450; m.SetOverrideTag("RenderType", "TransparentCutout"); });
        Mat("Sky", Shader.Find("Skybox/Procedural"), m => { });
        PBRMats();
        // player settings
        PlayerSettings.colorSpace = ColorSpace.Linear; PlayerSettings.companyName = "hl2dm"; PlayerSettings.productName = "Extract-Lite Unity";
        PlayerSettings.defaultScreenWidth = 1600; PlayerSettings.defaultScreenHeight = 900; PlayerSettings.fullScreenMode = FullScreenMode.Windowed; PlayerSettings.runInBackground = true; PlayerSettings.resizableWindow = true;
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled; PlayerSettings.WebGL.decompressionFallback = false;
        try { PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, new[] { GraphicsDeviceType.Direct3D11 }); PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false); } catch { }
        var ps = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset").FirstOrDefault();
        if (ps) { var so = new SerializedObject(ps); var p = so.FindProperty("activeInputHandler"); if (p != null) { p.intValue = 0; so.ApplyModifiedProperties(); } }
        var tm = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset").FirstOrDefault();
        if (tm) { var so = new SerializedObject(tm); var layers = so.FindProperty("layers"); layers.GetArrayElementAtIndex(8).stringValue = "Viewmodel"; layers.GetArrayElementAtIndex(9).stringValue = "EnemyMove"; so.ApplyModifiedProperties(); }
        // scene
        var scn = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorSceneManager.SaveScene(scn, "Assets/Scenes/Bootstrap.unity");
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/Bootstrap.unity", true) };
        AssetDatabase.SaveAssets(); Debug.Log("[setup] done");
    }
    static void Set(SerializedProperty st, string n, float v) { var p = st.FindPropertyRelative(n); if (p != null) p.floatValue = v; }
    static void SetI(SerializedObject so, string n, int v) { var p = so.FindProperty(n); if (p != null) p.intValue = v; else Debug.Log("[setup] no prop " + n); }
    static void SetB(SerializedObject so, string n, bool v) { var p = so.FindProperty(n); if (p != null) p.boolValue = v; else Debug.Log("[setup] no prop " + n); }
    static void Mat(string n, Shader s, Action<Material> f)
    {
        var path = "Assets/Resources/Mat/" + n + ".mat"; var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!m) { m = new Material(s); AssetDatabase.CreateAsset(m, path); } else m.shader = s;
        f(m); EditorUtility.SetDirty(m);
    }

    public static void PBRMats()
    {
        AssetDatabase.Refresh();
        var lit = Shader.Find("Universal Render Pipeline/Lit");
        Mat("LitPBR", lit, m => { m.EnableKeyword("_NORMALMAP"); m.EnableKeyword("_METALLICSPECGLOSSMAP"); m.EnableKeyword("_OCCLUSIONMAP");
            m.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Tex/brick_n.jpg")); m.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Tex/brick_m.png")); m.SetTexture("_OcclusionMap", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Tex/brick_m.png")); });
        Mat("LitPBRCut", lit, m => { m.EnableKeyword("_NORMALMAP"); m.EnableKeyword("_METALLICSPECGLOSSMAP"); m.EnableKeyword("_OCCLUSIONMAP"); m.EnableKeyword("_ALPHATEST_ON"); m.SetFloat("_AlphaClip", 1); m.SetFloat("_Cull", 0); m.SetFloat("_Cutoff", 0.45f); m.renderQueue = 2450;
            m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/PH/grass_medium_01_d.png")); m.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/PH/grass_medium_01_n.jpg")); m.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/PH/grass_medium_01_m.png")); m.SetTexture("_OcclusionMap", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/PH/grass_medium_01_m.png")); });
        Mat("SkyHDRI", Shader.Find("Skybox/Cubemap"), m => { m.SetTexture("_Tex", AssetDatabase.LoadAssetAtPath<Cubemap>("Assets/Resources/Sky/sky.hdr")); });
        AssetDatabase.SaveAssets(); Debug.Log("[pbr] mats " + (AssetDatabase.LoadAssetAtPath<Cubemap>("Assets/Resources/Sky/sky.hdr") != null));
    }
    public static void SoldierController()
    {
        var all = AssetDatabase.LoadAllAssetsAtPath("Assets/Resources/Anim/UAL.fbx");
        var clips = all.OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview")).ToList();
        Debug.Log("[ual] " + clips.Count + " clips: " + string.Join(",", clips.Select(c => c.name)));
        var sav = AssetDatabase.LoadAllAssetsAtPath("Assets/Resources/Soldier/Soldier.fbx").OfType<Avatar>().FirstOrDefault();
        Debug.Log("[soldier] avatar " + (sav ? sav.isValid + " human " + sav.isHuman : "MISSING"));
        AnimationClip C(string n) => clips.FirstOrDefault(c => c.name == n || c.name.EndsWith("|" + n)) ?? clips.FirstOrDefault(c => c.name.EndsWith(n)) ?? clips.FirstOrDefault(c => c.name.Contains(n));
        const string path = "Assets/Resources/Anim/Soldier.controller"; AssetDatabase.DeleteAsset(path);
        var ac = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(path); var sm = ac.layers[0].stateMachine; { var ls = ac.layers; ls[0].iKPass = true; ac.layers = ls; }
        var st = new System.Collections.Generic.Dictionary<string, UnityEditor.Animations.AnimatorState>();
        foreach (var (k, n) in new[] { ("Idle", "Idle_Loop"), ("Walk", "Walk_Loop"), ("Run", "Jog_Fwd_Loop"), ("Aim", "Pistol_Aim_Neutral"), ("Shoot", "Pistol_Shoot"), ("Hit", "Hit_Chest"), ("Death", "Death01"), ("Talk", "Idle_Talking_Loop") })
        { var s = sm.AddState(k); s.motion = C(n); st[k] = s; Debug.Log("[ual] state " + k + " <- " + (s.motion ? s.motion.name : "NULL")); }
        sm.defaultState = st["Idle"];
        var t1 = st["Shoot"].AddTransition(st["Aim"]); t1.hasExitTime = true; t1.exitTime = 0.85f; t1.duration = 0.1f;
        var t2 = st["Hit"].AddTransition(st["Idle"]); t2.hasExitTime = true; t2.exitTime = 0.85f; t2.duration = 0.15f;
        AssetDatabase.SaveAssets();
    }
    public static void Step2Win() { SoldierController(); PBRMats(); BuildWin(); }
    public static void Step2WebGL() { SoldierController(); PBRMats(); BuildWebGL(); }
    public static void PBRAndWin() { PBRMats(); BuildWin(); }
    public static void PBRAndWebGL() { PBRMats(); BuildWebGL(); }
    static string[] Scenes => new[] { "Assets/Scenes/Bootstrap.unity" };
    public static void BuildWin()
    {
        var r = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = Scenes, locationPathName = Path.GetFullPath("Build/Win/ExtractLite.exe"), target = BuildTarget.StandaloneWindows64, options = BuildOptions.None });
        Debug.Log("[build] win " + r.summary.result + " " + r.summary.totalSize); if (Application.isBatchMode) EditorApplication.Exit(r.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded ? 0 : 1);
    }
    public static void BuildWebGL()
    {
        var r = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = Scenes, locationPathName = Path.GetFullPath("Build/WebGL"), target = BuildTarget.WebGL, options = BuildOptions.None });
        Debug.Log("[build] webgl " + r.summary.result + " " + r.summary.totalSize); if (Application.isBatchMode) EditorApplication.Exit(r.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded ? 0 : 1);
    }
}
