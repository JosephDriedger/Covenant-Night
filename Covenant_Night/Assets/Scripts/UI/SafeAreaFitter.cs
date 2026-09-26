using UnityEngine;

// Fits a UI container to the display's safe area (notches, TV overscan). On console platforms a minimum
// margin is always kept so HUD text is never clipped by a television's overscan.
[RequireComponent(typeof(RectTransform))]
public class SafeAreaFitter : MonoBehaviour
{
    public float consoleMargin = 0.035f;

    RectTransform _rt;
    Rect _last;
    Vector2Int _res;

    void Awake() => _rt = GetComponent<RectTransform>();

    void Update()
    {
        if (Screen.safeArea != _last || _res.x != Screen.width || _res.y != Screen.height) Apply();
    }

    void Apply()
    {
        _last = Screen.safeArea;
        _res = new Vector2Int(Screen.width, Screen.height);
        if (Screen.width <= 0 || Screen.height <= 0) return;

        Vector2 min = _last.position, max = _last.position + _last.size;
        min.x /= Screen.width; min.y /= Screen.height;
        max.x /= Screen.width; max.y /= Screen.height;

        if (Application.isConsolePlatform)
        {
            min = Vector2.Max(min, new Vector2(consoleMargin, consoleMargin));
            max = Vector2.Min(max, new Vector2(1f - consoleMargin, 1f - consoleMargin));
        }
        _rt.anchorMin = min;
        _rt.anchorMax = max;
        _rt.offsetMin = _rt.offsetMax = Vector2.zero;
    }
}
