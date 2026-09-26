using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Handles additive zone loading, checkpoint save/restore, story-panel transitions, and zone hand-off.
// All zone scenes must be in Build Settings. The Persistent scene (this component's scene) is never unloaded.
public class ZoneManager : MonoBehaviour
{
    public static ZoneManager Instance { get; private set; }

    [Header("Zone Scene Names (in order)")]
    public string[] zoneSceneNames;

    [Header("References")]
    public CheckpointData[] zoneCheckpoints;      // one per zone (runtime copies are used)
    public CheckpointData checkpoint;             // legacy single checkpoint (used if zoneCheckpoints is empty)
    public Transform jonathan;
    public Transform david;
    public PlayerAbilities abilities;
    public DavidCompanion  davidCompanion;

    [Header("Story Beats (one per zone; shown before that zone loads — index 0 is the intro)")]
    public StoryBeatData[] zoneEntryBeats;

    [Header("Transition")]
    public CanvasGroup fadePanel;
    public float fadeDuration = 0.5f;

    public int  CurrentZoneIndex { get; private set; } = -1;
    public ZoneEntry CurrentEntry { get; private set; }
    public bool IsTransitioning   { get; private set; }

    string _loadedZoneScene;
    CheckpointData[] _runtimeCheckpoints;
    CheckpointData   _runtimeLegacy;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Work on copies so play mode never dirties the checkpoint assets
        if (zoneCheckpoints != null && zoneCheckpoints.Length > 0)
        {
            _runtimeCheckpoints = new CheckpointData[zoneCheckpoints.Length];
            for (int i = 0; i < zoneCheckpoints.Length; i++)
                _runtimeCheckpoints[i] = zoneCheckpoints[i] != null ? Instantiate(zoneCheckpoints[i]) : ScriptableObject.CreateInstance<CheckpointData>();
        }
        _runtimeLegacy = checkpoint != null ? Instantiate(checkpoint) : ScriptableObject.CreateInstance<CheckpointData>();
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    void Start()
    {
        if (fadePanel != null) fadePanel.alpha = 1f;
        StartCoroutine(Boot());
    }

    IEnumerator Boot()
    {
        if (MenuController.Instance != null) yield return MenuController.Instance.RunTitle();
        yield return TransitionToZone(0, restoreCheckpoint: false);
    }

    CheckpointData CheckpointFor(int index) =>
        _runtimeCheckpoints != null && index >= 0 && index < _runtimeCheckpoints.Length ? _runtimeCheckpoints[index] : _runtimeLegacy;

    // ── Public API ──────────────────────────────────────────────────────────

    // Called by ZoneExit when the player reaches the exit with David
    public void EnterNextZone()
    {
        if (IsTransitioning) return;
        if (CurrentZoneIndex + 1 >= zoneSceneNames.Length)
        {
            GameManager.Instance.TriggerWin();
            return;
        }
        StartCoroutine(TransitionToZone(CurrentZoneIndex + 1, restoreCheckpoint: false));
    }

    // Called by FailStateHandler after the fail panels
    public void RestartCurrentZone()
    {
        if (IsTransitioning) return;
        StartCoroutine(TransitionToZone(CurrentZoneIndex, restoreCheckpoint: true));
    }

    // Hardcore: a capture sends the whole run back to the first zone (its story panels are skipped).
    public void RestartRun()
    {
        if (IsTransitioning) return;
        StartCoroutine(TransitionToZone(0, restoreCheckpoint: false, skipBeats: true));
    }

    // Debug / automated tests: jump straight to a zone, skipping story panels.
    public void JumpToZone(int index)
    {
        if (IsTransitioning) return;
        StartCoroutine(TransitionToZone(index, restoreCheckpoint: false, skipBeats: true));
    }

    // ── Transition ──────────────────────────────────────────────────────────

    IEnumerator TransitionToZone(int index, bool restoreCheckpoint, bool skipBeats = false)
    {
        IsTransitioning = true;
        GameManager.Instance.Pause();

        yield return Fade(1f);

        // Park the characters far away while zones swap: zones share world coordinates, so a character left on
        // the old exit would overlap the next zone's triggers the moment it loads.
        jonathan.GetComponent<PlayerController>().Teleport(new Vector3(0f, -300f, 0f), Quaternion.identity);
        if (davidCompanion.IsReady) davidCompanion.SetCutsceneControl(true);
        david.position = new Vector3(0f, -300f, 4f);

        if (!string.IsNullOrEmpty(_loadedZoneScene))
        {
            AlarmSystem.Instance?.Reset();
            yield return SceneManager.UnloadSceneAsync(_loadedZoneScene);
            _loadedZoneScene = null;
            CurrentEntry = null;
        }

        // Story panels for the upcoming zone (not on a checkpoint restart)
        if (!restoreCheckpoint && !skipBeats &&
            zoneEntryBeats != null && index < zoneEntryBeats.Length &&
            zoneEntryBeats[index] != null && StoryPanelController.Instance != null)
        {
            yield return StoryPanelController.Instance.Show(zoneEntryBeats[index].beats);
        }

        yield return LoadZone(index, restoreCheckpoint);

        yield return Fade(0f);

        IsTransitioning = false;
        GameManager.Instance.ResumePlay();
    }

    IEnumerator LoadZone(int index, bool restoreCheckpoint)
    {
        CurrentZoneIndex = index;
        string sceneName = zoneSceneNames[index];

        yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        _loadedZoneScene = sceneName;

        // one frame so the zone's Awake/OnEnable/Start (NavMesh data, guards) have run
        yield return null;

        Scene scene = SceneManager.GetSceneByName(sceneName);
        CurrentEntry = FindEntry(scene);
        GameDifficulty.ZoneScale = CurrentEntry != null ? CurrentEntry.difficulty : 1f;
        Torch.AmbientVisibility = CurrentEntry != null ? CurrentEntry.ambientVisibility : 0.35f;

        var cp = CheckpointFor(index);
        Vector3 jPos, dPos; Quaternion jRot;
        if (restoreCheckpoint && cp.hasData)
        {
            jPos = cp.jonathanPosition; jRot = cp.jonathanRotation; dPos = cp.davidPosition;
        }
        else if (CurrentEntry != null)
        {
            jPos = CurrentEntry.jonathanSpawn.position;
            jRot = CurrentEntry.jonathanSpawn.rotation;
            dPos = CurrentEntry.davidSpawn.position;
        }
        else
        {
            Debug.LogWarning($"[ZoneManager] '{sceneName}' has no ZoneEntry; leaving characters where they are.");
            jPos = jonathan.position; jRot = jonathan.rotation; dPos = david.position;
        }

        jonathan.GetComponent<PlayerController>().Teleport(jPos, jRot);
        davidCompanion.Teleport(dPos, jRot);
        davidCompanion.SetRunWaypoints(CurrentEntry != null ? CurrentEntry.davidRunWaypoints : null);
        Physics.SyncTransforms();
        ThirdPersonCamera.Instance?.SnapBehind(jRot.eulerAngles.y);

        // Per-zone reset: stones, harp, David mode, HUD, alarm
        AlarmSystem.Instance?.Reset();
        abilities.ResetForZone();
        davidCompanion.ResetForZone();
        HUD.Instance?.ResetForZone();

        if (restoreCheckpoint && cp.hasData) abilities.SetStoneCount(cp.stoneCount);
        else cp.Save(jonathan, david, abilities.StoneCount);

        string title = CurrentEntry != null ? CurrentEntry.displayName : sceneName.Replace("_", " ");
        ZoneNameCard.Instance?.Show(title, CurrentEntry != null ? CurrentEntry.subtitle : null);
        HUD.Instance?.UpdateZone(index + 1, zoneSceneNames.Length);
    }

    static ZoneEntry FindEntry(Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            var e = root.GetComponentInChildren<ZoneEntry>(true);
            if (e != null) return e;
        }
        return null;
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
