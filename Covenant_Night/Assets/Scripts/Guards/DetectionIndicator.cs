using UnityEngine;

// Controls the ground-projected cone decal colour to show guard detection state.
// Attach to the guard. Assign a Light or DecalProjector reference; update its colour here.
// Works with any colour-settable component — default implementation uses a Light.
public class DetectionIndicator : MonoBehaviour
{
    [Header("Indicator Light (or assign a Decal Projector via a custom subclass)")]
    public Light indicatorLight;

    [Header("State Colours")]
    public Color unawareColor    = new Color(0.8f, 0.8f, 0.2f, 0.3f);  // pale yellow
    public Color suspiciousColor = new Color(1.0f, 0.5f, 0.0f, 0.6f);  // amber
    public Color alarmedColor    = new Color(1.0f, 0.1f, 0.1f, 1.0f);  // red

    public void SetState(GuardState state)
    {
        if (indicatorLight == null) return;
        indicatorLight.color = state switch
        {
            GuardState.Suspicious => suspiciousColor,
            GuardState.Alarmed    => alarmedColor,
            _                     => unawareColor,
        };
    }
}
