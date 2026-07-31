// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

#if MESHOPT_IS_RECENT
using System;
using Unity.Gltfast.Text.Json.Serialization;
using UnityEngine.Scripting.APIUpdating;

namespace Unity.Cloud.Gltfast.Schema
{
    [MovedFrom(true, sourceNamespace: "GLTFast.Schema", sourceAssembly: "glTFast")]
    public class BufferViewMeshoptExtension : IBufferView
    {
        /// <summary>
        /// The index of the buffer.
        /// </summary>
        /// <remarks>
        /// Required per the EXT_meshopt_compression extension. Defaults to
        /// <see cref="Constants.UnsetIndex"/> when absent from JSON so callers can
        /// distinguish "missing" from an explicit value.
        /// </remarks>
        [JsonIgnore]
        public int Buffer { get; set; } = Constants.UnsetIndex;

        [JsonPropertyName("buffer"), JsonInclude]
        internal int? BufferSerialized
        {
            get => Buffer < 0 ? null : Buffer;
            set => Buffer = value ?? Constants.UnsetIndex;
        }

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
        /// </summary>
        [JsonPropertyName("byteStride")]
        public int? ByteStride { get; set; }

        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("mode")]
        [JsonConverter(typeof(MeshoptModeConverter))]
        public MeshoptMode Mode { get; set; } = MeshoptMode.Undefined;

        [JsonPropertyName("filter")]
        [JsonConverter(typeof(MeshoptFilterConverter))]
        public MeshoptFilter Filter { get; set; } = MeshoptFilter.None;
    }
}

#endif
