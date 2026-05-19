using System.Collections;
using UnityEngine;

// Place on a trigger collider at the Zone 5 exit arch.
// Overrides the normal ZoneExit flow: shows the gate-bluff panels, then the
// farewell epilogue, then triggers the win state. No stealth solution is required.
public class GateFinalSequence : MonoBehaviour
{
    [Header("Story Panels")]
    public StoryBeatData gateBluffBeats;
    public StoryBeatData farewellBeats;

    bool _triggered;

    void OnTriggerEnter(Collider other)
    {
        if (_triggered) return;
        if (!other.CompareTag("Player")) return;
        if (AlarmSystem.Instance != null && AlarmSystem.Instance.IsAlarmed) return;

        _triggered = true;
        StartCoroutine(PlayFinalSequence());
    }

    IEnumerator PlayFinalSequence()
    {
        GameManager.Instance.Pause();

        if (gateBluffBeats != null && StoryPanelController.Instance != null)
            yield return StartCoroutine(StoryPanelController.Instance.Show(gateBluffBeats.beats));

        if (farewellBeats != null && StoryPanelController.Instance != null)
            yield return StartCoroutine(StoryPanelController.Instance.Show(farewellBeats.beats));

        GameManager.Instance.TriggerWin();
    }
}
