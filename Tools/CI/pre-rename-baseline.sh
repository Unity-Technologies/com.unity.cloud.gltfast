#!/bin/sh
# Usage: pre-rename-baseline.sh [verify|generate] [output-path]
# The output is reflected out of origin/develop, so it does not depend on the branch this runs on.
set -eu

MODE="${1:-verify}"
BASELINE=Packages/com.unity.cloud.gltfast.tests/Tests/Editor/ApiUpdater/PreRenameTypes.txt
# Sibling of the repo, not $TMPDIR: this holds a full checkout plus three Unity Library folders, which
# overruns a small /tmp.
SCRATCH=$(mktemp -d "$(dirname "$PWD")/gltfast-baseline.XXXXXX")
WORKTREE="$SCRATCH/gltfast-develop"
GENERATED="${2:-$(mktemp)}"
PARTIAL=$(mktemp)

cleanup() {
    git worktree remove --force "$WORKTREE" 2>/dev/null || true
    rm -rf "$SCRATCH" "$PARTIAL"
}
trap cleanup EXIT

git fetch --no-tags origin develop

. Tools/CI/unity-cli.sh
git worktree add --detach "$WORKTREE" origin/develop

# No single project loads every shipping assembly, so the baseline is the union of three, and each run
# demands exactly the assemblies its own project carries.
: > "$GENERATED"
for pair in \
    "glTFast-Test:glTFast,glTFast.Export,glTFast.Newtonsoft,glTFast.Editor" \
    "glTFast-Test-entities:glTFast.dots" \
    "glTFast-Test-HDRP:glTFast,glTFast.Export"
do
    PROJECT="$WORKTREE/Projects/${pair%%:*}"
    mkdir -p "$PROJECT/Assets/Editor"
    cp Tools/scripts/GeneratePreRenameTypes.cs "$PROJECT/Assets/Editor/"
    install_unity_editor "$PROJECT"
    # An editor run that exits 0 without executing Run would otherwise append the previous project's
    # types again, and sort -u would hide the assembly that went missing.
    : > "$PARTIAL"
    # `unity run` owns -batchmode and -quit; passing them after `--` is rejected
    unity run --timeout 2400 "$PROJECT" -- \
        -executeMethod Gltfast.Tools.GeneratePreRenameTypes.Run \
        -preRenameOutput "$PARTIAL" \
        -preRenameAssemblies "${pair#*:}"
    cat "$PARTIAL" >> "$GENERATED"
done

# C collation reproduces the ordinal order the generator and the committed file are written in.
LC_ALL=C sort -u "$GENERATED" -o "$GENERATED"

if [ "$MODE" = generate ]; then
    echo "Generated $(wc -l < "$GENERATED") type(s) into $GENERATED"
    exit 0
fi

if ! diff -u "$BASELINE" "$GENERATED"; then
    echo "::error:: $BASELINE is stale. Regenerate it from origin/develop and commit the result."
    exit 1
fi
echo "$BASELINE matches origin/develop"
