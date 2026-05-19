using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Displays a sequence of illustrated text panels between zones.
// Call Show(lines) from ZoneManager before loading the next zone.
public class StoryPanelController : MonoBehaviour
{
    public static StoryPanelController Instance { get; private set; }

    [Header("UI")]
    public CanvasGroup     panelGroup;
    public TextMeshProUGUI bodyText;
    public TextMeshProUGUI continuePrompt;
    public Image           illustration;   // optional; swap sprite per panel

    [Header("Timing")]
    public float fadeTime   = 0.4f;
    public float typeSpeed  = 0.03f;  // seconds per character

    bool _waitingForInput;
    bool _skipType;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        panelGroup.alpha = 0f;
        panelGroup.gameObject.SetActive(false);
    }

    // panels: array of (text, sprite) pairs. Sprite can be null.
    public IEnumerator Show(StoryBeat[] beats)
    {
        panelGroup.gameObject.SetActive(true);
        yield return Fade(1f);

        foreach (var beat in beats)
        {
            if (illustration != null && beat.sprite != null)
            {
                illustration.sprite  = beat.sprite;
                illustration.enabled = true;
            }
            else if (illustration != null)
            {
                illustration.enabled = false;
            }

            yield return TypeText(beat.text);
            yield return WaitForContinue();
        }

        yield return Fade(0f);
        panelGroup.gameObject.SetActive(false);
    }

    IEnumerator TypeText(string text)
    {
        bodyText.text = "";
        continuePrompt.gameObject.SetActive(false);
        _skipType = false;

        foreach (char c in text)
        {
            if (_skipType) { bodyText.text = text; break; }
            bodyText.text += c;
            yield return new WaitForSecondsRealtime(typeSpeed);
        }

        continuePrompt.gameObject.SetActive(true);
    }

    IEnumerator WaitForContinue()
    {
        _waitingForInput = true;
        while (_waitingForInput) yield return null;
    }

    void Update()
    {
        if (Input.anyKeyDown)
        {
            if (_skipType)        { _skipType = false; return; }
            if (bodyText != null && bodyText.text.Length > 0) _skipType = true;
            _waitingForInput = false;
        }
    }

    IEnumerator Fade(float target)
    {
        float start = panelGroup.alpha;
        float t = 0f;
        while (t < fadeTime)
        {
            t += Time.unscaledDeltaTime;
            panelGroup.alpha = Mathf.Lerp(start, target, t / fadeTime);
            yield return null;
        }
        panelGroup.alpha = target;
    }
}

[System.Serializable]
public class StoryBeat
{
    [TextArea(3, 8)] public string text;
    public Sprite sprite;
}
