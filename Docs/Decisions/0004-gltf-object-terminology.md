# 0004 - "glTF object" terminology, `Schema` namespace renamed to `Objects`

- **Status**: Accepted. Lands with the implementing change.
- **Date**: 2026-08-11
- **Affects**: `Unity.Cloud.Gltfast.Schema` → `Unity.Cloud.Gltfast.Objects` (7.0, `preview`)

## Context

The C# types the glTF JSON deserializes into were named after JSON Schema — `GLTFast.Schema` in 6.x, then `Unity.Cloud.Gltfast.Schema`. Two problems:

- This project uses no JSON Schema. Nothing validates against one, and no type is generated from one.
- More fundamentally, it is a category error. A schema *describes* a shape; these types *hold data*. They are also the public read model, returned from `IGltfReadable.GetSourceMaterial` and friends long after parsing is done, so naming them after the format's description misleads in both directions.

The provenance was not baseless: glTF ships 33 JSON Schema files, and these types are a hand-written port of them, base types included (`glTFProperty` → `IPropertyContainer`, `glTFChildOfRootProperty` → `NamedObject`). But provenance is not what the name needs to communicate.

The spec supplies its own genus for exactly this set of things. §Technical Terminology defines each concept as "An object describing…" (accessor, node, texture, sampler, scene, mesh primitive); the §Concepts figure is titled "glTF Object Hierarchy"; §Specifying Extensions says "Any glTF object **MAY** have an optional `extensions` property"; §Indices and Names says "Any top-level glTF object **MAY** have a `name` string property".

## Decision

**glTF object** is the prose term for any object defined by the glTF JSON structure. It replaces "schema class", "schema type", "schema object" and "JSON object" in documentation, XML doc comments and discussion.

The namespace becomes `Unity.Cloud.Gltfast.Objects`, and `Runtime/Scripts/Schema/` becomes `Runtime/Scripts/Objects/`, so code and prose agree.

Timing is the point, as in `0002`: 7.0 already renamed the root namespace, so this rides an existing breaking change rather than adding one. After 7.0 ships the window closes.

## Consequences

- 478 occurrences across 239 files (387 in the package, 91 in tests). Mechanical.
- **The 83 `MovedFrom` attributes need no change.** Every one names `GLTFast.Schema` — the 6.x namespace — so the API-updater path for 6.x consumers keeps working untouched, now resolving to `Unity.Cloud.Gltfast.Objects`.
- The `…Extensions` suffix carries three unrelated meanings: the extensions container on a glTF object (26 types), a C# extension-method class (3), and a glTF extension implementation (4 of 16 use the singular `…Extension`). Review feedback flagged the first draft of `CONTEXT.md` for asserting a naming rule the code contradicts. Only the glossary was wrong; the code was left alone. Extension implementations keep their feature names (`ClearCoat`, `TextureTransform`), with the `<Owner><Feature>Extension` form reserved for genuine ambiguity such as `KHR_materials_variants` appearing on both `Root` and `MeshPrimitive`.
- `AttributesExtensions` was briefly renamed to `AttributesHelper`, on the assumption that `Attributes` is a glTF object whose extensions container would need that name. It is not: the specification defines `attributes` as "a plain JSON object" mapping semantics to accessor indices, and it does not derive from `glTFProperty`, so it can never carry `extensions`. The container can never exist, the name is free, and `<Subject>Extensions` is the .NET convention for an extension-method class. The rename was reverted.
- **No type may be renamed in 7.0 and still auto-migrate.** The API Updater rejects a `[MovedFrom]` that renames a type *and* moves it to another assembly, and every type moved assembly in this release (`glTFast` → `Unity.Cloud.Gltfast`). The `AttributesHelper` attempt failed the ScriptUpdater run on `Projects/ApiUpdaterMigration` with `exitcode 130`. Renaming a shipping type therefore costs its migration path outright; `MovedFromAttributeCoverageTests` now asserts `sourceClassName` is never set, so the next attempt fails in tests rather than in the updater. Namespace moves are unaffected — that is what the other 83 attributes do.
- `MovedFromAttributeCoverageTests` needed a real fix, not just a string sweep. It derived each expected 6.x namespace by swapping the root prefix, so it demanded `GLTFast.Objects` and failed on all 82 annotated types. It now consults a `k_RenamedNamespaces` map (`Unity.Cloud.Gltfast.Objects` → `GLTFast.Schema`) first. Any future namespace whose last segment is renamed as well needs an entry there. `k_PostRenameTypes` holds fully-qualified names as strings and moved with the replacement.
- All 42 `CHANGELOG.md` mentions sit under `[Unreleased Preview]`, so there is no released entry to preserve. The 6.x→7.0 table in `UpgradeGuides.md` gains `Objects` as the target namespace.
- `Objects` is a weak identifier read on its own. Accepted deliberately: the segment's real job is disambiguating from `UnityEngine.*` at call sites — `Objects.Material` against `UnityEngine.Material`, and 26 alias directives exist for precisely this. Agreement with the prose term outweighs the segment reading well alone.
- `Unity.Cloud.Gltfast.Newtonsoft.Schema` is a separate, parallel type set and keeps its name.
- Records `0001`, `0002` and `0003` say "schema" throughout, and `0003` cites `Runtime/Scripts/Schema/ExtrasContainer.cs`. They are immutable and stay as written; this record supersedes their vocabulary and those paths, not their decisions.

## Alternatives considered

| Alternative | Rejected because |
|---|---|
| Keep `Schema` | Defensible on provenance only. Names the description, not the instance |
| `Document` | Reads best as a call-site qualifier (`Document.Material` — "the material the document describes"), but is glTF-Transform vocabulary, not spec vocabulary. The namespace should echo the glossary term |
| `Properties` | The spec's own schema base-type name (`glTFProperty`) and its "Properties Reference" appendix. Collides with the C# language concept, and `Properties`/`IPropertyContainer` already mean something narrower here |
| `Entities` | Also spec vocabulary — "Entities of a glTF asset are referenced by their indices". In a package that uses jobs and Burst, readers will hear DOTS `Entity` |
| `Dom` | Implies a parent-linked tree. glTF is flat `Root` arrays addressed by index, deliberately not a navigable graph |
| `Model`, `Asset` | `Asset` is already a type here (asset metadata only), and both collide with established Unity meanings |
| Prose term only, keep the namespace | Leaves code and glossary disagreeing, and forfeits the free window before 7.0 |

## References

- `CONTEXT.md` — glossary; **glTF object** and the terms it borders on
- glTF 2.0 [specification, Technical Terminology](https://registry.khronos.org/glTF/specs/2.0/glTF-2.0.html#technical-terminology) — defines each concept as "An object describing…"
- `0002-dedicated-extension-container-types.md` — the `*Extensions` suffix overload it records is now pinned in `CONTEXT.md`
