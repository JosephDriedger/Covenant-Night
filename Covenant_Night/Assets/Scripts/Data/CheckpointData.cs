using UnityEngine;

// ScriptableObject snapshot of a zone's entry state.
// Create one asset per zone via Assets > Create > CovenantNight > CheckpointData.
[CreateAssetMenu(menuName = "CovenantNight/CheckpointData")]
public class CheckpointData : ScriptableObject
{
    [HideInInspector] public Vector3 jonathanPosition;
    [HideInInspector] public Quaternion jonathanRotation;
    [HideInInspector] public Vector3 davidPosition;
    [HideInInspector] public int stoneCount;

    public void Save(Transform jonathan, Transform david, int stones)
    {
        jonathanPosition = jonathan.position;
        jonathanRotation = jonathan.rotation;
        davidPosition    = david.position;
        stoneCount       = stones;
    }
}
