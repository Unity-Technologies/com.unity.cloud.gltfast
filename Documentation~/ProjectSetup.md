---
uid: doc-project-setup
---

# Project Setup

This page explains how projects can be setup to fit your needs and tweaked in detail. As a prerequisite you first need to [install *glTFast*](installation.md).

## Materials and Shader Variants

For runtime import *Unity glTFast* uses custom shader graphs or shaders for rendering glTF&trade; materials. Depending on the properties of a glTF material (and extensions it relies on), a specific [shader variant][shader-variants] will get used. In the Editor this shader variant will be built on-demand, but in order for materials to work in your build, you **have** to make sure all shader variants that you're going to need are included.

Including all possible variants is the safest approach, but can make your build very big. There's another way to find the right subset, if you already know what files you'll expect:

- Run your scene that loads all glTFs you expect in the editor.
- Go to **Edit > Project Settings > Graphics**
- At the bottom end you'll see the *Shader Preloading* section
- Save the currently tracked shaders/variants to an asset
- Take this ShaderVariantCollection asset and add it to the *Preloaded Shaders* list

An alternative way is to create placeholder materials for all feature combinations you expect and put them in a "Resource" folder in your project.

Read the documentation about [Shader.Find](https://docs.unity3d.com/ScriptReference/Shader.Find.html) for details how to include shaders in builds. It's also recommended to learn more about [shader variants][shader-variants].

Depending on the Unity version and render pipeline in use, different shader graphs or shaders will be used.

- Shader graphs in `Runtime/Shader` for
  - [Universal Render Pipeline (URP)][URP]
  - [High-Definition Render Pipeline (HDRP)][HDRP]
  - [Built-In Render Pipeline (BiRP)][BiRP] (experimental opt-in; see below)
- Shader graphs in folder `Runtime/Shader/URP` for [URP] specific material types (e.g. clearcoat)
- Shader graphs in folder `Runtime/Shader/HDRP` for [HDRP] specific material types (e.g. StackLit)
- Shaders in folder `Runtime/Shader/Built-In` for [BiRP]
- Shared `Runtime/Shader/Includes` and `Runtime/Shader/SubGraphs` folders contain include files and shader subgraphs referenced by the shader graphs above; do not remove them when stripping unused content

### Missing Shader Variants

If a *glTFast* material renders correctly in Editor playmode, but does not in a player build, the cause is almost always one of two:

1. **The shader asset itself is missing from the build.** *glTFast* logs an error of the form *"Shader "&hellip;" is missing. Make sure to include it in the build"* and material generation returns `null` for the affected mesh primitive. The renderer ends up without a material entirely (renders solid magenta). Add the shader to *Always Included Shaders* (**Edit > Project Settings > Graphics**) or reference it from a Resources folder so the build pipeline picks it up.
2. **The shader is included, but the specific keyword variant requested at runtime was stripped from the build** by Unity's shader variant stripping. What the user actually sees as a result depends on the *Strict Shader Variant Matching* player setting (see below).

### Strict Shader Variant Matching

*Strict Shader Variant Matching* is a Unity engine setting. It is configured under **Edit > Project Settings > Player > Other Settings > Shader Settings** and only changes behavior in player builds.

- **Disabled (default):** Unity silently substitutes the closest available variant when an exact match for the requested keyword combination is missing. Materials still render, but *glTFast* features driven by keywords &mdash; normal mapping, alpha clipping, UV transforms, secondary UV sets, occlusion, transmission, double-sided rendering, etc. &mdash; may quietly produce wrong results without any console warning.
- **Enabled:** Unity refuses to substitute. The affected geometry renders with the **error (magenta) shader** and Unity logs an error naming the shader, subshader, pass and the requested keywords, making missing variants immediately visible.

Because *glTFast*'s shaders combine many keywords, even small stripping mistakes can mask bugs that only surface for specific glTF assets.

> [!TIP]
> Enabling *Strict Shader Variant Matching* while validating builds is recommended &mdash; it converts silent visual regressions into clear console errors.

### Shader Build Settings (Unity 6.3+)

Unity 6.3 introduced [Shader Build Settings](https://docs.unity3d.com/6000.3/Documentation/Manual/shader-variant-stripping.html) under **Edit > Project Settings > Graphics > Shader Build Settings** (also configurable per build target through Build Profiles). It lets you list keyword sets and apply a *Type Override* of either:

- `shader_feature` &mdash; strip variants of those keywords that are not statically reachable, or
- `dynamic_branch` &mdash; collapse variants into a runtime branch.

Both reduce variant count, build size, shader load time, and runtime memory usage.

Shader Build Settings **complements** `ShaderVariantCollection` / *Preloaded Shaders* workflow rather than replacing it: *Preloaded Shaders* warm up variants at runtime, while Shader Build Settings controls which variants exist in the build at all.

> [!CAUTION]
> Aggressive stripping can remove *glTFast* variants that are only requested for specific glTF assets. Pair Shader Build Settings with *Strict Shader Variant Matching* (see above) so missing variants are caught during build validation instead of shipping as silent visual regressions.

### Shader Graphs and the Built-In Render Pipeline

> This approach is experimental and has know shading issues

Built-In render pipe projects can optionally use the shader graphs instead of the Built-In shaders by:

- Installing Shader Graph version 12 or newer
- Adding `GLTFAST_BUILTIN_SHADER_GRAPH` to the list of scripting define symbols in the project settings

## Optional Packages

*glTFast* has soft-dependencies on some [optional packages](installation.md#optional-packages). By not installing those packages you might be able to reduce your final build size, so consider doing that.

For example, if you don't need PNG/Jpeg support (because you use only KTX&trade; 2.0 textures or no textures at all), you can disable the *Image Conversion* and *UnityWebRequestTexture* modules.

## Readable Mesh Data

By default *Unity glTFast* discards mesh data after it was uploaded to the GPU to free up main memory (see [`markNoLongerReadable`](https://docs.unity3d.com/ScriptReference/Mesh.UploadMeshData.html)). You can disable this globally by using the scripting define `GLTFAST_KEEP_MESH_DATA`.

Motivations for this might be using meshes as physics colliders amongst [other cases](https://docs.unity3d.com/ScriptReference/Mesh-isReadable.html).

## Safe Mode

Arbitrary (and potentially broken) input data is a challenge to software's robustness and safety. Some measurements to make *Unity glTFast* more robust have a negative impact on its performance though.

For this reason some pedantic safety checks in *Unity glTFast* are not performed by default. You can enable safe-mode by adding the scripting define `GLTFAST_SAFE` to your project.

Enable safe-mode if you are not in control over what content your application may end up loading and you cannot test up front.

## Disable Editor Import

By default, *Unity glTFast* provides Editor import for all files ending with `.gltf` or `.glb` via a `ScriptedImporter`.
If you experience conflicts with other packages that are offering `.gltf`/`.glb` import as well (e.g. [MixedRealityToolkit-Unity][MRTK]) or you simply want to disable Editor import,
add `GLTFAST_EDITOR_IMPORT_OFF` to the *Scripting Define Symbols* in the *Player Settings* and this feature will be turned off.

## Trademarks

*Unity&reg;* is a registered trademark of [Unity Technologies][Unity].

*Khronos&reg;* is a registered trademark and *glTF&trade;* is a trademark of [The Khronos Group Inc][Khronos].

*KTX&trade;* and the KTX logo are trademarks of the [The Khronos Group Inc][khronos].

[BiRP]: https://docs.unity3d.com/6000.6/Documentation/Manual/built-in-render-pipeline.html
[HDRP]: https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest/
[Khronos]: https://www.khronos.org
[MRTK]: https://github.com/microsoft/MixedRealityToolkit-Unity
[shader-variants]: https://docs.unity3d.com/Manual/shader-variants.html
[URP]: https://docs.unity3d.com/6000.6/Documentation/Manual/urp/urp-introduction.html
[Unity]: https://unity.com
