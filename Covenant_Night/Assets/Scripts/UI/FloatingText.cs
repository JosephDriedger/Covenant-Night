using UnityEngine;
using TMPro;

// Brief floating world-space text (GDD: "brief floating world-space text for David command
// confirmations"). Rises, fades, and billboards toward the camera.
public class FloatingText : MonoBehaviour
{
    TextMeshPro _tmp;
    float _t;
    float _life = 1.6f;
    Vector3 _start;
    Camera _cam;

    static TMP_FontAsset _font;
    static bool _fontLoaded;

    public static FloatingText Spawn(Vector3 worldPosition, string message, Color color, float size = 3f, float life = 1.6f)
    {
        if (!_fontLoaded) { _font = Resources.Load<TMP_FontAsset>("Fonts & Materials/Cardo SDF"); _fontLoaded = true; }

        var go = new GameObject("FloatingText");
        go.transform.position = worldPosition;
        var ft = go.AddComponent<FloatingText>();
        ft._tmp = go.AddComponent<TextMeshPro>();
        if (_font != null) ft._tmp.font = _font;
        ft._tmp.text = message;
        ft._tmp.fontSize = size;
        ft._tmp.color = color;
        ft._tmp.alignment = TextAlignmentOptions.Center;
        ft._tmp.textWrappingMode = TextWrappingModes.NoWrap;
        ft._tmp.outlineWidth = 0.2f;
        ft._tmp.outlineColor = new Color32(0, 0, 0, 255);
        ft._life = life;
        ft._start = worldPosition;
        return ft;
    }

    void Update()
    {
        _t += Time.unscaledDeltaTime;
        float k = _t / _life;
        if (k >= 1f) { Destroy(gameObject); return; }

        transform.position = _start + Vector3.up * (k * 0.9f);
        var c = _tmp.color;
        c.a = 1f - Mathf.SmoothStep(0.5f, 1f, k);
        _tmp.color = c;

        if (_cam == null) _cam = Camera.main;
        if (_cam != null) transform.rotation = _cam.transform.rotation;
    }
}
