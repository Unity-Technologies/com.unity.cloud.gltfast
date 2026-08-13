// SPDX-FileCopyrightText: 2026 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using UnityEngine.Scripting.APIUpdating;

namespace Unity.Cloud.Gltfast.Objects
{
    /// <summary>
    /// Vertex attribute filter to be applied
    /// </summary>
    [MovedFrom(true, sourceNamespace: "GLTFast.Schema", sourceAssembly: "glTFast")]
    public enum MeshoptFilter
    {
        /// <summary>
        /// No filter should be applied
        /// </summary>
        None,
        /// <summary>
        /// Apply octahedral filter, usually for normals
        /// </summary>
        Octahedral,
        /// <summary>
        /// Apply quaternion filter, usually for rotations
        /// </summary>
        Quaternion,
        /// <summary>
        /// Apply exponential filter, usually for positional data
        /// </summary>
        Exponential
    }
}
