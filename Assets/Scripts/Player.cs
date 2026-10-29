using UnityEngine;
using UnityEngine.Rendering.Universal;

/// CASUAL: kid first-person controller. No guns — left mouse throws a can (cats) or meat (dogs).
public class Player : MonoBehaviour
{
    public static Player I;
    public static bool Riding;
    public CharacterController cc; public Camera cam, vmCam; public Transform camRig, vmRoot, hand;
    public float yaw, pitch, hp = 100; public bool alive = true, inRaid, controlEnabled = true;
    public GearDef shoes; public int sel; // 0 can, 1 meat
    public int caught; public float cryT; public bool Crying => cryT > 0; public bool Sprinting { get; private set; }
    float bobT, stepT, vy, shake, throwT, throwAnim, noiseT; int handSel = -1;
    public float Sens = 2.0f;

    public static float Walk = 3.5f, Run = 5.6f, Eye = 1.15f, Fov = 70f;
    public static Player Create()
    {
        var g = new GameObject("Player"); var p = g.AddComponent<Player>(); I = p;
        p.cc = g.AddComponent<CharacterController>(); p.cc.height = 1.30f; p.cc.radius = 0.28f; p.cc.center = new Vector3(0, 0.65f, 0); p.cc.stepOffset = 0.35f; p.cc.slopeLimit = 50;
        p.camRig = new GameObject("CamRig").transform; p.camRig.SetParent(g.transform, false); p.camRig.localPosition = new Vector3(0, Eye, 0);
        var cg = new GameObject("MainCamera"); cg.tag = "MainCamera"; cg.transform.SetParent(p.camRig, false); p.cam = cg.AddComponent<Camera>(); cg.AddComponent<AudioListener>();
        p.cam.fieldOfView = Fov; p.cam.nearClipPlane = 0.05f; p.cam.farClipPlane = 400; p.cam.cullingMask = ~(1 << Art.VMLayer);
        var cd = p.cam.GetUniversalAdditionalCameraData(); cd.renderPostProcessing = true; AAQuality.Apply(p.cam); cd.renderShadows = true;
        var vg = new GameObject("VMCamera"); vg.transform.SetParent(cg.transform, false); p.vmCam = vg.AddComponent<Camera>(); p.vmCam.cullingMask = 1 << Art.VMLayer; p.vmCam.fieldOfView = 55; p.vmCam.nearClipPlane = 0.01f; p.vmCam.farClipPlane = 5;
        var vd = p.vmCam.GetUniversalAdditionalCameraData(); vd.renderType = CameraRenderType.Overlay; vd.renderShadows = false; cd.cameraStack.Add(p.vmCam);
        p.vmRoot = new GameObject("VM").transform; p.vmRoot.SetParent(cg.transform, false); p.vmRoot.localPosition = new Vector3(0.28f, -0.24f, 0.45f);
        p.Equip(SaveSys.S.shoes);
        return p;
    }
    public void Equip(string shoesId) { shoes = Data.GearOf("shoes", shoesId); }
    public static void SetLayer(GameObject g, int l) { foreach (var t in g.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = l; }
    public void Teleport(Vector3 p, float yawDeg) { cc.enabled = false; transform.position = p; cc.enabled = true; yaw = yawDeg; pitch = 0; vy = 0; }
    public bool Locked => Game.I.InputLocked;
    public int Ammo(int s) => s == 0 ? SaveSys.S.cans : SaveSys.S.meats;

    void BuildHand()
    {
        handSel = sel; if (hand) Destroy(hand.gameObject);
        var h = new GameObject("Hand").transform; h.SetParent(vmRoot, false); hand = h;
        var arm = Art.Prim(PrimitiveType.Capsule, h, new Vector3(0.02f, -0.1f, -0.05f), new Vector3(0.07f, 0.16f, 0.07f), Art.Toon(Data.H("ffd2b0")));
        arm.transform.localRotation = Quaternion.Euler(70, 0, 0);
        if (sel == 0) { Art.Prim(PrimitiveType.Cylinder, h, new Vector3(0, 0.03f, 0.04f), new Vector3(0.08f, 0.035f, 0.08f), Art.Toon(Data.H("f2c14e"))); Art.Prim(PrimitiveType.Cylinder, h, new Vector3(0, 0.03f, 0.04f), new Vector3(0.082f, 0.02f, 0.082f), Art.Toon(Data.H("ff7a8a"))); }
        else { Art.Prim(PrimitiveType.Sphere, h, new Vector3(0, 0.04f, 0.05f), new Vector3(0.1f, 0.08f, 0.13f), Art.Toon(Data.H("d9553c"))); Art.Prim(PrimitiveType.Cylinder, h, new Vector3(0, 0.04f, 0.13f), new Vector3(0.025f, 0.05f, 0.025f), Art.Toon(Data.H("fff5e6"))).transform.localRotation = Quaternion.Euler(90, 0, 0); }
        foreach (var c in h.GetComponentsInChildren<Collider>()) Destroy(c);
        foreach (var r in h.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        SetLayer(h.gameObject, Art.VMLayer);
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (Input.GetKeyDown(KeyCode.F8) && Game.I) Game.I.Toast(AAQuality.Cycle(cam));
        if (!alive || Riding) return;
        bool canAct = controlEnabled && !Locked;
        if (canAct) { yaw += TouchIn.Look("Mouse X") * Sens; pitch = Mathf.Clamp(pitch - TouchIn.Look("Mouse Y") * Sens, -85, 85); }
        shake = Mathf.Max(0, shake - dt * 3);
        float sx = (Mathf.PerlinNoise(Time.time * 25, 0) - 0.5f) * shake, sy = (Mathf.PerlinNoise(0, Time.time * 25) - 0.5f) * shake;
        transform.rotation = Quaternion.Euler(0, yaw, 0);
        cryT = Mathf.Max(0, cryT - dt);
        Vector3 mv = Vector3.zero; bool sprint = false;
        if (canAct)
        {
            mv = new Vector3(TouchIn.Axis("Horizontal"), 0, TouchIn.Axis("Vertical")); if (mv.sqrMagnitude > 1) mv.Normalize();
            sprint = TouchIn.Held(KeyCode.LeftShift) && mv.z > 0.1f && !Crying;
        }
        Sprinting = sprint && mv.sqrMagnitude > 0.1f;
        float spd = (sprint ? Run : Walk) * (shoes != null ? shoes.spd : 1) * (Crying ? 0.55f : 1);
        var vel = transform.TransformDirection(mv) * spd;
        if (cc.isGrounded) { vy = -1f; if (canAct && TouchIn.Down(KeyCode.Space)) vy = 4.4f; } else vy -= 14f * dt;
        vel.y = vy; cc.Move(vel * dt);
        float hs = new Vector2(vel.x, vel.z).magnitude;
        if (cc.isGrounded && hs > 0.5f)
        {
            bobT += dt * hs * 2.1f; stepT += dt * hs;
            if (stepT > (sprint ? 1.7f : 1.4f)) { stepT = 0; Footstep(); }
            noiseT -= dt; if (inRaid && noiseT <= 0) { noiseT = 0.4f; Pet.Noise(transform.position, Sprinting ? 10f : 4f); }
        }
        else bobT = Mathf.Lerp(bobT, Mathf.Round(bobT / Mathf.PI) * Mathf.PI, dt * 5);
        float bob = Mathf.Sin(bobT * 2) * 0.04f * Mathf.Clamp01(hs / 4), bobX = Mathf.Cos(bobT) * 0.03f * Mathf.Clamp01(hs / 4);
        float cry = Crying ? Mathf.Sin(Time.time * 9) * 0.6f : 0;
        camRig.localPosition = new Vector3(bobX * 0.4f, Eye + bob, 0);
        camRig.localRotation = Quaternion.Euler(pitch + sy + cry, sx, Mathf.Sin(bobT) * 0.5f * Mathf.Clamp01(hs / 4) + cry);
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, Fov + (sprint ? 5 : 0), dt * 10);
        // hand / throw
        if (canAct)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) sel = 0; if (Input.GetKeyDown(KeyCode.Alpha2)) sel = 1;
            float w = Input.mouseScrollDelta.y; if (Mathf.Abs(w) > 0.1f) sel = 1 - sel;
            throwT -= dt; if (inRaid && TouchIn.Fire() && throwT <= 0) Throw();
        }
        if (handSel != sel || !hand) BuildHand();
        throwAnim = Mathf.MoveTowards(throwAnim, 0, dt * 3.5f);
        bool show = inRaid && Ammo(sel) > 0 && !Locked;
        if (hand) hand.gameObject.SetActive(show);
        vmRoot.localPosition = Vector3.Lerp(vmRoot.localPosition, new Vector3(0.28f + bobX * 0.6f, -0.24f + bob * 0.6f - (sprint ? 0.05f : 0), 0.45f + throwAnim * 0.15f), dt * 16);
        vmRoot.localRotation = Quaternion.Euler(-throwAnim * 60, sprint ? -15 : 0, 0);
        Art.Hurt(Crying ? 0.3f + 0.1f * Mathf.Sin(Time.time * 4) : 0);
    }

    public void Throw()
    {
        var S = SaveSys.S;
        if (Ammo(sel) <= 0) { Game.I.Toast(sel == 0 ? "罐頭用完了！回房間用撲滿買。" : "肉肉用完了！回房間用撲滿買。"); Sfx.Play("error_004", 0.5f); throwT = 0.4f; return; }
        if (sel == 0) S.cans--; else S.meats--;
        throwT = 0.5f; throwAnim = 1; Sfx.Play("whoosh", 0.6f, 1.3f);
        var o = cam.transform.position + cam.transform.forward * 0.5f;
        Lure.Spawn(sel, o, cam.transform.forward * 9.5f + Vector3.up * 3.2f);
    }

    void Footstep()
    {
        string surf = "concrete";
        if (Physics.Raycast(transform.position + Vector3.up * 0.3f, Vector3.down, out var h, 1f)) { var n = h.collider.name; if (n.Contains("wood") || n.Contains("floor")) surf = "wood"; else if (n.Contains("grass")) surf = "grass"; }
        Sfx.PlayRand("footstep_" + surf + "_", 5, Sprinting ? 0.4f : 0.25f, 1.15f);
    }

    /// a cat/dog caught us
    public void Caught(Pet by)
    {
        caught++; shake = Mathf.Max(shake, 1.0f); Sfx.Play("impactSoft_heavy_000", 0.8f);
        if (caught % 3 == 0) { cryT = 7f; Sfx.Play("cry", 0.9f); Game.I.Toast("嗚哇——被抓到三次，哭哭了！（7 秒內跑不快）"); }
    }
    public void Shake(float s) { shake = Mathf.Max(shake, s); }
}

/// 反鋸齒 低/中/高 — MSAA (covers inverted-hull outlines) + SMAA post. F8 cycles; saved in PlayerPrefs "aa".
public static class AAQuality
{
    public static readonly string[] Names = { "低", "中", "高" };
    public static int Level = -1;
    static int Default => Application.platform == RuntimePlatform.WebGLPlayer ? 1 : 2;
    public static void Apply(Camera cam)
    {
        if (Level < 0) Level = Mathf.Clamp(PlayerPrefs.GetInt("aa", Default), 0, 2);
        var urp = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (urp) { urp.msaaSampleCount = Level == 0 ? 1 : (Level == 1 ? 2 : 4); urp.renderScale = 1f; }
        if (cam)
        {
            cam.allowMSAA = Level > 0;
            var cd = cam.GetUniversalAdditionalCameraData();
            cd.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            cd.antialiasingQuality = Level == 0 ? AntialiasingQuality.Medium : AntialiasingQuality.High;
        }
    }
    public static string Cycle(Camera cam) { Level = (Level + 1) % 3; PlayerPrefs.SetInt("aa", Level); PlayerPrefs.Save(); Apply(cam); return "反鋸齒：" + Names[Level]; }
}
