// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;

namespace GLTFast.Schema
{
    /// <summary>
    /// A view into a buffer generally representing a subset of the buffer.
    /// </summary>
    public interface IBufferView
    {
        /// <summary>
        /// The index of the buffer.
        /// </summary>
        int Buffer { get; }

        /// <summary>
        /// The offset into the buffer in bytes.
        /// </summary>
        int ByteOffset { get; }

        /// <summary>
        /// The length of the bufferView in bytes.
        /// </summary>
        int ByteLength { get; }

        /// <summary>
        /// The stride, in bytes, between vertex attributes. If defined, must be a multiple of 4 and between 4 and 252.
        /// </summary>
        /// <remarks>When this is 0 or negative, data is tightly packed.</remarks>
        int ByteStride { get; }
    }
}
