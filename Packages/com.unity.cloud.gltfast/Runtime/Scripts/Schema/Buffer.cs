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
    public class Buffer : NamedObject, IGltfObject
    {
        /// <summary>
        /// The length of the buffer in bytes.
        /// </summary>
        [JsonPropertyName("byteLength")]
        public uint ByteLength { get; set; }

        /// <summary>
        /// The URI (or IRI) of the buffer.
        /// </summary>
        [JsonPropertyName("uri")]
        public UriValue Uri { get; set; }

        /// <inheritdoc cref="Asset.Extensions"/>
        [JsonPropertyName("extensions")]
        public UnclassifiedData Extensions { get; set; }

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
            writer.AddObject();
            if (Uri != null)
            {
                writer.AddPropertySafe("uri", Uri.AsString());
            }
            writer.AddProperty("byteLength", ByteLength);
            writer.Close();
        }
    }
}
