using UnityEngine;

// ScriptableObject snapshot of a zone's entry state (session-only checkpoint; no save/load across sessions).
// Create one asset per zone via Assets > Create > CovenantNight > CheckpointData.
[CreateAssetMenu(menuName = "CovenantNight/CheckpointData")]
public class CheckpointData : ScriptableObject
{
    [HideInInspector] public Vector3 jonathanPosition;
    [HideInInspector] public Quaternion jonathanRotation = Quaternion.identity;
    [HideInInspector] public Vector3 davidPosition;
    [HideInInspector] public int stoneCount;
    [HideInInspector] public bool hasData;

    public void Save(Transform jonathan, Transform david, int stones)
    {
        jonathanPosition = jonathan.position;
        jonathanRotation = jonathan.rotation;
        davidPosition    = david.position;
        stoneCount       = stones;
        hasData          = true;
    }
}
