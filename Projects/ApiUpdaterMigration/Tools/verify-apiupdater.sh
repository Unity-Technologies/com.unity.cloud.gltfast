#!/usr/bin/env bash
# Verify that Unity's API Updater migrates glTFast 6.x consumer code to the 7.0
# Unity.Cloud.Gltfast.* namespaces via the [MovedFrom] attributes.
#
# It drives a real, headless Editor (the source rewrite cannot run in-process — see
# plans/research/api-updater-testing.md). The run is revertable: the legacy source is
# snapshotted and restored at the end, so the fixture stays pristine and re-runnable.
set -euo pipefail

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"   # project root
SRC="$HERE/Assets/LegacyConsumer/UsesGltfastLegacy.cs"

# Require the Unity CLI. It resolves the editor version (from ProjectVersion.txt) and its install
# location for us, so this harness is portable (no hard-coded macOS Hub path).
if ! command -v unity >/dev/null 2>&1; then
  cat >&2 <<'MSG'
error: the Unity CLI ('unity') was not found on PATH.

This harness uses the Unity CLI to launch the correct editor headlessly.
Install it, then re-run this script:
  https://docs.unity.com/en-us/unity-cli/use-unity-cli#install-the-unity-cli
MSG
  exit 127
fi

echo "Project: $HERE"

# 1) snapshot for revert (kept OUTSIDE Assets/ so Unity never imports it / leaves a .meta)
SNAPSHOT="$HERE/Tools/.UsesGltfastLegacy.cs.snapshot"
cp "$SRC" "$SNAPSHOT"
trap 'mv -f "$SNAPSHOT" "$SRC" 2>/dev/null || true' EXIT   # always restore the pristine fixture

# 2) run the updater headless via the Unity CLI. `unity run` launches the project's editor version
#    in batch mode, manages the -batchmode/-quit lifecycle, waits, and streams the editor log to
#    stdout. Only -accept-apiupdate is forwarded (it makes the namespace rewrite non-interactive);
#    --timeout guards against a hung import.

unity run "$HERE" --timeout 1200 -- -accept-apiupdate

# 3) show the migration
echo "=== migration diff (6.x -> 7.0) ==="
diff -u "$SNAPSHOT" "$SRC" || true

# 4) assert the old namespace is gone from CODE (ignore // comments, which the updater leaves as-is)
echo "=== assertion ==="
code_only="$(sed 's://.*::' "$SRC")"
if grep -qE '\bGLTFast\b' <<<"$code_only"; then
  echo "FAIL: old 'GLTFast' namespace still present in code after the API Updater ran:"
  grep -nE '\bGLTFast\b' <<<"$code_only"
  exit 1
fi
grep -q 'Unity\.Cloud\.Gltfast' <<<"$code_only" \
  && echo "PASS: source migrated to Unity.Cloud.Gltfast (old GLTFast references removed)." \
  || { echo "FAIL: no Unity.Cloud.Gltfast references found; the updater may not have run."; exit 1; }
