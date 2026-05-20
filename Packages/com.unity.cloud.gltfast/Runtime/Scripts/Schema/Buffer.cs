// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using Unity.Gltfast.Text.Json;
using Unity.Gltfast.Text.Json.Serialization;

namespace GLTFast.Schema
{

    /// <summary>
    /// A buffer points to binary geometry, animation, or skins.
    /// </summary>
    [System.Serializable]
    public class Buffer : NamedObject, IGltfObject
    {
        /// <summary>
        /// The length of the buffer in bytes.
        /// </summary>
        public uint byteLength;

        /// <summary>
        /// The URI (or IRI) of the buffer.
        /// </summary>
        public string uri;

        /// <inheritdoc cref="Asset.extensions"/>
        public UnclassifiedData extensions;

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
            writer.AddObject();
            if (!string.IsNullOrEmpty(uri))
            {
                writer.AddPropertySafe("uri", uri);
            }
            writer.AddProperty("byteLength", byteLength);
            writer.Close();
        }
    }
}
