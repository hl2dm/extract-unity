using UnityEngine;
using UnityEngine.UI;

/// v0.8.2 background music ("Sunday Tea in the Orchard", generated with Google Gemini).
/// Two sources ping-pong with a 2.5 s crossfade at the loop point; ducks while a container is being searched;
/// M key / corner button: volume 0.35 -> 0.5 -> 0.2 -> off. WebGL: starts only after the first user input (autoplay policy).
public class Bgm : MonoBehaviour
{
    public static Bgm I; public const float Default = 0.35f, Xfade = 2.5f;
    static readonly float[] Steps = { 0.35f, 0.5f, 0.2f, 0f };
    AudioSource[] src = new AudioSource[2]; int cur; AudioClip clip; bool started; float duck = 1, vol; Text label;
    public float Volume => vol; public bool Playing => started && vol > 0 && src[cur].isPlaying;
    void Awake()
    {
        I = this; clip = Resources.Load<AudioClip>("Audio/bgm_tea"); vol = PlayerPrefs.GetFloat("bgm_vol", Default);
        for (int i = 0; i < 2; i++) { var a = gameObject.AddComponent<AudioSource>(); a.clip = clip; a.loop = false; a.playOnAwake = false; a.spatialBlend = 0; a.volume = 0; a.priority = 0; src[i] = a; }
    }
    void Start()
    {
        var cv = UiKit.MakeCanvas("BgmUI", 60);
        var b = UiKit.Btn(cv.transform, Label(), Cycle, Data.H("6a5a8a"), 20); ((RectTransform)b.transform).At(new Vector2(1, 1), new Vector2(-12, -12), new Vector2(118, 40));
        label = b.GetComponentInChildren<Text>();
    }
    string Label() => vol <= 0 ? "音樂 關" : "音樂 " + Mathf.RoundToInt(vol * 100) + "%";
    public void Cycle() { int k = System.Array.IndexOf(Steps, vol); vol = Steps[(k + 1 + Steps.Length) % Steps.Length]; Save(); }
    public void ToggleMute() { vol = vol > 0 ? 0 : PlayerPrefs.GetFloat("bgm_last", Default); Save(); }
    void Save() { if (vol > 0) PlayerPrefs.SetFloat("bgm_last", vol); PlayerPrefs.SetFloat("bgm_vol", vol); if (label) label.text = Label(); }
    void Update()
    {
        if (!clip) return;
        if (Input.GetKeyDown(KeyCode.M)) ToggleMute();
        if (!started)
        {
            bool go = Application.platform != RuntimePlatform.WebGLPlayer || Input.anyKeyDown || Input.GetMouseButtonDown(0) || Input.touchCount > 0 || (Game.I && Game.I.autoshots);
            if (!go) return; started = true; src[cur].time = 0; src[cur].Play();
        }
        bool searching = Game.I && Game.I.ui && Game.I.ui.LootSearching;
        duck = Mathf.MoveTowards(duck, searching ? 0.6f : 1f, Time.unscaledDeltaTime * 0.8f);
        var a = src[cur]; var o = src[1 - cur]; float left = clip.length - a.time;
        if (a.isPlaying && left <= Xfade && !o.isPlaying) { o.time = 0; o.Play(); }
        if (!a.isPlaying && o.isPlaying) { cur = 1 - cur; a = src[cur]; o = src[1 - cur]; left = clip.length - a.time; }
        float target = vol * duck;
        if (o.isPlaying) { float k = Mathf.Clamp01(1 - left / Xfade); a.volume = target * Mathf.Cos(k * Mathf.PI * 0.5f); o.volume = target * Mathf.Sin(k * Mathf.PI * 0.5f); }
        else a.volume = target;
        if (!a.isPlaying && !o.isPlaying) { a.time = 0; a.Play(); }
    }
}
