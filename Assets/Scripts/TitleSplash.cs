using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Video;

/// v0.9.24: intro movie (desktop only; any key/click/tap skips) then the 《熊熊搜刮隊》 (Bear Bag Squad) title card (logo) on boot; any key / click / tap skips. Never shown in autotests.
/// Also sets the Windows window caption (productName stays "Extract-Lite Unity" so save data / PlayerPrefs keep working).
public class TitleSplash : MonoBehaviour
{
    public const string GameName = "熊熊搜刮隊";
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
    [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)] static extern bool SetWindowTextW(System.IntPtr h, string t);
    [System.Runtime.InteropServices.DllImport("user32.dll")] static extern System.IntPtr GetActiveWindow();
#endif
    public static void SetWindowTitle()
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        try { SetWindowTextW(GetActiveWindow(), GameName); } catch { }
#endif
    }
    public static void Show()
    {
        var tex = Resources.Load<Texture2D>("Art/logo_xiongxiong"); var go = new GameObject("TitleSplash"); DontDestroyOnLoad(go);
        var cv = go.AddComponent<Canvas>(); cv.renderMode = RenderMode.ScreenSpaceOverlay; cv.sortingOrder = 999;
        var cs = go.AddComponent<CanvasScaler>(); cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; cs.referenceResolution = new Vector2(1920, 1080); cs.matchWidthOrHeight = 0.5f;
        var grp = go.AddComponent<CanvasGroup>();
        var bg = new GameObject("bg").AddComponent<Image>(); bg.transform.SetParent(go.transform, false); bg.color = new Color(1f, 0.95f, 0.86f, 1f);
        var br = bg.rectTransform; br.anchorMin = Vector2.zero; br.anchorMax = Vector2.one; br.offsetMin = br.offsetMax = Vector2.zero;
        if (tex) { var im = new GameObject("logo").AddComponent<RawImage>(); im.transform.SetParent(go.transform, false); im.texture = tex; var r = im.rectTransform; r.sizeDelta = new Vector2(1300, 1300f * tex.height / tex.width); r.anchoredPosition = new Vector2(0, 60); }
        else { var t = MkTxt(go.transform, GameName, 120, new Vector2(0, 60)); t.fontStyle = FontStyle.Bold; }
        MkTxt(go.transform, "按任意鍵 / 點一下開始", 40, new Vector2(0, -360)).color = new Color(0.45f, 0.32f, 0.25f);
        var ts = go.AddComponent<TitleSplash>(); ts.grp = grp;
#if !UNITY_WEBGL
        var path = System.IO.Path.Combine(Application.streamingAssetsPath, "intro.mp4");
        if (System.IO.File.Exists(path) && !SaveSysSeen() && ShotArg() == null) ts.StartCoroutine(ts.Movie(go.transform, path));
#endif
    }
    static Text MkTxt(Transform p, string s, int size, Vector2 pos)
    {
        var t = new GameObject("txt").AddComponent<Text>(); t.transform.SetParent(p, false); t.font = Art.FontBold ?? Art.Font; t.text = s; t.fontSize = size; t.alignment = TextAnchor.MiddleCenter; t.color = new Color(0.36f, 0.24f, 0.18f);
        t.horizontalOverflow = HorizontalWrapMode.Overflow; t.rectTransform.sizeDelta = new Vector2(1600, size * 1.6f); t.rectTransform.anchoredPosition = pos; return t;
    }
    CanvasGroup grp; float t; bool gone, movie;
    static bool SaveSysSeen() => PlayerPrefs.GetInt("intro_seen_v2", 0) == 1;   // the intro plays once
    IEnumerator Movie(Transform root, string path)
    {
        movie = true; var bk = new GameObject("movie").AddComponent<RawImage>(); bk.transform.SetParent(root, false); bk.color = Color.black;
        var r = bk.rectTransform; r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
        var rt = new RenderTexture(1280, 720, 0); var vp = root.gameObject.AddComponent<VideoPlayer>(); vp.playOnAwake = false; vp.source = VideoSource.Url; vp.url = path;
        vp.renderMode = VideoRenderMode.RenderTexture; vp.targetTexture = rt; vp.audioOutputMode = VideoAudioOutputMode.Direct; vp.isLooping = false; vp.Prepare(); AudioListener.pause = true;
        float w = 0; while (!vp.isPrepared && w < 4f && !Skip()) { w += Time.unscaledDeltaTime; yield return null; }
        if (vp.isPrepared && !Skip()) { bk.texture = rt; bk.color = Color.white; vp.Play(); yield return null; float pt = 0;
            while (pt < 0.4f || (vp.isPlaying && !Skip())) { pt += Time.unscaledDeltaTime; yield return null; } }
        vp.Stop(); PlayerPrefs.SetInt("intro_seen_v2", 1); PlayerPrefs.Save(); Destroy(bk.gameObject); Destroy(vp); rt.Release(); t = 0; movie = false; AudioListener.pause = false;
    }
    /// autotest probe: decode the intro muted into a texture, grab a frame (proves the file ships + decodes); never shown on screen
    public static IEnumerator Probe(string shotPath, System.Action<bool, string> done)
    {
        var path = System.IO.Path.Combine(Application.streamingAssetsPath, "intro.mp4"); if (!System.IO.File.Exists(path)) { done(false, "intro.mp4 missing"); yield break; }
        var go = new GameObject("introProbe"); var rt = new RenderTexture(640, 360, 0); var vp = go.AddComponent<VideoPlayer>(); vp.playOnAwake = false; vp.url = path; vp.renderMode = VideoRenderMode.RenderTexture; vp.targetTexture = rt; vp.audioOutputMode = VideoAudioOutputMode.None; vp.Prepare();
        float w = 0; while (!vp.isPrepared && w < 6f) { w += Time.unscaledDeltaTime; yield return null; }
        if (!vp.isPrepared) { Destroy(go); rt.Release(); done(false, "not prepared"); yield break; }
        double len = vp.length; vp.time = 12; vp.Play(); w = 0; while (vp.frame < 5 && w < 4f) { w += Time.unscaledDeltaTime; yield return null; } yield return null;
        var tex = new Texture2D(640, 360, TextureFormat.RGB24, false); var old = RenderTexture.active; RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, 640, 360), 0, 0); tex.Apply(); RenderTexture.active = old;
        float lum = 0; var px = tex.GetPixels(); foreach (var c in px) lum += c.grayscale; lum /= px.Length;
        if (!string.IsNullOrEmpty(shotPath)) System.IO.File.WriteAllBytes(shotPath, tex.EncodeToPNG());
        Destroy(tex); vp.Stop(); Destroy(go); rt.Release(); done(lum > 0.05f, "len " + len.ToString("F1") + " s, frame lum " + lum.ToString("F2"));
    }
    void Quit() => Application.Quit();
    static string ShotArg() { var a = System.Environment.GetCommandLineArgs(); for (int i = 0; i + 1 < a.Length; i++) if (a[i] == "-titleshot") return a[i + 1]; return null; }
    static bool Skip() => Input.anyKeyDown || Input.GetMouseButtonDown(0) || Input.touchCount > 0;
    void Update()
    {
        if (movie) { SetWindowTitle(); return; } t += Time.unscaledDeltaTime; if (t < 1.2f) SetWindowTitle();
        var sa = ShotArg(); if (sa != null) { if (t > 2f && !gone) { gone = true; ScreenCapture.CaptureScreenshot(sa); Invoke(nameof(Quit), 1.5f); } return; }   // review screenshot of the title card
        if (!gone && t > 0.25f && (Input.anyKeyDown || Input.GetMouseButtonDown(0) || Input.touchCount > 0 || t > 6f)) { gone = true; StartCoroutine(Fade()); }
    }
    IEnumerator Fade() { for (float k = 1; k > 0; k -= Time.unscaledDeltaTime * 3f) { grp.alpha = k; yield return null; } Destroy(gameObject); }
}
