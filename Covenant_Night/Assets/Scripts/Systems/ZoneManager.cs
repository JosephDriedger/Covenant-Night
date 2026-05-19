using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Handles additive zone loading, checkpoint save/restore, and zone transitions.
// All zone scenes must be added to Build Settings.
public class ZoneManager : MonoBehaviour
{
    public static ZoneManager Instance { get; private set; }

    [Header("Zone Scene Names (in order)")]
    public string[] zoneSceneNames;

    [Header("References")]
    public CheckpointData checkpoint;
    public Transform jonathan;
    public Transform david;
    public PlayerAbilities abilities;
    public DavidCompanion  davidCompanion;

    [Header("Story Beats (one per zone; shown before loading)")]
    public StoryBeatData[] zoneEntryBeats;

    [Header("Transition")]
    public CanvasGroup fadePanel;
    public float fadeDuration = 0.5f;

    int _currentZoneIndex = -1;
    string _loadedZoneScene;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start() => StartCoroutine(LoadZone(0));

    // Called by ZoneExit trigger when the player reaches the exit
    public void EnterNextZone()
    {
        if (_currentZoneIndex + 1 >= zoneSceneNames.Length)
        {
            GameManager.Instance.TriggerWin();
            return;
        }
        StartCoroutine(TransitionToZone(_currentZoneIndex + 1));
    }

    // Called by GameManager fail state
    public void RestartCurrentZone()
    {
        StartCoroutine(TransitionToZone(_currentZoneIndex, restoreCheckpoint: true));
    }

    IEnumerator TransitionToZone(int index, bool restoreCheckpoint = false)
    {
        GameManager.Instance.Pause();

        yield return StartCoroutine(Fade(1f));

        if (!string.IsNullOrEmpty(_loadedZoneScene))
        {
            AlarmSystem.Instance.Reset();
            yield return SceneManager.UnloadSceneAsync(_loadedZoneScene);
        }

        // Show story panels for the upcoming zone (skip on checkpoint restart)
        if (!restoreCheckpoint &&
            zoneEntryBeats != null && index < zoneEntryBeats.Length &&
            zoneEntryBeats[index] != null &&
            StoryPanelController.Instance != null)
        {
            yield return StartCoroutine(StoryPanelController.Instance.Show(zoneEntryBeats[index].beats));
        }

        yield return StartCoroutine(LoadZone(index, restoreCheckpoint));

        yield return StartCoroutine(Fade(0f));

        GameManager.Instance.ResumePlay();
    }

    IEnumerator LoadZone(int index, bool restoreCheckpoint = false)
    {
        _currentZoneIndex = index;
        string sceneName = zoneSceneNames[index];

        yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        _loadedZoneScene = sceneName;

        if (restoreCheckpoint)
            RestoreFromCheckpoint();
        else
        {
            abilities.ResetForZone();   // stone count back to 3 before saving
            SaveCheckpoint();
        }

        davidCompanion?.ResetForZone();
        HUD.Instance?.ResetForZone();

        ZoneNameCard.Instance?.Show(sceneName.Replace("_", " "));
        HUD.Instance?.UpdateZone(index + 1, zoneSceneNames.Length);
    }

    public void SaveCheckpoint() =>
        checkpoint.Save(jonathan, david, abilities.StoneCount);

    void RestoreFromCheckpoint()
    {
        jonathan.SetPositionAndRotation(checkpoint.jonathanPosition, checkpoint.jonathanRotation);
        david.position = checkpoint.davidPosition;
        abilities.SetStoneCount(checkpoint.stoneCount);
    }

    IEnumerator Fade(float targetAlpha)
    {
        if (fadePanel == null) yield break;
        float start = fadePanel.alpha;
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            fadePanel.alpha = Mathf.Lerp(start, targetAlpha, t / fadeDuration);
            yield return null;
        }
        fadePanel.alpha = targetAlpha;
    }
}
