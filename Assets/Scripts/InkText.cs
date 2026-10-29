using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

/// v0.8.2: contrast is applied once, at assignment time, as a pure function of (input, background).
/// The same input always yields the same output, so nothing ever "fights" the game code and text can't flicker.
/// Replaces the old UIContrast runtime pass that rewrote Text every 0.2 s.
public class InkText : Text
{
    public const float Ratio = 4.5f;
    static readonly Regex rx = new Regex("<color=#([0-9a-fA-F]{6})([0-9a-fA-F]{2})?>");
    bool hasBg, resolved; Color bg;
    string lastIn, lastOut;
    /// explicit board colour behind this text (set by panels whose background colour changes at runtime)
    public Color Bg { get => bg; set { bg = value; hasBg = true; resolved = true; lastIn = null; base.color = Map(base.color); } }
    public void NoBg() { hasBg = false; resolved = true; lastIn = null; }
    void Resolve()
    {
        if (resolved || !Art.Casual) return; resolved = true;
        Color b = new Color(0.85f, 0.85f, 0.9f); float cover = 0; var stack = new List<Image>();
        for (Transform t = transform; t != null; t = t.parent)
        {
            var p = t.parent; if (p != null)
                for (int i = t.GetSiblingIndex() - 1; i >= 0; i--)
                {
                    var s = p.GetChild(i); var im = s.GetComponent<Image>(); var r = s as RectTransform;
                    if (im && r && r.anchorMin == Vector2.zero && r.anchorMax == Vector2.one && Solid(im)) { stack.Add(im); break; }
                }
            if (t != transform) { var im = t.GetComponent<Image>(); if (im && Solid(im)) stack.Add(im); }
        }
        for (int i = stack.Count - 1; i >= 0; i--) { var c = stack[i].color; b = Color.Lerp(b, new Color(c.r, c.g, c.b), c.a); cover = 1 - (1 - cover) * (1 - c.a); }
        if (cover >= 0.5f) { bg = b; hasBg = true; }
    }
    static bool Solid(Image im) => im.color.a >= 0.05f && !(im.sprite && (im.sprite == Art.Ring || im.sprite == Art.Soft)) && im.type != Image.Type.Filled;
    Color Map(Color c)
    {
        if (!hasBg || GetComponent<Shadow>()) return c;
        if (SoftProps.Contrast(c, bg) >= Ratio) return c;
        var r = SoftProps.Readable(c, bg, Ratio + 0.1f); if (SoftProps.Contrast(r, bg) < Ratio) r = SoftProps.Lum(bg) > 0.18f ? new Color(0.17f, 0.12f, 0.12f) : Color.white;
        r.a = c.a; return r;
    }
    public override string text
    {
        get => base.text;
        set
        {
            if (value == lastIn && lastOut != null) { if (base.text != lastOut) base.text = lastOut; return; }
            Resolve(); string o = value;
            if (hasBg && !string.IsNullOrEmpty(value) && value.IndexOf("<color=#", System.StringComparison.Ordinal) >= 0 && !GetComponent<Shadow>())
                o = rx.Replace(value, m => { ColorUtility.TryParseHtmlString("#" + m.Groups[1].Value, out var cc); return "<color=#" + ColorUtility.ToHtmlStringRGB(Map(cc)) + m.Groups[2].Value + ">"; });
            lastIn = value; lastOut = o; base.text = o;
        }
    }
    public override Color color { get => base.color; set { Resolve(); base.color = Map(value); } }
    public Color BgUsed => bg; public bool HasBg => hasBg;
}
