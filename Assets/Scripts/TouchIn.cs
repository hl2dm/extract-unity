using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// Unified input: keyboard/mouse on desktop, virtual joystick + drag-look + buttons on touch devices.
/// Touch controls show only in touch mode (mobile browser / first real touch / ?touch=1) and hide again on keyboard use.
[DefaultExecutionOrder(-500)]
public class TouchIn : MonoBehaviour
{
    public static bool Active; public static TouchIn I;
    public static int Taps; // test counter
    static Vector2 stick, look; static readonly Dictionary<KeyCode, int> down = new Dictionary<KeyCode, int>(); static readonly HashSet<KeyCode> held = new HashSet<KeyCode>();
    static bool sprintToggle;

    // ---------- query API (replaces raw Input calls) ----------
    public static float Axis(string a)
    {
        float k = Input.GetAxisRaw(a);
        if (!Active) return k;
        if (a == "Horizontal") k += stick.x; else if (a == "Vertical") { k += stick.y; if (held.Contains(KeyCode.S)) k = -1; }
        return Mathf.Clamp(k, -1, 1);
    }
    public static float Look(string a) { if (!Active) return Input.GetAxisRaw(a); return a == "Mouse X" ? look.x : look.y; }
    public static bool Down(KeyCode c) => Input.GetKeyDown(c) || (Active && down.TryGetValue(c, out var f) && f == Time.frameCount);
    public static bool Held(KeyCode c) => Input.GetKey(c) || (Active && (held.Contains(c) || (c == KeyCode.LeftShift && sprintToggle)));
    public static bool Fire() => Active ? Down(KeyCode.Mouse0) : Input.GetMouseButtonDown(0);
    public static bool AnyTap() => Active ? Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began : Input.GetMouseButtonDown(0);
    public static bool WantCursorLock => !Active;

    // ---------- UI ----------
    class B { public KeyCode key; public bool hold, toggle; public RectTransform rt; public Image img; public Text t; public int finger = -1; public string ctx; }
    readonly List<B> btns = new List<B>(); Canvas cv; RectTransform root, safe, stickBase, stickKnob, lookHint, portraitTip; Button fsBtn;
    int stickF = -1, lookF = -1; Vector2 stickC; float tipHideT; bool tipDismissed;
    const float StickR = 110;

    public static void Ensure()
    {
        if (I) return; var g = new GameObject("TouchIn"); DontDestroyOnLoad(g); I = g.AddComponent<TouchIn>();
        string url = Application.absoluteURL ?? "";
        bool force = url.Contains("touch=1"), off = url.Contains("touch=0");
        Active = !off && (force || Application.isMobilePlatform);
        foreach (var a in System.Environment.GetCommandLineArgs()) if (a == "-touch") Active = true;
    }

    void Start() { Build(); Apply(); }
    void Build()
    {
        cv = UiKit.MakeCanvas("TouchUI", 30); root = (RectTransform)cv.transform; cv.gameObject.AddComponent<AdaptiveScaler>(); DontDestroyOnLoad(cv.gameObject);
        safe = UiKit.Node(root, "safe").Fill(); safe.gameObject.AddComponent<SafeArea>();
        // joystick (bottom-left)
        stickBase = UiKit.Img(safe, new Color(1f, 0.97f, 0.92f, 0.5f), KnitKit.Disc, "stickBase").rectTransform; stickBase.At(new Vector2(0, 0), new Vector2(70, 150), new Vector2(StickR * 2, StickR * 2));
        var ring = UiKit.Img(stickBase, new Color(0.42f, 0.29f, 0.36f, 0.5f), Art.Ring, "ring"); ring.rectTransform.Fill();
        stickKnob = UiKit.Img(stickBase, new Color(1f, 0.83f, 0.55f, 0.95f), KnitKit.Disc, "knob").rectTransform; stickKnob.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(96, 96)); stickKnob.pivot = new Vector2(0.5f, 0.5f);
        // right-side buttons (bottom-right cluster), ctx: f=on foot, r=riding, a=always
        AddBtn("互動", KeyCode.E, false, false, "fr", new Vector2(-36, 150), 136, new Color(1f, 0.83f, 0.42f));
        AddBtn("丟", KeyCode.Mouse0, false, false, "f", new Vector2(-40, 316), 108, new Color(1f, 0.62f, 0.62f));
        AddBtn("跳", KeyCode.Space, false, false, "f", new Vector2(-206, 150), 108, new Color(0.62f, 0.85f, 1f));
        AddBtn("跑", KeyCode.LeftShift, false, true, "f", new Vector2(-344, 150), 96, new Color(0.65f, 0.92f, 0.7f));
        AddBtn("換零食", KeyCode.Alpha9, false, false, "f", new Vector2(-196, 300), 92, new Color(0.82f, 0.74f, 1f));
        AddBtn("往後", KeyCode.S, true, false, "r", new Vector2(-36, 160), 124, new Color(1f, 0.6f, 0.55f));
        AddBtn("視角", KeyCode.V, false, false, "r", new Vector2(-200, 170), 100, new Color(0.82f, 0.74f, 1f));
        lookHint = UiKit.Shadowed(UiKit.Txt(safe, "這半邊滑動看看", 26, Color.white, TextAnchor.MiddleRight)).rectTransform; lookHint.At(new Vector2(1, 0.5f), new Vector2(-40, 40), new Vector2(460, 40));
        // portrait tip
        var tip = UiKit.Img(safe, new Color(0.2f, 0.24f, 0.42f, 0.92f), Art.Rounded, "portraitTip"); tip.raycastTarget = true; portraitTip = tip.rectTransform; portraitTip.At(new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(620, 260)); portraitTip.pivot = new Vector2(0.5f, 0.5f);
        var tt = UiKit.Txt(portraitTip, "把手機轉過來玩比較好玩喔！\n<size=22>（點一下關閉）</size>", 30, Color.white, TextAnchor.MiddleCenter, true); tt.rectTransform.Fill(16);
        var tb = tip.gameObject.AddComponent<Button>(); tb.onClick.AddListener(() => tipDismissed = true);
    }
    void AddBtn(string label, KeyCode k, bool hold, bool toggle, string ctx, Vector2 pos, float size, Color c)
    {
        var b = new B { key = k, hold = hold, toggle = toggle, ctx = ctx };
        b.img = UiKit.Img(safe, new Color(c.r, c.g, c.b, 0.9f), KnitKit.Disc, "tbtn:" + label); b.rt = b.img.rectTransform; b.rt.At(new Vector2(1, 0), pos, new Vector2(size, size)); b.rt.pivot = new Vector2(1, 0); b.rt.anchoredPosition = pos;
        var ring = UiKit.Img(b.rt, new Color(0.42f, 0.29f, 0.36f, 0.6f), Art.Ring, "ring"); ring.rectTransform.Fill();
        b.t = UiKit.Txt(b.rt, label, size > 110 ? 28 : 24, new Color(0.3f, 0.2f, 0.26f), TextAnchor.MiddleCenter, true); b.t.rectTransform.Fill(); b.t.lineSpacing = 0.85f;
        btns.Add(b);
    }

    void Apply()
    {
        if (!cv) return; cv.gameObject.SetActive(Active);
        Input.simulateMouseWithTouches = !Active;
    }

    string Ctx()
    {
        var g = Game.I; if (!g || g.InputLocked) return "";
        if (Moto.Cur && Moto.Cur.Riding) return "r";
        return "f";
    }

    void Update()
    {
        // mode switching: keyboard => desktop, real touch => touch
        if (Active && Input.anyKeyDown && !Input.GetMouseButtonDown(0) && !Input.GetMouseButtonDown(1) && Input.touchCount == 0 && !Application.isMobilePlatform) { Active = false; Apply(); }
        else if (!Active && Input.touchCount > 0) { Active = true; Apply(); }
        look = Vector2.zero; if (!Active) { stick = Vector2.zero; held.Clear(); return; }
        string ctx = Ctx(); bool tipUp = Screen.height > Screen.width && !tipDismissed; bool playing = ctx != "" && !tipUp;
        stickBase.gameObject.SetActive(playing); lookHint.gameObject.SetActive(playing && Time.realtimeSinceStartup < 40);
        foreach (var b in btns) { bool on = playing && b.ctx.Contains(ctx); if (b.key == KeyCode.Mouse0 || b.key == KeyCode.Alpha9 || b.key == KeyCode.LeftShift) on &= Game.I.RaidActive; b.rt.gameObject.SetActive(on); if (!on) b.finger = -1; }
        bool portrait = Screen.height > Screen.width; portraitTip.gameObject.SetActive(portrait && !tipDismissed);
        if (!portrait) tipDismissed = false;
        float px2u = 1f / Mathf.Max(0.01f, cv.scaleFactor);
        for (int i = 0; i < Input.touchCount; i++)
        {
            var t = Input.GetTouch(i);
            if (t.phase == TouchPhase.Began)
            {
                Taps++;
                B hit = null; foreach (var b in btns) if (b.rt.gameObject.activeInHierarchy && Within(b.rt, t.position)) { hit = b; break; }
                if (hit != null)
                {
                    hit.finger = t.fingerId; Press(hit);
                }
                else if (playing && stickF < 0 && t.position.x < Screen.width * 0.42f && !OverUI(t)) { stickF = t.fingerId; stickC = t.position; RectTransformUtility.ScreenPointToLocalPointInRectangle(safe, t.position, null, out var lp); stickBase.pivot = new Vector2(0.5f, 0.5f); stickBase.localPosition = lp; }
                else if (playing && lookF < 0 && !OverUI(t)) lookF = t.fingerId;
            }
            else if (t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary)
            {
                if (t.fingerId == stickF) { var d = (t.position - stickC) * px2u; if (d.magnitude > StickR) d = d.normalized * StickR; stick = d / StickR; stickKnob.anchoredPosition = d; }
                else if (t.fingerId == lookF) { var d = t.deltaPosition * px2u; look += new Vector2(d.x, d.y) * 0.09f; }
            }
            else // ended / cancelled
            {
                if (t.fingerId == stickF) { stickF = -1; stick = Vector2.zero; stickKnob.anchoredPosition = Vector2.zero; ResetStick(); }
                if (t.fingerId == lookF) lookF = -1;
                foreach (var b in btns) if (b.finger == t.fingerId) { b.finger = -1; if (b.hold) held.Remove(b.key); }
            }
        }
        if (!playing) { stick = Vector2.zero; stickF = -1; lookF = -1; held.Clear(); sprintToggle = false; }
        foreach (var b in btns)
        {
            bool lit = (b.hold && held.Contains(b.key)) || (b.toggle && sprintToggle) || b.finger >= 0;
            b.rt.localScale = Vector3.one * (lit ? 0.92f : 1f); var c = b.img.color; c.a = lit ? 1f : 0.82f; b.img.color = c;
        }
        if (Input.touchCount == 0 && stickF >= 0) { stickF = -1; stick = Vector2.zero; stickKnob.anchoredPosition = Vector2.zero; ResetStick(); }
        if (Input.touchCount == 0) lookF = -1;
    }
    void ResetStick() { stickBase.pivot = new Vector2(0, 0); stickBase.anchorMin = stickBase.anchorMax = Vector2.zero; stickBase.anchoredPosition = new Vector2(70, 150); }
    void Press(B b)
    {
        if (b.toggle) { sprintToggle = !sprintToggle; return; }
        if (b.hold) { held.Add(b.key); return; }
        if (b.key == KeyCode.Alpha9) { if (Player.I) Player.I.sel = 1 - Player.I.sel; return; }
        down[b.key] = Time.frameCount;
    }
    /// test hook: press a virtual button by key this frame
    public static void Sim(KeyCode k) { down[k] = Time.frameCount + 1; }
    public static void Set(bool on) { Active = on; if (I) I.Apply(); }
    public static bool UIVisible => I && I.cv && I.cv.gameObject.activeSelf;
    public static void SimStick(Vector2 v) { stick = v; }
    static bool Within(RectTransform r, Vector2 sp) => RectTransformUtility.RectangleContainsScreenPoint(r, sp, null);
    static bool OverUI(Touch t) => EventSystem.current && EventSystem.current.IsPointerOverGameObject(t.fingerId);
}

/// keeps a RectTransform inside Screen.safeArea (notches, home bar)
public class SafeArea : MonoBehaviour
{
    Rect last; Vector2Int sz; public static Rect Area;
    void Update()
    {
        var sa = Screen.safeArea; if (sa == last && sz.x == Screen.width && sz.y == Screen.height) return; last = sa; sz = new Vector2Int(Screen.width, Screen.height); Area = sa;
        if (Screen.width <= 0 || Screen.height <= 0) return;
        var r = (RectTransform)transform; r.anchorMin = new Vector2(sa.xMin / Screen.width, sa.yMin / Screen.height); r.anchorMax = new Vector2(sa.xMax / Screen.width, sa.yMax / Screen.height); r.offsetMin = r.offsetMax = Vector2.zero;
    }
}

/// CanvasScaler that adapts to orientation: desktop landscape 1920x1080 (match 0.5), touch landscape 1280x720 expand, portrait 760x1280 expand.
public class AdaptiveScaler : MonoBehaviour
{
    CanvasScaler cs; Vector2Int sz; bool mode;
    void Awake() { cs = GetComponent<CanvasScaler>(); Apply(); }
    void Update() { if (sz.x != Screen.width || sz.y != Screen.height || mode != TouchIn.Active) Apply(); }
    void Apply()
    {
        if (!cs) return; sz = new Vector2Int(Screen.width, Screen.height); mode = TouchIn.Active;
        bool portrait = Screen.height > Screen.width;
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        if (portrait) { cs.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand; cs.referenceResolution = new Vector2(760, 1280); }
        else if (TouchIn.Active && Screen.height < 900) { cs.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand; cs.referenceResolution = new Vector2(1280, 720); }
        else { cs.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; cs.referenceResolution = new Vector2(1920, 1080); cs.matchWidthOrHeight = 0.5f; }
    }
}
