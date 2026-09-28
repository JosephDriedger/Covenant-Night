using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum GuardType { Patrol, Sentry, Commander }

public enum GuardState { Unaware, Suspicious, Alarmed }

// Main guard brain: a single FSM class parameterised by guard type.
//   Unaware    – follows routine (patrol loop / sentry sweep)
//   Suspicious – investigates a sighting or sound, looks around, then returns to routine
//   Alarmed    – confirmed sighting: the whole zone is alerted (AlarmSystem), guards converge and can capture
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(GuardVision))]
[RequireComponent(typeof(GuardHearing))]
public class GuardFSM : MonoBehaviour
{
    public static readonly List<GuardFSM> All = new List<GuardFSM>();

    [Header("Type")]
    public GuardType guardType;

    [Header("Patrol")]
    public PatrolPath patrolPath;
    public float waypointTolerance = 0.6f;
    public float waypointPause     = 1.0f;
    public float patrolSpeed       = 2f;

    [Header("Sentry Rotation")]
    public float sentryRotateSpeed = 25f;    // phase speed of the sweep (deg/s)
    public float sentryArcDegrees  = 100f;   // total arc

    [Header("Suspicious")]
    public float investigateSpeed   = 3.5f;
    public float investigateTimeout = 12f;   // seconds before giving up and returning to routine
    public float lookAroundTime     = 3f;    // time spent scanning at the investigation point

    [Header("Alarmed")]
    public float alarmSpeed     = 5.2f;
    public float catchDistance  = 1.5f;

    [Header("Commander")]
    public float commanderCalmRadius = 10f;  // Commander calms nearby Suspicious Patrol guards

    [Header("Detection Indicator")]
    public DetectionIndicator indicator;

    NavMeshAgent _agent;
    GuardVision  _vision;
    GuardState   _state;

    int     _waypointIndex;
    int     _dir = 1;
    bool    _needDestination = true;
    bool    _waiting;
    float   _waitTimer;

    Vector3 _investigateTarget;
    Vector3 _alarmPosition;
    bool    _arrived;
    float   _lookTimer;
    float   _lookBaseYaw;

    float   _stateTimer;
    float   _sentryBaseAngle;
    float   _sentryT;
    float   _repathTimer;
    bool    _scanning;          // alarmed guard searching at the last known position

    // Hardcore guards check the hiding spots near where they lost you.
    readonly List<Vector3> _search = new List<Vector3>();
    bool    _searchBuilt;
    bool    _hasSearchDest;
    Vector3 _searchDest;
    float   _commanderTimer;
    bool    _pausedApplied;

    public GuardState State     => _state;
    public float StateTime      => _stateTimer;
    public GuardVision Vision   => _vision;
    public float Awareness      => _vision != null ? _vision.Awareness : 0f;

    bool AgentReady => _agent != null && _agent.enabled && _agent.isOnNavMesh;

    void OnEnable()  => All.Add(this);
    void OnDisable() => All.Remove(this);

    void Awake()
    {
        _agent  = GetComponent<NavMeshAgent>();
        _vision = GetComponent<GuardVision>();
        _sentryBaseAngle = transform.eulerAngles.y;
        _sentryT = Random.value * 360f;
    }

    void Start()
    {
        if (guardType == GuardType.Sentry)
        {
            _agent.enabled = false;              // sentries never leave their post
        }
        else
        {
            // Agents are saved disabled so they don't try to register before the zone's NavMesh data is loaded.
            _agent.enabled = true;
            _agent.Warp(transform.position);
            if (!_agent.isOnNavMesh)
                Debug.LogWarning($"[GuardFSM] {name} is not on the NavMesh at {transform.position}.", this);
            _agent.angularSpeed  = 220f;
            _agent.acceleration  = 14f;
            _agent.stoppingDistance = 0.1f;
        }
        SetState(GuardState.Unaware);
    }

    void Update()
    {
        bool paused = GameManager.Instance != null && GameManager.Instance.IsPaused;
        if (paused != _pausedApplied)
        {
            _pausedApplied = paused;
            if (AgentReady) _agent.isStopped = paused;
        }
        if (paused) return;

        _stateTimer += Time.deltaTime;

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
        var prev = _state;
        _state = next;
        _stateTimer = 0f;
        _arrived = false;
        _lookTimer = 0f;
        _scanning = false;
        _search.Clear();
        _searchBuilt = false;
        _hasSearchDest = false;
        indicator?.SetState(next);

        if (AgentReady) _agent.updateRotation = true;

        switch (next)
        {
            case GuardState.Unaware:
                if (AgentReady) { _agent.speed = patrolSpeed * GameDifficulty.Tuning.speed; _agent.isStopped = false; }
                _needDestination = true;
                _waiting = false;
                if (patrolPath != null && patrolPath.Length > 0 && prev != GuardState.Unaware)
                    _waypointIndex = patrolPath.NearestIndex(transform.position);
                break;

            case GuardState.Suspicious:
                if (AgentReady)
                {
                    _agent.speed = investigateSpeed * GameDifficulty.Tuning.speed;
                    _agent.isStopped = false;
                    _agent.SetDestination(_investigateTarget);
                }
                break;

            case GuardState.Alarmed:
                if (AgentReady) { _agent.speed = alarmSpeed * GameDifficulty.Tuning.speed; _agent.isStopped = false; }
                _repathTimer = 0f;
                break;
        }
    }

    // ── Update Routines ─────────────────────────────────────────────────────

    void UpdateUnaware()
    {
        switch (guardType)
        {
            case GuardType.Patrol:
                DoPatrol();
                break;
            case GuardType.Commander:
                DoPatrol();
                DoCommanderCalming();
                break;
            case GuardType.Sentry:
                DoSentryRotate();
                break;
        }
    }

    void UpdateSuspicious()
    {
        if (_stateTimer >= investigateTimeout * GameDifficulty.Tuning.investigate) { SetState(GuardState.Unaware); return; }

        if (guardType == GuardType.Sentry)
        {
            FaceTowards(_investigateTarget, 70f);
            if (_stateTimer >= lookAroundTime && Awareness < 0.2f) SetState(GuardState.Unaware);
            return;
        }

        if (!AgentReady) return;

        if (!_arrived)
        {
            if (!_agent.pathPending && _agent.remainingDistance <= 0.7f)
            {
                _arrived = true;
                _lookTimer = 0f;
                _lookBaseYaw = transform.eulerAngles.y;
                _agent.updateRotation = false;
                _agent.ResetPath();
            }
            return;
        }

        // Scan the area at the investigation point
        _lookTimer += Time.deltaTime;
        transform.rotation = Quaternion.Euler(0f, _lookBaseYaw + Mathf.Sin(_lookTimer * 2.2f) * 75f, 0f);
        if (_lookTimer >= lookAroundTime && Awareness < 0.2f)
        {
            // Hardcore: check the hiding spots near the point before giving up (at most three legs)
            if (GameDifficulty.Tuning.smart)
            {
                if (!_searchBuilt) { BuildSearch(_investigateTarget); _searchBuilt = true; }
                if (NextSearch(out Vector3 spot))
                {
                    _investigateTarget = spot;
                    _arrived = false;
                    _lookTimer = 0f;
                    _stateTimer = 0f;
                    _agent.updateRotation = true;
                    _agent.SetDestination(spot);
                    return;
                }
            }
            SetState(GuardState.Unaware);
        }
    }

    // Hiding spots within reach of a point, nearest first (at most three).
    void BuildSearch(Vector3 around)
    {
        _search.Clear();
        foreach (var h in HidingSpot.All)
            if (h != null && Vector3.Distance(h.Center, around) < 10f) _search.Add(h.Center);
        _search.Sort((a, b) => Vector3.Distance(a, around).CompareTo(Vector3.Distance(b, around)));
        if (_search.Count > 3) _search.RemoveRange(3, _search.Count - 3);
    }

    bool NextSearch(out Vector3 spot)
    {
        spot = default;
        while (_search.Count > 0)
        {
            Vector3 c = _search[0];
            _search.RemoveAt(0);
            if (NavMesh.SamplePosition(c, out var hit, 2f, NavMesh.AllAreas)) { spot = hit.position; return true; }
        }
        return false;
    }

    void UpdateAlarmed()
    {
        // Captures: an alarmed guard that reaches Jonathan or David ends the attempt
        if (guardType != GuardType.Sentry) CheckCapture();

        if (guardType == GuardType.Sentry)
        {
            FaceTowards(_vision.TimeSinceSeen < 2f * GameDifficulty.Tuning.memory ? _vision.LastSeenPosition : _alarmPosition, 120f);
            return;
        }

        if (!AgentReady) return;

        _repathTimer -= Time.deltaTime;
        if (_repathTimer <= 0f)
        {
            _repathTimer = 0.2f;
            Vector3 dest = _vision.TimeSinceSeen < 4f * GameDifficulty.Tuning.memory ? _vision.LastSeenPosition
                         : _hasSearchDest ? _searchDest : _alarmPosition;
            _agent.SetDestination(dest);
        }

        // Nothing in sight at the spot: search it instead of standing on top of it (a hidden target is never shoved into).
        bool searching = !_vision.SeesTarget && !_agent.pathPending && _agent.remainingDistance <= 0.7f;
        if (searching)
        {
            if (!_scanning)
            {
                _scanning = true;
                _lookTimer = 0f;
                _lookBaseYaw = transform.eulerAngles.y;
                _agent.updateRotation = false;
            }
            _lookTimer += Time.deltaTime;
            transform.rotation = Quaternion.Euler(0f, _lookBaseYaw + Mathf.Sin(_lookTimer * 2.2f) * 75f, 0f);

            // Hardcore: after a look around, go and check the hiding spots nearby
            if (GameDifficulty.Tuning.smart && _lookTimer >= 2.5f)
            {
                if (!_searchBuilt) { BuildSearch(_hasSearchDest ? _searchDest : _alarmPosition); _searchBuilt = true; }
                if (NextSearch(out Vector3 spot))
                {
                    _searchDest = spot;
                    _hasSearchDest = true;
                    _repathTimer = 0f;
                    _lookTimer = 0f;
                    _scanning = false;
                    _agent.updateRotation = true;
                }
            }
        }
        else if (_scanning)
        {
            _scanning = false;
            _agent.updateRotation = true;
        }
    }

    // A hiding target is safe until a guard is close enough to actually see it (GuardVision.hiddenNoticeRange);
    // a guard that finds someone in hiding catches them, so a spotted alarm and a capture never disagree.
    void CheckCapture()
    {
        var pc = PlayerController.Instance;
        if (pc != null && (!pc.IsHidden || _vision.SeesPlayer) && Near(pc.transform.position))
        {
            // In the gate zone, before the bluff has started, this is the alternate ending instead of a fail.
            if (GateFinalSequence.Instance != null && GateFinalSequence.Instance.TryDetainJonathan()) return;
            GameManager.Instance?.TriggerFail(FailReason.JonathanCaptured);
            return;
        }
        var dv = DavidCompanion.Instance;
        if (dv != null && (!dv.IsHidden || _vision.SeesDavid) && Near(dv.transform.position))
            GameManager.Instance?.TriggerFail(FailReason.DavidCaptured);
    }

    bool Near(Vector3 p)
    {
        Vector3 d = p - transform.position;
        return Mathf.Abs(d.y) < 1.6f && new Vector2(d.x, d.z).magnitude <= catchDistance * GameDifficulty.Tuning.catchDistance;
    }

    // ── Patrol / Sentry Helpers ─────────────────────────────────────────────

    void DoPatrol()
    {
        if (!AgentReady || patrolPath == null || patrolPath.Length == 0) return;

        if (_needDestination)
        {
            _agent.SetDestination(patrolPath.GetWaypoint(_waypointIndex).position);
            _needDestination = false;
            return;
        }

        if (_waiting)
        {
            _waitTimer -= Time.deltaTime;
            if (_waitTimer <= 0f)
            {
                _waiting = false;
                AdvanceWaypoint();
                _agent.SetDestination(patrolPath.GetWaypoint(_waypointIndex).position);
            }
            return;
        }

        if (!_agent.pathPending && _agent.remainingDistance <= waypointTolerance)
        {
            _waiting = true;
            _waitTimer = waypointPause;
        }
    }

    void AdvanceWaypoint()
    {
        if (patrolPath.pingPong && patrolPath.Length > 1)
        {
            if (_waypointIndex + _dir >= patrolPath.Length || _waypointIndex + _dir < 0) _dir = -_dir;
            _waypointIndex += _dir;
        }
        else _waypointIndex = (_waypointIndex + 1) % patrolPath.Length;
    }

    void DoSentryRotate()
    {
        _sentryT += Time.deltaTime * sentryRotateSpeed * GameDifficulty.Tuning.speed;
        float angle = _sentryBaseAngle + Mathf.Sin(_sentryT * Mathf.Deg2Rad) * (sentryArcDegrees * 0.5f);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.Euler(0f, angle, 0f), 60f * Time.deltaTime);
    }

    void FaceTowards(Vector3 worldPoint, float degreesPerSecond)
    {
        Vector3 dir = worldPoint - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) return;
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation, Quaternion.LookRotation(dir), degreesPerSecond * Time.deltaTime);
    }

    // Commanders close off the "harp exploit": Patrol guards near them that fall Suspicious are calmed
    // (the Commander has already vouched for the area) as long as the Commander can see them.
    void DoCommanderCalming()
    {
        _commanderTimer -= Time.deltaTime;
        if (_commanderTimer > 0f) return;
        _commanderTimer = 0.5f;

        foreach (var g in All)
        {
            if (g == this || g.guardType != GuardType.Patrol) continue;
            if (g.State != GuardState.Suspicious || g.StateTime < 1.5f) continue;
            if (Vector3.Distance(transform.position, g.transform.position) > commanderCalmRadius) continue;
            if (_vision.CanSee(g.transform) || HasClearSightTo(g.transform.position)) g.Calm();
        }
    }

    bool HasClearSightTo(Vector3 point)
    {
        Vector3 eye = _vision.eyePoint != null ? _vision.eyePoint.position : transform.position + Vector3.up * 1.6f;
        Vector3 d = point + Vector3.up * 1.2f - eye;
        return !Physics.Raycast(eye, d.normalized, d.magnitude, GameLayers.SightBlockers, QueryTriggerInteraction.Ignore);
    }

    // ── External Callbacks ──────────────────────────────────────────────────

    // Called by GuardVision every check while a target is visible.
    public void OnSighting(Transform target, Vector3 position, float awareness)
    {
        if (_state == GuardState.Alarmed) return;

        // David: confirmed line of sight is an immediate alarm. Jonathan: alarm once awareness is full.
        if (target.CompareTag(GameLayers.DavidTag) || awareness >= 1f)
        {
            RaiseAlarm(position);
            return;
        }

        if (awareness >= _vision.suspicionThreshold)
        {
            _investigateTarget = position;
            if (_state == GuardState.Unaware) SetState(GuardState.Suspicious);
            else if (AgentReady && !_arrived) _agent.SetDestination(position);
        }
    }

    // Called by GuardHearing when a sound event is in range
    public void OnSoundHeard(Vector3 origin)
    {
        if (_state == GuardState.Alarmed) return;
        _investigateTarget = origin;
        if (_state != GuardState.Suspicious) SetState(GuardState.Suspicious);
        else
        {
            _stateTimer = 0f;         // fresh sound: keep investigating
            _arrived = false;
            if (AgentReady) { _agent.updateRotation = true; _agent.SetDestination(origin); }
        }
    }

    // Called by DavidCompanion.TryHarp() and the Commander to calm this guard
    public void Calm()
    {
        if (_state != GuardState.Suspicious) return;
        _vision.ResetAwareness();
        SetState(GuardState.Unaware);
    }

    void RaiseAlarm(Vector3 position)
    {
        _alarmPosition = position;
        if (AlarmSystem.Instance != null) AlarmSystem.Instance.Raise(position);
        else SetState(GuardState.Alarmed);
    }

    // Broadcast from AlarmSystem: reinforcements converge on the alarm position.
    public void OnAlarmBroadcast(Vector3 position)
    {
        _alarmPosition = position;
        if (_state != GuardState.Alarmed) SetState(GuardState.Alarmed);
    }

    // AlarmSystem.StandDown(): the player has vanished. Guards check the last known spot, then resume routine.
    public void OnStandDown()
    {
        if (_state != GuardState.Alarmed) return;
        _vision.ResetAwareness();
        _investigateTarget = _alarmPosition;
        SetState(GuardState.Suspicious);
    }
}
