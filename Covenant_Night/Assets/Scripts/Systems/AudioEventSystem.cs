using System;
using UnityEngine;

// Static broadcast bus for sound events.
// Anything that makes noise calls Emit(); guards listen via OnSoundEmitted.
public static class AudioEventSystem
{
    public static event Action<Vector3, float> OnSoundEmitted;

    public static void Emit(Vector3 worldPosition, float radius)
    {
        if (radius <= 0f) return;
        OnSoundEmitted?.Invoke(worldPosition, radius);
    }
}
