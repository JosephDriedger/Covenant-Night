using System;
using UnityEngine;

public enum FailReason { DavidCaptured, JonathanCaptured, TimeExpired }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public static event Action<FailReason> OnFailState;
    public static event Action OnWinState;

    // Gameplay is paused during story panels, transitions, cutscenes and end states, and while the pause menu is open.
    public bool IsPaused => _flowPaused || UserPaused;
    public bool HasEnded { get; private set; }
    public bool UserPaused { get; private set; }

    bool _flowPaused = true;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Jonathan and David share the Characters layer; they must not block one another.
        Physics.IgnoreLayerCollision(GameLayers.Characters, GameLayers.Characters, true);
    }

    void OnDestroy()
    {
        if (Instance != this) return;
        Instance = null;
        if (UserPaused) { Time.timeScale = 1f; AudioListener.pause = false; }   // never leave a reloaded scene frozen
    }

    public void TriggerFail(FailReason reason = FailReason.TimeExpired)
    {
        if (HasEnded) return;            // several guards can catch on the same frame
        HasEnded = true;
        _flowPaused = true;
        OnFailState?.Invoke(reason);
    }

    public void TriggerWin()
    {
        if (HasEnded) return;
        HasEnded = true;
        _flowPaused = true;
        GameDifficulty.UnlockNewGamePlus();
        OnWinState?.Invoke();
    }

    public void Pause() => _flowPaused = true;

    // Pause menu: freezes the simulation (timeScale 0) and all audio; the menu itself runs on unscaled time.
    public bool CanUserPause => !_flowPaused && !HasEnded;

    public void SetUserPaused(bool paused)
    {
        if (paused == UserPaused) return;
        UserPaused = paused;
        Time.timeScale = paused ? 0f : 1f;
        AudioListener.pause = paused;
    }

    public void ResumePlay()
    {
        _flowPaused = false;
        HasEnded = false;
    }
}
