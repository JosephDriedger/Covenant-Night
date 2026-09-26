using UnityEngine;

// Marks where Jonathan and David appear when a zone loads (the GDD's "ZoneEntry"), plus the zone's
// display name and David's marked Run waypoints. One per zone scene.
public class ZoneEntry : MonoBehaviour
{
    public string displayName = "Zone";
    public string subtitle;
    public Transform jonathanSpawn;
    public Transform davidSpawn;

    [Tooltip("David's 'Run' command sprints to the next of these, in order.")]
    public Transform[] davidRunWaypoints;

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        if (jonathanSpawn != null) Gizmos.DrawWireSphere(jonathanSpawn.position, 0.5f);
        Gizmos.color = Color.cyan;
        if (davidSpawn != null) Gizmos.DrawWireSphere(davidSpawn.position, 0.5f);
        Gizmos.color = Color.yellow;
        if (davidRunWaypoints != null)
            foreach (var w in davidRunWaypoints)
                if (w != null) Gizmos.DrawWireCube(w.position + Vector3.up * 0.5f, new Vector3(0.8f, 1f, 0.8f));
    }
}
