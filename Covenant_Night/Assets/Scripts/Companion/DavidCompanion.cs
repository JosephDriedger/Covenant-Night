using System.Collections;
using UnityEngine;
using UnityEngine.AI;

// Controls David's NavMeshAgent, three follow modes, and the Harp ability.
// Tag the David GameObject "David" so guards can identify him on detection.
[RequireComponent(typeof(NavMeshAgent))]
public class DavidCompanion : MonoBehaviour
{
    public enum Mode { Follow, Wait, Run }

    [Header("Follow")]
    public Transform followTarget;          // Jonathan's Transform
    public float     followStopDistance = 2f;

    [Header("Run")]
    public Transform runWaypoint;           // next zone exit waypoint; set per zone

    [Header("Speeds")]
    public float followSpeed = 2.8f;
    public float runSpeed    = 5f;

    [Header("Harp Ability")]
    public float harpCalmRadius = 8f;
    public float harpCooldown   = 60f;      // one use per zone enforced in ZoneManager

    NavMeshAgent _agent;
    Mode         _mode = Mode.Follow;
    float        _harpCooldownTimer;
    bool         _harpUsedThisZone;

    void Awake() => _agent = GetComponent<NavMeshAgent>();

    void Start()
    {
        _agent.speed = followSpeed;
        SetMode(Mode.Follow);
    }

    void Update()
    {
        if (GameManager.Instance.IsPaused) return;

        switch (_mode)
        {
            case Mode.Follow: UpdateFollow(); break;
            case Mode.Wait:   break;           // agent stopped; do nothing
            case Mode.Run:    UpdateRun();     break;
        }

        if (InputReader.Instance.HarpPressed) TryHarp();
    }

    // ── Mode Updates ────────────────────────────────────────────────────────

    void UpdateFollow()
    {
        if (followTarget == null) return;
        float dist = Vector3.Distance(transform.position, followTarget.position);
        if (dist > followStopDistance)
            _agent.SetDestination(followTarget.position);
        else
            _agent.ResetPath();
    }

    void UpdateRun()
    {
        if (runWaypoint == null) return;
        if (_agent.remainingDistance < 0.5f)
            SetMode(Mode.Follow); // reached waypoint; resume following
    }

    // ── Public API ──────────────────────────────────────────────────────────

    public void SetMode(Mode mode)
    {
        _mode = mode;
        switch (mode)
        {
            case Mode.Follow:
                _agent.isStopped = false;
                _agent.speed     = followSpeed;
                break;
            case Mode.Wait:
                _agent.ResetPath();
                _agent.isStopped = true;
                break;
            case Mode.Run:
                if (runWaypoint == null) { SetMode(Mode.Follow); return; }
                _agent.isStopped = false;
                _agent.speed     = runSpeed;
                _agent.SetDestination(runWaypoint.position);
                break;
        }
        HUD.Instance?.UpdateDavidMode(mode.ToString());
    }

    // ── Harp Ability ─────────────────────────────────────────────────────────

    void TryHarp()
    {
        if (_harpUsedThisZone) return;

        // Calm all Suspicious guards in radius that don't have LOS on David
        var guards = FindObjectsByType<GuardFSM>(FindObjectsSortMode.None);
        bool calmedAny = false;
        foreach (var g in guards)
        {
            if (g.State != GuardState.Suspicious) continue;
            if (Vector3.Distance(transform.position, g.transform.position) > harpCalmRadius) continue;

            // Check guard does NOT have direct LOS on David
            Vector3 dir = transform.position - g.transform.position;
            if (!Physics.Raycast(g.transform.position + Vector3.up, dir.normalized,
                    dir.magnitude, ~LayerMask.GetMask("Characters")))
            {
                g.Calm();
                calmedAny = true;
            }
        }

        if (calmedAny)
        {
            _harpUsedThisZone = true;
            HUD.Instance?.ShowHarpUsed();
        }
    }

    public void ResetForZone()
    {
        _harpUsedThisZone = false;
        SetMode(Mode.Follow);
    }
}
