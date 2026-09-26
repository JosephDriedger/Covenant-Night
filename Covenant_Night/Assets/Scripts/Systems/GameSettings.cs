using UnityEngine;

// Player-adjustable options, persisted with PlayerPrefs. Edited from the Settings screen (MenuController).
public static class GameSettings
{
    const string VolumeKey  = "cn.volume";
    const string LookKey    = "cn.lookScale";
    const string InvertKey  = "cn.invertY";
    const string FullKey    = "cn.fullscreen";

    public const float MinLookScale = 0.3f;
    public const float MaxLookScale = 2.5f;

    public static float Volume
    {
        get => PlayerPrefs.GetFloat(VolumeKey, 1f);
        set { PlayerPrefs.SetFloat(VolumeKey, Mathf.Clamp01(value)); AudioListener.volume = Mathf.Clamp01(value); }
    }

    // Multiplier on the default mouse / stick look speed.
    public static float LookScale
    {
        get => Mathf.Clamp(PlayerPrefs.GetFloat(LookKey, 1f), MinLookScale, MaxLookScale);
        set => PlayerPrefs.SetFloat(LookKey, Mathf.Clamp(value, MinLookScale, MaxLookScale));
    }

    public static bool InvertY
    {
        get => PlayerPrefs.GetInt(InvertKey, 0) == 1;
        set => PlayerPrefs.SetInt(InvertKey, value ? 1 : 0);
    }

    public static bool CanChangeFullscreen =>
        Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.OSXPlayer ||
        Application.platform == RuntimePlatform.LinuxPlayer;

    public static bool Fullscreen
    {
        get => PlayerPrefs.GetInt(FullKey, Screen.fullScreen ? 1 : 0) == 1;
        set
        {
            PlayerPrefs.SetInt(FullKey, value ? 1 : 0);
            if (CanChangeFullscreen) Screen.fullScreenMode = value ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        }
    }

    public static void Apply()
    {
        AudioListener.volume = Volume;
        if (CanChangeFullscreen && PlayerPrefs.HasKey(FullKey))
            Screen.fullScreenMode = Fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
    }

    public static void Save() => PlayerPrefs.Save();
}
