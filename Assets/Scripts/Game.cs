using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// CASUAL: kid sneaks out to 阿姨家 / 便利商店 / 學校 / 阿嬤家, loots snacks & treasures, dodges cats & dogs, takes the bus home before mom calls.
public class Game : MonoBehaviour
{
    static readonly Color Accent = Data.H("ffb347"), Good = Data.H("7dffa0"), Dim = new Color(1, 1, 1, 0.7f);
    public static Game I; public UI ui; public Player player; public Level level; Transform baseRoot;
    public enum St { Base, Raid, Cine, End } public St state;
    public List<Item> bag = new List<Item>(); public int keys; public float runT; public int searched, caughtRun, luresRun; bool warn60, warn20;
    LootContainer openC; Interactable focus; float tickT;
    public const float RaidTime = Data.RaidTime;
    public bool autoshots; string shotDir;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot() { if (I) return; var g = new GameObject("Game"); DontDestroyOnLoad(g); g.AddComponent<Game>(); }

    void Awake()
    {
        I = this; UIContrast.Ensure(); Application.targetFrameRate = 120; QualitySettings.vSyncCount = 1;
        var args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++) if (args[i] == "-autoshots") { autoshots = true; shotDir = i + 1 < args.Length ? args[i + 1] : "shots"; }
        if (autoshots) { SaveSys.Key = "casual_autoshot"; PlayerPrefs.DeleteKey(SaveSys.Key); }
        Art.Init(); gameObject.AddComponent<Sfx>();
        SaveSys.Load();
        ui = gameObject.AddComponent<UI>(); ui.Build();
        player = Player.Create();
        if (SaveSys.S.inRaid)
        {
            SaveSys.S.inRaid = false; SaveSys.S.seedN++; SaveSys.Save();
            StartCoroutine(Later(0.5f, () => Toast("上次偷溜出去沒回家…這次要記得坐公車喔！", 4)));
        }
        if (SaveSys.S.quest == null || string.IsNullOrEmpty(SaveSys.S.quest.t)) { SaveSys.S.quest = Data.MakeQuest(SaveSys.S.questN); SaveSys.S.hasQuest = true; }
        EnterBase(true);
        if (autoshots) StartCoroutine(AutoShotsCasual());
    }
    IEnumerator Later(float t, System.Action a) { yield return new WaitForSeconds(t); a(); }

    // ---------- state helpers ----------
    public bool InputLocked => ui.ModalOpen || ui.LootOpen || ui.EndOpen || state == St.Cine || state == St.End;
    public bool RaidActive => state == St.Raid;
    public float TimeLeft => RaidTime - runT;
    public bool CanBoard => state == St.Raid && level != null && level.bus && level.bus.CanBoard;
    public int BagCap => Data.GearOf("bag", SaveSys.S.bag).cap;
    public float LuckMul => Data.Upg[0].eff[SaveSys.S.luck];
    public void Toast(string s, float d = 2.6f) { ui.Toast(s, d); }

    void EnvBase() => Art.SetupEnvironment(Data.H("bfe6ff"), Data.H("ffe9c7"), 1.5f, new Color(1f, 0.93f, 0.8f), new Vector3(40, -30, 0), 30, 140, 0.0f, 0.5f, 0.45f, 6, new Color(1f, 0.98f, 0.95f));
    void EnvTown(ThemeDef th) => Art.SetupEnvironment(th.sky, Data.H("a6e08a"), 2.0f, new Color(1f, 0.96f, 0.85f), new Vector3(45, -35, 0), 60, 260, 0.0f, 0.5f, 0.45f, 10, new Color(1f, 1f, 0.98f));

    void EnterBase(bool first = false)
    {
        state = St.Base; if (level != null) { Destroy(level.root.gameObject); level = null; }
        Pet.All.Clear(); foreach (var l in Lure.All.ToList()) if (l) Destroy(l.gameObject);
        if (baseRoot) Destroy(baseRoot.gameObject);
        baseRoot = BaseHub.Build(out var sp, out var yaw); EnvBase();
        player.inRaid = false; player.alive = true; player.hp = 100; player.controlEnabled = true; player.enabled = true; player.cryT = 0;
        ReattachCam(); player.Equip(SaveSys.S.shoes); player.Teleport(sp, yaw);
        Sfx.Engine(0); ui.CloseEnd(); ui.Cine(false); ui.CloseLoot(); ui.CloseModal();
        if (!first) Toast("回到房間了！熊熊在等你說今天的冒險。");
    }
    void ReattachCam()
    {
        var c = player.cam.transform; c.SetParent(player.camRig, false); c.localPosition = Vector3.zero; c.localRotation = Quaternion.identity; player.cam.fieldOfView = 75; player.vmCam.enabled = true;
        player.camRig.localPosition = new Vector3(0, 1.25f, 0);
    }

    // ---------- deploy ----------
    bool deploying;
    public void Deploy(int themeIdx)
    {
        if (state != St.Base || deploying) return;
        var S = SaveSys.S; var th = Data.Themes[themeIdx]; if (!th.playable) { Toast("這裡還在施工中，下次再來玩！"); Sfx.Play("error_004"); return; }
        S.inRaid = true; S.stats.raids++; SaveSys.Save();
        deploying = true; ui.CloseModal(); StartCoroutine(DeployCo(th));
    }
    IEnumerator DeployCo(ThemeDef th)
    {
        for (float t = 0; t < 1; t += Time.deltaTime * 3) { ui.Fade(t); yield return null; }
        ui.Fade(1); Sfx.Play("whoosh", 0.7f); yield return null;
        if (baseRoot) Destroy(baseRoot.gameObject); Interactable.All.RemoveAll(x => x == null);
        int seed = SaveSys.S.seedN * 7919 + 17;
        Pet.All.Clear();
        level = LevelGen.BuildHouse(seed, th, th.mult, SaveSys.S.luck); EnvTown(th);
        bag.Clear(); keys = 0; runT = 0; searched = 0; caughtRun = 0; luresRun = 0; warn60 = warn20 = false;
        player.Equip(SaveSys.S.shoes); player.hp = 100; player.alive = true; player.inRaid = true; player.caught = 0; player.cryT = 0; player.Teleport(level.spawn, level.spawnYaw);
        state = St.Raid; deploying = false;
        yield return null;
        for (float t = 1; t > 0; t -= Time.deltaTime * 1.5f) { ui.Fade(t); yield return null; }
        ui.Fade(0); Toast("<b>" + th.name + "</b>　" + th.twist, 4); Sfx.Play("sting", 0.5f);
    }

    // ---------- update ----------
    void Update()
    {
        float dt = Time.deltaTime; var S = SaveSys.S;
        bool wantLock = !InputLocked && !autoshots;
        if (!autoshots) { Cursor.lockState = wantLock ? CursorLockMode.Locked : CursorLockMode.None; Cursor.visible = !wantLock; }
        string prompt = null, hint = null;
        if (state == St.Raid)
        {
            runT += dt; float tl = TimeLeft;
            if (!warn60 && tl <= 60) { warn60 = true; Toast("<color=#d97a2a>媽媽傳訊息：「一分鐘內給我回家！」最後一班公車會一直等你喔。</color>", 3.5f); Sfx.Play("alarm", 0.5f); }
            if (!warn20 && tl <= 20) { warn20 = true; Toast("<color=#ff6a5a>媽媽打電話來了！20 秒內快上公車！</color>", 2.5f); }
            if (tl <= 0 && player.alive) Fail("mom");
        }
        // interaction focus
        focus = null; HintRing.focused = null;
        if ((state == St.Raid || state == St.Base) && !InputLocked && player.alive)
        {
            var cam = player.cam.transform; float best = 999;
            // 1) whatever is under the crosshair wins (sphere cast, triggers included)
            var hits = Physics.SphereCastAll(cam.position, 0.2f, cam.forward, 3.4f, ~0, QueryTriggerInteraction.Collide);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var h in hits)
            {
                if (h.collider.transform.IsChildOf(player.transform)) continue;
                var it = h.collider.GetComponentInParent<Interactable>();
                if (it && it.isActiveAndEnabled && it.Dist(cam.position) <= it.reach + 0.3f) { focus = it; break; }
                if (!h.collider.isTrigger && h.distance > 0.05f)
                {   // decor mesh wrapped around a container (shelf frame, counter...): the container whose bounds hold the hit point wins
                    var hp = h.point == Vector3.zero ? cam.position + cam.forward * h.distance : h.point; float bd = 9;
                    foreach (var o in Interactable.All) { if (!o || !o.isActiveAndEnabled) continue; var ob = o.WorldBounds; ob.Expand(0.35f); if (!ob.Contains(hp)) continue; float od = o.Dist(cam.position); if (od <= o.reach + 0.3f && od < bd) { bd = od; focus = o; } }
                    break;
                }
            }
            // 2) otherwise: ~60° cone, ranked mostly by how close to screen centre
            if (!focus) foreach (var it in Interactable.All)
            {
                if (!it || !it.isActiveAndEnabled) continue; float dist = it.Dist(cam.position); if (dist > it.reach) continue;
                var p = it.Point; var d = p - cam.position; float ang = Vector3.Angle(cam.forward, d);
                var b = it.WorldBounds; float angB = Mathf.Min(ang, Vector3.Angle(cam.forward, b.ClosestPoint(cam.position + cam.forward * Mathf.Max(0.5f, d.magnitude)) - cam.position));
                if (angB > 34 && dist > 0.8f) continue;
                {   // line of sight to the object's near side (not its pivot, which can sit inside a wall)
                    var tp = Vector3.Lerp(b.ClosestPoint(cam.position), b.center, 0.3f); var td = tp - cam.position;
                    if (Physics.Raycast(cam.position, td.normalized, out var hh, td.magnitude, Art.ShotMask, QueryTriggerInteraction.Ignore) && !hh.collider.transform.IsChildOf(it.transform) && hh.collider.GetComponentInParent<Interactable>() != it) { var eb = b; eb.Expand(0.6f); if (!eb.Contains(hh.point)) continue; }
                }
                float score = angB + dist * 5; if (score < best) { best = score; focus = it; }
            }
            HintRing.focused = focus;
            if (focus) prompt = focus.Prompt;
            if (focus && Input.GetKeyDown(KeyCode.E)) { focus.Use(); }
            hint = state == St.Base ? "WASD 走路　滑鼠 看看　E 互動" : "WASD 走路　Shift 跑（會吵到狗狗）　1/2 或滾輪 換零食　左鍵 丟零食　E 互動";
        }
        // loot
        if (ui.LootOpen && openC)
        {
            if ((Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Escape)) && Time.frameCount != openFrame) CloseLoot();
            else
            {
                if (Input.GetKeyDown(KeyCode.F)) LootAll(); if (Input.GetKeyDown(KeyCode.Q)) PaySkip();
                StepLoot(dt);
            }
            ui.UpdateLoot(LuckMul, bag, BagCap);
            if (openC && (openC.transform.position - player.transform.position).magnitude > openC.reach + 2.5f) CloseLoot();
        }
        if (ui.ModalOpen && Input.GetKeyDown(KeyCode.Escape)) CloseModal();
        if (state == St.End && Input.GetKeyDown(KeyCode.Space)) BackToBase();
        ui.Hud(state == St.Raid, player, TimeLeft, Objective(), bag, BagCap, S.money, ui.LootOpen || ui.ModalOpen ? null : prompt, ui.LootOpen ? "點一下亮起來的東西拿走　F 全部拿　Q 花錢加速　E 關閉" : hint);
    }
    string Objective()
    {
        var b = level != null ? level.bus : null; if (!b) return "";
        if (b.CanBoard) return "公車到站了！快去公車站（E 上車）";
        if (TimeLeft <= 63) return "最後一班公車要來了，快去公車站！";
        return "找點心和寶貝・小心貓咪狗狗・下一班公車 " + Mathf.Max(0, Mathf.CeilToInt(b.NextIn)) + " 秒";
    }

    // ---------- loot ----------
    int openFrame;
    public void OpenLoot(LootContainer c)
    {
        if (state != St.Raid) return; openC = c; openFrame = Time.frameCount; if (!c.opened) { c.opened = true; searched++; }
        ui.OpenLoot(c); Sfx.Play("impactPlank_medium_000", 0.5f, 1.2f);
    }
    public void CloseLoot() { if (openC && openC.Empty) openC.MarkEmpty(); openC = null; ui.CloseLoot(); }
    void StepLoot(float dt)
    {
        LootSlot cur = null; foreach (var s in openC.slots) if (s.state == 1) { cur = s; break; }
        if (cur == null) foreach (var s in openC.slots) if (s.state == 0) { cur = s; s.state = 1; s.p = 0; s.dur = Data.RarSpin[s.it.r] * LuckMul * (autoshots ? 0.6f : 1); break; }
        if (cur == null) { if (ui.autoTake) for (int i = 0; i < openC.slots.Count; i++) { if (openC.slots[i].state == 2) { TakeSlot(i); if (!ui.autoTake) break; } } return; }
        cur.p += dt; tickT -= dt; if (tickT <= 0) { tickT = 0.11f; Sfx.Play("tick", 0.25f, 0.9f + cur.p / cur.dur * 0.5f); }
        if (cur.p >= cur.dur) { cur.state = 2; cur.p = Time.time; Sfx.Reveal(cur.it.r); if (cur.it.r >= 3) player.Shake(0.4f); if (ui.autoTake) TakeSlot(openC.slots.IndexOf(cur)); }
    }
    public void TakeSlot(int i)
    {
        if (!openC || i < 0 || i >= openC.slots.Count) return; var s = openC.slots[i]; if (s.state != 2) return;
        if (Pickup(s.it)) { s.state = 3; if (openC.Empty) { openC.MarkEmpty(); } }
    }
    public bool Pickup(Item it)
    {
        if (it.key) { keys++; Sfx.Play("confirmation_001", 0.7f); Toast("撿到 <color=#3a78c9>神秘鑰匙</color>！好像可以打開某扇門。"); return true; }
        if (bag.Count(b => !b.key) >= BagCap) { Toast("<color=#ff6a5a>袋子裝滿了！</color>（" + BagCap + " 格）回房間可以換大一點的袋子。"); Sfx.Play("error_004", 0.6f); if (ui.autoTake) ui.autoTake = false; return false; }
        bag.Add(it); Sfx.Play("cash", 0.45f, 0.9f + it.r * 0.08f); return true;
    }
    public void LootAll() { if (!openC) return; ui.autoTake = true; for (int i = 0; i < openC.slots.Count; i++) if (openC.slots[i].state == 2) { TakeSlot(i); if (!ui.autoTake) break; } }
    public void PaySkip()
    {
        if (!openC) return; var s = openC.slots.FirstOrDefault(x => x.state == 1); if (s == null || s.it.paid) return;
        if (!s.it.key) s.it.v = Mathf.Max(1, s.it.v / 2); s.it.paid = true; s.p = s.dur; Sfx.Play("cash", 0.5f, 0.7f); Toast("花錢加速：這樣東西只值一半了喔", 1.3f);
    }
    public bool UseKey() { if (keys <= 0) return false; keys--; Sfx.Play("confirmation_001"); return true; }
    // ---------- pet / lure hooks ----------
    public Item StealOne()
    {
        caughtRun++; SaveSys.S.stats.caught++; if (openC) CloseLoot();
        var c = bag.Where(i => !i.key).ToList(); if (c.Count == 0) return null;
        var it = c[Random.Range(0, c.Count)]; bag.Remove(it); return it;
    }
    public void OnLureLanded(Lure l) { Pet.Noise(l.transform.position, 4); }
    public void OnLureEaten() { luresRun++; SaveSys.S.stats.lures++; }

    // ---------- settle ----------
    class Info { public long sold, bonus; public List<Item> kept = new List<Item>(); public bool quest; }
    long AddToStash(IEnumerable<Item> items) { var S = SaveSys.S; long sold = 0; foreach (var it in items) { if (it.key) continue; var o = new Item { r = it.r, ic = it.ic, name = it.name, v = it.v }; if (S.stash.Count < S.StashCap) S.stash.Add(o); else sold += Mathf.RoundToInt(it.v * 0.5f); } S.money += sold; return sold; }
    long BagValue => bag.Where(b => !b.key).Sum(b => (long)b.v);
    bool CheckQuest(bool ok)
    {
        var q = SaveSys.S.quest; if (q == null || q.done || !ok) return false; var items = bag.Where(i => !i.key).ToList(); bool r = false;
        switch (q.type) { case "theme": r = level != null && q.theme >= 0 && q.theme < Data.Themes.Length && level.theme == Data.Themes[q.theme]; break; case "rare": r = items.Count(i => i.r >= q.r) >= q.n; break; case "lure": r = luresRun >= q.n; break; case "speed": r = runT <= q.s; break; case "value": r = BagValue >= q.v; break; case "clean": r = caughtRun == 0; break; }
        if (r) q.done = true; return r;
    }
    Info Settle(bool ok)
    {
        var S = SaveSys.S; var info = new Info();
        if (ok) { long bv0 = BagValue; info.bonus = 100 + bv0 / 5 + (caughtRun == 0 ? 150 : 0); S.money += info.bonus; Debug.Log("[casual] run earnings bag=" + bv0 + " bonus=" + info.bonus + " items=" + bag.Count(i => !i.key)); info.sold = AddToStash(bag); S.stats.extracts++; S.stats.best = (int)Mathf.Max(S.stats.best, BagValue); S.stats.earned += BagValue; }
        else { int N = (int)Data.Upg[3].eff[S.secure]; info.kept = bag.Where(i => !i.key).OrderByDescending(i => i.v).Take(N).ToList(); info.sold = AddToStash(info.kept); S.stats.deaths++; }
        info.quest = CheckQuest(ok); S.inRaid = false; S.seedN++; SaveSys.Save(); return info;
    }

    /// mom called: you are brought home, only the pocket (secure upgrade) items survive
    public void Fail(string reason)
    {
        if (state != St.Raid) return; state = St.End; player.alive = false; if (openC) CloseLoot();
        var lostItems = bag.Where(b => !b.key).ToList(); var info = Settle(false); lostItems.RemoveAll(i => info.kept.Contains(i)); long lost = lostItems.Sum(i => (long)i.v); int n = lostItems.Count;
        StartCoroutine(FailCo(lost, n, info));
    }
    IEnumerator FailCo(long lost, int n, Info info)
    {
        Sfx.Play("fail", 0.7f);
        for (float tt = 0; tt < 1.0f; tt += Time.deltaTime) { ui.Fade(tt * 0.6f); yield return null; }
        ui.Fade(0);
        var c = ui.End("媽媽來接你了…", Data.H("ff8a8a"));
        var sb = new System.Text.StringBuilder(); sb.AppendLine("電話響了：「你跑去哪裡了？！」媽媽騎機車把你載回家。"); sb.AppendLine();
        sb.AppendLine("<size=64><color=#c9505a>袋子裡 " + n + " 樣東西被沒收了</color></size>");
        sb.AppendLine("（價值 " + Data.Fmt(lost) + "，媽媽說要「先幫你保管」）");
        if (info.kept.Count > 0) sb.AppendLine("<color=#4a9a5a>口袋裡偷偷藏住：" + string.Join("、", info.kept.Select(i => i.name)) + "</color>");
        else sb.AppendLine("<size=24>小提示：在勞作桌升級「口袋」，被叫回家也能留下東西。下次記得坐公車！</size>");
        sb.AppendLine(); sb.AppendLine("<size=24>按空白鍵回房間</size>");
        var t = UiKit.Txt(c, sb.ToString(), 30, Color.white); t.rectTransform.Fill();
    }

    public void Extract()
    {
        if (state != St.Raid) return; if (openC) CloseLoot();
        var info = Settle(true); state = St.Cine; ui.Cine(true); player.controlEnabled = false;
        StartCoroutine(ExtractCo(info));
    }
    IEnumerator ExtractCo(Info info)
    {
        yield return new WaitForSeconds(0.4f);
        player.vmCam.enabled = false; var cam = player.cam; cam.transform.SetParent(null, true);
        bool skip = false;
        yield return level.bus.Cinematic(cam, ui.CineText, () => skip = skip || (!autoshots && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(0))));
        Sfx.Engine(0); ui.Cine(false); state = St.End; ShowExtract(info);
    }
    void ShowExtract(Info info)
    {
        var c = ui.End("平安回家囉！", Data.H("7dffa0")); var items = bag.Where(i => !i.key).OrderByDescending(i => i.r).ThenByDescending(i => i.v).ToList();
        var v = UiKit.Txt(c, "<size=26>今天帶回來的寶貝</size>\n<size=80><color=#d97a2a>" + Data.Fmt(BagValue) + "</color></size>", 30, Color.white, TextAnchor.UpperCenter, true); v.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -150), new Vector2(0, 0));
        float s = 92, gap = 10; int per = 12; float x0 = (1400 - per * (s + gap)) / 2;
        for (int i = 0; i < items.Count && i < 36; i++) ui.ItemIcon(c, items[i], new Vector2(x0 + (i % per) * (s + gap), -170 - (i / per) * (s + gap)), s);
        int fs = items.Count(i => i.r == 4);
        var sb = new System.Text.StringBuilder();
        sb.Append("翻了 " + searched + " 個地方　·　被抓 " + caughtRun + " 次　·　餵了 " + luresRun + " 次零食　·　用時 " + Mathf.FloorToInt(runT / 60) + ":" + ((int)runT % 60).ToString("00"));
        if (fs > 0) sb.Append("　·　傳說寶貝 " + fs + " 個！");
        if (info.sold > 0) sb.Append("\n<color=#c95a5a>玩具箱滿了，" + Data.Fmt(info.sold) + " 的東西半價賣掉了。</color>");
        sb.Append("\n<color=#3f9a6a>回家獎勵 +" + Data.Fmt(info.bonus) + (caughtRun == 0 ? "（沒被抓到，額外加碼！）" : "") + "</color>");
        if (info.quest) sb.Append("\n<color=#c98a1a>任務完成！去找熊熊領獎勵。</color>");
        sb.Append("\n<size=22>按空白鍵回房間</size>");
        var t = UiKit.Txt(c, sb.ToString(), 26, new Color(1, 1, 1, 0.9f), TextAnchor.LowerCenter); t.rectTransform.Place(Vector2.zero, new Vector2(1, 0), new Vector2(0, 0), new Vector2(0, 140));
        Sfx.Play("cash", 0.8f);
    }
    public void BackToBase() { if (state != St.End) return; StartCoroutine(BackCo()); }
    IEnumerator BackCo()
    {
        state = St.Base; for (float t = 0; t < 1; t += Time.deltaTime * 3) { ui.Fade(t); yield return null; }
        EnterBase(); yield return null; for (float t = 1; t > 0; t -= Time.deltaTime * 2) { ui.Fade(t); yield return null; } ui.Fade(0);
    }

    // ---------- base stations ----------
    public void CloseModal() { ui.CloseModal(); }
    public void OpenStation(string id)
    {
        if (state != St.Base || deploying) return; Sfx.Play("click_002", 0.6f);
        switch (id) { case "deploy": MenuDeploy(); break; case "stash": MenuStash(); break; case "shop": MenuShop(); break; case "upg": MenuUpg(); break; case "quest": MenuQuest(); break; }
    }
    void MenuDeploy()
    {
        var c = ui.Modal("書桌上的地圖：今天去哪裡玩？", 1500, 820); var S = SaveSys.S;
        for (int i = 0; i < Data.Themes.Length; i++)
        {
            var th = Data.Themes[i]; int idx = i; var card = ui.Card(c, new Vector2(i * 286, 0), new Vector2(270, 520), th.playable ? new Color(th.sky.r * 0.75f, th.sky.g * 0.7f, th.sky.b * 0.85f, 1) : new Color(0.55f, 0.55f, 0.6f, 1));
            UiKit.Txt(card, th.name, 34, Color.white, TextAnchor.UpperCenter, true).rectTransform.Place(new Vector2(0, 1), Vector2.one, new Vector2(10, -70), new Vector2(-10, -20));
            UiKit.Txt(card, th.playable ? "冒險等級 " + new string('★', th.tier) + "\n寶貝價值 ×" + th.mult.ToString("0.00") + "\n貓狗 " + th.enemies + " 隻" : "施工中", 22, Data.H("fff2a8"), TextAnchor.UpperCenter).rectTransform.Place(new Vector2(0, 1), Vector2.one, new Vector2(10, -180), new Vector2(-10, -80));
            UiKit.Txt(card, th.twist, 21, Color.white, TextAnchor.UpperCenter).rectTransform.Place(new Vector2(0, 0), Vector2.one, new Vector2(16, 110), new Vector2(-16, -190));
            var b = UiKit.Btn(card, th.playable ? "出發！" : "施工中", () => Deploy(idx), th.playable ? Data.H("3fae5a") : Data.H("8a8f99"), 28); ((RectTransform)b.transform).Place(new Vector2(0, 0), new Vector2(1, 0), new Vector2(20, 24), new Vector2(-20, 90));
        }
        var lo = "穿著：" + Data.GearName(S.shoes) + "　/　" + Data.GearName(S.bag) + "　　零食：罐頭 ×" + S.cans + "　肉肉 ×" + S.meats + "　　<size=20>（媽媽 4 分鐘後會叫你回家，記得坐公車！）</size>";
        UiKit.Txt(c, lo, 26, Color.white, TextAnchor.LowerLeft).rectTransform.Place(Vector2.zero, new Vector2(1, 0), new Vector2(0, 50), new Vector2(0, 100));
        UiKit.Txt(c, "任務：" + (S.quest != null ? S.quest.t + (S.quest.done ? "　<color=#3f9a6a>完成了！</color>" : "") : "—"), 24, Accent, TextAnchor.LowerLeft).rectTransform.Place(Vector2.zero, new Vector2(1, 0), Vector2.zero, new Vector2(0, 44));
    }
    void MenuStash()
    {
        var S = SaveSys.S; var c = ui.Modal("玩具箱　" + S.stash.Count + "/" + S.StashCap + "　　展示架 " + S.trophies.Count + "/" + Data.Upg[2].eff[S.shelf], 1500, 900);
        float s = 96, gx = 118, gy = 150; int per = 12;
        if (S.stash.Count > 48) Toast("只顯示前 48 樣（共 " + S.stash.Count + "），先賣掉一些吧。");
        for (int i = 0; i < S.stash.Count && i < 48; i++)
        {
            var it = S.stash[i]; int idx = i; var pos = new Vector2((i % per) * gx, -(i / per) * gy);
            ui.ItemIcon(c, it, pos, s);
            var sell = UiKit.Btn(c, "賣 " + Data.Fmt(it.v), () => { SellAt(idx); }, Data.H("2f5d7d"), 15); var r = (RectTransform)sell.transform; r.anchorMin = r.anchorMax = new Vector2(0, 1); r.pivot = new Vector2(0, 1); r.anchoredPosition = pos + new Vector2(0, -s - 4); r.sizeDelta = new Vector2(it.r >= 3 ? 64 : s, 34);
            if (it.r >= 3) { var sh = UiKit.Btn(c, "展", () => ShowTrophy(idx), Data.H("6a3d8a"), 16); var r2 = (RectTransform)sh.transform; r2.anchorMin = r2.anchorMax = new Vector2(0, 1); r2.pivot = new Vector2(0, 1); r2.anchoredPosition = pos + new Vector2(68, -s - 4); r2.sizeDelta = new Vector2(28, 34); }
        }
        if (S.stash.Count == 0) UiKit.Txt(c, "玩具箱空空的，出去找寶貝吧！", 30, Dim).rectTransform.Fill();
        long cv = S.stash.Where(i => i.r <= 1).Sum(i => (long)i.v);
        var b = UiKit.Btn(c, "賣掉普通/不錯的（" + Data.Fmt(cv) + "）", () => { var S2 = SaveSys.S; long v = S2.stash.Where(i => i.r <= 1).Sum(i => (long)i.v); S2.stash.RemoveAll(i => i.r <= 1); S2.money += v; SaveSys.Save(); if (v > 0) Sfx.Play("cash"); MenuStash(); }, Data.H("2f7d4a"), 24);
        ((RectTransform)b.transform).Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(520, 60));
        var tl = UiKit.Txt(c, "展示架：" + (S.trophies.Count == 0 ? "（空）" : string.Join("、", S.trophies.Select(t => t.name))), 22, Data.H("c9a6ff"), TextAnchor.MiddleLeft); tl.rectTransform.Place(new Vector2(0, 0), new Vector2(1, 0), new Vector2(560, 0), new Vector2(0, 60));
    }
    void SellAt(int i) { var S = SaveSys.S; if (i >= S.stash.Count) return; S.money += S.stash[i].v; S.stash.RemoveAt(i); SaveSys.Save(); Sfx.Play("cash", 0.7f); MenuStash(); }
    void ShowTrophy(int i)
    {
        var S = SaveSys.S; if (S.trophies.Count >= Data.Upg[2].eff[S.shelf]) { Toast("展示架滿了！去勞作桌升級。"); Sfx.Play("error_004"); return; }
        var t = S.stash[i]; S.stash.RemoveAt(i); S.trophies.Add(t); SaveSys.Save(); Sfx.Reveal(t.r); RebuildBaseKeepPos(); MenuStash();
    }
    void RebuildBaseKeepPos() { var p = player.transform.position; float y = player.yaw; if (baseRoot) Destroy(baseRoot.gameObject); baseRoot = BaseHub.Build(out _, out _); player.Teleport(p, y); }
    void MenuShop()
    {
        var S = SaveSys.S; var c = ui.Modal("小豬撲滿：買零食和裝備　　<size=24>存款 " + Data.Fmt(S.money) + "</size>", 1500, 860);
        // snacks
        UiKit.Txt(c, "零食", 30, Accent, TextAnchor.UpperLeft, true).rectTransform.At(new Vector2(0, 1), new Vector2(0, 0), new Vector2(460, 40));
        for (int k = 0; k < 2; k++)
        {
            int kk = k; var card = ui.Card(c, new Vector2(0, -50 - k * 200), new Vector2(456, 186), Panel2Col);
            UiKit.Txt(card, (k == 0 ? "貓罐頭" : "狗狗肉肉") + "　<size=22>有 " + (k == 0 ? S.cans : S.meats) + " 個</size>", 28, Color.white, TextAnchor.UpperLeft, true).rectTransform.Place(new Vector2(0, 1), Vector2.one, new Vector2(18, -50), new Vector2(-18, -12));
            UiKit.Txt(card, k == 0 ? "丟出去，貓咪會跑去吃 3～4 秒。" : "丟出去，狗狗會跑去吃 3～4 秒。", 20, Dim, TextAnchor.UpperLeft).rectTransform.Place(new Vector2(0, 0), Vector2.one, new Vector2(18, 64), new Vector2(-18, -54));
            int price = k == 0 ? Data.CanPrice : Data.MeatPrice;
            var bb = UiKit.Btn(card, "買 1 個 " + Data.Fmt(price), () => BuySnack(kk), S.money >= price ? Data.H("3fae5a") : Data.H("8a8f99"), 20); ((RectTransform)bb.transform).At(new Vector2(1, 0), new Vector2(-14, 14), new Vector2(200, 46));
        }
        int col = 1;
        foreach (var slot in new[] { "shoes", "bag" })
        {
            UiKit.Txt(c, slot == "shoes" ? "鞋子" : "袋子", 30, Accent, TextAnchor.UpperLeft, true).rectTransform.At(new Vector2(0, 1), new Vector2(col * 476, 0), new Vector2(460, 40));
            int row = 0;
            foreach (var g in Data.Gear[slot])
            {
                var gg = g; bool eq = (slot == "shoes" ? S.shoes : S.bag) == g.id; bool own = g.price == 0 || S.GearN(g.id) > 0;
                var card = ui.Card(c, new Vector2(col * 476, -50 - row * 200), new Vector2(456, 186), eq ? new Color(0.3f, 0.6f, 0.4f, 1) : Panel2Col);
                UiKit.Txt(card, g.n + (eq ? "　<size=20><color=#4a8a5f>穿著中</color></size>" : ""), 28, Color.white, TextAnchor.UpperLeft, true).rectTransform.Place(new Vector2(0, 1), Vector2.one, new Vector2(18, -50), new Vector2(-18, -12));
                string st = slot == "bag" ? g.cap + " 格" : "速度 ×" + g.spd.ToString("0.00");
                UiKit.Txt(card, st + "\n<color=#6a5a8a>" + g.d + "</color>", 20, Color.white, TextAnchor.UpperLeft).rectTransform.Place(new Vector2(0, 0), Vector2.one, new Vector2(18, 64), new Vector2(-18, -54));
                if (!own) { var bb = UiKit.Btn(card, "買 " + Data.Fmt(g.price), () => Buy(gg), S.money >= g.price ? Data.H("3fae5a") : Data.H("8a8f99"), 20); ((RectTransform)bb.transform).At(new Vector2(1, 0), new Vector2(-14, 14), new Vector2(170, 46)); }
                else if (!eq) { var be = UiKit.Btn(card, "換上", () => { if (slot == "shoes") SaveSys.S.shoes = gg.id; else SaveSys.S.bag = gg.id; SaveSys.Save(); player.Equip(SaveSys.S.shoes); MenuShop(); }, Data.H("4a7fd0"), 20); ((RectTransform)be.transform).At(new Vector2(1, 0), new Vector2(-14, 14), new Vector2(120, 46)); }
                row++;
            }
            col++;
        }
    }
    static readonly Color Panel2Col = new Color(0.36f, 0.4f, 0.62f, 1);
    void BuySnack(int k) { var S = SaveSys.S; int p = k == 0 ? Data.CanPrice : Data.MeatPrice; if (S.money < p) { Toast("撲滿裡的錢不夠…"); Sfx.Play("error_004"); return; } S.money -= p; if (k == 0) S.cans++; else S.meats++; SaveSys.Save(); Sfx.Play("cash", 0.6f); MenuShop(); }
    void Buy(GearDef g) { var S = SaveSys.S; if (S.money < g.price) { Toast("撲滿裡的錢不夠，再多找一點寶貝吧！"); Sfx.Play("error_004"); return; } S.money -= g.price; S.AddGear(g.id, 1); if (Data.Gear["shoes"].Contains(g)) S.shoes = g.id; else S.bag = g.id; SaveSys.Save(); player.Equip(S.shoes); Sfx.Play("cash"); Toast("買了「" + g.n + "」，已經換上囉！"); MenuShop(); }
    void MenuUpg()
    {
        var S = SaveSys.S; var c = ui.Modal("勞作桌：做升級", 1300, 760);
        for (int i = 0; i < Data.Upg.Length; i++)
        {
            var u = Data.Upg[i]; int lv = S.Upg(u.id), mx = u.lv.Length - 1;
            var card = ui.Card(c, new Vector2(0, -i * 150), new Vector2(1228, 136), Panel2Col);
            string val = u.id == "luck" ? "轉圈 ×" + u.eff[lv] : u.id == "stash" ? u.eff[lv] + " 格" : u.id == "shelf" ? u.eff[lv] + " 格" : "保留 " + u.eff[lv] + " 件";
            UiKit.Txt(card, u.n + "　<size=22>Lv." + lv + "/" + mx + "　<color=#d97a2a>" + val + "</color></size>", 30, Color.white, TextAnchor.UpperLeft, true).rectTransform.Place(new Vector2(0, 1), Vector2.one, new Vector2(24, -60), new Vector2(-24, -16));
            UiKit.Txt(card, u.d, 22, Dim, TextAnchor.UpperLeft).rectTransform.Place(new Vector2(0, 0), Vector2.one, new Vector2(24, 16), new Vector2(-300, -66));
            var uu = u;
            if (lv < mx) { int price = u.lv[lv + 1]; var b = UiKit.Btn(card, "升級 " + Data.Fmt(price), () => { var S2 = SaveSys.S; if (S2.money < price) { Toast("撲滿裡的錢不夠…"); Sfx.Play("error_004"); return; } S2.money -= price; S2.SetUpg(uu.id, S2.Upg(uu.id) + 1); SaveSys.Save(); Sfx.Play("confirmation_001"); RebuildBaseKeepPos(); MenuUpg(); }, S.money >= price ? Data.H("2f7d4a") : Data.H("3a3f46"), 24); ((RectTransform)b.transform).At(new Vector2(1, 0.5f), new Vector2(-24, 0), new Vector2(240, 64)); }
            else UiKit.Txt(card, "已經最厲害了", 26, Good, TextAnchor.MiddleRight, true).rectTransform.Place(Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-30, 0));
        }
    }
    void MenuQuest()
    {
        var S = SaveSys.S; var c = ui.Modal("熊熊", 1100, 560);
        var line = Data.NpcLines[Random.Range(0, Data.NpcLines.Length)];
        UiKit.Txt(c, line, 28, Color.white, TextAnchor.UpperLeft).rectTransform.Place(new Vector2(0, 1), Vector2.one, new Vector2(0, -90), Vector2.zero);
        var q = S.quest; string qs = q == null ? "" : "今天的任務：<b>" + q.t + "</b>\n獎勵 <color=#d97a2a>" + Data.Fmt(q.reward) + "</color>" + (q.done ? "　<color=#3f9a6a>完成了！</color>" : "");
        UiKit.Txt(c, qs, 28, Color.white, TextAnchor.UpperLeft).rectTransform.Place(new Vector2(0, 0), Vector2.one, new Vector2(0, 110), new Vector2(0, -130));
        if (q != null && q.done)
        {
            var b = UiKit.Btn(c, "領獎勵", () => { var S2 = SaveSys.S; S2.money += S2.quest.reward; S2.questN++; S2.quest = Data.MakeQuest(S2.questN); SaveSys.Save(); Sfx.Play("cash"); Toast("熊熊：「好棒！這是給你的零用錢。」"); RebuildBaseKeepPos(); MenuQuest(); }, Data.H("3fae5a"), 28);
            ((RectTransform)b.transform).Place(Vector2.zero, new Vector2(0, 0), Vector2.zero, new Vector2(320, 70));
        }
        UiKit.Txt(c, "紀錄：出門 " + S.stats.raids + "　平安回家 " + S.stats.extracts + "　被叫回家 " + S.stats.deaths + "　被抓 " + S.stats.caught + "　最多一次 " + Data.Fmt(S.stats.best), 22, Dim, TextAnchor.LowerRight).rectTransform.Place(Vector2.zero, new Vector2(1, 0), new Vector2(340, 0), new Vector2(0, 60));
    }
    public Interactable Focus => focus;

    // ---------- casual autotest ----------
    int errs; readonly List<string> errList = new List<string>();
    void OnLog(string msg, string st, LogType t) { if (t == LogType.Exception || t == LogType.Error || t == LogType.Assert) { errs++; if (errList.Count < 20) errList.Add(msg + " @ " + (st ?? "").Split('\n')[0]); } }
    IEnumerator Aim(Vector3 eye, Vector3 target)
    {
        var d = target - eye; var hd = new Vector3(d.x, 0, d.z); if (hd.sqrMagnitude < 0.001f) hd = Vector3.forward;
        player.Teleport(eye - Vector3.up * 1.25f, Quaternion.LookRotation(hd).eulerAngles.y); player.pitch = -Mathf.Atan2(d.y, hd.magnitude) * Mathf.Rad2Deg; yield return null; yield return null;
    }
    IEnumerator CamShot(Vector3 from, Vector3 at, string name, float wait = 0.4f)
    {
        var cm = player.cam.transform; cm.position = from; cm.rotation = Quaternion.LookRotation(at - from); yield return new WaitForSeconds(wait);
        cm.position = from; cm.rotation = Quaternion.LookRotation(at - from); yield return Shot(name); cm.localPosition = Vector3.zero; cm.localRotation = Quaternion.identity;
    }
    Vector3 FreeCam(Vector3 c, float rad)
    {   // pick the horizontal direction with the most free space (walls block), stay inside the room
        float y = c.y + 0.9f; Vector3 best = c + Vector3.up; float bd = -1;
        for (int i = 0; i < 16; i++)
        {
            var d = Quaternion.Euler(0, i * 22.5f, 0) * Vector3.forward; var o = new Vector3(c.x, y, c.z) + d * (rad + 0.1f);
            float free = Physics.SphereCast(o, 0.25f, d, out var h, 4f, ~0, QueryTriggerInteraction.Ignore) ? h.distance : 4f;
            if (Physics.CheckSphere(o, 0.25f, ~0, QueryTriggerInteraction.Ignore)) free = 0;
            if (free > bd) { bd = free; best = o + d * Mathf.Clamp(free - 0.35f, 0.3f, 2.6f); }
        }
        return best;
    }
    void Check(bool ok, string what) { Debug.Log("[casual] " + (ok ? "PASS " : "FAIL ") + what); if (!ok) fails++; }
    int fails;
    void PlacePet(Pet p, Vector3 pos) { var cc = p.GetComponent<CharacterController>(); if (cc) cc.enabled = false; p.transform.position = pos; if (cc) cc.enabled = true; }
    IEnumerator AutoShotsCasual()
    {
        Application.logMessageReceived += OnLog; Time.timeScale = 1; yield return new WaitForSeconds(2.5f);
        var S0 = SaveSys.S; S0.money += 20000;
        yield return Shot("oneMap_bedroom");
        var desk = FindObjectsByType<Station>(FindObjectsSortMode.None).FirstOrDefault(x => x.id == "deploy");
        if (desk) { var b = desk.WorldBounds; yield return Aim(b.center + new Vector3(0, 0.9f, 0) + (player.transform.position - b.center).normalized * 1.6f, b.center); yield return new WaitForSeconds(0.6f); Check(focus == desk, "bedroom desk focus"); yield return Shot("oneMap_bedroom_desk"); }
        { var bc0 = baseRoot.position; yield return CamShot(bc0 + new Vector3(3.6f, 2.6f, 3.4f), bc0 + new Vector3(-1.5f, 0.7f, -2f), "oneMap_bedroom_wide"); }
        OpenStation("shop"); yield return new WaitForSeconds(0.4f); yield return Shot("oneMap_shop"); CloseModal();
        OpenStation("deploy"); yield return new WaitForSeconds(0.4f); yield return Shot("oneMap_map"); CloseModal();
        int parkIdx = System.Array.FindIndex(Data.Themes, t => !t.playable); Deploy(parkIdx); yield return null; Check(state == St.Base, "park (施工中) not deployable");
        for (int run = 0; run < 2; run++)
        {
            int ti = 0; var th = Data.Themes[ti]; string tn = th.id;
            if (run > 0) { BackToBase(); for (float w = 0; w < 5 && state != St.Base; w += Time.deltaTime) yield return null; yield return new WaitForSeconds(1.0f); }
            Deploy(ti); yield return new WaitForSeconds(3.0f);
            if (state != St.Raid || level == null) { Check(false, "deploy " + tn); continue; }
            Pet.FreezeAll = 999; S0.cans = S0.meats = 5;
            Check(Pet.All.Count == th.enemies, tn + " pets " + Pet.All.Count + "/" + th.enemies);
            Check(level.bus != null, tn + " bus stop");
            yield return new WaitForSeconds(0.5f); yield return Shot("oneMap_" + tn + "_spawn_" + run);
            var bld = level.root.GetComponentsInChildren<Transform>().Where(t => t.name == "House").OrderByDescending(t => t.GetComponentsInChildren<LootContainer>().Length).FirstOrDefault();
            if (bld) { yield return CamShot(bld.position + new Vector3(9, 6.5f, -15), bld.position + new Vector3(0, 3, 3), "oneMap_" + tn + "_ext_" + run); }
            if (run == 0 && bld)
            {   // street wide, then one shot per room
                Debug.Log("[contrast] signs checked " + SoftProps.SignCount + ", fails " + SoftProps.SignFails);
                yield return CamShot(new Vector3(-34, 3.5f, -1), new Vector3(-12, 2, 3), "oneMap_street");
                { var thz = GameObject.Find("TownHouse"); int left = thz ? LevelGen.ClearStreet(thz.transform.parent, false) : -1;
                  Debug.Log("[overlap] props " + LevelGen.OverlapProps + " moved " + LevelGen.OverlapMoved + " hidden " + LevelGen.OverlapHidden + " remaining " + left);
                  Check(left == 0, "street dressing clear of buildings + each other (remaining " + left + ")"); }
                yield return CamShot(new Vector3(30, 3.5f, 1), new Vector3(52, 2.5f, 8), "oneMap_street_e");
                yield return CamShot(new Vector3(-30, 3.5f, -1), new Vector3(-50, 2.5f, -8), "oneMap_street_w");
                var slide = FindFirstObjectByType<SlideRide>(); Check(slide, "slide is interactable");
                if (slide)
                {
                    var st = slide.transform; var near = st.TransformPoint(new Vector3(0, 0, -2.6f));
                    player.Teleport(near, st.eulerAngles.y); yield return null; yield return null;
                    Check(slide.Prompt.Contains("溜滑梯"), "slide prompt");
                    int r0 = SlideRide.Rides; slide.Use(); yield return new WaitForSeconds(1.75f); yield return Shot("oneMap_slide_ride");
                    for (float w = 0; w < 6 && SlideRide.Busy; w += Time.deltaTime) yield return null;
                    var land = st.TransformPoint(new Vector3(0, 0, 2.9f));
                    Check(SlideRide.Rides == r0 + 1 && !SlideRide.Busy && Vector3.Distance(player.transform.position, land) < 0.6f && player.cc.enabled, "slide ride lands at the bottom");
                    yield return CamShot(st.TransformPoint(new Vector3(3.5f, 2.2f, 4.5f)), st.TransformPoint(new Vector3(0, 1, 0)), "oneMap_slide");
                }
                {   // sign close-ups from 3 angles (text clipping check)
                    var signs = FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t => t.name == "sk_text3d").OrderBy(t => (t.position - bld.position).sqrMagnitude).Take(10).ToList(); int si = 0;
                    foreach (var sg in signs.Take(2)) yield return CamShot(sg.position + sg.forward * 5f + Vector3.up * 0.5f, sg.position, "oneMap_occlusion" + si++, 0.2f); si = 0;
                    foreach (var sg in signs) { int ai = 0; foreach (var ang in new[] { 0f, -40f, 40f }) { var dir = Quaternion.Euler(0, ang, 0) * (-sg.forward); yield return CamShot(sg.position + dir * 3.2f + Vector3.up * 0.3f, sg.position, "oneMap_sign" + si + "_" + ai++, 0.2f); } si++; }
                }
                var picked = new List<LootContainer>();
                foreach (var lc in level.containers.Where(x => x.transform.IsChildOf(bld))) if (picked.All(q => (q.transform.position - lc.transform.position).sqrMagnitude > 16f) && picked.Count < 6) picked.Add(lc);
                for (int ri = 0; ri < picked.Count; ri++)
                {
                    var cb = picked[ri].WorldBounds; var sp = level.spots.OrderBy(s2 => (s2 - cb.center).sqrMagnitude).First(); var dd = sp - cb.center; dd.y = 0; if (dd.sqrMagnitude < 0.3f) dd = picked[ri].transform.forward; dd.Normalize();
                    yield return CamShot(FreeCam(cb.center, cb.extents.magnitude), cb.center, "oneMap_room_" + ri);
                }
            }
            // loot UI
            var c = level.containers.Where(x => !x.special && (!bld || x.transform.IsChildOf(bld))).FirstOrDefault();
            if (c)
            {
                var b = c.WorldBounds; var room = level.spots.OrderBy(s => (s - b.center).sqrMagnitude).First(); var dir = room - b.center; dir.y = 0; if (dir.sqrMagnitude < 0.3f) dir = c.transform.forward; dir.Normalize();
                var eye = b.center + dir * 1.7f; eye.y = b.min.y + 1.25f; yield return Aim(eye, b.center); yield return new WaitForSeconds(0.6f);
                if (run == 0) yield return Shot("oneMap_container_prompt");
                OpenLoot(c); yield return new WaitForSeconds(0.5f); if (run == 0) yield return Shot("oneMap_loot_spin");
                for (float w = 0; w < 15 && c.slots.Any(s => s.state < 2); w += Time.deltaTime) yield return null; yield return new WaitForSeconds(0.3f);
                UIContrast.PassNow(); Debug.Log("[contrast-ui] checked " + UIContrast.Checked + " fixed " + UIContrast.Fixed + " fails " + UIContrast.Fails); Check(UIContrast.Fails == 0, "UI text contrast >= 4.5 (fails " + UIContrast.Fails + ")");
                yield return Shot("oneMap_" + tn + "_loot_" + run); LootAll(); yield return new WaitForSeconds(0.3f); CloseLoot();
                Check(bag.Count > 0, tn + " looted " + bag.Count);
            }
            if (run == 0)
            {
                var cat = Pet.All.FirstOrDefault(p => !p.dog); var dog = Pet.All.FirstOrDefault(p => p.dog);
                foreach (var pt in new[] { cat, dog }) if (pt) { var pp = pt.transform.position; yield return CamShot(pp + pt.transform.forward * 1.5f + pt.transform.right * 0.6f + Vector3.up * 0.7f, pp + Vector3.up * 0.3f, pt.dog ? "oneMap_dog" : "oneMap_cat", 0.2f); }
                // catch -> steal
                if (cat)
                {
                    var open = new Vector3(-40, 0.15f, 1.5f); player.Teleport(open, 90); PlacePet(cat, open + new Vector3(3, 0, 0)); Pet.FreezeAll = 0; int bag0 = bag.Count;
                    cat.ForceState(Pet.St.Chase); for (float w = 0; w < 4 && player.caught == 0; w += Time.deltaTime) yield return null;
                    yield return new WaitForSeconds(0.25f); yield return Shot("oneMap_caught");
                    Check(player.caught == 1 && (bag0 == 0 || bag.Count == bag0 - 1), "pet catch steals (bag " + bag0 + "->" + bag.Count + ")");
                    for (float w = 0; w < 4 && cat.st == Pet.St.Flee; w += Time.deltaTime) yield return null; yield return new WaitForSeconds(0.3f);
                    Check(FindObjectsByType<GroundLoot>(FindObjectsSortMode.None).Length > 0, "stolen item dropped");
                    player.Caught(cat); player.Caught(cat); Check(player.Crying, "3rd catch -> crying"); yield return new WaitForSeconds(0.3f); yield return Shot("oneMap_crying"); player.cryT = 0; Pet.FreezeAll = 999;
                    // lure throw
                    float ly = 90; for (int k = 0; k < 8; k++) { float yy = k * 45; var dd = Quaternion.Euler(0, yy, 0) * Vector3.forward; if (!Physics.Raycast(open + Vector3.up * 0.6f, dd, 11f, ~0, QueryTriggerInteraction.Ignore) && !Physics.Raycast(open + Vector3.up * 1.4f, dd, 11f, ~0, QueryTriggerInteraction.Ignore)) { ly = yy; break; } }
                    player.Teleport(open, ly); PlacePet(cat, open + Quaternion.Euler(0, ly, 0) * new Vector3(2.5f, 0, 7)); cat.ForceState(Pet.St.Patrol); cat.patrol.Clear(); yield return new WaitForSeconds(0.2f);
                    int cans0 = S0.cans; player.sel = 0; player.pitch = -8; player.Throw(); yield return new WaitForSeconds(0.25f); yield return Shot("oneMap_lure_throw");
                    Check(S0.cans == cans0 - 1, "lure consumes can");
                    for (float w = 0; w < 3 && cat.st != Pet.St.Lured; w += Time.deltaTime) yield return null; Check(cat.st == Pet.St.Lured, "cat lured");
                    for (float w = 0; w < 9 && luresRun == 0; w += Time.deltaTime) yield return null; yield return new WaitForSeconds(0.3f); yield return Shot("oneMap_lure_eat");
                    Check(luresRun > 0, "cat eats lure");
                    for (float w = 0; w < 6 && Lure.All.Count > 0; w += Time.deltaTime) yield return null; Check(Lure.All.Count == 0, "lure expires");
                }
                // natural bus schedule
                runT = Data.BusFirst - 1; for (float w = 0; w < 9 && !level.bus.CanBoard; w += Time.deltaTime) yield return null;
                Check(level.bus.CanBoard, "bus arrives on schedule");
                for (float w = 0; w < 10 && level.bus.CanBoard; w += Time.deltaTime) yield return null;
                Check(!level.bus.CanBoard, "bus leaves after waiting");
                runT = RaidTime - 62; for (float w = 0; w < 12 && !level.bus.CanBoard; w += Time.deltaTime) yield return null;
                yield return new WaitForSeconds(9f); Check(level.bus.CanBoard, "last bus waits (guaranteed)");
            }
            if (run == 1)
            {   // mom timer fail
                runT = RaidTime - 0.5f; for (float w = 0; w < 3 && state != St.End; w += Time.deltaTime) yield return null;
                Check(state == St.End && bag.Count >= 0, "mom timer ends run"); yield return new WaitForSeconds(1.5f); yield return Shot("oneMap_mom_fail"); continue;
            }
            // bus stop + cinematic
            if (!level.bus.CanBoard) level.bus.ForceArrive();
            var bp = level.bus.Point; yield return Aim(bp + new Vector3(-2.2f, 0, -2.0f) + Vector3.up * 0.2f, bp); yield return new WaitForSeconds(0.8f);
            Check(focus == level.bus, tn + " bus focus"); if (run == 0) yield return Shot("oneMap_bus_stop");
            if (run == 0) yield return CamShot(bp + new Vector3(-9, 2.5f, 8), bp + new Vector3(0, 0, 1.5f), "oneMap_bus_wide");
            if (focus == level.bus) focus.Use(); else Extract();
            yield return new WaitForSeconds(2.5f); if (run == 0) yield return Shot("oneMap_bus_cine_a");
            yield return new WaitForSeconds(3.5f); if (run == 0) yield return Shot("oneMap_bus_cine_b");
            for (float w = 0; w < 20 && state != St.End; w += Time.deltaTime) yield return null; yield return new WaitForSeconds(1f);
            Check(state == St.End, tn + " extract"); yield return Shot("oneMap_" + tn + "_results");
        }
        Pet.FreezeAll = 0; BackToBase(); yield return new WaitForSeconds(2f);
        OpenStation("stash"); yield return new WaitForSeconds(0.4f); yield return Shot("oneMap_stash"); CloseModal();
        OpenStation("quest"); yield return new WaitForSeconds(0.4f); yield return Shot("oneMap_quest"); CloseModal();
        UIContrast.PassNow(); Check(UIContrast.Fails == 0, "UI contrast across all screens (checked " + UIContrast.Checked + ", fixed " + UIContrast.Fixed + ")");
        Debug.Log("[casual] errors " + errs + (errList.Count > 0 ? " :: " + string.Join(" || ", errList) : "") + " checks failed " + fails);
        Debug.Log(errs == 0 && fails == 0 ? "[casual] full loop ok" : "[casual] full loop FAILED");
        PlayerPrefs.DeleteKey(SaveSys.Key); yield return new WaitForSeconds(0.5f); Application.Quit();
    }
    // ---------- autoshots ----------
    IEnumerator Shot(string name)
    {
        yield return new WaitForEndOfFrame();
        var path = System.IO.Path.Combine(shotDir, name + ".png"); System.IO.Directory.CreateDirectory(shotDir);
        ScreenCapture.CaptureScreenshot(path); Debug.Log("[autoshot] " + path); yield return null; yield return null;
    }
}
