using System.Collections;
using UnityEngine;
using TMPro;

// Fades a zone name card in and then out at the start of each zone.
public class ZoneNameCard : MonoBehaviour
{
    public static ZoneNameCard Instance { get; private set; }

    public CanvasGroup     cardGroup;
    public TextMeshProUGUI nameText;

    public float fadeIn   = 0.5f;
    public float holdTime = 2.0f;
    public float fadeOut  = 0.8f;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        if (cardGroup != null) cardGroup.alpha = 0f;
    }

    public void Show(string zoneName)
    {
        StopAllCoroutines();
        StartCoroutine(PlayCard(zoneName));
    }

    IEnumerator PlayCard(string zoneName)
    {
        nameText.text = zoneName;
        yield return Fade(0f, 1f, fadeIn);
        yield return new WaitForSeconds(holdTime);
        yield return Fade(1f, 0f, fadeOut);
    }

    IEnumerator Fade(float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            cardGroup.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        cardGroup.alpha = to;
    }
}
