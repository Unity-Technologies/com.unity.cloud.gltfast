// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using Unity.Gltfast.Text.Json;
using Unity.Gltfast.Text.Json.Serialization;

namespace GLTFast.Schema
{
    /// <summary>
    /// Application-specific data for meshes
    /// </summary>
    public class MeshExtras : IGltfObject
    {
        /// <summary>
        /// Morph targets' names
        /// </summary>
        [JsonPropertyName("targetNames")]
        public List<string> TargetNames { get; set; }

        /// <summary>JSON properties without a matching member.</summary>
        [JsonExtensionData, JsonInclude] internal Dictionary<string, JsonElement> ExtensionsData { get; set; }

        /// <inheritdoc/>
        public bool TryGetValue<T>(string key, out T value)
        {
            return ExtensionsData.TryGetValue(key, out value);
        }

        internal void GltfSerialize(JsonWriter writer)
        {
            if (TargetNames != null)
            {
                writer.AddArrayPropertySafe("targetNames", TargetNames);
            }
        }
    }
}
