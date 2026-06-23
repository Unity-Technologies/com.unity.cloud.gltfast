---
uid: doc-upgrade-guides
---

# Upgrade Guides

These guides will help you upgrade your project to use the latest version of *Unity glTFast*. If you still encounter problems, help us improving this guide and *Unity glTFast* in general by reaching out by raising an issue.

## Repository Structure: Monorepo

The Git repository of *glTFast* used to have the package content only at its root level. Shortly after version 6.8.0 this was changed to a [Monorepo][Monorepo] structure where the package resides in a sub-folder (`/Packages/com.unity.cloud.gltfast`). This was done so that the repository can also host additional content like test projects and assets that improve the development experience. Read the [development guide](development.md) and [Repository Structure](sources.md#repository-structure) for details.

Users who installed *glTFast* via its package identifier/name won't notice a difference, but if you've [installed it via Git URL][GitPackageInstall] (usually for development purpose), you'll need to update the URL to include a `path` parameter like so:

```none
https://github.com/Unity-Technologies/com.unity.cloud.gltfast.git?path=/Packages/com.unity.cloud.gltfast
```

You can do this by manually editing the URL in the [project manifest][ProjectManifest].

## Upgrade to 7.0

The `glTFast.Newtonsoft` assembly will be removed when 7.0 leaves the experimental phase. Migrate to the main `glTFast` assembly now to avoid breakage at that cutover.

| Before | After |
|--------|-------|
| `using GLTFast.Newtonsoft;` | `using GLTFast;` |
| `GLTFast.Newtonsoft.GltfImport` | `GLTFast.GltfImport` |
| `using GLTFast.Newtonsoft.Schema;` | `using GLTFast.Schema;` |
| `GLTFast.Newtonsoft.Schema.Accessor`, `…Asset`, `…Material`, `…Node`, `…Root`, `…Mesh`, etc. | `GLTFast.Schema.Accessor`, `…Asset`, `…Material`, `…Node`, `…Root`, `…Mesh`, etc. |
| `GLTFast.Newtonsoft.Schema.IJsonObject` interface | `GLTFast.Schema.IGltfObject` interface (note: the interface itself was renamed) |

If your assembly definition referenced `glTFast.Newtonsoft`, replace the reference with `glTFast`.

### Schema enum properties wrapped in `EnumOrRawValue<TEnum>`

Several `GLTFast.Schema` properties that used to be enums or strings are now wrapped in [EnumOrRawValue&lt;TEnum&gt;](xref:GLTFast.Schema.EnumOrRawValue`1) so that values introduced by glTF extensions (and therefore unknown at build time) are preserved through deserialization and serialization.

| Property | Before | After |
| -------- | ------ | ----- |
| `Accessor.Type` | `GltfAccessorAttributeType` | `EnumOrRawValue<AccessorType>` |
| `AnimationChannelTarget.Path` | `AnimationPath` | `EnumOrRawValue<AnimationPath>` |
| `AnimationSampler.Interpolation` | `Interpolation` | `EnumOrRawValue<Interpolation>` |
| `Camera.Type` | `CameraType` | `EnumOrRawValue<CameraType>` |
| [Image.MimeType](xref:GLTFast.Schema.Image.MimeType) | `string` | [EnumOrRawValue&lt;ImageMimeType&gt;](xref:GLTFast.Schema.EnumOrRawValue`1) |
| `Material.AlphaMode` | `AlphaMode` | `EnumOrRawValue<AlphaMode>` |
| `LightPunctual.Type` | `LightType` | `EnumOrRawValue<LightType>` |
| `Material.AlphaMode` | `AlphaMode` | `EnumOrRawValue<AlphaMode>` |

Reading: access the known enum via `.Value`; an unknown string is exposed as a UTF-8 byte sequence in `.RawValue`.

> [!TIP]
> Writing: an implicit conversion from the enum exists, so existing assignments such as `material.AlphaMode = AlphaMode.Blend;` continue to compile unchanged.

#### Image MIME type

| Before | After |
| ------ | ----- |
| `if (image.MimeType == "image/png") …` | `if (image.MimeType == ImageMimeType.Png) …` |
| `if (string.IsNullOrEmpty(image.MimeType)) …` | `if (image.MimeType.Value == ImageMimeType.Undefined && image.MimeType.RawValue == null) …` |
| `image.MimeType = "image/png";` | `image.MimeType = ImageMimeType.Png;` (uses implicit enum conversion) |

The legacy `image/ktx` MIME string is no longer mapped to `ImageFormat.Ktx`. Per the glTF 2.0 specification and `KHR_texture_basisu`, use `image/ktx2`. Assets carrying the bare `image/ktx` will round-trip via `RawValue`, but `ImageFormatExtensions.FromMimeType` now resolves them to `ImageFormat.Unknown`.

#### glTF extension lists

[Root.ExtensionsUsed](xref:GLTFast.Schema.Root.ExtensionsUsed) and [Root.ExtensionsRequired](xref:GLTFast.Schema.Root.ExtensionsRequired) changed from `string[]` to `List<`[EnumOrRawValue&lt;Extension&gt;](xref:GLTFast.Schema.EnumOrRawValue`1)`>`. Recognized extension names deserialize directly into the [Extension](xref:GLTFast.Extension) enum and never allocate a managed `string`; names not known at build time are preserved as UTF-8 bytes in `.RawValue`.

Membership checks via the implicit enum conversion:

| Before | After |
| ------ | ----- |
| `Array.IndexOf(root.ExtensionsRequired, "KHR_lights_punctual") >= 0` | `root.ExtensionsRequired.Contains(Extension.LightsPunctual)` |
| `root.ExtensionsUsed.Length` | `root.ExtensionsUsed.Count` |

Iteration (e.g. to log every entry):

```csharp
// Before
foreach (var name in root.ExtensionsUsed) Debug.Log(name);

// After
foreach (var extension in root.ExtensionsUsed) Debug.Log(extension.GetName());
```

Constructing from code (e.g. for export):

| Before | After |
| ------ | ----- |
| `root.ExtensionsUsed = new[] { "KHR_materials_unlit" };` | `root.ExtensionsUsed = new List<EnumOrRawValue<Extension>> { Extension.MaterialsUnlit };` (uses implicit enum conversion) |

### Schema index properties wrapped in `int?`

Schema properties that hold indices into root-level arrays (and are optional in the glTF specification) changed from `int` to `int?`. The legacy `-1` sentinel that previously signaled "not set" is gone; an absent value is now `null`.

This applies to (non-exhaustive — see the changelog for the full list):

- `Accessor.BufferView`
- `BufferView.ByteStride` (and the [IBufferView](xref:GLTFast.Schema.IBufferView) interface)
- `Image.BufferView`
- `MeshPrimitive.Indices`, `MeshPrimitive.Material`
- `Attributes` (`POSITION`, `NORMAL`, `TANGENT`, `TEXCOORD_*`, `COLOR_0`, `JOINTS_0`, `WEIGHTS_0`) and `MorphTarget` (`POSITION`, `NORMAL`, `TANGENT`)
- `Node.Mesh`, `Node.Skin`, `Node.Camera`
- `Root.Scene`
- `Skin.InverseBindMatrices`, `Skin.Skeleton`
- `Texture.Sampler`, `Texture.Source`
- `TextureInfo.Index`, `TextureTransform.TexCoord`
- `NodeLightsPunctual.Light`, `TextureBasisUniversal.Source`
- `InstancesAttributes` (`TRANSLATION`, `ROTATION`, `SCALE`)

Reading: replace `x >= 0` checks with `x.HasValue`, and dereference via `x.Value`. The C# `is int` pattern combines both:

| Before | After |
| ------ | ----- |
| `if (primitive.Material >= 0) { var m = gltf.GetMaterial(primitive.Material); … }` | `if (primitive.Material is int materialIndex) { var m = gltf.GetMaterial(materialIndex); … }` |
| `if (node.Mesh >= 0) Use(node.Mesh);` | `if (node.Mesh.HasValue) Use(node.Mesh.Value);` |
| `var idx = textureInfo.Index;` (was `int`) | `var idx = textureInfo.Index;` (now `int?`) — use `.Value` at the point of use |

Writing: assign an `int` directly (implicit conversion to `int?` works), or `null` to clear:

| Before | After |
| ------ | ----- |
| `node.Mesh = 3;` | `node.Mesh = 3;` (unchanged) |
| `node.Mesh = -1;` | `node.Mesh = null;` |

The corresponding `GltfSerialize` writers omit the property when `null`. Existing code that left a property at its default (`-1`) for "not set" should now leave it at `null` (the new default).

#### Related API signature changes

| Member | Before | After |
| ------ | ------ | ----- |
| [Texture.GetImageIndex](xref:GLTFast.Schema.Texture.GetImageIndex) | `int` | `int?` |
| `MeshPrimitive.GetMaterialIndex` | `int` | `int?` |
| `IMaterialsVariantsSlot.GetMaterialIndex` | `int` | `int?` |
| `MeshResult.materialIndices` | `int[]` | `int?[]` |
| `IGltfBuffers.GetBufferView` / `GetAccessorAndData` `byteStride` out param | `int` | `int?` |

Custom implementations of [IMaterialsVariantsSlot](xref:GLTFast.IMaterialsVariantsSlot), [IBufferView](xref:GLTFast.Schema.IBufferView) or [IGltfBuffers](xref:GLTFast.IGltfBuffers) need to update their member signatures accordingly.

### `BufferView.Target` typed as `BufferViewTarget`

[BufferView.Target](xref:GLTFast.Schema.BufferView.Target) is now [BufferViewTarget](xref:GLTFast.Schema.BufferViewTarget) instead of `int`. The enum members carry the WebGL constants, and `BufferViewTarget.Undefined` (value `0`) represents the absent target. Comparisons against the raw integer no longer compile; use the enum members directly.

| Before | After |
| ------ | ----- |
| `bufferView.Target = 34962;` | `bufferView.Target = BufferViewTarget.ArrayBuffer;` |
| `bufferView.Target = (int)BufferViewTarget.ElementArrayBuffer;` | `bufferView.Target = BufferViewTarget.ElementArrayBuffer;` |
| `if (bufferView.Target > 0) …` | `if (bufferView.Target != BufferViewTarget.Undefined) …` |
| `var raw = bufferView.Target;` (was `int`) | `var raw = (int)bufferView.Target;` if you still need the WebGL constant |

### Schema collection properties moved from `T[]` to `List<T>`

Variable-length collection properties on `GLTFast.Schema` types are now `List<T>` instead of `T[]`, completing the migration started with `Root.Accessors`, `Root.Materials`, `Mesh.Primitives`, etc. Fixed-size mathematical arrays (`Node.Matrix`/`.Rotation`/`.Scale`/`.Translation`, `TextureTransform.Offset`/`.Scale`) keep their array type — their length is part of the contract enforced by the JSON converters.

| Property | Before | After |
| -------- | ------ | ----- |
| [LightsPunctual.Lights](xref:GLTFast.Schema.LightsPunctual.Lights) | `LightPunctual[]` | `List<LightPunctual>` |
| `MaterialVariantsMapping.Variants` | `int[]` | `List<int>` |
| `MeshExtras.TargetNames` | `string[]` | `List<string>` |
| [MeshPrimitive.Targets](xref:GLTFast.Schema.MeshPrimitive.Targets) | `MorphTarget[]` | `List<MorphTarget>` |
| [Node.Children](xref:GLTFast.Schema.Node.Children) | `uint[]` | `List<uint>` |
| [Root.Buffers](xref:GLTFast.Schema.Root.Buffers) | `Buffer[]` | `List<Buffer>` |
| [Scene.Nodes](xref:GLTFast.Schema.Scene.Nodes) | `uint[]` | `List<uint>` |
| [Skin.Joints](xref:GLTFast.Schema.Skin.Joints) | `uint[]` | `List<uint>` |

Reading is mostly unchanged — indexing (`[i]`) and `foreach` work the same, but `.Length` becomes `.Count`:

| Before | After |
| ------ | ----- |
| `for (var i = 0; i < primitive.Targets.Length; i++) …` | `for (var i = 0; i < primitive.Targets.Count; i++) …` |
| `if (scene.Nodes is { Length: > 0 }) …` | `if (scene.Nodes is { Count: > 0 }) …` |
| `var bones = new Transform[skin.Joints.Length];` | `var bones = new Transform[skin.Joints.Count];` |

Constructing from code (e.g. for export) uses list initializers:

| Before | After |
| ------ | ----- |
| `node.Children = new[] { 1u, 2u };` | `node.Children = new List<uint> { 1u, 2u };` |
| `mapping.Variants = new[] { 0, 1 };` | `mapping.Variants = new List<int> { 0, 1 };` |

#### Related API signature changes

Public method parameters that used to take `uint[]` / `string[]` were updated based on how the receiver consumes the value.

**Borrowed inputs** (consumed during the call; never stored) take `IReadOnlyList<…>`. Arrays and Lists both satisfy this, so existing call sites that pass arrays continue to compile; only custom *implementations* of the interface need to update their signatures.

| Member | Before | After |
| ------ | ------ | ----- |
| [IInstantiator.BeginScene](xref:GLTFast.IInstantiator.BeginScene*) `rootNodeIndices` | `uint[]` | `IReadOnlyList<uint>` |
| [IInstantiator.EndScene](xref:GLTFast.IInstantiator.EndScene*) `rootNodeIndices` | `uint[]` | `IReadOnlyList<uint>` |
| [IInstantiator.AddPrimitive](xref:GLTFast.IInstantiator.AddPrimitive*) `joints` | `uint[]` | `IReadOnlyList<uint>` |
| `GameObjectInstantiator.MeshAddedDelegate` `joints` | `uint[]` | `IReadOnlyList<uint>` |
| `IAnimationProcessor.AddMorphTargetWeightCurves` `morphTargetNames` | `string[]` | `IReadOnlyList<string>` |

**Adopted inputs** (stored in the schema and serialized later) take `List<uint>`. Ownership of the list transfers to the writer — the caller must not mutate it after the call. An obsolete `uint[]` overload preserves back-compat by copying the array into a new list; callers should migrate to passing a `List<uint>` they will not modify further.

| Member | Before | After |
| ------ | ------ | ----- |
| [IGltfWritable.AddNode](xref:GLTFast.Export.IGltfWritable.AddNode*) `children` | `uint[]` | `List<uint>` (ownership transferred) |
| [IGltfWritable.AddScene](xref:GLTFast.Export.IGltfWritable.AddScene*) `nodes` | `uint[]` | `List<uint>` (ownership transferred) |
| [IGltfWritable.AddMeshToNode](xref:GLTFast.Export.IGltfWritable.AddMeshToNode*) `joints` | `uint[]` | `List<uint>` (ownership transferred) |

```csharp
// Before
var children = new uint[] { 1, 2, 3 };
writer.AddNode(children: children);

// After — build a List you no longer touch
var children = new List<uint> { 1, 2, 3 };
writer.AddNode(children: children);
// Don't mutate `children` here; the writer now owns it.
```

Custom subclasses or implementations of these interfaces and delegates need to update their member signatures to match.

### Export image format and MIME type

The redundant `GLTFast.Export.ImageFormat` enum was removed and merged into the canonical [GLTFast.ImageFormat](xref:GLTFast.ImageFormat). The enum value `Jpg` was renamed to `Jpeg` to match.

| Before | After |
| ------ | ----- |
| `GLTFast.Export.ImageFormat` | [GLTFast.ImageFormat](xref:GLTFast.ImageFormat) |
| `ImageFormat.Jpg` | `ImageFormat.Jpeg` |

[Export.ImageExportBase.MimeType](xref:GLTFast.Export.ImageExportBase.MimeType) changed from `string` to [ImageMimeType](xref:GLTFast.Schema.ImageMimeType). Custom subclasses overriding the property must return the enum directly.

| Before | After |
| ------ | ----- |
| `public override string MimeType => "image/png";` | `public override ImageMimeType MimeType => ImageMimeType.Png;` |

The internal helpers `GLTFast.Export.Constants.mimeTypePNG` and `mimeTypeJPG` were removed; use `ImageMimeType.Png` / `ImageMimeType.Jpeg` instead.

## Upgrade to 6.0

Use Unity 2021.3.46f1 or newer only.

*GltfAnimation* was renamed to [Animation](xref:GLTFast.Schema.Animation).

## Unity Fork

With the release of version 5.2.0 the package name and identifier were changed to *Unity glTFast* (`com.unity.cloud.gltfast`) for the following reasons:

- Better integration into Unity internal development processes (including quality assurance and support)
- Distribution via the Unity Package Manager (no scoped libraries required anymore)

For now, both the Unity variant and the original version will receive updates.

### Transition to *Unity glTFast*

The C# namespaces are identical between the variants, so all you need to do is:

- Removed original *glTFast* (with package identifier `com.atteneder.gltfast`).
- [Install *Unity glTFast*](installation.md) (`com.unity.cloud.gltfast`).
- Update assembly definition references (if your project had any).
- Update any dependencies in your packages manifest (if your package had any)

#### Transition Depending Packages

Unity forks have been created for *KtxUnity* and *DracoUnity* as well. If you've used them in conjunction with *glTFast*, you need to transition them to the Unity variants as well.

See their respective upgrade guides

- Upgrade to [*KTX for Unity*](https://docs.unity3d.com/Packages/com.unity.cloud.ktx@3.2/manual/upgrade-guide.html)
- Upgrade to [*Draco for Unity*](https://docs.unity3d.com/Packages/com.unity.cloud.draco@5.0/manual/upgrade-guide.html)

### Keep using the original glTFast

The original *glTFast* (`com.atteneder.gltfast`) as well as *KtxUnity* and *DracoUnity* will still receive identical updates for now. You may choose to continue using them.

If you've installed the packages via the installer script (i.e. via [OpenUPM][OpenUPM] scoped registry - the recommended way), you don't need to change anything. You'll receive updates as usual.

If you've cloned the package via GIT, make sure to switch to the `openupm` branch to make sure the package identifier and name remain the original.

See [Original *glTFast*](./Original.md) for instructions to install the original version from scratch.

## Upgrade to 5.0

### General

The API in general was changed considerably to conform closer to Unity's coding standard and the Microsoft's Framework Design Guidelines. If you have custom code, you likely need to update parts of it to the new API. Some notable items:

- PascalCase on properties (first char is upper-case)
- Removed direct access to fields (replaced by getter-property, where required)
- More consistent naming of assemblies, namespaces, classes, constants, static members, etc.
  - Renamed and moved classes/structs to different files.
- Auto-formatted code for consistent line-endings and code look (a necessary, one-time evil; might be troublesome if you forked *Unity glTFast*)

If you have issues, please also go through the 5.0.0 changelog entry and feel free to reach out for support.

### Moved or Renamed Types

Some assemblies, classes, structs and enum types have been renamed or moved. Make sure you adopt your code appropriately. All entries are within the `GLTFast` namespace.

- Refactored Assembly Definitions
  - `glTFastSchema` was merged into `glTFast` and thus removed
  - `glTFastEditor` was renamed to `glTFast.Editor`
  - `glTFastEditorTests` was renamed to `glTFast.Editor.Tests`
- Moved logging related code into `GLTFast.Logging` namespace
- Replaced `CollectingLogger.item` with `.Count` and `.Items` iterator
- `GameObjectInstantiator.SceneInstance` is now `GameObjectSceneInstance`
- `ImportSettings.NameImportMethod` is now `NameImportMethod`
- Converted  `GameObjectInstantiator.Settings` to `InstantiationSettings`
- `InstantiationSettings.SceneObjectCreation` is now `SceneObjectCreation`
- Converted properties that were hiding conversion logic or caching into methods
  - `Accessor`: `typeEnum` to `GetAttributeType`/`SetAttributeType`
  - `BufferView`: `modeEnum` to `GetMode`
  - `BufferView`: `filterEnum` to `GetFilter`
  - `AnimationChannelTarget`: `pathEnum` to `GetPath`
  - `AnimationSampler`: `interpolationEnum` to `GetInterpolationType`
  - `Camera`: `typeEnum` to `GetCameraType`/`SetCameraType`
  - `LightPunctual`: `typeEnum` to `GetLightType`/`SetLightType`
  - `Material`: `alphaModeEnum` to `GetAlphaMode`/`SetAlphaMode`
- `HttpHeader`'s properties are readonly now. A constructor was added as compensation.
- Obsolete code that was finally removed
  - `GltfImport.Destroy` (was renamed to `GltfImport.Dispose`)
  - `GLTFast.GltFast` (was renamed to `GltfImport`)
  - `GltfImport.InstantiateGltf` (was replaced by `InstantiateMainScene` and `InstantiateScene`)

### Async Scene Instantiation

The addition of `GltfImport.InstantiateSceneAsync` and `GltfImport.InstantiateMainSceneAsync` now provides an asynchronous way of instantiating glTF&trade; scenes. For large scenes this means that the instantiation can be spread over multiple frames, resulting in a smoother frame rate.

The existing, synchronous instantiation methods `GltfImport.InstantiateScene` and `GltfImport.InstantiateMainScene` (including overloads) have been marked obsolete and will be removed eventually. Though they still work, it's recommended to update your code to use the async variants.

Since loading a glTF (the step before instantiation) has been async before, chances are high your enclosing method is already async, as it should be.

```csharp
async void Start() {
    var gltf = new GltfImport();
    var success = await gltf.Load("file:///path/to/file.gltf");
    if(!success) return;

    // Old, sync instantiation
    success = gltf.InstantiateMainScene(transform);
    if(success) Debug.Log("glTF instantiated successfully!");
}
```

All you now have to do is switch to the async method and await it.

```csharp
async void Start() {
    var gltf = new GltfImport();
    var success = await gltf.Load("file:///path/to/file.gltf");
    if(!success) return;

    // New, async instantiation
    success = await gltf.InstantiateMainSceneAsync(transform);
    if(success) Debug.Log("glTF instantiated successfully!");
}
```

### `IInstantiator` Changes

`IInstantiator.BeginScene` signature dropped third parameter `AnimationClip[] animationClips`. As replacement `IInstantiator.AddAnimation` was added. It's only available when built-in Animation module is enabled.

### Texture Support

The built-in packages [*Unity Web Request Texture*][uwrt] and [*Image Conversion*][ImgConv] provide support for PNG/Jpeg texture import and export. They are not a hard requirement anymore, so you…

- …**can** disable them if you don't require PNG/Jpeg texture support
- …**need to** enable them in the Package Manager if you require PNG/Jpeg texture support

See [*Texture Support* in Project Setup](ProjectSetup.md#materials-and-shader-variants) for details.

### Play Animation

Previously the first animation clip would start playing by default, which is not the case anymore. There is a way to restore animation auto-play, depending on how you load glTFs.

#### Play Automatically with the `GltfAsset` component

There's a new property `Play Automatically`, which is checked by default. You shouldn't experience change in behavior, unless you disable this setting.

#### Play Automatically when loading from script

You have to explicitly use a [`GameObjectInstantiator`][GameObjectInstantiator]. It provides a [`SceneInstance`][GameObjectSceneInstance] object which has a `legacyAnimation` property, referencing the `Animation` component. Use it to start or stop playback of any of the animation clips it holds.

```csharp
async void Start() {

    var gltfImport = new GltfImport();
    await gltfImport.Load("test.gltf");
    var instantiator = new GameObjectInstantiator(gltfImport,transform);
    var success = gltfImport.InstantiateMainScene(instantiator);
    if (success) {

        // Get the SceneInstance to access the instance's properties
        var sceneInstance = instantiator.SceneInstance;

        // Play the default (i.e. the first) animation clip
        var legacyAnimation = instantiator.SceneInstance.LegacyAnimation;
        if (legacyAnimation != null) {
            legacyAnimation.Play();
        }
    }
}
```

### `IMaterialGenerator` API change

Rendering meshes with points topology/draw mode (Point clouds) requires special shaders (with a `PSIZE` vertex output). For that reason the `pointsSupport` parameter (`bool`; optional) was added to

- `IMaterialGenerator.GetDefaultMaterial`
- `IMaterialGenerator.GenerateMaterial`

If `pointsSupport` is true, the generated material has to support meshes with points topology.

The bundled default material generators don't support point cloud rendering yet (with the exception of the built-in unlit shader), but this change will allow implementing that in the future (or in custom implementations).

If a material is used on mesh primitives with different draw modes (e.g. on triangles as well as points), still just one Unity material with points support will be created and used for all of them.

### Misc. API Changes

`RenderPipelineUtils.DetectRenderPipeline()` turned to `RenderPipelineUtils.RenderPipeline`

## Upgrade to 4.5

New shader graphs are used with certain Universal and High Definition render pipeline versions, so projects that included *Unity glTFast*'s shaders have to check and update their included shaders or shader variant collections (see [Materials and Shader Variants](ProjectSetup.md#materials-and-shader-variants) for details).

## Upgrade to 4.x

### Coordinate system conversion change

When upgrading from an older version to 4.x or newer the most notable difference is the imported models' orientation. They will appear 180° rotated around the up-axis (Y).

![GltfAsset component][gltfast3to4]

To counter-act this in applications that used older versions of *Unity glTFast* before, make sure you rotate the parent `Transform` by 180° around the Y-axis, which brings the model back to where it should be.

This change was implemented to conform more closely to the [glTF specification][gltf-spec-coords], which says:

> The front of a glTF asset faces +Z.

In Unity, the positive Z axis is also defined as forward, so it makes sense to align those and so the coordinate space conversion from glTF's right-handed to Unity's left-handed system is performed by inverting the X-axis (before the Z-axis was inverted).

### New Logging

During loading and instantiation, *Unity glTFast* used to log messages (infos, warnings and errors) directly to Unity's console. The new logging solution allows you to:

- Omit *Unity glTFast* logging completely to avoid clogging the message log
- Retrieve the logs to process them (e.g. reporting analytics or inform the user properly)

See [Logging](ImportRuntime.md#logging) above.

### Scene based instantiation

*Unity glTFast* 4.0 introduces scene-based instantiation. While most glTF assets contain only one scene they could consist of multiple scenes and optionally have one of declared the default scene.

The old behavior was, that all of the glTF's content was loaded. The new interface allows you to load the default scene or any scene of choice. If none of the scenes was declared the default scene (by setting the `scene` property), no objects are instantiated (as defined in the glTF specification).

[`GltfImport`][GltfImport] (formerly named `GLTFast`) provides the following properties and methods for scene instantiation:

```csharp
// To get the number of scenes
public int sceneCount;
// Returns the default scene's index
public int? defaultSceneIndex;
// Methods for instantiation
public bool InstantiateMainScene( Transform parent );
public bool InstantiateMainScene(IInstantiator instantiator);
public bool InstantiateScene( Transform parent, int sceneIndex = 0);
public bool InstantiateScene( IInstantiator instantiator, int sceneIndex = 0 );
```

Please look at [`GltfAsset`][GltfAsset] for a reference implementation and look at the properties'/methods' XML documentation comments in the source code for details.

### Custom material generation

Creating a custom `IMaterialGenerator` was mainly about implementing the following method:

```csharp
Material GenerateMaterial(Schema.Material gltfMaterial, ref Schema.Texture[] textures, ref Schema.Image[] schemaImages, ref Dictionary<int, Texture2D>[] imageVariants);
```

You'd receive all textures/images/image variants to pick from. This was changed to:

```csharp
Material GenerateMaterial(Schema.Material gltfMaterial, IGltfReadable gltf);
```

[`IGltfReadable`][IGltfReadable] is an interface that allows you to query all loaded textures and much more, allowing more flexible implementations. Please look at the source code.

In the future materials can be created before textures are available/downloaded to speed up the loading.

## Trademarks

*Unity&reg;* is a registered trademark of [Unity Technologies][unity].

*Khronos&reg;* is a registered trademark and *glTF&trade;* is a trademark of [The Khronos Group Inc][khronos].

[GameObjectInstantiator]: xref:GLTFast.GameObjectInstantiator
[GameObjectSceneInstance]: xref:GLTFast.GameObjectSceneInstance
[GitPackageInstall]: https://docs.unity3d.com/Manual/upm-ui-giturl.html
[gltf-spec-coords]: https://www.khronos.org/registry/glTF/specs/2.0/glTF-2.0.html#coordinate-system-and-units
[GltfAsset]: xref:GLTFast.GltfAsset
[gltfast3to4]: Images/gltfast3to4.png  "3D scene view showing BoomBoxWithAxes model twice. One with the legacy axis conversion and one with the new orientation"
[GltfImport]: xref:GLTFast.GltfImport
[Monorepo]: https://en.wikipedia.org/wiki/Monorepo
[ProjectManifest]: https://docs.unity3d.com/Manual/upm-git.html
[IGltfReadable]: xref:GLTFast.IGltfReadable
[ImgConv]: https://docs.unity3d.com/2021.3/Documentation/ScriptReference/UnityEngine.ImageConversionModule.html
[OpenUPM]: https://openupm.com/
[khronos]: https://www.khronos.org
[unity]: https://unity.com
[uwrt]: https://docs.unity3d.com/2021.3/Documentation/ScriptReference/UnityEngine.UnityWebRequestTextureModule.html
