using UnityEngine;
using UnityEngine.AI;

// Controls David's NavMeshAgent, three follow modes, and the Harp ability.
// Tag the David GameObject "David" so guards can identify him on detection.
//
// The agent is saved disabled on the prefab and switched on by Teleport(), which the ZoneManager calls
// once a zone's NavMesh is loaded (an agent enabled before its NavMesh exists never registers).
[RequireComponent(typeof(NavMeshAgent))]
public class DavidCompanion : MonoBehaviour
{
    public static DavidCompanion Instance { get; private set; }

    public enum Mode { Follow, Wait, Run }

    [Header("Follow")]
    public Transform followTarget;          // Jonathan's Transform
    public float startFollowDistance = 3.0f;
    public float stopFollowDistance  = 1.9f;

    [Header("Run")]
    [Tooltip("Marked waypoints for this zone. Set by ZoneManager from the ZoneEntry. Run goes to the nearest one ahead of David (higher Z), so either route through a zone works.")]
    public Transform[] runWaypoints;
    [Tooltip("Legacy single waypoint (used only when runWaypoints is empty).")]
    public Transform runWaypoint;
    public float runNoiseRadius = 9f;

    [Header("Speeds (slower than Jonathan)")]
    public float followSpeed       = 2.8f;
    public float crouchFollowSpeed = 1.7f;
    public float runSpeed          = 5f;
    [Tooltip("Follow speed while Jonathan is sprinting, so David runs when he runs.")]
    public float sprintFollowSpeed = 6.6f;

    [Header("Crouch")]
    public float standingHeight = 1.75f;
    public float crouchHeight   = 1.05f;

    [Header("Harp Ability (once per zone)")]
    public float harpCalmRadius = 10f;
    public AudioSource harpSource;
    public AudioClip   harpClip;

    [Header("Hush Command")]
    [Tooltip("Seconds David stays quiet and crouched after being told to hush.")]
    public float hushDuration = 6f;

    public Mode CurrentMode   { get; private set; } = Mode.Follow;
    public bool IsHidden      { get; private set; }
    public bool IsCrouching   { get; private set; }
    public bool IsHushed      { get; private set; }
    public bool IsSprinting   { get; private set; }
    public bool HarpAvailable => !_harpUsedThisZone;
    public bool IsReady       => _agent != null && _agent.enabled && _agent.isOnNavMesh;

    NavMeshAgent    _agent;
    CapsuleCollider _capsule;
    bool  _harpUsedThisZone;
    bool  _following;
    bool  _pausedApplied;
    Transform _runTarget;
    float _repathTimer;
    float _noiseTimer;
    float _hushTimer;

    void Awake()
    {
        Instance = this;
        _agent   = GetComponent<NavMeshAgent>();
        _capsule = GetComponent<CapsuleCollider>();
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    void Update()
    {
        if (!IsReady) return;

        bool paused = GameManager.Instance != null && GameManager.Instance.IsPaused;
        if (paused != _pausedApplied)
        {
            _pausedApplied = paused;
            _agent.isStopped = paused || CurrentMode == Mode.Wait;
        }
        if (paused) return;

        if (IsHushed)
        {
            _hushTimer -= Time.deltaTime;
            if (_hushTimer <= 0f) { IsHushed = false; RefreshHudMode(); }
        }

        UpdateCrouch();

        switch (CurrentMode)
        {
            case Mode.Follow: UpdateFollow(); break;
            case Mode.Wait:   break;
            case Mode.Run:    UpdateRun();    break;
        }
    }

    // ── Mode Updates ────────────────────────────────────────────────────────

    void UpdateCrouch()
    {
        SetCrouch(IsHushed ||
            (PlayerController.Instance != null && PlayerController.Instance.IsCrouching && CurrentMode != Mode.Run));
    }

    void SetCrouch(bool want)
    {
        if (want == IsCrouching) return;
        IsCrouching = want;
        if (_capsule != null)
        {
            _capsule.height = IsCrouching ? crouchHeight : standingHeight;
            _capsule.center = Vector3.up * (_capsule.height * 0.5f);
        }
    }

    // Cutscenes run while gameplay (and so UpdateCrouch) is paused; this sets the pose directly.
    public void SetCutsceneCrouch(bool on) => SetCrouch(on);

    void UpdateFollow()
    {
        IsSprinting = !IsCrouching && _following && PlayerController.Instance != null && PlayerController.Instance.IsSprinting;
        _agent.speed = IsCrouching ? crouchFollowSpeed : IsSprinting ? sprintFollowSpeed : followSpeed;
        if (IsSprinting) EmitRunNoise();
        if (followTarget == null) return;

        float dist = Vector3.Distance(transform.position, followTarget.position);
        if (!_following && dist > startFollowDistance) _following = true;
        else if (_following && dist < stopFollowDistance) { _following = false; _agent.ResetPath(); }

        if (!_following) return;
        _repathTimer -= Time.deltaTime;
        if (_repathTimer <= 0f)
        {
            _repathTimer = 0.2f;
            _agent.SetDestination(followTarget.position);
        }
    }

    void UpdateRun()
    {
        if (_runTarget == null) { SetMode(Mode.Follow); return; }

        EmitRunNoise();

        if (!_agent.pathPending && _agent.remainingDistance < 0.8f)
            SetMode(Mode.Wait);      // reached the marked waypoint: hold there until called
    }

    void EmitRunNoise()
    {
        _noiseTimer -= Time.deltaTime;
        if (_noiseTimer > 0f) return;
        _noiseTimer = 0.45f;
        if (!IsHushed && _agent.velocity.sqrMagnitude > 1f) AudioEventSystem.Emit(transform.position, runNoiseRadius);
    }

    // Nearest marked waypoint that is ahead of David (zones progress toward +Z).
    Transform PickRunWaypoint()
    {
        Transform best = null;
        float bestD = float.MaxValue;
        if (runWaypoints != null)
            foreach (var w in runWaypoints)
            {
                if (w == null || w.position.z < transform.position.z + 3f) continue;
                float d = (w.position - transform.position).sqrMagnitude;
                if (d < bestD) { bestD = d; best = w; }
            }
        return best != null ? best : runWaypoint;
    }

    // ── Public API ──────────────────────────────────────────────────────────

    public void SetMode(Mode mode, bool announce = false)
    {
        if (!IsReady)
        {
            // Not placed on a NavMesh yet (between zones): remember the mode; Teleport() applies the agent state.
            CurrentMode = mode;
            RefreshHudMode();
            return;
        }

        if (mode == Mode.Run)
        {
            _runTarget = PickRunWaypoint();
        }
        if (mode == Mode.Run && _runTarget == null)
        {
            if (announce) FloatingText.Spawn(transform.position + Vector3.up * 2.4f, "No waypoint ahead", new Color(1f, 0.8f, 0.4f));
            return;
        }

        CurrentMode = mode;
        _following = false;
        IsSprinting = false;
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
                _agent.isStopped = false;
                _agent.speed     = runSpeed;
                _agent.SetDestination(_runTarget.position);
                break;
        }

        RefreshHudMode();
        if (announce)
        {
            string msg = mode switch { Mode.Follow => "Follow", Mode.Wait => "Wait", _ => "Run!" };
            FloatingText.Spawn(transform.position + Vector3.up * 2.4f, msg, new Color(0.7f, 0.9f, 1f));
        }
    }

    public void SetRunWaypoints(Transform[] waypoints)
    {
        runWaypoints = waypoints;
        _runTarget = null;
    }

    public void SetHidden(bool hidden) => IsHidden = hidden;

    void RefreshHudMode() => HUD.Instance?.UpdateDavidMode(CurrentMode.ToString() + (IsHushed ? " (Hushed)" : ""));

    // Proactively quiets David for a few seconds: forces a crouched pose (shrinking guards' effective sight
    // range the same way Jonathan's crouch does) and suppresses his Run-mode footstep noise. Distinct from
    // Harp Calm, which de-escalates guards who are already Suspicious rather than avoiding notice in the first place.
    public void TryHush()
    {
        IsHushed = true;
        _hushTimer = hushDuration;
        RefreshHudMode();
        FloatingText.Spawn(transform.position + Vector3.up * 2.4f, "Hush", new Color(0.75f, 0.85f, 0.95f), 2.5f);
    }

    public void Teleport(Vector3 position, Quaternion rotation)
    {
        _agent.enabled = false;
        transform.SetPositionAndRotation(position, rotation);
        _agent.enabled = true;
        _agent.Warp(position);
        if (!_agent.isOnNavMesh)
            Debug.LogWarning($"[DavidCompanion] David could not be placed on the NavMesh at {position}.", this);
        _pausedApplied = false;
    }

    // Scripted sequences (gate finale) move David directly; the agent is switched off while they run.
    public void SetCutsceneControl(bool on)
    {
        if (on)
        {
            _agent.enabled = false;
        }
        else
        {
            _agent.enabled = true;
            _agent.Warp(transform.position);
        }
    }

    // ── Harp Ability ─────────────────────────────────────────────────────────

    public bool TryHarp()
    {
        if (_harpUsedThisZone)
        {
            FloatingText.Spawn(transform.position + Vector3.up * 2.4f, "Harp already played", new Color(0.8f, 0.8f, 0.8f), 2.5f);
            return false;
        }

        // Calm all Suspicious guards in hearing range that don't have direct sight of David
        bool calmedAny = false;
        foreach (var g in GuardFSM.All.ToArray())
        {
            if (g.State != GuardState.Suspicious) continue;
            if (Vector3.Distance(transform.position, g.transform.position) > harpCalmRadius) continue;
            if (g.Vision.CanSee(transform)) continue;
            g.Calm();
            calmedAny = true;
        }

        if (!calmedAny)
        {
            FloatingText.Spawn(transform.position + Vector3.up * 2.4f, "No one to calm", new Color(0.8f, 0.8f, 0.8f), 2.5f);
            return false;
        }

        _harpUsedThisZone = true;
        if (harpSource != null && harpClip != null) harpSource.PlayOneShot(harpClip);
        GetComponent<ProceduralCharacterAnim>()?.PlayHarp();
        FloatingText.Spawn(transform.position + Vector3.up * 2.4f, "♪ Harp", new Color(1f, 0.9f, 0.5f), 4f, 2.2f);
        HUD.Instance?.ShowHarpUsed();
        return true;
    }

    public void ResetForZone()
    {
        _harpUsedThisZone = false;
        IsHidden = false;
        IsHushed = false;
        _hushTimer = 0f;
        _runTarget = null;
        HUD.Instance?.ResetForZone();
        if (IsReady) SetMode(Mode.Follow); else { CurrentMode = Mode.Follow; RefreshHudMode(); }
    }
}
