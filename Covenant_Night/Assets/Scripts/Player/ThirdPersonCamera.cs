using UnityEngine;

// Over-the-shoulder third-person camera with wall collision and a cutscene "shot" override.
// The GDD specifies a Cinemachine third-person rig (with a fixed camera fallback); Cinemachine is not
// in this project's package manifest, so this rig provides the same behaviour with no dependency:
//   - mouse / right-stick orbit around Jonathan
//   - SphereCast collision against the Walls layer (no wall clipping)
//   - FOV narrows slightly while crouching to raise tension
//   - a "shot" mode used by the story / gate cutscenes (equivalent to a second virtual camera)
public class ThirdPersonCamera : MonoBehaviour
{
    public static ThirdPersonCamera Instance { get; private set; }

    [Header("Target")]
    public Transform target;                 // Jonathan's CameraTarget

    [Header("Orbit")]
    public float distance      = 4.2f;
    public Vector3 shoulderOffset = new Vector3(0.55f, 0.15f, 0f);
    public float minPitch      = -25f;
    public float maxPitch      = 60f;
    public float startPitch    = 22f;
    public float followSharpness = 14f;

    [Header("Collision")]
    public float collisionRadius = 0.25f;
    public LayerMask collisionMask;

    [Header("FOV")]
    public float standingFov = 62f;
    public float crouchFov   = 54f;
    public float fovSharpness = 5f;

    float  _yaw, _pitch;
    float  _currentDistance;
    Camera _cam;
    Vector3 _smoothedPivot;

    // Cutscene shot
    Transform _shotPoint, _shotLookAt;
    float _shotBlend;   // 0 = gameplay, 1 = shot

    void Awake()
    {
        Instance = this;
        _cam = GetComponent<Camera>();
        _pitch = startPitch;
        _currentDistance = distance;
        if (collisionMask.value == 0) collisionMask = GameLayers.CameraBlockers;
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
        if (target != null) _smoothedPivot = Pivot();
    }

    Vector3 Pivot() => target.position;

    void LateUpdate()
    {
        if (target == null) return;

        bool paused = GameManager.Instance != null && GameManager.Instance.IsPaused;

        if (!paused && InputReader.Instance != null)
        {
            Vector2 look = InputReader.Instance.LookDelta;
            _yaw   += look.x;
            _pitch  = Mathf.Clamp(_pitch - look.y, minPitch, maxPitch);
        }

        // Smoothly follow the target (also smooths crouch height changes)
        _smoothedPivot = Vector3.Lerp(_smoothedPivot, Pivot(), 1f - Mathf.Exp(-followSharpness * Time.unscaledDeltaTime));

        Quaternion rot = Quaternion.Euler(_pitch, _yaw, 0f);
        Vector3 pivot  = _smoothedPivot + rot * new Vector3(shoulderOffset.x, shoulderOffset.y, 0f);

        // Collision: pull the camera in when a wall is between it and Jonathan
        Vector3 back = rot * Vector3.back;
        float desired = distance;
        if (Physics.SphereCast(pivot, collisionRadius, back, out RaycastHit hit, distance, collisionMask, QueryTriggerInteraction.Ignore))
            desired = Mathf.Max(0.4f, hit.distance);
        _currentDistance = desired < _currentDistance
            ? desired
            : Mathf.Lerp(_currentDistance, desired, 1f - Mathf.Exp(-6f * Time.unscaledDeltaTime));

        Vector3 gameplayPos = pivot + back * _currentDistance;
        Quaternion gameplayRot = rot;

        // Cutscene shot blending
        float shotTarget = _shotPoint != null ? 1f : 0f;
        _shotBlend = Mathf.MoveTowards(_shotBlend, shotTarget, Time.unscaledDeltaTime * 1.2f);

        if (_shotBlend > 0.001f && _shotPoint != null)
        {
            Quaternion shotRot = _shotLookAt != null
                ? Quaternion.LookRotation((_shotLookAt.position - _shotPoint.position).normalized)
                : _shotPoint.rotation;
            float t = Mathf.SmoothStep(0f, 1f, _shotBlend);
            transform.SetPositionAndRotation(
                Vector3.Lerp(gameplayPos, _shotPoint.position, t),
                Quaternion.Slerp(gameplayRot, shotRot, t));
        }
        else transform.SetPositionAndRotation(gameplayPos, gameplayRot);

        // FOV
        var pc = PlayerController.Instance;
        float fovTarget = pc != null && pc.IsCrouching ? crouchFov : standingFov;
        if (_cam != null)
            _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, fovTarget, 1f - Mathf.Exp(-fovSharpness * Time.unscaledDeltaTime));
    }

    // Instantly re-orient behind a heading (used when a zone loads).
    public void SnapBehind(float yawDegrees)
    {
        _yaw = yawDegrees;
        _pitch = startPitch;
        _shotPoint = null;
        _shotBlend = 0f;
        if (target != null) _smoothedPivot = Pivot();
        _currentDistance = distance;
        LateUpdate();
    }

    public void SetShot(Transform shotPoint, Transform lookAt)
    {
        _shotPoint = shotPoint;
        _shotLookAt = lookAt;
    }

    public void ClearShot() => _shotPoint = null;

    public float Yaw => _yaw;
}
