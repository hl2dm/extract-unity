using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable] public class Item { public int r; public string ic; public string name; public int v; public bool key; public bool paid; }

public class ItemDef { public int r; public string ic, name; public int v; public ItemDef(int r, string ic, string n, int v) { this.r = r; this.ic = ic; name = n; this.v = v; } }
public class CKind { public string l, model; public Vector3 size; public string pool; public int nMin, nMax; public float reach = 1.9f; }
public class ThemeDef { public string id, name, twist; public int tier; public float mult; public bool playable; public Color sky; public ItemDef[] items; public string[] rooms; public string special; public int enemies = 2; public string style = "house"; }
public class GearDef { public string id, n, d; public int price; public float dmg, rate, spread, red = 0, spd = 1; public int mag, cap; public string model; }
public class UpgDef { public string id, n, d; public int[] lv; public float[] eff; }

public static class Data
{
    public static readonly string[] RarName = { "普通", "不錯", "稀有", "超稀有", "傳說" };
    public static readonly Color[] RarCol = { H("b9c2cc"), H("4fd16a"), H("3fa0ff"), H("c070ff"), H("ff9a1f") };
    public static readonly float[] RarSpin = { 0.45f, 0.8f, 1.15f, 1.7f, 2.4f };
    public const float RaidTime = 240f, BoardWindow = 60f;
    public const float BusFirst = 35f, BusEvery = 45f;
    public static Color H(string h) { ColorUtility.TryParseHtmlString("#" + h, out var c); return c; }

    public static readonly ItemDef[] Items = {
        new ItemDef(0,"🍬","糖果",12), new ItemDef(0,"🧽","橡皮擦",16), new ItemDef(0,"🖍️","蠟筆",24), new ItemDef(0,"📎","迴紋針",8), new ItemDef(0,"🎈","氣球",20),
        new ItemDef(1,"🍫","巧克力",60), new ItemDef(1,"🧃","果汁",50), new ItemDef(1,"🪀","溜溜球",90), new ItemDef(1,"🃏","卡牌",100),
        new ItemDef(2,"🎮","掌上遊戲機",390), new ItemDef(2,"🧸","泰迪熊",300), new ItemDef(2,"🚗","玩具車",270),
        new ItemDef(3,"🤖","機器人模型",900), new ItemDef(3,"🛹","滑板",750),
        new ItemDef(4,"🏆","金色獎盃",4000), new ItemDef(4,"🦖","恐龍化石",4800), new ItemDef(4,"👑","公主皇冠",4500) };

    public static readonly Dictionary<string, float[]> Pools = new Dictionary<string, float[]> {
        {"low", new[]{0.55f,0.28f,0.12f,0.04f,0.01f}}, {"mid", new[]{0.4f,0.3f,0.18f,0.09f,0.03f}},
        {"high", new[]{0.1f,0.2f,0.3f,0.25f,0.15f}}, {"top", new[]{0f,0.1f,0.3f,0.35f,0.25f}} };

    public static readonly Dictionary<string, CKind> CK = new Dictionary<string, CKind> {
        {"box", new CKind{l="神秘紙箱", model="Env/CardboardBoxes_1", size=new Vector3(0.9f,0.7f,0.9f), pool="low", nMin=3, nMax=6}},
        {"fridge", new CKind{l="冰箱", model="Ext/fridge", size=new Vector3(0.8f,1.85f,0.85f), pool="low", nMin=3, nMax=7}},
        {"wardrobe", new CKind{l="大衣櫃", model="Furn/bookcaseClosedDoors", size=new Vector3(1.5f,2.1f,0.65f), pool="mid", nMin=3, nMax=8}},
        {"drawer", new CKind{l="小抽屜", model="Furn/sideTableDrawers", size=new Vector3(0.9f,0.9f,0.55f), pool="low", nMin=2, nMax=5}},
        {"safe", new CKind{l="上鎖的寶物盒", model=null, size=new Vector3(0.6f,0.6f,0.6f), pool="high", nMin=3, nMax=4}},
        {"shelf", new CKind{l="零食貨架", model="PH/steel_frame_shelves_01", size=new Vector3(1.1f,2.1f,0.52f), pool="mid", nMin=2, nMax=5}},
        {"display", new CKind{l="木頭展示櫃", model="PH/wooden_display_shelves_01", size=new Vector3(1.08f,1.56f,0.4f), pool="mid", nMin=2, nMax=4}},
        {"cooler", new CKind{l="飲料冰櫃", model=null, size=new Vector3(0.8f,1.85f,0.85f), pool="low", nMin=3, nMax=6}},
        {"register", new CKind{l="收銀台", model=null, size=new Vector3(0.55f,0.35f,0.45f), pool="mid", nMin=2, nMax=4}},
        {"desk", new CKind{l="課桌抽屜", model=null, size=new Vector3(1.5f,0.76f,0.75f), pool="low", nMin=2, nMax=4}},
        {"rack", new CKind{l="教具櫃", model=null, size=new Vector3(0.62f,2.0f,0.95f), pool="high", nMin=2, nMax=4}},
        {"crate", new CKind{l="木箱子", model="PH/wooden_crate_02", size=new Vector3(1.0f,0.7f,0.7f), pool="mid", nMin=3, nMax=6}},
        {"milcrate", new CKind{l="長長的箱子", model="PH/old_military_crate", size=new Vector3(1.8f,0.3f,0.98f), pool="high", nMin=2, nMax=4}},
        {"toolchest", new CKind{l="工具櫃", model="PH/metal_tool_chest", size=new Vector3(0.69f,0.65f,0.41f), pool="mid", nMin=2, nMax=5}},
    };

    // style: house / mart / office(=school classrooms)
    public static readonly ThemeDef[] Themes = {
        new ThemeDef{ id="aunt", name="阿姨家", tier=1, mult=1f, playable=true, sky=H("8fd3ff"), enemies=2, twist="阿姨去買菜了，家裡只剩小咪跟旺財。點心就在冰箱裡！",
            rooms=new[]{"box","fridge","wardrobe","box","wardrobe","drawer"}, special="safe",
            items=new[]{ new ItemDef(0,"🍪","餅乾",20), new ItemDef(1,"🍮","布丁",80), new ItemDef(2,"🍰","草莓蛋糕",450), new ItemDef(3,"🧧","紅包",1200), new ItemDef(4,"🎁","巧克力禮盒",5000)} },
        new ThemeDef{ id="mart", name="便利商店", tier=2, mult=1.15f, playable=false, enemies=3, style="mart", sky=H("9fe0ff"), twist="叮咚～歡迎光臨！店貓在櫃台睡午覺，不要吵醒牠。",
            items=new[]{ new ItemDef(0,"🍙","御飯糰",24), new ItemDef(1,"🥤","珍奶",90), new ItemDef(1,"🍦","霜淇淋",76), new ItemDef(2,"🎴","集點貼紙",480), new ItemDef(3,"🎁","盲盒",1300), new ItemDef(4,"🎟️","一番賞",5500)} },
        new ThemeDef{ id="school", name="學校", tier=3, mult=1.35f, playable=false, enemies=3, style="office", sky=H("a8e6ff"), twist="放學後的教室。警衛伯伯的狗狗在走廊巡邏喔。",
            items=new[]{ new ItemDef(0,"📝","作業簿",16), new ItemDef(1,"✏️","自動鉛筆",70), new ItemDef(1,"📏","尺",60), new ItemDef(2,"📕","聯絡簿",420), new ItemDef(3,"💯","考卷100分",1500), new ItemDef(4,"🏅","校長的金牌",6000)} },
        new ThemeDef{ id="grandma", name="阿嬤家", tier=2, mult=1.2f, playable=false, enemies=3, sky=H("ffd9a8"), twist="阿嬤在午睡，三隻貓狗在院子曬太陽。阿嬤的紅包藏在哪裡呢？",
            rooms=new[]{"box","fridge","wardrobe","drawer","crate","drawer"}, special="safe",
            items=new[]{ new ItemDef(0,"🍊","橘子",20), new ItemDef(1,"🍡","麻糬",80), new ItemDef(2,"🥮","月餅",390), new ItemDef(3,"🧧","阿嬤的大紅包",1600), new ItemDef(4,"🏺","古董花瓶",6500)} },
        new ThemeDef{ id="park", name="公園", tier=2, mult=1.2f, sky=H("b6f0c0"), twist="溜滑梯還在施工中，下次再來玩！", items=new ItemDef[0] },
    };

    public static readonly Dictionary<string, GearDef[]> Gear = new Dictionary<string, GearDef[]> {
        {"shoes", new[]{ new GearDef{id="s0",n="拖鞋",price=0,spd=1f,d="啪噠啪噠，跑不快。"},
                         new GearDef{id="s1",n="運動鞋",price=800,spd=1.12f,d="跑得比較快！"},
                         new GearDef{id="s2",n="發光跑鞋",price=2500,spd=1.22f,d="一跑就閃閃發光，咻～"}}},
        {"bag", new[]{ new GearDef{id="b0",n="購物袋",price=0,cap=12,d="可以裝 12 樣東西。"},
                       new GearDef{id="b1",n="書包",price=600,cap=20,d="可以裝 20 樣東西。"},
                       new GearDef{id="b2",n="露營大背包",price=2200,cap=30,d="可以裝 30 樣東西！好重喔。"}}},
    };
    public static GearDef GearOf(string slot, string id) { foreach (var g in Gear[slot]) if (g.id == id) return g; return Gear[slot][0]; }
    public static string GearName(string id) { foreach (var kv in Gear) foreach (var g in kv.Value) if (g.id == id) return g.n; return id; }
    public const int CanPrice = 30, MeatPrice = 40;

    public static readonly UpgDef[] Upg = {
        new UpgDef{id="luck",n="幸運襪",d="轉圈圈變快，也比較容易找到好東西",lv=new[]{0,800,2500,6000},eff=new[]{1f,0.85f,0.72f,0.6f}},
        new UpgDef{id="stash",n="玩具箱",d="玩具箱可以放幾樣東西",lv=new[]{0,600,2000,5000},eff=new[]{30f,45f,60f,90f}},
        new UpgDef{id="shelf",n="展示架",d="書架上可以擺幾個寶貝",lv=new[]{0,1000,3500},eff=new[]{3f,6f,10f}},
        new UpgDef{id="secure",n="口袋",d="被媽媽叫回家時，口袋裡最貴的 N 樣東西可以留著",lv=new[]{0,1200,4000,9000},eff=new[]{0f,1f,2f,3f}},
    };

    public static readonly string[] NpcLines = { "熊熊：「今天也要小心貓咪喔，牠們最愛搶東西了。」", "熊熊：「丟罐頭給貓咪、丟肉肉給狗狗，牠們就會乖乖吃東西～」", "熊熊：「公車每 45 秒來一班，錯過就等下一班吧！」", "熊熊：「媽媽打電話來就要趕快回家，不然只能帶口袋裡的東西喔。」", "熊熊：「走路小聲一點，跑步會吵醒睡覺的狗狗。」" };
    public static readonly string[] CineSubs = { "司機伯伯：「小朋友，刷卡上車喔～」", "司機伯伯：「東西抱好，不要掉在車上啦。」", "你靠在窗邊數今天的戰利品，嘴角一直往上翹。", "窗外的貓咪好像在跟你揮手說掰掰。" };

    public static Quest MakeQuest(int q)
    {
        switch (q % 6)
        {
            case 0: { int ti = (q / 6) % 4; return new Quest { type = "theme", theme = ti, reward = 200 + 120 * ti, t = "去【" + Themes[ti].name + "】玩一趟，平安坐公車回家" }; }
            case 1: return new Quest { type = "rare", r = 2, n = 2 + (q % 3), reward = 350 + 80 * (q % 3), t = "一次帶回 " + (2 + q % 3) + " 樣「稀有」以上的寶貝" };
            case 2: return new Quest { type = "lure", n = 2, reward = 300, t = "丟 2 次零食，讓貓咪或狗狗吃得很開心" };
            case 3: return new Quest { type = "speed", s = 80, reward = 350, t = "80 秒內坐上公車（熊熊想早點吃晚餐）" };
            case 4: { int v = 1500 + 800 * (q % 3); return new Quest { type = "value", v = v, reward = 500, t = "一次帶回 " + Fmt(v) + " 以上的寶貝" }; }
            default: return new Quest { type = "clean", reward = 400, t = "一次都不被貓咪狗狗抓到，平安回家" };
        }
    }
    public static string Fmt(long v) => v.ToString("N0") + " 元";
    public static string IconKey(string ic) { var sb = new System.Text.StringBuilder("u"); bool first = true; for (int i = 0; i < ic.Length; i++) { int cp = char.ConvertToUtf32(ic, i); if (char.IsHighSurrogate(ic[i])) i++; if (cp == 0xfe0f) continue; if (!first) sb.Append('_'); sb.Append(cp.ToString("x")); first = false; } return sb.ToString(); }
}

[Serializable] public class Quest { public string type, t; public int theme, r, n, s, v, reward; public bool done; }
[Serializable] public class GearCount { public string id; public int n; }
[Serializable] public class Stats { public int raids, extracts, deaths, best, caught, lures; public long earned; }
[Serializable] public class SaveData
{
    public int v = 1; public long money = 300; public List<Item> stash = new List<Item>(); public List<Item> trophies = new List<Item>();
    public List<GearCount> gear = new List<GearCount>();
    public string shoes = "s0", bag = "b0"; public int cans = 3, meats = 3;
    public int luck, stashLv, shelf, secure;
    public Quest quest; public bool hasQuest; public int questN; public Stats stats = new Stats();
    public bool inRaid; public int seedN = 1; public List<string> found = new List<string>(); public int bestStars;
    public bool Owns(string id) => Data.GearOf(id[0] == 's' ? "shoes" : "bag", id).price == 0 || GearN(id) > 0;
    public int GearN(string id) { foreach (var g in gear) if (g.id == id) return g.n; return 0; }
    public void AddGear(string id, int d) { foreach (var g in gear) if (g.id == id) { g.n = Mathf.Max(0, g.n + d); return; } if (d > 0) gear.Add(new GearCount { id = id, n = d }); }
    public int Upg(string id) => id == "luck" ? luck : id == "stash" ? stashLv : id == "shelf" ? shelf : secure;
    public void SetUpg(string id, int l) { if (id == "luck") luck = l; else if (id == "stash") stashLv = l; else if (id == "shelf") shelf = l; else secure = l; }
    public int StashCap => (int)Data.Upg[1].eff[stashLv];
}

public static class SaveSys
{
    public static string Key = "casual_save_v1";
    public static SaveData S;
    public static void Load()
    {
        try { var j = PlayerPrefs.GetString(Key, ""); S = string.IsNullOrEmpty(j) ? new SaveData() : JsonUtility.FromJson<SaveData>(j); } catch { S = new SaveData(); }
        if (S == null) S = new SaveData();
        if (S.stash == null) S.stash = new List<Item>(); if (S.trophies == null) S.trophies = new List<Item>(); if (S.gear == null) S.gear = new List<GearCount>(); if (S.stats == null) S.stats = new Stats(); if (S.found == null) S.found = new List<string>();
    }
    public static void Save() { PlayerPrefs.SetString(Key, JsonUtility.ToJson(S)); PlayerPrefs.Save(); }
}
