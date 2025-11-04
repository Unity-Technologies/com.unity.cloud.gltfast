// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Runtime.InteropServices;
using GLTFast.Schema;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;
using Mesh = UnityEngine.Mesh;
using System.Threading.Tasks;

namespace GLTFast
{

    class MorphTargetsGenerator
    {
        readonly string[] m_MorphTargetNames;
        readonly GltfImportBase m_GltfImport;

        MorphTargetGenerator[] m_Contexts;
        NativeArray<JobHandle> m_Handles;

        public MorphTargetsGenerator(
            int vertexCount,
            int subMeshCount,
            int morphTargetCount,
            string[] morphTargetNames,
            bool hasNormals,
            bool hasTangents,
            GltfImportBase gltfImport
            )
        {
            m_MorphTargetNames = morphTargetNames;
            m_GltfImport = gltfImport;

            m_Contexts = new MorphTargetGenerator[morphTargetCount];
            for (var i = 0; i < morphTargetCount; i++)
            {
                m_Contexts[i] = new MorphTargetGenerator(vertexCount, hasNormals, hasTangents);
            }
            m_Handles = new NativeArray<JobHandle>(morphTargetCount * subMeshCount, VertexBufferGeneratorBase.defaultAllocator);
        }

        public bool AddMorphTarget(
            int offset,
            int subMesh,
            int morphTargetIndex,
            MorphTarget morphTarget
            )
        {
            var morphTargetGenerator = m_Contexts[morphTargetIndex];
            var jobHandle = morphTargetGenerator.ScheduleMorphTargetJobs(
                morphTarget,
                offset,
                m_GltfImport
                );
            if (jobHandle.HasValue)
            {
                m_Handles[subMesh * m_Contexts.Length + morphTargetIndex] = jobHandle.Value;
            }
            else
            {
                return false;
            }
            return true;
        }

        public JobHandle GetJobHandle()
        {
            var handle = m_Handles.Length > 1 ? JobHandle.CombineDependencies(m_Handles) : m_Handles[0];
            m_Handles.Dispose();
            return handle;
        }

        public async Task ApplyOnMeshAndDispose(Mesh mesh)
        {
            for (var index = 0; index < m_Contexts.Length; index++)
            {
                var context = m_Contexts[index];
                context.AddToMesh(mesh, m_MorphTargetNames?[index] ?? index.ToString());
                context.Dispose();
                await m_GltfImport.DeferAgent.BreakPoint();
            }
            m_Contexts = null;
        }
    }

    sealed class MorphTargetGenerator : IDisposable
    {
        Vector3[] m_Positions;
        Vector3[] m_Normals;
        Vector3[] m_Tangents;

        GCHandle m_PositionsHandle;
        GCHandle m_NormalsHandle;
        GCHandle m_TangentsHandle;

        public MorphTargetGenerator(int vertexCount, bool hasNormals, bool hasTangents)
        {
            m_Positions = new Vector3[vertexCount];
            m_PositionsHandle = GCHandle.Alloc(m_Positions, GCHandleType.Pinned);

            if (hasNormals)
            {
                m_Normals = new Vector3[vertexCount];
                m_NormalsHandle = GCHandle.Alloc(m_Normals, GCHandleType.Pinned);
            }

            if (hasTangents)
            {
                m_Tangents = new Vector3[vertexCount];
                m_TangentsHandle = GCHandle.Alloc(m_Tangents, GCHandleType.Pinned);
            }
        }

        public unsafe JobHandle? ScheduleMorphTargetJobs(
            MorphTarget morphTarget,
            int offset,
            IGltfBuffers buffers
        )
        {
            Profiler.BeginSample("ScheduleMorphTargetJobs");

            buffers.GetAccessorAndData(
                morphTarget.POSITION,
                out var posAcc,
                out var posData,
                out _
                );

            var jobCount = 1;
            if (posAcc.IsSparse && posAcc.bufferView >= 0)
                jobCount++;

            AccessorBase nrmAcc = null;
            void* nrmInput = null;
            var nrmInputByteStride = 0;

            if (morphTarget.NORMAL >= 0)
            {
                buffers.GetAccessorAndData(morphTarget.NORMAL, out nrmAcc, out nrmInput, out nrmInputByteStride);
                jobCount += nrmAcc.IsSparse && nrmAcc.bufferView >= 0 ? 2 : 1;
            }

            AccessorBase tanAcc = null;
            void* tanInput = null;
            var tanInputByteStride = 0;

            if (morphTarget.TANGENT >= 0)
            {
                buffers.GetAccessorAndData(morphTarget.TANGENT, out tanAcc, out tanInput, out tanInputByteStride);
                jobCount += tanAcc.IsSparse && tanAcc.bufferView >= 0 ? 2 : 1;
            }

            var handles = new NativeArray<JobHandle>(jobCount, VertexBufferGeneratorBase.defaultAllocator);
            var handleIndex = 0;

            if (!SchedulePositionsJobs(offset, buffers, posData, posAcc, handles, ref handleIndex))
                return null;

            if (nrmAcc != null
                && !ScheduleNormalsJobs(
                    offset,
                    buffers,
                    nrmAcc,
                    nrmInput,
                    nrmInputByteStride,
                    handles,
                    ref handleIndex))
            {
                return null;
            }

            if (tanAcc != null
                && !ScheduleTangentsJobs(offset, buffers, tanAcc, tanInput, tanInputByteStride, handles, handleIndex))
            {
                return null;
            }

            var handle = jobCount > 1 ? JobHandle.CombineDependencies(handles) : handles[0];
            handles.Dispose();
            Profiler.EndSample();
            return handle;
        }

        unsafe bool SchedulePositionsJobs(
            int offset,
            IGltfBuffers buffers,
            void* posData,
            AccessorBase posAcc,
            NativeArray<JobHandle> handles,
            ref int handleIndex
            )
        {
            fixed (void* dest = &m_Positions[offset])
            {
                JobHandle? h = null;
                if (posData != null)
                {
                    h = VertexBufferGeneratorBase.GetVector3Job(
                        buffers,
                        posAcc,
                        (float3*)dest,
                        12,
                        posAcc.normalized,
                        false // positional data never needs to be normalized
                    );
                    if (h.HasValue)
                    {
                        handles[handleIndex] = h.Value;
                        handleIndex++;
                    }
                    else
                    {
                        Profiler.EndSample();
                        return false;
                    }
                }
                if (posAcc.IsSparse)
                {
                    buffers.GetAccessorSparseIndices(posAcc.Sparse.Indices, out var posIndexData);
                    buffers.GetAccessorSparseValues(posAcc.Sparse.Values, out var posValueData);
                    var sparseJobHandle = VertexBufferGeneratorBase.GetVector3SparseJob(
                        posIndexData,
                        posValueData,
                        posAcc.Sparse.count,
                        posAcc.Sparse.Indices.componentType,
                        posAcc.componentType,
                        (float3*)dest,
                        12,
                        dependsOn: ref h,
                        posAcc.normalized
                    );
                    if (sparseJobHandle.HasValue)
                    {
                        handles[handleIndex] = sparseJobHandle.Value;
                        handleIndex++;
                    }
                    else
                    {
                        Profiler.EndSample();
                        return false;
                    }
                }
            }

            return true;
        }

        unsafe bool ScheduleNormalsJobs(
            int offset,
            IGltfBuffers buffers,
            AccessorBase nrmAcc,
            void* nrmInput,
            int nrmInputByteStride,
            NativeArray<JobHandle> handles,
            ref int handleIndex
            )
        {
            fixed (void* dest = &(m_Normals[offset]))
            {
                JobHandle? h = null;
                if (nrmAcc.bufferView >= 0)
                {
                    h = VertexBufferGeneratorBase.GetVector3Job(
                        buffers,
                        nrmAcc,
                        (float3*)dest,
                        12,
                        nrmAcc.normalized,
                        false // morph target normals are deltas -> don't normalize
                    );
                    if (h.HasValue)
                    {
                        handles[handleIndex] = h.Value;
                        handleIndex++;
                    }
                    else
                    {
                        Profiler.EndSample();
                        return false;
                    }
                }
                if (nrmAcc.IsSparse)
                {
                    buffers.GetAccessorSparseIndices(nrmAcc.Sparse.Indices, out var indexData);
                    buffers.GetAccessorSparseValues(nrmAcc.Sparse.Values, out var valueData);
                    var sparseJobHandle = VertexBufferGeneratorBase.GetVector3SparseJob(
                        indexData,
                        valueData,
                        nrmAcc.Sparse.count,
                        nrmAcc.Sparse.Indices.componentType,
                        nrmAcc.componentType,
                        (float3*)dest,
                        12,
                        dependsOn: ref h,
                        nrmAcc.normalized
                    );
                    if (sparseJobHandle.HasValue)
                    {
                        handles[handleIndex] = sparseJobHandle.Value;
                        handleIndex++;
                    }
                    else
                    {
                        Profiler.EndSample();
                        return false;
                    }
                }
            }

            return true;
        }

        unsafe bool ScheduleTangentsJobs(
            int offset,
            IGltfBuffers buffers,
            AccessorBase tanAcc,
            void* tanInput,
            int tanInputByteStride,
            NativeArray<JobHandle> handles,
            int handleIndex
            )
        {
            fixed (void* dest = &(m_Tangents[offset]))
            {
                JobHandle? h = null;
                if (tanAcc.bufferView >= 0)
                {
                    h = VertexBufferGeneratorBase.GetVector3Job(
                        buffers,
                        tanAcc,
                        (float3*)dest,
                        12,
                        tanAcc.normalized,
                        false // morph target tangents are deltas -> don't normalize
                    );
                    if (h.HasValue)
                    {
                        handles[handleIndex] = h.Value;
                        handleIndex++;
                    }
                    else
                    {
                        Profiler.EndSample();
                        return false;
                    }
                }
                if (tanAcc.IsSparse)
                {
                    buffers.GetAccessorSparseIndices(tanAcc.Sparse.Indices, out var indexData);
                    buffers.GetAccessorSparseValues(tanAcc.Sparse.Values, out var valueData);
                    var sparseJobHandle = VertexBufferGeneratorBase.GetVector3SparseJob(
                        indexData,
                        valueData,
                        tanAcc.Sparse.count,
                        tanAcc.Sparse.Indices.componentType,
                        tanAcc.componentType,
                        (float3*)dest,
                        12,
                        dependsOn: ref h,
                        tanAcc.normalized
                    );
                    if (sparseJobHandle.HasValue)
                    {
                        handles[handleIndex] = sparseJobHandle.Value;
                    }
                    else
                    {
                        Profiler.EndSample();
                        return false;
                    }
                }
            }

            return true;
        }

        public void AddToMesh(Mesh mesh, string name)
        {
            Profiler.BeginSample("AddBlendShapeFrame");
            mesh.AddBlendShapeFrame(name, 1f, m_Positions, m_Normals, m_Tangents);
            Profiler.EndSample();
        }

        public void Dispose()
        {
            m_PositionsHandle.Free();
            m_Positions = null;
            if (m_Normals != null)
            {
                m_NormalsHandle.Free();
                m_Normals = null;
            }
            if (m_Tangents != null)
            {
                m_TangentsHandle.Free();
                m_Tangents = null;
            }
        }
    }
}
