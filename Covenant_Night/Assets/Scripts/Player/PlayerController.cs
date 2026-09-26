using UnityEngine;

// Handles Jonathan's movement, crouch, sprint, gravity, and noise footprint.
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    [Header("Movement Speeds")]
    public float walkSpeed   = 4f;
    public float crouchSpeed = 1.8f;
    public float sprintSpeed = 7f;
    public float acceleration = 35f;

    [Header("Crouch")]
    public float standingHeight = 1.8f;
    public float crouchHeight   = 1.0f;
    public Transform cameraTarget; // third-person camera follows / looks at this

    [Header("Gravity")]
    public float gravity = -20f;

    [Header("Noise Radii")]
    public float walkNoiseRadius   = 4f;
    public float sprintNoiseRadius = 11f;
    public float crouchNoiseRadius = 0f;

    [Header("Footstep Audio")]
    public AudioSource footstepSource;  // fallback if FootstepAudio component absent
    public float footstepInterval = 0.45f;

    // Read by GuardVision
    public bool IsWallPressed { get; private set; }
    public bool IsCrouching   => _isCrouching;
    public bool IsSprinting   { get; private set; }
    public bool IsHidden      { get; private set; }
    public Vector3 Velocity   => _moveVelocity;                    // horizontal velocity
    public bool IsStationary  => _moveVelocity.sqrMagnitude < 0.01f;

    CharacterController _cc;
    FootstepAudio       _footstepAudio;
    Camera              _cam;
    Vector3 _moveVelocity;
    float   _vy;
    float   _footstepTimer;
    bool    _isCrouching;
    Vector3 _wallFacing;

    void Awake()
    {
        Instance       = this;
        _cc            = GetComponent<CharacterController>();
        _footstepAudio = GetComponent<FootstepAudio>();
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsPaused)
        {
            _moveVelocity = Vector3.zero;
            return;
        }

        HandleCrouch();
        HandleMovement();
        HandleFootstepNoise();
    }

    void HandleCrouch()
    {
        bool wantCrouch = InputReader.Instance.CrouchHeld;
        if (!wantCrouch && _isCrouching && !CanStand()) wantCrouch = true;   // low ceiling
        if (wantCrouch == _isCrouching) return;

        _isCrouching = wantCrouch;
        _cc.height   = _isCrouching ? crouchHeight : standingHeight;
        _cc.center   = Vector3.up * (_cc.height * 0.5f);

        if (cameraTarget != null)
            cameraTarget.localPosition = Vector3.up * (_cc.height * 0.85f);
    }

    bool CanStand()
    {
        float r = _cc.radius * 0.9f;
        Vector3 bottom = transform.position + Vector3.up * (crouchHeight - r + 0.05f);
        Vector3 top    = transform.position + Vector3.up * (standingHeight - r);
        return !Physics.CheckCapsule(bottom, top, r, GameLayers.StaticSight, QueryTriggerInteraction.Ignore);
    }

    void HandleMovement()
    {
        if (_cam == null) _cam = Camera.main;

        Vector2 input = IsWallPressed ? Vector2.zero : InputReader.Instance.Move;
        bool hasInput = input.sqrMagnitude > 0.01f;
        IsSprinting = !_isCrouching && hasInput && InputReader.Instance.SprintHeld;

        float speed = _isCrouching ? crouchSpeed : IsSprinting ? sprintSpeed : walkSpeed;

        Vector3 dir = Vector3.zero;
        if (hasInput)
        {
            Transform cam = _cam != null ? _cam.transform : transform;
            Vector3 fwd   = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
            Vector3 right = Vector3.ProjectOnPlane(cam.right,   Vector3.up).normalized;
            dir = fwd * input.y + right * input.x;
            if (dir.sqrMagnitude > 1f) dir.Normalize();
        }

        _moveVelocity = Vector3.MoveTowards(_moveVelocity, dir * speed, acceleration * Time.deltaTime);

        if (_cc.isGrounded && _vy < 0f) _vy = -2f;
        _vy += gravity * Time.deltaTime;

        _cc.Move((_moveVelocity + Vector3.up * _vy) * Time.deltaTime);

        // Facing: back to the wall while pressed, otherwise along the movement direction
        Vector3 face = IsWallPressed ? _wallFacing :
                       _moveVelocity.sqrMagnitude > 0.05f ? _moveVelocity : Vector3.zero;
        if (face != Vector3.zero)
            transform.rotation = Quaternion.Slerp(
                transform.rotation, Quaternion.LookRotation(face), 12f * Time.deltaTime);
    }

    void HandleFootstepNoise()
    {
        if (_moveVelocity.sqrMagnitude < 0.25f) return;

        _footstepTimer -= Time.deltaTime * (IsSprinting ? 1.5f : 1f);
        if (_footstepTimer > 0f) return;
        _footstepTimer = _isCrouching ? footstepInterval * 1.6f : footstepInterval;

        float noiseRadius = _isCrouching ? crouchNoiseRadius :
                            IsSprinting  ? sprintNoiseRadius : walkNoiseRadius;

        AudioEventSystem.Emit(transform.position, noiseRadius);

        if (!_isCrouching)
        {
            if (_footstepAudio != null) _footstepAudio.TriggerStep();
            else if (footstepSource != null) footstepSource.Play();
        }
    }

    // ── API ─────────────────────────────────────────────────────────────────

    // Called by PlayerAbilities. wallNormal points away from the wall (Jonathan's back is to it).
    public void SetWallPressed(bool value, Vector3 wallNormal = default)
    {
        IsWallPressed = value;
        if (value && wallNormal != Vector3.zero)
        {
            _wallFacing = wallNormal;
            _wallFacing.y = 0f;
        }
    }

    public void SetHidden(bool hidden) => IsHidden = hidden;

    public void Teleport(Vector3 position, Quaternion rotation)
    {
        _cc.enabled = false;
        transform.SetPositionAndRotation(position, rotation);
        _cc.enabled = true;
        _moveVelocity = Vector3.zero;
        _vy = 0f;
    }
}
