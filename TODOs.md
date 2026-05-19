# Covenant Night — TODO List

## Code Fixes

- [ ] `ZoneManager`: call `david.ResetForZone()`, `abilities.ResetForZone()`, and `HUD.ResetForZone()` after restoring a checkpoint
- [ ] `InputReader`: `HarpPressed` and `InteractPressed` are both mapped to `_interact` — separate harp onto a different action (e.g. repurpose `Previous` or add a new binding)
- [ ] `ZoneManager`: wire in `StoryPanelController.Show()` calls between zone transitions — needs a story beats data structure per zone
- [ ] All scripts: add `Animator` parameter updates once character rigs are imported (speed, isCrouching, isAlerted floats/bools)
- [ ] `HidingSpot`: `Time.timeScale = 0` pauses everything including the alarm countdown — replace with a flag that `AlarmSystem` checks instead of using timeScale

---

## Project Settings (Unity Editor)

- [ ] Add tags: `Player`, `David`
- [ ] Add layers: `Characters`, `Walls`
- [ ] Assign `Characters` layer to Jonathan and David GameObjects/prefabs
- [ ] Assign `Walls` layer to all environment geometry
- [ ] Set Physics layer collision matrix so `Characters` does not collide with itself

---

## Scenes

- [ ] Create `Persistent` scene — managers, UI, camera (loaded first, never unloaded)
- [ ] Create `Zone1_PalaceDistrict` scene
- [ ] Create `Zone2_MarketQuarter` scene
- [ ] Create `Zone3_PottersAlley` scene
- [ ] Create `Zone4_WellSquare` scene
- [ ] Create `Zone5_EasternGate` scene
- [ ] Add all six scenes to **File → Build Settings** — Persistent first, then zones in order
- [ ] Delete `SampleScene` (URP template leftover)
- [ ] Set `ZoneManager.zoneSceneNames[]` to match zone scene file names exactly

---

## Persistent Scene Setup

- [ ] Create `GameManager` GameObject — attach `GameManager.cs` + `FailStateHandler.cs`
- [ ] Create `AlarmSystem` GameObject — attach `AlarmSystem.cs`
- [ ] Create `ZoneManager` GameObject — attach `ZoneManager.cs`; assign all Inspector fields
- [ ] Create `InputReader` GameObject — attach `InputReader.cs`; drag `InputSystem_Actions.inputactions` onto `actionsAsset` field
- [ ] Set up **Cinemachine** Virtual Camera with third-person rig targeting Jonathan's camera target bone
- [ ] Create full-screen **Fade Panel** (black Image, CanvasGroup) — assign to `ZoneManager.fadePanel`
- [ ] Create **Fail Panel** UI (text: "Caught") — assign to `FailStateHandler.failPanel`
- [ ] Create **Win Panel** UI (epilogue text) — assign to `FailStateHandler.winPanel`
- [ ] Create **HUD** canvas — attach `HUD.cs`; wire stone count text, David mode text, alarm timer, harp indicator, zone text
- [ ] Create **StoryPanel** canvas — attach `StoryPanelController.cs`; wire panel group, body text, continue prompt, illustration image
- [ ] Create **ZoneNameCard** canvas — attach `ZoneNameCard.cs`; wire card group and name text

---

## Prefabs

### Jonathan
- [ ] Mesh + CharacterController (height 1.8, radius 0.35)
- [ ] Attach `PlayerController.cs` — assign camera target bone, footstep AudioSource
- [ ] Attach `PlayerAbilities.cs` — assign stone prefab, throw origin, wall layer mask, David reference
- [ ] Tag as `Player`; set layer to `Characters`
- [ ] Create child `CameraTarget` empty Transform at head height

### David
- [ ] Mesh + NavMeshAgent
- [ ] Attach `DavidCompanion.cs` — assign Jonathan transform as `followTarget`
- [ ] Tag as `David`; set layer to `Characters`

### Guard_Patrol
- [ ] Mesh + NavMeshAgent
- [ ] Attach `GuardFSM.cs` (GuardType = Patrol) + `GuardVision.cs` + `GuardHearing.cs` + `DetectionIndicator.cs`
- [ ] Create child `EyePoint` empty Transform at eye height; assign to `GuardVision.eyePoint`
- [ ] Set `obstacleMask` = Walls; `targetMask` = Characters on `GuardVision`
- [ ] Attach a point Light to guard; assign to `DetectionIndicator.indicatorLight`

### Guard_Sentry
- [ ] Same as Patrol prefab with `GuardType = Sentry`; NavMeshAgent can have zero speed

### Guard_Commander
- [ ] Same as Patrol prefab with `GuardType = Commander`; set higher speed and wider vision cone

### Stone
- [ ] Sphere mesh + Rigidbody + SphereCollider + `Stone.cs`
- [ ] Assign to `PlayerAbilities.stonePrefab`

### StonePickup
- [ ] Small mesh (clay pot) + Trigger Collider + `StonePickup.cs`

### ZoneExit
- [ ] Empty GameObject + Trigger Collider (box) + `ZoneExit.cs`

### HidingSpot
- [ ] Empty GameObject + Trigger Collider + `HidingSpot.cs`

### PatrolPath
- [ ] Empty GameObject + `PatrolPath.cs`; add child Transforms as waypoints; assign to `PatrolPath.waypoints[]`

---

## Per-Zone Scene Setup (repeat for all 5 zones)

- [ ] Block out environment with ProBuilder or imported modular mesh kit
- [ ] Assign `Walls` layer to all static geometry
- [ ] Bake NavMesh (**Window → AI → Navigation → Bake**) after placing all geometry
- [ ] Place Jonathan and David spawn positions (or set in `CheckpointData`)
- [ ] Place guard prefab instances; assign `PatrolPath` to each Patrol/Commander guard
- [ ] Place `ZoneExit` trigger at zone end
- [ ] Place at least two `HidingSpot` triggers
- [ ] Place `StonePickup` props in pots/baskets
- [ ] Set `DavidCompanion.runWaypoint` to a mid-zone safe waypoint
- [ ] Create a `CheckpointData` asset in `ScriptableObjects/Checkpoints/`; assign to `ZoneManager.checkpoint`
- [ ] Set up torch point lights (warm amber, range ~8, intensity ~2)
- [ ] Set up moonlight directional light (cool white, low intensity, shadows on)

### Zone 5 only
- [ ] Add gate finale scripted sequence trigger (calls `GameManager.TriggerWin()` after cutscene)

---

## Asset Sourcing

- [ ] Download low-poly environment kit from Unity Asset Store or Quaternius (quaternius.com)
- [ ] Download character models (Quaternius Ultimate Character pack or similar)
- [ ] Download Mixamo animations: idle, walk, run, crouch-walk, alert-idle for Jonathan, David, and guards
- [ ] Download ambient SFX from freesound.org: night wind, dog barks, torch crackle, sandal footsteps on stone
- [ ] Download stealth/ambient music from OpenGameArt.org
- [ ] Set up Mixamo workflow: import FBX with skin → set avatar to Humanoid → extract animation clips to `Animations/Clips/`

---

## Audio Setup

- [ ] Create AudioMixer with three submixes: Music, Ambience, SFX
- [ ] Create two AudioMixer Snapshots: Calm, Alarmed (boost tension drone in Alarmed)
- [ ] Wire AudioMixer to `AlarmSystem` — call snapshot transition on alarm raised/cleared
- [ ] Add spatial AudioSources (Spatial Blend = 1.0) to all torch objects
- [ ] Add footstep AudioSource to Jonathan prefab; assign SFX clips and AudioMixer group
- [ ] Add harp AudioSource to David prefab; trigger on harp ability use

---

## Polish (Day 5)

- [ ] URP Post Process Volume: Bloom + Vignette on Persistent camera
- [ ] Add vignette pulse on alarm raised (animate Vignette intensity via script)
- [ ] Tune guard speeds, vision ranges, and patrol gaps across all zones
- [ ] Tune stone count and pickup placement
- [ ] Confirm WebGL build exports and runs in browser
- [ ] Write CREDITS.txt listing all borrowed assets with source URLs and licenses
- [ ] Set up itch.io page: title, description, screenshots, CREDITS.txt upload
