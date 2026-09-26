using System.Collections.Generic;
using UnityEngine;

// Place on a trigger collider (alcove, shadow, hay pile, etc.).
// Jonathan / David inside are hidden: guards only find them at very close range.
// While Jonathan is inside during an alarm the hide countdown is held and, after a few seconds,
// the alarm stands down (AlarmSystem). Replaces the old Time.timeScale = 0 hack.
public class HidingSpot : MonoBehaviour
{
    public static readonly List<HidingSpot> All = new List<HidingSpot>();

    public Vector3 Center { get { var c = GetComponent<Collider>(); return c != null ? c.bounds.center : transform.position; } }

    void OnEnable()  => All.Add(this);
    void OnDisable() => All.Remove(this);

    [Tooltip("Optional glow that pulses while an alarm is active, pointing the player to safety.")]
    public Light markerLight;
    public float idleIntensity  = 0.4f;
    public float alarmIntensity = 3.0f;

    int _jonathanInside;
    int _davidInside;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _jonathanInside++;
            other.GetComponent<PlayerController>()?.SetHidden(true);
            AlarmSystem.Instance?.SetPlayerHidden(true);
        }
        else if (other.CompareTag(GameLayers.DavidTag))
        {
            _davidInside++;
            other.GetComponent<DavidCompanion>()?.SetHidden(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _jonathanInside = Mathf.Max(0, _jonathanInside - 1);
            if (_jonathanInside == 0)
            {
                other.GetComponent<PlayerController>()?.SetHidden(false);
                AlarmSystem.Instance?.SetPlayerHidden(false);
            }
        }
        else if (other.CompareTag(GameLayers.DavidTag))
        {
            _davidInside = Mathf.Max(0, _davidInside - 1);
            if (_davidInside == 0) other.GetComponent<DavidCompanion>()?.SetHidden(false);
        }
    }

    // Zone unloaded while someone was inside: don't leave them permanently hidden.
    void OnDestroy()
    {
        if (_jonathanInside > 0)
        {
            PlayerController.Instance?.SetHidden(false);
            AlarmSystem.Instance?.SetPlayerHidden(false);
        }
        if (_davidInside > 0) DavidCompanion.Instance?.SetHidden(false);
    }

    void Update()
    {
        if (markerLight == null) return;
        bool alarm = AlarmSystem.Instance != null && AlarmSystem.Instance.IsAlarmed;
        float target = alarm ? alarmIntensity * (0.6f + 0.4f * Mathf.Sin(Time.time * 6f)) : idleIntensity;
        markerLight.intensity = Mathf.Lerp(markerLight.intensity, target, 8f * Time.deltaTime);
    }
}
