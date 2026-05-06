# SubMeshIndexFormats

## Description

Contains a single mesh with four primitives that mix index component types and vertex attribute layouts:

| Primitive | Indices                  | Attributes              |
| --------- | ------------------------ | ----------------------- |
| 0         | `UNSIGNED_SHORT` (5123)  | POSITION                |
| 1         | `UNSIGNED_SHORT` (5123)  | POSITION + TEXCOORD_0   |
| 2         | `UNSIGNED_INT`   (5125)  | POSITION                |
| 3         | `UNSIGNED_INT`   (5125)  | POSITION + TEXCOORD_0   |

Because the four primitives have incompatible vertex buffer layouts, glTFast is expected to split them across two distinct Unity meshes.

All four primitives share a single 12-vertex POSITION accessor (and a single TEXCOORD_0 accessor with the same count). Each primitive's indices are offset to its slice of the shared vertex range:

| Primitive | Index range |
| --------- | ----------- |
| 0         | 0..2        |
| 1         | 3..5        |
| 2         | 6..8        |
| 3         | 9..11       |

## Legal

Model: *SubMeshIndexFormats*

Attribution notice for the files directly associated with the above-referenced model in this directory tree, including all text, image and binary files.

&copy; 2026, Unity Technologies and the glTFast authors. Licensed under Apache 2.0. See [license file at root](https://github.com/Unity-Technologies/com.unity.cloud.gltfast/blob/main/LICENSE.md).
