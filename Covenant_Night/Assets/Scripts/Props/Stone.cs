using UnityEngine;

// Thrown stone projectile. Emits a sound event on landing (guards investigate the impact point).
[RequireComponent(typeof(Rigidbody))]
public class Stone : MonoBehaviour
{
    [Tooltip("Noise radius emitted when the stone lands.")]
    public float noiseRadius = 10f;

    [Tooltip("Seconds before the stone self-destructs if it never lands.")]
    public float lifetime = 6f;

    public AudioSource audioSource;
    public AudioClip   clatter;

    bool _landed;

    void Start() => Destroy(gameObject, lifetime);

    void OnCollisionEnter(Collision col)
    {
        if (_landed) return;
        // Ignore the characters (the thrower / David); guards and world geometry count as landing
        if (col.gameObject.layer == GameLayers.Characters) return;

        _landed = true;
        AudioEventSystem.Emit(transform.position, noiseRadius);

        if (audioSource != null && clatter != null) audioSource.PlayOneShot(clatter);

        // Keep the stone (and its sound) around briefly, then remove
        var r = GetComponentInChildren<Renderer>();
        Destroy(gameObject, 1.2f);
        if (r != null) r.material.color = new Color(0.45f, 0.42f, 0.38f);
    }
}
