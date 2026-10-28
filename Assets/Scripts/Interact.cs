using System.Collections.Generic;
using UnityEngine;

public abstract class Interactable : MonoBehaviour
{
    public float reach = 2.6f;
    public abstract string Prompt { get; }
    public abstract void Use();
    public virtual Vector3 Point => transform.position + Vector3.up * 0.8f;
    public static readonly List<Interactable> All = new List<Interactable>();
    Collider[] cols; Renderer[] rens;
    /// world bounds of the object (colliders, else renderers), used for reach + hint placement
    public Bounds WorldBounds
    {
        get
        {
            if (cols == null || cols.Length == 0 || !cols[0]) cols = GetComponentsInChildren<Collider>();
            bool any = false; var b = new Bounds(Point, Vector3.one * 0.3f);
            foreach (var c in cols) if (c && c.enabled) { if (!any) { b = c.bounds; any = true; } else b.Encapsulate(c.bounds); }
            if (!any) { if (rens == null) rens = GetComponentsInChildren<Renderer>(); foreach (var r in rens) if (r && !(r.GetComponentInParent<HintRing>())) { if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds); } }
            return b;
        }
    }
    public virtual Vector3 TopPoint { get { var b = WorldBounds; return new Vector3(b.center.x, b.max.y, b.center.z); } }
    /// distance from eye to nearest point of the object (lenient reach)
    public float Dist(Vector3 eye) { var b = WorldBounds; return Vector3.Distance(eye, b.ClosestPoint(eye)); }
    protected virtual void OnEnable() { All.Add(this); }
    protected virtual void OnDisable() { All.Remove(this); }
}

public class LootSlot { public Item it; public int state; public float p, dur; } // 0 wait 1 spin 2 revealed 3 taken

public class LootContainer : Interactable
{
    public string kind, label; public List<LootSlot> slots = new List<LootSlot>(); public bool opened, special; Renderer[] rends;
    public override string Prompt => (label) + (Empty ? "（空空的）" : opened ? "（再翻翻）" : "") + "　[E] 翻翻看";
    public bool Empty { get { foreach (var s in slots) if (s.state != 3) return false; return true; } }
    public override Vector3 Point => GetComponentInChildren<Collider>() ? GetComponentInChildren<Collider>().bounds.center : transform.position + Vector3.up * 0.6f;
    public void Init(string k, System.Random rng, ThemeDef th, float vmult, int luck, string labelOverride = null)
    {
        kind = k; var ck = Data.CK[k]; label = labelOverride ?? ck.l; reach = Mathf.Max(2.6f, ck.reach + 0.4f);
        int n = rng.Next(ck.nMin, ck.nMax + 1);
        for (int i = 0; i < n; i++) slots.Add(new LootSlot { it = Roll(ck.pool, rng, th, vmult, luck) });
    }
    public static Item Roll(string pool, System.Random rng, ThemeDef th, float vmult, int luck)
    {
        var p = (float[])Data.Pools[pool].Clone(); float sh = Mathf.Min(luck * 0.035f, p[0]); if (sh > 0) { p[0] -= sh; p[3] += sh * 0.6f; p[4] += sh * 0.4f; }
        double x = rng.NextDouble(); int r = p.Length - 1; for (int i = 0; i < p.Length; i++) { x -= p[i]; if (x < 0) { r = i; break; } }
        var list = new List<ItemDef>(); foreach (var d in Data.Items) if (d.r == r) list.Add(d);
        if (th != null && rng.NextDouble() < 0.45) { var tl = new List<ItemDef>(); foreach (var d in th.items) if (d.r == r) tl.Add(d); if (tl.Count > 0) list = tl; }
        var def = list[rng.Next(list.Count)];
        return new Item { r = def.r, ic = def.ic, name = def.name, v = Mathf.Max(1, Mathf.RoundToInt(def.v * (0.85f + (float)rng.NextDouble() * 0.3f) * vmult)) };
    }
    public override void Use() { Pet.Noise(transform.position, 3f); Game.I.OpenLoot(this); }
    void Start()
    {
        var b = WorldBounds; float rx = Mathf.Clamp(b.extents.x + 0.22f, 0.4f, 1.6f), rz = Mathf.Clamp(b.extents.z + 0.22f, 0.4f, 1.6f);
        var floor = new Vector3(b.center.x, kind == "corpse" ? transform.position.y : b.min.y, b.center.z);
        var h = HintRing.Add(this, floor, kind == "corpse" ? 0.9f : rx, special ? new Color(1f, 0.75f, 0.25f) : new Color(1f, 0.88f, 0.55f)); if (kind != "corpse") h.aspect = rz / rx;
    }
    public void MarkEmpty()
    {
        if (rends == null) rends = GetComponentsInChildren<Renderer>();
        foreach (var r in rends) if (r) { var mpb = new MaterialPropertyBlock(); r.GetPropertyBlock(mpb); mpb.SetColor("_BaseColor", new Color(0.55f, 0.55f, 0.55f)); r.SetPropertyBlock(mpb); }
    }
}

public class GroundLoot : Interactable
{
    public Item it; Transform model; float t0; public bool fallbackModel;
    public override string Prompt => "<color=#" + ColorUtility.ToHtmlStringRGB(Data.RarCol[it.r]) + ">" + it.name + "</color>　" + (it.key ? "" : Data.Fmt(it.v) + "　") + "[E] 撿起";
    public override Vector3 Point => transform.position + Vector3.up * 0.15f;
    public override Vector3 TopPoint => transform.position + Vector3.up * 0.3f;
    public void Init(Item item)
    {
        it = item; reach = 2.4f; t0 = Random.value * 6;
        var c = Data.RarCol[it.r];
        model = ItemArt.Build(transform, it, out fallbackModel); model.localRotation = Quaternion.Euler(0, Random.value * 360, 0);
        var col = gameObject.AddComponent<SphereCollider>(); col.radius = 0.4f; col.center = Vector3.up * 0.2f; col.isTrigger = true;
        if (it.r >= 2) { var l = new GameObject("l").AddComponent<Light>(); l.transform.SetParent(transform, false); l.transform.localPosition = Vector3.up * 0.4f; l.type = LightType.Point; l.color = c; l.range = 1.8f; l.intensity = 0.8f + it.r * 0.3f; l.shadows = LightShadows.None; }
        HintRing.Add(this, transform.position, 0.42f, c);
    }
    public override void Use() { if (Game.I.Pickup(it)) { Destroy(gameObject); } }
}

public class Door : Interactable
{
    public bool locked, open; public Transform hinge; float ang, target; public Collider col;
    public override string Prompt => locked ? "鎖起來的門（要鑰匙）　[E]" : open ? "[E] 關門" : "[E] 開門";
    public override Vector3 Point => col ? col.bounds.center : transform.position + Vector3.up;
    public override void Use()
    {
        if (locked) { if (!Game.I.UseKey()) { Game.I.Toast("打不開…鑰匙一定藏在哪個抽屜裡！"); Sfx.Play("error_004", 0.6f); return; } locked = false; Game.I.Toast("喀嚓！門開了，裡面一定有好東西！"); }
        open = !open; target = open ? 100 : 0; Pet.Noise(Point, 5f); Sfx.At("impactPlank_medium_000", Point, 0.7f);
    }
    void Update() { ang = Mathf.MoveTowards(ang, target, 240 * Time.deltaTime); if (hinge) hinge.localRotation = Quaternion.Euler(0, ang, 0); }
}

public class Station : Interactable
{
    public string label, id; public override string Prompt => "[E] " + label;
    public override void Use() { Game.I.OpenStation(id); }
    void Start() { var b = WorldBounds; float rx = Mathf.Clamp(b.extents.x + 0.2f, 0.4f, 1.3f), rz = Mathf.Clamp(b.extents.z + 0.2f, 0.4f, 1.3f); var h = HintRing.Add(this, new Vector3(b.center.x, 0.02f, b.center.z), rx, new Color(0.45f, 0.85f, 1f)); h.aspect = rz / rx; }
}
