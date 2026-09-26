using UnityEngine;
using UnityEngine.Audio;

// Drives the tension drone, the underscore, the alert sting and the AudioMixer snapshots
// (Calm / Suspicious / Alarmed) in response to guard states. Lives in the Persistent scene.
//
//   Drone: "a low sustained pad that rises in volume and pitch as guards approach Suspicious state" —
//          volume/pitch follow the highest guard awareness near Jonathan, and peak while alarmed.
//   Music: sparse underscore (no melody). The epilogue plays a single melodic phrase.
public class TensionAudioManager : MonoBehaviour
{
    public static TensionAudioManager Instance { get; private set; }

    [Header("Tension Drone")]
    public AudioSource droneSource;
    public float calmVolume  = 0.05f;
    public float riseVolume  = 0.35f;   // any guard Suspicious
    public float alarmVolume = 0.65f;
    public float alarmPitch  = 1.15f;
    public float risePitch   = 1.06f;
    public float lerpSpeed   = 2.5f;
    public float awarenessRange = 25f;  // only guards this close raise the drone

    [Header("Music")]
    public AudioSource musicSource;
    public AudioSource epilogueSource;

    [Header("Alert Sting")]
    public AudioSource stingSource;

    [Header("Mixer Snapshots")]
    public AudioMixerSnapshot calmSnapshot;
    public AudioMixerSnapshot suspiciousSnapshot;
    public AudioMixerSnapshot alarmedSnapshot;
    public float snapshotTransition = 0.8f;

    float _targetVolume;
    float _targetPitch = 1f;
    int   _snapshot = -1;   // 0 calm, 1 suspicious, 2 alarmed
    bool  _epilogue;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

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

    void OnDestroy() { if (Instance == this) Instance = null; }

    void Start()
    {
        _targetVolume = calmVolume;

        if (droneSource != null)
        {
            droneSource.volume = calmVolume;
            droneSource.loop   = true;
            if (!droneSource.isPlaying) droneSource.Play();
        }
        if (musicSource != null)
        {
            musicSource.loop = true;
            if (!musicSource.isPlaying) musicSource.Play();
        }
        SetSnapshot(0);
    }

    void Update()
    {
        if (droneSource == null || _epilogue) return;

        bool alarmed = AlarmSystem.Instance != null && AlarmSystem.Instance.IsAlarmed;
        float awareness = alarmed ? 1f : MaxNearbyAwareness(out bool anySuspicious);
        bool suspicious = !alarmed && (awareness > 0.15f || AnySuspicious());

        if (alarmed)
        {
            _targetVolume = alarmVolume;
            _targetPitch  = alarmPitch;
        }
        else
        {
            float k = Mathf.Clamp01(awareness);
            _targetVolume = Mathf.Lerp(calmVolume, riseVolume, k);
            _targetPitch  = Mathf.Lerp(1f, risePitch, k);
        }

        droneSource.volume = Mathf.Lerp(droneSource.volume, _targetVolume, lerpSpeed * Time.deltaTime);
        droneSource.pitch  = Mathf.Lerp(droneSource.pitch,  _targetPitch,  lerpSpeed * Time.deltaTime);

        SetSnapshot(alarmed ? 2 : suspicious ? 1 : 0);
    }

    float MaxNearbyAwareness(out bool anySuspicious)
    {
        anySuspicious = false;
        var pc = PlayerController.Instance;
        if (pc == null) return 0f;
        float best = 0f;
        foreach (var g in GuardFSM.All)
        {
            float d = Vector3.Distance(g.transform.position, pc.transform.position);
            if (d > awarenessRange) continue;
            float a = g.State == GuardState.Suspicious ? Mathf.Max(0.6f, g.Awareness) : g.Awareness;
            if (g.State == GuardState.Suspicious) anySuspicious = true;
            best = Mathf.Max(best, a * Mathf.Clamp01(1.4f - d / awarenessRange));
        }
        return best;
    }

    static bool AnySuspicious()
    {
        foreach (var g in GuardFSM.All)
            if (g.State == GuardState.Suspicious) return true;
        return false;
    }

    void SetSnapshot(int idx)
    {
        if (idx == _snapshot) return;
        _snapshot = idx;
        var snap = idx == 2 ? alarmedSnapshot : idx == 1 ? suspiciousSnapshot : calmSnapshot;
        if (snap != null) snap.TransitionTo(snapshotTransition);
    }

    void OnAlarmRaised()
    {
        if (stingSource != null) stingSource.Play();   // sharp staccato string hit
    }

    void OnAlarmCleared()
    {
        _targetVolume = calmVolume;
        _targetPitch  = 1f;
    }

    // Epilogue: fade the stealth bed out; a single melodic phrase plays over the farewell.
    public void PlayEpilogue()
    {
        _epilogue = true;
        StartCoroutine(EpilogueRoutine());
    }

    System.Collections.IEnumerator EpilogueRoutine()
    {
        if (calmSnapshot != null) calmSnapshot.TransitionTo(2f);
        if (epilogueSource != null && epilogueSource.clip != null) epilogueSource.Play();
        for (float t = 0f; t < 2f; t += Time.unscaledDeltaTime)
        {
            float k = 1f - t / 2f;
            if (droneSource != null) droneSource.volume *= k;
            if (musicSource != null) musicSource.volume *= k;
            yield return null;
        }
        if (droneSource != null) droneSource.volume = 0f;
        if (musicSource != null) musicSource.volume = 0f;
    }
}
