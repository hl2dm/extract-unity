using System.Collections.Generic;
using UnityEngine;

/// v0.9.16: visible noise ring under the kid + hiding spots (under tables, behind curtains)
public class NoiseRing : MonoBehaviour
{
    public const float Sneak = 1.6f, Walk = 4f, Run = 10f, Search = 3f;
    LineRenderer lr; const int N = 72; float shown; Material mSneak, mWalk, mRun;
    public static NoiseRing I;
    public static NoiseRing Make() { var g = new GameObject("NoiseRing"); I = g.AddComponent<NoiseRing>(); return I; }
    void Awake()
    {
        lr = gameObject.AddComponent<LineRenderer>(); lr.useWorldSpace = true; lr.loop = true; lr.positionCount = N; lr.widthMultiplier = 0.07f; lr.numCornerVertices = 2;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; lr.receiveShadows = false; lr.textureMode = LineTextureMode.Tile;
        mSneak = Art.Unlit(Data.H("5fb889")); mWalk = Art.Unlit(Data.H("e8a640")); mRun = Art.Unlit(Data.H("e06a5a")); lr.sharedMaterial = mWalk; lr.enabled = false;
    }
    void LateUpdate()
    {
        var p = Player.I; bool on = p && p.inRaid && Game.I && Game.I.RaidActive && !p.hidden && p.NoiseR > 0.1f;
        shown = Mathf.MoveTowards(shown, on ? p.NoiseR : 0, Time.deltaTime * 14f);
        lr.enabled = shown > 0.15f; if (!lr.enabled) return;
        lr.sharedMaterial = p.NoiseR >= Run - 0.1f ? mRun : p.NoiseR <= Sneak + 0.1f ? mSneak : mWalk;
        var c = p.transform.position; float y = c.y + 0.04f, r = shown * (1 + 0.025f * Mathf.Sin(Time.time * 6));
        for (int i = 0; i < N; i++) { float a = i * Mathf.PI * 2 / N; float w = 1 + 0.02f * Mathf.Sin(a * 9 + Time.time * 2); lr.SetPosition(i, new Vector3(c.x + Mathf.Cos(a) * r * w, y, c.z + Mathf.Sin(a) * r * w)); }
    }
}

public class HideSpot : Interactable
{
    public static readonly List<HideSpot> Spots = new List<HideSpot>();
    public string kind = "桌子底下"; public bool low = true;
    void OnEnable() { if (!Spots.Contains(this)) Spots.Add(this); }
    void OnDisable() { Spots.Remove(this); }
    public override string Prompt => Player.I && Player.I.hidden && Player.I.hideAt == this ? "[E] 出來" : "[E] 躲到" + kind;
    public override void Use()
    {
        var p = Player.I; if (!p) return;
        if (p.hidden) { p.Unhide(); return; }
        p.Hide(this); Game.I.Toast("躲好了！貓咪狗狗看不到你　（E 或走動就出來）", 2.6f); Sfx.Play("whoosh", 0.35f);
    }
    /// add hiding spots to the aunt's house: round tables + curtains (indoors only)
    public static int Setup(Transform root)
    {
        Spots.Clear(); int n = 0; if (!root) return 0;
        foreach (var t in root.GetComponentsInChildren<Transform>())
        {
            if (t.name != "sk_rtable" && t.name != "sk_curtain" && t.name != "sk_haystack") continue;
            if (t.GetComponent<HideSpot>()) continue; if (t.name == "sk_curtain" && n >= 8) continue;
            var h = t.gameObject.AddComponent<HideSpot>(); h.low = t.name != "sk_curtain"; h.kind = t.name == "sk_haystack" ? "草堆裡面" : h.low ? "桌子底下" : "窗簾後面"; h.reach = t.name == "sk_haystack" ? 2.4f : 2.2f; n++;
        }
        return n;
    }
}
