using UnityEngine;
using UnityEngine.InputSystem;

// Singleton input reader. Loads actions by name from the InputActionAsset so no
// C# class generation is required — just drag InputSystem_Actions.inputactions
// onto the actionsAsset field in the Inspector.
public class InputReader : MonoBehaviour
{
    public static InputReader Instance { get; private set; }

    [SerializeField] InputActionAsset actionsAsset;

    // Cached per-frame values
    public Vector2 Move     { get; private set; }
    public Vector2 Look     { get; private set; }
    public bool SprintHeld  { get; private set; }
    public bool CrouchHeld  { get; private set; }
    public bool WallPressHeld      { get; private set; }   // Jump action repurposed
    public bool ThrowPressed       { get; private set; }   // Attack action
    public bool InteractPressed    { get; private set; }
    public bool DavidFollowPressed { get; private set; }   // Previous
    public bool DavidWaitPressed   { get; private set; }   // Next
    public bool DavidRunPressed    { get; private set; }   // Sprint tap
    public bool HarpPressed        { get; private set; }   // Interact tap

    InputAction _move, _look, _sprint, _crouch, _attack, _interact, _jump, _previous, _next;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        var map = actionsAsset.FindActionMap("Player", throwIfNotFound: true);
        _move     = map.FindAction("Move",     throwIfNotFound: true);
        _look     = map.FindAction("Look",     throwIfNotFound: true);
        _sprint   = map.FindAction("Sprint",   throwIfNotFound: true);
        _crouch   = map.FindAction("Crouch",   throwIfNotFound: true);
        _attack   = map.FindAction("Attack",   throwIfNotFound: true);
        _interact = map.FindAction("Interact", throwIfNotFound: true);
        _jump     = map.FindAction("Jump",     throwIfNotFound: true);
        _previous = map.FindAction("Previous", throwIfNotFound: true);
        _next     = map.FindAction("Next",     throwIfNotFound: true);

        map.Enable();
    }

    void Update()
    {
        Move          = _move.ReadValue<Vector2>();
        Look          = _look.ReadValue<Vector2>();
        SprintHeld    = _sprint.IsPressed();
        CrouchHeld    = _crouch.IsPressed();
        WallPressHeld = _jump.IsPressed();

        ThrowPressed       = _attack.WasPressedThisFrame();
        InteractPressed    = _interact.WasPressedThisFrame();
        DavidFollowPressed = _previous.WasPressedThisFrame();
        DavidWaitPressed   = _next.WasPressedThisFrame();
        DavidRunPressed    = _sprint.WasPressedThisFrame();
        HarpPressed        = _interact.WasPressedThisFrame();
    }

    void OnDestroy() => actionsAsset?.FindActionMap("Player")?.Disable();
}
