using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI : MonoBehaviour
{
    public Canvas canvas; RectTransform root, hud, lootP, modal, endP, cineP, toastBox, fade;
    Text hpT, ammoT, timerT, objT, bagT, promptT, moneyT, cineTitle, cineSub, skipT, interactHint;
    Image promptBox, promptKey; Outline promptEdge; string lastPrompt; Text promptKeyT;
    Image hpBar, hpBarLag, hitM, dmgDir, crossH; float hitT, dmgT, hpLag = 1;
    public bool ModalOpen => modal && modal.gameObject.activeSelf; public bool LootOpen => lootP && lootP.gameObject.activeSelf; public bool EndOpen => endP && endP.gameObject.activeSelf;
    static readonly Color Panel = new Color(0.3f, 0.26f, 0.22f, 0.92f), Panel2 = new Color(0.42f, 0.37f, 0.31f, 0.95f), Accent = Data.H("ffb347"), Good = Data.H("7dffa0"), Bad = Data.H("ff6a5a"), Dim = new Color(1, 1, 1, 0.6f);

    public void Build()
    {
        canvas = UiKit.MakeCanvas("UI", 10); canvas.gameObject.AddComponent<AdaptiveScaler>(); var full = (RectTransform)canvas.transform; root = UiKit.Node(full, "Safe").Fill(); root.gameObject.AddComponent<SafeArea>(); /* paper overlay removed: clean flat look */
        // HUD
        hud = UiKit.Node(root, "HUD").Fill();
        var hpBg = UiKit.Img(hud, new Color(0, 0, 0, 0.5f), Art.Rounded, "hpbg"); hpBg.rectTransform.At(new Vector2(0, 0), new Vector2(40, 40), new Vector2(360, 26));
        hpBarLag = UiKit.Img(hpBg.transform, new Color(1, 0.85f, 0.6f, 0.8f), Art.Rounded, "lag"); hpBarLag.rectTransform.Fill(3); hpBarLag.type = Image.Type.Filled; hpBarLag.fillMethod = Image.FillMethod.Horizontal;
        hpBar = UiKit.Img(hpBg.transform, Good, Art.Rounded, "hp"); hpBar.rectTransform.Fill(3); hpBar.type = Image.Type.Filled; hpBar.fillMethod = Image.FillMethod.Horizontal;
        hpT = UiKit.Shadowed(UiKit.Txt(hud, "100", 28, Color.white, TextAnchor.LowerLeft, true)); hpT.rectTransform.At(new Vector2(0, 0), new Vector2(42, 70), new Vector2(400, 40));
        ammoT = UiKit.Shadowed(UiKit.Txt(hud, "", 36, Color.white, TextAnchor.LowerRight, true)); ammoT.rectTransform.At(new Vector2(1, 0), new Vector2(-40, 34), new Vector2(700, 100));
        { var tb = UiKit.Img(hud, new Color(0.3f, 0.2f, 0.26f, 0.38f), Art.Rounded, "timerBg"); tb.raycastTarget = false; tb.rectTransform.At(new Vector2(0.5f, 1), new Vector2(0, -14), new Vector2(380, 62)); tb.rectTransform.pivot = new Vector2(0.5f, 1); }
        timerT = UiKit.Shadowed(UiKit.Txt(hud, "4:00", 46, Color.white, TextAnchor.UpperCenter, true)); timerT.rectTransform.At(new Vector2(0.5f, 1), new Vector2(0, -18), new Vector2(300, 60));
        objT = UiKit.Shadowed(UiKit.Txt(hud, "", 24, new Color(1, 1, 1, 0.9f), TextAnchor.UpperCenter)); objT.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(24, -148), new Vector2(-24, -78));
        bagT = UiKit.Shadowed(UiKit.Txt(hud, "", 24, Color.white, TextAnchor.UpperLeft, true)); bagT.rectTransform.At(new Vector2(0, 1), new Vector2(36, -26), new Vector2(700, 80));
        moneyT = UiKit.Shadowed(UiKit.Txt(root, "", 28, Accent, TextAnchor.UpperRight, true)); moneyT.rectTransform.At(new Vector2(1, 1), new Vector2(-70, -24), new Vector2(600, 50));
        promptBox = UiKit.Img(root, new Color(0.2f, 0.24f, 0.42f, 0.85f), Art.Rounded, "promptBox"); promptBox.rectTransform.At(new Vector2(0.5f, 0.5f), new Vector2(0, -96), new Vector2(400, 54)); promptBox.rectTransform.pivot = new Vector2(0.5f, 0.5f); promptBox.rectTransform.anchoredPosition = new Vector2(0, -96);
        promptEdge = promptBox.gameObject.AddComponent<Outline>(); promptEdge.effectColor = new Color(1f, 0.83f, 0.3f, 0.55f); promptEdge.effectDistance = new Vector2(2, -2);
        promptKey = UiKit.Img(promptBox.transform, Data.H("ffd34d"), Art.Rounded, "key"); promptKey.rectTransform.At(new Vector2(0, 0.5f), new Vector2(10, 0), new Vector2(40, 40));
        var kt = UiKit.Txt(promptKey.transform, "E", 26, Color.black, TextAnchor.MiddleCenter, true); kt.rectTransform.Fill(); promptKeyT = kt;
        promptBox.raycastTarget = true; promptBox.gameObject.AddComponent<Button>().onClick.AddListener(() => { if (TouchIn.Active) TouchIn.Sim(KeyCode.E); });
        promptT = UiKit.Txt(promptBox.transform, "", 26, Color.white, TextAnchor.MiddleLeft, true); promptT.rectTransform.Place(Vector2.zero, Vector2.one, new Vector2(62, 0), new Vector2(-16, 0)); promptT.horizontalOverflow = HorizontalWrapMode.Overflow;
        crossH = UiKit.Img(hud, new Color(1, 1, 1, 0.85f), Art.Disc, "cross"); crossH.rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(6, 6));
        hitM = UiKit.Img(hud, Color.white, Art.Ring, "hit"); hitM.rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(36, 36)); hitM.enabled = false;
        var dd = UiKit.Node(hud, "dmgdir"); dd.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(10, 10));
        dmgDir = UiKit.Img(dd, new Color(1, 0.2f, 0.15f, 0.8f), Art.Soft, "arc"); dmgDir.rectTransform.At(new Vector2(0.5f, 0.5f), new Vector2(0, 200), new Vector2(240, 60)); dmgDir.enabled = false;
        toastBox = UiKit.Node(root, "toasts"); toastBox.At(new Vector2(0.5f, 1), new Vector2(0, -130), new Vector2(1000, 400));
        var vl = toastBox.gameObject.AddComponent<VerticalLayoutGroup>(); vl.childAlignment = TextAnchor.UpperCenter; vl.spacing = 8; vl.childControlHeight = true; vl.childControlWidth = false; vl.childForceExpandHeight = false;
        interactHint = UiKit.Shadowed(UiKit.Txt(root, "", 20, Dim, TextAnchor.LowerCenter)); interactHint.rectTransform.Place(new Vector2(0, 0), new Vector2(1, 0), new Vector2(330, 16), new Vector2(-330, 46));
        // panels
        lootP = UiKit.Node(full, "Loot").Fill(); lootP.gameObject.SetActive(false);
        modal = UiKit.Node(full, "Modal").Fill(); modal.gameObject.SetActive(false);
        endP = UiKit.Node(full, "End").Fill(); endP.gameObject.SetActive(false);
        cineP = UiKit.Node(full, "Cine").Fill(); cineP.gameObject.SetActive(false);
        var top = UiKit.Img(cineP, Color.black, null, "bar"); top.rectTransform.Place(new Vector2(0, 0.88f), Vector2.one, Vector2.zero, Vector2.zero);
        var bot = UiKit.Img(cineP, Color.black, null, "bar"); bot.rectTransform.Place(Vector2.zero, new Vector2(1, 0.12f), Vector2.zero, Vector2.zero);
        cineTitle = UiKit.Shadowed(UiKit.Txt(cineP, "", 96, Color.white, TextAnchor.MiddleCenter, true), 0.8f); cineTitle.rectTransform.At(new Vector2(0.5f, 0.5f), new Vector2(0, 120), new Vector2(1600, 140));
        cineSub = UiKit.Txt(bot.transform, "", 30, Color.white); cineSub.rectTransform.Fill(10);
        skipT = UiKit.Txt(top.transform, TouchIn.Active ? "點一下跳過" : "［空白鍵］跳過", 22, Dim, TextAnchor.MiddleRight); skipT.rectTransform.Fill(30);
        fade = UiKit.Img(full, Color.black, null, "fade").rectTransform; fade.Fill(); fade.GetComponent<Image>().color = new Color(0, 0, 0, 0);
    }

    // ---------- HUD ----------
    public void Hud(bool raid, Player p, float timeLeft, string obj, List<Item> bag, int cap, long money, string prompt, string hint)
    {
        hud.gameObject.SetActive(raid && !EndOpen);
        moneyT.text = raid ? "" : "小豬撲滿 " + Data.Fmt(money);
        interactHint.text = hint ?? "";
        if (string.IsNullOrEmpty(prompt)) promptBox.gameObject.SetActive(false);
        else
        {
            promptBox.gameObject.SetActive(true);
            if (prompt != lastPrompt) { lastPrompt = prompt; promptKeyT.text = TouchIn.Active ? "點" : "E"; promptKeyT.fontSize = TouchIn.Active ? 22 : 26; promptT.text = prompt.Replace("[E] ", "").Replace("　[E]", "").Replace("[E]", "").Trim(); promptBox.rectTransform.sizeDelta = new Vector2(Mathf.Clamp(promptT.preferredWidth + 84, 160, 1100), 54); }
            float a = 0.45f + 0.25f * Mathf.Sin(Time.time * 5); promptEdge.effectColor = new Color(1f, 0.83f, 0.3f, a);
        }
        if (!raid) return;
        // mood bar: full = happy, drains while crying
        float k = p.Crying ? 1 - Mathf.Clamp01(p.cryT / 7f) : 1; hpBar.fillAmount = k; hpLag = Mathf.MoveTowards(hpLag, k, Time.deltaTime * 0.5f); if (hpLag < k) hpLag = k; hpBarLag.fillAmount = hpLag;
        hpBar.color = p.Crying ? Data.H("6fb7ff") : Data.H("ffd34d");
        int toCry = 3 - p.caught % 3;
        hpT.text = p.Crying ? "<color=#4a7fb5>嗚嗚嗚…哭哭中（跑不快）</color>" : "心情很好！<size=18>　再被抓 " + toCry + " 次會哭哭</size>";
        if (ammoTouch != TouchIn.Active) { ammoTouch = TouchIn.Active; if (ammoTouch) ammoT.rectTransform.At(new Vector2(1, 1), new Vector2(-36, -96), new Vector2(560, 60)); else ammoT.rectTransform.At(new Vector2(1, 0), new Vector2(-40, 34), new Vector2(700, 100)); ammoT.alignment = ammoTouch ? TextAnchor.UpperRight : TextAnchor.LowerRight; }
        ammoT.text = (p.sel == 0 ? "<color=#c98a1a>▶罐頭 ×" : "<color=#5c3d4a>罐頭 ×") + SaveSys.S.cans + "</color>　" + (p.sel == 1 ? "<color=#c98a1a>▶肉肉 ×" : "<color=#5c3d4a>肉肉 ×") + SaveSys.S.meats + "</color>" + (TouchIn.Active ? "" : "<size=20>\n[1][2] 切換　左鍵 丟出去</size>");
        int m = Mathf.Max(0, Mathf.CeilToInt(timeLeft)); timerT.text = "<size=24>媽媽叫你回家　</size>" + (m / 60) + ":" + (m % 60).ToString("00"); timerT.color = timeLeft <= 60 ? (Mathf.Repeat(Time.time, 1) < 0.5f ? Bad : Color.white) : Color.white;
        objT.text = obj; long v = 0; int n = 0; foreach (var it in bag) if (!it.key) { v += it.v; n++; }
        bagT.text = "袋子 " + n + "/" + cap + "　<color=#c98a1a>" + Data.Fmt(v) + "</color>" + (Game.I.keys > 0 ? "　<size=20>鑰匙 ×" + Game.I.keys + "</size>" : "");
        hitT -= Time.deltaTime; hitM.enabled = hitT > 0; dmgT -= Time.deltaTime; dmgDir.enabled = false;
        crossH.enabled = true;
    }
    bool ammoTouch;
    public void HitMarker(bool big) { hitT = 0.15f; hitM.color = big ? Bad : Color.white; hitM.rectTransform.sizeDelta = Vector2.one * (big ? 44 : 30); }
    Vector3 dmgFrom; public void DamageDir(Vector3 from) { dmgFrom = from; dmgT = 1f; }

    public void Toast(string s, float dur = 2.6f)
    {
        var bg = UiKit.Img(toastBox, new Color(0.22f, 0.27f, 0.48f, 0.9f), Art.Rounded, "toast");
        var le = bg.gameObject.AddComponent<LayoutElement>(); le.preferredWidth = Mathf.Min(900, root.rect.width - 40); le.minHeight = 52;
        var fit = bg.gameObject.AddComponent<ContentSizeFitter>(); fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var hl = bg.gameObject.AddComponent<HorizontalLayoutGroup>(); hl.padding = new RectOffset(24, 24, 12, 12); hl.childControlHeight = true; hl.childControlWidth = true;
        var t = UiKit.Txt(bg.transform, s, 26, Color.white); bg.rectTransform.sizeDelta = new Vector2(900, 52);
        var cg = bg.gameObject.AddComponent<CanvasGroup>(); StartCoroutine(FadeOut(cg, dur));
        while (toastBox.childCount > 4) DestroyImmediate(toastBox.GetChild(0).gameObject);
    }
    System.Collections.IEnumerator FadeOut(CanvasGroup g, float d) { float t = 0; while (t < d && g) { t += Time.unscaledDeltaTime; g.alpha = t < 0.15f ? t / 0.15f : t > d - 0.4f ? (d - t) / 0.4f : 1; yield return null; } if (g) Destroy(g.gameObject); }
    public void Fade(float a) { fade.GetComponent<Image>().color = new Color(0, 0, 0, a); }

    // ---------- loot (STEP6: web-style container grid + backpack panel) ----------
    class LCell { public RectTransform root, content; public Image bg, glow, ring, spin, icon, bar, flash, chip; public Text name, val, chipT, q; public Outline edge; }
    LootContainer lc; readonly List<LCell> cells = new List<LCell>(); Text lootTitle, lootInfo, bagHead, bagTotal; RectTransform bagGrid; int bagSig = -1; public bool autoTake;
    const float CW = 136, CH = 170, CG = 12, BW = 84, BG = 8;
    int bCols = 5, bRows = 5;
    public void OpenLoot(LootContainer c)
    {
        lc = c; autoTake = false; bagSig = -1; UiKit.Clear(lootP); cells.Clear(); lootP.gameObject.SetActive(true);
        var dim = UiKit.Img(lootP, new Color(0, 0, 0, 0.6f), null, "dim"); dim.rectTransform.Fill();
        int n = c.slots.Count, cols = 4, rows = Mathf.Max(2, Mathf.CeilToInt(n / 4f));
        float W = cols * (CW + CG) - CG + 56, H = rows * (CH + CG) - CG + 120 + 96;
        int bc0 = Mathf.Max(Game.I.BagCap, Game.I.bag.Count); bCols = bc0 > 25 ? 6 : 5; bRows = Mathf.Max(5, Mathf.CeilToInt(bc0 / (float)bCols));
        float BWp = bCols * (BW + BG) - BG + 48, BH = Mathf.Max(H, bRows * (BW + BG) + 170);
        H = BH; float total = W + 24 + BWp;
        // container panel
        var hold = UiKit.Node(lootP, "fit"); hold.At(new Vector2(0.5f, 0.5f), new Vector2(0, 10), new Vector2(total, H)); hold.pivot = new Vector2(0.5f, 0.5f); hold.anchoredPosition = new Vector2(0, 10); hold.gameObject.AddComponent<Fit>();
        var p = UiKit.Img(hold, new Color(0.09f, 0.1f, 0.12f, 0.97f), Art.Rounded, "panel"); p.raycastTarget = true; var pt = p.rectTransform; pt.anchorMin = pt.anchorMax = new Vector2(0.5f, 0.5f); pt.pivot = new Vector2(0, 0.5f); pt.anchoredPosition = new Vector2(-total / 2, 0); pt.sizeDelta = new Vector2(W, H);
        var hd = UiKit.Img(pt, new Color(1, 1, 1, 0.05f), Art.Rounded, "hdr"); hd.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(10, -76), new Vector2(-10, -10));
        lootTitle = UiKit.Txt(pt, c.label, 36, Color.white, TextAnchor.MiddleLeft, true); lootTitle.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(28, -76), new Vector2(-28, -10));
        lootInfo = UiKit.Txt(pt, "", 21, Dim, TextAnchor.MiddleRight); lootInfo.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(28, -76), new Vector2(-28, -10));
        float gx = 28, gy = -96;
        for (int i = 0; i < n; i++)
        {
            int idx = i, r = i / cols, col = i % cols; var L = new LCell();
            L.root = UiKit.Node(pt, "cell"); L.root.anchorMin = L.root.anchorMax = new Vector2(0, 1); L.root.pivot = new Vector2(0.5f, 0.5f); L.root.sizeDelta = new Vector2(CW, CH); L.root.anchoredPosition = new Vector2(gx + col * (CW + CG) + CW / 2, gy - r * (CH + CG) - CH / 2);
            L.glow = UiKit.Img(L.root, new Color(1, 1, 1, 0), Art.Soft, "glow"); L.glow.rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(CW * 1.7f, CH * 1.55f));
            L.content = UiKit.Node(L.root, "content").Fill();
            L.bg = UiKit.Img(L.content, Panel2, Art.Rounded, "bg"); L.bg.rectTransform.Fill(); L.bg.raycastTarget = true; L.bg.type = Image.Type.Sliced;
            L.edge = L.bg.gameObject.AddComponent<Outline>(); L.edge.effectDistance = new Vector2(3, -3); L.edge.effectColor = new Color(0, 0, 0, 0);
            var b = L.bg.gameObject.AddComponent<Button>(); b.transition = Selectable.Transition.None; b.onClick.AddListener(() => Game.I.TakeSlot(idx));
            L.ring = UiKit.Img(L.content, new Color(1, 1, 1, 0.08f), Art.Ring, "ring"); L.ring.rectTransform.At(new Vector2(0.5f, 1), new Vector2(0, -12), new Vector2(98, 98));
            L.spin = UiKit.Img(L.ring.transform, Color.white, Art.Ring, "spin"); L.spin.rectTransform.Fill(); L.spin.type = Image.Type.Filled; L.spin.fillMethod = Image.FillMethod.Radial360; L.spin.fillAmount = 0.28f;
            L.q = UiKit.Txt(L.ring.transform, "?", 46, new Color(1, 1, 1, 0.25f), TextAnchor.MiddleCenter, true); L.q.rectTransform.Fill();
            L.icon = UiKit.Img(L.content, Color.white, null, "icon"); L.icon.rectTransform.At(new Vector2(0.5f, 1), new Vector2(0, -10), new Vector2(102, 102)); L.icon.preserveAspect = true;
            L.name = UiKit.Txt(L.content, "", 19, Color.white, TextAnchor.MiddleCenter, true); L.name.rectTransform.Place(new Vector2(0, 0), new Vector2(1, 0), new Vector2(6, 30), new Vector2(-6, 58)); L.name.horizontalOverflow = HorizontalWrapMode.Wrap; L.name.resizeTextForBestFit = true; L.name.resizeTextMinSize = 13; L.name.resizeTextMaxSize = 19;
            L.val = UiKit.Txt(L.content, "", 19, Accent, TextAnchor.MiddleCenter, true); L.val.rectTransform.Place(new Vector2(0, 0), new Vector2(1, 0), new Vector2(6, 6), new Vector2(-6, 32));
            L.bar = UiKit.Img(L.content, Color.white, Art.Rounded, "bar"); L.bar.rectTransform.Place(new Vector2(0, 0), new Vector2(1, 0), new Vector2(14, 14), new Vector2(-14, 22)); L.bar.type = Image.Type.Filled; L.bar.fillMethod = Image.FillMethod.Horizontal;
            L.chip = UiKit.Img(L.content, Color.white, Art.Rounded, "chip"); L.chip.rectTransform.At(new Vector2(0, 1), new Vector2(6, -6), new Vector2(46, 22));
            L.chipT = UiKit.Txt(L.chip.transform, "", 14, Color.black, TextAnchor.MiddleCenter, true); L.chipT.rectTransform.Fill();
            L.flash = UiKit.Img(L.content, new Color(1, 1, 1, 0), Art.Rounded, "flash"); L.flash.rectTransform.Fill(); L.flash.raycastTarget = false;
            cells.Add(L);
        }
        var bb = UiKit.Node(pt, "btns"); bb.Place(new Vector2(0, 0), new Vector2(1, 0), new Vector2(24, 22), new Vector2(-24, 82));
        var hl = bb.gameObject.AddComponent<HorizontalLayoutGroup>(); hl.spacing = 12; hl.childControlWidth = true; hl.childForceExpandWidth = true; hl.childControlHeight = true;
        UiKit.Btn(bb, TouchIn.Active ? "全部拿走" : "全部拿走 [F]", () => Game.I.LootAll(), Data.H("2f7d4a"), 24);
        UiKit.Btn(bb, TouchIn.Active ? "加速 -50%" : "加速 -50% [Q]", () => Game.I.PaySkip(), Data.H("7a5a1e"), 22);
        UiKit.Btn(bb, TouchIn.Active ? "關閉" : "關閉 [E]", () => Game.I.CloseLoot(), Data.H("3a3f46"), 24);
        // backpack panel
        var q = UiKit.Img(hold, new Color(0.09f, 0.1f, 0.12f, 0.97f), Art.Rounded, "bag"); q.raycastTarget = true; var qt = q.rectTransform; qt.anchorMin = qt.anchorMax = new Vector2(0.5f, 0.5f); qt.pivot = new Vector2(0, 0.5f); qt.anchoredPosition = new Vector2(-total / 2 + W + 24, 0); qt.sizeDelta = new Vector2(BWp, H);
        var hd2 = UiKit.Img(qt, new Color(1, 1, 1, 0.05f), Art.Rounded, "hdr"); hd2.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(10, -76), new Vector2(-10, -10));
        bagHead = UiKit.Txt(qt, "背包", 32, Color.white, TextAnchor.MiddleLeft, true); bagHead.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(24, -76), new Vector2(-24, -10));
        bagGrid = UiKit.Node(qt, "grid"); bagGrid.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(24, -96 - bRows * (BW + BG)), new Vector2(-24, -96));
        var tb = UiKit.Img(qt, new Color(1, 1, 1, 0.05f), Art.Rounded, "tot"); tb.rectTransform.Place(new Vector2(0, 0), new Vector2(1, 0), new Vector2(14, 22), new Vector2(-14, 82));
        bagTotal = UiKit.Txt(tb.transform, "", 28, Accent, TextAnchor.MiddleCenter, true); bagTotal.rectTransform.Fill();
    }
    public void CloseLoot() { lootP.gameObject.SetActive(false); lc = null; }
    static float Pop(float x) { if (x >= 1) return 1; if (x < 0.55f) return Mathf.Lerp(0.4f, 1.12f, Mathf.SmoothStep(0, 1, x / 0.55f)); return Mathf.Lerp(1.12f, 1f, Mathf.SmoothStep(0, 1, (x - 0.55f) / 0.45f)); }
    public void UpdateLoot(float luckMul, List<Item> bag, int cap)
    {
        if (lc == null) return; float t = Time.time; int left = 0; foreach (var s in lc.slots) if (s.state < 2) left++;
        lootInfo.text = (autoTake ? "<color=#3f9a6a>自動拿取中…</color>　" : "") + (left > 0 ? "翻找中 " + (lc.slots.Count - left) + "/" + lc.slots.Count : "<color=#3f9a6a>已搜完</color>");
        for (int i = 0; i < cells.Count && i < lc.slots.Count; i++)
        {
            var s = lc.slots[i]; var c = cells[i]; var rc = Data.RarCol[s.it.r]; string hex = ColorUtility.ToHtmlStringRGB(rc);
            c.content.localScale = Vector3.one; c.flash.color = new Color(1, 1, 1, 0); c.glow.color = new Color(1, 1, 1, 0);
            if (s.state == 0)
            {
                c.ring.enabled = true; c.ring.color = new Color(1, 1, 1, 0.08f); c.spin.enabled = false; c.q.enabled = true; c.icon.enabled = false; c.chip.enabled = false;
                c.name.text = "<color=#7a6a78>未搜索</color>"; c.val.text = ""; c.bar.enabled = false; c.bg.color = Panel2; c.edge.effectColor = new Color(1, 1, 1, 0.06f);
            }
            else if (s.state == 1)
            {
                float k = Mathf.Clamp01(s.p / Mathf.Max(0.01f, s.dur));
                c.ring.enabled = true; c.ring.color = new Color(rc.r, rc.g, rc.b, 0.15f); c.spin.enabled = true; c.spin.color = rc; c.spin.transform.localRotation = Quaternion.Euler(0, 0, -t * (360 + 240 * s.it.r)); c.q.enabled = true; c.q.color = new Color(rc.r, rc.g, rc.b, 0.5f + 0.3f * Mathf.Sin(t * 8));
                c.icon.enabled = false; c.chip.enabled = false; c.name.text = "翻翻翻…"; c.val.text = s.it.paid ? "<color=#c95a5a>已付費</color>" : "";
                c.bar.enabled = true; c.bar.fillAmount = k; c.bar.color = rc; c.bg.color = Color.Lerp(Panel2, rc * 0.45f, 0.18f + 0.1f * Mathf.Sin(t * 10)); c.edge.effectColor = new Color(rc.r, rc.g, rc.b, 0.5f + 0.4f * Mathf.Sin(t * 10));
                c.glow.color = new Color(rc.r, rc.g, rc.b, 0.12f + 0.08f * s.it.r * k);
            }
            else
            {
                c.ring.enabled = false; c.spin.enabled = false; c.q.enabled = false; c.bar.enabled = false; c.icon.enabled = true; if (!c.icon.sprite) c.icon.sprite = ItemArt.Pic(s.it);
                if (s.state == 2)
                {
                    float x = (t - s.p) / 0.42f; c.content.localScale = Vector3.one * Pop(x);
                    c.flash.color = new Color(1, 1, 1, Mathf.Clamp01(1 - (t - s.p) / 0.35f) * (0.35f + 0.12f * s.it.r));
                    float gl = s.it.r >= 2 ? 0.28f + 0.1f * s.it.r + 0.12f * Mathf.Sin(t * 4) : s.it.r == 1 ? 0.18f : 0.06f; c.glow.color = new Color(rc.r, rc.g, rc.b, gl);
                    c.bg.color = Color.Lerp(Panel2, rc, 0.2f); c.edge.effectColor = rc; c.icon.color = Color.white;
                    c.chip.enabled = true; c.chip.color = rc; c.chipT.text = Data.RarName[s.it.r];
                    c.name.text = "<color=#" + hex + ">" + s.it.name + "</color>"; c.val.text = s.it.key ? "<color=#3a78c9>鑰匙</color>" : Data.Fmt(s.it.v);
                }
                else
                {
                    c.bg.color = new Color(0.16f, 0.19f, 0.34f, 0.9f); c.edge.effectColor = new Color(0, 0, 0, 0); c.chip.enabled = false; c.icon.color = new Color(1, 1, 1, 0.16f);
                    c.name.text = "<color=#6b737b>已拿取</color>"; c.val.text = "";
                }
            }
        }
        // backpack
        int sig = bag.Count * 1000 + cap; foreach (var b in bag) sig = sig * 31 + b.v;
        if (sig != bagSig)
        {
            bagSig = sig; UiKit.Clear(bagGrid); var items = bag.FindAll(b => !b.key); long tot = 0;
            for (int i = 0; i < Mathf.Max(cap, items.Count); i++)
            {
                int r = i / bCols, col = i % bCols; var pos = new Vector2(col * (BW + BG), -r * (BW + BG));
                if (i < items.Count) { var it = items[i]; tot += it.v; var ic = ItemIcon(bagGrid, it, pos, BW); }
                else { var e = UiKit.Img(bagGrid, new Color(1, 1, 1, 0.045f), Art.Rounded, "empty"); var er = e.rectTransform; er.anchorMin = er.anchorMax = new Vector2(0, 1); er.pivot = new Vector2(0, 1); er.anchoredPosition = pos; er.sizeDelta = new Vector2(BW, BW); }
            }
            int keys = bag.Count - items.Count;
            bagHead.text = "背包　<color=#" + (items.Count >= cap ? "ff6a5a" : "ffffff") + ">" + items.Count + "</color><size=24> / " + cap + "</size>" + (keys > 0 ? "<size=22>　鑰匙×" + keys + "</size>" : "");
            bagTotal.text = "<size=22><color=#7a6a78>總價值</color></size>　" + Data.Fmt(tot);
        }
    }

    // ---------- modal ----------
    public RectTransform Modal(string title, float w = 1300, float h = 820)
    {
        UiKit.Clear(modal); modal.gameObject.SetActive(true);
        var dim = UiKit.Img(modal, new Color(0, 0, 0, 0.55f), null, "dim"); dim.rectTransform.Fill(); dim.raycastTarget = true;
        var p = UiKit.Img(modal, Panel, Art.Rounded, "panel"); p.rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(w, h)); p.rectTransform.pivot = new Vector2(0.5f, 0.5f); p.rectTransform.anchoredPosition = Vector2.zero; p.raycastTarget = true; p.gameObject.AddComponent<Fit>();
        var t = UiKit.Txt(p.transform, title, 44, Color.white, TextAnchor.UpperLeft, true); t.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(36, -80), new Vector2(-36, -22));
        var m = UiKit.Txt(p.transform, Data.Fmt(SaveSys.S.money), 32, Accent, TextAnchor.UpperRight, true); m.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(36, -80), new Vector2(-180, -26));
        var x = UiKit.Btn(p.transform, TouchIn.Active ? "關閉" : "關閉 [Esc]", () => Game.I.CloseModal(), Data.H("3a3f46"), 22); ((RectTransform)x.transform).At(new Vector2(1, 1), new Vector2(-24, -22), new Vector2(140, 48));
        var c = UiKit.Node(p.transform, "content"); c.Place(Vector2.zero, Vector2.one, new Vector2(36, 30), new Vector2(-36, -100));
        return c;
    }
    public void CloseModal() { modal.gameObject.SetActive(false); }

    public RectTransform Card(Transform parent, Vector2 pos, Vector2 size, Color c)
    {
        var i = UiKit.Img(parent, c, Art.Rounded, "card"); i.rectTransform.anchorMin = i.rectTransform.anchorMax = new Vector2(0, 1); i.rectTransform.pivot = new Vector2(0, 1); i.rectTransform.anchoredPosition = pos; i.rectTransform.sizeDelta = size; return i.rectTransform;
    }
    public Image ItemIcon(Transform parent, Item it, Vector2 pos, float s)
    {
        var bg = UiKit.Img(parent, Data.RarCol[it.r] * new Color(1, 1, 1, 0.25f), Art.Rounded, "ibg"); var r = bg.rectTransform; r.anchorMin = r.anchorMax = new Vector2(0, 1); r.pivot = new Vector2(0, 1); r.anchoredPosition = pos; r.sizeDelta = new Vector2(s, s);
        var ring = UiKit.Img(r, Data.RarCol[it.r], Art.Ring, "ring"); ring.rectTransform.Fill(2);
        var ic = UiKit.Img(r, Color.white, ItemArt.Pic(it), "icon"); ic.rectTransform.Fill(s * 0.08f); ic.preserveAspect = true; return bg;
    }

    // ---------- end screens ----------
    public RectTransform End(string title, Color tc)
    {
        UiKit.Clear(endP); endP.gameObject.SetActive(true);
        var dim = UiKit.Img(endP, new Color(0.16f, 0.2f, 0.38f, 0.9f), null, "dim"); dim.rectTransform.Fill(); dim.raycastTarget = true;
        var hold = UiKit.Node(endP, "fit"); hold.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1600, 1080)); hold.pivot = new Vector2(0.5f, 0.5f); hold.anchoredPosition = Vector2.zero; hold.gameObject.AddComponent<Fit>();
        var t = UiKit.Shadowed(UiKit.Txt(hold, title, 84, tc, TextAnchor.MiddleCenter, true)); t.rectTransform.At(new Vector2(0.5f, 1), new Vector2(0, -60), new Vector2(1600, 120));
        var c = UiKit.Node(hold, "content"); c.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 1), new Vector2(-700, 150), new Vector2(700, -200));
        var b = UiKit.Btn(hold, TouchIn.Active ? "回到基地" : "回到基地 [空白鍵]", () => Game.I.BackToBase(), Data.H("2f7d4a"), 30); ((RectTransform)b.transform).At(new Vector2(0.5f, 0), new Vector2(0, 50), new Vector2(380, 74));
        return c;
    }
    public void CloseEnd() { endP.gameObject.SetActive(false); }
    public void Cine(bool on) { cineP.gameObject.SetActive(on); if (on) { cineTitle.text = ""; cineSub.text = ""; } }
    public void CineText(string title, string sub) { if (cineTitle.text != title) { cineTitle.text = title; if (title == "回家囉！") Sfx.Play("sting", 0.8f); } cineSub.text = sub; cineTitle.color = new Color(1, 1, 1, 0.95f); }
}

/// scales a fixed-size panel down so it always fits inside its parent (portrait phones, small windows)
public class Fit : MonoBehaviour
{
    void LateUpdate()
    {
        var r = (RectTransform)transform; var pr = r.parent as RectTransform; if (!pr || r.rect.width < 1 || r.rect.height < 1) return;
        float k = Mathf.Min(1f, pr.rect.width * 0.97f / r.rect.width, pr.rect.height * 0.95f / r.rect.height); r.localScale = new Vector3(k, k, 1);
    }
}
