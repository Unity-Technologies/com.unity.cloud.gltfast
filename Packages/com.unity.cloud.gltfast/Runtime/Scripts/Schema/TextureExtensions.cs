// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using Unity.Gltfast.Text.Json;
using Unity.Gltfast.Text.Json.Serialization;

namespace GLTFast.Schema
{
    /// <summary>
    /// Texture extensions
    /// </summary>
    public class TextureExtensions : IGltfObject
    {
        /// <inheritdoc cref="Extension.TextureBasisUniversal"/>
        [JsonPropertyName("KHR_texture_basisu")]
        public TextureBasisUniversal BasisU { get; set; }

        /// <inheritdoc cref="Root.Extras"/>
        [JsonPropertyName("extras")]
        public UnclassifiedData Extras { get; set; }

        /// <summary>JSON properties without a matching member.</summary>
        [JsonExtensionData, JsonInclude] internal Dictionary<string, JsonElement> ExtensionsData { get; set; }

        /// <inheritdoc/>
        public bool TryGetValue<T>(string key, out T value)
        {
            return ExtensionsData.TryGetValue(key, out value);
        }

        internal void GltfSerialize(JsonWriter writer)
        {
            throw new System.NotImplementedException($"GltfSerialize missing on {GetType()}");
        }
    }
}
