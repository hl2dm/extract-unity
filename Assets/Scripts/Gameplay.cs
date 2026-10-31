using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// v0.9.15 gameplay layer: family wishlist, late-bus bonus, treasure book (圖鑑), end-of-run stars
public class Wish { public string who, name; public int r, reward; public bool got; }

public static class Gameplay
{
    static readonly string[] Who = { "媽媽", "哥哥", "妹妹", "爸爸", "阿公" };
    public static List<Wish> MakeWishes(ThemeDef th, int seed)
    {
        var rng = new System.Random(seed * 31 + 7); var pool = new List<ItemDef>();
        foreach (var d in th.items) if (d.r <= 2) pool.Add(d);
        foreach (var d in Data.Items) if (d.r <= 1) pool.Add(d);
        var who = Who.OrderBy(_ => rng.Next()).ToList(); var res = new List<Wish>();
        // two easy wishes + one stretch wish (rarity >= 1)
        foreach (var want in new[] { 0, 0, 1 })
        {
            var cand = pool.Where(d => (want == 0 ? d.r == 0 : d.r >= 1) && res.All(w => w.name != d.name)).ToList(); if (cand.Count == 0) cand = pool.Where(d => res.All(w => w.name != d.name)).ToList(); if (cand.Count == 0) break;
            var d2 = cand[rng.Next(cand.Count)]; res.Add(new Wish { who = who[res.Count], name = d2.name, r = d2.r, reward = d2.r == 0 ? 120 : d2.r == 1 ? 250 : 450 });
        }
        return res;
    }
    /// mark wishes satisfied by what is in the bag (each bag item satisfies at most one wish)
    public static int MatchWishes(List<Wish> ws, List<Item> bag)
    {
        if (ws == null) return 0; var used = new HashSet<Item>(); int n = 0;
        foreach (var w in ws) { var it = bag.FirstOrDefault(i => !i.key && i.name == w.name && !used.Contains(i)); w.got = it != null; if (it != null) { used.Add(it); n++; } }
        return n;
    }
    public static string WishLine(List<Wish> ws)
    {
        if (ws == null || ws.Count == 0) return "";
        return "家人的願望：" + string.Join("　", ws.Select(w => (w.got ? "<color=#2f7a4a>✔" : "<color=#5c3d4a>") + w.who + "想要" + w.name + "</color>"));
    }
    // late bus: the longer you stay, the bigger the bonus (but pets + mom's timer keep pushing)
    public static float LateMul(int busN) => busN <= 1 ? 1f : busN == 2 ? 1.2f : busN == 3 ? 1.4f : 1.6f;
    // treasure book
    public static List<string> AllBook()
    {
        var l = new List<string>(); foreach (var th in Data.Themes) if (th.playable) foreach (var d in th.items) if (!l.Contains(d.name)) l.Add(d.name);
        foreach (var d in Data.Items) if (!l.Contains(d.name)) l.Add(d.name); return l;
    }
    public static int BookTotal => AllBook().Count;
    public static bool Discover(Item it)
    {
        var S = SaveSys.S; if (S.found == null) S.found = new List<string>(); if (it == null || it.key || S.found.Contains(it.name)) return false;
        S.found.Add(it.name); return true;
    }
    public static int Stars(bool ok, long value, int wishes, int caught) { if (!ok) return 0; int s = 1; if (wishes >= 2 || value >= 1500) s++; if (caught == 0) s++; return s; }
    public static string StarText(int s) => "<color=#d97a2a>" + new string('★', s) + "</color><color=#8a7a70>" + new string('☆', 3 - s) + "</color>";
}
