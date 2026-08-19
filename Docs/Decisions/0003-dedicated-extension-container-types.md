# 0003 - Dedicated extension container types on every schema object

- **Status**: Accepted. Lands with the implementing change.
- **Date**: 2026-08-10
- **Affects**: `Unity.Cloud.Gltfast.Schema` (7.0, `preview`)

## Context

Every glTF object may carry `extensions`. The schema models it as a property whose type derives from `AdditionalPropertyContainer`, which stores unmatched JSON in `[JsonExtensionData]` and exposes it through `IPropertyContainer` (`TryGetValue<T>`, `Set<T>`, enumeration).

Supporting a glTF extension the importer or exporter itself consumes means adding a typed property, so it deserializes in one pass instead of retaining a `JsonElement` and re-deserializing per access. A typed property needs a *declared* type to live on, so it goes on a class derived from `AdditionalPropertyContainer` — the pattern `MaterialExtensions`, `NodeExtensions`, `RootExtensions`, `TextureExtensions`, `TextureInfoExtensions`, `MeshPrimitiveExtensions` and `BufferViewExtensions` already follow.

Seven of the 26 objects had such a type. The other 19 declared `Extensions` as `AdditionalPropertyContainer`. Narrowing one of those later is a break on two levels:

- **Binary**: the type is part of the `get_Extensions`/`set_Extensions` signatures. Precompiled consumers get `MissingMethodException`; Unity's API Validation demands a major version bump.
- **Source**: reads keep compiling (derived → base upcast), every write stops: `scene.Extensions = new AdditionalPropertyContainer()`.

So each newly supported extension on a base-typed object would force a major release, for a change that is otherwise purely additive.

## Decision

All 26 schema objects declare `Extensions` as a dedicated type derived from `AdditionalPropertyContainer`, named `<Object>Extensions`. The 19 new ones are empty; they exist so a later typed property is additive.

`Extras` stays `AdditionalPropertyContainer` on every object. Extras is application data with no schema, so it never gains typed members and needs no per-object type.

Timing is the point: 7.0 is already a breaking release, so the 19 signature changes cost nothing now and are unavailable later.

## Consequences

- No internal churn. Every existing assignment (`GltfWriter.cs:350`, `MaterialExportBase.cs:133`, `GltfMaterialExporter`, `GltfUnlitMaterialExporter`, `TextureInfo.cs:54`) already constructs a derived type.
- Wire format and behavior unchanged. The new types add no members, so serialization is identical and extension access still goes through `TryGetValue<T>`/`Set<T>`.
- 19 new public types, most of which may stay empty. `AccessorSparseIndicesExtensions` is unlikely to ever hold a member; `SceneExtensions` (`KHR_audio`), `BufferExtensions` (`EXT_meshopt_compression` fallback) and `AnimationChannelTargetExtensions` (`KHR_animation_pointer`) have known candidates.
- The four animation ones sit behind `UNITY_ANIMATION`/`GLTFAST_ANIMATION`, like the objects that declare them.
- `MovedFromAttributeCoverageTests.k_PostRenameTypes` gains 19 entries: post-rename types have no 6.x name to migrate from.
- Adding a typed property later stays **behaviorally** breaking, which this decision does not address. `[JsonExtensionData]` only collects *unmatched* keys, so once `KHR_foo` becomes a property it disappears from `Count`, `Keys`, the indexer and `TryGetValue<T>("KHR_foo", …)`. Generic extension readers see the key vanish, so each such addition needs a changelog note.
- The `*Extensions` suffix is now overloaded: `MeshExtensions` is a glTF extension container, `AttributesExtensions` is a static extension-method class. Names for extension-method helpers on schema objects are consumed, so such helpers need a different name (e.g. `…Helper`, or members on the type itself).

## Alternatives considered

| Alternative | Rejected because |
|---|---|
| Add the type only when an extension needs it | Each addition is a binary + source break, so it waits for a major release. That is the cost this record removes |
| Keep the base type and rely on `TryGetValue<T>`/`Set<T>` | Works, and stays the escape hatch for extensions gltfast does not consume itself. Retains a `JsonElement` and re-deserializes per access, so it is unsuitable for the import/export hot path |
| Keep the declared type as the base and cast to the derived one at use sites | The serializer binds properties by declared type, so the typed member would never be populated without a custom converter |
| Per-object types for `Extras` too | Extras has no schema; a typed member would contradict its purpose |
| Seal the new types | The seven existing containers are unsealed; consistency wins, and sealing later is the breaking direction |

## References

- `0002-nullable-gltf-index-properties.md` — the "schema is a permissive projection of the JSON" principle these containers implement
- glTF 2.0 [specification, extensions](https://registry.khronos.org/glTF/specs/2.0/glTF-2.0.html#specifying-extensions) — any glTF object may carry `extensions`
- `Runtime/Scripts/Schema/AdditionalPropertyContainer.cs` — base class
- `Runtime/Scripts/Json/GltfJsonContext.cs` — source generation discovers the types by walking from `Root`, so no registration is needed
