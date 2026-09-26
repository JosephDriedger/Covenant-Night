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
    [Header("Story Panels")]
    public StoryBeatData gateBluffBeats;
    public StoryBeatData farewellBeats;
    public StoryBeatData epilogueBeats;

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
    public Transform shotEmptyGate;
    public Transform lookAtGate;

    [Header("Audio")]
    public AudioSource sfxSource;
    public AudioClip   gateCreak;

    bool _triggered;
    BoxCollider _box;
    float _messageCooldown;

    void Awake() => _box = GetComponent<BoxCollider>();

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
            Say("The alarm is up — the commander will not listen!");
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
        yield return MoveTo(pc.transform, jonathanMark, 2.2f, isPlayer: true);
        yield return MoveTo(david.transform, davidMark, 1.6f, isPlayer: false);

        // 2. The bluff
        if (gateBluffBeats != null && story != null) yield return story.Show(gateBluffBeats.beats);

        // 3. Gate opens
        pc.GetComponent<ProceduralCharacterAnim>()?.PlayRaiseHand(3.6f);
        if (sfxSource != null && gateCreak != null) sfxSource.PlayOneShot(gateCreak);
        Vector3 cmdStart = commander != null ? commander.position : Vector3.zero;
        Vector3 lStart = gateLeft != null ? gateLeft.position : Vector3.zero;
        Vector3 rStart = gateRight != null ? gateRight.position : Vector3.zero;
        for (float t = 0f; t < 3.5f; t += Time.unscaledDeltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / 3.5f);
            if (gateLeft != null)  gateLeft.position  = lStart + Vector3.left  * (gateOpenDistance * k);
            if (gateRight != null) gateRight.position = rStart + Vector3.right * (gateOpenDistance * k);
            if (commander != null) commander.position = cmdStart + commanderStepAside * Mathf.Clamp01(k * 1.4f);
            yield return null;
        }

        // 4. David walks through the gate into the hills; Jonathan watches
        cam?.SetShot(shotFarewell, david.transform);
        if (davidExitPath != null)
            foreach (var p in davidExitPath)
                yield return MoveTo(david.transform, p, 1.9f, isPlayer: false);

        if (farewellBeats != null && story != null) yield return story.Show(farewellBeats.beats);

        // 5. Epilogue over the empty gate
        david.gameObject.SetActive(false);
        cam?.SetShot(shotEmptyGate, lookAtGate);
        TensionAudioManager.Instance?.PlayEpilogue();
        yield return new WaitForSecondsRealtime(1.2f);
        if (epilogueBeats != null && story != null) yield return story.Show(epilogueBeats.beats);

        GameManager.Instance.TriggerWin();
    }

    // Smoothly move a character to a mark at walking speed, facing the direction of travel (then the mark's facing).
    IEnumerator MoveTo(Transform who, Transform mark, float speed, bool isPlayer)
    {
        if (mark == null) yield break;
        var pc = PlayerController.Instance;
        while (true)
        {
            Vector3 to = mark.position - who.position; to.y = 0f;
            if (to.magnitude < 0.08f) break;
            Vector3 next = who.position + to.normalized * Mathf.Min(to.magnitude, speed * Time.unscaledDeltaTime);
            Quaternion rot = Quaternion.LookRotation(to.normalized);
            if (isPlayer) pc.Teleport(next, Quaternion.Slerp(who.rotation, rot, 0.25f));
            else who.SetPositionAndRotation(next, Quaternion.Slerp(who.rotation, rot, 0.25f));
            yield return null;
        }
        // Settle facing the mark's forward direction
        if (isPlayer) pc.Teleport(who.position, mark.rotation);
        else who.rotation = mark.rotation;
    }
}
