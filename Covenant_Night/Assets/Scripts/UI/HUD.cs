using UnityEngine;
using TMPro;

// Central HUD controller. All other systems call HUD.Instance.UpdateXxx().
// Assign UI references in the Inspector (the editor builder wires them).
public class HUD : MonoBehaviour
{
    public static HUD Instance { get; private set; }

    [Header("Stones")]
    public TextMeshProUGUI stoneCountText;

    [Header("Decoys")]
    public TextMeshProUGUI decoyCountText;

    [Header("David Mode")]
    public TextMeshProUGUI davidModeText;

    [Header("Alarm Timer")]
    public GameObject      alarmTimerRoot;
    public TextMeshProUGUI alarmTimerText;

    [Header("Harp")]
    public GameObject      harpUsedIndicator;   // shown once the harp has been played this zone
    public TextMeshProUGUI harpText;

    [Header("Zone")]
    public TextMeshProUGUI zoneText;
    public TextMeshProUGUI bestTimeText;

    [Header("Messages / Aim")]
    public TextMeshProUGUI messageText;
    public GameObject      reticle;

    float _messageTimer;
    bool  _harpUsed;
    CanvasGroup _group;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        _group = GetComponent<CanvasGroup>();
        if (_group == null) _group = gameObject.AddComponent<CanvasGroup>();
        _group.interactable = false;
        _group.blocksRaycasts = false;
    }

    // Gameplay readouts fade out while a cutscene shot or a story panel is up.
    void LateUpdate()
    {
        var cam = ThirdPersonCamera.Instance;
        var story = StoryPanelController.Instance;
        bool cinematic = (cam != null && cam.InShot) || (story != null && story.IsShowing);
        // gone at once when a cutscene starts (it usually starts on a cut or under a fade), eased back in after
        _group.alpha = cinematic ? 0f : Mathf.MoveTowards(_group.alpha, 1f, Time.unscaledDeltaTime * 3f);
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    void Start()
    {
        ShowAlarmTimer(false);
        RefreshHarp();
        if (messageText != null) messageText.text = "";
    }

    void Update()
    {
        if (_messageTimer > 0f)
        {
            _messageTimer -= Time.unscaledDeltaTime;
            if (_messageTimer <= 0f && messageText != null) messageText.text = "";
        }

        // Hidden indicator while the alarm countdown is held
        if (alarmTimerRoot != null && alarmTimerRoot.activeSelf && AlarmSystem.Instance != null && AlarmSystem.Instance.PlayerHidden)
            alarmTimerText.text = "Hidden: Stay Still";
    }

    public void UpdateStoneCount(int count)
    {
        if (stoneCountText != null) stoneCountText.text = $"Stones: {count}";
    }

    public void UpdateDecoyCount(int count)
    {
        if (decoyCountText != null) decoyCountText.text = $"Decoys: {count}";
    }

    public void UpdateDavidMode(string mode)
    {
        if (davidModeText != null) davidModeText.text = $"David: {mode}";
    }

    public void ShowAlarmTimer(bool visible)
    {
        if (alarmTimerRoot != null) alarmTimerRoot.SetActive(visible);
    }

    public void UpdateAlarmTimer(float seconds)
    {
        if (alarmTimerText != null && !(AlarmSystem.Instance != null && AlarmSystem.Instance.PlayerHidden))
            alarmTimerText.text = $"Hide: {Mathf.CeilToInt(Mathf.Max(0f, seconds))}";
    }

    public void ShowHarpUsed()
    {
        _harpUsed = true;
        RefreshHarp();
    }

    void RefreshHarp()
    {
        if (harpUsedIndicator != null) harpUsedIndicator.SetActive(_harpUsed);
        if (harpText != null)
        {
            harpText.text = _harpUsed ? "Harp: Used" : "Harp: Ready";
            harpText.color = _harpUsed ? new Color(0.6f, 0.6f, 0.6f) : new Color(1f, 0.9f, 0.55f);
        }
    }

    public void UpdateZone(int current, int total)
    {
        if (zoneText != null) zoneText.text = $"Zone {current} / {total}  ({GameDifficulty.Label})";
    }

    public void UpdateBestTime(string text)
    {
        if (bestTimeText != null) bestTimeText.text = $"Best: {text}";
    }

    public void ShowMessage(string message, float seconds = 2.5f)
    {
        if (messageText == null) return;
        messageText.text = message;
        _messageTimer = seconds;
    }

    // Called by ZoneManager on zone reset
    public void ResetForZone()
    {
        ShowAlarmTimer(false);
        _harpUsed = false;
        RefreshHarp();
    }
}
