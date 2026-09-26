using System;
using UnityEngine;

// Manages the global alarm state for the current zone.
//   Raise()      – a guard has a confirmed sighting: every guard converges (reinforcements), exits lock,
//                  and the player gets `hideWindow` seconds to reach a hiding spot.
//   Hiding       – while Jonathan is inside a hiding spot the countdown is held (no Time.timeScale hack);
//                  after `standDownDelay` seconds hidden, the alarm stands down and guards resume routine.
//   Expiry       – if the timer runs out the attempt fails and the zone resets.
public class AlarmSystem : MonoBehaviour
{
    public static AlarmSystem Instance { get; private set; }

    public static event Action OnAlarmRaised;
    public static event Action OnAlarmCleared;

    [Tooltip("Seconds the player has to hide after an alarm before zone resets.")]
    public float hideWindow = 30f;

    [Tooltip("Seconds Jonathan must stay hidden before the alarm stands down.")]
    public float standDownDelay = 3f;

    public bool    IsAlarmed     { get; private set; }
    public float   TimeRemaining { get; private set; }
    public Vector3 AlarmPosition { get; private set; }
    public bool    PlayerHidden  { get; private set; }

    float _hiddenTimer;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    void Update()
    {
        if (!IsAlarmed) return;
        if (GameManager.Instance != null && GameManager.Instance.IsPaused) return;

        if (PlayerHidden)
        {
            _hiddenTimer += Time.deltaTime;
            if (_hiddenTimer >= standDownDelay) StandDown();
            return;
        }

        _hiddenTimer = 0f;
        TimeRemaining -= Time.deltaTime;
        HUD.Instance?.UpdateAlarmTimer(TimeRemaining);

        if (TimeRemaining <= 0f)
            GameManager.Instance?.TriggerFail(FailReason.TimeExpired);
    }

    public void Raise(Vector3 position)
    {
        AlarmPosition = position;

        if (!IsAlarmed)
        {
            IsAlarmed = true;
            TimeRemaining = hideWindow;
            _hiddenTimer = 0f;
            OnAlarmRaised?.Invoke();
            HUD.Instance?.ShowAlarmTimer(true);
        }

        // Reinforcements: every guard in the zone is alerted and converges
        foreach (var g in GuardFSM.All.ToArray())
            g.OnAlarmBroadcast(position);
    }

    // Called by HidingSpot when Jonathan enters / leaves a hiding spot.
    public void SetPlayerHidden(bool hidden)
    {
        PlayerHidden = hidden;
        if (!hidden) _hiddenTimer = 0f;
    }

    public void StandDown()
    {
        if (!IsAlarmed) return;
        IsAlarmed = false;
        TimeRemaining = 0f;
        _hiddenTimer = 0f;
        foreach (var g in GuardFSM.All.ToArray())
            g.OnStandDown();
        OnAlarmCleared?.Invoke();
        HUD.Instance?.ShowAlarmTimer(false);
    }

    // Wipes all alarm state (zone reset / transition).
    public void Reset()
    {
        bool wasAlarmed = IsAlarmed;
        IsAlarmed = false;
        PlayerHidden = false;
        TimeRemaining = 0f;
        _hiddenTimer = 0f;
        if (wasAlarmed) OnAlarmCleared?.Invoke();
        HUD.Instance?.ShowAlarmTimer(false);
    }
}
