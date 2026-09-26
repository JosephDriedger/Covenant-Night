using UnityEngine;

// Listens for AudioEventSystem broadcasts and notifies GuardFSM.
public class GuardHearing : MonoBehaviour
{
    [Tooltip("Scales every sound's radius as heard by this guard.")]
    public float hearingMultiplier = 1f;

    GuardFSM _fsm;

    void Awake() => _fsm = GetComponent<GuardFSM>();

    void OnEnable()  => AudioEventSystem.OnSoundEmitted += OnSoundHeard;
    void OnDisable() => AudioEventSystem.OnSoundEmitted -= OnSoundHeard;

    void OnSoundHeard(Vector3 origin, float radius)
    {
        if (GameManager.Instance != null && GameManager.Instance.IsPaused) return;
        if (Vector3.Distance(transform.position, origin) <= radius * hearingMultiplier * GameDifficulty.Tuning.hearing)
            _fsm?.OnSoundHeard(origin);
    }
}
