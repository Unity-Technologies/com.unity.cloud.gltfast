// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
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

    /// <inheritdoc/>
    [Serializable]
    public class BufferView : BufferViewBase<BufferViewExtensions> { }

    /// <inheritdoc/>
    [Serializable]
    public class BufferViewBase<TExtensions> : BufferViewBase
    where TExtensions : BufferViewExtensions
    {
        /// <inheritdoc cref="Extensions"/>
        public TExtensions extensions;

        /// <inheritdoc cref="BufferViewBase.Extensions"/>
        public override BufferViewExtensions Extensions => extensions;
    }

    /// <inheritdoc cref="IBufferView"/>
    [Serializable]
    public abstract class BufferViewBase : NamedObject, IBufferView, IGltfObject
    {
        /// <summary>
        /// The index of the buffer.
        /// </summary>
        public int buffer;

        /// <summary>
        /// The offset into the buffer in bytes.
        /// </summary>
        public int byteOffset;

        /// <summary>
        /// The length of the bufferView in bytes.
        /// </summary>
        public int byteLength;

        /// <summary>
        /// The stride, in bytes, between vertex attributes or other interleaved data.
        /// When this is zero, data is tightly packed.
        /// </summary>
        public int byteStride = -1;

        /// <summary>
        /// The target that the WebGL buffer should be bound to.
        /// All valid values correspond to WebGL enums.
        /// When this is not provided, the bufferView contains animation or skin data.
        /// </summary>
        public int target;

        /// <inheritdoc cref="Root.extras"/>
        public UnclassifiedData extras;

        /// <summary>JSON properties without a matching member.</summary>
        [JsonExtensionData, JsonInclude] internal Dictionary<string, JsonElement> ExtensionsData { get; set; }

        /// <inheritdoc/>
        public bool TryGetValue<T>(string key, out T value)
        {
            return ExtensionsData.TryGetValue(key, out value);
        }

        /// <inheritdoc cref="IBufferView.Buffer"/>
        public int Buffer => buffer;

        /// <inheritdoc cref="IBufferView.ByteOffset"/>
        public int ByteOffset => byteOffset;

        /// <inheritdoc cref="IBufferView.ByteLength"/>
        public int ByteLength => byteLength;

        /// <inheritdoc cref="IBufferView.ByteStride"/>
        public int ByteStride => byteStride;

        internal void GltfSerialize(JsonWriter writer)
        {
            writer.AddObject();
            writer.AddProperty("buffer", buffer);
            writer.AddProperty("byteLength", byteLength);
            if (byteOffset > 0)
            {
                writer.AddProperty("byteOffset", byteOffset);
            }
            if (byteStride > 0)
            {
                writer.AddProperty("byteStride", byteStride);
            }
            if (target > 0)
            {
                writer.AddProperty("target", target);
            }
            writer.Close();
        }

        /// <inheritdoc cref="BufferViewExtensions"/>
        public abstract BufferViewExtensions Extensions { get; }
    }
}
