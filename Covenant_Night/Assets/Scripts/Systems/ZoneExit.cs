using UnityEngine;

// Place on a trigger collider at the zone exit. Advances to the next zone when Jonathan reaches it with
// David beside him. Locks while an alarm is active (GDD: "zone exit locks").
public class ZoneExit : MonoBehaviour
{
    [Tooltip("Locked while the alarm is active.")]
    public bool lockDuringAlarm = true;

    [Tooltip("David must be inside the exit or this close to Jonathan for the zone to end.")]
    public bool requireDavid = true;
    public float davidMaxDistance = 6f;

    [Tooltip("Red barrier shown while the exit is locked.")]
    public GameObject lockVisual;

    [Header("Gate")]
    [Tooltip("Hinge pivots of the two door leaves; they swing open (away from the approach) as Jonathan nears and stay shut during an alarm.")]
    public Transform doorL;
    public Transform doorR;
    public float openAngle  = 100f;
    public float openRadius = 9f;
    public float openSpeed  = 0.9f;     // fraction of the full swing per second
    public AudioSource sfx;
    public AudioClip   creak;

    BoxCollider _box;
    bool _fired;
    float _messageCooldown;
    float _open;

    void OnEnable()
    {
        AlarmSystem.OnAlarmRaised  += RefreshLock;
        AlarmSystem.OnAlarmCleared += RefreshLock;
    }

    void OnDisable()
    {
        AlarmSystem.OnAlarmRaised  -= RefreshLock;
        AlarmSystem.OnAlarmCleared -= RefreshLock;
    }

    void Awake() => _box = GetComponent<BoxCollider>();

    void Start() => RefreshLock();

    void RefreshLock()
    {
        if (lockVisual != null)
            lockVisual.SetActive(lockDuringAlarm && AlarmSystem.Instance != null && AlarmSystem.Instance.IsAlarmed);
    }

    // Position test instead of trigger enter/exit events: zones share world coordinates, and a teleport
    // (zone hand-off) does not reliably deliver OnTriggerExit, which used to make a freshly loaded exit fire at once.
    bool JonathanInside()
    {
        var pc = PlayerController.Instance;
        if (pc == null || _box == null) return false;
        Bounds b = _box.bounds;
        Vector3 p = pc.transform.position;
        return p.x >= b.min.x && p.x <= b.max.x && p.z >= b.min.z && p.z <= b.max.z && p.y > -1f && p.y < b.max.y;
    }

    void UpdateGate()
    {
        if (doorL == null && doorR == null) return;
        var pc = PlayerController.Instance;
        bool alarmed = lockDuringAlarm && AlarmSystem.Instance != null && AlarmSystem.Instance.IsAlarmed;
        bool near = pc != null && pc.transform.position.y > -50f &&
                    (pc.transform.position - transform.position).sqrMagnitude < openRadius * openRadius;
        float target = near && !alarmed ? 1f : 0f;
        if (target > _open && _open <= 0f && sfx != null && creak != null) sfx.PlayOneShot(creak);
        _open = Mathf.MoveTowards(_open, target, Time.deltaTime * openSpeed);

        float a = Mathf.SmoothStep(0f, 1f, _open) * openAngle;
        if (doorL != null) doorL.localRotation = Quaternion.Euler(0f, -a, 0f);
        if (doorR != null) doorR.localRotation = Quaternion.Euler(0f, a, 0f);
    }

    void Update()
    {
        UpdateGate();
        if (_fired || !JonathanInside()) return;
        if (GameManager.Instance != null && GameManager.Instance.IsPaused) return;

        _messageCooldown -= Time.deltaTime;

        if (lockDuringAlarm && AlarmSystem.Instance != null && AlarmSystem.Instance.IsAlarmed)
        {
            Say("The way is barred while the alarm is sounding.");
            return;
        }

        if (requireDavid)
        {
            var david = DavidCompanion.Instance;
            var pc = PlayerController.Instance;
            if (david != null && pc != null &&
                Vector3.Distance(david.transform.position, pc.transform.position) > davidMaxDistance)
            {
                Say("Wait for David. He must come with you.");
                return;
            }
        }

        _fired = true;
        Debug.Log($"[ZoneExit] {name} in scene {gameObject.scene.name} fired; Jonathan at {(PlayerController.Instance != null ? PlayerController.Instance.transform.position.ToString() : "?")}");
        ZoneManager.Instance?.EnterNextZone();
    }

    void Say(string msg)
    {
        if (_messageCooldown > 0f) return;
        _messageCooldown = 3f;
        HUD.Instance?.ShowMessage(msg, 2.5f);
    }
}
