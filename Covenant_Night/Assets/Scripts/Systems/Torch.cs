using System.Collections.Generic;
using UnityEngine;

// A world torch: warm flickering point light + a registry used by the stealth model.
// "Dynamic lighting as a stealth mechanic": guards see targets standing in torchlight from farther
// away and notice them faster than targets in shadow (see GuardVision).
public class Torch : MonoBehaviour
{
    static readonly List<Torch> All = new List<Torch>();

    public Light  torchLight;
    public Transform flame;                 // optional flame mesh that flickers with the light
    [Tooltip("Radius (metres) of the pool of light for stealth purposes.")]
    public float  lightRadius = 8f;
    [Range(0f, 0.6f)] public float flicker = 0.18f;
    public float  flickerSpeed = 5f;

    float _baseIntensity;
    Vector3 _baseFlameScale;
    float _seed;

    void OnEnable()  => All.Add(this);
    void OnDisable() => All.Remove(this);

    void Start()
    {
        _seed = Random.value * 100f;
        if (torchLight != null) _baseIntensity = torchLight.intensity;
        if (flame != null) _baseFlameScale = flame.localScale;
    }

    void Update()
    {
        float n = Mathf.PerlinNoise(_seed, Time.time * flickerSpeed) - 0.5f;
        if (torchLight != null) torchLight.intensity = _baseIntensity * (1f + n * 2f * flicker);
        if (flame != null) flame.localScale = _baseFlameScale * (1f + n * 0.5f);
    }

    public const float AmbientVisibility = 0.35f;   // moonlight only

    // 0.35 (deep shadow) .. 1.0 (standing in a torch's pool of light)
    public static float VisibilityAt(Vector3 position)
    {
        float v = AmbientVisibility;
        for (int i = 0; i < All.Count; i++)
        {
            var t = All[i];
            float d = Vector3.Distance(position, t.transform.position);
            if (d < t.lightRadius)
                v += (1f - d / t.lightRadius) * 0.9f;
        }
        return Mathf.Clamp01(v);
    }
}
