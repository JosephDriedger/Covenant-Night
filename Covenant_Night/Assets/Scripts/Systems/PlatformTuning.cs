using UnityEngine;

// Per-platform runtime settings. Desktop (Windows / macOS / Linux) keeps the quality-settings defaults;
// consoles get a fixed 60 fps target, a shorter shadow distance and no idle sleep, and hide the cursor.
public class PlatformTuning : MonoBehaviour
{
    public float consoleShadowDistance = 35f;
    public int   consoleTargetFrameRate = 60;

    void Awake()
    {
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        if (Application.isConsolePlatform || Application.isMobilePlatform)
        {
            QualitySettings.vSyncCount = 1;
            Application.targetFrameRate = consoleTargetFrameRate;
            QualitySettings.shadowDistance = Mathf.Min(QualitySettings.shadowDistance, consoleShadowDistance);
            Cursor.visible = false;
        }
    }
}
