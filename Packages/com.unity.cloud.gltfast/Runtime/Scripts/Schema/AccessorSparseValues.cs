// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using Unity.Gltfast.Text.Json;
using Unity.Gltfast.Text.Json.Serialization;

namespace GLTFast.Schema
{

    /// <summary>
    /// Sparse values property of a glTF
    /// </summary>
    /// <seealso cref="AccessorSparse"/>
    public class AccessorSparseValues : IGltfObject
    {
        /// <summary>
        /// The index of the bufferView with sparse values.
        /// Referenced bufferView can't have ARRAY_BUFFER or ELEMENT_ARRAY_BUFFER target.
        /// </summary>
        [JsonIgnore]
        public int BufferView { get; set; } = Constants.UnsetIndex;

        [JsonPropertyName("bufferView"), JsonInclude]
        internal int? BufferViewSerialized
        {
            get => BufferView < 0 ? null : BufferView;
            set => BufferView = value ?? Constants.UnsetIndex;
        }

        /// <summary>
        /// The offset relative to the start of the bufferView in bytes. Must be aligned.
        /// </summary>
        [JsonPropertyName("byteOffset")]
        public int ByteOffset { get; set; }

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
            if (BufferView >= 0)
            {
                writer.AddProperty("bufferView", BufferView);
            }
            if (ByteOffset >= 0)
            {
                writer.AddProperty("byteOffset", ByteOffset);
            }
            writer.Close();
        }
    }
}
