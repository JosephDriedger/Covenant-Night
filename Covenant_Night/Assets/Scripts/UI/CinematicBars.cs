using UnityEngine;

// Letterbox bars that slide in while the camera is on a cutscene shot and out again for play.
public class CinematicBars : MonoBehaviour
{
    public RectTransform top;
    public RectTransform bottom;
    public float height = 110f;

    float _t;

    void LateUpdate()
    {
        var cam = ThirdPersonCamera.Instance;
        bool on = cam != null && cam.InShot;
        _t = Mathf.MoveTowards(_t, on ? 1f : 0f, Time.unscaledDeltaTime * 2.2f);
        float h = height * Mathf.SmoothStep(0f, 1f, _t);
        if (top != null) top.sizeDelta = new Vector2(0f, h);
        if (bottom != null) bottom.sizeDelta = new Vector2(0f, h);
    }
}
