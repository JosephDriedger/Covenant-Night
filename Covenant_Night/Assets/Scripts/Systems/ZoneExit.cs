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

    BoxCollider _box;
    bool _fired;
    float _messageCooldown;

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

    void Update()
    {
        if (_fired || !JonathanInside()) return;
        if (GameManager.Instance != null && GameManager.Instance.IsPaused) return;

        _messageCooldown -= Time.deltaTime;

        if (lockDuringAlarm && AlarmSystem.Instance != null && AlarmSystem.Instance.IsAlarmed)
        {
            Say("The way is barred — the alarm is up!");
            return;
        }

        if (requireDavid)
        {
            var david = DavidCompanion.Instance;
            var pc = PlayerController.Instance;
            if (david != null && pc != null &&
                Vector3.Distance(david.transform.position, pc.transform.position) > davidMaxDistance)
            {
                Say("Wait for David — he must come with you.");
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
