// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using Unity.Gltfast.Text.Json;
using Unity.Gltfast.Text.Json.Serialization;

namespace GLTFast.Schema
{
    /// <summary>
    /// Node extensions
    /// </summary>
    public class NodeExtensions : IGltfObject
    {
        /// <inheritdoc cref="Schema.MeshGpuInstancing"/>
        [JsonPropertyName("EXT_mesh_gpu_instancing")]
        public MeshGpuInstancing MeshGpuInstancing { get; set; }

        /// <inheritdoc cref="Schema.LightsPunctual"/>
        [JsonPropertyName("KHR_lights_punctual")]
        public NodeLightsPunctual LightsPunctual { get; set; }

        /// <summary>JSON properties without a matching member.</summary>
        [JsonExtensionData, JsonInclude] internal Dictionary<string, JsonElement> ExtensionsData { get; set; }

        /// <inheritdoc/>
        public bool TryGetValue<T>(string key, out T value)
        {
            return ExtensionsData.TryGetValue(key, out value);
        }

        internal void GltfSerialize(JsonWriter writer)
        {
            writer.AddObject();
            if (MeshGpuInstancing != null)
            {
                writer.AddProperty("EXT_mesh_gpu_instancing");
                MeshGpuInstancing.GltfSerialize(writer);
            }
            if (LightsPunctual != null)
            {
                writer.AddProperty("KHR_lights_punctual");
                LightsPunctual.GltfSerialize(writer);
            }
            writer.Close();
        }
    }
}
