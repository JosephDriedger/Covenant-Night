using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public enum GuardType { Patrol, Sentry, Commander }

public enum GuardState { Unaware, Suspicious, Alarmed }

// Main guard brain. Requires NavMeshAgent, GuardVision, GuardHearing on the same GameObject.
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(GuardVision))]
[RequireComponent(typeof(GuardHearing))]
public class GuardFSM : MonoBehaviour
{
    [Header("Type")]
    public GuardType guardType;

    [Header("Patrol")]
    public PatrolPath patrolPath;
    public float waypointTolerance = 0.4f;
    public float patrolSpeed       = 2f;

    [Header("Sentry Rotation")]
    public float sentryRotateSpeed = 25f;
    public float sentryArcDegrees  = 120f;

    [Header("Suspicious")]
    public float investigateSpeed   = 3.5f;
    public float investigateTimeout = 6f;   // seconds before returning to patrol

    [Header("Alarmed")]
    public float alarmSpeed = 5f;

    [Header("Detection Indicator")]
    public DetectionIndicator indicator;

    NavMeshAgent _agent;
    GuardVision  _vision;
    GuardState   _state;

    int     _waypointIndex;
    Vector3 _investigateTarget;
    float   _stateTimer;
    float   _sentryBaseAngle;
    float   _sentryT;

    void Awake()
    {
        _agent  = GetComponent<NavMeshAgent>();
        _vision = GetComponent<GuardVision>();
        _sentryBaseAngle = transform.eulerAngles.y;
    }

    void Start() => SetState(GuardState.Unaware);

    void Update()
    {
        if (GameManager.Instance.IsPaused) return;

        switch (_state)
        {
            case GuardState.Unaware:    UpdateUnaware();    break;
            case GuardState.Suspicious: UpdateSuspicious(); break;
            case GuardState.Alarmed:    UpdateAlarmed();    break;
        }
    }

    // ── State Transitions ───────────────────────────────────────────────────

    void SetState(GuardState next)
    {
        _state = next;
        _stateTimer = 0f;
        indicator?.SetState(next);

        switch (next)
        {
            case GuardState.Unaware:
                _agent.speed = patrolSpeed;
                _agent.isStopped = (guardType == GuardType.Sentry);
                break;

            case GuardState.Suspicious:
                _agent.speed = investigateSpeed;
                break;

            case GuardState.Alarmed:
                _agent.speed = alarmSpeed;
                AlarmSystem.Instance?.Raise();
                // Commander calms nearby patrol guards so they stay on posts
                if (guardType == GuardType.Commander) CalmNearbyPatrolGuards();
                break;
        }
    }

    // ── Update Routines ─────────────────────────────────────────────────────

    void UpdateUnaware()
    {
        switch (guardType)
        {
            case GuardType.Patrol:
            case GuardType.Commander:
                DoPatrol();
                break;
            case GuardType.Sentry:
                DoSentryRotate();
                break;
        }
    }

    void UpdateSuspicious()
    {
        _stateTimer += Time.deltaTime;

        if (guardType != GuardType.Sentry)
        {
            _agent.SetDestination(_investigateTarget);

            if (_agent.remainingDistance < 0.5f || _stateTimer >= investigateTimeout)
                SetState(GuardState.Unaware);
        }
        else
        {
            // Sentry looks toward sound origin
            Vector3 dir = (_investigateTarget - transform.position).normalized;
            dir.y = 0f;
            if (dir != Vector3.zero)
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation, Quaternion.LookRotation(dir), 60f * Time.deltaTime);

            if (_stateTimer >= investigateTimeout)
            {
                _stateTimer = 0f;
                SetState(GuardState.Unaware);
            }
        }
    }

    void UpdateAlarmed()
    {
        if (_vision.LastSpottedTarget != null)
            _agent.SetDestination(_vision.LastSpottedTarget.position);
    }

    // ── Patrol / Sentry Helpers ─────────────────────────────────────────────

    void DoPatrol()
    {
        if (patrolPath == null || patrolPath.Length == 0) return;
        if (_agent.pathPending) return;

        if (_agent.remainingDistance < waypointTolerance)
        {
            _waypointIndex = (_waypointIndex + 1) % patrolPath.Length;
            _agent.SetDestination(patrolPath.GetWaypoint(_waypointIndex).position);
        }
    }

    void DoSentryRotate()
    {
        _sentryT += Time.deltaTime * sentryRotateSpeed;
        float angle = _sentryBaseAngle + Mathf.Sin(_sentryT * Mathf.Deg2Rad) * (sentryArcDegrees * 0.5f);
        transform.rotation = Quaternion.Euler(0f, angle, 0f);
    }

    // ── External Callbacks ──────────────────────────────────────────────────

    // Called by GuardVision when confirmed LOS on a target
    public void OnTargetSpotted(Transform target)
    {
        bool isDavid = target.CompareTag("David");

        if (_state == GuardState.Alarmed) return;

        if (isDavid)
        {
            SetState(GuardState.Alarmed);
        }
        else
        {
            // Jonathan: if wall-pressed and close, still alarm; otherwise suspicious->alarm progression
            if (_state == GuardState.Suspicious)
                SetState(GuardState.Alarmed);
            else
                BecomeSuspicious(target.position);
        }
    }

    // Called by GuardHearing when a sound event is in range
    public void OnSoundHeard(Vector3 origin)
    {
        if (_state == GuardState.Alarmed) return;

        // Commander investigates; Patrol/Sentry become suspicious
        _investigateTarget = origin;
        if (_state != GuardState.Suspicious)
            SetState(GuardState.Suspicious);
    }

    // Called by DavidCompanion.Harp() to calm this guard
    public void Calm()
    {
        if (_state == GuardState.Alarmed) return;
        if (_state == GuardState.Suspicious)
        {
            AlarmSystem.Instance?.Suppress();
            SetState(GuardState.Unaware);
        }
    }

    void BecomeSuspicious(Vector3 position)
    {
        _investigateTarget = position;
        SetState(GuardState.Suspicious);
    }

    void CalmNearbyPatrolGuards()
    {
        var guards = FindObjectsByType<GuardFSM>(FindObjectsSortMode.None);
        foreach (var g in guards)
        {
            if (g == this) continue;
            if (g.guardType != GuardType.Patrol) continue;
            if (Vector3.Distance(transform.position, g.transform.position) < 15f)
                g.Calm();
        }
    }

    public GuardState State => _state;
}
