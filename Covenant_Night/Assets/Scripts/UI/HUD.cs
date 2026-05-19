using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Central HUD controller. All other systems call HUD.Instance.UpdateXxx().
// Assign UI references in the Inspector.
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
    public GameObject harpUsedIndicator;

    [Header("Zone")]
    public TextMeshProUGUI zoneText;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        ShowAlarmTimer(false);
        if (harpUsedIndicator != null) harpUsedIndicator.SetActive(false);
    }

    public void UpdateStoneCount(int count)
    {
        if (stoneCountText != null)
            stoneCountText.text = $"Stones: {count}";
    }

    public void UpdateDavidMode(string mode)
    {
        if (davidModeText != null)
            davidModeText.text = $"David: {mode}";
    }

    public void ShowAlarmTimer(bool visible)
    {
        if (alarmTimerRoot != null) alarmTimerRoot.SetActive(visible);
    }

    public void UpdateAlarmTimer(float seconds)
    {
        if (alarmTimerText != null)
            alarmTimerText.text = $"HIDE: {Mathf.CeilToInt(seconds)}";
    }

    public void ShowHarpUsed()
    {
        if (harpUsedIndicator != null) harpUsedIndicator.SetActive(true);
    }

    public void UpdateZone(int current, int total)
    {
        if (zoneText != null)
            zoneText.text = $"Zone {current} / {total}";
    }

    // Called by ZoneManager on zone reset
    public void ResetForZone()
    {
        ShowAlarmTimer(false);
        if (harpUsedIndicator != null) harpUsedIndicator.SetActive(false);
    }
}
