using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// v0.9 UI design system "針織・手作" (direction C): felt patches with stitched seams, sewn pockets, yarn tags.
/// Sprites are greyscale-baked (white felt, grey stitches) and tinted with Image.color, so stitches always harmonise with the fill.
public static class KnitKit
{
    public static readonly Color Scrim = Data.H("241E2A"), PatchA = Data.H("F3E9E3"), PatchB = Data.H("F0E1D8"), TagPaper = Data.H("EFE2D0"),
        Ink1 = Data.H("4A3238"), Ink2 = Data.H("74585A"), Thread = Data.H("8A6A6E"), Thread2 = Data.H("C9726A"),
        SlotEmpty = Data.H("E6D8CE"), SlotEmptyLine = Data.H("8F726C"), SlotFill = Data.H("FBF5F0"), SlotTaken = Data.H("E9DFD6"),
        Go = Data.H("8FBF9A"), GoInk = Data.H("1E3F28"), Warn = Data.H("E8A6A0"), WarnInk = Data.H("5A1E1E"), Neutral = Data.H("FBF5F0"),
        Money = Data.H("8C4E32"), TimerBg = Data.H("2E2430"), TimerInk = Data.H("F3E9E3"), HintBg = new Color(0.141f, 0.118f, 0.165f, 0.72f),
        Yarn = Data.H("B99A94"), Tape = Data.H("E3C98F"), Select = Data.H("A8445A"), KeyChip = Data.H("A8445A");
    /// rarity (普通, 不錯, 稀有, 超稀有, 傳說) tuned to the felt palette; badge ink chosen for >= 4.5:1
    public static readonly Color[] Rar = { Data.H("806A60"), Data.H("4F7A52"), Data.H("4F6E9C"), Data.H("6E4F9C"), Data.H("E3B766") };
    public static Color RarInk(int r) => r >= 4 ? Ink1 : Color.white;

    public static bool IsLine(Sprite s) => s != null && s == pocketLine;
    static Sprite felt, pocket, pocketLine, disc, tag;
    /// felt patch: soft fibre noise, darker rim, dashed running stitch inset 9px. Tiled (edges repeat) so dashes never stretch.
    public static Sprite Felt { get { if (!felt) felt = MakeFelt(); return felt; } }
    /// sewn pocket for item slots: flat fill, inner top shade, dashed outline
    public static Sprite Pocket { get { if (!pocket) pocket = MakePocket(true); return pocket; } }
    /// dashed outline only (tint = rarity colour)
    public static Sprite PocketLine { get { if (!pocketLine) pocketLine = MakePocket(false); return pocketLine; } }
    /// felt button disc with circular stitch (touch controls, coat buttons)
    public static Sprite Disc { get { if (!disc) disc = MakeDisc(); return disc; } }

    static float Hash(int x, int y) { unchecked { int h = x * 374761393 + y * 668265263; h = (h ^ (h >> 13)) * 1274126177; return ((h ^ (h >> 16)) & 0xffff) / 65535f; } }
    static float RoundRectDist(float x, float y, float w, float h, float r)
    {   // signed distance to a rounded rect [0,w]x[0,h] (negative inside)
        float qx = Mathf.Abs(x - w / 2) - (w / 2 - r), qy = Mathf.Abs(y - h / 2) - (h / 2 - r);
        float ox = Mathf.Max(qx, 0), oy = Mathf.Max(qy, 0);
        return Mathf.Sqrt(ox * ox + oy * oy) + Mathf.Min(Mathf.Max(qx, qy), 0) - r;
    }
    static Sprite MakeFelt()
    {
        const int S = 184, B = 28, R = 20; /* 128px tiled centre keeps big panels under the 65k vertex limit */ const float inset = 9f, period = 8f, on = 5f;
        var t = new Texture2D(S, S, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        var px = new Color[S * S];
        for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
        {
            float fx = x + 0.5f, fy = y + 0.5f, d = RoundRectDist(fx, fy, S, S, R);
            float a = Mathf.Clamp01(-d + 0.5f);
            // fibre noise: periodic in the 16px tiled centre (coordinates folded mod 16)
            int nx = ((x - B) % 128 + 128) % 128, ny = ((y - B) % 128 + 128) % 128;
            float n = 0.955f + 0.045f * Hash(nx, ny) - 0.02f * Hash(nx / 2 + 40, ny / 2 + 9);
            float v = n;
            if (d > -4f) v *= 0.80f;            // felt rim, slightly darker
            else if (d > -5.5f) v *= 0.9f;
            // running stitch on the inset rounded path
            float sd = Mathf.Abs(RoundRectDist(fx, fy, S, S, R) + inset);
            if (sd < 1.3f)
            {
                bool dash;
                bool inHx = fx > R && fx < S - R, inVy = fy > R && fy < S - R;
                if (inHx && !inVy) dash = (((fx - B) % period) + period) % period < on;
                else if (inVy && !inHx) dash = (((fy - B) % period) + period) % period < on;
                else { float ang = Mathf.Atan2(fy - S / 2f, fx - S / 2f); dash = Mathf.Repeat(ang * 6f / Mathf.PI, 2f) < 1.25f; }
                if (dash) v = Mathf.Lerp(v, 0.5f, Mathf.Clamp01(1.3f - sd));
            }
            px[y * S + x] = new Color(v, v, v, a);
        }
        t.SetPixels(px); t.Apply();
        return Sprite.Create(t, new Rect(0, 0, S, S), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect, new Vector4(B, B, B, B));
    }
    static Sprite MakePocket(bool fill)
    {
        const int S = 84, B = 22, R = 16; const float inset = 3.5f, period = 10f, on = 6f;
        var t = new Texture2D(S, S, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        var px = new Color[S * S];
        for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
        {
            float fx = x + 0.5f, fy = y + 0.5f, d = RoundRectDist(fx, fy, S, S, R);
            float a = Mathf.Clamp01(-d + 0.5f); float v = 1f, la = 0f;
            if (fill && fy > S - 8 && fx > B && fx < S - B) v = 0.93f + 0.07f * (S - fy) / 8f;   // inner top shade (pocket opening)
            float sd = Mathf.Abs(d + inset);
            if (sd < 1.6f)
            {
                bool inHx = fx > R && fx < S - R, inVy = fy > R && fy < S - R, dash;
                if (inHx && !inVy) dash = (((fx - B) % period) + period) % period < on;
                else if (inVy && !inHx) dash = (((fy - B) % period) + period) % period < on;
                else { float ang = Mathf.Atan2(fy - S / 2f, fx - S / 2f); dash = Mathf.Repeat(ang * 8f / Mathf.PI, 2f) < 1.2f; }
                if (dash) { la = Mathf.Clamp01(1.6f - sd); v = Mathf.Lerp(v, 0.55f, la); }
            }
            px[y * S + x] = fill ? new Color(v, v, v, a) : new Color(1, 1, 1, la * a);
        }
        t.SetPixels(px); t.Apply();
        return Sprite.Create(t, new Rect(0, 0, S, S), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect, new Vector4(B, B, B, B));
    }
    static Sprite MakeDisc()
    {
        const int S = 128; var t = new Texture2D(S, S, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color[S * S];
        for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
        {
            float dx = x + 0.5f - S / 2f, dy = y + 0.5f - S / 2f, r = Mathf.Sqrt(dx * dx + dy * dy);
            float a = Mathf.Clamp01(S / 2f - r); float v = 0.955f + 0.045f * Hash(x, y);
            if (r > S / 2f - 5) v *= 0.8f;
            float sr = Mathf.Abs(r - (S / 2f - 13));
            if (sr < 1.5f && Mathf.Repeat(Mathf.Atan2(dy, dx) * 14f / Mathf.PI, 2f) < 1.2f) v = Mathf.Lerp(v, 0.5f, Mathf.Clamp01(1.5f - sr));
            px[y * S + x] = new Color(v, v, v, a);
        }
        t.SetPixels(px); t.Apply(); return Sprite.Create(t, new Rect(0, 0, S, S), new Vector2(.5f, .5f));
    }

    /// apply the felt look to an existing Image (keeps its colour as the fill tint)
    public static Image AsFelt(Image i, bool shadow = true)
    {
        i.sprite = Felt; i.type = Image.Type.Tiled; i.fillCenter = true;
        foreach (var o in i.GetComponents<Outline>()) Object.Destroy(o);
        if (shadow && !i.GetComponent<Shadow>()) { var s = i.gameObject.AddComponent<Shadow>(); s.effectColor = new Color(0.14f, 0.1f, 0.16f, 0.35f); s.effectDistance = new Vector2(0, -6); }
        return i;
    }
    public static Image Patch(Transform parent, Color fill, string name = "patch")
    {
        var r = UiKit.Node(parent, name); var i = r.gameObject.AddComponent<Image>(); i.color = fill; i.raycastTarget = false; return AsFelt(i);
    }
    public static Image PocketImg(Transform parent, Color fill, string name = "pocket")
    {
        var r = UiKit.Node(parent, name); var i = r.gameObject.AddComponent<Image>(); i.sprite = Pocket; i.type = Image.Type.Tiled; i.color = fill; i.raycastTarget = false; return i;
    }
    public static Image Line(Transform parent, Color c, string name = "rim")
    {
        var r = UiKit.Node(parent, name); var i = r.gameObject.AddComponent<Image>(); i.sprite = PocketLine; i.type = Image.Type.Tiled; i.color = c; i.raycastTarget = false; r.Fill(); return i;
    }
    /// yarn hanging tag with a title, sewn on the panel's top-left corner
    public static Text Tag(Transform parent, string title, Vector2 pos, Vector2 size, int fontSize = 30)
    {
        var p = Patch(parent, TagPaper, "tag"); var r = p.rectTransform; r.anchorMin = r.anchorMax = new Vector2(0, 1); r.pivot = new Vector2(0, 0.5f); r.anchoredPosition = pos; r.sizeDelta = size; r.localRotation = Quaternion.Euler(0, 0, 1.5f);
        var hole = UiKit.Img(p.transform, Thread, Art.Disc, "hole"); hole.rectTransform.At(new Vector2(0, 0.5f), new Vector2(14, 0), new Vector2(12, 12));
        var t = UiKit.Txt(p.transform, title, fontSize, Ink1, TextAnchor.MiddleLeft, true, "tagT"); t.rectTransform.Place(Vector2.zero, Vector2.one, new Vector2(36, 0), new Vector2(-14, 0)); ((InkText)t).Bg = TagPaper; t.color = Ink1;
        return t;
    }
    /// strip of washi tape (decor)
    public static Image TapeAt(Transform parent, Vector2 anchor, Vector2 pos, float rot)
    {
        var i = UiKit.Img(parent, new Color(Tape.r, Tape.g, Tape.b, 0.85f), null, "tape"); i.color = new Color(Tape.r, Tape.g, Tape.b, 0.85f);
        i.rectTransform.At(anchor, pos, new Vector2(92, 26)); i.rectTransform.pivot = new Vector2(0.5f, 0.5f); i.rectTransform.anchoredPosition = pos; i.rectTransform.localRotation = Quaternion.Euler(0, 0, rot); return i;
    }
    /// coat button (decor)
    public static Image ButtonAt(Transform parent, Vector2 anchor, Vector2 pos, Color c)
    {
        var i = UiKit.Img(parent, c, Disc, "coatBtn"); i.color = c; i.rectTransform.At(anchor, pos, new Vector2(26, 26)); i.rectTransform.pivot = new Vector2(0.5f, 0.5f); i.rectTransform.anchoredPosition = pos;
        for (int k = 0; k < 4; k++) { var h = UiKit.Img(i.transform, Ink1, Art.Disc, "bh"); h.color = new Color(Ink1.r, Ink1.g, Ink1.b, 0.7f); h.rectTransform.At(new Vector2(0.5f, 0.5f), new Vector2(k % 2 == 0 ? -4 : 4, k < 2 ? -4 : 4), new Vector2(5, 5)); h.rectTransform.pivot = new Vector2(0.5f, 0.5f); h.rectTransform.anchoredPosition = new Vector2(k % 2 == 0 ? -4 : 4, k < 2 ? -4 : 4); }
        return i;
    }
    /// world scrim: plum darkening + URP gaussian blur (never a cream whitewash)
    public static Image ScrimImg(Transform parent, float a = 0.48f)
    {
        var i = UiKit.Img(parent, new Color(Scrim.r, Scrim.g, Scrim.b, a), null, "scrim"); i.color = new Color(Scrim.r, Scrim.g, Scrim.b, a); i.rectTransform.Fill(); i.raycastTarget = true;
        i.gameObject.AddComponent<BlurWhileActive>(); return i;
    }
    public static Color BtnFill(Color raw)
    {
        Color.RGBToHSV(raw, out var h, out var s, out var v); Color o;
        if (s < 0.22f) o = Neutral; else if (h >= 0.22f && h < 0.5f) o = Go; else if (h < 0.05f || h >= 0.92f) o = Warn; else if (h < 0.18f) o = Tape;
        else if (h < 0.72f) o = Data.H("B8C8E2"); else o = Data.H("D2C0E2");
        o.a = Mathf.Max(raw.a, 0.95f); return o;
    }
    public static Color BtnInk(Color fill) { if (fill == Go) return GoInk; if (fill == Warn) return WarnInk; return Ink1; }
    /// felt button: fill colour + ink, stitched border
    public static Button Btn(Transform parent, string label, System.Action onClick, Color fill, Color ink, int size = 24)
    {
        var b = UiKit.Btn(parent, label, onClick, fill, size); var img = b.GetComponent<Image>(); img.color = fill; AsFelt(img, true);
        var t = b.GetComponentInChildren<Text>(); foreach (var o in t.GetComponents<Shadow>()) Object.DestroyImmediate(o); ((InkText)t).Bg = fill; t.color = ink;
        return b;
    }
}

/// turns the world blur on while any scrim is active
public class BlurWhileActive : MonoBehaviour
{
    static int n;
    void OnEnable() { n++; Art.UiBlur(true); }
    void OnDisable() { n = Mathf.Max(0, n - 1); if (n == 0) Art.UiBlur(false); }
}
