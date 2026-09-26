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
    public AudioClip   barkClip;

    bool _onCooldown;

    void OnTriggerEnter(Collider other)
    {
        if (_onCooldown) return;
        if (!other.CompareTag("Player") && !other.CompareTag(GameLayers.DavidTag)) return;
        StartCoroutine(Bark());
    }

    IEnumerator Bark()
    {
        _onCooldown = true;
        AudioEventSystem.Emit(transform.position, barkNoiseRadius);
        if (barkSource != null)
        {
            if (barkClip != null) barkSource.PlayOneShot(barkClip);
            else barkSource.Play();
        }
        FloatingText.Spawn(transform.position + Vector3.up * 2f, "Woof!", new Color(1f, 0.7f, 0.4f), 4f, 1.4f);
        yield return new WaitForSeconds(cooldown);
        _onCooldown = false;
    }
}
