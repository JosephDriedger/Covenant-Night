using UnityEngine;

// Dropped decoy prop: unlike a thrown Stone (single landing clatter), this re-emits noise on a
// loop for a few seconds, holding a guard's attention on the spot instead of a one-off sound.
public class Decoy : MonoBehaviour
{
    [Tooltip("Noise radius emitted on every pulse.")]
    public float noiseRadius = 9f;

    [Tooltip("Seconds between noise pulses.")]
    public float pulseInterval = 2f;

    [Tooltip("Total seconds before the decoy burns out and is removed.")]
    public float duration = 8f;

    public AudioSource audioSource;
    public AudioClip   whistle;

    float _age;
    float _pulseTimer;

    void Start() => Pulse();

    void Update()
    {
        _age += Time.deltaTime;
        if (_age >= duration) { Destroy(gameObject); return; }

        _pulseTimer -= Time.deltaTime;
        if (_pulseTimer <= 0f) Pulse();
    }

    void Pulse()
    {
        _pulseTimer = pulseInterval;
        AudioEventSystem.Emit(transform.position, noiseRadius);
        if (audioSource != null && whistle != null) audioSource.PlayOneShot(whistle);
    }
}
