using UnityEngine;

// Manages Jonathan's active abilities: stone throw, wall press (Shadow Step), and David commands.
public class PlayerAbilities : MonoBehaviour
{
    [Header("Stone Throw")]
    public GameObject stonePrefab;
    public Transform  throwOrigin;
    public float      throwSpeed     = 14f;
    public float      maxThrowRange  = 18f;
    public int        stonesPerZone  = 3;
    public Transform  aimMarker;          // optional landing-spot marker

    [Header("Decoy")]
    public GameObject decoyPrefab;
    public int        decoysPerZone  = 1;

    [Header("Wall Press")]
    public float      wallDetectDist = 0.85f;
    public LayerMask  wallLayer;

    [Header("References")]
    public PlayerController controller;
    public DavidCompanion   david;

    public int StoneCount { get; private set; }
    public int DecoyCount { get; private set; }

    bool   _wallPressed;
    Camera _cam;

    int StonesThisZone => Mathf.Max(1, stonesPerZone + GameDifficulty.Tuning.stoneDelta);

    void Start()
    {
        SetStoneCount(StonesThisZone);
        SetDecoyCount(decoysPerZone);
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsPaused)
        {
            if (aimMarker != null) aimMarker.gameObject.SetActive(false);
            return;
        }
        HandleWallPress();
        HandleThrow();
        HandleDecoy();
        HandleDavidCommands();
    }

    // ── Stone Throw ─────────────────────────────────────────────────────────

    bool TryGetAimPoint(out Vector3 aimPoint, out bool onSurface)
    {
        if (_cam == null) _cam = Camera.main;
        aimPoint  = transform.position + transform.forward * 8f;
        onSurface = false;
        if (_cam == null) return false;

        Ray ray = _cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        int mask = GameLayers.Solid;
        if (Physics.Raycast(ray, out RaycastHit hit, 80f, mask, QueryTriggerInteraction.Ignore))
        {
            aimPoint  = hit.point;
            onSurface = true;
        }
        else aimPoint = ray.origin + ray.direction * maxThrowRange;

        Vector3 origin = throwOrigin != null ? throwOrigin.position : transform.position + Vector3.up * 1.3f;
        Vector3 to = aimPoint - origin;
        if (to.magnitude > maxThrowRange)
        {
            aimPoint  = origin + to.normalized * maxThrowRange;
            onSurface = false;
        }
        return true;
    }

    void HandleThrow()
    {
        bool have = StoneCount > 0;
        if (aimMarker != null)
        {
            if (have && TryGetAimPoint(out Vector3 p, out bool surf) && surf)
            {
                aimMarker.gameObject.SetActive(true);
                aimMarker.position = p + Vector3.up * 0.04f;
            }
            else aimMarker.gameObject.SetActive(false);
        }

        if (!InputReader.Instance.ThrowPressed) return;
        if (!have || stonePrefab == null) return;
        if (!TryGetAimPoint(out Vector3 aimPoint, out _)) return;

        Vector3 origin = throwOrigin != null ? throwOrigin.position : transform.position + Vector3.up * 1.3f;
        var go = Instantiate(stonePrefab, origin, Quaternion.identity);
        var rb = go.GetComponent<Rigidbody>();
        rb.linearVelocity = SolveLaunchVelocity(origin, aimPoint, throwSpeed);

        // Don't collide with the thrower / David on the way out
        var stoneCol = go.GetComponent<Collider>();
        foreach (var c in GetComponentsInChildren<Collider>()) Physics.IgnoreCollision(stoneCol, c, true);
        if (david != null)
            foreach (var c in david.GetComponentsInChildren<Collider>()) Physics.IgnoreCollision(stoneCol, c, true);

        SetStoneCount(StoneCount - 1);
        GetComponent<ProceduralCharacterAnim>()?.PlayThrow();
    }

    // ── Decoy ───────────────────────────────────────────────────────────────

    void HandleDecoy()
    {
        if (!InputReader.Instance.DecoyPressed) return;
        if (DecoyCount <= 0 || decoyPrefab == null) return;

        Vector3 origin = throwOrigin != null ? throwOrigin.position : transform.position + Vector3.up * 1.3f;
        Instantiate(decoyPrefab, origin, Quaternion.identity);
        SetDecoyCount(DecoyCount - 1);
    }

    // Low-arc ballistic solution to hit `target` from `origin` at a fixed launch speed.
    static Vector3 SolveLaunchVelocity(Vector3 origin, Vector3 target, float speed)
    {
        Vector3 d = target - origin;
        Vector3 flat = new Vector3(d.x, 0f, d.z);
        float x = flat.magnitude;
        float y = d.y;
        float g = -Physics.gravity.y;

        if (x < 0.4f) return d.normalized * speed;

        float v2   = speed * speed;
        float disc = v2 * v2 - g * (g * x * x + 2f * y * v2);
        float angle = disc < 0f
            ? Mathf.PI * 0.25f
            : Mathf.Atan((v2 - Mathf.Sqrt(disc)) / (g * x));

        return flat.normalized * (speed * Mathf.Cos(angle)) + Vector3.up * (speed * Mathf.Sin(angle));
    }

    // ── Wall Press (Shadow Step) ────────────────────────────────────────────

    void HandleWallPress()
    {
        if (InputReader.Instance.WallPressHeld && TryFindWall(out Vector3 normal))
        {
            _wallPressed = true;
            controller.SetWallPressed(true, normal);
        }
        else if (_wallPressed)
        {
            _wallPressed = false;
            controller.SetWallPressed(false);
        }
    }

    // Looks around Jonathan for the closest vertical surface; returns its outward normal.
    bool TryFindWall(out Vector3 normal)
    {
        normal = Vector3.zero;
        Vector3 origin = transform.position + Vector3.up * 0.9f;
        float best = float.MaxValue;
        for (int i = 0; i < 8; i++)
        {
            Vector3 dir = Quaternion.Euler(0f, i * 45f, 0f) * Vector3.forward;
            if (Physics.Raycast(origin, dir, out RaycastHit hit, wallDetectDist, wallLayer, QueryTriggerInteraction.Ignore)
                && Mathf.Abs(hit.normal.y) < 0.3f && hit.distance < best)
            {
                best   = hit.distance;
                normal = hit.normal;
            }
        }
        return best < float.MaxValue;
    }

    // ── David Commands ──────────────────────────────────────────────────────

    void HandleDavidCommands()
    {
        if (david == null) return;
        var input = InputReader.Instance;
        if (input.DavidFollowPressed) david.SetMode(DavidCompanion.Mode.Follow, announce: true);
        if (input.DavidWaitPressed)   david.SetMode(DavidCompanion.Mode.Wait,   announce: true);
        if (input.DavidRunPressed)    david.SetMode(DavidCompanion.Mode.Run,    announce: true);
        if (input.InteractPressed)
            david.SetMode(david.CurrentMode == DavidCompanion.Mode.Follow
                ? DavidCompanion.Mode.Wait : DavidCompanion.Mode.Follow, announce: true);
        if (input.HarpPressed) david.TryHarp();
        if (input.HushPressed) david.TryHush();
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    public void AddStones(int count)    => SetStoneCount(StoneCount + count);
    public void SetStoneCount(int count)
    {
        StoneCount = Mathf.Max(0, count);
        if (HUD.Instance != null) HUD.Instance.UpdateStoneCount(StoneCount);
    }

    public void SetDecoyCount(int count)
    {
        DecoyCount = Mathf.Max(0, count);
        if (HUD.Instance != null) HUD.Instance.UpdateDecoyCount(DecoyCount);
    }

    public void ResetForZone()
    {
        SetStoneCount(StonesThisZone);
        SetDecoyCount(decoysPerZone);
        _wallPressed = false;
        controller.SetWallPressed(false);
    }
}
