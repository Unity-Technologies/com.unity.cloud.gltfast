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
    [System.Serializable]
    public class TextureExtensions : IGltfObject
    {
        /// <inheritdoc cref="Extension.TextureBasisUniversal"/>
        // ReSharper disable once InconsistentNaming
        public TextureBasisUniversal KHR_texture_basisu;

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
            throw new System.NotImplementedException($"GltfSerialize missing on {GetType()}");
        }
    }

    /// <summary>
    /// Basis Universal texture extension
    /// </summary>
    /// <seealso cref="Extension.TextureBasisUniversal"/>
    [System.Serializable]
    public class TextureBasisUniversal
    {

        /// <summary>
        /// Index of the image which defines a reference to the KTX v2 image
        /// with Basis Universal super-compression.
        /// </summary>
        public int source = -1;
    }
}
