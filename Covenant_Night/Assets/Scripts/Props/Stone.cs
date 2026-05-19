using UnityEngine;

// Thrown stone projectile. Emits a sound event on landing and destroys itself.
[RequireComponent(typeof(Rigidbody))]
public class Stone : MonoBehaviour
{
    [Tooltip("Noise radius emitted when the stone lands.")]
    public float noiseRadius = 10f;

    [Tooltip("Seconds before the stone self-destructs if it never lands.")]
    public float lifetime = 5f;

    void Start() => Destroy(gameObject, lifetime);

    void OnCollisionEnter(Collision col)
    {
        // Don't trigger off the thrower immediately
        if (col.gameObject.CompareTag("Player")) return;

        AudioEventSystem.Emit(transform.position, noiseRadius);
        Destroy(gameObject);
    }
}
