using System.Collections.Generic;
using UnityEngine;

/// v0.9.16: visible noise ring under the kid + hiding spots (under tables, behind curtains)
public class NoiseRing : MonoBehaviour
{   // v0.9.19: soft translucent glow disc on the ground instead of a solid LineRenderer line
    public const float Sneak = 1.6f, Walk = 4f, Run = 10f, Search = 3f;
    public MeshRenderer mr; float shown; Material mSneak, mWalk, mRun; Transform q;
    public static NoiseRing I;
    public bool Visible => mr && mr.enabled;
    public static NoiseRing Make() { var g = new GameObject("NoiseRing"); I = g.AddComponent<NoiseRing>(); return I; }
    Material M(string hex, float a, float edge)
    {
        var sh = Resources.Load<Shader>("Shaders/NoiseRing") ?? Shader.Find("Soft/NoiseRing");
        Material m = sh ? new Material(sh) : Art.Unlit(Data.H(hex)); var c = Data.H(hex);
        m.SetColor("_Color", c); m.SetFloat("_A", a); m.SetFloat("_Edge", edge); m.renderQueue = 3010; return m;
    }
    void Awake()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad); go.name = "noise_glow"; Destroy(go.GetComponent<Collider>());
        q = go.transform; q.SetParent(transform, false); q.rotation = Quaternion.Euler(90, 0, 0);
        mr = go.GetComponent<MeshRenderer>(); mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; mr.receiveShadows = false;
        mr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off; mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        // bigger rings get a thinner rim in uv so the glow band stays ~0.3-0.5 m wide on the floor
        mSneak = M("a8ffe0", 0.62f, 0.34f / Sneak); mWalk = M("ffc93a", 0.44f, 0.62f / Walk); mRun = M("f2655a", 0.44f, 0.95f / Run);   // rim ~0.3-0.4 m wide on the floor; red boosted so the biggest ring stays readable
        mr.sharedMaterial = mWalk; mr.enabled = false;
    }
    void LateUpdate()
    {
        var p = Player.I; bool on = p && p.inRaid && Game.I && Game.I.RaidActive && !p.hidden && p.NoiseR > 0.1f;
        shown = Mathf.MoveTowards(shown, on ? p.NoiseR : 0, Time.deltaTime * 14f);
        mr.enabled = shown > 0.15f; if (!mr.enabled) return;
        mr.sharedMaterial = p.NoiseR >= Run - 0.1f ? mRun : p.NoiseR <= Sneak + 0.1f ? mSneak : mWalk;
        var c = p.transform.position; float r = shown * (1 + 0.02f * Mathf.Sin(Time.time * 4));
        q.position = new Vector3(c.x, c.y + 0.05f, c.z); q.localScale = new Vector3(r * 2, r * 2, 1);
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
