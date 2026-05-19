using System.Collections;
using UnityEngine;

// Place on a trigger collider around a dog pen (Zone 3 – Potter's Alley).
// Barks and emits a loud noise event when Jonathan or David enters the area.
public class DogTrigger : MonoBehaviour
{
    [Tooltip("Noise radius emitted when the dogs bark.")]
    public float barkNoiseRadius = 15f;

    [Tooltip("Seconds before the trigger can fire again.")]
    public float cooldown = 4f;

    public AudioSource barkSource;

    bool _onCooldown;

    void OnTriggerEnter(Collider other)
    {
        if (_onCooldown) return;
        if (!other.CompareTag("Player") && !other.CompareTag("David")) return;
        StartCoroutine(Bark());
    }

    IEnumerator Bark()
    {
        _onCooldown = true;
        AudioEventSystem.Emit(transform.position, barkNoiseRadius);
        barkSource?.Play();
        yield return new WaitForSeconds(cooldown);
        _onCooldown = false;
    }
}
