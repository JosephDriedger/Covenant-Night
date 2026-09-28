using System.Collections;
using UnityEngine;

// Zone 1's opening cutscene: live camera work and character performance in place of the old static
// illustrated panels. Runs once, right after the title screen, before normal Zone 1 play begins.
//   1. In a throne room set apart from the playable district: David plays the harp for Saul; Saul
//      hurls his spear at him; David dodges. Jonathan, watching, resolves to act.
//   2. Jonathan and David slip out of the throne room together (real walking, David hushed and
//      crouched), a brief cut bridges the distance to Zone 1's actual entrance, then they cover the
//      last stretch on foot into the zone's real spawn point, where normal play begins.
// Structurally modeled on GateFinalSequence, but simpler: no gameplay pause/resume of its own since
// ZoneManager already holds gameplay paused for the whole zone transition this runs inside of.
public class IntroCutscene : MonoBehaviour
{
    public static IntroCutscene Instance { get; private set; }

    [Header("Story Beats (5: Saul's Rage, Warning, Plan, Slipping Past Guards, Leading David Out)")]
    public StoryBeatData beats;

    [Header("Throne Room")]
    public Transform saul;                // the Saul standee (has ProceduralCharacterAnim)
    public Transform saulSpearVisual;      // Saul's rig-held spear; hidden at the throw so the thrown prop reads clean
    public GameObject thrownSpearPrefab;
    public Transform saulMark;
    public Transform davidHarpMark;
    public Transform jonathanWatchMark;
    public Transform spearTargetMark;      // where the thrown spear ends up, embedded in the wall
    public Transform doorwayMark;
    public Transform shotThrone;

    [Header("Sneak-Out")]
    public Transform approachMark;         // just inside Zone 1's real entrance, reached via a brief cut
    public Transform shotSneak;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    // Called by ZoneManager right after LoadZone but before the fade-in, so the player's first view of
    // the zone is already the throne room rather than a jump-cut from the normal spawn point.
    public void PrepareBeforeReveal()
    {
        var pc = PlayerController.Instance;
        var david = DavidCompanion.Instance;
        if (pc == null || david == null || jonathanWatchMark == null || davidHarpMark == null) return;

        david.SetCutsceneControl(true);
        pc.Teleport(jonathanWatchMark.position, jonathanWatchMark.rotation);
        david.transform.SetPositionAndRotation(davidHarpMark.position, davidHarpMark.rotation);
    }

    public IEnumerator Play(Transform jonathanSpawn, Transform davidSpawn)
    {
        var pc = PlayerController.Instance;
        var david = DavidCompanion.Instance;
        var cam = ThirdPersonCamera.Instance;
        var story = StoryPanelController.Instance;
        if (pc == null || david == null || beats == null || beats.beats == null || beats.beats.Length < 5) yield break;

        var jAnim = pc.GetComponent<ProceduralCharacterAnim>();
        var dAnim = david.GetComponent<ProceduralCharacterAnim>();
        var sAnim = saul != null ? saul.GetComponent<ProceduralCharacterAnim>() : null;

        // Beat 0: Saul's Rage — David plays the harp for Saul; Saul suddenly hurls his spear at him.
        cam?.SetShot(shotThrone, davidHarpMark);
        dAnim?.PlayHarp(3f);
        yield return new WaitForSecondsRealtime(1.4f);
        sAnim?.PlayThrow();
        yield return new WaitForSecondsRealtime(0.16f);
        if (saulSpearVisual != null) saulSpearVisual.gameObject.SetActive(false);
        if (saul != null && spearTargetMark != null)
            yield return ThrowSpear(saul.position + Vector3.up * 1.5f, spearTargetMark.position);
        dAnim?.PlayFlinch();
        if (story != null) yield return story.Show(new[] { beats.beats[0] }, overlayMode: true);

        // Beat 1: The Warning — Jonathan, watching, resolves to act
        cam?.SetShot(shotThrone, jonathanWatchMark);
        jAnim?.PlayFlinch();
        if (story != null) yield return story.Show(new[] { beats.beats[1] }, overlayMode: true);

        // Beat 2: The Plan — Jonathan crosses to David
        yield return CutsceneSequencer.MoveTo(pc.transform, davidHarpMark, 2.2f, isPlayer: true);
        dAnim?.PlayGesture();
        if (story != null) yield return story.Show(new[] { beats.beats[2] }, overlayMode: true);

        // Beat 3: Slipping Past the Guards — both slip toward the throne room's doorway, hushed and crouched
        pc.SetCutsceneCrouch(true);
        david.TryHush();
        cam?.SetShot(shotSneak, david.transform);
        yield return CutsceneSequencer.MoveTo(david.transform, doorwayMark, 1.6f, isPlayer: false);
        yield return CutsceneSequencer.MoveTo(pc.transform, doorwayMark, 1.8f, isPlayer: true);
        if (story != null) yield return story.Show(new[] { beats.beats[3] }, overlayMode: true);

        // A brief fade bridges the distance from the throne room to Zone 1's real entrance.
        var fadePanel = ZoneManager.Instance != null ? ZoneManager.Instance.fadePanel : null;
        yield return CutsceneSequencer.Fade(fadePanel, 1f, 0.35f);
        if (approachMark != null)
        {
            Vector3 side = approachMark.right * 0.6f;
            david.transform.SetPositionAndRotation(approachMark.position - side, approachMark.rotation);
            pc.Teleport(approachMark.position + side, approachMark.rotation);
        }
        cam?.SetShot(shotSneak, david.transform);
        yield return CutsceneSequencer.Fade(fadePanel, 0f, 0.35f);

        // Beat 4: Leading David Out — the final approach, on foot, to the zone's actual starting point
        yield return CutsceneSequencer.MoveTo(david.transform, davidSpawn, 1.6f, isPlayer: false);
        yield return CutsceneSequencer.MoveTo(pc.transform, jonathanSpawn, 1.8f, isPlayer: true);
        if (story != null) yield return story.Show(new[] { beats.beats[4] }, overlayMode: true);

        cam?.ClearShot();
        pc.SetCutsceneCrouch(false);
        david.SetCutsceneControl(false);
    }

    // Tweens a standalone spear prop from Saul's hand to its resting point, with a slight arc.
    IEnumerator ThrowSpear(Vector3 from, Vector3 to)
    {
        if (thrownSpearPrefab == null) yield break;
        Vector3 dir = (to - from).normalized;
        Vector3 rest = to - dir * 0.45f;   // land tip-first: back off by roughly the prop's own length
        var spear = Instantiate(thrownSpearPrefab, from, Quaternion.LookRotation(dir));

        const float dur = 0.3f;
        for (float t = 0f; t < dur; t += Time.unscaledDeltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / dur);
            spear.transform.position = Vector3.Lerp(from, rest, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 0.4f;
            yield return null;
        }
        spear.transform.position = rest;
    }
}
