// SPDX-FileCopyrightText: 2024 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Threading.Tasks;
using Unity.Collections;
using UnityEngine;

namespace GLTFast.Export
{
    interface IMeshData
    {
        int subMeshCount { get; }

        MeshTopology GetTopology(int subMesh);
        int GetIndexCount(int subMesh);

        Task<NativeArray<byte>> GetVertexData(int stream, bool sync);
    }

    interface IMeshData<TIndex> : IMeshData where TIndex : unmanaged
    {
        Task<NativeArray<TIndex>> GetIndexData(bool sync);
    }
}
