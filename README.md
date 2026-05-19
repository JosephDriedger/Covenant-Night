# Covenant Night

A third-person 3D stealth game of loyalty, shadow, and sacrifice — built in Unity for a 5-day hackathon.

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
- **Shadow Step** — press flat against a wall to become nearly invisible, even inside a guard's detection cone

**David's Abilities**
- **Follow / Wait / Run** — toggle David's NavMesh behaviour to coordinate movement through tight spots
- **Harp Calm** *(once per zone)* — de-escalates all nearby Suspicious guards who don't have direct sight of David

**Guard Types**

| Type | Behaviour |
|------|-----------|
| Patrol Guard | Fixed waypoint loop, predictable if observed |
| Sentry | Stationary, rotating arc, widest vision cone — guards exits and choke points |
| Commander | Patrols but breaks to investigate audio; triggers instant alarm on confirmed sighting |

**Detection States**
- **Unaware** (green decal) — guard follows routine
- **Suspicious** (yellow) — investigating; break line-of-sight and use Shadow Step to reset
- **Alarmed** (red) — confirmed sighting; zone exit locks, 30 seconds to reach a hiding spot or the zone resets

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
- **Camera:** Cinemachine third-person rig
- **Movement:** Unity `CharacterController`
- **Guard AI:** C# Finite State Machine (Patrol / Suspicious / Alarmed)
- **Line-of-Sight:** `Physics.Raycast` from guard eye position, checked every 0.1s
- **Companion AI:** `NavMeshAgent` with Follow / Wait / Run modes
- **Scene Loading:** Additive scene loading per zone (`LoadSceneMode.Additive`)
- **Audio:** `AudioMixer` with Music / Ambience / SFX submixes; spatial blend 1.0 on all in-world sources
- **Detection Visualisation:** URP Decal Projector cone decals on the ground

---

## Project Structure

```
Covenant_Night/Assets/Scenes/
├── Persistent.unity          # Always-loaded manager scene
├── Zone1_PalaceDistrict.unity
├── Zone2_MarketQuarter.unity
├── Zone3_PottersAlley.unity
├── Zone4_WellSquare.unity
└── Zone5_EasternGate.unity
```

---

## Team

2-person team — 5-day jam.

- **Person 1 (Programmer):** Player controller, Cinemachine rig, Guard FSM, David NavMeshAgent, zone loading, checkpoint system, fail/win state flows, gate scripted sequence, detection decals
- **Person 2 (Designer/Artist):** 3D environment blockout and dressing for all 5 zones, asset sourcing, Animator Controllers, URP torch lighting, story panel UI, audio integration, itch.io submission

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
- Spatial audio, AudioMixer snapshots, and tension drone
- URP torch lighting and detection decals across all zones

---

## Inspiration

- *Splinter Cell: Chaos Theory* — dynamic lighting as a core stealth mechanic
- *Mark of the Ninja* — readable, telegraphed guard state design
- *Assassin's Creed Origins* — ancient Near Eastern environment reference
- *Hitman* (series) — guard social behaviour and bluff mechanics

---

## Credits

See `CREDITS.txt` in the build root for full asset attribution. Assets sourced from Unity Asset Store, Quaternius, Mixamo, freesound.org, and OpenGameArt.org.
