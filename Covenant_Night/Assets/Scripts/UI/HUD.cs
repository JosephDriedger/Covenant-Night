using UnityEngine;
using TMPro;

// Central HUD controller. All other systems call HUD.Instance.UpdateXxx().
// Assign UI references in the Inspector (the editor builder wires them).
public class HUD : MonoBehaviour
{
    public static HUD Instance { get; private set; }

    [Header("Stones")]
    public TextMeshProUGUI stoneCountText;

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

    [Header("Messages / Aim")]
    public TextMeshProUGUI messageText;
    public GameObject      reticle;

    float _messageTimer;
    bool  _harpUsed;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
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
            alarmTimerText.text = "HIDDEN — hold still";
    }

    public void UpdateStoneCount(int count)
    {
        if (stoneCountText != null) stoneCountText.text = $"Stones: {count}";
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
            alarmTimerText.text = $"HIDE: {Mathf.CeilToInt(Mathf.Max(0f, seconds))}";
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
            harpText.text = _harpUsed ? "Harp: used" : "Harp: ready";
            harpText.color = _harpUsed ? new Color(0.6f, 0.6f, 0.6f) : new Color(1f, 0.9f, 0.55f);
        }
    }

    public void UpdateZone(int current, int total)
    {
        if (zoneText != null) zoneText.text = $"Zone {current} / {total}";
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
