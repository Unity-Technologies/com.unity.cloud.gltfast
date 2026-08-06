// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using Unity.Gltfast.Text.Json.Serialization;
using UnityEngine.Scripting.APIUpdating;

namespace Unity.Cloud.Gltfast.Schema
{
    /// <summary>
    /// Application-specific data for meshes
    /// </summary>
    [MovedFrom(true, sourceNamespace: "GLTFast.Schema", sourceAssembly: "glTFast")]
    public class MeshExtras : AdditionalPropertyContainer
    {
        /// <summary>
        /// Morph targets' names
        /// </summary>
        [JsonPropertyName("targetNames")]
        public List<string> TargetNames { get; set; }
    }
}
