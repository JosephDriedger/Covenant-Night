using UnityEngine;

// Hides a building's roof mesh while Jonathan is inside its trigger volume so the camera can see in.
// The roof collider stays on the Roof layer, so it still blocks guards' sight from rooftop sentries —
// interiors are cover from above.
public class RoofFader : MonoBehaviour
{
    public Renderer[] roofRenderers;

    void OnTriggerEnter(Collider other) { if (other.CompareTag("Player")) Set(false); }
    void OnTriggerExit(Collider other)  { if (other.CompareTag("Player")) Set(true); }

    void Set(bool visible)
    {
        if (roofRenderers == null) return;
        foreach (var r in roofRenderers) if (r != null) r.enabled = visible;
    }
}
