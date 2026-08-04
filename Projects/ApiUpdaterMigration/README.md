# API Updater migration test project

A dedicated, throwaway consumer project that proves the glTFast **7.0 assembly/namespace rename**
(`glTFast`/`GLTFast.*` → `Unity.Cloud.Gltfast.*`) auto-migrates consumer code through Unity's
**API Updater**, driven by the `[MovedFrom]` attributes on the 7.0 public types.

## Why a separate project + script (not an NUnit test)

The source rewrite is performed by Unity's external `ScriptUpdater` process during asset import; there
is no public/in-process API to run it and assert the result. The only reliable way to exercise it is to
drive a real Editor with `-accept-apiupdate`, exactly as Unity does in its own integration tests. See
[`plans/research/api-updater-testing.md`](../../plans/research/api-updater-testing.md) for the sourced
analysis.

The complementary in-process guardrail — asserting every public 7.0 type carries the correct
`[MovedFrom]` — lives in the package tests as `MovedFromAttributeCoverageTests` (EditMode).

## Fixture

- `Assets/LegacyConsumer/UsesGltfastLegacy.cs` — code written against glTFast **6.x** (`namespace GLTFast`).
  Does not compile against 7.0 until the updater rewrites it. Covers the core assembly + its
  `Schema`/`Logging`/`Materials` sub-namespaces and the `Export` assembly, via both `using`-reachable and
  fully-qualified references, plus the `Async`-suffixed method renames — those ride on
  `[Obsolete("… (UnityUpgradable) -> …Async(*)")]` shims rather than `[MovedFrom]`, so they are a
  separate mechanism the script asserts separately.
- `Assets/LegacyConsumer/ApiUpdaterMigration.LegacyConsumer.asmdef` — references the **new** assembly
  names (`Unity.Cloud.Gltfast`, `Unity.Cloud.Gltfast.Export`), mirroring a consumer who has already done
  the one manual step the updater cannot do for them (updating `.asmdef` references by name).

## Run

Requires the [Unity CLI](https://docs.unity.com/en-us/unity-cli/use-unity-cli#install-the-unity-cli)
(`unity`) on `PATH` — the script uses it to launch the correct editor headlessly and fails with an
install pointer if it is missing.

```bash
Projects/ApiUpdaterMigration/Tools/verify-apiupdater.sh
```

The script snapshots the legacy source, runs the project via `unity run … -- -accept-apiupdate` (the CLI
resolves the editor version from `ProjectSettings/ProjectVersion.txt` and its install location, runs in
batch mode, and waits), prints the before/after diff, asserts the old `GLTFast` namespace is gone from
code and `Unity.Cloud.Gltfast` is present, then **restores the pristine 6.x source** so the run is
repeatable. The editor can be overridden with the CLI's own `--editor-version` / `--editor-path` (or the
`UNITY_EDITOR_VERSION` env var).

To inspect the migrated result without auto-reverting, run `unity run` yourself and skip the restore.
