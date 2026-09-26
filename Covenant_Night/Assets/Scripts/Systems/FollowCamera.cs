using UnityEngine;

// Keeps an object centred on the main camera (sky dome: moon and stars stay infinitely far away).
public class FollowCamera : MonoBehaviour
{
    Camera _cam;

    void LateUpdate()
    {
        if (_cam == null) _cam = Camera.main;
        if (_cam != null) transform.position = _cam.transform.position;
    }
}
