# Releasing

A release publishes two files to GitHub Releases:

| File | Platform | Made by |
|------|----------|---------|
| `CovenantNight-Setup.exe` | Windows (x64) | Inno Setup (`installer/CovenantNight.iss`) on a Windows runner |
| `CovenantNight-<version>-macOS.dmg` | macOS (Intel + Apple silicon) | `installer/mac/make-dmg.sh` on a macOS runner |

A `.dmg` can only be created on macOS and the Inno Setup installer only on Windows, so the
[release workflow](../.github/workflows/release.yml) builds both on GitHub's runners.

## One-time setup: Unity licence

The workflow builds the players with [game.ci](https://game.ci/docs/github/getting-started)'s Unity builder, which needs a Unity
licence stored as repository secrets (**Settings > Secrets and variables > Actions**):

- `UNITY_LICENSE`: the contents of the activated licence file (`.ulf`)
- `UNITY_EMAIL` and `UNITY_PASSWORD`: the Unity account the licence belongs to

Follow game.ci's [activation guide](https://game.ci/docs/github/activation) to get the licence file for a personal licence. If that
flow does not work for Unity 6 or your licence type, game.ci documents the alternatives (a serial for Pro/Plus licences).

## Cutting a release

```
git tag v1.0.0
git push origin v1.0.0
```

The workflow builds the Windows and macOS players, packages them, and attaches the installer and the disk image to a new release for the tag.
The tag (without the `v`) becomes the installer's version and part of the `.dmg` name.

To try the pipeline without publishing, run **Actions > Release > Run workflow**: it builds everything and keeps the two files as downloadable
run artifacts instead of making a release.

## Building locally

- **Windows installer:** see the README. Build the player, then run `ISCC.exe installer\CovenantNight.iss`.
- **macOS disk image:** needs a Mac. Build the player (Unity with Mac Build Support), then
  `bash installer/mac/make-dmg.sh <folder containing CovenantNight.app> CovenantNight.dmg`.

## macOS and Gatekeeper

The app is signed ad hoc (required for Apple silicon to launch it) but is not notarised, so on first launch macOS says the developer cannot be
verified. Players right-click the app, choose **Open**, then **Open** again. Removing the prompt, which most storefronts require, needs an Apple
Developer ID certificate: add `codesign --options runtime --sign "Developer ID Application: ..."` and `xcrun notarytool` / `xcrun stapler` steps
to `installer/mac/make-dmg.sh` (credentials as repository secrets).
