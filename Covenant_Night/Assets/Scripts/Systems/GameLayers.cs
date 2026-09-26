using UnityEngine;

// Single source of truth for the physics layers / tags used by the game.
// The editor builder (Assets/Editor/CovenantNightBuilder.cs) registers these in TagManager.
public static class GameLayers
{
    public const int Characters  = 6;   // Jonathan + David (guard targets)
    public const int Walls       = 7;   // solid environment geometry (blocks sight + camera)
    public const int Guards      = 8;   // guard bodies (excluded from NavMesh bake and from guard targeting)
    public const int GroundStone = 9;
    public const int GroundDirt  = 10;
    public const int GroundWood  = 11;
    public const int Roof        = 12;  // roof slabs: block sight, but not the camera; excluded from NavMesh
    public const int Civilians   = 13;  // townsfolk: solid for Jonathan, block guards' sight, avoided by guard agents

    public const string DavidTag = "David";

    public static int Mask(params int[] layers)
    {
        int m = 0;
        foreach (int l in layers) m |= 1 << l;
        return m;
    }

    // What blocks a guard's line of sight (a crowd is cover).
    public static int SightBlockers => Mask(Walls, Roof, Civilians);

    // Static geometry only (ceilings for standing up, walls for the shadow-step).
    public static int StaticSight => Mask(Walls, Roof);

    // What the third-person camera collides with.
    public static int CameraBlockers => Mask(Walls);

    // Anything solid a thrown stone / aim ray can land on.
    public static int Solid => Mask(0, Walls, Roof, GroundStone, GroundDirt, GroundWood);
}
