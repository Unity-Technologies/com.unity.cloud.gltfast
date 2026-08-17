# 0001 - Internal ValueTask migration

- **Status**: Accepted. Lands with the implementing change.
- **Date**: 2026-08-12
- **Affects**: `Unity.Cloud.Gltfast` export, buffer-view and image-import internals (6.x, `develop`)

## Context and Problem Statement

Awaiting a `Task` costs a heap allocation whenever the task is not already cached. `IDeferAgent.BreakPoint` was the suspected hotspot, so the question was how far `ValueTask` should replace `Task` across the package.

The premise turns out to be narrower than it looks. An `async Task` or `async Task<bool>` that completes synchronously **already allocates nothing**: the compiler's builder hands back a cached completed task and the state machine stays on the stack. An `async ValueTask` that suspends still boxes its state machine. The win therefore exists only where a method completes synchronously *and* returns a value that cannot be cached.

Among internal methods that leaves exactly one unconditional allocation: `MeshDataProxy` was not `async` at all and returned `Task.FromResult(...)` for mesh data already resident in memory.

## Considered Options

1. Leave everything on `Task`.
2. Move internal single-await methods on the export and image-import paths to `ValueTask`.
3. Also move the public `Task`-returning surface (`IDeferAgent.BreakPoint`, `GltfImport.GetMaterialAsync`).
4. Adopt `UnityEngine.Awaitable` instead.

## Decision Outcome

**Option 2.** `MeshDataProxy.GetVertexData` / `GetIndexData`, `IMeshData` and `NonReadableMeshData`, `GltfImport.GetBufferViewAsync`, `ImageImport.LoadDataAsync` and the two `GltfWriter` buffer helpers return `ValueTask`.

The rule applied is not "migrate where it pays" but "one idiom per path": internal single-await methods on these two paths move uniformly, so the shape is predictable, and the measurable gain is concentrated in the single site above. `ImageImport.LoadDataAsync` and the `GltfWriter` helpers moved under that rule, not because they measured better.

Option 3 is out of scope for a 6.x release — see [Not migrated](#not-migrated). Option 4 is deferred to the 7.0 preview line: `Awaitable` is main-thread-only and single-await, so it fits fewer sites than `ValueTask`, its value would be concentrated in exactly the public surface 6.x cannot change, and it collides by name with the shipped `AwaitableDownload` and `AwaitableTextureDownload`.

### Consequences

- Per call on the migrated proxy: **120 B to 0**. `MeshDataProxyTests` holds 20,000 calls to a 256 KiB budget; before the change the same loop allocated 2.4 MB.
- At whole-import scale the change is **not measurable**. Every measured delta fell inside one standard deviation, and an earlier revision of the measuring harness produced deltas of the same size with the *opposite* sign. A few KB either way disappears into ~11 MB of allocation per import.
- A case for changing public `Task`-returning API cannot rest on these numbers. The per-call win is real and deterministic; it is simply not what dominates an import.
- `ValueTask` may be awaited only once, which permanently constrains where this idiom can spread — see below.

## More Information

### Not migrated

`IDeferAgent.BreakPoint` is public API with shipped and user implementers, so changing it breaks consumers. It also matters less than assumed: `UninterruptedDeferAgent.BreakPoint()` has an empty body, completes synchronously, and already returns a cached task.

`GltfImport.GetMaterialAsync(int, CancellationToken)` and `GetDefaultMaterialAsync(CancellationToken)` return `Task.FromResult(...)` unconditionally, and their parameterless overloads wrap that in a second state machine. Both are public, so out of scope for 6.x — but they allocate on every call and are the strongest remaining candidates for the public surface.

`ValueTask`'s single-await rule rules out every task this package **stores**, as distinct from the methods above, which are awaited once at a single call site. The stored values stay on `Task`: the mesh generator's creation task (polled via `IsCompleted`, read via `Result`, then disposed), the texture load task map (awaited in `Prepare` **and again** in `DisposeTextureLoadTasks`), the buffer load task map (awaited in `WaitForBufferDownloads` **and again** inside `GetBufferViewAsync`), and every `Task.WhenAll` / `WhenAny` site in `GltfWriter` and the materials-variants controls.

### Where the export path actually allocates

`GltfWriter.BakeMeshIndices` and `WriteBindPosesToBuffer` only avoid an allocation when called with `sync: true`. Otherwise they poll `while (!job.IsCompleted) await Task.Yield()`, which suspends — and that loop allocates a continuation every frame, far outweighing the single `Task<int>` the signature change removed. Those poll loops, not the return types, are what allocation work should target next.

### The measurements

Per call, `MeshDataProxy.GetVertexData` / `GetIndexData`, 20,000 calls: `Task.FromResult` 2,400,434 B, `ValueTask` 434 B. 434 B is the idle-frame baseline, identical in both runs.

Median bytes per import, 10 repetitions, warmup excluded:

| Fixture | Before | After | Delta | Std dev, before / after |
|---|---|---|---|---|
| `FlatHierarchy` | 10,912,388 | 10,918,594 | +6,206 | 2,315 / 37,108 |
| `FlatHierarchyBinary` | 12,033,027 | 12,043,316 | +10,289 | 31,955 / 51,201 |
| `FlatHierarchyMemory` | 11,432,815 | 11,437,869 | +5,054 | 4,585 / 3,017 |

The per-import harness that produced this table was a one-time instrument and is not in the tree; only the per-call budget test remains. The numbers are recorded here so the question does not have to be re-measured to be answered.

### Measuring

`GC.GetAllocatedBytesForCurrentThread` reports 0 in this runtime and `GC.TryStartNoGCRegion` throws `NotImplementedException`, so `AllocationRecorder` reads `ProfilerRecorder(ProfilerCategory.Memory, "GC Allocated In Frame")` instead. `AllocationRecorderTests.ObservesKnownAllocation` pins one thing only: that a resolved counter reads non-zero, so a counter that resolves but reports nothing fails the suite rather than flattering a budget.
