using System;
using System.Collections;
using UnityEngine;

/// CASUAL: bus-stop extraction. A bus arrives every 45 s and waits 6–8 s; in the last ~60 s it waits until the end.
public class Bus : Interactable
{
    public enum BS { Away, Arriving, Waiting, Leaving }
    public BS state = BS.Away; public Transform bus, door, sign; public Vector3 stopPos; public float nextArrive = Data.BusFirst, waitLeft; public bool cine, guaranteed;
    Vector3 lane, dir; float tState; Transform[] wheels = new Transform[0];
    public bool CanBoard => state == BS.Waiting;
    public override string Prompt => CanBoard ? "<color=#3f9a6a>[E] 上公車回家！</color>" + (guaranteed ? "" : "（再 " + Mathf.CeilToInt(waitLeft) + " 秒開走）") : state == BS.Arriving ? "公車快到了，等一下下～" : "公車站：下一班大約 " + Mathf.Max(0, Mathf.CeilToInt(NextIn)) + " 秒後";
    public float NextIn => Game.I ? nextArrive - Game.I.runT : 0;
    public override Vector3 Point => sign ? sign.position : transform.position + Vector3.up;
    public override void Use() { if (CanBoard) Game.I.Extract(); else { Game.I.Toast("公車還沒來，先去找點心吧！下一班大約 " + Mathf.Max(0, Mathf.CeilToInt(NextIn)) + " 秒。"); Sfx.Play("error_004", 0.5f); } }

    public static Bus Build(Transform parent, Vector3 stop, Vector3 laneDir)
    {
        var root = new GameObject("BusStop"); root.transform.SetParent(parent, false); root.transform.position = stop;
        var b = root.AddComponent<Bus>(); b.reach = 6.5f; b.stopPos = stop; b.dir = laneDir.normalized;
        // shelter + sign
        var side = Vector3.Cross(Vector3.up, laneDir).normalized; // shelter sits off the lane
        var rt = root.transform;
        // SoftKit shelter: pink arched roof, mint posts, butter back wall with round window, bench, stone pad
        var sh = SoftKit.Group(rt, "shelter", -side * 1.6f, Quaternion.LookRotation(side).eulerAngles.y);
        SoftKit.Block(sh, new Vector3(0, 0.03f, 0), new Vector3(3.8f, 0.06f, 2.0f), Color.Lerp(SoftKit.LavD, Color.white, 0.4f), 0.03f, false, 0, 0);
        foreach (int s in new[] { -1, 1 }) foreach (float z in new[] { -0.7f, 0.7f }) SoftKit.Cyl(sh, new Vector3(s * 1.55f, 1.2f, z), 0.09f, 2.4f, SoftKit.MintD, 0.04f);
        var arc = SoftKit.Cyl(sh, new Vector3(0, 2.45f, 0), 1.05f, 3.7f, SoftKit.PinkD, 0.2f, Quaternion.Euler(0, 0, 90), 26); arc.transform.localScale = new Vector3(0.42f, 1, 1);
        foreach (int s in new[] { -1, 1 }) for (int i = 0; i < 4; i++) SoftKit.Ball(sh, new Vector3(-1.4f + i * 0.93f, 2.38f, s * 1.0f), new Vector3(0.9f, 0.22f, 0.18f), i % 2 == 0 ? SoftKit.Pink : SoftKit.White, 10);
        SoftKit.Block(sh, new Vector3(0, 1.25f, -0.75f), new Vector3(3.2f, 2.1f, 0.12f), SoftKit.Butter, 0.08f, true);
        var rw = SoftProps.RoundWin(sh, new Vector3(0.75f, 1.55f, -0.66f), 0.42f, SoftKit.White, true); rw.localRotation = Quaternion.Euler(0, 180, 0);
        SoftKit.Block(sh, new Vector3(-0.3f, 0.48f, -0.4f), new Vector3(2.4f, 0.12f, 0.5f), SoftKit.Coral, 0.05f, true);
        foreach (int s in new[] { -1, 1 }) SoftKit.Block(sh, new Vector3(-0.3f + s * 1.0f, 0.22f, -0.4f), new Vector3(0.12f, 0.42f, 0.4f), SoftKit.MintD, 0.04f, false);
        SoftProps.FlowerPot(sh, new Vector3(-1.75f, 0.06f, 0.9f), SoftKit.Sky, SoftKit.Rose, 0.8f, false);
        SoftProps.StreetLamp(rt, -side * 0.3f - laneDir * 2.3f, Quaternion.LookRotation(side).eulerAngles.y);
        // sign: mint pole + butter disc (b.sign) with labels on both faces
        SoftKit.Cyl(rt, new Vector3(0, 1.3f, 0), 0.07f, 2.6f, SoftKit.Mint, 0.03f);
        var sg = SoftKit.Cyl(rt, new Vector3(0, 2.7f, 0), 0.62f, 0.1f, SoftKit.Butter, 0.04f, Quaternion.LookRotation(side) * Quaternion.Euler(90, 0, 0), 24); b.sign = sg.transform;
        SoftKit.Ring(rt, new Vector3(0, 2.7f, 0), Quaternion.LookRotation(side), 0.62f, 0.05f, SoftKit.Coral, 28);
        foreach (int s in new[] { -1, 1 }) SoftKit.Text(rt, new Vector3(0, 2.7f, 0) + side * 0.11f * s, Quaternion.LookRotation(-side * s), "公車站", 0.05f, SoftKit.Plum);
        SoftKit.Block(rt, Vector3.up * -0.085f + side * 2.5f, new Vector3(12, 0.03f, 3.2f), SoftKit.Butter, 0.01f, false, 0, 0); // yellow bay
        var col = root.AddComponent<BoxCollider>(); col.isTrigger = true; col.center = Vector3.up * 1.2f; col.size = new Vector3(1.4f, 2.6f, 1.4f);
        // bus
        b.lane = stop + side * 2.6f;
        var bt = new GameObject("Bus").transform; bt.SetParent(parent, false); b.bus = bt; bt.rotation = Quaternion.LookRotation(laneDir);
        // SoftKit chubby bus: butter body, sky roof + skirt, mint stripe, headlight eyes and a smile
        var C = SoftKit.Butter;
        SoftKit.Block(bt, new Vector3(0, 1.75f, 0), new Vector3(2.5f, 2.4f, 8.4f), C, 0.55f, true, 0, 0.02f);
        SoftKit.Block(bt, new Vector3(0, 0.78f, 0), new Vector3(2.56f, 0.5f, 8.5f), SoftKit.SkyD, 0.2f, false);
        SoftKit.Block(bt, new Vector3(0, 1.25f, 0), new Vector3(2.54f, 0.18f, 8.2f), SoftKit.MintD, 0.08f, false);
        SoftKit.Block(bt, new Vector3(0, 3.0f, -0.1f), new Vector3(2.2f, 0.35f, 7.6f), SoftKit.Sky, 0.16f, false);
        for (int i = 0; i < 4; i++) foreach (int s in new[] { -1, 1 }) SoftKit.Block(bt, new Vector3(s * 1.26f, 2.2f, -2.6f + i * 1.6f), new Vector3(0.06f, 0.95f, 1.3f), SoftKit.Sky, 0.22f, false, 0, 0);
        SoftKit.Block(bt, new Vector3(0, 2.25f, 4.18f), new Vector3(2.0f, 1.0f, 0.08f), SoftKit.Sky, 0.25f, false, 0, 0);
        foreach (int s in new[] { -1, 1 })
        {
            SoftKit.Ball(bt, new Vector3(s * 0.78f, 1.05f, 4.24f), new Vector3(0.55f, 0.55f, 0.16f), SoftKit.White, 16);
            SoftKit.Ball(bt, new Vector3(s * 0.74f, 1.08f, 4.31f), new Vector3(0.24f, 0.28f, 0.06f), SoftKit.Plum, 12);
            SoftKit.Ball(bt, new Vector3(s * 0.7f, 1.16f, 4.34f), Vector3.one * 0.08f, SoftKit.White, 8);
            SoftKit.Ball(bt, new Vector3(s * 0.85f, 0.7f, 4.24f), new Vector3(0.3f, 0.16f, 0.08f), SoftKit.Rose, 10);
            SoftKit.Cyl(bt, new Vector3(s * 1.38f, 2.55f, 3.85f), 0.03f, 0.4f, SoftKit.GreyD, 0.01f, Quaternion.Euler(0, 0, 90), 8);
            SoftKit.Ball(bt, new Vector3(s * 1.6f, 2.45f, 3.85f), new Vector3(0.14f, 0.3f, 0.1f), SoftKit.Plum, 10);
        }
        for (int i = 0; i < 5; i++) { float a = Mathf.Lerp(-0.9f, 0.9f, i / 4f); SoftKit.Ball(bt, new Vector3(Mathf.Sin(a) * 0.32f, 0.82f - Mathf.Cos(a) * 0.1f + 0.1f, 4.27f), Vector3.one * 0.07f, SoftKit.Plum, 6); }
        foreach (int s in new[] { -1, 1 }) SoftKit.Block(bt, new Vector3(0, 0.62f, s * 4.28f), new Vector3(2.4f, 0.3f, 0.22f), SoftKit.SkyD, 0.12f, false);
        SoftKit.Block(bt, new Vector3(0, 2.92f, 4.12f), new Vector3(1.5f, 0.38f, 0.1f), SoftKit.White, 0.12f, false);
        var dr = new GameObject("door").transform; dr.SetParent(bt, false); dr.localPosition = new Vector3(-1.27f, 1.5f, 3.3f); b.door = dr;
        SoftKit.Block(dr, Vector3.zero, new Vector3(0.06f, 2.0f, 1.1f), SoftKit.MintD, 0.12f, false);
        SoftKit.Block(dr, new Vector3(-0.02f, 0.35f, 0), new Vector3(0.05f, 0.9f, 0.7f), SoftKit.Sky, 0.15f, false);
        var ws = new System.Collections.Generic.List<Transform>();
        foreach (float z in new[] { -2.8f, 2.6f }) foreach (int s in new[] { -1, 1 })
        {
            var w = SoftKit.Cyl(bt, new Vector3(s * 1.2f, 0.5f, z), 0.5f, 0.34f, SoftKit.Plum, 0.12f, Quaternion.Euler(0, 0, 90), 22);
            SoftKit.Cyl(w.transform, Vector3.zero, 0.22f, 0.38f, SoftKit.ButterD, 0.06f, Quaternion.identity, 12);
            SoftKit.Block(w.transform, new Vector3(0, 0, 0.12f), new Vector3(0.08f, 0.4f, 0.08f), SoftKit.White, 0.03f, false, 0, 0);
            ws.Add(w.transform);
        }
        b.wheels = ws.ToArray();
        var nm = new GameObject("route"); nm.transform.SetParent(bt, false); nm.transform.localPosition = new Vector3(0, 2.92f, 4.19f); nm.transform.localRotation = Quaternion.Euler(0, 180, 0); var t2 = nm.AddComponent<TextMesh>(); t2.font = Art.FontBold; if (Art.FontBold) nm.GetComponent<MeshRenderer>().sharedMaterial = Art.WorldFontMat; t2.text = "回家號"; t2.characterSize = 0.04f; t2.fontSize = 80; t2.anchor = TextAnchor.MiddleCenter; t2.color = SoftKit.Rose;
        // the bus door faces the stop: side = -X of bus if laneDir x side mapping; flip if needed
        bt.position = b.lane - laneDir * 80; bt.gameObject.SetActive(false);
        return b;
    }
    Vector3 BusStopPos => lane - dir * 3.3f; // door lines up with the sign
    void Update()
    {
        if (cine || !Game.I || !Game.I.RaidActive) return;
        float dt = Time.deltaTime; tState += dt; float tl = Game.I.TimeLeft;
        switch (state)
        {
            case BS.Away:
                if (Game.I.runT >= nextArrive || tl <= 63f) { state = BS.Arriving; tState = 0; guaranteed = tl <= 63f; bus.gameObject.SetActive(true); bus.position = BusStopPos - dir * 60; Sfx.At("honk", BusStopPos, 1f, 6, 80); Game.I.Toast("叭叭！公車來了，快去公車站！"); }
                break;
            case BS.Arriving:
                { float k = Mathf.Clamp01(tState / 4.5f); float e = 1 - (1 - k) * (1 - k); bus.position = Vector3.Lerp(BusStopPos - dir * 60, BusStopPos, e); Spin(60 * (1 - e) + 2);
                  if (k >= 1) { state = BS.Waiting; tState = 0; waitLeft = UnityEngine.Random.Range(6f, 8f); Sfx.At("honk", BusStopPos, 0.8f, 6, 80); } }
                break;
            case BS.Waiting:
                if (tl <= 63f) guaranteed = true;
                if (!guaranteed) { waitLeft -= dt; if (waitLeft <= 0) { state = BS.Leaving; tState = 0; Game.I.Toast("公車開走了…下一班大約 " + Mathf.CeilToInt(Data.BusEvery - 4.5f - 8) + " 秒後。"); nextArrive = Game.I.runT + Data.BusEvery - 12.5f; } }
                break;
            case BS.Leaving:
                { float k = Mathf.Clamp01(tState / 4f); bus.position = BusStopPos + dir * (k * k * 70); Spin(k * 60); if (k >= 1) { state = BS.Away; bus.gameObject.SetActive(false); } }
                break;
        }
        if (door) door.localPosition = new Vector3(-1.27f, 1.5f, 3.3f - (state == BS.Waiting ? Mathf.Clamp01(tState * 2) * 1.1f : 0));
        if (Player.I && bus.gameObject.activeSelf) { float d = Vector3.Distance(Player.I.transform.position, bus.position); Sfx.Engine(Mathf.Clamp01(1 - d / 50f) * 0.5f, state == BS.Waiting ? 0.8f : 1.1f); } else Sfx.Engine(0);
    }
    void Spin(float v) { foreach (var w in wheels) if (w) w.Rotate(Vector3.up, v * Time.deltaTime * 20, Space.Self); }
    /// test helper: bring the bus to the stop right now
    public void ForceArrive() { bus.gameObject.SetActive(true); bus.position = BusStopPos; state = BS.Waiting; tState = 1; waitLeft = 8; guaranteed = true; }

    public IEnumerator Cinematic(Camera cam, Action<string, string> sub, Func<bool> skip)
    {
        cine = true; float t = 0; bus.gameObject.SetActive(true); var start = BusStopPos; bus.position = start;
        string[] lines = Data.CineSubs; string line1 = lines[UnityEngine.Random.Range(0, lines.Length)], line2 = lines[UnityEngine.Random.Range(0, lines.Length)];
        var side = Vector3.Cross(Vector3.up, dir).normalized;
        Sfx.Play("honk", 0.7f);
        while (t < 8f)
        {
            float dt = Time.deltaTime; t += dt; if (t > 1.0f && skip()) break;
            float go = Mathf.Max(0, t - 2.2f); bus.position = start + dir * (go * go * 1.6f); Spin(go * 25);
            if (door) door.localPosition = new Vector3(-1.27f, 1.5f, 3.3f - (t < 1.8f ? 1.1f : Mathf.Clamp01(1 - (t - 1.8f) * 2) * 1.1f));
            Sfx.Engine(0.6f, 0.9f + Mathf.Min(0.5f, go * 0.08f));
            Vector3 cp, look;
            if (t < 2.6f) { cp = start - side * 5.5f + dir * 7f + Vector3.up * 1.6f; look = bus.position + Vector3.up * 1.6f + dir * 2.5f; }
            else if (t < 5.4f) { float k = (t - 2.6f) / 2.8f; cp = bus.position - side * (4.5f + k * 2) - dir * (6 - k * 2) + Vector3.up * (2.2f + k); look = bus.position + Vector3.up * 1.8f + dir * 2; }
            else { float k = (t - 5.4f) / 2.6f; cp = start - side * 3f - dir * 8 + Vector3.up * (4 + k * 6); look = bus.position + Vector3.up * 1.5f; }
            cam.transform.position = Vector3.Lerp(cam.transform.position, cp, t < 0.1f ? 1 : dt * 3); cam.transform.rotation = Quaternion.Slerp(cam.transform.rotation, Quaternion.LookRotation(look - cam.transform.position), t < 0.1f ? 1 : dt * 4);
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, 58, dt * 2);
            if (t < 2.4f) sub("上車囉", line1); else sub("回家囉！", t < 5.2f ? line1 : line2);
            yield return null;
        }
        Sfx.Engine(0); cine = false;
    }
}
