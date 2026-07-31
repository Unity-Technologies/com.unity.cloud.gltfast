// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using UnityEngine.Scripting.APIUpdating;

namespace Unity.Cloud.Gltfast.Schema
{
    /// <summary>
    /// Texture wrap mode.
    /// </summary>
    [MovedFrom(true, sourceNamespace: "GLTFast.Schema", sourceAssembly: "glTFast")]
    public enum WrapMode
    {
        /// <summary>Undefined</summary>
        Undefined = 0,
        /// <summary>Clamp to edge</summary>
        ClampToEdge = 33071,
        /// <summary>Mirrored repeat</summary>
        MirroredRepeat = 33648,
        /// <summary>Repeat</summary>
        Repeat = 10497
    }
}
