using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Displays a sequence of illustrated text panels (Unity UI canvas): zone intros, gate scene,
// fail cutscene, epilogue. `yield return StoryPanelController.Instance.Show(beats)`.
// Space / Enter / click / A: the first press completes the typewriter, a later one advances.
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
    public RectTransform[] captionParts;   // plate, heading, body: re-laid out low on screen over a live cutscene shot
    [Tooltip("Over a live shot: caption bottom edge, in canvas units above the screen bottom (clears the letterbox bar).")]
    public float           overlayBottom = 122f;
    public float           overlayBodySize = 28f;

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
            _base = new RectState[captionParts.Length];
            for (int i = 0; i < captionParts.Length; i++) _base[i] = new RectState(captionParts[i]);
        }
        _baseBodySize = bodyText.fontSize;
    }

    struct RectState
    {
        public Vector2 anchorMin, anchorMax, pivot, pos, size;
        public RectState(RectTransform rt)
        {
            anchorMin = rt.anchorMin; anchorMax = rt.anchorMax; pivot = rt.pivot; pos = rt.anchoredPosition; size = rt.sizeDelta;
        }
        public void Apply(RectTransform rt)
        {
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax; rt.pivot = pivot; rt.anchoredPosition = pos; rt.sizeDelta = size;
        }
    }

    RectState[] _base;
    float _baseBodySize;

    // Over a live shot the caption is stacked up from just above the letterbox bar and sized to its text,
    // so it covers as little of the action as possible and never runs into the bar. Full-screen panels
    // keep the builder's layout.
    void LayoutCaption(string text, bool overlayMode)
    {
        if (captionParts == null || _base == null || captionParts.Length < 3) return;
        for (int i = 0; i < captionParts.Length; i++) _base[i].Apply(captionParts[i]);
        bodyText.fontSize = overlayMode ? overlayBodySize : _baseBodySize;
        if (!overlayMode) return;

        RectTransform plate = captionParts[0], head = captionParts[1], body = captionParts[2];
        const float pad = 16f, gap = 6f;
        float bodyH = Mathf.Ceil(bodyText.GetPreferredValues(text ?? "", body.sizeDelta.x, 0f).y) + 4f;
        float headH = head.sizeDelta.y;
        PinToBottom(body, overlayBottom + pad, bodyH);
        PinToBottom(head, overlayBottom + pad + bodyH + gap, headH);
        PinToBottom(plate, overlayBottom, pad + bodyH + gap + headH + pad * 0.6f);
    }

    static void PinToBottom(RectTransform rt, float y, float height)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, y);
        rt.sizeDelta = new Vector2(rt.sizeDelta.x, height);
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
        LayoutCaption(beats[0].text, overlayMode);
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
            LayoutCaption(beat.text, overlayMode);

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
