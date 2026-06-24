// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using Unity.Gltfast.Text.Json;
using Unity.Gltfast.Text.Json.Serialization;

namespace GLTFast.Schema
{
    /// <summary>
    /// Sparse property of a glTF
    /// </summary>
    /// <seealso cref="Accessor"/>
    public class AccessorSparse : IGltfObject
    {
        /// <summary>
        /// Number of entries stored in the sparse array.
        /// </summary>
        [JsonIgnore]
        public int Count { get; set; } = Constants.UnsetIndex;

        [JsonPropertyName("count"), JsonInclude]
        internal int? CountSerialized
        {
            get => Count < 0 ? null : Count;
            set => Count = value ?? Constants.UnsetIndex;
        }

        /// <summary>
        /// Index array of size `count` that points to those accessor attributes that
        /// deviate from their initialization value. Indices must strictly increase.
        /// </summary>
        [JsonPropertyName("indices")]
        public AccessorSparseIndices Indices { get; set; }

        /// <summary>
        /// "Array of size `count` times number of components, storing the displaced
        /// accessor attributes pointed by `indices`. Substituted values must have
        /// the same `componentType` and number of components as the base accessor.
        /// </summary>
        [JsonPropertyName("values")]
        public AccessorSparseValues Values { get; set; }

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
            if (Count >= 0)
            {
                writer.AddProperty("count", Count);
            }
            if (Indices != null)
            {
                writer.AddProperty("indices");
                Indices.GltfSerialize(writer);
            }
            if (Values != null)
            {
                writer.AddProperty("values");
                Values.GltfSerialize(writer);
            }
            writer.Close();
        }
    }
}
