#!/usr/bin/env bash
# Packages the macOS player into a drag-to-Applications disk image.
# Run on macOS (hdiutil and codesign only exist there):
#   bash installer/mac/make-dmg.sh <folder containing CovenantNight.app> <output.dmg>
set -euo pipefail

src="${1:?folder that contains the built .app}"
out="${2:?output .dmg path}"

app="$(find "$src" -maxdepth 2 -name '*.app' -type d | head -n 1)"
[ -n "$app" ] || { echo "No .app found in $src" >&2; exit 1; }

# Build artifacts lose their unix permissions in transit; the player and its plug-ins must be executable again.
chmod -R u+rwX,go+rX "$app"
find "$app/Contents/MacOS" -type f -exec chmod +x {} +

# Apple Silicon refuses to launch code with no signature at all, so apply an ad-hoc one.
# (This is not a Developer ID signature: players still get the Gatekeeper prompt until the app is notarised.)
xattr -cr "$app"
codesign --force --deep --sign - "$app"

stage="$(mktemp -d)"
trap 'rm -rf "$stage"' EXIT
cp -R "$app" "$stage/Covenant Night.app"
ln -s /Applications "$stage/Applications"

mkdir -p "$(dirname "$out")"
rm -f "$out"
hdiutil create -volname "Covenant Night" -srcfolder "$stage" -ov -format UDZO "$out"
echo "Created $out"
