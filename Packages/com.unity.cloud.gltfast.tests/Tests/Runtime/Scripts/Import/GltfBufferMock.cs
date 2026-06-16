// SPDX-FileCopyrightText: 2024 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using GLTFast.Schema;
using Unity.Collections;

namespace GLTFast.Tests
{
    unsafe class GltfBufferMock : IGltfBuffers
    {
        public const int sparseAccessorIndex = 42;

        public Accessor GetAccessor(int index)
        {
            return new Accessor
            {
                BufferView = index,
                ComponentType = AccessorDataType.Float,
                Sparse = index == sparseAccessorIndex
                    ? new AccessorSparse()
                    : null,
                Min = new List<float> { -1, -1, -1 },
                Max = new List<float> { 1, 1, 1 }
            };
        }

        public ReadOnlyNativeArray<byte> GetAccessorData(int index)
        {
            throw new NotImplementedException();
        }

        public void GetAccessorAndData(int index, out Accessor accessor, out void* data, out int byteStride)
        {
            accessor = GetAccessor(index);
            accessor.Type = AccessorType.Vector3;
            data = null;
            byteStride = 1;
        }

        public void GetAccessorSparseIndices(AccessorSparseIndices sparseIndices, out void* data)
        {
            data = null;
        }

        public void GetAccessorSparseValues(AccessorSparseValues sparseValues, out void* data)
        {
            data = null;
        }

        public ReadOnlyNativeArray<byte> GetBufferView(int bufferViewIndex, out int byteStride, int offset = 0, int length = 0)
        {
            throw new NotImplementedException();
        }

        public ReadOnlyNativeArray<T> GetAccessorData<T>(int bufferViewIndex, int offset = 0, int length = 0) where T : unmanaged
        {
            throw new NotImplementedException();
        }

        public ReadOnlyNativeStridedArray<T> GetStridedAccessorData<T>(int bufferViewIndex, int count, int offset = 0) where T : unmanaged
        {
            throw new NotImplementedException();
        }

    }
}
