# Platforms

## Status

| Platform | Status |
|----------|--------|
| Windows (x64) | Built and played end to end (all five zones through the ending). |
| macOS | Supported by the project (URP + Metal, new Input System, no Windows-only code). Not built yet: needs the **Mac Build Support** module — Unity Hub > Installs > Add modules. Build with **Covenant Night > Build macOS Player**. |
| Linux (x64) | Same as macOS: install **Linux Build Support**, then **Covenant Night > Build Linux Player**. |
| PlayStation / Xbox / Nintendo Switch | Prepared, not built. Console builds require the platform holder's licensed SDK, a developer account, and the matching Unity module. See below. |

## What is already console-ready

- **Gamepad complete.** Every action is bound on a gamepad (see the Controls table in the README); nothing needs a mouse or keyboard. Menus and story panels advance with any button.
- **Device-aware hints.** The HUD switches between keyboard and gamepad prompts as soon as the player uses the other device.
- **Safe area.** HUD elements sit inside `SafeAreaFitter`, which honours the display safe area and keeps a 3.5% margin on console platforms for TV overscan.
- **Performance defaults.** `PlatformTuning` sets a 60 fps target, vsync and a shorter shadow distance on console and mobile platforms; desktop keeps the quality settings.
- **No PC-only APIs at runtime.** No Windows-specific calls, no reflection or editor code in the player, no file access; input reads keyboard, mouse and gamepad through the Input System with null checks so a missing device is fine.
- **Scripting backend.** Consoles require IL2CPP; the player code avoids anything that would not compile under it.

## Building for consoles

1. Get access to the target's developer program and install its SDK plus the Unity console module.
2. Open the project and switch the platform in **File > Build Profiles**.
3. Build with `Unity -batchmode -projectPath Covenant_Night -executeMethod CovenantNightBuilder.BuildTargetBatch -cnTarget <BuildTarget name>` (for example `PS5`, `GameCoreXboxSeries`, `Switch`), or use the Build Profiles window.
4. Expect a per-platform pass: performance (the torch lights are the main cost; keep shadows on the few key torches, and reduce the number of lit torches on Switch), controller glyphs for each console's button names, certification requirements (suspend/resume, user switching, achievements/trophies, age rating) and storage/save handling.

The game currently keeps progress in memory only (checkpoints inside a session), so there is no save data to certify.
