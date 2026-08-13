// SPDX-FileCopyrightText: 2026 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using Unity.Cloud.Gltfast.Text.Json.Serialization;
using UnityEngine.Scripting.APIUpdating;

namespace Unity.Cloud.Gltfast.Schema
{
    /// <summary>
    /// Per-instance attributes collection
    /// </summary>
    [MovedFrom(true, sourceNamespace: "GLTFast.Schema", sourceAssembly: "glTFast")]
    public class InstancesAttributes
    {
        /// <summary>
        /// Instance positions accessor index
        /// </summary>
        [JsonPropertyName("TRANSLATION")]
        public int? Translation { get; set; }

        /// <summary>
        /// Instance rotations accessor index
        /// </summary>
        [JsonPropertyName("ROTATION")]
        public int? Rotation { get; set; }

        /// <summary>
        /// Instance scales accessor index
        /// </summary>
        [JsonPropertyName("SCALE")]
        public int? Scale { get; set; }
    }
}
