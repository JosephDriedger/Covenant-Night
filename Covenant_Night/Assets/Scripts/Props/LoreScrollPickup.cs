using UnityEngine;

// Place on a scroll/note prop in the level (Trigger collider). Purely narrative: reading it has no
// effect on stealth or resources. Jonathan picks it up by walking over it, same shape as StonePickup.
public class LoreScrollPickup : MonoBehaviour
{
    public StoryBeatData lore;
    public AudioSource audioSource;
    public AudioClip   pickupClip;
    public Transform   bobTarget;

    Vector3 _basePos;
    float   _seed;
    bool    _taken;

    void Start()
    {
        _seed = Random.value * 10f;
        if (bobTarget != null) _basePos = bobTarget.localPosition;
    }

    void Update()
    {
        if (bobTarget != null)
            bobTarget.localPosition = _basePos + Vector3.up * (Mathf.Sin(Time.time * 2f + _seed) * 0.03f);
    }

    void OnTriggerEnter(Collider other)
    {
        if (_taken) return;
        var abilities = other.GetComponent<PlayerAbilities>();
        if (abilities == null) return;

        _taken = true;
        abilities.CollectLore(lore);
        FloatingText.Spawn(transform.position + Vector3.up * 1.4f, "Lore Found", new Color(0.85f, 0.8f, 0.6f), 3f, 1.2f);
        if (audioSource != null && pickupClip != null) AudioSource.PlayClipAtPoint(pickupClip, transform.position, 0.8f);
        Destroy(gameObject);
    }
}
