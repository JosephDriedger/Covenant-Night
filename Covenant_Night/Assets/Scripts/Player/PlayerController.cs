using UnityEngine;

// Handles Jonathan's movement, crouch, sprint, gravity, and noise footprint.
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Speeds")]
    public float walkSpeed   = 4f;
    public float crouchSpeed = 1.8f;
    public float sprintSpeed = 7f;

    [Header("Crouch")]
    public float standingHeight = 1.8f;
    public float crouchHeight   = 1.0f;
    public Transform cameraTarget; // Cinemachine look-at target

    [Header("Gravity")]
    public float gravity = -20f;

    [Header("Noise Radii")]
    public float walkNoiseRadius   = 5f;
    public float sprintNoiseRadius = 12f;
    public float crouchNoiseRadius = 0f;

    [Header("Footstep Audio")]
    public AudioSource footstepSource;
    public float footstepInterval = 0.45f;

    // Read by GuardVision to apply wall-press suppression
    public bool IsWallPressed { get; private set; }
    public Vector3 Velocity   => _velocity;

    CharacterController _cc;
    Vector3 _velocity;
    Vector3 _moveVelocity;
    float   _footstepTimer;
    bool    _isCrouching;

    void Awake() => _cc = GetComponent<CharacterController>();

    void Update()
    {
        if (GameManager.Instance.IsPaused) return;

        HandleCrouch();
        HandleMovement();
        ApplyGravity();
        HandleFootstepNoise();
    }

    void HandleCrouch()
    {
        bool wantCrouch = InputReader.Instance.CrouchHeld;
        if (wantCrouch == _isCrouching) return;

        _isCrouching  = wantCrouch;
        _cc.height    = _isCrouching ? crouchHeight : standingHeight;
        _cc.center    = Vector3.up * (_cc.height * 0.5f);

        if (cameraTarget != null)
            cameraTarget.localPosition = Vector3.up * (_cc.height * 0.85f);
    }

    void HandleMovement()
    {
        Vector2 input = InputReader.Instance.Move;
        bool sprinting = !_isCrouching && InputReader.Instance.SprintHeld && input.sqrMagnitude > 0.01f;

        float speed = _isCrouching ? crouchSpeed : sprinting ? sprintSpeed : walkSpeed;

        // Camera-relative movement
        Transform cam = Camera.main.transform;
        Vector3 fwd   = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
        Vector3 right = Vector3.ProjectOnPlane(cam.right,   Vector3.up).normalized;
        _moveVelocity = (fwd * input.y + right * input.x).normalized * speed;

        _cc.Move(_moveVelocity * Time.deltaTime);

        if (_moveVelocity.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(_moveVelocity),
                12f * Time.deltaTime);
    }

    void ApplyGravity()
    {
        if (_cc.isGrounded && _velocity.y < 0f) _velocity.y = -2f;
        _velocity.y += gravity * Time.deltaTime;
        _cc.Move(new Vector3(0f, _velocity.y, 0f) * Time.deltaTime);
    }

    void HandleFootstepNoise()
    {
        if (_moveVelocity.sqrMagnitude < 0.01f) return;

        _footstepTimer -= Time.deltaTime;
        if (_footstepTimer > 0f) return;
        _footstepTimer = footstepInterval;

        float noiseRadius = _isCrouching ? crouchNoiseRadius :
                            InputReader.Instance.SprintHeld ? sprintNoiseRadius : walkNoiseRadius;

        AudioEventSystem.Emit(transform.position, noiseRadius);

        if (footstepSource != null && !_isCrouching)
            footstepSource.Play();
    }

    // Called by PlayerAbilities
    public void SetWallPressed(bool value) => IsWallPressed = value;
}
