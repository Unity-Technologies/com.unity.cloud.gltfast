// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using Unity.Gltfast.Text.Json;
using Unity.Gltfast.Text.Json.Serialization;

namespace GLTFast.Schema
{

    /// <summary>
    /// TextureInfo extensions
    /// </summary>
    [System.Serializable]
    public class TextureInfoExtensions : IGltfObject
    {
        /// <inheritdoc cref="Extension.TextureTransform"/>
        // ReSharper disable once InconsistentNaming
        public TextureTransform KHR_texture_transform;

        /// <inheritdoc cref="Root.extras"/>
        public UnclassifiedData extras;

        /// <summary>JSON properties without a matching member.</summary>
        [JsonExtensionData, JsonInclude] internal Dictionary<string, JsonElement> ExtensionsData { get; set; }

        /// <inheritdoc/>
        public bool TryGetValue<T>(string key, out T value)
        {
            return ExtensionsData.TryGetValue(key, out value);
        }

        internal void GltfSerialize(JsonWriter writer)
        {
            if (KHR_texture_transform != null)
            {
                writer.AddObject();
                writer.AddProperty("KHR_texture_transform");
                KHR_texture_transform.GltfSerialize(writer);
                writer.Close();
            }
        }
    }
}
