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
    public RectTransform[] captionParts;   // plate, heading, body: dropped lower over a live cutscene shot
    public float           overlayDrop = 105f;

    [Header("Timing")]
    public float fadeTime        = 0.4f;
    public float charsPerSecond  = 45f;
    public float inputGuardTime  = 0.5f;   // ignore presses right after a panel appears
    public float minReadTime     = 1.0f;   // the full text stays up at least this long before it can be dismissed

    public bool IsShowing { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        panelGroup.alpha = 0f;
        panelGroup.gameObject.SetActive(false);
        if (illustration != null) illustration.enabled = false;
        if (captionParts != null)
        {
            _captionBase = new Vector2[captionParts.Length];
            for (int i = 0; i < captionParts.Length; i++) _captionBase[i] = captionParts[i].anchoredPosition;
        }
    }

    Vector2[] _captionBase;

    // Over a live shot the caption sits low, toward the letterbox bar, so it covers less of the action.
    void PlaceCaption(bool overlayMode)
    {
        if (captionParts == null || _captionBase == null) return;
        for (int i = 0; i < captionParts.Length; i++)
            captionParts[i].anchoredPosition = _captionBase[i] + (overlayMode ? Vector2.down * overlayDrop : Vector2.zero);
    }

    void SetIllustration(StoryBeat beat, bool overlayMode)
    {
        if (illustration == null) return;
        bool showSprite = !overlayMode && beat.sprite != null;
        if (showSprite) illustration.sprite = beat.sprite;
        illustration.enabled = showSprite;
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    // beats: (heading, text, sprite) entries. Sprite can be null.
    // overlayMode: hides the opaque backdrop and illustration so the caption plays over a live camera shot
    // instead of a static picture (used by the intro, zone transitions and the ending cutscenes).
    public IEnumerator Show(StoryBeat[] beats, bool overlayMode = false)
    {
        if (beats == null || beats.Length == 0) yield break;
        IsShowing = true;

        // Everything is set for the first beat before the panel becomes visible, so nothing stale (or the
        // illustration's blank default) can flash up during the fade-in.
        if (background != null) background.gameObject.SetActive(!overlayMode);
        PlaceCaption(overlayMode);
        SetIllustration(beats[0], overlayMode);
        if (headingText != null) headingText.text = beats[0].heading ?? "";
        bodyText.text = "";
        continuePrompt.gameObject.SetActive(false);
        panelGroup.gameObject.SetActive(true);
        yield return Fade(1f);

        foreach (var beat in beats)
        {
            SetIllustration(beat, overlayMode);
            if (headingText != null) headingText.text = beat.heading ?? "";

            yield return TypeAndWait(beat.text);
        }

        yield return Fade(0f);
        panelGroup.gameObject.SetActive(false);
        if (background != null) background.gameObject.SetActive(true);
        if (illustration != null) illustration.enabled = false;
        IsShowing = false;
    }

    IEnumerator TypeAndWait(string text)
    {
        bodyText.text = text;
        bodyText.maxVisibleCharacters = 0;
        bodyText.ForceMeshUpdate();
        int total = bodyText.textInfo.characterCount;
        continuePrompt.gameObject.SetActive(false);
        bool pad = InputReader.Instance != null && InputReader.Instance.UsingGamepad;
        continuePrompt.text = pad ? "Press A / Cross to Continue" : "Press Space, Enter or Click to Continue";

        // First press reveals the rest of the text at once; it can't also dismiss the panel.
        float t = 0f;
        float guard = inputGuardTime;
        while (bodyText.maxVisibleCharacters < total)
        {
            t += Time.unscaledDeltaTime;
            guard -= Time.unscaledDeltaTime;
            bodyText.maxVisibleCharacters = Mathf.Min(total, Mathf.FloorToInt(t * charsPerSecond));
            if (guard <= 0f && InputReader.ConfirmPressedThisFrame()) break;
            yield return null;
        }
        bodyText.maxVisibleCharacters = total;

        // The full text then stays up for a minimum read time before a press can move on.
        for (float hold = 0f; hold < minReadTime; hold += Time.unscaledDeltaTime)
            yield return null;
        continuePrompt.gameObject.SetActive(true);
        while (!InputReader.ConfirmPressedThisFrame()) yield return null;
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
