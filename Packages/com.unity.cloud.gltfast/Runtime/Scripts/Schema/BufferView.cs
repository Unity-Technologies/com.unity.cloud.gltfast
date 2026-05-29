// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using Unity.Gltfast.Text.Json;
using Unity.Gltfast.Text.Json.Serialization;

namespace GLTFast.Schema
{
    /// <summary>
    /// GPU buffer type.
    /// Relates to WebGL's bindBuffer.
    /// </summary>
    /// <seealso href="https://registry.khronos.org/glTF/specs/2.0/glTF-2.0.html#_bufferview_target"/>
    public enum BufferViewTarget
    {
        /// <summary>No target.</summary>
        None = 0,
        /// <summary>ARRAY_BUFFER</summary>
        ArrayBuffer = 34962,
        /// <summary>ELEMENT_ARRAY_BUFFER</summary>
        ElementArrayBuffer = 34963,
    }

    /// <inheritdoc cref="IBufferView"/>
    public class BufferView : NamedObject, IBufferView, IGltfObject
    {
        /// <inheritdoc cref="BufferViewExtensions"/>
        [JsonPropertyName("extensions")]
        public BufferViewExtensions Extensions { get; set; }

        /// <summary>
        /// The index of the buffer.
        /// </summary>
        [JsonPropertyName("buffer")]
        public int Buffer { get; set; }

        /// <summary>
        /// The offset into the buffer in bytes.
        /// </summary>
        [JsonPropertyName("byteOffset")]
        public int ByteOffset { get; set; }

        /// <summary>
        /// The length of the bufferView in bytes.
        /// </summary>
        [JsonPropertyName("byteLength")]
        public int ByteLength { get; set; }

        /// <summary>
        /// The stride, in bytes, between vertex attributes or other interleaved data.
        /// When this is zero, data is tightly packed.
        /// </summary>
        [JsonPropertyName("byteStride")]
        public int ByteStride { get; set; } = -1;

        /// <summary>
        /// The target that the WebGL buffer should be bound to.
        /// All valid values correspond to WebGL enums.
        /// When this is not provided, the bufferView contains animation or skin data.
        /// </summary>
        [JsonPropertyName("target")]
        public int Target { get; set; }

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
            writer.AddProperty("buffer", Buffer);
            writer.AddProperty("byteLength", ByteLength);
            if (ByteOffset > 0)
            {
                writer.AddProperty("byteOffset", ByteOffset);
            }
            if (ByteStride > 0)
            {
                writer.AddProperty("byteStride", ByteStride);
            }
            if (Target > 0)
            {
                writer.AddProperty("target", Target);
            }
            writer.Close();
        }
    }
}
