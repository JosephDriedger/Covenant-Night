using System;
using UnityEngine;

// Manages the global alarm state for the current zone.
// When a guard hits Alarmed, it calls AlarmSystem.Raise().
// If the player doesn't hide within HideWindow seconds, GameManager triggers fail.
public class AlarmSystem : MonoBehaviour
{
    public static AlarmSystem Instance { get; private set; }

    public static event Action OnAlarmRaised;
    public static event Action OnAlarmCleared;

    [Tooltip("Seconds the player has to hide after an alarm before zone resets.")]
    public float hideWindow = 30f;

    public bool IsAlarmed { get; private set; }
    public float TimeRemaining { get; private set; }

    int _alarmCount; // multiple guards can raise alarm; track count so clear only when all calm

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Update()
    {
        if (!IsAlarmed) return;

        TimeRemaining -= Time.deltaTime;
        HUD.Instance?.UpdateAlarmTimer(TimeRemaining);

        if (TimeRemaining <= 0f)
            GameManager.Instance.TriggerFail();
    }

    public void Raise()
    {
        _alarmCount++;
        if (IsAlarmed) return;

        IsAlarmed = true;
        TimeRemaining = hideWindow;
        OnAlarmRaised?.Invoke();
        HUD.Instance?.ShowAlarmTimer(true);
    }

    public void Suppress()
    {
        _alarmCount = Mathf.Max(0, _alarmCount - 1);
        if (_alarmCount > 0) return;

        IsAlarmed = false;
        OnAlarmCleared?.Invoke();
        HUD.Instance?.ShowAlarmTimer(false);
    }

    // Call when zone resets to wipe all alarm state
    public void Reset()
    {
        _alarmCount = 0;
        IsAlarmed = false;
        TimeRemaining = 0f;
        OnAlarmCleared?.Invoke();
        HUD.Instance?.ShowAlarmTimer(false);
    }
}
