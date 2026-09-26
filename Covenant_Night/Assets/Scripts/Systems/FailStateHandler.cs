using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

// Listens for GameManager fail/win events and drives the UI response.
// Attach to the persistent GameManager GameObject alongside GameManager.cs.
//
// Fail flow (GDD 3.8):
//   David captured   – brief text panel, zone resets to its entry checkpoint.
//   Jonathan captured – a short cutscene: Saul's guards bring Jonathan before the king; he reveals
//                       nothing; the zone resets. Failure becomes story.
//   Alarm expired     – "the guards have closed in" panel, zone resets.
// Win flow: the epilogue has already played (GateFinalSequence); this shows the credits.
public class FailStateHandler : MonoBehaviour
{
    [Header("Fail UI")]
    public CanvasGroup     failPanel;
    public TextMeshProUGUI failTitle;
    public TextMeshProUGUI failReasonText;

    [Header("Win / Credits UI")]
    public CanvasGroup winPanel;

    [Header("Cutscene beats")]
    public StoryBeatData jonathanCapturedBeats;   // played after the fail panel when Jonathan is caught

    [Header("Timing")]
    public float showDelay    = 1.0f;
    public float holdTime     = 3.0f;
    public float creditsInputDelay = 2.0f;

    void OnEnable()
    {
        GameManager.OnFailState += HandleFail;
        GameManager.OnWinState  += HandleWin;
    }

    void OnDisable()
    {
        GameManager.OnFailState -= HandleFail;
        GameManager.OnWinState  -= HandleWin;
    }

    void HandleFail(FailReason reason) => StartCoroutine(ShowFailSequence(reason));
    void HandleWin()                   => StartCoroutine(ShowWinSequence());

    IEnumerator ShowFailSequence(FailReason reason)
    {
        yield return new WaitForSecondsRealtime(showDelay);

        string title, line;
        switch (reason)
        {
            case FailReason.DavidCaptured:
                title = "David Seized";
                line  = "Saul's guards have laid hands on David. Jonathan pulls back into the dark —\nthe night begins again.";
                break;
            case FailReason.JonathanCaptured:
                title = "Caught";
                line  = "Jonathan is taken.";
                break;
            default:
                title = "The City Closes In";
                line  = "The alarm rings through Gibeah and every exit is barred.\nThe night begins again.";
                break;
        }

        if (failTitle != null) failTitle.text = title;
        if (failReasonText != null) failReasonText.text = line;

        if (failPanel != null)
        {
            failPanel.gameObject.SetActive(true);
            yield return Fade(failPanel, 0f, 1f, 0.5f);
        }

        yield return new WaitForSecondsRealtime(reason == FailReason.JonathanCaptured ? 1.6f : holdTime);

        // Jonathan captured: narrative cutscene before the reset
        if (reason == FailReason.JonathanCaptured && jonathanCapturedBeats != null && StoryPanelController.Instance != null)
            yield return StoryPanelController.Instance.Show(jonathanCapturedBeats.beats);

        if (failPanel != null)
        {
            yield return Fade(failPanel, 1f, 0f, 0.3f);
            failPanel.gameObject.SetActive(false);
        }

        ZoneManager.Instance?.RestartCurrentZone();
    }

    IEnumerator ShowWinSequence()
    {
        yield return new WaitForSecondsRealtime(showDelay);

        if (winPanel != null)
        {
            winPanel.gameObject.SetActive(true);
            yield return Fade(winPanel, 0f, 1f, 1.5f);
        }

        // Credits: wait, then any key restarts the whole game from the Persistent scene
        yield return new WaitForSecondsRealtime(creditsInputDelay);
        while (!InputReader.ConfirmPressedThisFrame()) yield return null;
        SceneManager.LoadScene("Persistent");     // Single mode: unloads every zone and rebuilds all managers
    }

    IEnumerator Fade(CanvasGroup cg, float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        cg.alpha = to;
    }
}
