# 0002 - Nullable glTF index properties

- **Status**: Accepted. Lands with the implementing change.
- **Date**: 2026-08-06
- **Affects**: `Unity.Cloud.Gltfast.Schema` (7.0, `preview`)

## Context

glTF references are indices into root-level arrays. `0` is legal, so `int` cannot distinguish "index 0" from "absent".

7.0 made *optional* indices `int?`. For *required* ones it kept `int` plus `Constants.UnsetIndex` (`-1`), with an internal `int?` shim per member:

```csharp
[JsonIgnore]
public int Buffer { get; set; } = Constants.UnsetIndex;

[JsonPropertyName("buffer"), JsonInclude]
internal int? BufferSerialized
{
    get => Buffer < 0 ? null : Buffer;
    set => Buffer = value ?? Constants.UnsetIndex;
}
```

Goal was a smaller API break. Costs outweighed it:

- Required and optional indices had different types for one concept.
- Two encodings of absence; 13 shim pairs (~65 lines) only translated between them.
- Break avoided was small. Of the 13 members, 2 already change type in 7.0 (`uint` → `int`), 4 are sizes. Only **7** stayed source-compatible, all plumbing. The indices users actually touch (`Node.Mesh`, `MeshPrimitive.Material`, `Accessor.BufferView`) are already `int?`.
- Extensions may relax required-ness, so non-nullable forces a later break.
- Caused a defect: `MaterialsVariantsExtension.TryGetMaterialIndex` returned `true` with `-1`, `MeshPrimitive.GetMaterialIndex` passed it on as `int?`, so `HasValue` was `true` and callers indexed `Root.Materials[-1]`.

Serialization uses `WhenWritingDefault`, which drops `"buffer": 0` from a plain `int` (`0` == `default`). Hence every shim is `int?` — the design was already nullable internally, public `int` was a facade.

## Decision

Every index property is `int?`, `null` = absent, uniform across required and optional.

Sizes and counts stay non-nullable with `0` = absent. Spec `minimum: 1` makes `0` illegal, so no sentinel is needed.

| | Members | Type |
|---|---|---|
| Indices (9) | `BufferView.Buffer`, `AccessorSparseIndices.BufferView`, `AccessorSparseValues.BufferView`, `AnimationChannel.Sampler`, `AnimationSampler.Input`, `AnimationSampler.Output`, `MeshPrimitiveDracoExtension.BufferView`, `BufferViewMeshoptExtension.Buffer`, `MaterialVariantsMapping.Material` | `int?` |
| Sizes (4) | `Buffer.ByteLength` | `long` |
| | `BufferView.ByteLength`, `Accessor.Count`, `AccessorSparse.Count` | `int` |

- Delete `Constants.UnsetIndex`, `Constants.UnsetByteLength` and all 13 shim pairs. `epsilon` stays.
- `IBufferView.Buffer` → `int?`, `ByteLength` stays `int`, interface → `internal`: only four private `GltfImport` methods consume it, no public member takes or returns it.
- Widths by role. External-resource size → `long` (compared only, never addresses memory). In-memory offset or slice length → `int`, matching `NativeArray<T>.Length`, sub-array slicing and `GlbBinChunk.Start`.

### Principle

> Schema is a permissive projection of the JSON. It does not enforce spec requirements, because extensions relax them.

Also settles: no `Root` validation pass, no export validation. Validity is contextual, so consumers judge it at point of use.

## Consequences

- Wire format byte-identical for the 9: absent → `null` → omitted, `0` → `0`. Zero-index guards (`JsonSerializationTests.cs:711`, `:793`) unaffected.
- Writes compile unchanged (`Buffer = 0` converts implicitly). Only reads break — ~114 sites in `Runtime/`.
- Incremental public break: 7 members, all plumbing.
- Fixes the `MaterialsVariants` defect by construction; `TryGetMaterialIndex` now returns `false` for an absent material. Needs a *Fixed* changelog entry.
- Nullability is not safety: malformed documents reference missing elements, so `null` and out-of-range both need handling. `internal static class GltfIndex.TryGetElement<T>(List<T>, int?, out T)` covers null list, null index, negative and out-of-range in one call. Deliberately not an extension method, so a later promotion to public adds nothing to every `int?` and `List<T>`.
- Sizes: `0` now omitted instead of written. Never assign `-1` to a size, it would serialize.
- `int?.ToString()` on `null` is the empty string, so log args go blank. `GltfIndex.Describe(int?)` fixes that; a new `LogCode` separates absent from out-of-range (`AnimationChannelSamplerInvalid` currently covers both).
- Memory: ~4 bytes per buffer view, animation channel and sampler. Negligible — `Accessor.Count` stays `int` and `Accessor.BufferView` was already `int?`.
- 5 of the 9 members sit behind `UNITY_ANIMATION`/`GLTFAST_ANIMATION`, `DRACO_IS_INSTALLED` and `MESHOPT_IS_RECENT`, as do their read sites. Default builds hide the breakage.

## Alternatives considered

| Alternative | Rejected because |
|---|---|
| Keep the sentinel | See Context |
| `Root` validation pass, letting downstream trust values | Rejects documents valid under an unknown extension. When an extension supplies the data, its own path handles it and the base-spec read site is never reached |
| Export validation in `Serialize` | Export add-ons may legitimately create buffer views without a valid buffer |
| Nullable sizes too (13 uniform) | Adds nothing over spec `minimum: 1`, and puts lifted comparisons into offset arithmetic where `length <= byteLength` silently yields `false` on `null` |
| `Accessor.Count` as `uint`/`uint?` | Range gain unusable: `NativeArray<T>` ctor, `ScheduleBatch` and `SetVertexBufferParams` are `int`-bound. Reintroduces the casts the 7.0 `uint` → `int` change removed, wraps silently in Burst code, not CLS-compliant |
| `BufferView.ByteLength` as `long` | `ByteOffset` is `int`, so offsets overflow before lengths do — a `long` length could express a slice the offset cannot reach. Adds casts plus a break to a member that has none |
| `int Buffer` on `IBufferView` via explicit impl `?? -1` | Hides the sentinel instead of removing it |
| Public or extension-method helpers | An extension on `int?`/`List<T>` shows up on every such value in every consuming project, for a niche concern |

## References

- glTF 2.0 [JSON schema](https://github.com/KhronosGroup/glTF/tree/main/specification/2.0/schema) — `minimum` for indices (`0`) and sizes (`1`)
- `Runtime/Scripts/Json/GltfJsonContext.cs` — `DefaultIgnoreCondition = WhenWritingDefault`
- `CHANGELOG.md` — 7.0 `uint` → `long`/`int` rationale
- `Documentation~/ImplementationDetails.md`, "Working with the schema" — user-facing summary
