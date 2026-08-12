# 0003 - Non-object `extras`

- **Status**: Accepted. Lands with the implementing change.
- **Date**: 2026-08-10
- **Affects**: `Unity.Cloud.Gltfast.Schema` (7.0, `preview`). Supersedes the `Extras` paragraph of `0002-dedicated-extension-container-types.md`.

## Context

`extras` is the one glTF slot with no type. [`extras.schema.json`](https://github.com/KhronosGroup/glTF/blob/main/specification/2.0/schema/extras.schema.json) declares no `type`; the specification says `extras` **MAY** have any type and **SHOULD** be a JSON object "for best portability". glTF-Validator grades `NON_OBJECT_EXTRAS` at *Information* severity — its lowest tier, alongside style hints.

The schema modelled `Extras` as a class (`AdditionalPropertyContainer`, or `MeshExtras` on `Mesh`), so `System.Text.Json` could not bind a scalar or an array to it:

| `extras` | Before |
|---|---|
| `{…}` | OK |
| `null` | OK (`Extras == null`) |
| `42`, `"s"`, `true`, `[1,2]` | `JsonException` |

The exception was not contained. `GltfImport.ParseJson` catches only to dispose pending `UriValue`s and rethrows; every caller up to `LoadGltf` catches only `OperationCanceledException`. So one scalar `extras` anywhere in a document aborted the whole import with an unhandled exception — not a `LogCode`, not a `false` return. glTFast rejected, loudly, files the reference validator waves through.

Nothing covered it: every `extras` in every test asset and fixture was an object.

## Decision

`extras` accepts any JSON value. Objects deserialize as before; anything else is retained verbatim and exposed.

**`ExtrasContainer : AdditionalPropertyContainer`** carries the new surface. All 26 `Extras` properties are typed to it, `MeshExtras` derives from it.

```csharp
public ValueKind Kind { get; }    // Object unless the JSON carried a non-object value
public Value RawValue { get; }    // the value, when Kind != Object
```

`Kind` is derived from an `internal JsonElement RawValueElement`; `default(JsonElement)` reports `Undefined`, which is the free "this is a regular object" sentinel. `Value` and `ValueKind` already exist and already traverse arbitrary JSON, so the additions are two members, plus promoting `Value.TryGetValue<T>(out T)` from `internal` to `public` for whole-`extras` conversion.

**`ExtrasConverterBase<T> : JsonConverter<T> where T : ExtrasContainer, new()`** handles the non-object case — `JsonElement.ParseValue(ref reader)` on read, `WriteTo` on write — and defers the object case to a subclass. It is applied to the *property*:

```csharp
[JsonPropertyName("extras")]
[JsonConverter(typeof(ExtrasConverter))]
public ExtrasContainer Extras { get; set; }
```

Two subclasses, because the object case has two different right answers:

- `ExtrasConverter` (25 sites) **reads the JSON object directly** into the `[JsonExtensionData]` dictionary, the way `AttributesConverter` already does.
- `MeshExtrasConverter` (`Mesh.Extras`) **delegates** to `MeshExtras`'s own `JsonTypeInfo<MeshExtras>`, so `TargetNames` — and any member added later — is handled without duplicating generated logic.

The split is measured, not assumed. Delegating everywhere costs **+19 %** parse time on the `extras` benchmark (1111 nodes, 2–20 properties each): each `extras` re-enters `JsonSerializer.Deserialize`, which builds a fresh `ReadStack`. Reading the object inline instead lands at **−11 %**, i.e. faster than before the change. Meshes are far less numerous than nodes, so `MeshExtras` keeps the safer delegating form. The inline read is only equivalent while `ExtrasContainer` declares nothing to (de-)serialize; `ExtrasContainerHasNoSerializedMembers` asserts that by reflection, and points at `MeshExtrasConverter` as the pattern to follow if that ever changes.

Placement on the property, not the type, is load-bearing twice over. `[JsonConverter]` on a type is resolved with `inherit: false` in both the reflection resolver and the source generator, so a type-level attribute would silently miss `Mesh.Extras`; and `MeshExtrasConverter`'s delegation resolves `MeshExtras`'s own type info, which a type-level attribute would resolve back to the converter, recursing until the stack overflows.

Semantics settled here:

- `"extras": null` stays `Extras == null`. `HandleNullOnRead` is `false` for reference types, so the converter is never entered — the previous behaviour, for free, and `if (extras == null)` keeps meaning "absent".
- When `Kind != Object` the container has no properties: `Count` is `0`, `Keys` is empty, `ContainsKey`/`TryGetValue` return `false`, enumeration yields nothing. These fall out of the existing implementations, since `ExtensionData` stays null.
- `Set<T>` and `Clear()` discard the raw value — writing a property turns the `extras` into a JSON object. Both become `virtual` on the base so interface dispatch reaches the overrides. This keeps `Write` single-valued rather than having a silently-ignored dictionary.
- `extensions` is untouched and still rejects non-objects. It is `"type": "object"` in the schema, so that is a genuine spec violation.

## Consequences

- Spec-valid documents with non-object `extras` import, and round-trip byte-for-byte through `RootExtension.Serialize`.
- **Breaking**: 26 public property signatures change type. Reads keep compiling (derived → base upcast); writes such as `node.Extras = new AdditionalPropertyContainer()` stop. Affordable only because 7.0 is already breaking — the same argument, and the same churn, as 0002.
- **Parsing gets faster, not slower.** `JsonPerformanceTests`, median of 9 samples, paired adjacent runs: `extras` 35.1 ms → 31.3 ms (−10.9 %), `omni` 36.2 ms → 33.5 ms (−7.6 %). The 14 configurations without `extras` drifted −2.8 % (median) between the two runs, so drift-adjusted this is roughly −8 % and −5 %. Reading the object inline beats the generated converter's extension-data path. Allocations rise 0.4 % (4.398 → 4.415 MB on `extras`), the per-container `JsonElement`. Two baseline runs agreed to within 0.5 %.
- `where T : ExtrasContainer` on the converter base documents the intent, but with concrete subclasses a misapplication to an `extensions` container surfaces at runtime (the generated `(JsonConverter<NodeExtensions>)` cast) rather than at compile time.
- `AdditionalPropertyContainer` is no longer the declared type of any property, so the source generator stops emitting `GltfJsonContext.Default.AdditionalPropertyContainer`. Two test fixtures that used it now go through `GltfJsonContext.Default.ExtrasContainer`.
- `MovedFromAttributeCoverageTests.k_PostRenameTypes` gains `ExtrasContainer`: a post-rename type has no 6.x name to migrate from.
- No serialization fast-path regression. `IsFastPathSupported()` already returned `false` for all 26 declaring types, because each carries `[JsonExtensionData]`. Adding a property converter changes nothing there.
- Memory: one `JsonElement` (16 B) per `ExtrasContainer` *instance*, and instances exist only where the JSON carried `extras`. `JsonElement.ParseValue` uses `useArrayPools: false`, so each retained value owns a small standalone `JsonDocument` (~150–200 B plus payload) and does **not** pin the glTF JSON buffer — the same mechanism `[JsonExtensionData]` and `AttributesConverter` already rely on.
- The exporter is unaffected; it never populates `Extras`.
- The uncaught-`JsonException` path out of `GltfImport.LoadAsync` remains for genuinely malformed JSON. That is a broader error-handling gap, deliberately not conflated with this change.

## Alternatives considered

| Alternative | Rejected because |
|---|---|
| Discard non-object `extras` (`Extras = null`) | What three.js's `assignExtrasToUserData` does, and the smallest change. Makes the value unrecoverable and indistinguishable from absent, and silently drops it on re-export. The converter is needed either way, so the parse cost is identical — this only buys away two public members |
| Discard and log a `LogCode` | Same loss, plus a logger threaded into a converter. No existing converter has one |
| `Kind`/`RawValue` on `AdditionalPropertyContainer` | No signature churn, but puts two permanently meaningless members on all 26 `*Extensions` containers — `extensions` must be an object, so they could never be anything but `Object`/`Undefined` there |
| Per-object `Extras` types, mirroring 0002 | `extras` has no schema, so there is nothing per-object to type. One shared container is what the alternative-value problem needs |
| Type-level `[JsonConverter]` on the container | Resolved with `inherit: false`, so it would silently miss `Mesh.Extras`, and it makes the object case self-recursive — forcing the `[JsonExtensionData]` and `targetNames` handling to be hand-rolled |
| `JsonConverterFactory` | Same recursion problem; `MakeGenericType` is not IL2CPP/AOT-safe; over-broad `CanConvert` would swallow the `*Extensions` containers. No precedent in the package |
| Declare `Extras` as `JsonElement` | Binds from any token for free, but discards `IPropertyContainer`, `MeshExtras.TargetNames` and the extension-data dictionary, and retains a `JsonDocument` on the common path |
| Keyless `TryGetValue<T>(out T)` on `IPropertyContainer` | Would force `ReadOnlyProperties` to mirror it, per its documented contract. Promoting the existing `Value.TryGetValue<T>(out T)` to public covers the same need |

## References

- glTF 2.0 [`extras.schema.json`](https://github.com/KhronosGroup/glTF/blob/main/specification/2.0/schema/extras.schema.json) and [specification, extras](https://registry.khronos.org/glTF/specs/2.0/glTF-2.0.html#reference-extras) — MAY be any type, SHOULD be an object
- glTF-Validator [`ISSUES.md`](https://github.com/KhronosGroup/glTF-Validator/blob/main/ISSUES.md) — `NON_OBJECT_EXTRAS`, severity *Information*
- `0001-nullable-gltf-index-properties.md` — "schema is a permissive projection of the JSON"
- `0002-dedicated-extension-container-types.md` — the `Extensions` counterpart; its `Extras` paragraph is superseded here
- `Runtime/Scripts/Json/ExtrasConverter.cs`, `Runtime/Scripts/Schema/ExtrasContainer.cs`
- `Runtime/Scripts/Json/AttributesConverter.cs` — the in-repo precedent for `JsonElement.ParseValue` + `WriteTo`
