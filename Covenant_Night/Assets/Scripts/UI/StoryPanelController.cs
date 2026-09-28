using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Displays a sequence of illustrated text panels (Unity UI canvas): zone intros, gate scene,
// fail cutscene, epilogue. `yield return StoryPanelController.Instance.Show(beats)`.
// Any key / click: first press completes the typewriter, the next advances.
public class StoryPanelController : MonoBehaviour
{
    public static StoryPanelController Instance { get; private set; }

    [Header("UI")]
    public CanvasGroup     panelGroup;
    public Image           background;     // opaque backdrop; hidden in overlay mode so a live camera shot shows through
    public TextMeshProUGUI headingText;
    public TextMeshProUGUI bodyText;
    public TextMeshProUGUI continuePrompt;
    public Image           illustration;   // optional; sprite swapped per panel

    [Header("Timing")]
    public float fadeTime        = 0.4f;
    public float charsPerSecond  = 55f;
    public float inputGuardTime  = 0.2f;   // ignore presses right after a panel appears

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        panelGroup.alpha = 0f;
        panelGroup.gameObject.SetActive(false);
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    // beats: (heading, text, sprite) entries. Sprite can be null.
    // overlayMode: hides the opaque backdrop and illustration so the caption plays over a live camera shot
    // instead of a static picture (used by the intro, zone transitions and the ending cutscenes).
    public IEnumerator Show(StoryBeat[] beats, bool overlayMode = false)
    {
        if (beats == null || beats.Length == 0) yield break;

        if (background != null) background.gameObject.SetActive(!overlayMode);
        panelGroup.gameObject.SetActive(true);
        yield return Fade(1f);

        foreach (var beat in beats)
        {
            if (illustration != null)
            {
                bool showSprite = !overlayMode && beat.sprite != null;
                illustration.enabled = showSprite;
                if (showSprite) illustration.sprite = beat.sprite;
            }
            if (headingText != null) headingText.text = beat.heading ?? "";

            yield return TypeAndWait(beat.text);
        }

        yield return Fade(0f);
        panelGroup.gameObject.SetActive(false);
        if (background != null) background.gameObject.SetActive(true);
    }

    IEnumerator TypeAndWait(string text)
    {
        bodyText.text = text;
        bodyText.maxVisibleCharacters = 0;
        bodyText.ForceMeshUpdate();
        int total = bodyText.textInfo.characterCount;
        continuePrompt.gameObject.SetActive(false);

        float t = 0f;
        float guard = inputGuardTime;
        bool skipped = false;
        while (!skipped && bodyText.maxVisibleCharacters < total)
        {
            t += Time.unscaledDeltaTime;
            guard -= Time.unscaledDeltaTime;
            bodyText.maxVisibleCharacters = Mathf.Min(total, Mathf.FloorToInt(t * charsPerSecond));
            if (guard <= 0f && InputReader.ConfirmPressedThisFrame()) skipped = true;
            yield return null;
        }
        bodyText.maxVisibleCharacters = total;
        continuePrompt.gameObject.SetActive(true);

        yield return null;   // never let the skip press also advance
        while (true)
        {
            guard -= Time.unscaledDeltaTime;
            if (guard <= 0f && InputReader.ConfirmPressedThisFrame()) break;
            yield return null;
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
    public string heading;
    [TextArea(3, 8)] public string text;
    public Sprite sprite;
}
