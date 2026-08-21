# glTFast

Import and export of glTF 2.0 assets in Unity. Terms below are anchored to the
[glTF 2.0 specification](https://registry.khronos.org/glTF/specs/2.0/glTF-2.0.html);
where the spec and Unity use the same word for different things, this glossary says which is which.

## Language

**glTF object**:
Any object defined by the glTF JSON structure — an accessor, node, material, mesh primitive, and so on.
The spec's own genus for these ("An object describing…" in §Technical Terminology, the "glTF Object Hierarchy"
figure, "Any glTF object MAY have an optional `extensions` property"). In code these live in
`Unity.Cloud.Gltfast.Objects`.
_Avoid_: schema class, schema type, schema object, JSON object, entity, glTF property

**Root**:
The single top-level glTF object, holding the arrays every other glTF object is reached through.
_Avoid_: document, model, scene graph

**glTF asset**:
A complete glTF 2.0 asset — the JSON structure plus its binary buffers and images, whether separate,
embedded, or in a GLB container. The spec's term for the whole.
_Avoid_: Unity's meaning of "asset" (anything in a project); glTF's `asset` object is metadata only (see **Asset**)

**Asset**:
The glTF object carrying asset metadata: `version`, `minVersion`, `generator`, `copyright`.
Metadata only — not the asset itself (see **glTF asset**).

**glTF extension**:
An optional, named feature layered onto the base format, listed in `extensionsUsed` / `extensionsRequired`
and surfacing as an `extensions` property on a glTF object.
The type modelling one is named after the feature, without a suffix — `ClearCoat`, `Transmission`,
`TextureTransform`, `MeshGpuInstancing`. The form `<Owner><Feature>Extension` is used only where the feature
name alone would be ambiguous: the same extension on two owners (`MaterialsVariantsRootExtension` versus
`MaterialsVariantsMeshPrimitiveExtension`, both `KHR_materials_variants`), or a bare vendor name that would
say too little (`BufferViewMeshoptExtension`, `MeshPrimitiveDracoExtension`).
_Avoid_: requiring an `…Extension` suffix — most of these types correctly have none

**Extensions container**:
The type of a glTF object's `Extensions` property, named `<Object>Extensions` and deriving from
`AdditionalPropertyContainer` — `MaterialExtensions` is the container for `Material.Extensions`.
It carries a typed property per glTF extension glTFast consumes itself and keeps the rest as
**additional properties**. Here `Extensions` is the plural noun matching the JSON key, *not* the .NET
convention for a class holding C# extension methods.
_Avoid_: reading `<Object>Extensions` as C# extension methods; calling it an extension "wrapper"

**Extension-method class**:
A static class holding C# extension methods, named per .NET convention `<Subject>Extensions`.
Reusable only where the subject cannot own an extensions container: `MeshoptFilterExtensions` and
`MeshoptModeExtensions` extend enums, and `AttributesExtensions` extends `Attributes`, which the
specification defines as "a plain JSON object" of accessor indices — not a glTF object, so it has no
`extensions`. A subject that *is* a glTF object already gives its container that name, so an
extension-method class for one needs a different name.
_Avoid_: naming a new extension-method class after a glTF object that owns a container

**Additional property**:
A property present in the JSON but not modelled by a typed member — an unrecognized glTF extension,
`extras`, or an application-specific key. Preserved verbatim so it survives a round trip.
_Avoid_: unknown property, custom property, extra

**Index**:
A glTF object's reference to another glTF object, given as its position in the corresponding `Root` array.
Absent when `null`; there is no sentinel value.
_Avoid_: id, handle, reference, pointer

**Import add-on**:
A component extending an import with behavior glTFast does not provide itself, most often support for
a glTF extension. The registered `ImportAddon` creates one `ImportAddonInstance` per `GltfImport`, and
that instance is what implements the hook interfaces the import calls.
_Avoid_: extension (here that is a **glTF extension**), plug-in, Unity's meaning of "add-on" (an
installable package)

**Lease**:
A claim on a glTF asset's buffer memory, which the import keeps alive for as long as at least
one lease has not been disposed. An `IGltfBufferData` *is* the lease, so disposing it is what ends
the claim.
_Avoid_: reference count (leases are counted, buffers are not), pin, handle, subscription
