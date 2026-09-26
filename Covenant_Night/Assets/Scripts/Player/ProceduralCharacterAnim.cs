using System.Collections;
using UnityEngine;

// Procedural animation for the low-poly humanoids (Visual/Rig/Hips/{Skirt, LegL, LegR, Torso/{Head, ArmL, ArmR, Cloak}}):
//   idle breathing, walk / run / sprint cycle, crouch-creep, wall-press flatten, guard patrol / suspicious /
//   alarmed poses (head scanning, spear levelled), and one-shots: PlayThrow, PlayHarp, PlayRaiseHand.
// Stylised, exaggerated poses — not realistic. If an Animator with a controller is present it is also fed the
// "Speed" / "IsCrouching" / "IsAlerted" parameters, so a Mixamo Humanoid rig can replace this later
// (set proceduralEnabled = false then).
public class ProceduralCharacterAnim : MonoBehaviour
{
    public Transform visual;
    public Animator  animator;
    public bool      proceduralEnabled = true;
    [Tooltip("Extra head turn in degrees (civilians look toward sounds); smoothed.")]
    public float     lookYaw;

    PlayerController _player;
    DavidCompanion   _david;
    GuardFSM         _guard;

    Transform _hips, _torso, _head, _armL, _armR, _legL, _legR, _cloak, _spear;
    Vector3 _lastPos;
    float _speed, _phase, _t, _crouchT, _alertT, _alarmT, _wallT;
    float _seed;

    // one-shot animations
    float _throwT = -1f, _harpT = -1f, _raiseT = -1f, _gestT = -1f, _flinchT = -1f;
    float _lookYaw;
    float _harpLen, _raiseLen;

    static readonly int SpeedHash   = Animator.StringToHash("Speed");
    static readonly int CrouchHash  = Animator.StringToHash("IsCrouching");
    static readonly int AlertedHash = Animator.StringToHash("IsAlerted");

    static Transform FindDeep(Transform root, string name)
    {
        if (root == null) return null;
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t;
        return null;
    }

    void Awake()
    {
        _player = GetComponent<PlayerController>();
        _david  = GetComponent<DavidCompanion>();
        _guard  = GetComponent<GuardFSM>();
        if (visual == null) visual = transform.Find("Visual");
        _hips  = FindDeep(visual, "Hips");
        _torso = FindDeep(visual, "Torso");
        _head  = FindDeep(visual, "Head");
        _armL  = FindDeep(visual, "ArmL");
        _armR  = FindDeep(visual, "ArmR");
        _legL  = FindDeep(visual, "LegL");
        _legR  = FindDeep(visual, "LegR");
        _cloak = FindDeep(visual, "Cloak");
        _spear = FindDeep(visual, "Spear");
        _lastPos = transform.position;
        _seed = Random.value * 20f;
    }

    // ── one-shots ───────────────────────────────────────────────────────────

    public void PlayThrow()               { _throwT = 0f; }
    public void PlayGesture()             { _gestT = 0f; }      // small talking / waving arm motion
    public void PlayFlinch()              { _flinchT = 0f; }    // duck and throw the arms up
    public void PlayHarp(float seconds = 2.2f)      { _harpT = 0f; _harpLen = seconds; }
    public void PlayRaiseHand(float seconds = 3f)   { _raiseT = 0f; _raiseLen = seconds; }

    // ── update ──────────────────────────────────────────────────────────────

    void LateUpdate()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;
        _t += dt;

        Vector3 delta = transform.position - _lastPos;
        delta.y = 0f;
        _lastPos = transform.position;
        float inst = delta.magnitude / dt;
        if (inst > 30f) inst = 0f;                              // teleport
        _speed = Mathf.Lerp(_speed, inst, 1f - Mathf.Exp(-12f * dt));

        bool crouching = (_player != null && _player.IsCrouching) || (_david != null && _david.IsCrouching);
        bool sprinting = _player != null && _player.IsSprinting;
        bool wallPress = _player != null && _player.IsWallPressed;
        var state = _guard != null ? _guard.State : GuardState.Unaware;
        bool alerted = _guard != null && state != GuardState.Unaware;

        if (animator != null && animator.runtimeAnimatorController != null)
        {
            animator.SetFloat(SpeedHash, _speed);
            animator.SetBool(CrouchHash, crouching);
            animator.SetBool(AlertedHash, alerted);
        }
        if (!proceduralEnabled || _hips == null) return;

        _crouchT = Mathf.MoveTowards(_crouchT, crouching ? 1f : 0f, dt * 5f);
        _wallT   = Mathf.MoveTowards(_wallT, wallPress ? 1f : 0f, dt * 6f);
        _alertT  = Mathf.MoveTowards(_alertT, state != GuardState.Unaware ? 1f : 0f, dt * 4f);
        _alarmT  = Mathf.MoveTowards(_alarmT, state == GuardState.Alarmed ? 1f : 0f, dt * 5f);

        float moving = Mathf.Clamp01(_speed / 0.7f);
        float freq = 2.3f * (crouching ? 1.2f : 1f);
        _phase += dt * _speed * freq;
        float s = Mathf.Sin(_phase), c = Mathf.Cos(_phase);

        float legAmp = Mathf.Clamp(_speed * 9f, 0f, sprinting || _alarmT > 0.5f ? 52f : 40f) * Mathf.Lerp(1f, 0.55f, _crouchT);
        float armAmp = legAmp * 0.85f;
        float lean = Mathf.Clamp(_speed * 3.2f, 0f, 16f) + _crouchT * 34f - _wallT * 6f;
        float bob = Mathf.Abs(s) * 0.05f * moving * (1f - _crouchT);
        float breathe = Mathf.Sin(_t * 2.2f + _seed);

        // ── hips / legs ──
        float crouchDrop = 0.5f * _crouchT;
        _hips.localPosition = new Vector3(0f, 0.98f - crouchDrop + bob, 0f);
        _hips.localRotation = Quaternion.Euler(0f, s * 5f * moving, s * 2f * moving);

        float crouchFold = -60f * _crouchT;
        _legL.localRotation = Quaternion.Euler(s * legAmp + crouchFold, 0f, 0f);
        _legR.localRotation = Quaternion.Euler(-s * legAmp + crouchFold, 0f, 0f);

        // ── torso ──
        _torso.localRotation = Quaternion.Euler(lean, -s * 4f * moving, 0f);
        _torso.localScale = new Vector3(1f, 1f + breathe * 0.012f, 1f);

        // ── arms ──
        float armLx = -s * armAmp, armRx = s * armAmp;
        float armLz = -6f - _wallT * 22f, armRz = 6f + _wallT * 22f;
        float idleSway = breathe * 2f * (1f - moving);
        armLx += idleSway; armRx -= idleSway;

        bool guardLike = _guard != null;
        if (guardLike)
        {
            // spear arm: carried at the ready; levelled when alarmed
            float carry = Mathf.Lerp(-12f + armAmp * 0.15f * s, -55f, _alarmT);
            armRx = carry;
            armRz = 8f;
            if (_spear != null) _spear.localRotation = Quaternion.Euler(Mathf.Lerp(0f, 145f, _alarmT), 0f, 0f);
            armLx = Mathf.Lerp(armLx, -25f, _alarmT);
        }

        // one-shot: throw (wind-up, overhead release, follow-through)
        if (_throwT >= 0f)
        {
            _throwT += dt;
            float k = _throwT / 0.5f;
            if (k >= 1f) _throwT = -1f;
            else
            {
                float x = k < 0.3f ? Mathf.Lerp(0f, 55f, k / 0.3f)
                        : k < 0.6f ? Mathf.Lerp(55f, -165f, (k - 0.3f) / 0.3f)
                        :            Mathf.Lerp(-165f, -20f, (k - 0.6f) / 0.4f);
                armRx = x; armRz = 8f;
                _torso.localRotation = Quaternion.Euler(lean, Mathf.Lerp(-25f, 20f, Mathf.Clamp01(k * 1.6f)), 0f);
            }
        }

        // one-shot: harp — both arms up and forward, hands strumming
        if (_harpT >= 0f)
        {
            _harpT += dt;
            float k = _harpT / _harpLen;
            if (k >= 1f) _harpT = -1f;
            else
            {
                float env = Mathf.Clamp01(Mathf.Min(k * 6f, (1f - k) * 6f));
                float strum = Mathf.Sin(_t * 14f) * 12f * env;
                armLx = Mathf.Lerp(armLx, -80f + strum, env);
                armRx = Mathf.Lerp(armRx, -95f - strum, env);
                armLz = Mathf.Lerp(armLz, 10f, env);
                armRz = Mathf.Lerp(armRz, -10f, env);
                _torso.localRotation *= Quaternion.Euler(-6f * env, Mathf.Sin(_t * 3f) * 6f * env, 0f);
            }
        }

        // one-shot: raise the right hand (Jonathan showing the signet ring)
        if (_raiseT >= 0f)
        {
            _raiseT += dt;
            float k = _raiseT / _raiseLen;
            if (k >= 1f) _raiseT = -1f;
            else
            {
                float env = Mathf.Clamp01(Mathf.Min(k * 5f, (1f - k) * 5f));
                armRx = Mathf.Lerp(armRx, -125f, env);
                armRz = Mathf.Lerp(armRz, 22f, env);
            }
        }

        // one-shot: conversational gesture
        if (_gestT >= 0f)
        {
            _gestT += dt;
            float k = _gestT / 1.4f;
            if (k >= 1f) _gestT = -1f;
            else
            {
                float env = Mathf.Clamp01(Mathf.Min(k * 5f, (1f - k) * 4f));
                armRx = Mathf.Lerp(armRx, -70f + Mathf.Sin(_t * 9f) * 18f, env);
                armRz = Mathf.Lerp(armRz, 18f, env);
            }
        }

        // one-shot: flinch (startled by the alarm)
        if (_flinchT >= 0f)
        {
            _flinchT += dt;
            float k = _flinchT / 0.9f;
            if (k >= 1f) _flinchT = -1f;
            else
            {
                float env = Mathf.Clamp01(Mathf.Min(k * 8f, (1f - k) * 3f));
                armLx = Mathf.Lerp(armLx, -120f, env); armRx = Mathf.Lerp(armRx, -120f, env);
                armLz = Mathf.Lerp(armLz, -25f, env);  armRz = Mathf.Lerp(armRz, 25f, env);
                _hips.localPosition += Vector3.down * (0.18f * env);
                _torso.localRotation *= Quaternion.Euler(20f * env, 0f, 0f);
            }
        }

        _armL.localRotation = Quaternion.Euler(armLx, 0f, armLz);
        _armR.localRotation = Quaternion.Euler(armRx, 0f, armRz);

        // ── head ──
        float yaw = 0f, pitch = 0f;
        if (guardLike)
        {
            if (state == GuardState.Unaware) yaw = Mathf.Sin(_t * 0.7f + _seed) * 22f;
            else if (state == GuardState.Suspicious) yaw = Mathf.Sin(_t * 2.6f + _seed) * 55f;
            pitch = -_alarmT * 6f;
        }
        else pitch = -lean * 0.5f + breathe * 1.2f;        // keep looking ahead while the torso leans
        _lookYaw = Mathf.Lerp(_lookYaw, lookYaw, 1f - Mathf.Exp(-6f * dt));
        yaw += _wallT * 35f + _lookYaw;
        _head.localRotation = Quaternion.Euler(pitch, yaw, Mathf.Sin(_t * 1.3f + _seed) * 1.5f);

        // ── cloak trails behind ──
        if (_cloak != null)
            _cloak.localRotation = Quaternion.Euler(Mathf.Clamp(_speed * 5f, 0f, 32f) + Mathf.Sin(_t * 3f + _seed) * 2.5f + lean * 0.3f, 0f, Mathf.Sin(_t * 2f + _seed) * 1.5f);
    }
}
