// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using UnityEngine;

namespace GLTFast
{
    readonly struct GltfBufferRange
    {
        public long Start { get; }

        public long Length { get; }

        public GltfBufferRange(long start, long length)
        {
            Start = start;
            Length = length;
        }
    }
}
