using UnityEngine;

// Container for a guard's patrol waypoints.
// Add this to an empty GameObject; place child Transforms as waypoints.
public class PatrolPath : MonoBehaviour
{
    public Transform[] waypoints;

    public Transform GetWaypoint(int index) =>
        waypoints[index % waypoints.Length];

    public int Length => waypoints.Length;

    void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Length < 2) return;
        Gizmos.color = Color.cyan;
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null) continue;
            Gizmos.DrawSphere(waypoints[i].position, 0.15f);
            var next = waypoints[(i + 1) % waypoints.Length];
            if (next != null) Gizmos.DrawLine(waypoints[i].position, next.position);
        }
    }
}
