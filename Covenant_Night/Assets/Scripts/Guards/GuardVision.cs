using UnityEngine;

// Performs 3D cone line-of-sight detection via Physics.Raycast, every `checkRate` seconds.
//
// The check (per target — Jonathan and David):
//   1. within an effective range (max range, scaled by torch light, crouching, sprinting)
//   2. inside the horizontal cone (plus a vertical limit for rooftop sentries)
//   3. Physics.Raycast from the eye to the target's collider — head and chest samples — using a mask that
//      blocks on Walls/Roof and hits on Characters
// Jonathan fills an "awareness" meter (faster when close, lit, or sprinting); David is a confirmed
// sighting immediately ("David cannot bluff his way out").
public class GuardVision : MonoBehaviour
{
    [Header("Detection Shape")]
    [Range(10f, 170f)] public float coneAngle = 70f;
    public float maxRange  = 11f;
    public float checkRate = 0.1f;   // seconds between checks

    [Header("Layers")]
    public LayerMask obstacleMask;   // walls, roofs
    public LayerMask targetMask;     // Characters

    [Header("References")]
    public Transform eyePoint;       // where the ray originates (guard's head)

    [Header("Awareness")]
    public float awarenessRate      = 1f;      // base fill per second
    public float gainMultiplier     = 1f;      // Commander is much quicker to confirm
    public float awarenessDecay     = 0.35f;   // per second while nothing is seen
    [Range(0.05f, 0.95f)] public float suspicionThreshold = 0.35f;
    public float verticalLimit      = 55f;
    public float closeNoticeRange   = 1.0f;    // anything this close is noticed regardless of cone
    public float hiddenNoticeRange  = 1.3f;    // a target in a hiding spot is only found this close
    public float wallPressRange     = 2.0f;    // a wall-pressed, motionless Jonathan is only seen this close

    public float Awareness        { get; private set; }
    public Transform LastSpottedTarget { get; private set; }
    public Vector3 LastSeenPosition    { get; private set; }
    public float TimeSinceSeen         { get; private set; } = 999f;
    // Base values scaled by the chosen difficulty.
    public float EffectiveRange => maxRange * GameDifficulty.Tuning.vision;
    public float EffectiveCone  => Mathf.Min(170f, coneAngle * GameDifficulty.Tuning.cone);

    public bool  SeesTarget            { get; private set; }
    public bool  SeesPlayer            { get; private set; }   // per target: a hidden target counts as seen only within hiddenNoticeRange
    public bool  SeesDavid             { get; private set; }

    GuardFSM _fsm;
    float _accum;

    void Awake()
    {
        _fsm = GetComponentInParent<GuardFSM>();
        if (eyePoint == null) eyePoint = transform;
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsPaused) return;

        _accum += Time.deltaTime;
        if (_accum < checkRate) return;
        float step = _accum;
        _accum = 0f;
        Tick(step);
    }

    void Tick(float dt)
    {
        bool seen = false, seenPlayer = false, seenDavid = false;

        var pc = PlayerController.Instance;
        if (pc != null && Evaluate(pc.transform, pc.IsHidden, pc.IsWallPressed && pc.IsStationary,
                pc.IsCrouching, pc.IsSprinting, out Vector3 pos, out float dist, out float range))
        {
            seen = seenPlayer = true;
            LastSpottedTarget = pc.transform;
            LastSeenPosition  = pos;
            TimeSinceSeen     = 0f;

            float vis  = Torch.VisibilityAt(pc.transform.position);
            float gain = awarenessRate * gainMultiplier * GameDifficulty.Tuning.gain
                         * Mathf.Lerp(1.6f, 0.4f, Mathf.Clamp01(dist / Mathf.Max(0.01f, range)))
                         * Mathf.Lerp(0.35f, 1f, vis);
            if (pc.IsWallPressed) gain *= 0.5f;
            if (pc.IsSprinting)   gain *= 1.4f;
            Awareness = Mathf.Min(1f, Awareness + gain * dt);
            _fsm?.OnSighting(pc.transform, pos, Awareness);
        }

        var dv = DavidCompanion.Instance;
        if (dv != null && Evaluate(dv.transform, dv.IsHidden, false, dv.IsCrouching, false,
                out Vector3 dpos, out _, out _))
        {
            seen = seenDavid = true;
            LastSpottedTarget = dv.transform;
            LastSeenPosition  = dpos;
            TimeSinceSeen     = 0f;
            Awareness         = 1f;
            _fsm?.OnSighting(dv.transform, dpos, 1f);
        }

        SeesTarget = seen;
        SeesPlayer = seenPlayer;
        SeesDavid  = seenDavid;
        if (!seen)
        {
            TimeSinceSeen += dt;
            Awareness = Mathf.Max(0f, Awareness - awarenessDecay * GameDifficulty.Tuning.decay * dt);
        }
    }

    // Public LOS test used by the harp ("must not have direct sight of David") and the Commander.
    public bool CanSee(Transform t)
    {
        if (t == null) return false;
        var pc = t.GetComponent<PlayerController>();
        var dv = t.GetComponent<DavidCompanion>();
        bool hidden   = pc != null ? pc.IsHidden : dv != null && dv.IsHidden;
        bool crouched = pc != null ? pc.IsCrouching : dv != null && dv.IsCrouching;
        return Evaluate(t, hidden, false, crouched, false, out _, out _, out _);
    }

    bool Evaluate(Transform t, bool hidden, bool wallPressedStill, bool crouching, bool sprinting,
                  out Vector3 seenPosition, out float distance, out float effectiveRange)
    {
        seenPosition = t.position;
        distance = 0f;
        effectiveRange = 0f;

        var col = t.GetComponent<Collider>();
        Vector3 center = col != null ? col.bounds.center : t.position + Vector3.up;
        Vector3 head   = col != null ? new Vector3(center.x, col.bounds.max.y - 0.12f, center.z) : center + Vector3.up * 0.4f;
        seenPosition = t.position;

        Vector3 eye  = eyePoint.position;
        Vector3 flat = center - eye;
        distance = flat.magnitude;

        // Effective range: torch light widens it, shadow / crouching shrink it
        float vis = Torch.VisibilityAt(t.position);
        effectiveRange = EffectiveRange * Mathf.Lerp(0.6f, 1f, vis) * (crouching ? 0.75f : 1f) * (sprinting ? 1.15f : 1f);
        if (hidden)           effectiveRange = hiddenNoticeRange;
        else if (wallPressedStill) effectiveRange = Mathf.Min(effectiveRange, wallPressRange);

        if (distance > effectiveRange) return false;

        // Cone (horizontal) + vertical limit; very close targets are always noticed
        Vector3 h = flat; h.y = 0f;
        Vector3 f = eyePoint.forward; f.y = 0f;
        if (distance > closeNoticeRange)
        {
            if (h.sqrMagnitude > 0.0001f && f.sqrMagnitude > 0.0001f && Vector3.Angle(f, h) > EffectiveCone * 0.5f) return false;
            float vAng = Mathf.Atan2(Mathf.Abs(flat.y), h.magnitude) * Mathf.Rad2Deg;
            if (vAng > verticalLimit) return false;
        }

        return RayClear(eye, center, t) || RayClear(eye, head, t);
    }

    bool RayClear(Vector3 eye, Vector3 point, Transform target)
    {
        Vector3 dir = point - eye;
        float len = dir.magnitude;
        if (len < 0.01f) return true;
        if (!Physics.Raycast(eye, dir / len, out RaycastHit hit, len + 0.4f,
                obstacleMask | targetMask, QueryTriggerInteraction.Ignore))
            return false;
        if (((1 << hit.collider.gameObject.layer) & targetMask) == 0) return false;
        return hit.collider.transform == target || hit.collider.transform.IsChildOf(target);
    }

    public void ResetAwareness()
    {
        Awareness = 0f;
        TimeSinceSeen = 999f;
        SeesTarget = SeesPlayer = SeesDavid = false;
    }

    void OnDrawGizmosSelected()
    {
        Transform eye = eyePoint != null ? eyePoint : transform;
        Gizmos.color = Color.yellow;
        Vector3 leftDir  = Quaternion.Euler(0, -coneAngle * 0.5f, 0) * eye.forward;
        Vector3 rightDir = Quaternion.Euler(0,  coneAngle * 0.5f, 0) * eye.forward;
        Gizmos.DrawRay(eye.position, leftDir  * maxRange);
        Gizmos.DrawRay(eye.position, rightDir * maxRange);
        Gizmos.DrawRay(eye.position, eye.forward * maxRange);
    }
}
