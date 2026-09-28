using System.Collections.Generic;
using UnityEngine;

// The five hand-blocked zones. Z runs "north" from the entry (z≈3) to the exit (z≈69).
// Each zone is described by its walkable rectangles (everything else becomes buildings) plus dressing,
// guards, hiding spots, pots, torch pools and David's run waypoints.
public static class ZoneBuilders
{
    static readonly R Floor = new R(-22, 0, 22, 72);

    static void Common(ZoneKit k, R floor)
    {
        k.Ground(floor, k.M.floorStone);
        float h = 5f;
        k.Block(new R(floor.x0 - 1, floor.z0 - 1, floor.x0, floor.z1 + 1), h, k.M.darkStone, GameLayers.Walls, 0, "Boundary");
        k.Block(new R(floor.x1, floor.z0 - 1, floor.x1 + 1, floor.z1 + 1), h, k.M.darkStone, GameLayers.Walls, 0, "Boundary");
        k.Block(new R(floor.x0 - 1, floor.z0 - 1, floor.x1 + 1, floor.z0), h, k.M.darkStone, GameLayers.Walls, 0, "Boundary");
        k.Block(new R(floor.x0 - 1, floor.z1, floor.x1 + 1, floor.z1 + 1), h, k.M.darkStone, GameLayers.Walls, 0, "Boundary");
        k.Moonlight();
    }

    static Vector2 V(float x, float z) => new Vector2(x, z);
    static Vector3 V3(float x, float y, float z) => new Vector3(x, y, z);

    // ═══════════════════════════════════════════════════════════════════════
    // ZONE 1 — THE PALACE DISTRICT
    // Dense guards who recognise Jonathan's face (awareness gain x1.3). Three routes: the open west passage
    // (fast, patrolled), the narrow east alley, and the dark servant corridor with hiding nooks (slow, safe).
    // ═══════════════════════════════════════════════════════════════════════
    public static void Zone1(ZoneKit k, StoryAssets story)
    {
        Common(k, Floor);
        var hall = new R(-9, 27, 9, 47);
        var walk = new List<R>
        {
            new R(-22, 0, 22, 14),          // entry court
            new R(-6, 14, 6, 15.2f),        // main gate
            new R(13, 14, 17, 15.2f),       // servant door
            new R(-22, 15.2f, 12, 27),      // forecourt
            new R(-22, 27, -9, 47),         // west passage
            new R(9, 27, 12, 47),           // east alley
            new R(13, 15.2f, 17, 47),       // servant corridor
            new R(17, 24, 20.5f, 27.5f),    // nook 1
            new R(17, 36, 20.5f, 39.5f),    // nook 2
            new R(-22, 47, 22, 72),         // north courtyard
        };
        k.CarveBuildings(Floor, walk, r => hall.Contains(r.Center) ? 7f : 4.5f, 1);
        k.Patch(new R(-22, 47, 22, 72), k.M.dirt, GameLayers.GroundDirt);

        // ── dressing ──
        foreach (float x in new[] { -16f, 16f }) k.PlaceTorch(x, 9);
        k.PlaceTorch(-7.2f, 13.4f, true); k.PlaceTorch(7.2f, 13.4f);
        k.PlaceTorch(-16, 22); k.PlaceTorch(2, 22, true);
        k.PlaceTorch(-10.6f, 30); k.PlaceTorch(-10.6f, 44, true);
        k.PlaceTorch(15, 45.6f);
        k.PlaceTorch(-8, 50); k.PlaceTorch(8, 50); k.PlaceTorch(-16, 60); k.PlaceTorch(16, 60, true);

        k.LowCover(-8, 22.5f, 2.4f, 1.2f, 1.1f, k.M.clothOchre, "Planter");
        k.LowCover(-18, 24, 1.2f, 3f, 1.1f, k.M.clothOchre, "Planter");
        k.LowCover(-14, 31, 4f, 1.2f, 1.1f, k.M.clothOchre, "Planter");
        k.LowCover(-14.5f, 42, 3f, 1.2f, 1.1f, k.M.clothOchre, "Planter");
        k.LowCover(-15.5f, 56, 4f, 1.2f, 1.1f, k.M.clothOchre, "Planter");
        k.LowCover(15.5f, 56, 4f, 1.2f, 1.1f, k.M.clothOchre, "Planter");
        k.LowCover(-6, 64, 3f, 1.2f, 1.1f, k.M.clothOchre, "Planter");
        k.LowCover(6, 64, 3f, 1.2f, 1.1f, k.M.clothOchre, "Planter");
        k.Pillar(-4, 58); k.Pillar(4, 58);
        k.LowCover(16.35f, 21, 1.3f, 1.3f, 1.1f, k.M.crateWood, "Crate");
        k.LowCover(13.65f, 32, 1.3f, 1.3f, 1.1f, k.M.crateWood, "Crate");
        k.LowCover(5, 8, 1.6f, 1.4f, 1.1f, k.M.crateWood, "Crate");

        k.Alcove(-20, 6, 'E');
        k.Alcove(-20, 38, 'E');
        k.Alcove(20, 60, 'W');
        k.HidingSpot(18.75f, 25.75f, 3.2f, 3.4f);
        k.HidingSpot(18.75f, 37.75f, 3.2f, 3.4f);

        k.Pot(18, 4); k.Pot(-21, 30, 2); k.Pot(18.7f, 25.75f); k.Pot(14, 40); k.Pot(-20, 66); k.Pot(11, 33);
        k.Scroll(18.6f, 4.6f, story.loreLedger); k.Scroll(-20.4f, 66.6f, story.loreSoldier);

        // ── guards ──
        const float g = 1.3f;   // they recognise Jonathan's face
        // The first zone is the gentlest: four guards, and the zone strength below keeps their sight short.
        k.Guard(GuardType.Sentry,   V3(-4.5f, 0, 17.2f), 60f, gain: g);
        k.Guard(GuardType.Patrol,   V3(0, 0, 0), 0, new[] { V(-16, 19), V(8, 19), V(8, 24), V(-16, 24) }, gain: g);
        k.Guard(GuardType.Patrol,   V3(0, 0, 0), 0, new[] { V(-11, 29), V(-17, 29), V(-17, 45), V(-11, 45) }, gain: g);
        k.Guard(GuardType.Commander, V3(0, 0, 0), 0, new[] { V(-12, 52), V(12, 52), V(12, 62), V(-12, 62) }, gain: 1f);

        // ── townsfolk, greenery, banners ──
        k.NpcPair(0, V(10, 7), 3, V(11.6f, 7.7f));
        k.Npc(2, V3(0, 0, 0), 0, path: new[] { V(-19.5f, 17), V(-19.5f, 26) });
        k.Npc(1, V3(16.3f, 0, 40), 270f);
        k.Npc(4, V3(0, 0, 0), 0, path: new[] { V(-18, 66), V(-12, 66) });
        k.Npc(5, V3(-6.5f, 0, 50.5f), 180f);
        k.Tree(-13, 2, true); k.Tree(13, 2, true); k.Tree(-19, 52, true); k.Tree(19, 52, true); k.Tree(-19, 68, true); k.Tree(19, 68, true);
        k.Banner(-5, 26.9f, Vector3.back, 6.4f, 1.1f, 3.4f); k.Banner(5, 26.9f, Vector3.back, 6.4f, 1.1f, 3.4f);
        k.Banner(-5, 47.1f, Vector3.forward, 6.4f, 1.1f, 3.4f); k.Banner(5, 47.1f, Vector3.forward, 6.4f, 1.1f, 3.4f);
        k.Fireflies(new R(-20, 50, 20, 70), 24);

        // ── zone plumbing ──
        k.Exit(0, 69.5f, 8f);
        k.Entry("The Palace District", "Zone 1", V(0, 3), 0f,
            new[] { V(0, 11), V(15, 19), V(-16, 26), V(15, 42), V(-16, 44), V(-6, 58) }, difficulty: 0.70f, ambient: 0.28f);

        // ── intro cutscene: live camera work and performance in place of the old text-and-picture opening ──
        var introRoot = new GameObject("IntroCutscene");
        introRoot.transform.SetParent(k.root, false);
        var intro = introRoot.AddComponent<IntroCutscene>();
        Transform IMark(string mname, Vector3 pos, float yaw = 0f)
        {
            var mt = new GameObject(mname).transform;
            mt.SetParent(introRoot.transform, false);
            mt.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0));
            return mt;
        }
        intro.beats = story.intro;
        intro.togetherMark  = IMark("TogetherMark", V3(0.9f, 0.05f, 3f));
        intro.shotWake      = IMark("ShotWake", V3(1.3f, 1.55f, 1.4f));
        intro.shotTogether  = IMark("ShotTogether", V3(2.6f, 1.75f, 2.2f));
        intro.shotAhead     = IMark("ShotAhead", V3(0f, 2.1f, 0.2f));
        intro.lookAtJonathan = IMark("LookAtJonathan", V3(0f, 1.6f, 3f));
        intro.lookAtAhead    = IMark("LookAtAhead", V3(0f, 1.5f, 15f));

        k.Ambience(new[] { V3(-27, 3, 20), V3(27, 3, 50), V3(0, 3, 78) });
        k.BakeNavMesh();
        k.VerifyRoute("west passage", V(0, 3), V(0, 12), V(-14, 20), V(-14, 30), V(-14, 46), V(0, 55), V(0, 68));
        k.VerifyRoute("servant corridor", V(0, 3), V(15, 12), V(15, 20), V(15, 46), V(5, 55), V(0, 68));
        k.VerifyRoute("east alley", V(0, 3), V(0, 12), V(10.5f, 20), V(10.5f, 46), V(0, 55), V(0, 68));
        k.Save();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ZONE 2 — THE MARKET QUARTER
    // Open plaza with sparse cover (stalls, carts, torch shadow gaps) vs. a west lane lit by fire pits.
    // ═══════════════════════════════════════════════════════════════════════
    public static void Zone2(ZoneKit k, StoryAssets story)
    {
        Common(k, Floor);
        var walk = new List<R>
        {
            new R(-5, 0, 5, 17),            // entry lane
            new R(-14, 17, 15, 55),         // plaza
            new R(-22, 17, -15, 55),        // west lane (fire pits)
            new R(-15, 19, -14, 22.5f),     // west lane gap (south)
            new R(-15, 49, -14, 52.5f),     // west lane gap (north)
            new R(-8, 55, 8, 72),           // north lane
        };
        k.CarveBuildings(Floor, walk, r => (r.W < 1.5f || r.D < 1.5f) ? 3.2f : 4.5f, 5);
        k.Patch(new R(-14, 17, 15, 55), k.M.dirt, GameLayers.GroundDirt);
        k.Patch(new R(-22, 17, -15, 55), k.M.dirt, GameLayers.GroundDirt);

        // ── dressing ──
        k.PlaceTorch(-10, 21, true); k.PlaceTorch(10, 21);
        k.PlaceTorch(-11, 36); k.PlaceTorch(11, 36, true);
        k.PlaceTorch(-2, 50); k.PlaceTorch(7, 50);
        k.PlaceTorch(-7.5f, 58); k.PlaceTorch(7.5f, 58);
        k.FirePit(-18.5f, 29); k.FirePit(-18.5f, 44);

        k.Stall(-7, 26, 3f, 1.6f, k.M.clothRed);
        k.Stall(6, 28, 3f, 1.6f, k.M.clothBlue);
        k.Stall(-7, 46, 3f, 1.6f, k.M.clothOchre);
        k.Stall(9, 43, 3f, 1.6f, k.M.clothRed);
        k.Cart(0, 24); k.Cart(-4, 40); k.Cart(4, 49); k.Cart(10, 33, false);
        k.LowCover(-3, 8, 1.4f, 1.4f, 1.1f, k.M.crateWood, "Crate");
        k.LowCover(3.2f, 12.5f, 1.4f, 1.4f, 1.1f, k.M.crateWood, "Crate");
        k.LowCover(-7, 59, 1.4f, 3f, 1.1f, k.M.crateWood, "Crate");
        k.LowCover(7, 64, 1.4f, 3f, 1.1f, k.M.crateWood, "Crate");
        k.Pillar(0, 65, 3.5f, 1f);

        k.Alcove(-20, 36.5f, 'E');
        k.Alcove(-20, 24, 'E');
        k.HidingSpot(3.5f, 3, 2.6f, 2.6f);

        k.Pot(-16.5f, 21); k.Pot(-20.5f, 48, 2); k.Pot(12, 44); k.Pot(3.5f, 3); k.Pot(-7.5f, 68); k.Pot(-3, 31);
        k.Scroll(-16.9f, 21.6f, story.loreMarket);

        // ── guards ──
        k.Guard(GuardType.Patrol,   V3(0, 0, 0), 0, new[] { V(-11, 20), V(12, 20), V(12, 52), V(-11, 52) });
        k.Guard(GuardType.Patrol,   V3(0, 0, 0), 0, new[] { V(-12, 35), V(13, 35) }, pingPong: true);
        k.Guard(GuardType.Commander, V3(0, 0, 0), 0, new[] { V(-4, 57), V(4, 57), V(4, 63), V(-4, 63) });
        k.Guard(GuardType.Sentry,   V3(13.5f, 0, 53.5f), 215f);
        k.Guard(GuardType.Sentry,   V3(-6.5f, 0, 66), 60f);

        // ── townsfolk, greenery ──
        k.Npc(0, V3(-7, 0, 27.6f), 180f);          // merchants behind their stalls
        k.Npc(3, V3(6, 0, 29.6f), 180f);
        k.Npc(4, V3(-7, 0, 47.6f), 180f);
        k.Npc(2, V3(9, 0, 44.6f), 180f);
        k.Npc(1, V3(0, 0, 25.7f), 180f);           // cart owner
        k.Npc(5, V3(0, 0, 0), 0, path: new[] { V(-3, 29.5f), V(3, 29.5f) });                 // shoppers
        k.Npc(1, V3(0, 0, 0), 0, path: new[] { V(-12.6f, 24), V(-12.6f, 33) });            // walks the guards' beat
        k.Npc(5, V3(-16.7f, 0, 29.5f), 270f);      // warming up at the fire pits
        k.Npc(2, V3(-16.7f, 0, 44), 270f);
        k.NpcPair(0, V(-2.6f, 6.5f), 4, V(-1.2f, 7.2f));
        k.Tree(-13.2f, 47, false, 1.1f); k.Tree(14.3f, 26.5f, false, 1.1f);
        k.Fireflies(new R(-13, 20, 14, 54), 30);

        // ── zone plumbing ──
        k.Exit(0, 69.5f, 8f);
        k.Entry("The Market Quarter", "Zone 2", V(0, 3), 0f,
            new[] { V(0, 12), V(-18.5f, 24), V(-6, 31), V(-18.5f, 47), V(-5, 50), V(0, 62) }, difficulty: 0.80f, ambient: 0.34f);
        k.Ambience(new[] { V3(-27, 3, 25), V3(27, 3, 45), V3(0, 3, 80) });
        k.BakeNavMesh();
        k.VerifyRoute("plaza", V(0, 3), V(0, 20), V(0, 35), V(0, 53), V(0, 68));
        k.VerifyRoute("west lane", V(0, 3), V(0, 18), V(-14.5f, 20.5f), V(-18.5f, 25), V(-18.5f, 48), V(-14.5f, 50.5f), V(-8, 54), V(0, 68));
        k.Save();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ZONE 3 — POTTER'S ALLEY
    // A snake of 3 m alleys, dog pens that bark if you stray, and an exposed rooftop shortcut watched by a sentry.
    // ═══════════════════════════════════════════════════════════════════════
    public static void Zone3(ZoneKit k, StoryAssets story)
    {
        Common(k, Floor);
        var pen1 = new R(5, 21, 9, 25);
        var pen2 = new R(-8, 39, -4, 43);
        var walk = new List<R>
        {
            new R(-6, 0, 6, 12),                // yard
            new R(-1.5f, 12, 1.5f, 21),         // L1
            new R(-1.5f, 18, 15.5f, 21),        // L2
            new R(12.5f, 18, 15.5f, 39),        // L3
            new R(-15.5f, 36, 15.5f, 39),       // L4
            new R(-15.5f, 36, -12.5f, 57),      // L5
            new R(-15.5f, 54, 1.5f, 57),        // L6
            new R(-1.5f, 54, 1.5f, 72),         // L7
            pen1, pen2,
            new R(-4.5f, 13, -1.5f, 16),        // hiding niches
            new R(15.5f, 26, 18.5f, 29),
            new R(-18.5f, 44, -15.5f, 47),
            new R(-9, 57, -6, 60),
            new R(1.5f, 62, 9, 65),             // rooftop descent ramp notch
        };
        const float roof = 3.6f;
        k.CarveBuildings(Floor, walk, r => roof, 9);

        // Rooftop shortcut: up the ramp from the yard, over the eastern roofs, down the notch near the exit
        k.Ramp(V3(-1f, 0f, 8f), V3(6f, roof, 8f), 3f, k.M.woodDark, "RoofRampUp");
        k.Ramp(V3(1.5f, 0f, 63.5f), V3(9f, roof, 63.5f), 3f, k.M.woodDark, "RoofRampDown");
        // chimneys = cover on the roofs
        foreach (var c in new[] { V(18, 26), V(18, 44), V(9, 50), V(14, 58), V(19, 8) })
            k.Block(new R(c.x - 0.9f, c.y - 0.9f, c.x + 0.9f, c.y + 0.9f), 1.8f, k.M.roofTile, GameLayers.Walls, roof, "Chimney");

        // ── dressing ──
        k.PlaceTorch(-4, 8); k.PlaceTorch(0.7f, 20.3f); k.PlaceTorch(13.9f, 28, true);
        k.PlaceTorch(-13.9f, 46); k.PlaceTorch(-0.8f, 60); k.PlaceTorch(9, 37.6f);
        k.DogPen(pen1, "DogPen_A");
        k.DogPen(pen2, "DogPen_B");
        k.Pot(7, 22.6f, 2); k.Pot(-6, 40.6f, 2);         // bait inside the pens
        k.Pot(-5, 2); k.Pot(19.5f, 30, 1, roof);          // roof pot rewards the risky route
        k.Pot(20, 20, 1, roof);
        k.Pot(-14, 50); k.Pot(13.9f, 32);
        k.Scroll(-5.4f, 2.4f, story.lorePotter);
        foreach (var c in new[] { V(1.0f, 16), V(15.15f, 24), V(-13f, 50) })
            k.LowCover(c.x, c.y, 0.7f, 0.7f, 1.0f, k.M.clay, "Kiln");     // kilns: clutter tucked against the alley walls

        foreach (var h in new[] { V(-3, 14.5f), V(17, 27.5f), V(-17, 45.5f), V(-7.5f, 58.5f) }) k.HidingSpot(h.x, h.y, 3f, 3f);

        // ── guards ──
        // The alley is narrow and the dog pens sit beside its route, so nobody patrols the route itself: the commander keeps
        // to the west plaza, a slow patrol walks the west lane and two lookouts watch the open stretches.
        k.Guard(GuardType.Patrol,   V3(0, 0, 0), 0, new[] { V(-16, 22), V(-16, 34) }, pingPong: true, tweak: f => { f.patrolSpeed = 1.6f; f.waypointPause = 3f; });
        k.Guard(GuardType.Commander, V3(0, 0, 0), 0, new[] { V(-14, 62), V(-6, 62) }, pingPong: true, tweak: f => f.waypointPause = 3f);
        k.Guard(GuardType.Sentry, V3(-9, 0, 26), 90f, tweak: f => { f.GetComponent<GuardVision>().maxRange = 12f; f.sentryArcDegrees = 90f; });
        k.Guard(GuardType.Sentry, V3(9, 0, 44), 270f, tweak: f => { f.GetComponent<GuardVision>().maxRange = 12f; f.sentryArcDegrees = 90f; });
        // rooftop lookout over the shortcut
        k.Guard(GuardType.Sentry, V3(4, roof, 46), 90f, tweak: f =>
        {
            f.GetComponent<GuardVision>().maxRange = 16f;
            f.sentryArcDegrees = 140f;
        });

        // ── townsfolk ──
        k.Npc(3, V3(11, 0, 20.4f), 270f);          // potter at his kiln
        k.Npc(1, V3(0, 0, 0), 0, path: new[] { V(3, 37.5f), V(8, 37.5f) });                // jar carrier on the patrol line
        k.Npc(2, V3(-14.4f, 0, 52), 0f);
        k.NpcPair(3, V(-4.5f, 4), 0, V(-3, 4.7f));

        // ── zone plumbing ──
        k.Exit(0, 69.2f, 3f);
        k.Entry("Potter's Alley", "Zone 3", V(0, 3), 0f,
            new[] { V(0, 15), V(7, 19.5f), V(14, 30), V(0, 37.5f), V(-14, 45), V(-8, 55.5f), V(0, 62) }, difficulty: 0.90f, ambient: 0.40f);
        k.Ambience(new[] { V3(-27, 3, 20), V3(27, 3, 35), V3(-27, 3, 55), V3(0, 3, 80) });
        k.BakeNavMesh();
        k.VerifyRoute("alley snake", V(0, 3), V(0, 15), V(7, 19.5f), V(14, 30), V(0, 37.5f), V(-14, 45), V(-8, 55.5f), V(0, 62), V(0, 68));
        k.VerifyRoute3D("rooftop shortcut", V3(0, 0.1f, 8f), V3(10, roof + 0.1f, 8.5f), V3(18, roof + 0.1f, 20), V3(19, roof + 0.1f, 45), V3(12, roof + 0.1f, 58), V3(6, 1.5f, 63.5f), V3(0, 0.1f, 64), V3(0, 0.1f, 68));
        k.Save();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ZONE 4 — WELL SQUARE
    // Rooftop sentries with long sight lines. Roofed houses (interiors) or the raised terrace behind its parapet.
    // ═══════════════════════════════════════════════════════════════════════
    public static void Zone4(ZoneKit k, StoryAssets story)
    {
        Common(k, Floor);
        var walk = new List<R>
        {
            new R(-4, 0, 4, 14),        // entry street
            new R(-20, 14, 20, 54),     // the square
            new R(-4, 54, 4, 72),       // exit street
        };
        k.CarveBuildings(Floor, walk, r => 4.5f, 13);
        k.Patch(new R(-20, 14, 20, 54), k.M.dirt, GameLayers.GroundDirt);

        // West gallery: a long roofed building with rooms (an interior route safe from rooftop sentries)
        const float bh = 4.2f;
        k.Hollow(new R(-20, 19, -10, 49), bh, 0.6f, new[]
        {
            new Door('S', -15, 3f), new Door('N', -15, 3f),
            new Door('E', 24, 2.4f), new Door('E', 44, 2.4f),
        }, k.M.sandC);
        k.WallLine('x', 29, -19.4f, -10.6f, 0.5f, bh, new[] { new Door('x', -15, 3f), new Door('x', -12, 1.6f) }, k.M.sandC);
        k.WallLine('x', 39, -19.4f, -10.6f, 0.5f, bh, new[] { new Door('x', -15, 3f), new Door('x', -18, 1.6f) }, k.M.sandC);
        k.Patch(new R(-19.4f, 19.6f, -10.6f, 48.4f), k.M.woodFloor, GameLayers.GroundWood);
        k.HidingSpot(-15, 24, 3f, 3f);
        k.HidingSpot(-15, 44, 3f, 3f);
        k.PlaceTorch(-17, 21); k.PlaceTorch(-13, 34); k.PlaceTorch(-17, 46);

        // Centre-north house
        k.Hollow(new R(-6, 44, 6, 52), bh, 0.6f, new[] { new Door('S', 0, 3f), new Door('N', 0, 3f) }, k.M.sandA);
        k.Patch(new R(-5.4f, 44.6f, 5.4f, 51.4f), k.M.woodFloor, GameLayers.GroundWood);
        k.HidingSpot(0, 48, 3f, 3f);

        // East terrace: raised 1.2 m, low parapet along its open edge — elevation difference for crouch routes
        k.Block(new R(12, 20, 20, 47), 1.2f, k.M.limestone, GameLayers.Walls, 0f, "Terrace");
        k.Block(new R(12, 22, 12.5f, 45), 1.1f, k.M.limestone, GameLayers.Walls, 1.2f, "Parapet");
        k.Ramp(V3(16, 0, 14.6f), V3(16, 1.2f, 20), 3.5f, k.M.limestone, "TerraceRampS");
        k.Ramp(V3(16, 1.2f, 47), V3(16, 0, 51.5f), 3.5f, k.M.limestone, "TerraceRampN");

        // Square dressing
        k.Well(0, 34);
        k.LowCover(3, 22, 1.6f, 1.4f, 1.1f, k.M.crateWood, "Crate");
        k.LowCover(-3.5f, 31, 1.4f, 1.4f, 1.1f, k.M.crateWood, "Crate");
        k.LowCover(4, 38, 1.4f, 1.4f, 1.1f, k.M.crateWood, "Crate");
        k.LowCover(9.5f, 46, 1.4f, 1.4f, 1.1f, k.M.crateWood, "Crate");
        k.LowCover(-7.5f, 50.5f, 1.4f, 1.4f, 1.1f, k.M.crateWood, "Crate");
        k.PlaceTorch(-8, 16); k.PlaceTorch(8, 16); k.PlaceTorch(-8.5f, 34, true); k.PlaceTorch(9, 30); k.PlaceTorch(9, 42, true);
        k.PlaceTorch(-3, 53); k.PlaceTorch(3, 53);
        k.Alcove(9, 49.5f, 'E');

        k.Pot(-3, 16); k.Pot(-18, 52.6f); k.Pot(19, 30, 1, 1.2f); k.Pot(0, 49.5f); k.Pot(-14, 34, 2); k.Pot(6, 20);
        k.Scroll(0.6f, 34.6f, story.loreWell);

        // ── guards: three rooftop sentries + a well patrol + a commander on the north strip ──
        k.Guard(GuardType.Sentry, V3(-15, bh + 0.4f, 34), 90f, tweak: f => f.GetComponent<GuardVision>().maxRange = 14f);
        k.Guard(GuardType.Sentry, V3(0, bh + 0.4f, 48), 180f, tweak: f => f.GetComponent<GuardVision>().maxRange = 14f);
        k.Guard(GuardType.Sentry, V3(14, 4.5f, 56), 200f, tweak: f => f.GetComponent<GuardVision>().maxRange = 14f);
        k.Guard(GuardType.Patrol, V3(0, 0, 0), 0, new[] { V(-7, 26), V(7, 26), V(7, 41), V(-7, 41) });
        k.Guard(GuardType.Patrol, V3(0, 0, 0), 0, new[] { V(3, 24), V(8, 46) }, pingPong: true, tweak: f => f.patrolSpeed = 1.8f);
        k.Guard(GuardType.Commander, V3(0, 0, 0), 0, new[] { V(-17.5f, 53), V(17.5f, 53) }, pingPong: true);

        // ── townsfolk, greenery, banners ──
        k.Npc(0, V3(-3.4f, 0, 33), 90f);           // gathered around the well
        k.Npc(4, V3(3.6f, 0, 35), 270f);
        k.Npc(2, V3(0.4f, 0, 37.4f), 180f);
        k.Npc(1, V3(0, 0, 0), 0, path: new[] { V(-5.7f, 29), V(-5.7f, 39) });               // water carrier
        k.Npc(5, V3(-18.4f, 0, 25.6f), 90f);       // inside the gallery
        k.Npc(3, V3(-8.6f, 0, 23), 270f);
        k.Npc(2, V3(18, 1.2f, 36), 270f);          // on the terrace
        k.Tree(-8.5f, 20); k.Tree(8.5f, 20); k.Tree(11, 36.5f); k.Tree(-9.5f, 46.5f);
        k.Banner(-3.6f, 43.9f, Vector3.back, 3.9f, 0.9f, 2.4f); k.Banner(3.6f, 43.9f, Vector3.back, 3.9f, 0.9f, 2.4f);
        k.Fireflies(new R(-19, 15, 19, 53), 30);

        // ── zone plumbing ──
        k.Exit(0, 69.5f, 7f);
        k.Entry("Well Square", "Zone 4", V(0, 3), 0f,
            new[] { V(0, 10), V(-15, 22), V(-15, 45), V(16, 26), V(16, 44), V(-12, 52.8f), V(0, 60) }, difficulty: 1.00f, ambient: 0.46f);
        k.Ambience(new[] { V3(-27, 3, 25), V3(27, 3, 40), V3(0, 3, 80) });
        k.BakeNavMesh();
        k.VerifyRoute("west gallery", V(0, 3), V(0, 12), V(-15, 17), V(-15, 25), V(-15, 34), V(-15, 45), V(-15, 52.5f), V(0, 53), V(0, 68));
        k.VerifyRoute3D("east terrace", V3(0, 0.1f, 12), V3(16, 0.1f, 16.5f), V3(16, 1.3f, 25), V3(16, 1.3f, 45), V3(16, 0.1f, 51.2f), V3(0, 0.1f, 53), V3(0, 0.1f, 68));
        k.VerifyRoute("open square", V(0, 3), V(0, 12), V(0, 24), V(6, 34), V(9, 50), V(0, 53), V(0, 68));
        k.Save();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ZONE 5 — THE EASTERN GATE (scripted finale)
    // A stealth approach through the barracks yard, then the gate: no stealth solution — the signet-ring bluff.
    // ═══════════════════════════════════════════════════════════════════════
    public static void Zone5(ZoneKit k, StoryAssets story)
    {
        var floor = new R(-22, 0, 22, 92);
        Common(k, floor);
        var walk = new List<R>
        {
            new R(-6, 0, 6, 20),            // entry avenue
            new R(-22, 20, 22, 48),         // barracks yard
            new R(-10, 48, 10, 62),         // gate approach
            new R(-4, 62, 4, 64),           // gate opening
            new R(-12, 64, 12, 92),         // the road into the hills
        };
        k.CarveBuildings(floor, walk, r => (r.z0 >= 61.9f && r.z1 <= 64.1f) ? 7f : 4.5f, 21);
        k.Patch(new R(-22, 20, 22, 48), k.M.dirt, GameLayers.GroundDirt);
        k.Patch(new R(-12, 64, 12, 92), k.M.dirt, GameLayers.GroundDirt);

        // barracks
        k.Block(new R(-20, 26, -12, 40), 4f, k.M.sandB, GameLayers.Walls, 0, "Barracks");
        k.Block(new R(12, 26, 20, 40), 4f, k.M.sandC, GameLayers.Walls, 0, "Barracks");

        // ── yard dressing ──
        k.FirePit(0, 34);
        k.PlaceTorch(-4, 8); k.PlaceTorch(4, 8); k.PlaceTorch(-5.2f, 19); k.PlaceTorch(5.2f, 19, true);
        k.PlaceTorch(-10, 24); k.PlaceTorch(10, 24); k.PlaceTorch(-10, 44, true); k.PlaceTorch(10, 44);
        k.PlaceTorch(-20, 46); k.PlaceTorch(20, 46);
        k.Cart(-6, 25); k.Cart(6, 37); k.Cart(-14, 44); k.Cart(15, 22, false);
        k.LowCover(-3, 12, 1.4f, 1.4f, 1.1f, k.M.crateWood, "Crate");
        k.LowCover(3, 16, 1.4f, 1.4f, 1.1f, k.M.crateWood, "Crate");
        k.LowCover(-8, 36, 1.2f, 3f, 1.1f, k.M.crateWood, "Crate");
        k.LowCover(8, 32, 1.2f, 3f, 1.1f, k.M.crateWood, "Crate");
        k.Pillar(-4, 40, 4f); k.Pillar(4, 28, 4f); k.Pillar(-4, 28, 4f); k.Pillar(4, 40, 4f);
        k.LowCover(-6, 53, 3f, 1.2f, 1.1f, k.M.clothOchre, "Planter");
        k.LowCover(6, 55, 3f, 1.2f, 1.1f, k.M.clothOchre, "Planter");

        k.Alcove(-20, 22, 'E');
        k.Alcove(20, 44, 'W');
        k.Alcove(-8.4f, 58, 'E');
        k.HidingSpot(3, 5, 2.6f, 2.6f);

        k.Pot(5, 3); k.Pot(-21, 24, 2); k.Pot(-14, 34); k.Pot(9, 46); k.Pot(-8.4f, 58, 2); k.Pot(19, 20);

        // ── guards ──
        k.Guard(GuardType.Patrol,   V3(0, 0, 0), 0, new[] { V(-10, 23), V(10, 23), V(10, 44), V(-10, 44) });
        k.Guard(GuardType.Patrol,   V3(0, 0, 0), 0, new[] { V(-19, 47), V(19, 47) }, pingPong: true, tweak: f => { f.patrolSpeed = 1.5f; f.waypointPause = 3f; });
        k.Guard(GuardType.Commander, V3(0, 0, 0), 0, new[] { V(-8, 51.5f), V(8, 51.5f) }, pingPong: true, tweak: f => { f.patrolSpeed = 1.4f; f.waypointPause = 3f; });
        k.Guard(GuardType.Sentry, V3(-16, 0, 21.5f), 20f);
        k.Guard(GuardType.Sentry, V3(9, 0, 30), 270f, tweak: f => { f.GetComponent<GuardVision>().maxRange = 13f; f.sentryArcDegrees = 90f; });
        k.Guard(GuardType.Patrol, V3(0, 0, 0), 0, new[] { V(-16, 26), V(-16, 44) }, pingPong: true, tweak: f => f.patrolSpeed = 1.8f);
        k.Guard(GuardType.Patrol, V3(0, 0, 0), 0, new[] { V(14, 24), V(14, 44) }, pingPong: true, tweak: f => f.patrolSpeed = 1.8f);

        // ── the gate ──
        var pal = k.M.palace;
        var gateLeft  = k.Cube("GateDoorL", k.envRoot, V3(-2, 2.2f, 62.6f), new Vector3(4f, 4.4f, 0.8f), k.M.woodDark, GameLayers.Walls);
        var gateRight = k.Cube("GateDoorR", k.envRoot, V3(2, 2.2f, 62.6f), new Vector3(4f, 4.4f, 0.8f), k.M.woodDark, GameLayers.Walls);
        gateLeft.isStatic = false; gateRight.isStatic = false;
        foreach (var d in new[] { gateLeft, gateRight })
            for (int i = 0; i < 3; i++)
                k.Cube("IronBand", d.transform, V3(d.transform.position.x, 0.9f + i * 1.3f, 62.15f), new Vector3(3.9f, 0.14f, 0.12f), k.M.metal, 0, false).transform.SetParent(d.transform, true);
        k.Block(new R(-4, 62, 4, 64), 2.6f, pal, GameLayers.Walls, 4.4f, "GateLintel");
        k.PlaceTorch(-5.5f, 60.2f, true); k.PlaceTorch(5.5f, 60.2f, true);

        // NPCs at the gate (no AI — the encounter is scripted)
        var npcPrefab = k.P.gateNpc;
        GameObject Npc(float x, float z, string name)
        {
            var go = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(npcPrefab, k.scene);
            go.name = name;
            go.transform.SetParent(k.guardsRoot, false);
            go.transform.SetPositionAndRotation(V3(x, 0, z), Quaternion.Euler(0, 180, 0));
            return go;
        }
        var commander = Npc(0, 60.6f, "GateCommander");
        Npc(-5.5f, 61f, "GateGuardL");
        Npc(5.5f, 61f, "GateGuardR");

        // finale controller
        var finale = new GameObject("GateFinale");
        finale.transform.SetParent(k.triggersRoot, false);
        finale.transform.position = V3(0, 1.5f, 57.5f);
        var box = finale.AddComponent<BoxCollider>();
        box.isTrigger = true; box.size = new Vector3(12, 3, 7);
        var seq = finale.AddComponent<GateFinalSequence>();

        Transform Mark(string name, Vector3 pos, float yaw)
        {
            var t = new GameObject(name).transform;
            t.SetParent(finale.transform, false);
            t.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0));
            return t;
        }
        Transform Shot(string name, Vector3 pos) { var t = Mark(name, pos, 0); return t; }

        seq.jonathanMark = Mark("JonathanMark", V3(-1.3f, 0.05f, 57.6f), 0f);
        seq.davidMark    = Mark("DavidMark",    V3(1.4f, 0.05f, 57.0f), 0f);
        seq.commander    = commander.transform;
        seq.gateLeft = gateLeft.transform;
        seq.gateRight = gateRight.transform;
        seq.gateOpenDistance = 3.6f;
        seq.davidExitPath = new[]
        {
            Mark("ExitPath0", V3(0, 0.05f, 63.5f), 0), Mark("ExitPath1", V3(0, 0.05f, 68), 0),
            Mark("ExitPath2", V3(1.5f, 0.05f, 78), 0),  Mark("ExitPath3", V3(0, 0.05f, 91), 0),
        };
        seq.lookAtGate = Mark("LookAtGate", V3(0, 2.6f, 63), 0);
        seq.shotBluff     = Shot("ShotBluff",     V3(-4.2f, 1.9f, 53.8f));
        seq.shotFarewell  = Shot("ShotFarewell",  V3(-2.4f, 1.75f, 58.4f));
        seq.shotEmptyGate = Shot("ShotEmptyGate", V3(0, 2.1f, 55.5f));
        seq.shotBluff.LookAt(V3(0, 1.6f, 60.6f));
        seq.shotEmptyGate.LookAt(seq.lookAtGate);
        seq.gateBluffBeats = story.gateBluff;
        seq.farewellBeats = story.farewell;
        seq.epilogueBeats = story.epilogue;
        seq.sfxSource = PrefabFactory.AddSource(finale, null, k.MX.sfx, false, false, 1f, 60f);
        seq.gateCreak = k.A.creak;
        // the commander steps aside toward the tower
        seq.commanderStepAside = new Vector3(-3.2f, 0, 0);

        // ── townsfolk, greenery, banners ──
        k.Npc(2, V3(1.9f, 0, 34.7f), 270f);        // cook at the fire pit
        k.Npc(4, V3(-8.4f, 0, 54.5f), 0f);         // townsfolk waiting at the gate
        k.Npc(0, V3(8.2f, 0, 55.6f), 0f);
        k.Npc(3, V3(6.6f, 0, 53.4f), 20f);
        foreach (var t in new[] { V(-9, 60), V(9, 60), V(-9, 68), V(9, 70), V(-9, 76), V(9, 79), V(-9, 86), V(9, 88) }) k.Tree(t.x, t.y, true);
        k.Banner(-7, 61.9f, Vector3.back, 6.4f, 1.3f, 3.6f); k.Banner(7, 61.9f, Vector3.back, 6.4f, 1.3f, 3.6f);
        k.Fireflies(new R(-10, 66, 10, 90), 40);

        // ── zone plumbing ──
        k.Entry("The Eastern Gate", "Zone 5", V(0, 3), 0f,
            new[] { V(0, 14), V(-14, 30), V(14, 30), V(0, 46), V(-9, 55), V(9, 55) }, difficulty: 1.10f, ambient: 0.52f);
        k.Ambience(new[] { V3(-27, 3, 25), V3(27, 3, 40), V3(-10, 3, 100), V3(10, 3, 100) });
        k.BakeNavMesh();
        k.VerifyRoute("avenue to gate", V(0, 3), V(0, 18), V(0, 30), V(0, 47), V(0, 56));
        k.VerifyRoute("yard flank", V(0, 3), V(0, 19), V(-16, 24), V(-16, 46), V(-8, 50), V(0, 56));
        k.Save();
    }
}
