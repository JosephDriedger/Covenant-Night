using UnityEngine;
using TMPro;

// HUD marker for the current zone's exit: sits over the exit when it is on screen, and pins to the screen edge
// in its direction when it is not, with the distance from Jonathan. Fades out on arrival.
public class ExitIndicator : MonoBehaviour
{
    public RectTransform marker;       // child of the HUD canvas
    public TextMeshProUGUI label;
    public CanvasGroup group;
    public float heightAboveExit = 3.2f;
    public float edgeMargin = 70f;

    RectTransform _canvasRect;
    Canvas _canvas;

    void Awake()
    {
        _canvasRect = transform as RectTransform;
        _canvas = GetComponent<Canvas>();
    }

    void LateUpdate()
    {
        var zm = ZoneManager.Instance;
        var cam = Camera.main;
        var pc = PlayerController.Instance;
        Transform exit = zm != null && !zm.IsTransitioning ? zm.CurrentExitPoint : null;
        if (exit == null || cam == null || pc == null || marker == null)
        {
            if (group != null) group.alpha = 0f;
            return;
        }

        float W = cam.pixelWidth, H = cam.pixelHeight;
        Vector3 world = exit.position + Vector3.up * heightAboveExit;
        Vector3 sp = cam.WorldToScreenPoint(world);
        bool behind = sp.z < 0f;
        if (behind) sp = new Vector3(W - sp.x, H - sp.y, 0f);   // mirror so the edge pin points the right way

        float m = edgeMargin * (H / 1080f);
        bool offscreen = behind || sp.x < m || sp.x > W - m || sp.y < m || sp.y > H - m;
        if (offscreen)
        {
            Vector2 c = new Vector2(W, H) * 0.5f;
            Vector2 d = (Vector2)sp - c;
            if (behind && d.y > 0f) d.y = -d.y;            // something behind you reads as "below"
            if (d.sqrMagnitude < 0.01f) d = Vector2.down;
            float sx = (c.x - m) / Mathf.Max(0.001f, Mathf.Abs(d.x));
            float sy = (c.y - m) / Mathf.Max(0.001f, Mathf.Abs(d.y));
            sp = c + d * Mathf.Min(sx, sy);
        }

        Camera uiCam = _canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay ? _canvas.worldCamera : null;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, sp, uiCam, out Vector2 local);
        marker.anchoredPosition = local;

        float dist = Vector3.Distance(new Vector3(pc.transform.position.x, 0f, pc.transform.position.z),
                                      new Vector3(exit.position.x, 0f, exit.position.z));
        if (label != null) label.text = $"Exit  {Mathf.RoundToInt(dist)} m";
        if (group != null) group.alpha = Mathf.Clamp01((dist - 3f) / 5f);
    }
}
