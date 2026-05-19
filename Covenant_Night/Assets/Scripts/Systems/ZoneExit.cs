using UnityEngine;

// Place on a trigger collider at the zone exit.
// When Jonathan enters, advance to the next zone.
public class ZoneExit : MonoBehaviour
{
    [Tooltip("Locked while the alarm is active.")]
    public bool lockDuringAlarm = true;

    void Start() => AlarmSystem.OnAlarmRaised  += OnAlarm;
    void OnDestroy() => AlarmSystem.OnAlarmRaised -= OnAlarm;

    void OnAlarm() { /* exit collider remains; ZoneManager just won't load next scene */ }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (lockDuringAlarm && AlarmSystem.Instance != null && AlarmSystem.Instance.IsAlarmed) return;
        ZoneManager.Instance?.EnterNextZone();
    }
}
