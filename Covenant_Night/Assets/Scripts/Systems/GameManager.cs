using System;
using UnityEngine;

public enum FailReason { DavidCaptured, JonathanCaptured, TimeExpired }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public static event Action<FailReason> OnFailState;
    public static event Action OnWinState;

    // Gameplay is paused during story panels, transitions, cutscenes and end states.
    public bool IsPaused { get; private set; } = true;
    public bool HasEnded { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Jonathan and David share the Characters layer; they must not block one another.
        Physics.IgnoreLayerCollision(GameLayers.Characters, GameLayers.Characters, true);
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    public void TriggerFail(FailReason reason = FailReason.TimeExpired)
    {
        if (HasEnded) return;            // several guards can catch on the same frame
        HasEnded = true;
        IsPaused = true;
        OnFailState?.Invoke(reason);
    }

    public void TriggerWin()
    {
        if (HasEnded) return;
        HasEnded = true;
        IsPaused = true;
        OnWinState?.Invoke();
    }

    public void Pause() => IsPaused = true;

    public void ResumePlay()
    {
        IsPaused = false;
        HasEnded = false;
    }
}
