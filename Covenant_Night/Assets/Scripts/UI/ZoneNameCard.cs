using System.Collections;
using UnityEngine;
using TMPro;

// Fades a zone name card in and then out at the start of each zone.
public class ZoneNameCard : MonoBehaviour
{
    public static ZoneNameCard Instance { get; private set; }

    public CanvasGroup     cardGroup;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI subtitleText;

    public float fadeIn   = 0.5f;
    public float holdTime = 2.2f;
    public float fadeOut  = 0.8f;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (cardGroup != null) cardGroup.alpha = 0f;
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    public void Show(string zoneName, string subtitle = null)
    {
        StopAllCoroutines();
        StartCoroutine(PlayCard(zoneName, subtitle));
    }

    IEnumerator PlayCard(string zoneName, string subtitle)
    {
        nameText.text = zoneName;
        if (subtitleText != null) subtitleText.text = subtitle ?? "";
        yield return Fade(0f, 1f, fadeIn);
        yield return new WaitForSecondsRealtime(holdTime);
        yield return Fade(1f, 0f, fadeOut);
    }

    IEnumerator Fade(float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            cardGroup.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        cardGroup.alpha = to;
    }
}
