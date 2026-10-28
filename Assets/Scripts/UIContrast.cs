using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

/// Enforces >= 4.5:1 luminance contrast for every UI Text against the panels drawn behind it
/// (all Images earlier in render order whose rect covers the text centre, alpha-composited).
/// Fixes both the Text colour and inline <color=#..> tags, keeping hue. Counts checks / fixes / residual fails.
public class UIContrast : MonoBehaviour
{
    public static int Checked, Fixed, Fails; public const float Ratio = 4.5f;
    static UIContrast inst; float t;
    class S { public string src, outStr; public Color srcCol, outCol; }
    readonly Dictionary<Text, S> map = new Dictionary<Text, S>(); readonly HashSet<string> logged = new HashSet<string>();
    static readonly Regex rx = new Regex("<color=#([0-9a-fA-F]{6})([0-9a-fA-F]{2})?>");
    static readonly Vector3[] wc = new Vector3[4];
    public static void Ensure() { if (!inst) { var g = new GameObject("UIContrast"); DontDestroyOnLoad(g); inst = g.AddComponent<UIContrast>(); } }
    public static void PassNow() { if (inst) inst.Pass(); }
    void LateUpdate() { t -= Time.unscaledDeltaTime; if (t > 0) return; t = 0.2f; Pass(); }
    static bool Fill(Graphic g)
    {
        if (!(g is Image im) || !g.isActiveAndEnabled || g.color.a < 0.05f) return false;
        if (im.sprite && Art.Casual && (im.sprite == Art.Ring || im.sprite == Art.Soft)) return false;
        if (im.type == Image.Type.Filled && im.fillAmount < 0.98f) return false;
        return true;
    }
    static bool Contains(RectTransform r, Vector3 p) { r.GetWorldCorners(wc); return p.x >= wc[0].x && p.x <= wc[2].x && p.y >= wc[0].y && p.y <= wc[2].y; }
    static Color Fix(Color c, Color bg, ref bool changed)
    {
        if (SoftProps.Contrast(c, bg) >= Ratio) return c;
        changed = true; var r = SoftProps.Readable(c, bg, Ratio + 0.1f);
        if (SoftProps.Contrast(r, bg) < Ratio) r = SoftProps.Lum(bg) > 0.18f ? Color.black : Color.white;
        return r;
    }
    void Pass()
    {
        foreach (var cv in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (!cv.isRootCanvas || !cv.isActiveAndEnabled) continue;
            var gs = cv.GetComponentsInChildren<Graphic>(false); var fills = new List<Graphic>();
            foreach (var g in gs)
            {
                if (g is Text tx) { Check(tx, fills); continue; }
                if (Fill(g)) fills.Add(g);
            }
        }
    }
    void Check(Text tx, List<Graphic> fills)
    {
        if (!tx.isActiveAndEnabled || string.IsNullOrEmpty(tx.text) || tx.color.a < 0.35f) return;
        var p = tx.rectTransform.TransformPoint(tx.rectTransform.rect.center);
        Color bg = new Color(0.85f, 0.85f, 0.9f); float cover = 0;
        foreach (var g in fills) if (Contains(g.rectTransform, p)) { var c = g.color; bg = Color.Lerp(bg, new Color(c.r, c.g, c.b), c.a); cover = 1 - (1 - cover) * (1 - c.a); }
        if (cover < 0.5f) return;   // floats over the 3D view: HUD text carries its own outline/shadow
        if (!map.TryGetValue(tx, out var s)) { s = new S(); map[tx] = s; }
        if (tx.text != s.outStr) { s.src = tx.text; s.srcCol = tx.color; } else if (tx.color != s.outCol) s.srcCol = tx.color;
        Checked++; bool ch = false;
        var col = Fix(s.srcCol, bg, ref ch); col.a = s.srcCol.a;
        bool bad = SoftProps.Contrast(col, bg) < Ratio;
        string str = rx.Replace(s.src, m =>
        {
            ColorUtility.TryParseHtmlString("#" + m.Groups[1].Value, out var cc); var r = Fix(cc, bg, ref ch);
            if (SoftProps.Contrast(r, bg) < Ratio) bad = true;
            return "<color=#" + ColorUtility.ToHtmlStringRGB(r) + m.Groups[2].Value + ">";
        });
        if (ch) Fixed++;
        if (bad) { Fails++; if (logged.Add(tx.name + s.src)) Debug.Log("[contrast-ui] FAIL '" + s.src + "' on " + ColorUtility.ToHtmlStringRGB(bg)); }
        if (tx.color != col) tx.color = col; if (tx.text != str) tx.text = str;
        s.outStr = tx.text; s.outCol = tx.color;
    }
}
