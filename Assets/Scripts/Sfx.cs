using System.Collections.Generic;
using UnityEngine;

public class Sfx : MonoBehaviour
{
    public static Sfx I;
    readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
    AudioSource ui, rotor, amb, engine;
    void Awake()
    {
        I = this; ui = gameObject.AddComponent<AudioSource>(); ui.spatialBlend = 0;
        rotor = gameObject.AddComponent<AudioSource>(); rotor.loop = true; rotor.clip = Clip("rotor"); rotor.volume = 0; rotor.spatialBlend = 0;
        engine = gameObject.AddComponent<AudioSource>(); engine.loop = true; engine.clip = Clip("engine"); engine.volume = 0; engine.spatialBlend = 0;
        amb = gameObject.AddComponent<AudioSource>(); amb.loop = true; amb.clip = Clip("wind"); amb.volume = 0.06f; amb.Play();
    }
    public AudioClip Clip(string n) { if (!clips.TryGetValue(n, out var c)) { c = Resources.Load<AudioClip>("Audio/" + n); clips[n] = c; } return c; }
    public static void Play(string n, float vol = 1f, float pitch = 1f) { if (!I) return; var c = I.Clip(n); if (!c) return; I.ui.pitch = pitch; I.ui.PlayOneShot(c, vol); }
    public static void PlayRand(string pre, int count, float vol = 1f, float pm = 1f) { Play(pre + Random.Range(0, count).ToString("000"), vol, Random.Range(0.92f, 1.08f) * pm); }
    public static void Engine(float v, float pitch = 1f) { if (!I || !I.engine) return; if (v > 0 && !I.engine.isPlaying) I.engine.Play(); I.engine.volume = v; I.engine.pitch = pitch; if (v <= 0 && I.engine.isPlaying) I.engine.Stop(); }
    public static void At(string n, Vector3 p, float vol = 1f, float minD = 3f, float maxD = 60f)
    {
        if (!I) return; var c = I.Clip(n); if (!c) return;
        var g = new GameObject("sfx"); g.transform.position = p; var a = g.AddComponent<AudioSource>(); a.clip = c; a.spatialBlend = 1; a.volume = vol; a.minDistance = minD; a.maxDistance = maxD; a.rolloffMode = AudioRolloffMode.Linear; a.pitch = Random.Range(0.94f, 1.06f); a.Play(); Destroy(g, c.length + 0.2f);
    }
    public static void Rotor(float v, float pitch = 1f) { if (!I) return; if (v > 0 && !I.rotor.isPlaying) I.rotor.Play(); I.rotor.volume = v; I.rotor.pitch = pitch; if (v <= 0) I.rotor.Stop(); }
    public static void Reveal(int r) => Play("reveal" + r, 0.9f);
}
