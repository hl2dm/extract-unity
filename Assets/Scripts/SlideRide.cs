using System.Collections;
using UnityEngine;

/// The park slide: [E] at the ladder -> climb, ride the chute (FOV kick, roll, whoosh), land at the bottom.
/// Pets that were chasing / noticing you lose track while you whizz away.
public class SlideRide : Interactable
{
    public static int Rides; public static bool Busy; public static Vector3 LastLanding;
    public override string Prompt => Busy ? "" : "[E] 上溜滑梯";
    public override Vector3 Point => transform.TransformPoint(new Vector3(0, 1.2f, -1.4f));
    Vector3 L(float y, float z) => transform.TransformPoint(new Vector3(0, y, z));
    public override void Use() { if (!Busy && Player.I && Player.I.alive) StartCoroutine(Ride()); }
    IEnumerator Ride()
    {
        var p = Player.I; Busy = true; Player.Riding = true; p.cc.enabled = false;
        float yaw = transform.eulerAngles.y;
        Vector3 a = p.transform.position, b = L(0, -1.75f), c = L(2.1f, -1.0f), d = L(2.08f, -0.35f), e = L(0.16f, 1.95f), f = L(0, 2.9f);
        float t;
        for (t = 0; t < 1; t += Time.deltaTime / 0.3f) { Pose(p, Vector3.Lerp(a, b, t), Mathf.LerpAngle(p.yaw, yaw, t), Mathf.Lerp(p.pitch, -10, t), 0, 75); yield return null; }
        for (t = 0; t < 1; t += Time.deltaTime / 0.9f)
        {
            if ((int)(t * 6) != (int)((t - Time.deltaTime / 0.9f) * 6)) Sfx.Play("click_002", 0.25f);
            Pose(p, Vector3.Lerp(b, c, t) + Vector3.up * Mathf.Sin(t * 37) * 0.03f, yaw, -10 - 25 * t, 0, 75); yield return null;
        }
        for (t = 0; t < 1; t += Time.deltaTime / 0.25f) { Pose(p, Vector3.Lerp(c, d, t), yaw, Mathf.Lerp(-35, 34, t), 0, 75); yield return null; }
        Sfx.Play("whoosh", 0.9f);
        foreach (var pet in Pet.All) if (pet) pet.LoseTrack();
        for (t = 0; t < 1; t += Time.deltaTime / 1.1f)
        {
            float s = t * t * (2.2f - 1.2f * t); // ease-in, slight ease-out at the end
            Pose(p, Vector3.Lerp(d, e, s), yaw, 34 - 22 * t, Mathf.Sin(t * Mathf.PI * 3) * 6 * (1 - t), Mathf.Lerp(75, 98, Mathf.Sin(t * Mathf.PI * 0.9f))); yield return null;
        }
        for (t = 0; t < 1; t += Time.deltaTime / 0.35f) { Pose(p, Vector3.Lerp(e, f, t) + Vector3.up * Mathf.Sin(t * Mathf.PI) * 0.12f, yaw, Mathf.Lerp(12, 0, t), 0, Mathf.Lerp(90, 75, t)); yield return null; }
        Sfx.Play("confirmation_001", 0.5f);
        Pose(p, f, yaw, 0, 0, 75); p.yaw = yaw; p.pitch = 0;
        p.cc.enabled = true; Player.Riding = false; Busy = false; Rides++; LastLanding = p.transform.position;
    }
    static void Pose(Player p, Vector3 pos, float yaw, float pitch, float roll, float fov)
    {
        bool sit = pitch > 15; // seated on the chute: lower eye so the chute fills the bottom of the view
        p.transform.position = pos; p.yaw = yaw; p.pitch = pitch; p.transform.rotation = Quaternion.Euler(0, yaw, 0);
        p.camRig.localPosition = new Vector3(0, sit ? 0.8f : Player.Eye, 0); p.camRig.localRotation = Quaternion.Euler(pitch, 0, roll);
        if (p.cam) p.cam.fieldOfView = fov;
    }
    void OnDisable() { if (Busy) { Busy = false; Player.Riding = false; if (Player.I) Player.I.cc.enabled = true; } }
}
