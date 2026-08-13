// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using Unity.Cloud.Gltfast.Text.Json.Serialization;
using UnityEngine.Scripting.APIUpdating;

namespace Unity.Cloud.Gltfast.Schema
{
    /// <summary>
    /// Node extensions
    /// </summary>
    [MovedFrom(true, sourceNamespace: "GLTFast.Schema", sourceAssembly: "glTFast")]
    public class NodeExtensions : AdditionalPropertyContainer
    {
        /// <inheritdoc cref="Schema.MeshGpuInstancing"/>
        [JsonPropertyName("EXT_mesh_gpu_instancing")]
        public MeshGpuInstancing MeshGpuInstancing { get; set; }

        /// <inheritdoc cref="Schema.LightsPunctual"/>
        [JsonPropertyName("KHR_lights_punctual")]
        public NodeLightsPunctual LightsPunctual { get; set; }
    }
}
