// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

#if MESHOPT_IS_RECENT
using System;
using Meshoptimizer;
using Unity.Gltfast.Text.Json.Serialization;

namespace GLTFast.Schema
{
    public class BufferViewMeshoptExtension : IBufferView
    {

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

        [JsonPropertyName("count")]
        public int Count { get; set; }

        // Property is public for unified serialization only. Warn via Obsolete attribute.
        [Obsolete("Use GetMode for access.")]
        [JsonPropertyName("mode")]
        public string Mode { get; set; }

        // Property is public for unified serialization only. Warn via Obsolete attribute.
        [Obsolete("Use GetFilter for access.")]
        [JsonPropertyName("filter")]
        public string Filter { get; set; }

        Mode m_ModeEnum = Meshoptimizer.Mode.Undefined;
        Filter m_FilterEnum = Meshoptimizer.Filter.Undefined;

        public Mode GetMode()
        {
            if (m_ModeEnum != Meshoptimizer.Mode.Undefined)
            {
                return m_ModeEnum;
            }

#pragma warning disable CS0618 // Type or member is obsolete
            if (!Enum.TryParse(Mode, true, out m_ModeEnum))
            {
                m_ModeEnum = Meshoptimizer.Mode.Undefined;
            }

            Mode = null;
#pragma warning restore CS0618 // Type or member is obsolete
            return m_ModeEnum;
        }

        public Filter GetFilter()
        {
            if (m_FilterEnum != Meshoptimizer.Filter.Undefined)
            {
                return m_FilterEnum;
            }

#pragma warning disable CS0618 // Type or member is obsolete
            if (!Enum.TryParse(Filter, true, out m_FilterEnum))
            {
                m_FilterEnum = Meshoptimizer.Filter.None;
            }

            Filter = null;
#pragma warning restore CS0618 // Type or member is obsolete
            return m_FilterEnum;
        }
    }
}

#endif
