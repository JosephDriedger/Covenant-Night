using UnityEngine;

// Container for a guard's patrol waypoints.
// Add this to an empty GameObject; place child Transforms as waypoints.
public class PatrolPath : MonoBehaviour
{
    public Transform[] waypoints;

    [Tooltip("Walk out to the last waypoint and back instead of looping (corridor patrols).")]
    public bool pingPong;

    public Transform GetWaypoint(int index) =>
        waypoints[Mathf.Clamp(index, 0, waypoints.Length - 1)];

    public int Length => waypoints == null ? 0 : waypoints.Length;

    public int NearestIndex(Vector3 position)
    {
        int best = 0;
        float bestD = float.MaxValue;
        for (int i = 0; i < Length; i++)
        {
            float d = (waypoints[i].position - position).sqrMagnitude;
            if (d < bestD) { bestD = d; best = i; }
        }
        return best;
    }

    void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Length < 2) return;
        Gizmos.color = Color.cyan;
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null) continue;
            Gizmos.DrawSphere(waypoints[i].position, 0.15f);
            if (pingPong && i == waypoints.Length - 1) continue;
            var next = waypoints[(i + 1) % waypoints.Length];
            if (next != null) Gizmos.DrawLine(waypoints[i].position, next.position);
        }
    }
}
