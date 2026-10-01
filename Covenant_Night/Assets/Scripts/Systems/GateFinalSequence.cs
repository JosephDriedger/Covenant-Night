using System.Collections;
using UnityEngine;

// Zone 5 finale (GDD 3.7 / 3.9). Place on a trigger collider in front of the Eastern Gate.
// Replaces the normal ZoneExit: once Jonathan and David reach the gate (and no alarm is up) a scripted
// sequence plays — no stealth solution required:
//   1. both walk to their marks facing the gate commander
//   2. story panels: Jonathan bluffs with his father's signet ring
//   3. the commander steps aside and the gate groans open
//   4. David walks through into the dark hills; Jonathan watches from the threshold (farewell panels)
//   5. epilogue over a still of the empty gate (1 Samuel 20:42), then the win state / credits
public class GateFinalSequence : MonoBehaviour
{
    public static GateFinalSequence Instance { get; private set; }

    [Header("Story Panels")]
    public StoryBeatData gateBluffBeats;
    public StoryBeatData farewellBeats;
    public StoryBeatData epilogueBeats;

    [Header("Alternate Ending: Jonathan Detained")]
    public StoryBeatData detainedBeats;
    public StoryBeatData aloneEpilogueBeats;

    [Header("Scene References")]
    public Transform jonathanMark;
    public Transform davidMark;
    public Transform commander;
    public Vector3   commanderStepAside = new Vector3(3f, 0f, 0f);
    public Transform gateLeft;
    public Transform gateRight;
    public float     gateOpenDistance = 3.4f;
    public Transform[] davidExitPath;

    [Header("Camera Shots")]
    public Transform shotBluff;
    public Transform shotFarewell;
    public Transform lookFarewell;          // a point beyond the gate: David walks away into depth
    public Transform shotEmptyGate;
    public Transform shotEpilogue;          // from inside the open gateway, back toward Jonathan at the threshold
    public Transform lookAtGate;

    [Header("Audio")]
    public AudioSource sfxSource;
    public AudioClip   gateCreak;

    bool _triggered;
    BoxCollider _box;
    float _messageCooldown;

    void Awake()
    {
        _box = GetComponent<BoxCollider>();
        Instance = this;
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    // Called by GuardFSM instead of a normal fail when Jonathan is caught in this zone before the bluff
    // has started. Returns false (letting the normal fail proceed) once the bluff sequence is underway,
    // so a capture mid-cutscene still behaves the way it always has.
    public bool TryDetainJonathan()
    {
        if (_triggered || (GameManager.Instance != null && GameManager.Instance.HasEnded)) return false;
        // Only makes narrative sense once David has actually reached the gate approach; otherwise this is
        // a normal capture, however Jonathan lost track of him.
        var david = DavidCompanion.Instance;
        if (david == null || david.transform.position.z < 48f) return false;
        _triggered = true;
        StartCoroutine(PlayAlternateEnding());
        return true;
    }

    // Position tests (not trigger events): see ZoneExit for why.
    bool Inside(Transform t)
    {
        if (t == null || _box == null) return false;
        Bounds b = _box.bounds;
        Vector3 p = t.position;
        return p.x >= b.min.x && p.x <= b.max.x && p.z >= b.min.z && p.z <= b.max.z && p.y > -1f && p.y < b.max.y;
    }

    void Update()
    {
        if (_triggered || !Inside(PlayerController.Instance != null ? PlayerController.Instance.transform : null)) return;
        if (GameManager.Instance != null && GameManager.Instance.IsPaused) return;

        _messageCooldown -= Time.deltaTime;

        if (AlarmSystem.Instance != null && AlarmSystem.Instance.IsAlarmed)
        {
            Say("The commander will not listen while the alarm is sounding.");
            return;
        }

        var david = DavidCompanion.Instance;
        var pc = PlayerController.Instance;
        bool davidClose = Inside(david != null ? david.transform : null) ||
            (david != null && pc != null && Vector3.Distance(david.transform.position, pc.transform.position) < 5f);
        if (!davidClose)
        {
            Say("David must be beside you at the gate.");
            return;
        }

        _triggered = true;
        StartCoroutine(PlayFinalSequence());
    }

    void Say(string msg)
    {
        if (_messageCooldown > 0f) return;
        _messageCooldown = 3f;
        HUD.Instance?.ShowMessage(msg, 2.5f);
    }

    IEnumerator PlayFinalSequence()
    {
        var pc = PlayerController.Instance;
        var david = DavidCompanion.Instance;
        var cam = ThirdPersonCamera.Instance;
        var story = StoryPanelController.Instance;

        GameManager.Instance.Pause();
        AlarmSystem.Instance?.Reset();
        david.SetCutsceneControl(true);

        // 1. Walk to marks
        cam?.SetShot(shotBluff, lookAtGate);
        yield return CutsceneSequencer.MoveTo(pc.transform, jonathanMark, 2.2f, isPlayer: true);
        yield return CutsceneSequencer.MoveTo(david.transform, davidMark, 1.6f, isPlayer: false);

        // 2. The bluff
        if (gateBluffBeats != null && story != null) yield return story.Show(gateBluffBeats.beats, overlayMode: true);

        // 3. Gate opens
        pc.GetComponent<ProceduralCharacterAnim>()?.PlayRaiseHand(3.6f);
        yield return OpenGate(1f, 3.5f, commanderStepsAside: true);

        // 4. Over Jonathan's shoulder: David walks out through the gate into the hills. The farewell caption
        //    comes up once he is through, while he keeps walking.
        FrameFarewell(cam, david);
        Coroutine walk = StartCoroutine(WalkOut(david.transform, 2.1f));
        yield return new WaitForSecondsRealtime(0.8f);
        pc.GetComponent<ProceduralCharacterAnim>()?.PlayFarewellWave(3f);
        yield return new WaitForSecondsRealtime(3.4f);

        if (farewellBeats != null && story != null) yield return story.Show(farewellBeats.beats, overlayMode: true);
        StopCoroutine(walk);

        // 5. Epilogue: reverse angle on Jonathan, alone at the threshold, looking out after his friend
        david.gameObject.SetActive(false);
        if (shotEpilogue != null) cam?.SetShot(shotEpilogue, pc.transform, 1.45f, cut: true);
        else cam?.SetShot(shotEmptyGate, lookAtGate, cut: true);
        TensionAudioManager.Instance?.PlayEpilogue();
        yield return new WaitForSecondsRealtime(2.4f);
        if (epilogueBeats != null && story != null) yield return story.Show(epilogueBeats.beats, overlayMode: true);

        GameManager.Instance.TriggerWin();
    }

    IEnumerator OpenGate(float fraction, float duration, bool commanderStepsAside)
    {
        if (sfxSource != null && gateCreak != null) sfxSource.PlayOneShot(gateCreak);
        Vector3 cmdStart = commander != null ? commander.position : Vector3.zero;
        Vector3 lStart = gateLeft != null ? gateLeft.position : Vector3.zero;
        Vector3 rStart = gateRight != null ? gateRight.position : Vector3.zero;
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / duration);
            float open = gateOpenDistance * fraction * k;
            if (gateLeft != null)  gateLeft.position  = lStart + Vector3.left  * open;
            if (gateRight != null) gateRight.position = rStart + Vector3.right * open;
            if (commanderStepsAside && commander != null) commander.position = cmdStart + commanderStepAside * Mathf.Clamp01(k * 1.4f);
            yield return null;
        }
    }

    void FrameFarewell(ThirdPersonCamera cam, DavidCompanion david)
    {
        if (lookFarewell != null) cam?.SetShot(shotFarewell, lookFarewell, cut: true);
        else cam?.SetShot(shotFarewell, david.transform, 1.2f, cut: true);
    }

    IEnumerator WalkOut(Transform who, float speed)
    {
        if (davidExitPath == null) yield break;
        foreach (var p in davidExitPath)
            yield return CutsceneSequencer.MoveTo(who, p, speed, isPlayer: false);
    }

    // Jonathan is seized before he can bluff his way through, but David — already close to the gate — slips
    // through alone in the confusion. Reuses the same shots and exit path as the normal ending.
    IEnumerator PlayAlternateEnding()
    {
        var pc = PlayerController.Instance;
        var david = DavidCompanion.Instance;
        var cam = ThirdPersonCamera.Instance;
        var story = StoryPanelController.Instance;

        GameManager.Instance.Pause();
        david.SetCutsceneControl(true);

        // The commander leaves his post to confront Jonathan, which is what leaves the gate unwatched.
        cam?.SetShot(shotBluff, lookAtGate, cut: true);
        pc.GetComponent<ProceduralCharacterAnim>()?.PlayFlinch();
        Transform confront = null;
        Coroutine stride = null;
        if (commander != null)
        {
            Vector3 toCmd = commander.position - pc.transform.position; toCmd.y = 0f;
            confront = new GameObject("CommanderConfront").transform;
            confront.position = pc.transform.position + toCmd.normalized * 1.3f;
            confront.rotation = Quaternion.LookRotation(-toCmd.normalized);
            stride = StartCoroutine(CutsceneSequencer.MoveTo(commander, confront, 2.4f, isPlayer: false));
        }
        if (detainedBeats != null && story != null) yield return story.Show(detainedBeats.beats, overlayMode: true);

        // Unnoticed, David eases the gate ajar and slips out.
        if (davidMark != null) david.transform.SetPositionAndRotation(davidMark.position, davidMark.rotation);
        FrameFarewell(cam, david);
        StartCoroutine(OpenGate(0.45f, 1.8f, commanderStepsAside: false));
        Coroutine walk = StartCoroutine(WalkOut(david.transform, 2.1f));
        yield return new WaitForSecondsRealtime(5.5f);
        StopCoroutine(walk);
        if (stride != null) StopCoroutine(stride);
        if (confront != null) Destroy(confront.gameObject);

        david.gameObject.SetActive(false);
        cam?.SetShot(shotEmptyGate, lookAtGate, cut: true);
        yield return new WaitForSecondsRealtime(1.6f);
        if (aloneEpilogueBeats != null && story != null) yield return story.Show(aloneEpilogueBeats.beats, overlayMode: true);

        GameManager.Instance.TriggerAlternateEnding();
    }
}
