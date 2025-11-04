// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GLTFast.Jobs;
using GLTFast.Logging;
using GLTFast.Schema;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine;
using UnityEngine.Assertions;
using Mesh = UnityEngine.Mesh;

namespace GLTFast
{
    class MeshGenerator : MeshGeneratorBase
    {
        VertexBufferGeneratorBase m_VertexData;

        NativeArray<int>[] m_Indices;
        List<IDisposable> m_Disposables;
        readonly SubMeshAssignment[] m_SubMeshAssignments;
        readonly IReadOnlyList<MeshPrimitiveBase> m_Primitives;

        MeshTopology m_Topology;

        int SubMeshCount => m_SubMeshAssignments?.Length ?? m_Primitives.Count;

        public MeshGenerator(
            IReadOnlyList<MeshPrimitiveBase> primitives,
            SubMeshAssignment[] subMeshAssignments,
            string[] morphTargetNames,
            string meshName,
            GltfImportBase gltfImport
        )
            : base(meshName)
        {
            m_Primitives = primitives;
            m_SubMeshAssignments = subMeshAssignments;
            if (CreateVertexGenerator(gltfImport, out var hasNormals, out var hasTangents))
            {
                CreateMorphTargetGenerator(morphTargetNames, hasNormals, hasTangents, gltfImport);
                m_CreationTask = GenerateMesh(gltfImport);
            }
        }

        bool CreateVertexGenerator(
            GltfImportBase gltfImport,
            out bool hasNormals,
            out bool hasTangents
            )
        {
            var drawMode = m_Primitives[0].mode;
            if (!SetTopology(drawMode))
            {
                gltfImport.Logger?.Error(LogCode.PrimitiveModeUnsupported, drawMode.ToString());
            }

            var mainBufferType = GetMainBufferType(gltfImport, out hasNormals, out hasTangents);

            switch (mainBufferType)
            {
                case MainBufferType.Position:
                    m_VertexData = new VertexBufferGenerator<Vertex.VPos>(m_Primitives.Count, gltfImport);
                    break;
                case MainBufferType.PosNorm:
                    m_VertexData = new VertexBufferGenerator<Vertex.VPosNorm>(m_Primitives.Count, gltfImport);
                    break;
                case MainBufferType.PosNormTan:
                    m_VertexData = new VertexBufferGenerator<Vertex.VPosNormTan>(m_Primitives.Count, gltfImport);
                    break;
                default:
                    gltfImport.Logger?.Error(LogCode.BufferMainInvalidType, mainBufferType.ToString());
                    return false;
            }
            m_VertexData.calculateNormals = !hasNormals && (mainBufferType & MainBufferType.Normal) > 0;
            m_VertexData.calculateTangents = !hasTangents && (mainBufferType & MainBufferType.Tangent) > 0;

            foreach (var primitive in m_Primitives)
            {
                m_VertexData.AddPrimitive(primitive.attributes);
            }

            m_VertexData.Initialize();
            return true;
        }

        MainBufferType GetMainBufferType(
            GltfImportBase gltfImport,
            out bool hasNormals,
            out bool hasTangents
            )
        {
            var mainBufferType = MainBufferType.Position;
            var firstAttributes = m_Primitives[0].attributes;
            hasNormals = firstAttributes.NORMAL >= 0;
            hasTangents = firstAttributes.TANGENT >= 0;

            if (hasTangents)
                mainBufferType = MainBufferType.PosNormTan;
            else if (hasNormals)
                mainBufferType = MainBufferType.PosNorm;

            Profiler.BeginSample("LoadAccessorData.ScheduleVertexJob");
            foreach (var primitive in IterateSubMeshes())
            {
                if (primitive.mode == DrawMode.Triangles
                    || primitive.mode == DrawMode.TriangleFan
                    || primitive.mode == DrawMode.TriangleStrip)
                {
                    if (primitive.material < 0)
                    {
                        mainBufferType |= MainBufferType.Normal;
                    }
                    else
                    {
                        var material = gltfImport.GetSourceMaterial(primitive.material);
                        if (material.RequiresTangents)
                        {
                            mainBufferType |= MainBufferType.Normal | MainBufferType.Tangent;
                        }
                        else if (material.RequiresNormals)
                        {
                            mainBufferType |= MainBufferType.Normal;
                        }
                    }
                }
            }

            return mainBufferType;
        }

        bool SetTopology(DrawMode drawMode)
        {
            switch (drawMode)
            {
                case DrawMode.Triangles:
                case DrawMode.TriangleStrip:
                case DrawMode.TriangleFan:
                    m_Topology = MeshTopology.Triangles;
                    break;
                case DrawMode.Points:
                    m_Topology = MeshTopology.Points;
                    break;
                case DrawMode.Lines:
                    m_Topology = MeshTopology.Lines;
                    break;
                case DrawMode.LineLoop:
                case DrawMode.LineStrip:
                    m_Topology = MeshTopology.LineStrip;
                    break;
                default:
                    m_Topology = MeshTopology.Triangles;
                    return false;
            }
            return true;
        }

        void CreateMorphTargetGenerator(
            string[] morphTargetNames,
            bool hasNormals,
            bool hasTangents,
            GltfImportBase gltfImport
            )
        {
            var morphTargets = m_Primitives[0].targets;
            if (morphTargets != null)
            {
                m_MorphTargetsGenerator = new MorphTargetsGenerator(
                    m_VertexData.VertexCount,
                    m_Primitives.Count,
                    morphTargets.Length,
                    morphTargetNames,
                    hasNormals,
                    hasTangents,
                    gltfImport
                );
            }
        }

        async Task<Mesh> GenerateMesh(GltfImportBase gltfImport)
        {
            if (!await m_VertexData.CreateVertexBuffer())
                return null;

            m_Indices = new NativeArray<int>[SubMeshCount];

            var tmpList = new List<JobHandle>(SubMeshCount);
            foreach (var subMesh in IterateSubMeshesIndexed())
            {
                var subMeshIndex = subMesh.index;
                var primitive = subMesh.primitive;
                if (primitive.indices >= 0)
                {
                    var flip = primitive.mode == DrawMode.Triangles;
                    GetIndicesJob(gltfImport, primitive.indices, out var indices, out var getIndicesJob, flip);
                    if (!getIndicesJob.HasValue)
                        return null;

                    switch (primitive.mode)
                    {
                        case DrawMode.LineLoop:
                        {
                            m_Indices[subMeshIndex] = new NativeArray<int>(indices.Length + 1, Allocator.Persistent);

                            // TODO: Allocate larger index buffer right away and only set last index here
                            // Wait for indices to be ready.
                            while (!getIndicesJob.Value.IsCompleted)
                            {
                                await Task.Yield();
                            }

                            getIndicesJob.Value.Complete();

                            NativeArray<int>.Copy(indices, m_Indices[subMeshIndex], indices.Length);
                            m_Indices[subMeshIndex][indices.Length] = indices[0];
                            indices.Dispose();
                            break;
                        }
                        case DrawMode.TriangleStrip:
                        {
                            // TODO: Allocate larger index buffer right away and recalculate indices in-place.
                            var triangleStripTriangleCount = indices.Length - 2;
                            m_Indices[subMeshIndex] = new NativeArray<int>(triangleStripTriangleCount * 3, Allocator.Persistent);
                            var triangleStripJob = new RecalculateIndicesForTriangleStripJob
                            {
                                input = indices,
                                result = m_Indices[subMeshIndex]
                            };
                            var job = triangleStripJob.Schedule(
                                triangleStripTriangleCount,
                                GltfImportBase.DefaultBatchCount,
                                getIndicesJob.Value
                                );
                            tmpList.Add(job);
                            m_Disposables ??= new List<IDisposable>();
                            m_Disposables.Add(indices);
                            break;
                        }
                        case DrawMode.TriangleFan:
                        {
                            // TODO: Allocate larger index buffer right away and recalculate indices in-place.
                            var triangleFanTriangleCount = indices.Length - 2;
                            m_Indices[subMeshIndex] = new NativeArray<int>(triangleFanTriangleCount * 3, Allocator.Persistent);
                            var triangleFanJob = new RecalculateIndicesForTriangleFanJob
                            {
                                input = indices,
                                result = m_Indices[subMeshIndex]
                            };
                            var job = triangleFanJob.Schedule(triangleFanTriangleCount, GltfImportBase.DefaultBatchCount, getIndicesJob.Value);
                            m_Disposables ??= new List<IDisposable>();
                            m_Disposables.Add(indices);
                            tmpList.Add(job);
                            break;
                        }
                        default:
                            m_Indices[subMeshIndex] = indices;
                            tmpList.Add(getIndicesJob.Value);
                            break;
                    }
                }
                else
                {
                    var vertexCount = ((IGltfBuffers)gltfImport).GetAccessor(primitive.attributes.POSITION).count;
                    CalculateIndicesJob(primitive, vertexCount, out m_Indices[subMeshIndex], out var job);
                    tmpList.Add(job);
                }
            }

            if (m_MorphTargetsGenerator != null)
            {
                for (var subMeshIndex = 0; subMeshIndex < m_Primitives.Count; subMeshIndex++)
                {
                    var primitive = m_Primitives[subMeshIndex];
                    AddMorphTargets(subMeshIndex, primitive, gltfImport.Logger);
                }
                tmpList.Add(m_MorphTargetsGenerator.GetJobHandle());
            }

            await AwaitJobs(tmpList);

            return await CreateMeshResultAsync();
        }

        void AddMorphTargets(int subMesh, MeshPrimitiveBase primitive, ICodeLogger logger)
        {
            if (m_MorphTargetsGenerator == null)
                return;
            var vertexOffset = m_VertexData.VertexIntervals[subMesh];
            for (var morphTargetIndex = 0; morphTargetIndex < primitive.targets.Length; morphTargetIndex++)
            {
                var morphTarget = primitive.targets[morphTargetIndex];
                var success = m_MorphTargetsGenerator.AddMorphTarget(
                    vertexOffset,
                    subMesh,
                    morphTargetIndex,
                    morphTarget
                );
                if (!success)
                {
                    logger?.Error(LogCode.MorphTargetContextFail);
                }
            }
        }

        async Task<Mesh> CreateMeshResultAsync()
        {
            Profiler.BeginSample("CreateMesh");
            var msh = new Mesh
            {
                name = m_MeshName
            };

            m_VertexData.ApplyOnMesh(msh);

            Profiler.BeginSample("SetIndices");
            var indexCount = 0;
            for (var i = 0; i < m_Indices.Length; i++)
            {
                indexCount += m_Indices[i].Length;
            }
            Profiler.BeginSample("SetIndexBufferParams");
            msh.SetIndexBufferParams(indexCount, IndexFormat.UInt32); //TODO: UInt16 maybe?
            Profiler.EndSample();
            msh.subMeshCount = m_Indices.Length;
            indexCount = 0;
            Bounds bounds = default;
            for (var i = 0; i < m_Indices.Length; i++)
            {
                Profiler.BeginSample("SetIndexBufferData");
                msh.SetIndexBufferData(m_Indices[i], 0, indexCount, m_Indices[i].Length, defaultMeshUpdateFlags);
                Profiler.EndSample();

                Profiler.BeginSample("SetSubMesh");
                var vertexBufferIndex = m_SubMeshAssignments != null ? m_SubMeshAssignments[i].VertexBufferIndex : i;
                m_VertexData.GetVertexRange(vertexBufferIndex, out var baseVertex, out var vertexCount);
                var subMeshBoundsValid = m_VertexData.TryGetBounds(vertexBufferIndex, out var subMeshBounds);
                var subMeshDescriptor = new SubMeshDescriptor
                {
                    indexStart = indexCount,
                    indexCount = m_Indices[i].Length,
                    topology = m_Topology,
                    baseVertex = baseVertex,
                    firstVertex = baseVertex,
                    vertexCount = vertexCount,
                    bounds = subMeshBounds
                };
                msh.SetSubMesh(
                    i,
                    subMeshDescriptor,
                    subMeshBoundsValid
                        ? defaultMeshUpdateFlags
                        : defaultMeshUpdateFlags & ~MeshUpdateFlags.DontRecalculateBounds
                    );
                if (!subMeshBoundsValid)
                {
                    subMeshDescriptor = msh.GetSubMesh(i);
                    subMeshBounds = subMeshDescriptor.bounds;
                }

                if (i == 0)
                {
                    bounds = subMeshBounds;
                }
                else
                {
                    bounds.Encapsulate(subMeshBounds);
                }
                Profiler.EndSample();
                indexCount += m_Indices[i].Length;
            }

            msh.bounds = bounds;

            Profiler.EndSample();

            if (m_Topology == MeshTopology.Triangles || m_Topology == MeshTopology.Quads)
            {
                if (m_VertexData.calculateNormals)
                {
                    Profiler.BeginSample("RecalculateNormals");
                    msh.RecalculateNormals();
                    Profiler.EndSample();
                }
                if (m_VertexData.calculateTangents)
                {
                    Profiler.BeginSample("RecalculateTangents");
                    msh.RecalculateTangents();
                    Profiler.EndSample();
                }
            }

            if (m_MorphTargetsGenerator != null)
            {
                await m_MorphTargetsGenerator.ApplyOnMeshAndDispose(msh);
            }

#if GLTFAST_KEEP_MESH_DATA
            Profiler.BeginSample("UploadMeshData");
            msh.UploadMeshData(false);
            Profiler.EndSample();
#endif

            Profiler.EndSample();

            return msh;
        }

        IEnumerable<(int index, MeshPrimitiveBase primitive)> IterateSubMeshesIndexed()
        {
            if (m_SubMeshAssignments == null)
            {
                for (var index = 0; index < m_Primitives.Count; index++)
                {
                    var primitive = m_Primitives[index];
                    yield return (index, primitive);
                }
            }
            else
            {
                for (var index = 0; index < m_SubMeshAssignments.Length; index++)
                {
                    var subMesh = m_SubMeshAssignments[index];
                    yield return (index, subMesh.Primitive);
                }
            }
        }

        IEnumerable<MeshPrimitiveBase> IterateSubMeshes()
        {
            if (m_SubMeshAssignments == null)
            {
                foreach (var primitive in m_Primitives)
                    yield return primitive;
            }
            else
            {
                foreach (var subMesh in m_SubMeshAssignments)
                    yield return subMesh.Primitive;
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                m_VertexData?.Dispose();
                if (m_Indices != null)
                {
                    for (var index = 0; index < m_Indices.Length; index++)
                    {
                        var indices = m_Indices[index];
                        if (indices.IsCreated)
                            indices.Dispose();
                    }

                    m_Indices = null;
                }
            }

            if (m_Disposables != null)
            {
                foreach (var disposable in m_Disposables)
                {
                    disposable.Dispose();
                }

                m_Disposables = null;
            }
        }

        static void GetIndicesJob(
            GltfImportBase gltfImport,
            int accessorIndex,
            out NativeArray<int> indices,
            out JobHandle? jobHandle,
            bool flip
            )
        {
            Profiler.BeginSample("PrepareGetIndicesJob");
            var accessor = ((IGltfBuffers)gltfImport).GetAccessor(accessorIndex);
            var accessorData = ((IGltfBuffers)gltfImport).GetBufferView(
                accessor.bufferView,
                out _,
                accessor.byteOffset,
                accessor.ByteSize
                );

            Profiler.BeginSample("Alloc");
            indices = new NativeArray<int>(accessor.count, Allocator.Persistent);
            Profiler.EndSample();

            Assert.AreEqual(accessor.GetAttributeType(), GltfAccessorAttributeType.SCALAR);
            if (accessor.IsSparse)
            {
                gltfImport.Logger?.Error(LogCode.SparseAccessor, "indices");
            }

            Profiler.BeginSample("CreateJob");
            switch (accessor.componentType)
            {
                case GltfComponentType.UnsignedByte:
                {
                    if (flip)
                    {
                        var job8 = new ConvertIndicesUInt8ToInt32FlippedJob
                        {
                            input = accessorData.Reinterpret<byte3>().AsNativeArrayReadOnly(),
                            result = indices.Reinterpret<int3>(sizeof(int))
                        };
                        jobHandle = job8.Schedule(accessor.count / 3, GltfImportBase.DefaultBatchCount);
                    }
                    else
                    {
                        var job8 = new ConvertIndicesUInt8ToInt32Job
                        {
                            input = accessorData.AsNativeArrayReadOnly(),
                            result = indices
                        };
                        jobHandle = job8.Schedule(accessor.count, GltfImportBase.DefaultBatchCount);
                    }
                    break;
                }
                case GltfComponentType.UnsignedShort:
                {
                    if (flip)
                    {
                        var job16 = new ConvertIndicesUInt16ToInt32FlippedJob
                        {
                            input = accessorData.Reinterpret<ushort3>().AsNativeArrayReadOnly(),
                            result = indices.Reinterpret<int3>(sizeof(int))
                        };
                        jobHandle = job16.Schedule(accessor.count / 3, GltfImportBase.DefaultBatchCount);
                    }
                    else
                    {
                        var job16 = new ConvertIndicesUInt16ToInt32Job
                        {
                            input = accessorData.Reinterpret<ushort>().AsNativeArrayReadOnly(),
                            result = indices
                        };
                        jobHandle = job16.Schedule(accessor.count, GltfImportBase.DefaultBatchCount);
                    }
                    break;
                }
                case GltfComponentType.UnsignedInt:
                {
                    if (flip)
                    {
                        var job32 = new ConvertIndicesUInt32ToInt32FlippedJob
                        {
                            input = accessorData.Reinterpret<uint3>().AsNativeArrayReadOnly(),
                            result = indices.Reinterpret<int3>(sizeof(int))
                        };
                        jobHandle = job32.Schedule(accessor.count / 3, GltfImportBase.DefaultBatchCount);
                    }
                    else
                    {
                        var job32 = new ConvertIndicesUInt32ToInt32Job
                        {
                            input = accessorData.Reinterpret<uint>().AsNativeArrayReadOnly(),
                            result = indices
                        };
                        jobHandle = job32.Schedule(accessor.count, GltfImportBase.DefaultBatchCount);
                    }
                    break;
                }
                default:
                    gltfImport.Logger?.Error(LogCode.IndexFormatInvalid, accessor.componentType.ToString());
                    jobHandle = null;
                    break;
            }
            Profiler.EndSample();
            Profiler.EndSample();
        }

        static void CalculateIndicesJob(
            MeshPrimitiveBase primitive,
            int vertexCount,
            out NativeArray<int> indices,
            out JobHandle jobHandle
            )
        {
            Profiler.BeginSample("CalculateIndicesJob");
            // No indices: calculate them
            switch (primitive.mode)
            {
                case DrawMode.LineLoop:
                {
                    // extra index (first vertex again) for closing line loop
                    indices = new NativeArray<int>(vertexCount + 1, Allocator.Persistent);
                    // Set the last index to the first vertex
                    indices[vertexCount] = 0;
                    var job = new CreateIndicesInt32Job()
                    {
                        result = indices
                    };
                    jobHandle = job.Schedule(vertexCount, GltfImportBase.DefaultBatchCount);
                    break;
                }
                case DrawMode.Triangles:
                {
                    indices = new NativeArray<int>(vertexCount, Allocator.Persistent);
                    var job = new CreateIndicesInt32FlippedJob
                    {
                        result = indices
                    };
                    jobHandle = job.Schedule(indices.Length, GltfImportBase.DefaultBatchCount);
                    break;
                }
                case DrawMode.TriangleStrip:
                {
                    indices = new NativeArray<int>((vertexCount - 2) * 3, Allocator.Persistent);
                    var job = new CreateIndicesForTriangleStripJob
                    {
                        result = indices
                    };
                    jobHandle = job.Schedule(indices.Length, GltfImportBase.DefaultBatchCount);
                    break;
                }
                case DrawMode.TriangleFan:
                    indices = new NativeArray<int>((vertexCount - 2) * 3, Allocator.Persistent);
                    var triangleFanJob = new CreateIndicesForTriangleFanJob
                    {
                        result = indices
                    };
                    jobHandle = triangleFanJob.Schedule(indices.Length, GltfImportBase.DefaultBatchCount);
                    break;
                default:
                {
                    indices = new NativeArray<int>(vertexCount, Allocator.Persistent);
                    var job = new CreateIndicesInt32Job()
                    {
                        result = indices
                    };
                    jobHandle = job.Schedule(vertexCount, GltfImportBase.DefaultBatchCount);
                    break;
                }
            }
            Profiler.EndSample();
        }

        static async Task AwaitJobs(List<JobHandle> tmpList)
        {
            if (tmpList.Count > 0)
            {
                var jobHandles = new NativeArray<JobHandle>(tmpList.ToArray(), Allocator.Persistent);
                var allJobs = JobHandle.CombineDependencies(jobHandles);
                jobHandles.Dispose();
                while (!allJobs.IsCompleted)
                {
                    await Task.Yield();
                }
                allJobs.Complete();
            }
        }
    }
}
