using UnityEngine;

// Place on clay pots / baskets in the level.
// Player picks up by walking over (trigger collider).
public class StonePickup : MonoBehaviour
{
    public int stoneCount = 1;

    void OnTriggerEnter(Collider other)
    {
        var abilities = other.GetComponent<PlayerAbilities>();
        if (abilities == null) return;

        abilities.AddStones(stoneCount);
        Destroy(gameObject);
    }
}
