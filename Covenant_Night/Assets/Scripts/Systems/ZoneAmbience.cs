using UnityEngine;

// Randomly plays spatial one-shots (distant dog barks, a clatter, a shutter) from fixed emitter points
// around a zone — the "distant dog barks" layer of the ambient soundscape. Each emitter is a full-3D source.
public class ZoneAmbience : MonoBehaviour
{
    public AudioSource[] emitters;      // spatial blend 1.0 sources parked around the zone
    public AudioClip[]   clips;
    public Vector2 intervalRange = new Vector2(6f, 16f);

    float _timer;

    void Start() => _timer = Random.Range(intervalRange.x, intervalRange.y);

    void Update()
    {
        if (emitters == null || emitters.Length == 0 || clips == null || clips.Length == 0) return;
        _timer -= Time.deltaTime;
        if (_timer > 0f) return;
        _timer = Random.Range(intervalRange.x, intervalRange.y);

        var src = emitters[Random.Range(0, emitters.Length)];
        if (src != null) src.PlayOneShot(clips[Random.Range(0, clips.Length)]);
    }
}
