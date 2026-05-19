using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public static event Action OnFailState;
    public static event Action OnWinState;

    public bool IsPaused { get; private set; }

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void TriggerFail()
    {
        IsPaused = true;
        OnFailState?.Invoke();
    }

    public void TriggerWin()
    {
        IsPaused = true;
        OnWinState?.Invoke();
    }

    public void Pause()
    {
        IsPaused = true;
    }

    public void ResumePlay()
    {
        IsPaused = false;
    }
}
