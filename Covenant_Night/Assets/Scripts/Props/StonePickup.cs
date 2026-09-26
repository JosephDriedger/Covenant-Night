using UnityEngine;

// Place on clay pots / baskets in the level (Trigger collider).
// Jonathan picks stones up by walking over them; the pot bobs slightly so it reads as interactive.
public class StonePickup : MonoBehaviour
{
    public int stoneCount = 1;
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
        abilities.AddStones(stoneCount);
        FloatingText.Spawn(transform.position + Vector3.up * 1.4f, $"+{stoneCount} stone" + (stoneCount > 1 ? "s" : ""), new Color(0.9f, 0.85f, 0.7f), 3f, 1.2f);
        if (audioSource != null && pickupClip != null) AudioSource.PlayClipAtPoint(pickupClip, transform.position, 0.8f);
        Destroy(gameObject);
    }
}
