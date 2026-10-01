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
    public float shotFov     = 50f;    // cutscene lens: a little longer than play for tighter framing
    public float fovSharpness = 5f;

    float  _yaw, _pitch;
    float  _currentDistance;
    Camera _cam;
    Vector3 _smoothedPivot;

    // Cutscene shot
    Transform _shotPoint, _shotLookAt;
    float _shotLookHeight;
    float _shotBlend;   // 0 = gameplay, 1 = shot
    Vector3 _lastShotPos;
    Quaternion _lastShotRot = Quaternion.identity;

    public bool InShot => _shotPoint != null || _shotBlend > 0.001f;

    [Header("Cutscene Key Light")]
    public float keyLightIntensity = 1.8f;
    Light _keyLight;

    void Awake()
    {
        Instance = this;
        _cam = GetComponent<Camera>();
        _gameplayFov = standingFov;

        // A soft fill that rides with the camera during cutscene shots so faces read at night; off in play.
        var kl = new GameObject("CutsceneKeyLight");
        kl.transform.SetParent(transform, false);
        kl.transform.localPosition = new Vector3(0.7f, 0.6f, 0f);
        _keyLight = kl.AddComponent<Light>();
        _keyLight.type = LightType.Point;
        _keyLight.color = new Color(1f, 0.9f, 0.78f);
        _keyLight.range = 11f;
        _keyLight.shadows = LightShadows.None;
        _keyLight.intensity = 0f;
        _keyLight.enabled = false;
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

        if (_shotPoint != null)
        {
            _lastShotPos = _shotPoint.position;
            _lastShotRot = _shotLookAt != null
                ? Quaternion.LookRotation((_shotLookAt.position + Vector3.up * _shotLookHeight - _shotPoint.position).normalized)
                : _shotPoint.rotation;
        }

        // Blending in and out both run between the gameplay pose and the last shot pose, so clearing a
        // shot glides back to play instead of snapping. The slight lift keeps the path over the actors' heads.
        if (_shotBlend > 0.001f)
        {
            float t = Mathf.SmoothStep(0f, 1f, _shotBlend);
            Vector3 pos = Vector3.Lerp(gameplayPos, _lastShotPos, t) + Vector3.up * (Mathf.Sin(t * Mathf.PI) * 0.8f);
            transform.SetPositionAndRotation(pos, Quaternion.Slerp(gameplayRot, _lastShotRot, t));
        }
        else transform.SetPositionAndRotation(gameplayPos, gameplayRot);

        float key = Mathf.SmoothStep(0f, 1f, _shotBlend) * keyLightIntensity;
        _keyLight.enabled = key > 0.01f;
        _keyLight.intensity = key;

        // FOV
        var pc = PlayerController.Instance;
        float fovTarget = pc != null && pc.IsCrouching ? crouchFov : standingFov;
        _gameplayFov = Mathf.Lerp(_gameplayFov, fovTarget, 1f - Mathf.Exp(-fovSharpness * Time.unscaledDeltaTime));
        if (_cam != null)
            _cam.fieldOfView = Mathf.Lerp(_gameplayFov, shotFov, Mathf.SmoothStep(0f, 1f, _shotBlend));
    }

    float _gameplayFov = 62f;

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

    // lookHeight raises the aim point above lookAt (characters' pivots are at their feet). cut skips the
    // glide from the gameplay camera, for a hard cut straight onto the shot.
    public void SetShot(Transform shotPoint, Transform lookAt, float lookHeight = 0f, bool cut = false)
    {
        _shotPoint = shotPoint;
        _shotLookAt = lookAt;
        _shotLookHeight = lookHeight;
        if (cut && shotPoint != null)
        {
            _shotBlend = 1f;
            LateUpdate();
        }
    }

    public void ClearShot() => _shotPoint = null;

    public float Yaw => _yaw;
}
