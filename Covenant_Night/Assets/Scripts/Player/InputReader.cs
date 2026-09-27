using UnityEngine;
using UnityEngine.InputSystem;

// Singleton input reader. Loads actions by name from the InputActionAsset so no
// C# class generation is required. Falls back to the project-wide actions asset.
//
// Controls (keyboard / gamepad):
//   Move WASD / left stick      Look mouse / right stick
//   Sneak  Left Shift / East button    Sprint  Left Ctrl or Cmd / L3
//   Wall press (Shadow Step) Space (hold) / South button
//   Throw stone LMB / Enter / West button
//   David: 1 Follow, 2 Wait, 3 Run (D-pad left / right / down), E toggles Follow/Wait
//   Harp H / D-pad up
//   Decoy G / Right Shoulder     Hush David Q / Left Shoulder
public class InputReader : MonoBehaviour
{
    public static InputReader Instance { get; private set; }

    [SerializeField] InputActionAsset actionsAsset;

    [Header("Look")]
    public float mouseSensitivity   = 0.09f;   // degrees per pixel
    public float gamepadLookSpeed   = 140f;    // degrees per second at full deflection

    // Cached per-frame values
    public Vector2 Move            { get; private set; }
    public Vector2 LookDelta       { get; private set; }   // degrees this frame (x = yaw, y = pitch)
    public bool SprintHeld         { get; private set; }
    public bool CrouchHeld         { get; private set; }   // "Sneak"
    public bool WallPressHeld      { get; private set; }   // Jump action repurposed
    public bool ThrowPressed       { get; private set; }   // Attack action
    public bool InteractPressed    { get; private set; }
    public bool DavidFollowPressed { get; private set; }   // Previous
    public bool DavidWaitPressed   { get; private set; }   // Next
    public bool DavidRunPressed    { get; private set; }   // dedicated "DavidRun" action
    public bool HarpPressed        { get; private set; }   // dedicated "Harp" action
    public bool DecoyPressed       { get; private set; }   // dedicated "Decoy" action
    public bool HushPressed        { get; private set; }   // dedicated "Hush" action
    public bool PausePressed       { get; private set; }   // Esc / Start
    public bool UsingGamepad       { get; private set; }   // last device used (drives on-screen control hints)

    // ── Scripted input (automated playtests) ────────────────────────────────
    [Header("Scripted input (automated playtests only)")]
    public bool scripted;
    public Vector2 scriptedMove;
    public bool scriptedSprint, scriptedCrouch, scriptedWallPress;
    public static bool ScriptedConfirm;
    bool _sThrow, _sFollow, _sWait, _sRun, _sHarp, _sDecoy, _sHush;

    public void InjectThrow()  => _sThrow  = true;
    public void InjectFollow() => _sFollow = true;
    public void InjectWait()   => _sWait   = true;
    public void InjectRun()    => _sRun    = true;
    public void InjectHarp()   => _sHarp   = true;
    public void InjectDecoy()  => _sDecoy  = true;
    public void InjectHush()   => _sHush   = true;

    InputActionMap _map;
    InputAction _move, _look, _sprint, _crouch, _attack, _interact, _jump, _previous, _next;
    InputAction _harp, _run, _decoy, _hush;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        var asset = actionsAsset != null ? actionsAsset : InputSystem.actions;
        _map      = asset.FindActionMap("Player", throwIfNotFound: true);
        _move     = _map.FindAction("Move",     throwIfNotFound: true);
        _look     = _map.FindAction("Look",     throwIfNotFound: true);
        _sprint   = _map.FindAction("Sprint",   throwIfNotFound: true);
        _crouch   = _map.FindAction("Crouch",   throwIfNotFound: true);
        _attack   = _map.FindAction("Attack",   throwIfNotFound: true);
        _interact = _map.FindAction("Interact", throwIfNotFound: true);
        _jump     = _map.FindAction("Jump",     throwIfNotFound: true);
        _previous = _map.FindAction("Previous", throwIfNotFound: true);
        _next     = _map.FindAction("Next",     throwIfNotFound: true);

        // Harp and David-Run get their own bindings so they no longer collide with Interact / Sprint.
        _harp = new InputAction("Harp", InputActionType.Button);
        _harp.AddBinding("<Keyboard>/h");
        _harp.AddBinding("<Gamepad>/dpad/up");
        _run = new InputAction("DavidRun", InputActionType.Button);
        _run.AddBinding("<Keyboard>/3");
        _run.AddBinding("<Gamepad>/dpad/down");

        _decoy = new InputAction("Decoy", InputActionType.Button);
        _decoy.AddBinding("<Keyboard>/g");
        _decoy.AddBinding("<Gamepad>/rightShoulder");

        _hush = new InputAction("Hush", InputActionType.Button);
        _hush.AddBinding("<Keyboard>/q");
        _hush.AddBinding("<Gamepad>/leftShoulder");

        _map.Enable();
        _harp.Enable();
        _run.Enable();
        _decoy.Enable();
        _hush.Enable();
    }

    void Update()
    {
        if (scripted)
        {
            Move          = scriptedMove;
            LookDelta     = Vector2.zero;
            SprintHeld    = scriptedSprint;
            CrouchHeld    = scriptedCrouch;
            WallPressHeld = scriptedWallPress;
            ThrowPressed       = _sThrow;
            DavidFollowPressed = _sFollow;
            DavidWaitPressed   = _sWait;
            DavidRunPressed    = _sRun;
            HarpPressed        = _sHarp;
            DecoyPressed       = _sDecoy;
            HushPressed        = _sHush;
            InteractPressed    = false;
            PausePressed       = false;
            _sThrow = _sFollow = _sWait = _sRun = _sHarp = _sDecoy = _sHush = false;
            return;
        }

        UpdateDeviceKind();

        Move          = _move.ReadValue<Vector2>();
        SprintHeld    = _sprint.IsPressed();
        CrouchHeld    = _crouch.IsPressed();
        WallPressHeld = _jump.IsPressed();

        Vector2 look = _look.ReadValue<Vector2>();
        LookDelta = _look.activeControl?.device is Gamepad
            ? look * gamepadLookSpeed * Time.unscaledDeltaTime
            : look * mouseSensitivity;
        LookDelta *= GameSettings.LookScale;
        if (GameSettings.InvertY) LookDelta = new Vector2(LookDelta.x, -LookDelta.y);

        ThrowPressed       = _attack.WasPressedThisFrame();
        InteractPressed    = _interact.WasPressedThisFrame();
        DavidFollowPressed = _previous.WasPressedThisFrame();
        DavidWaitPressed   = _next.WasPressedThisFrame();
        DavidRunPressed    = _run.WasPressedThisFrame();
        HarpPressed        = _harp.WasPressedThisFrame();
        DecoyPressed       = _decoy.WasPressedThisFrame();
        HushPressed        = _hush.WasPressedThisFrame();
        PausePressed       = (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) ||
                             (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame);
    }

    void UpdateDeviceKind()
    {
        var gp = Gamepad.current;
        var kb = Keyboard.current;
        var ms = Mouse.current;
        if (gp != null && gp.wasUpdatedThisFrame &&
            (gp.leftStick.ReadValue().sqrMagnitude > 0.09f || gp.rightStick.ReadValue().sqrMagnitude > 0.09f || AnyButton(gp)))
            UsingGamepad = true;
        else if ((kb != null && kb.anyKey.isPressed) || (ms != null && (ms.delta.ReadValue().sqrMagnitude > 1f || ms.leftButton.isPressed)))
            UsingGamepad = false;
        else if (gp != null && kb == null && ms == null)
            UsingGamepad = true;     // console: a pad is the only device
    }

    static bool AnyButton(Gamepad g) =>
        g.buttonSouth.isPressed || g.buttonEast.isPressed || g.buttonWest.isPressed || g.buttonNorth.isPressed ||
        g.leftShoulder.isPressed || g.rightShoulder.isPressed || g.leftStickButton.isPressed || g.startButton.isPressed ||
        g.dpad.up.isPressed || g.dpad.down.isPressed || g.dpad.left.isPressed || g.dpad.right.isPressed ||
        g.leftTrigger.isPressed || g.rightTrigger.isPressed;

    // Used by story panels / credits: any key, click, or main gamepad button.
    public static bool ConfirmPressedThisFrame()
    {
        if (ScriptedConfirm) { ScriptedConfirm = false; return true; }

        var k = Keyboard.current;
        if (k != null && k.anyKey.wasPressedThisFrame) return true;
        var m = Mouse.current;
        if (m != null && (m.leftButton.wasPressedThisFrame || m.rightButton.wasPressedThisFrame)) return true;
        var g = Gamepad.current;
        if (g != null && (g.buttonSouth.wasPressedThisFrame || g.startButton.wasPressedThisFrame)) return true;
        return false;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        _map?.Disable();
        _harp?.Dispose();
        _run?.Dispose();
        _decoy?.Dispose();
        _hush?.Dispose();
    }
}
