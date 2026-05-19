using UnityEngine;

// Performs 3D cone line-of-sight detection via Physics.Raycast.
// Attach to the guard's eye point Transform, or the guard root (set eyePoint manually).
public class GuardVision : MonoBehaviour
{
    [Header("Detection Shape")]
    [Range(10f, 120f)] public float coneAngle   = 60f;
    public float maxRange  = 12f;
    public float checkRate = 0.1f;   // seconds between checks

    [Header("Layers")]
    public LayerMask obstacleMask;   // walls, environment geometry
    public LayerMask targetMask;     // Player, David

    [Header("References")]
    public Transform eyePoint;       // where the ray originates (guard's head)

    // Set by GuardFSM; vision is suppressed while wall-pressed
    public bool IsWallPressImmune { get; set; }

    public Transform LastSpottedTarget { get; private set; }

    GuardFSM _fsm;
    float _timer;

    void Awake()
    {
        _fsm = GetComponentInParent<GuardFSM>();
        if (eyePoint == null) eyePoint = transform;
    }

    void Update()
    {
        _timer -= Time.deltaTime;
        if (_timer > 0f) return;
        _timer = checkRate;
        CheckForTargets();
    }

    void CheckForTargets()
    {
        // Find all potential targets in max range
        Collider[] hits = Physics.OverlapSphere(eyePoint.position, maxRange, targetMask);
        foreach (var hit in hits)
        {
            Transform t = hit.transform;
            Vector3 dirToTarget = (t.position - eyePoint.position);

            // Cone angle check
            if (Vector3.Angle(eyePoint.forward, dirToTarget) > coneAngle * 0.5f) continue;

            // Wall-press suppression: Jonathan is nearly invisible while pressed + stationary
            var pc = t.GetComponent<PlayerController>();
            if (pc != null && pc.IsWallPressed && pc.Velocity.magnitude < 0.1f)
            {
                // Only visible if guard is very close
                if (dirToTarget.magnitude > 2f) continue;
            }

            // Line-of-sight raycast
            if (Physics.Raycast(eyePoint.position, dirToTarget.normalized,
                    out RaycastHit rayHit, maxRange, obstacleMask | targetMask))
            {
                if (((1 << rayHit.collider.gameObject.layer) & targetMask) != 0)
                {
                    LastSpottedTarget = t;
                    _fsm?.OnTargetSpotted(t);
                    return;
                }
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (eyePoint == null) return;
        Gizmos.color = Color.yellow;
        Vector3 leftDir  = Quaternion.Euler(0, -coneAngle * 0.5f, 0) * eyePoint.forward;
        Vector3 rightDir = Quaternion.Euler(0,  coneAngle * 0.5f, 0) * eyePoint.forward;
        Gizmos.DrawRay(eyePoint.position, leftDir  * maxRange);
        Gizmos.DrawRay(eyePoint.position, rightDir * maxRange);
        Gizmos.DrawRay(eyePoint.position, eyePoint.forward * maxRange);
    }
}
