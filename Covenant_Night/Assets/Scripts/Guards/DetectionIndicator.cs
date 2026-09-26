using UnityEngine;
using TMPro;

// Shows guard detection state: the ground cone (green → amber → red), the guard's lantern light and a
// floating "?" / "!" icon that fills in with awareness (Mark of the Ninja-style readability).
// Attach to the guard.
public class DetectionIndicator : MonoBehaviour
{
    [Header("Lantern Light")]
    public Light indicatorLight;

    [Header("Cone + Icon")]
    public GuardConeVisual coneVisual;
    public TextMeshPro     icon;

    [Header("Cone Colours (GDD: unaware = green, suspicious = amber, alarmed = red)")]
    public Color unawareColor    = new Color(0.45f, 0.85f, 0.35f, 0.22f);
    public Color suspiciousColor = new Color(1.00f, 0.72f, 0.10f, 0.36f);
    public Color alarmedColor    = new Color(1.00f, 0.10f, 0.10f, 0.46f);

    [Header("Lantern Colours")]
    public Color lanternUnaware    = new Color(1.0f, 0.78f, 0.45f);
    public Color lanternSuspicious = new Color(1.0f, 0.55f, 0.10f);
    public Color lanternAlarmed    = new Color(1.0f, 0.15f, 0.10f);

    GuardFSM _fsm;
    GuardState _state;
    Camera _cam;

    void Awake() => _fsm = GetComponent<GuardFSM>();

    public void SetState(GuardState state)
    {
        _state = state;
        if (indicatorLight != null)
            indicatorLight.color = state switch
            {
                GuardState.Suspicious => lanternSuspicious,
                GuardState.Alarmed    => lanternAlarmed,
                _                     => lanternUnaware,
            };
    }

    void Update()
    {
        float awareness = _fsm != null ? _fsm.Awareness : 0f;
        float thr = _fsm != null && _fsm.Vision != null ? _fsm.Vision.suspicionThreshold : 0.35f;

        if (coneVisual != null)
        {
            Color c = _state switch
            {
                GuardState.Alarmed    => alarmedColor,
                GuardState.Suspicious => Color.Lerp(suspiciousColor, alarmedColor, Mathf.InverseLerp(thr, 1f, awareness)),
                _                     => Color.Lerp(unawareColor, suspiciousColor, Mathf.Clamp01(awareness / thr)),
            };
            coneVisual.SetColor(c);
        }

        if (icon != null)
        {
            string glyph = "";
            Color col = Color.white;
            float alpha = 0f;
            switch (_state)
            {
                case GuardState.Alarmed:    glyph = "!"; col = new Color(1f, 0.15f, 0.1f); alpha = 1f; break;
                case GuardState.Suspicious: glyph = "?"; col = new Color(1f, 0.75f, 0.15f); alpha = 1f; break;
                default:
                    if (awareness > 0.1f) { glyph = "?"; col = new Color(0.95f, 0.9f, 0.4f); alpha = Mathf.Clamp01(awareness / thr); }
                    break;
            }
            icon.text = glyph;
            col.a = alpha;
            icon.color = col;

            if (_cam == null) _cam = Camera.main;
            if (_cam != null && alpha > 0f)
                icon.transform.rotation = _cam.transform.rotation;
        }
    }
}
