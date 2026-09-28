using System.Collections;
using UnityEngine;

// Zone 1's opening cutscene: live camera work and character performance in place of the old static
// illustrated panels. Runs once, right after the title screen, before normal Zone 1 play begins.
// Structurally modeled on GateFinalSequence, but simpler: no gameplay pause/resume of its own since
// ZoneManager already holds gameplay paused for the whole zone transition this runs inside of.
public class IntroCutscene : MonoBehaviour
{
    public static IntroCutscene Instance { get; private set; }

    [Header("Story Beats (4: Warning, Plan, Slipping Past Guards, Leading David Out)")]
    public StoryBeatData beats;

    [Header("Marks")]
    public Transform togetherMark;   // where David comes to stand beside Jonathan

    [Header("Camera Shots")]
    public Transform shotWake;
    public Transform shotTogether;
    public Transform shotAhead;
    public Transform lookAtJonathan;
    public Transform lookAtAhead;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    public IEnumerator Play()
    {
        var pc = PlayerController.Instance;
        var david = DavidCompanion.Instance;
        var cam = ThirdPersonCamera.Instance;
        var story = StoryPanelController.Instance;
        if (pc == null || david == null || beats == null || beats.beats == null || beats.beats.Length < 4) yield break;

        david.SetCutsceneControl(true);
        var jAnim = pc.GetComponent<ProceduralCharacterAnim>();
        var dAnim = david.GetComponent<ProceduralCharacterAnim>();

        // Beat 1: The Warning — a close shot on Jonathan, roused from sleep
        cam?.SetShot(shotWake, lookAtJonathan);
        jAnim?.PlayFlinch();
        if (story != null) yield return story.Show(new[] { beats.beats[0] }, overlayMode: true);

        // Beat 2: The Plan — David comes to stand beside Jonathan
        yield return CutsceneSequencer.MoveTo(david.transform, togetherMark, 1.8f, isPlayer: false);
        cam?.SetShot(shotTogether, lookAtJonathan);
        dAnim?.PlayGesture();
        if (story != null) yield return story.Show(new[] { beats.beats[1] }, overlayMode: true);

        // Beat 3: Slipping Past the Guards — a wide shot looking ahead into the district
        cam?.SetShot(shotAhead, lookAtAhead);
        jAnim?.PlayLookAround(3f);
        if (story != null) yield return story.Show(new[] { beats.beats[2] }, overlayMode: true);

        // Beat 4: Leading David Out
        dAnim?.PlayGesture();
        if (story != null) yield return story.Show(new[] { beats.beats[3] }, overlayMode: true);

        cam?.ClearShot();
        david.SetCutsceneControl(false);
    }
}
