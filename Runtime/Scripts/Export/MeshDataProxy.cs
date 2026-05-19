// SPDX-FileCopyrightText: 2024 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Threading.Tasks;
using Unity.Collections;
using UnityEngine;

namespace GLTFast.Export
{
    class MeshDataProxy<TIndex> : IMeshData<TIndex> where TIndex : unmanaged
    {
        Mesh.MeshData m_MeshData;

        public MeshDataProxy(Mesh.MeshData meshData)
        {
            m_MeshData = meshData;
        }

        public int subMeshCount => m_MeshData.subMeshCount;

        public MeshTopology GetTopology(int subMesh)
        {
            return m_MeshData.GetSubMesh(subMesh).topology;
        }

        public int GetIndexCount(int subMesh)
        {
            return m_MeshData.GetSubMesh(subMesh).indexCount;
        }

        public Task<NativeArray<TIndex>> GetIndexData(bool sync)
        {
            return Task.FromResult(m_MeshData.GetIndexData<TIndex>());
        }

        public Task<NativeArray<byte>> GetVertexData(int stream, bool sync)
        {
            return Task.FromResult(m_MeshData.GetVertexData<byte>(stream));
        }
    }
}
