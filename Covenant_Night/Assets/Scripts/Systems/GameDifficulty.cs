using UnityEngine;

public enum DifficultyLevel { Easy, Medium, Hard, Hardcore }

// Multipliers applied to guards and the alarm. Medium is the baseline (all 1).
public struct DifficultyTuning
{
    public float vision;          // guard sight range
    public float cone;            // guard cone angle
    public float gain;            // how fast awareness fills
    public float decay;           // how fast awareness drains
    public float speed;           // guard movement and sentry sweep speed
    public float hearing;         // how far guards hear sounds
    public float investigate;     // how long guards investigate before giving up
    public float memory;          // how long an alarmed guard keeps chasing the last sighting
    public float hideWindow;      // seconds to reach cover once the alarm is raised
    public float standDown;       // how long you must stay hidden before the alarm stands down
    public float catchDistance;
    public int   stoneDelta;      // added to the stones per zone
    public bool  smart;           // guards search nearby hiding spots
}

// Regular play (Easy, Medium, Hard): a capture restarts the current zone.
// Hardcore: one life, so any capture restarts the run from the first zone, and guards are faster and smarter.
public static class GameDifficulty
{
    const string Key = "cn.difficulty";

    static DifficultyLevel? _cached;
    static DifficultyLevel? _session;   // set by automated tests so they never touch saved preferences

    public static DifficultyLevel Level
    {
        get
        {
            if (_session.HasValue) return _session.Value;
            if (!_cached.HasValue) _cached = (DifficultyLevel)Mathf.Clamp(PlayerPrefs.GetInt(Key, (int)DifficultyLevel.Medium), 0, 3);
            return _cached.Value;
        }
        set
        {
            _cached = value;
            PlayerPrefs.SetInt(Key, (int)value);
            PlayerPrefs.Save();
        }
    }

    public static void SetForSession(DifficultyLevel level) => _session = level;

    public static bool Hardcore => Level == DifficultyLevel.Hardcore;

    public static string Label => Level == DifficultyLevel.Hardcore ? "Hardcore" : Level.ToString();

    // How sharp the current zone's guards are (set by ZoneManager from the zone's entry; 1 = baseline).
    // The zones ramp up from the first to the last, and this stacks with the chosen difficulty.
    public static float ZoneScale = 1f;

    public static DifficultyTuning Tuning
    {
        get
        {
            var t = ModeTuning();
            float z = ZoneScale;
            t.vision *= z;
            t.cone   *= Mathf.Sqrt(z);
            t.gain   *= z;
            t.speed  *= Mathf.Sqrt(z);
            t.hearing *= z;
            return t;
        }
    }

    static DifficultyTuning ModeTuning()
    {
        switch (Level)
        {
            case DifficultyLevel.Easy:
                return new DifficultyTuning
                {
                    vision = 0.8f, cone = 0.85f, gain = 0.7f, decay = 1.4f, speed = 0.9f, hearing = 0.8f, investigate = 0.7f,
                    memory = 0.6f, hideWindow = 1.4f, standDown = 0.7f, catchDistance = 0.9f, stoneDelta = 2, smart = false,
                };
            case DifficultyLevel.Hard:
                return new DifficultyTuning
                {
                    vision = 1.04f, cone = 1.04f, gain = 1.15f, decay = 0.85f, speed = 1.05f, hearing = 1.1f, investigate = 1.3f,
                    memory = 1.5f, hideWindow = 0.8f, standDown = 1.3f, catchDistance = 1.05f, stoneDelta = -1, smart = false,
                };
            case DifficultyLevel.Hardcore:
                return new DifficultyTuning
                {
                    vision = 1.05f, cone = 1.03f, gain = 1.2f, decay = 0.7f, speed = 1.05f, hearing = 1.25f, investigate = 1.6f,
                    memory = 2f, hideWindow = 0.65f, standDown = 2f, catchDistance = 1.15f, stoneDelta = -1, smart = true,
                };
            default:
                return new DifficultyTuning
                {
                    vision = 1f, cone = 1f, gain = 1f, decay = 1f, speed = 1f, hearing = 1f, investigate = 1f,
                    memory = 1f, hideWindow = 1f, standDown = 1f, catchDistance = 1f, stoneDelta = 0, smart = false,
                };
        }
    }
}
