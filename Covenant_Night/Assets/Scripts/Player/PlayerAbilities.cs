using UnityEngine;

// Manages Jonathan's active abilities: stone throw, wall press, and David commands.
public class PlayerAbilities : MonoBehaviour
{
    [Header("Stone Throw")]
    public GameObject stonePrefab;
    public Transform  throwOrigin;
    public float      throwForce     = 14f;
    public int        stonesPerZone  = 3;

    [Header("Wall Press")]
    public float      wallDetectDist = 0.65f;
    public LayerMask  wallLayer;

    [Header("References")]
    public PlayerController controller;
    public DavidCompanion   david;

    public int StoneCount { get; private set; }

    bool _wallPressed;

    void Start() => SetStoneCount(stonesPerZone);

    void Update()
    {
        if (GameManager.Instance.IsPaused) return;
        HandleWallPress();
        HandleThrow();
        HandleDavidCommands();
    }

    // ── Stone Throw ─────────────────────────────────────────────────────────

    void HandleThrow()
    {
        if (!InputReader.Instance.ThrowPressed) return;
        if (StoneCount <= 0 || _wallPressed) return;

        // Aim toward screen centre
        Ray ray = Camera.main.ScreenPointToRay(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));
        Vector3 aimPoint = Physics.Raycast(ray, out RaycastHit hit, 30f)
            ? hit.point
            : ray.origin + ray.direction * 20f;

        Vector3 dir = (aimPoint - throwOrigin.position).normalized;
        var s = Instantiate(stonePrefab, throwOrigin.position, Quaternion.identity);
        s.GetComponent<Rigidbody>().AddForce(dir * throwForce, ForceMode.Impulse);
        // Prevent self-hit by ignoring the player's own collider briefly
        Physics.IgnoreCollision(s.GetComponent<Collider>(), GetComponent<Collider>(), true);

        SetStoneCount(StoneCount - 1);
    }

    // ── Wall Press ──────────────────────────────────────────────────────────

    void HandleWallPress()
    {
        if (InputReader.Instance.WallPressHeld &&
            Physics.Raycast(transform.position + Vector3.up * 0.8f,
                transform.forward, wallDetectDist, wallLayer))
        {
            if (!_wallPressed) EnterWallPress();
        }
        else
        {
            if (_wallPressed) ExitWallPress();
        }
    }

    void EnterWallPress()
    {
        _wallPressed = true;
        controller.SetWallPressed(true);
    }

    void ExitWallPress()
    {
        _wallPressed = false;
        controller.SetWallPressed(false);
    }

    // ── David Commands ──────────────────────────────────────────────────────

    void HandleDavidCommands()
    {
        if (david == null) return;
        if (InputReader.Instance.DavidFollowPressed) david.SetMode(DavidCompanion.Mode.Follow);
        if (InputReader.Instance.DavidWaitPressed)   david.SetMode(DavidCompanion.Mode.Wait);
        if (InputReader.Instance.DavidRunPressed)    david.SetMode(DavidCompanion.Mode.Run);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    public void AddStones(int count)    => SetStoneCount(StoneCount + count);
    public void SetStoneCount(int count)
    {
        StoneCount = Mathf.Max(0, count);
        HUD.Instance?.UpdateStoneCount(StoneCount);
    }

    public void ResetForZone() => SetStoneCount(stonesPerZone);
}
