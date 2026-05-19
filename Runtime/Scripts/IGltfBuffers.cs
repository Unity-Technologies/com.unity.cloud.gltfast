// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using UnityEngine;

namespace GLTFast
{

    using Schema;

    interface IGltfBuffers
    {
        AccessorBase GetAccessor(int index);
        unsafe void GetAccessorAndData(int index, out AccessorBase accessor, out void* data, out int byteStride);
        unsafe void GetAccessorSparseIndices(AccessorSparseIndices sparseIndices, out void* data);
        unsafe void GetAccessorSparseValues(AccessorSparseValues sparseValues, out void* data);
        ReadOnlyNativeArray<byte> GetBufferView(int bufferViewIndex, out int byteStride, int offset = 0, int length = 0);

        ReadOnlyNativeArray<T> GetAccessorData<T>(
            int bufferViewIndex,
            int count,
            int offset = 0
        )
            where T : unmanaged;

        ReadOnlyNativeStridedArray<T> GetStridedAccessorData<T>(
            int bufferViewIndex,
            int count,
            int offset = 0
        )
            where T : unmanaged;
    }
}
