# 0006 - Buffer data access and lifetime

- **Status**: Accepted. Lands with the implementing change.
- **Date**: 2026-08-14
- **Affects**: `Unity.Cloud.Gltfast` (7.0, `preview`)

## Context

glTF stores geometry in three nested layers: `buffers` (raw bytes), `bufferViews` (a possibly strided window into a buffer) and `accessors` (a typed window into a buffer view, optionally sparse). Reading them was internal, pointer-based and undocumented.

What existed before, none of it covered by a record:

- `IGltfBuffers` (internal) mixed accessor-keyed and bufferView-keyed members under accessor names, so every caller resolved `accessor.BufferView.Value` and passed `accessor.ByteOffset` by hand.
- `IGltfReadable.GetAccessor`/`GetAccessorData` were public, `[Obsolete]`, and documented their own lifetime in prose: *"Only available during loading phase as underlying buffers are disposed right afterward."*
- `GetAccessorSparseIndices`/`GetAccessorSparseValues` were **public** and returned `void*` into buffer memory with no lifetime guarantee at all.
- Import add-ons got no buffer access. No hook is both awaitable and positioned where buffers are valid: `IPostJsonDeserialization` returns `bool` and runs before buffers exist; `LoadAccessorDataEvent` is a bare `Action`.
- A repeated `// TODO fix resource disposal when calling DisposeVolatileData() while jobs are queued/running` recorded a known race between disposal and in-flight jobs.

Buffer memory is not owned by the buffer array. `m_Buffers` holds non-owning views; the memory belongs to downloads, pinned managed arrays and `UriValue`s.

## Decision

| Concern | Decision |
|---|---|
| Access surface | `IGltfBufferData`, public, keyed by **index**. Buffer views and accessors, not raw buffers. |
| Lifetime | The buffer data **is** the lease. Buffer memory is released once no lease is undisposed. |
| Acquisition | `GltfImport.LeaseBufferData`, plus the `IBufferDataConsumer` add-on hook at the barrier where every buffer is resident. |
| Revocation | `GltfImport.Dispose()` releases the memory regardless of outstanding leases, logging `LogCode.BufferDataForceDisposed`. Later reads throw `ObjectDisposedException` via the collections safety system. |
| `unsafe` | Banned from the public contract; permitted in the implementation. |
| Burst | Returned containers are usable as job fields. |
| Failure | `BufferAccessStatus`, not exceptions and not `bool`. |
| Validation | Buffer-level at resolution time, accessor-level per retrieval, nothing per element. |
| Data | Raw glTF space. No conversion, normalization or coordinate flip. |
| Sparse | Not provided. Reported as `BufferAccessStatus.SparseUnsupported`. |
| Description | Read off the public `Objects.Accessor` (`ComponentType`, `Type`, `Normalized`, `ElementByteSize`). Not duplicated on the buffer data. |

### Principle

> Buffer data is borrowed, never owned by the caller. A lease is the borrow; disposing it is what ends it.

Also settles: no post-import buffer retention without an explicit lease. Import releases buffers at the end of loading, as before; a lease is the only thing that extends that.

## Consequences

- Add-ons and callers can read buffer data safely for the first time. `unsafe` is not required of them.
- The disposal-versus-jobs race is closed for leased data: memory outlives every lease.
- Removed: `IGltfReadable.GetAccessor`, `IGltfReadable.GetAccessorData` (both already `[Obsolete]` with removal pre-announced) and the two `public unsafe void*` sparse methods, which become explicit `IGltfBuffers` implementations until that interface goes.
- `ReadOnlyNativeStridedArray<T>` is public. Its field layout and constructor set are frozen for 7.x, so its defects were fixed first: the conditionally compiled trailing constructor parameter, and the missing `Length`, `ByteStride` and `IsCreated`.
- `ReadOnlyNativeArray<T>` stays internal. It is used in zero Burst jobs, so its Burst behaviour is unproven, and callers never need to construct a view themselves.
- `ReadOnlyNativeArrayFromManagedArray` releases its `AtomicSafetyHandle` and its `Dispose` is idempotent. It previously leaked one handle per instance.
- Memory owners are split in two: those backing buffer data wait for the last lease, everything else is released at the end of loading as before.
- A leaked lease holds the asset's whole buffer memory until `GltfImport.Dispose()`. It is loud when it finally fires and silent until then.

## Alternatives considered

| Alternative | Rejected because |
|---|---|
| Per-buffer reference counting | Ownership is a flat list shared with the image pipeline, and `UriValueConverter` pushes buffer and image data URIs onto one pending list with no marker. Per-buffer counts cannot drive per-buffer freeing, so they would describe a granularity the ownership model does not have. Revisit if partial release is ever needed. |
| Scoped callback (`ReadAsync(ctx => …)`) | Cannot schedule jobs and await them, cannot hold data across frames |
| True deferred free (refcount with no forced release) | A leaking add-on leaks the memory permanently and survives domain reload badly. `Dispose()` has to be deterministic |
| Copy on acquisition | One memcpy per buffer for every consumer, including those that only read |
| `bool TryGet…` | Cannot distinguish "sparse unsupported" from "type mismatch"; callers would re-derive the reason from the schema |
| Exposing raw `buffers` | Whole-buffer byte access has almost no real audience and is the widest possible surface |
| Public `IBufferView` as the key | Reverses [0001](0001-nullable-gltf-index-properties.md), and lets third parties supply synthetic views whose memory the store neither owns nor can validate |
| Re-exposing `ComponentType`/`Type`/`Normalized` on the buffer data | `Objects.Accessor` is public and already carries them |
| Implicit conversion to a requested type | Conversion is a schedulable job in this package; hiding it behind a getter either allocates per call or costs a pass the caller cannot schedule |
| Replacing `IGltfAccessors` | Different product and lifetime: it serves *decoded, Unity-space* arrays whose coordinate conversion depends on internal `AccessorUsage` classification |

## References

- glTF 2.0 [specification](https://github.com/KhronosGroup/glTF/tree/main/specification/2.0) — buffers, buffer views and accessors
- [0001](0001-nullable-gltf-index-properties.md) — `IBufferView` is internal; in-memory offsets and lengths are `int`
- `Documentation~/UpgradeGuides.md` — user-facing migration
