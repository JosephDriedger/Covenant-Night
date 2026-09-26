using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// URP post-processing swap on alarm: vignette pulses red and the image desaturates (GDD stretch goal
// "vignette pulse + desaturation on alarm"). The scope cut allows only Bloom + Vignette + colour
// adjustment (colour-grade unification), all on one global Volume in the Persistent scene.
[RequireComponent(typeof(Volume))]
public class PostFXController : MonoBehaviour
{
    public float calmVignette   = 0.32f;
    public float alarmVignette  = 0.48f;
    public float pulseSpeed     = 5f;
    public float pulseAmount    = 0.1f;
    public float calmSaturation = 0f;       // colour adjustments "saturation" offset
    public float alarmSaturation = -35f;

    Volume _volume;
    Vignette _vignette;
    ColorAdjustments _color;
    float _blend;   // 0 calm .. 1 alarmed

    void Awake()
    {
        _volume = GetComponent<Volume>();
        var profile = _volume.profile;    // instance copy (not the asset)
        profile.TryGet(out _vignette);
        profile.TryGet(out _color);
    }

    void Update()
    {
        bool alarmed = AlarmSystem.Instance != null && AlarmSystem.Instance.IsAlarmed;
        _blend = Mathf.MoveTowards(_blend, alarmed ? 1f : 0f, Time.unscaledDeltaTime * 2.5f);

        if (_vignette != null)
        {
            float pulse = alarmed ? Mathf.Sin(Time.time * pulseSpeed) * pulseAmount : 0f;
            _vignette.intensity.value = Mathf.Lerp(calmVignette, alarmVignette, _blend) + pulse * _blend;
            _vignette.color.value = Color.Lerp(Color.black, new Color(0.35f, 0f, 0f), _blend);
        }
        if (_color != null)
            _color.saturation.value = Mathf.Lerp(calmSaturation, alarmSaturation, _blend);
    }
}
