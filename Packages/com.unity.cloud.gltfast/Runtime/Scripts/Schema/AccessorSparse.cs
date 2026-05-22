// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
#if UNITY_6000_5_OR_NEWER
using System.Text.Json;
using System.Text.Json.Serialization;
#else
using Unity.Gltfast.Text.Json;
using Unity.Gltfast.Text.Json.Serialization;
#endif

namespace GLTFast.Schema
{
    /// <summary>
    /// Sparse property of a glTF
    /// </summary>
    /// <seealso cref="Accessor"/>
    [System.Serializable]
    public class AccessorSparse : IGltfObject
    {
        /// <summary>
        /// Number of entries stored in the sparse array.
        /// </summary>
        public int count;

        /// <summary>
        /// Index array of size `count` that points to those accessor attributes that
        /// deviate from their initialization value. Indices must strictly increase.
        /// </summary>
        public AccessorSparseIndices indices;

        /// <inheritdoc cref="indices"/>
        public AccessorSparseIndices Indices => indices;

        /// <summary>
        /// "Array of size `count` times number of components, storing the displaced
        /// accessor attributes pointed by `indices`. Substituted values must have
        /// the same `componentType` and number of components as the base accessor.
        /// </summary>
        public AccessorSparseValues values;

        /// <inheritdoc cref="values"/>
        public AccessorSparseValues Values => values;

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
            writer.AddProperty("count", count);
            if (indices != null)
            {
                writer.AddProperty("indices");
                indices.GltfSerialize(writer);
            }
            if (values != null)
            {
                writer.AddProperty("values");
                values.GltfSerialize(writer);
            }
            writer.Close();
        }
    }
}
