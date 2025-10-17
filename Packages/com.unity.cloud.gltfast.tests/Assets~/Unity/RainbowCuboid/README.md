# Rainbow Cuboid

## Description

Contains an animated, skinned mesh with morph targets and the following vertex attributes:

- Positions
- Normals
- Tangents
- Texture coordinates
- Colors
- Morph target indices/weights

### Variants

- [RainbowCuboid.gltf](original/RainbowCuboid.gltf) is the original.
  - [*meshopt*][meshopt] compressed variants of the original glTF.
    - [RainbowCuboid-meshopt-c.gltf](meshopt-c/RainbowCuboid-meshopt-c.gltf)
    - [RainbowCuboid-meshopt-cc.gltf](meshopt-cc/RainbowCuboid-meshopt-cc.gltf)
    - [RainbowCuboid-meshopt-c-vpn.gltf](meshopt-c-vpn/RainbowCuboid-meshopt-c-vpn.gltf)
    - [RainbowCuboid-meshopt-cc-vpn.gltf](meshopt-cc-vpn/RainbowCuboid-meshopt-cc-vpn.gltf)
  - [RainbowCuboid-Draco.gltf](draco/RainbowCuboid-Draco.gltf) – [*Draco&trade;*][draco] compressed variant
- [RainbowCuboidSubMesh.gltf](submesh/RainbowCuboidSubMesh.gltf) is a variant of the original where the mesh has been split up into two primitives/sub-meshes.
  - [*meshopt*][meshopt] compressed variants
    - [RainbowCuboidSubMesh-meshopt-c.gltf](./submesh-meshopt-c/RainbowCuboidSubMesh-meshopt-c.gltf)
    - [RainbowCuboidSubMesh-meshopt-cc.gltf](./submesh-meshopt-cc/RainbowCuboidSubMesh-meshopt-cc.gltf)
    - [RainbowCuboidSubMesh-meshopt-c-vpn.gltf](./submesh-meshopt-c-vpn/RainbowCuboidSubMesh-meshopt-c-vpn.gltf)
    - [RainbowCuboidSubMesh-meshopt-cc-vpn.gltf](./submesh-meshopt-cc-vpn/RainbowCuboidSubMesh-meshopt-cc-vpn.gltf)
  - [RainbowCuboidSubMesh-Draco.gltf](submesh-draco/RainbowCuboidSubMesh-Draco.gltf) – [*Draco&trade;*][draco] compressed variant

## Compression

The *meshopt* compressed variants have been created with [gltfpack][gltfpack] [0.25](https://github.com/zeux/meshoptimizer/releases/tag/v0.25) and the following parameters:

```shell
 gltfpack -c       -i original/RainbowCuboid.gltf  -o meshopt-c/RainbowCuboid-meshopt-c.gltf
 gltfpack -cc      -i original/RainbowCuboid.gltf  -o meshopt-cc/RainbowCuboid-meshopt-cc.gltf
 gltfpack -c -vpn  -i original/RainbowCuboid.gltf  -o meshopt-c-vpn/RainbowCuboid-meshopt-c-vpn.gltf
 gltfpack -cc -vpn -i original/RainbowCuboid.gltf  -o meshopt-cc-vpn/RainbowCuboid-meshopt-cc-vpn.gltf
```

The *Draco* compressed variants have been created with [glTF Transform] CLI tool v4.2.1 and the following parameters:

```shell
gltf-transform draco RainbowCuboid.gltf RainbowCuboid-Draco.gltf
gltf-transform draco RainbowCuboidSubMesh.gltf glTF-Draco/RainbowCuboidSubMesh-Draco.gltf
```

## Extensions used

- [EXT_meshopt_compression][meshoptExt]
- [KHR_draco_mesh_compression][dracoExt]

## Legal

Model: *Rainbow Cuboid*

Attribution notice for the files directly associated with the above-referenced model in this directory tree, including all text, image and binary files.

&copy; 2024, Unity Technologies and the glTFast authors. Licensed under Apache 2.0. See [license file at root](https://github.com/Unity-Technologies/com.unity.cloud.gltfast/blob/main/LICENSE.md).

## Trademarks

*Draco&trade;* is a trademark of [*Google LLC*][GoogleLLC].

[draco]: https://google.github.io/draco
[dracoExt]: https://github.com/KhronosGroup/glTF/tree/main/extensions/2.0/Khronos/KHR_draco_mesh_compression
[GoogleLLC]: https://about.google/
[meshopt]: https://github.com/zeux/meshoptimizer
[meshoptExt]: https://github.com/KhronosGroup/glTF/tree/main/extensions/2.0/Vendor/EXT_meshopt_compression
[gltfpack]: https://github.com/zeux/meshoptimizer/tree/master/gltf
[glTF Transform]: https://gltf-transform.dev/
