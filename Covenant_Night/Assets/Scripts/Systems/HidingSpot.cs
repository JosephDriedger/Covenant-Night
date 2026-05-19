using UnityEngine;

// Place on a trigger collider (alcove, shadow, hay pile, etc.).
// While Jonathan is inside and the alarm is active, the hide timer pauses.
public class HidingSpot : MonoBehaviour
{
    bool _jonathanInside;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        _jonathanInside = true;
        // Pause the alarm countdown while inside
        Time.timeScale = 0f;
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        _jonathanInside = false;
        Time.timeScale = 1f;
    }

    // If the alarm clears while hiding, resume time
    void OnEnable()  => AlarmSystem.OnAlarmCleared += OnAlarmCleared;
    void OnDisable() => AlarmSystem.OnAlarmCleared -= OnAlarmCleared;

    void OnAlarmCleared()
    {
        if (_jonathanInside) Time.timeScale = 1f;
    }
}
