// SPDX-FileCopyrightText: 2026 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

#if MESHOPT_IS_RECENT

using System;
using Meshoptimizer;
using UnityEngine.Scripting.APIUpdating;

namespace Unity.Cloud.Gltfast.Schema
{
    [MovedFrom(true, sourceNamespace: "GLTFast.Schema", sourceAssembly: "glTFast")]
    public static class MeshoptModeExtensions
    {
        public static Mode ToMeshoptimizerMode(this MeshoptMode mode)
        {
            return mode switch
            {
                MeshoptMode.Undefined => Mode.Undefined,
                MeshoptMode.Attributes => Mode.Attributes,
                MeshoptMode.Triangles => Mode.Triangles,
                MeshoptMode.Indices => Mode.Indices,
                _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
            };
        }
    }
}
#endif // MESHOPT_IS_RECENT
