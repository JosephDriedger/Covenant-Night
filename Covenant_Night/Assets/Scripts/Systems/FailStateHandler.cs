using System.Collections;
using UnityEngine;

// Listens for GameManager fail/win events and drives the UI response.
// Attach to the persistent GameManager GameObject alongside GameManager.cs.
public class FailStateHandler : MonoBehaviour
{
    [Header("UI")]
    public CanvasGroup failPanel;
    public CanvasGroup winPanel;

    [Header("Timing")]
    public float showDelay   = 1.2f;
    public float restartDelay = 2.5f;

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

    void HandleFail() => StartCoroutine(ShowFailSequence());
    void HandleWin()  => StartCoroutine(ShowWinSequence());

    IEnumerator ShowFailSequence()
    {
        yield return new WaitForSeconds(showDelay);

        if (failPanel != null)
        {
            failPanel.gameObject.SetActive(true);
            yield return Fade(failPanel, 0f, 1f, 0.5f);
        }

        yield return new WaitForSeconds(restartDelay);

        if (failPanel != null)
            yield return Fade(failPanel, 1f, 0f, 0.3f);

        ZoneManager.Instance?.RestartCurrentZone();
    }

    IEnumerator ShowWinSequence()
    {
        yield return new WaitForSeconds(showDelay);

        if (winPanel != null)
        {
            winPanel.gameObject.SetActive(true);
            yield return Fade(winPanel, 0f, 1f, 1.0f);
        }
        // Win state: no auto-restart; let the player sit in the epilogue
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
