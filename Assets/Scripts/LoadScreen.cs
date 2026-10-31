using UnityEngine;
using UnityEngine.UI;

/// v0.9.18 style-B 繪本水粉 loading screen (knit patches): shown on the very first frame after picking a map, so the
/// player never sits on a frozen black screen. Progress is the real build progress reported by the staged loader.
public class LoadScreen : MonoBehaviour
{ bool ownSprite;
    public static LoadScreen I;
    public static bool Up => I && I.gameObject.activeSelf;
    CanvasGroup cg; RectTransform fill, kid; Text pct, stage, tip, title, sub; Image pic;
    float target, shown, tipT, fadeOut = -1; int tipI;
    static readonly string[] Tips = {
        "按住 Ctrl 躡手躡腳走，腳步聲小小的，狗狗比較不會醒來。",
        "鵝看到你會先冒出「？」—— 這時候快躲起來，牠就不會叫！",
        "躲進草叢或稻田裡蹲好，大人走過去也看不到你。",
        "背包滿了就先回公車！越晚的公車，回家的獎勵越多。",
        "看看畫面上方：家人的願望清單，帶回去有加碼。",
        "丟個玩具可以把狗狗引開，趁機溜過去。",
        "上鎖的箱子要鑰匙，翻翻抽屜和信箱找找看。",
        "跑步很快但很吵，靠近狗狗的時候慢慢走喔。",
    };
    public static void Show(ThemeDef th)
    {
        if (!I) Make();
        I.gameObject.SetActive(true); I.fadeOut = -1; I.cg.alpha = 1; I.target = I.shown = 0; I.tipT = 0; I.tipI = Random.Range(0, Tips.Length);
        I.title.text = th.name; I.sub.text = th.twist ?? "";
        var tex = Resources.Load<Texture2D>("Art/load_" + th.id) ?? Resources.Load<Texture2D>("Art/load_aunt");
        if (tex) { if (I.pic.sprite && I.ownSprite) Destroy(I.pic.sprite); I.ownSprite = true; I.pic.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f)); I.pic.color = Color.white; }
        I.tip.text = Tips[I.tipI]; I.stage.text = "準備出發…"; I.Apply();
    }
    public static void Set(float frac, string what = null) { if (!I) return; I.target = Mathf.Max(I.target, Mathf.Clamp01(frac)); if (what != null) I.stage.text = what; }
    public static void Hide() { if (I) { I.target = 1; I.fadeOut = 0; } }
    static void Make()
    {
        var c = UiKit.MakeCanvas("LoadScreen", 900); DontDestroyOnLoad(c.gameObject); I = c.gameObject.AddComponent<LoadScreen>(); I.cg = c.gameObject.AddComponent<CanvasGroup>(); I.cg.blocksRaycasts = true;
        var root = c.transform;
        var bg = UiKit.Img(root, Data.H("F6EBDD"), null, "paper"); bg.rectTransform.Fill(); bg.raycastTarget = true;
        var felt = KnitKit.Patch(root, Data.H("EFE0CF"), "board"); felt.rectTransform.Fill(28);
        // framed illustration (knit patch frame + washi tape)
        var fr = KnitKit.Patch(root, KnitKit.PatchA, "frame"); fr.rectTransform.At(new Vector2(0.5f, 1), new Vector2(0, -70), new Vector2(1180, 670)); fr.rectTransform.localRotation = Quaternion.Euler(0, 0, -0.8f);
        I.pic = UiKit.Img(fr.transform, Color.white, null, "pic"); I.pic.rectTransform.Fill(22); I.pic.preserveAspect = false; I.pic.raycastTarget = false;
        KnitKit.Line(fr.transform, KnitKit.Thread, "stitch");
        KnitKit.TapeAt(fr.transform, new Vector2(0, 1), new Vector2(40, -8), 24); KnitKit.TapeAt(fr.transform, new Vector2(1, 1), new Vector2(-40, -8), -22);
        // map name tag + twist line
        I.title = KnitKit.Tag(root, "", new Vector2(110, -120), new Vector2(430, 84), 44);
        I.sub = UiKit.Txt(root, "", 30, KnitKit.Ink2, TextAnchor.MiddleCenter, false, "sub"); I.sub.rectTransform.At(new Vector2(0.5f, 0), new Vector2(0, 268), new Vector2(1500, 46));
        // progress bar: felt track, sewn fill, a little kid walking along it
        var tr = KnitKit.Patch(root, KnitKit.SlotEmpty, "track"); tr.rectTransform.At(new Vector2(0.5f, 0), new Vector2(0, 178), new Vector2(1240, 50));
        KnitKit.Line(tr.transform, KnitKit.SlotEmptyLine, "trackRim");
        var f = KnitKit.Patch(tr.transform, KnitKit.Go, "fill"); I.fill = f.rectTransform; I.fill.Place(Vector2.zero, new Vector2(0, 1), new Vector2(7, 7), new Vector2(-7, -7));
        var ks = Art.Icon("🎒"); var k = UiKit.Img(tr.transform, Color.white, ks, "kid"); k.preserveAspect = true; k.raycastTarget = false;
        I.kid = k.rectTransform; I.kid.anchorMin = I.kid.anchorMax = new Vector2(0, 0.5f); I.kid.pivot = new Vector2(0.5f, 0.5f); I.kid.sizeDelta = new Vector2(54, 54); if (!ks) k.color = new Color(0, 0, 0, 0);
        I.pct = UiKit.Txt(tr.transform, "0%", 28, KnitKit.Ink1, TextAnchor.MiddleRight, true, "pct"); I.pct.rectTransform.Place(new Vector2(1, 0), new Vector2(1, 1), new Vector2(10, 0), new Vector2(110, 0));
        I.stage = UiKit.Txt(root, "", 26, KnitKit.Ink1, TextAnchor.MiddleLeft, false, "stage"); I.stage.rectTransform.At(new Vector2(0.5f, 0), new Vector2(-620, 128), new Vector2(800, 34)); I.stage.rectTransform.pivot = new Vector2(0, 0.5f); I.stage.rectTransform.anchoredPosition = new Vector2(-620, 128);
        // rotating tip on a paper chip
        var tp = KnitKit.Patch(root, KnitKit.TagPaper, "tipChip"); tp.rectTransform.At(new Vector2(0.5f, 0), new Vector2(0, 52), new Vector2(1420, 64));
        var bulb = UiKit.Txt(tp.transform, "小撇步", 24, KnitKit.Select, TextAnchor.MiddleLeft, true, "tipHead"); bulb.rectTransform.Place(Vector2.zero, new Vector2(0, 1), new Vector2(26, 0), new Vector2(130, 0));
        I.tip = UiKit.Txt(tp.transform, "", 27, KnitKit.Ink1, TextAnchor.MiddleLeft, false, "tip"); I.tip.rectTransform.Place(Vector2.zero, Vector2.one, new Vector2(140, 0), new Vector2(-20, 0));
        c.gameObject.SetActive(false);
    }
    void Apply()
    {
        fill.anchorMax = new Vector2(Mathf.Max(0.02f, shown), 1);
        kid.anchorMin = kid.anchorMax = new Vector2(Mathf.Clamp(shown, 0.03f, 0.97f), 0.5f); kid.anchoredPosition = new Vector2(0, 4 + Mathf.Abs(Mathf.Sin(Time.unscaledTime * 9f)) * 6f);
        pct.text = Mathf.RoundToInt(shown * 100) + "%";
    }
    void Update()
    {
        float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
        shown = Mathf.MoveTowards(shown, target, Mathf.Max(0.6f * dt, (target - shown) * 6f * dt)); Apply();
        tipT += dt; if (tipT > 4.5f) { tipT = 0; tipI = (tipI + 1) % Tips.Length; tip.text = Tips[tipI]; }
        if (fadeOut >= 0 && shown > 0.995f) { fadeOut += dt; cg.alpha = 1 - Mathf.Clamp01(fadeOut / 0.45f); if (fadeOut >= 0.45f) { gameObject.SetActive(false); fadeOut = -1; } }
    }
}
