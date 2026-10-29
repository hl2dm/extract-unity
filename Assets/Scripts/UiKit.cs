using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class UiKit
{
    public static RectTransform RT(GameObject g) { var r = g.GetComponent<RectTransform>(); return r ? r : g.AddComponent<RectTransform>(); }
    public static RectTransform Node(Transform parent, string name)
    {
        var g = new GameObject(name, typeof(RectTransform)); g.transform.SetParent(parent, false); return (RectTransform)g.transform;
    }
    public static RectTransform Place(this RectTransform r, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax) { r.anchorMin = aMin; r.anchorMax = aMax; r.offsetMin = oMin; r.offsetMax = oMax; return r; }
    public static RectTransform Fill(this RectTransform r, float pad = 0) => r.Place(Vector2.zero, Vector2.one, new Vector2(pad, pad), new Vector2(-pad, -pad));
    /// anchored at pivot point a (0..1), position p, size s
    public static RectTransform At(this RectTransform r, Vector2 a, Vector2 p, Vector2 s) { r.anchorMin = r.anchorMax = a; r.pivot = a; r.anchoredPosition = p; r.sizeDelta = s; return r; }
    public static Image Img(Transform parent, Color c, Sprite s = null, string name = "img")
    {
        var r = Node(parent, name); var i = r.gameObject.AddComponent<Image>(); i.color = Art.Casual && !name.Contains("fade") ? PanelCol(c) : c; i.sprite = s; if (s && s.border != Vector4.zero) i.type = Image.Type.Sliced; i.raycastTarget = false;
        if (Art.Casual && name == "dim") { i.color = new Color(KnitKit.Scrim.r, KnitKit.Scrim.g, KnitKit.Scrim.b, Mathf.Clamp(c.a, 0.52f, 0.6f)); r.gameObject.AddComponent<BlurWhileActive>(); return i; }   // v0.9: dim + blur, never whitewash
        if (Art.Casual && s == Art.Rounded && (name == "panel" || name == "bag" || name == "card" || name == "toast" || name == "portraitTip" || name == "promptBox" || name.StartsWith("btn:"))) { if (name.StartsWith("btn:")) i.color = KnitKit.BtnFill(c); KnitKit.AsFelt(i); return i; }   // v0.9 felt patches
        if (Art.Casual && s && s == Art.Rounded && c.a > 0.3f) { var o = r.gameObject.AddComponent<Outline>(); o.effectColor = new Color(0.45f, 0.3f, 0.36f, 0.55f); o.effectDistance = new Vector2(2.5f, -2.5f); }
        return i;
    }
    public static Text Txt(Transform parent, string t, int size, Color c, TextAnchor al = TextAnchor.MiddleCenter, bool bold = false, string name = "txt")
    {
        var r = Node(parent, name); var x = r.gameObject.AddComponent<InkText>(); x.font = bold ? Art.FontBold : Art.Font; x.text = t; x.fontSize = size; x.color = Art.Casual ? Ink(c) : c; x.alignment = al;
        x.horizontalOverflow = HorizontalWrapMode.Wrap; x.verticalOverflow = VerticalWrapMode.Overflow; x.raycastTarget = false; x.supportRichText = true; x.lineSpacing = 1.05f;
        return x;
    }
    public static Text Shadowed(Text t, float a = 0.6f) { if (Art.Casual && SoftProps.Lum(t.color) > 0.45f) { var ol = t.gameObject.AddComponent<Outline>(); ol.effectColor = new Color(0.36f, 0.24f, 0.3f, 0.85f); ol.effectDistance = new Vector2(1.6f, -1.6f); var sd = t.gameObject.AddComponent<Shadow>(); sd.effectColor = new Color(0.3f, 0.2f, 0.26f, 0.45f); sd.effectDistance = new Vector2(2, -3); return t; } var s = t.gameObject.AddComponent<Shadow>(); s.effectColor = Art.Casual ? new Color(1f, 0.97f, 0.92f, a * 0.8f) : new Color(0, 0, 0, a); s.effectDistance = new Vector2(2, -2); return t; }
    public static Button Btn(Transform parent, string label, Action onClick, Color bg, int size = 26, Color? fg = null)
    {
        var img = Img(parent, bg, Art.Rounded, "btn:" + label); img.raycastTarget = true;
        var b = img.gameObject.AddComponent<Button>(); var cb = b.colors; cb.highlightedColor = new Color(1.15f, 1.15f, 1.15f); cb.pressedColor = new Color(0.8f, 0.8f, 0.8f); cb.fadeDuration = 0.06f; b.colors = cb;
        b.onClick.AddListener(() => { Sfx.Play("click_002", 0.5f); onClick?.Invoke(); });
        var t = Txt(img.transform, label, size, fg ?? Color.white, TextAnchor.MiddleCenter, true); if (Art.Casual && SoftProps.Contrast(t.color, img.color) < 4.5f) t.color = SoftProps.Readable(t.color, img.color); t.rectTransform.Fill(6); if (Art.Casual && fg == null) { ((InkText)t).Bg = img.color; t.color = KnitKit.BtnInk(img.color); } if (false) { var ol = t.gameObject.AddComponent<Outline>(); ol.effectColor = new Color(0.42f, 0.29f, 0.32f, 0.35f); ol.effectDistance = new Vector2(1.5f, -1.5f); }
        return b;
    }
    /// pastel crayon UI: dark neutral panels -> cream paper, saturated fills -> soft pastel. Pure white (icons) untouched.
    public static Color PanelCol(Color c)
    {
        Color.RGBToHSV(c, out var h, out var s, out var v);
        if (s < 0.06f && v > 0.95f) return c;
        if (s < 0.22f && v < 0.45f) return new Color(1f, 0.965f, 0.91f, Mathf.Max(c.a, 0.86f));
        if (v < 0.5f) { var q = Color.HSVToRGB(h, 0.13f, 0.98f); q.a = Mathf.Max(c.a, 0.86f); return q; }
        var p = Color.HSVToRGB(h, Mathf.Clamp01(s * 0.5f + 0.08f), Mathf.Clamp01(0.62f + v * 0.36f)); p.a = c.a; return p;
    }
    /// text becomes crayon "ink": whites -> warm plum, bright colours -> deeper shade of the same hue (readable on cream)
    public static Color Ink(Color c)
    {
        Color.RGBToHSV(c, out var h, out var s, out var v);
        Color o = c;
        if (s < 0.25f && v > 0.6f) o = new Color(0.36f, 0.24f, 0.3f);
        else if (v > 0.6f) o = Color.HSVToRGB(h, Mathf.Clamp01(s + 0.1f), 0.5f);
        o.a = c.a; return o;
    }
    /// faint screen-space paper grain over the 3D view (below the UI canvas)
    public static void PaperOverlay()
    {
        if (GameObject.Find("PaperOverlay")) return;
        var cv = MakeCanvas("PaperOverlay", 5); UnityEngine.Object.Destroy(cv.GetComponent<GraphicRaycaster>());
        const int N = 256; var tex = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
        var px = new Color32[N * N]; var rnd = new System.Random(7);
        for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
        {
            float n = (float)rnd.NextDouble();
            float fib = Mathf.PerlinNoise(x * 0.05f, y * 0.6f) * 0.6f + Mathf.PerlinNoise(x * 0.3f + 9, y * 0.03f) * 0.4f;
            float d = n * 0.5f + fib * 0.5f; byte a = (byte)(Mathf.Clamp01(Mathf.Abs(d - 0.5f) * 1.6f) * 70);
            px[y * N + x] = d > 0.5f ? new Color32(255, 250, 240, a) : new Color32(150, 120, 140, (byte)(a * 0.6f));
        }
        tex.SetPixels32(px); tex.Apply();
        var r = Node(cv.transform, "paper"); r.Fill(); var ri = r.gameObject.AddComponent<RawImage>(); ri.texture = tex; ri.raycastTarget = false;
        ri.uvRect = new Rect(0, 0, Screen.width / 256f, Screen.height / 256f); ri.color = new Color(1, 1, 1, 0.32f);
    }
    public static void Clear(Transform t) { for (int i = t.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(t.GetChild(i).gameObject); }
    public static Canvas MakeCanvas(string name, int order)
    {
        var g = new GameObject(name); var c = g.AddComponent<Canvas>(); c.renderMode = RenderMode.ScreenSpaceOverlay; c.sortingOrder = order;
        var cs = g.AddComponent<CanvasScaler>(); cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; cs.referenceResolution = new Vector2(1920, 1080); cs.matchWidthOrHeight = 0.5f;
        g.AddComponent<GraphicRaycaster>();
        if (!UnityEngine.Object.FindAnyObjectByType<EventSystem>()) { var e = new GameObject("EventSystem"); e.AddComponent<EventSystem>(); e.AddComponent<StandaloneInputModule>(); }
        return c;
    }
}
