using UnityEngine;

// Attach to the Jonathan GameObject alongside PlayerController.
// PlayerController calls TriggerStep() at its footstep cadence instead of
// playing footstepSource directly, gaining surface-type clip selection.
public class FootstepAudio : MonoBehaviour
{
    [System.Serializable]
    public struct SurfaceAudio
    {
        public string label;        // editor label only (Stone, Dirt, Wood…)
        public LayerMask layer;
        public AudioClip[] clips;
    }

    [Tooltip("Per-surface clip banks. Checked in order; first match wins.")]
    public SurfaceAudio[] surfaces;

    [Tooltip("Fallback clips when no surface layer matches.")]
    public AudioClip[] defaultClips;

    public AudioSource audioSource;

    static readonly float RayLength = 0.4f;

    public void TriggerStep()
    {
        if (audioSource == null) return;

        AudioClip[] bank = PickBank();
        if (bank == null || bank.Length == 0) return;

        audioSource.PlayOneShot(bank[Random.Range(0, bank.Length)]);
    }

    AudioClip[] PickBank()
    {
        if (Physics.Raycast(transform.position + Vector3.up * 0.1f,
                Vector3.down, out RaycastHit hit, RayLength))
        {
            int hitLayer = hit.collider.gameObject.layer;
            foreach (var s in surfaces)
            {
                if ((s.layer.value & (1 << hitLayer)) != 0)
                    return s.clips;
            }
        }
        return defaultClips;
    }
}
