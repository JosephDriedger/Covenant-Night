using UnityEngine;

// Drives the ambient tension drone and alert sting in response to guard states.
// Place on the Persistent scene alongside GameManager.
// Assign a looping AudioSource for the drone and a one-shot source for the sting.
public class TensionAudioManager : MonoBehaviour
{
    [Header("Tension Drone")]
    public AudioSource droneSource;
    public float calmVolume  = 0.05f;
    public float riseVolume  = 0.35f;   // any guard Suspicious
    public float alarmVolume = 0.65f;
    public float alarmPitch  = 1.15f;
    public float lerpSpeed   = 2.5f;

    [Header("Alert Sting")]
    public AudioSource stingSource;

    float _targetVolume;
    float _targetPitch = 1f;

    void OnEnable()
    {
        AlarmSystem.OnAlarmRaised  += OnAlarmRaised;
        AlarmSystem.OnAlarmCleared += OnAlarmCleared;
    }

    void OnDisable()
    {
        AlarmSystem.OnAlarmRaised  -= OnAlarmRaised;
        AlarmSystem.OnAlarmCleared -= OnAlarmCleared;
    }

    void Start()
    {
        _targetVolume = calmVolume;

        if (droneSource != null)
        {
            droneSource.volume = calmVolume;
            droneSource.loop   = true;
            if (!droneSource.isPlaying) droneSource.Play();
        }
    }

    void Update()
    {
        if (droneSource == null) return;

        if (AlarmSystem.Instance != null && !AlarmSystem.Instance.IsAlarmed)
            _targetVolume = IsAnySuspicious() ? riseVolume : calmVolume;

        droneSource.volume = Mathf.Lerp(droneSource.volume, _targetVolume, lerpSpeed * Time.deltaTime);
        droneSource.pitch  = Mathf.Lerp(droneSource.pitch,  _targetPitch,  lerpSpeed * Time.deltaTime);
    }

    void OnAlarmRaised()
    {
        _targetVolume = alarmVolume;
        _targetPitch  = alarmPitch;
        stingSource?.Play();
    }

    void OnAlarmCleared()
    {
        _targetVolume = calmVolume;
        _targetPitch  = 1f;
    }

    static bool IsAnySuspicious()
    {
        foreach (var g in FindObjectsByType<GuardFSM>(FindObjectsSortMode.None))
            if (g.State == GuardState.Suspicious) return true;
        return false;
    }
}
