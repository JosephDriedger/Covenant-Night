using UnityEngine;

// Local personal-best time per zone. Offline, single-player only — no leaderboard server involved.
// Follows the same static-class + PlayerPrefs pattern as GameDifficulty / GameSettings.
public static class ZoneTimes
{
    const string KeyPrefix = "cn.besttime.";

    public static bool HasBest(string zoneKey) => PlayerPrefs.HasKey(KeyPrefix + zoneKey);

    public static float GetBest(string zoneKey) => PlayerPrefs.GetFloat(KeyPrefix + zoneKey, float.PositiveInfinity);

    // Returns true if this run set a new best for the zone.
    public static bool SetIfBest(string zoneKey, float seconds)
    {
        if (seconds >= GetBest(zoneKey)) return false;
        PlayerPrefs.SetFloat(KeyPrefix + zoneKey, seconds);
        PlayerPrefs.Save();
        return true;
    }

    public static string Format(float seconds)
    {
        if (float.IsInfinity(seconds)) return "--:--";
        int m = Mathf.FloorToInt(seconds / 60f);
        int s = Mathf.FloorToInt(seconds % 60f);
        return $"{m}:{s:00}";
    }
}
