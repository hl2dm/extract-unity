using UnityEngine;
public static class FX
{
    public static void Tracer(Vector3 a, Vector3 b, Color c)
    {
        var g = new GameObject("tracer"); var lr = g.AddComponent<LineRenderer>(); lr.useWorldSpace = true; lr.positionCount = 2;
        var dir = (b - a); float len = dir.magnitude; var p0 = a; var p1 = a + dir.normalized * Mathf.Min(len, 6f);
        lr.SetPosition(0, p0); lr.SetPosition(1, p1); lr.startWidth = 0.025f; lr.endWidth = 0.01f; lr.sharedMaterial = Art.Unlit(c); lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        g.AddComponent<Mover>().Init(dir.normalized * 220f, Mathf.Min(0.12f, len / 220f + 0.02f));
    }
    public static void Burst(Vector3 p, Vector3 n, Color c, int count)
    {
        for (int i = 0; i < count; i++)
        {
            var g = Art.Prim(PrimitiveType.Cube, null, p, Vector3.one * Random.Range(0.02f, 0.05f), Art.Lit(c));
            g.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var v = (n + Random.insideUnitSphere * 0.8f).normalized * Random.Range(1.5f, 4.5f);
            g.AddComponent<Mover>().Init(v, Random.Range(0.25f, 0.5f), 9.8f);
        }
    }
    public static void Decal(Vector3 p, Vector3 n, Transform parent)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Quad); Object.Destroy(g.GetComponent<Collider>());
        g.transform.position = p + n * 0.01f; g.transform.rotation = Quaternion.LookRotation(-n); g.transform.localScale = Vector3.one * 0.07f;
        g.GetComponent<Renderer>().sharedMaterial = Art.Lit(new Color(0.08f, 0.08f, 0.08f)); g.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        if (parent) g.transform.SetParent(parent, true); Object.Destroy(g, 20f);
    }
}
public class Mover : MonoBehaviour
{
    Vector3 v; float life, g, t0; Vector3 s0;
    public void Init(Vector3 vel, float l, float grav = 0) { v = vel; life = l; g = grav; t0 = l; s0 = transform.localScale; }
    void Update() { v.y -= g * Time.deltaTime; transform.position += v * Time.deltaTime; life -= Time.deltaTime; if (g > 0) transform.localScale = s0 * Mathf.Clamp01(life / t0); if (life <= 0) Destroy(gameObject); }
}
