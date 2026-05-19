using UnityEngine;

// Listens for AudioEventSystem broadcasts and notifies GuardFSM.
public class GuardHearing : MonoBehaviour
{
    GuardFSM _fsm;

    void Awake() => _fsm = GetComponent<GuardFSM>();

    void OnEnable()  => AudioEventSystem.OnSoundEmitted += OnSoundHeard;
    void OnDisable() => AudioEventSystem.OnSoundEmitted -= OnSoundHeard;

    void OnSoundHeard(Vector3 origin, float radius)
    {
        if (Vector3.Distance(transform.position, origin) <= radius)
            _fsm?.OnSoundHeard(origin);
    }
}
