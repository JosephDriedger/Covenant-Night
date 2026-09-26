using UnityEngine;

// Gentle cloth sway (banners, awnings): rotates around `axis` by a slow noise-driven angle.
public class Sway : MonoBehaviour
{
    public float amplitude = 3f;
    public float speed = 1f;
    public Vector3 axis = Vector3.right;

    Quaternion _base;
    float _seed;

    void Start()
    {
        _base = transform.localRotation;
        _seed = Random.value * 50f;
    }

    void Update()
    {
        float a = (Mathf.PerlinNoise(_seed, Time.time * speed * 0.5f) - 0.5f) * 2f * amplitude
                + Mathf.Sin(Time.time * speed * 1.7f + _seed) * amplitude * 0.25f;
        transform.localRotation = _base * Quaternion.AngleAxis(a, axis);
    }
}
