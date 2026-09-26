using UnityEngine;
using UnityEngine.AI;

// Townsfolk that give Gibeah life and get in everyone's way.
//   Stand  – holds a spot (merchant, onlooker). Carves the NavMesh so guards walk around them.
//   Chat   – like Stand, but faces a partner and takes turns gesturing.
//   Wander – walks a short path with pauses. Low avoidance priority: guards push past, Jonathan has to wait.
// Every civilian has a solid collider (Jonathan cannot walk through them) and sits on the Civilians layer,
// which blocks guards' line of sight — a crowd is cover. They react to noise (turn and look) and to the alarm
// (cower and stay put).
[RequireComponent(typeof(NavMeshAgent))]
public class CivilianNPC : MonoBehaviour
{
    public enum Mode { Stand, Chat, Wander }

    public Mode mode = Mode.Stand;
    public Transform[] path;
    public bool  pingPong = true;
    public float speed = 1.1f;
    public float pauseTime = 2.5f;
    public CivilianNPC partner;

    NavMeshAgent _agent;
    NavMeshObstacle _obstacle;
    ProceduralCharacterAnim _anim;

    Quaternion _baseRot;
    float _gestureTimer, _lookTimer, _reactTimer, _waitTimer;
    float _yawOffset, _yawTarget;
    int   _wp, _dir = 1;
    bool  _waiting, _scared, _pausedApplied;

    static float s_lastSpeech;

    void Awake()
    {
        _agent    = GetComponent<NavMeshAgent>();
        _obstacle = GetComponent<NavMeshObstacle>();
        _anim     = GetComponent<ProceduralCharacterAnim>();
    }

    void OnEnable()
    {
        AudioEventSystem.OnSoundEmitted += OnSound;
        AlarmSystem.OnAlarmRaised  += OnAlarm;
        AlarmSystem.OnAlarmCleared += OnAlarmCleared;
    }

    void OnDisable()
    {
        AudioEventSystem.OnSoundEmitted -= OnSound;
        AlarmSystem.OnAlarmRaised  -= OnAlarm;
        AlarmSystem.OnAlarmCleared -= OnAlarmCleared;
    }

    void Start()
    {
        _baseRot = transform.rotation;
        _gestureTimer = Random.Range(2f, 6f);
        _lookTimer = Random.Range(3f, 8f);

        if (mode == Mode.Wander && path != null && path.Length > 1)
        {
            // agents are saved disabled so they only register once the zone's NavMesh exists
            _agent.enabled = true;
            _agent.Warp(transform.position);
            _agent.speed = speed;
            _agent.acceleration = 6f;
            _agent.angularSpeed = 200f;
            _agent.stoppingDistance = 0.2f;
            _agent.avoidancePriority = 90;            // guards are 50: they get the right of way
            if (_agent.isOnNavMesh) _agent.SetDestination(path[0].position);
            if (_obstacle != null) _obstacle.enabled = false;
        }
        else
        {
            _agent.enabled = false;
            if (_obstacle != null) { _obstacle.carving = true; _obstacle.enabled = true; }
        }
    }

    void Update()
    {
        bool paused = GameManager.Instance != null && GameManager.Instance.IsPaused;
        if (paused != _pausedApplied)
        {
            _pausedApplied = paused;
            if (_agent.enabled && _agent.isOnNavMesh) _agent.isStopped = paused || _scared;
        }
        if (paused) return;

        _reactTimer -= Time.deltaTime;
        if (_reactTimer <= 0f && _anim != null && !_scared) { _anim.lookYaw = 0f; }

        if (_scared) { FaceAlarm(); return; }

        if (mode == Mode.Wander && _agent.enabled) UpdateWander();
        else UpdateStanding();

        // chatter / gestures
        _gestureTimer -= Time.deltaTime;
        if (_gestureTimer <= 0f)
        {
            _gestureTimer = Random.Range(4f, 9f);
            if (mode != Mode.Wander || _waiting) _anim?.PlayGesture();
        }
    }

    // ── behaviours ──────────────────────────────────────────────────────────

    void UpdateStanding()
    {
        if (mode == Mode.Chat && partner != null)
        {
            Vector3 d = partner.transform.position - transform.position; d.y = 0f;
            if (d.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(d), 120f * Time.deltaTime);
            return;
        }

        // idle: occasionally glance around (whole body turns a little)
        _lookTimer -= Time.deltaTime;
        if (_lookTimer <= 0f)
        {
            _lookTimer = Random.Range(4f, 10f);
            _yawTarget = Random.Range(-55f, 55f);
        }
        _yawOffset = Mathf.MoveTowards(_yawOffset, _yawTarget, 25f * Time.deltaTime);
        if (_reactTimer <= 0f)
            transform.rotation = _baseRot * Quaternion.Euler(0f, _yawOffset, 0f);
    }

    void UpdateWander()
    {
        if (!_agent.isOnNavMesh) return;

        if (_waiting)
        {
            _waitTimer -= Time.deltaTime;
            if (_waitTimer <= 0f)
            {
                _waiting = false;
                Advance();
                _agent.SetDestination(path[_wp].position);
            }
            return;
        }

        if (!_agent.pathPending && _agent.remainingDistance <= 0.4f)
        {
            _waiting = true;
            _waitTimer = pauseTime * Random.Range(0.7f, 1.4f);
        }
    }

    void Advance()
    {
        if (pingPong)
        {
            if (_wp + _dir >= path.Length || _wp + _dir < 0) _dir = -_dir;
            _wp += _dir;
        }
        else _wp = (_wp + 1) % path.Length;
    }

    // ── reactions ───────────────────────────────────────────────────────────

    void OnSound(Vector3 origin, float radius)
    {
        if (_scared) return;
        Vector3 d = origin - transform.position;
        d.y = 0f;
        if (d.magnitude > Mathf.Min(radius, 12f) || d.sqrMagnitude < 0.5f) return;

        _reactTimer = 2.5f;
        if (mode != Mode.Wander)
        {
            transform.rotation = Quaternion.LookRotation(d);
            _baseRot = transform.rotation;
            _yawOffset = _yawTarget = 0f;
        }
        else if (_anim != null)
        {
            float signed = Vector3.SignedAngle(transform.forward, d, Vector3.up);
            _anim.lookYaw = Mathf.Clamp(signed, -70f, 70f);
        }
        if (Time.time - s_lastSpeech > 1.5f)
        {
            s_lastSpeech = Time.time;
            FloatingText.Spawn(transform.position + Vector3.up * 2.3f, "?", new Color(0.95f, 0.9f, 0.7f), 4f, 1.3f);
        }
    }

    void OnAlarm()
    {
        _scared = true;
        if (_agent.enabled && _agent.isOnNavMesh) _agent.isStopped = true;
        _anim?.PlayFlinch();
        FloatingText.Spawn(transform.position + Vector3.up * 2.3f, "!", new Color(1f, 0.5f, 0.35f), 5f, 1.4f);
    }

    void OnAlarmCleared()
    {
        _scared = false;
        if (_agent.enabled && _agent.isOnNavMesh) _agent.isStopped = false;
    }

    void FaceAlarm()
    {
        var alarm = AlarmSystem.Instance;
        if (alarm == null) return;
        Vector3 d = alarm.AlarmPosition - transform.position; d.y = 0f;
        if (d.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(d), 200f * Time.deltaTime);
    }
}
