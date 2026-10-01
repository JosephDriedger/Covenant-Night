# Covenant Night

A third-person 3D stealth game of loyalty, shadow, and sacrifice — a one-person summer project built in Unity.

**Scriptural Basis:** 1 Samuel 19–20

---

## Overview

Play as **Jonathan**, son of King Saul, and escort your closest friend **David** through the winding streets of ancient Gibeah before your father's guards close every exit. Hug walls, throw stones, command your companion, and navigate a city that knows your face. Every shadow is cover. Every patrol route is a puzzle. Every choice carries the weight of a covenant sworn before God.

No combat. Movement, patience, and loyalty are the only tools.

---

## Gameplay

Jonathan navigates five hand-crafted city zones, each a self-contained 3D stealth puzzle. David follows as an NPC companion — if a guard gets a confirmed line-of-sight on him, it's over.

**Jonathan's Abilities**
- **Crouch / Slow Move** — halves speed, drops noise to near zero, lets you slip behind low cover
- **Distraction Throw** — toss a stone to pull guards away; 3 stones per zone (more in clay pots)
- **Decoy** — drop a pouch that whistles every couple of seconds for 8 seconds, holding a guard's attention on the spot instead of a single clatter; 1 per zone
- **Shadow Step** — press flat against a wall to become nearly invisible, even inside a guard's detection cone

**David's Abilities**
- **Follow / Wait / Run** — toggle David's NavMesh behaviour to coordinate movement through tight spots
- **Hush** *(6 seconds)* — orders David to crouch and stay quiet, shrinking guards' effective sight range on him and silencing his footsteps while running
- **Harp Calm** *(once per zone)* — de-escalates all nearby Suspicious guards who don't have direct sight of David

**Guard Types**

| Type | Behaviour |
|------|-----------|
| Patrol Guard | Fixed waypoint loop, predictable if observed |
| Sentry | Stationary, rotating arc, widest vision cone — guards exits and choke points |
| Commander | Patrols but breaks to investigate audio; triggers instant alarm on confirmed sighting |

**Detection States**
- **Unaware** (green cone) — guard follows routine
- **Suspicious** (amber) — investigating; break line-of-sight and use Shadow Step to reset
- **Alarmed** (red) — confirmed sighting; every guard converges, the zone exit locks, and you have 30 seconds to reach a hiding spot or the zone resets

Detection is graded: an awareness meter fills faster when a guard is close, when you are sprinting, or when you stand in torchlight, and drains when you break line of sight. David is different — a confirmed sighting of him is an immediate alarm.

**Townsfolk**

Merchants, shoppers and villagers fill every zone. They are solid (Jonathan cannot walk through them), guards have to path around them, and a crowd between you and a guard blocks its line of sight. They flinch at the alarm and turn toward noises.

---

## Zones

Gibeah is divided into five linearly connected zones, each with at least two routes through.

| Zone | Name | Key Challenge |
|------|------|---------------|
| 1 | Palace District | Dense guards who recognise Jonathan's face; use servant corridors |
| 2 | Market Quarter | Open plaza with sparse cover; use stalls, carts, and torch shadow gaps |
| 3 | Potter's Alley | Narrow corridors; rooftop shortcut available but exposed; avoid dog pens |
| 4 | Well Square | Rooftop sentries with long sight lines; use building interiors and elevation |
| 5 | Eastern Gate | Scripted finale: Jonathan bluffs the gate commander with his signet ring |

---

## Technical Stack

- **Engine:** Unity 3D (URP)
- **Language:** C#
- **Camera:** custom over-the-shoulder third-person rig with wall collision (no Cinemachine dependency)
- **Movement:** Unity `CharacterController`
- **Guard AI:** C# Finite State Machine (Patrol / Suspicious / Alarmed)
- **Line-of-Sight:** `Physics.Raycast` from guard eye position, checked every 0.1s
- **Companion AI:** `NavMeshAgent` with Follow / Wait / Run modes
- **Scene Loading:** Additive scene loading per zone (`LoadSceneMode.Additive`)
- **Audio:** `AudioMixer` with Music / Ambience / SFX submixes; spatial blend 1.0 on all in-world sources
- **Detection Visualisation:** ground-projected cone mesh, clipped by walls and fading toward its far edge
- **Look:** flat-shaded low-poly characters and buildings, tiling procedural textures, torch/fire particles, lit windows, moon and stars, URP Bloom + Vignette
- **Cinematics:** the intro, each zone transition, and the ending play as staged scenes over the live 3D world — a cutscene camera shot (`ThirdPersonCamera.SetShot`) and character performance (procedural gestures) with the story text as an overlaid caption, instead of a static illustration. The intro opens in Saul's torchlit throne room (Saul rises from his throne and hurls his spear past David's head into the wall while David plays the harp), cuts to Jonathan's reaction, then Jonathan leads David out, both crouched and actually walking, to the zone's real starting point. Cutscenes use letterbox bars, a slightly longer lens and a soft camera-mounted key light, hide the gameplay HUD, and glide back to the play camera when they end. Story captions advance only on Space, Enter, a click or A/Cross, and each stays up for a minimum reading time
- **Wayfinding:** each zone ends at a wooden gate in a stone arch that swings open as Jonathan approaches (and stays shut during an alarm), with a pale shaft of light rising over the rooftops and a glowing threshold; the HUD marks the exit with its distance, pinned to the screen edge when it is out of view
- **Typography:** Cinzel for titles, headings and buttons, Cardo for story text (both SIL Open Font License; see `CREDITS.txt`)
- **Input:** new Input System — keyboard and mouse or gamepad, with on-screen hints that follow the device in use

---

## Controls

| Action | Keyboard / mouse | Gamepad |
|--------|------------------|---------|
| Move / look | WASD / mouse | Left stick / right stick |
| Sneak (silent) | Left Shift (hold) | B / Circle (hold) |
| Sprint (loud) | Left Ctrl or Cmd (hold) | L3 |
| Shadow Step | Space (hold, beside a wall) | A / Cross (hold) |
| Throw a stone | Left mouse | X / Square |
| Drop a decoy | G | Right Shoulder |
| David: Follow / Wait / Run | 1 / 2 / 3 | D-pad left / right / down |
| David: toggle Follow / Wait | E | Y / Triangle |
| Hush David | Q | Left Shoulder |
| Harp Calm | H | D-pad up |
| Pause menu (controls, settings, main menu, quit) | Esc | Start |

## Difficulty

Choose a night on the title screen. **Easy**, **Medium** and **Hard** are regular play: a capture restarts the current zone, and Easy gives slower, near-sighted guards with more stones and more time to hide, while Hard gives keener guards, fewer stones and less time. **Hardcore** is one life: any capture sends the whole run back to Zone 1, and the guards are faster, remember longer and check the hiding spots near where they lost you.

The game also ramps up zone by zone in every mode. Each zone has its own guard sharpness (0.70 in the first zone, rising to 1.10 in the last) and the moonlight grows brighter toward dawn, so guards see farther and notice faster as the night wears on. Guard counts rise from four to seven.

**New Game+.** Finishing the game once (on any difficulty) unlocks **Mixed** on the difficulty screen: instead of one difficulty for the whole run, choose Easy, Medium or Hard separately for each of the five zones. Hardcore stays a whole-run, one-life mode and isn't offered per zone.

Every zone was checked to be beatable in every mode by stealth alone, with David following and the dog pens counted, and a scripted playthrough reached the gate in Easy, Medium, Hard and Hardcore.

## Building and Platforms

Open `Covenant_Night` in Unity 6000.3.14f1. The scenes, prefabs, materials, audio and UI are produced by the editor builder in `Assets/Editor/Builder` (menu **Covenant Night > Build Everything**), so changes to layouts or art generators belong there; re-run it to regenerate the assets.

Windows, macOS and Linux builds are under **Covenant Night > Build ... Player**. The Windows player is built and verified end to end. Console notes and requirements are in [docs/PLATFORMS.md](docs/PLATFORMS.md).

## Screenshots

Renders of each zone are in [docs/screenshots](docs/screenshots).

## Project Structure

```
Covenant_Night/Assets/
├── Scenes/         Persistent + Zone1–Zone5 (each zone has its own baked NavMesh)
├── Scripts/        Player, Guards, Companion, Systems, UI, Props, Data
├── Editor/Builder/ Generators for scenes, prefabs, audio, art and UI
├── Prefabs/  Materials/  Audio/  Art/  ScriptableObjects/
docs/               Game design document, platform notes, screenshots
```

---

## About

A one-person summer project: design, code, level building, audio and art are all done by a single developer. The full design lives in `docs/GameDesignDocument.docx`; the scope cuts listed there (no inventory, no branching dialogue, no full save/load) still apply.

---

## Success Criteria

**MVP**
- Jonathan can move, crouch, and throw stones in a 3D environment
- At least one guard with working 3D line-of-sight and patrol loop
- David follows and can be commanded to wait
- Zone 1 → Zone 5 navigation with working fail state and win state
- At least one story text panel displays

**Full Success**
- All 5 zones with distinct layouts, guard placements, and branching paths
- All Jonathan abilities functional
- David harp ability implemented
- Story panels for all beats including epilogue
- Spatial audio, AudioMixer snapshots, and a heartbeat tension layer
- URP torch lighting and detection decals across all zones

---

## Inspiration

- *Splinter Cell: Chaos Theory* — dynamic lighting as a core stealth mechanic
- *Mark of the Ninja* — readable, telegraphed guard state design
- *Assassin's Creed Origins* — ancient Near Eastern environment reference
- *Hitman* (series) — guard social behaviour and bluff mechanics

---

## Credits

See `CREDITS.txt` for the full attribution list. All characters, environment, audio and story illustrations in the current build are original, procedurally created assets and can be swapped for hand-made ones at any time.
