using System.Collections;
using UnityEngine;

// Zone 1's opening cutscene, staged live in a throne room set apart from the playable district:
//   1. Wide shot, slowly pushing in: Saul broods on his throne while David plays the harp. Saul rises and
//      hurls his spear; David ducks and it buries itself in the wall behind him.
//   2. Cut to Jonathan, watching from the side of the hall (Saul storms out while the camera is on him).
//   3. Two-shot: Jonathan crosses to David and tells him the plan.
//   4. Jonathan leads David out through the doorway, both crouched, the camera panning with them.
//   5. A fade bridges to Zone 1's entry court; they cover the last stretch on foot to the real spawn
//      points, then the camera glides down behind Jonathan and play begins.
// ZoneManager holds gameplay paused for the whole transition this runs inside of.
public class IntroCutscene : MonoBehaviour
{
    public static IntroCutscene Instance { get; private set; }

    [Header("Story Beats (5: Saul's Rage, Warning, Plan, Slipping Past Guards, Leading David Out)")]
    public StoryBeatData beats;

    [Header("Throne Room")]
    public Transform saul;                 // the Saul standee (has ProceduralCharacterAnim), placed on his throne
    public Transform saulSpearVisual;      // Saul's rig-held spear; hidden at the throw so the thrown prop reads clean
    public GameObject thrownSpearPrefab;
    public Transform davidHarpMark;
    public Transform jonathanWatchMark;
    public Transform spearTargetMark;      // where the thrown spear ends up, embedded in the wall
    public Transform planMark;             // where Jonathan stops beside David
    public Transform doorwayLeadMark;
    public Transform doorwayFollowMark;

    [Header("Throne Room Shots")]
    public Transform shotWide;             // moved from its start pose to shotWideEnd during the harp
    public Transform shotWideEnd;
    public Transform lookWide;
    public Transform shotInsert;           // the spear quivering in the wall, David ducked beside it
    public Transform lookInsert;
    public Transform shotReaction;
    public Transform shotTwo;
    public Transform lookTwo;
    public Transform shotSneak;

    [Header("Zone 1 Approach")]
    public Transform approachDavidMark;
    public Transform approachJonathanMark;
    public Transform shotApproach;
    public Transform lookApproach;

    ProceduralCharacterAnim _jAnim, _dAnim, _sAnim;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    // Called by ZoneManager right after LoadZone but before the fade-in, so the zone is revealed on the
    // opening shot of the throne room rather than on a jump cut from the normal spawn point.
    public void PrepareBeforeReveal()
    {
        var pc = PlayerController.Instance;
        var david = DavidCompanion.Instance;
        if (pc == null || david == null || jonathanWatchMark == null || davidHarpMark == null) return;

        _jAnim = pc.GetComponent<ProceduralCharacterAnim>();
        _dAnim = david.GetComponent<ProceduralCharacterAnim>();
        _sAnim = saul != null ? saul.GetComponent<ProceduralCharacterAnim>() : null;

        david.SetCutsceneControl(true);
        pc.Teleport(jonathanWatchMark.position, jonathanWatchMark.rotation);
        david.transform.SetPositionAndRotation(davidHarpMark.position, davidHarpMark.rotation);
        if (_dAnim != null) _dAnim.holdHarp = true;
        if (_sAnim != null) _sAnim.SnapSeated(true);

        ThirdPersonCamera.Instance?.SetShot(shotWide, lookWide, cut: true);
    }

    public IEnumerator Play(Transform jonathanSpawn, Transform davidSpawn)
    {
        var pc = PlayerController.Instance;
        var david = DavidCompanion.Instance;
        var cam = ThirdPersonCamera.Instance;
        var story = StoryPanelController.Instance;
        if (pc == null || david == null || beats == null || beats.beats == null || beats.beats.Length < 5) yield break;

        // ── 1. The harp, and the spear ──
        Coroutine dolly = StartCoroutine(Dolly(shotWide, shotWideEnd, 4.0f));
        _dAnim?.PlayHarp(3.6f);
        yield return new WaitForSecondsRealtime(2.8f);
        if (_sAnim != null) _sAnim.seated = false;                  // Saul rises
        yield return new WaitForSecondsRealtime(0.55f);
        _sAnim?.PlayThrow();
        yield return new WaitForSecondsRealtime(0.17f);              // the throw's release point
        if (saulSpearVisual != null) saulSpearVisual.gameObject.SetActive(false);
        _dAnim?.PlayFlinch();
        if (saul != null && spearTargetMark != null)
        {
            Vector3 hand = saul.position + Vector3.up * 1.75f + saul.right * 0.3f;
            StartCoroutine(Nudge(david.transform, david.transform.right * 0.35f, 0.22f));    // ducks away from the spear's line
            yield return ThrowSpear(hand, spearTargetMark.position);
        }
        cam?.SetShot(shotInsert, lookInsert, cut: true);                 // cut on impact
        yield return new WaitForSecondsRealtime(1.1f);
        if (story != null) yield return story.Show(new[] { beats.beats[0] }, overlayMode: true);
        StopCoroutine(dolly);

        // ── 2. Jonathan's reaction (Saul leaves while the camera is on his son) ──
        cam?.SetShot(shotReaction, pc.transform, 1.5f, cut: true);
        if (saul != null) saul.gameObject.SetActive(false);
        _jAnim?.PlayLookAround(1.8f);
        yield return new WaitForSecondsRealtime(1.2f);
        if (story != null) yield return story.Show(new[] { beats.beats[1] }, overlayMode: true);

        // ── 3. The plan: Jonathan crosses to David, who turns to meet him ──
        cam?.SetShot(shotTwo, lookTwo, cut: true);
        if (planMark != null)
        {
            StartCoroutine(TurnToward(david.transform, planMark.position, 0.8f));
            yield return CutsceneSequencer.MoveTo(pc.transform, planMark, 2.2f, isPlayer: true);
        }
        _jAnim?.PlayGesture();
        yield return new WaitForSecondsRealtime(0.5f);
        if (story != null) yield return story.Show(new[] { beats.beats[2] }, overlayMode: true);

        // ── 4. Out through the doorway, crouched ──
        if (_dAnim != null) _dAnim.holdHarp = false;                 // slung back over his shoulder, between cuts
        // Jonathan leads; David follows a step behind.
        cam?.SetShot(shotSneak, pc.transform, 1.0f, cut: true);
        pc.SetCutsceneCrouch(true);
        david.SetCutsceneCrouch(true);
        Coroutine jMove = StartCoroutine(CutsceneSequencer.MoveTo(pc.transform, doorwayLeadMark, 1.9f, isPlayer: true));
        yield return new WaitForSecondsRealtime(0.7f);
        Coroutine dMove = StartCoroutine(CutsceneSequencer.MoveTo(david.transform, doorwayFollowMark, 1.9f, isPlayer: false));
        yield return new WaitForSecondsRealtime(1.6f);
        if (story != null) yield return story.Show(new[] { beats.beats[3] }, overlayMode: true);
        yield return dMove;
        yield return jMove;

        // ── 5. A fade bridges the distance to Zone 1's entry court ──
        var fadePanel = ZoneManager.Instance != null ? ZoneManager.Instance.fadePanel : null;
        yield return CutsceneSequencer.Fade(fadePanel, 1f, 0.4f);
        if (approachDavidMark != null && approachJonathanMark != null)
        {
            david.transform.SetPositionAndRotation(approachDavidMark.position, approachDavidMark.rotation);
            pc.Teleport(approachJonathanMark.position, approachJonathanMark.rotation);
        }
        cam?.SetShot(shotApproach, lookApproach, cut: true);
        yield return new WaitForSecondsRealtime(0.15f);
        yield return CutsceneSequencer.Fade(fadePanel, 0f, 0.4f);

        jMove = StartCoroutine(CutsceneSequencer.MoveTo(pc.transform, jonathanSpawn, 1.9f, isPlayer: true));
        yield return new WaitForSecondsRealtime(0.4f);
        dMove = StartCoroutine(CutsceneSequencer.MoveTo(david.transform, davidSpawn, 1.9f, isPlayer: false));
        yield return new WaitForSecondsRealtime(1.4f);
        if (story != null) yield return story.Show(new[] { beats.beats[4] }, overlayMode: true);
        yield return dMove;
        yield return jMove;

        cam?.ClearShot();
        pc.SetCutsceneCrouch(false);
        david.SetCutsceneCrouch(false);
        david.SetCutsceneControl(false);
    }

    // Slow push-in: moves the shot transform itself, which the camera follows every frame.
    static IEnumerator Dolly(Transform shot, Transform end, float duration)
    {
        if (shot == null || end == null) yield break;
        Vector3 from = shot.position;
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            shot.position = Vector3.Lerp(from, end.position, Mathf.SmoothStep(0f, 1f, t / duration));
            yield return null;
        }
        shot.position = end.position;
    }

    static IEnumerator Nudge(Transform who, Vector3 offset, float duration)
    {
        Vector3 from = who.position;
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            who.position = from + offset * Mathf.SmoothStep(0f, 1f, t / duration);
            yield return null;
        }
        who.position = from + offset;
    }

    static IEnumerator TurnToward(Transform who, Vector3 point, float duration)
    {
        Vector3 to = point - who.position; to.y = 0f;
        if (to.sqrMagnitude < 0.001f) yield break;
        Quaternion from = who.rotation, target = Quaternion.LookRotation(to);
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            who.rotation = Quaternion.Slerp(from, target, Mathf.SmoothStep(0f, 1f, t / duration));
            yield return null;
        }
        who.rotation = target;
    }

    // Tweens a standalone spear prop from Saul's hand to its resting point, with a slight arc.
    IEnumerator ThrowSpear(Vector3 from, Vector3 to)
    {
        if (thrownSpearPrefab == null) yield break;
        Vector3 dir = (to - from).normalized;
        Vector3 rest = to - dir * 2.05f;   // the head (ending 2.25 m along the prop) sinks ~0.2 m into the wall
        var spear = Instantiate(thrownSpearPrefab, from, Quaternion.LookRotation(dir));

        const float dur = 0.34f;
        for (float t = 0f; t < dur; t += Time.unscaledDeltaTime)
        {
            float k = t / dur;
            spear.transform.position = Vector3.Lerp(from, rest, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 0.3f;
            yield return null;
        }
        spear.transform.position = rest;
    }
}
