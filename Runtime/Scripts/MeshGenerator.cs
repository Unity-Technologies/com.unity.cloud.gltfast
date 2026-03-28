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

        IndicesData m_Indices;

        readonly SubMeshAssignment[] m_SubMeshAssignments;
        readonly IReadOnlyList<MeshPrimitiveBase> m_Primitives;

        MeshTopology m_Topology;

        int SubMeshCount => m_SubMeshAssignments?.Length ?? m_Primitives.Count;

        public MeshGenerator(
            IReadOnlyList<MeshPrimitiveBase> primitives,
            SubMeshAssignment[] subMeshAssignments,
            string[] morphTargetNames,
            string meshName,
            IGltfReadable gltf,
            IGltfBuffers buffers,
            IDeferAgent deferAgent,
            ICodeLogger logger
        )
            : base(meshName)
        {
            m_Primitives = primitives;
            m_SubMeshAssignments = subMeshAssignments;
            if (CreateVertexGenerator(gltf, buffers, logger, out var hasNormals, out var hasTangents))
            {
                CreateMorphTargetGenerator(morphTargetNames, hasNormals, hasTangents, buffers, deferAgent, logger);
                m_CreationTask = GenerateMesh(buffers, logger);
            }
        }

        bool CreateVertexGenerator(
            IGltfReadable gltf,
            IGltfBuffers buffers,
            ICodeLogger logger,
            out bool hasNormals,
            out bool hasTangents
            )
        {
            var drawMode = m_Primitives[0].mode;
            if (!SetTopology(drawMode))
            {
                logger?.Error(LogCode.PrimitiveModeUnsupported, drawMode.ToString());
            }

            var mainBufferType = GetMainBufferType(gltf, out hasNormals, out hasTangents);

            switch (mainBufferType)
            {
                case MainBufferType.Position:
                    m_VertexData = new VertexBufferGenerator<Vertex.VPos>(m_Primitives.Count, buffers, logger);
                    break;
                case MainBufferType.PosNorm:
                    m_VertexData = new VertexBufferGenerator<Vertex.VPosNorm>(m_Primitives.Count, buffers, logger);
                    break;
                case MainBufferType.PosNormTan:
                    m_VertexData = new VertexBufferGenerator<Vertex.VPosNormTan>(m_Primitives.Count, buffers, logger);
                    break;
                default:
                    logger?.Error(LogCode.BufferMainInvalidType, mainBufferType.ToString());
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
            IGltfReadable gltf,
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
                        var material = gltf.GetSourceMaterial(primitive.material);
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
            IGltfBuffers buffers,
            IDeferAgent deferAgent,
            ICodeLogger logger
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
                    buffers,
                    deferAgent
                );
            }
        }

        async Task<Mesh> GenerateMesh(IGltfBuffers buffers, ICodeLogger logger)
        {
            if (!await m_VertexData.CreateVertexBuffer())
                return null;

            var indexFormat = IndexFormat.UInt16;
            foreach (var primitive in m_Primitives)
            {
                if (primitive.indices >= 0)
                {
                    var accessor = buffers.GetAccessor(primitive.indices);
                    if (accessor.componentType == GltfComponentType.UnsignedInt)
                    {
                        indexFormat = IndexFormat.UInt32;
                        break;
                    }
                }
                else
                {
                    var vertexCount = buffers.GetAccessor(primitive.attributes.POSITION).count;
                    if (vertexCount > ushort.MaxValue)
                    {
                        indexFormat = IndexFormat.UInt32;
                        break;
                    }
                }
            }

            m_Indices = new IndicesData(indexFormat, SubMeshCount);

            var tmpList = new List<JobHandle>(SubMeshCount);
            foreach (var (subMeshIndex, primitive) in IterateSubMeshesIndexed())
            {
                if (primitive.indices >= 0)
                {
                    var flip = primitive.mode == DrawMode.Triangles;
                    var accessor = buffers.GetAccessor(primitive.indices);

                    var minIndexCount = 3;
                    var indexCount = accessor.count;
                    switch (primitive.mode)
                    {
                        case DrawMode.TriangleStrip or DrawMode.TriangleFan:
                            indexCount = (accessor.count - 2) * 3;
                            break;
                        case DrawMode.LineLoop:
                            minIndexCount = 2;
                            indexCount = accessor.count + 1;
                            break;
                        case DrawMode.Lines or DrawMode.LineStrip:
                            minIndexCount = 2;
                            break;
                        case DrawMode.Points:
                            minIndexCount = 1;
                            break;
                    }

                    if (accessor.count < minIndexCount)
                    {
                        logger?.Error(
                            LogCode.IndexCountInvalid,
                            accessor.count.ToString()
                        );
                        return null;
                    }

                    JobHandle? getIndicesJob = null;

                    m_Indices.Allocate(subMeshIndex, indexCount);

                    var accessorData = buffers.GetBufferView(
                        accessor.bufferView,
                        out _,
                        accessor.byteOffset,
                        accessor.ByteSize
                    );

                    Assert.AreEqual(accessor.GetAttributeType(), GltfAccessorAttributeType.SCALAR);
                    if (accessor.IsSparse)
                    {
                        logger?.Error(LogCode.SparseAccessor, "indices");
                    }

                    switch (indexFormat)
                    {
                        case IndexFormat.UInt16:
                        {
                            var indices = m_Indices.GetIndices16(subMeshIndex);
                            GetIndicesUInt16Job(accessor, accessorData, indices, out getIndicesJob, flip, logger);
                            break;
                        }
                        case IndexFormat.UInt32:
                        {
                            var indices = m_Indices.GetIndices32(subMeshIndex);
                            GetIndicesUInt32Job(accessor, accessorData, indices, out getIndicesJob, flip, logger);
                            break;
                        }
                    }
                    if (!getIndicesJob.HasValue)
                        return null;

                    switch (primitive.mode)
                    {
                        case DrawMode.LineLoop:
                        {
                            // Wait for indices to be ready.
                            while (!getIndicesJob.Value.IsCompleted)
                            {
                                await Task.Yield();
                            }
                            getIndicesJob.Value.Complete();

                            if (indexFormat == IndexFormat.UInt16)
                            {
                                var indices = m_Indices.GetIndices16(subMeshIndex);
                                indices[^1] = indices[0];
                            }
                            else
                            {
                                var indices = m_Indices.GetIndices32(subMeshIndex);
                                indices[^1] = indices[0];
                            }

                            break;
                        }
                        case DrawMode.TriangleStrip:
                        {
                            JobHandle job;
                            if (indexFormat == IndexFormat.UInt16)
                            {
                                job = new RecalculateIndicesForTriangleStripInPlaceJob<ushort>
                                {
                                    indices = m_Indices.GetIndices16(subMeshIndex),
                                }.Schedule(getIndicesJob.Value);
                            }
                            else
                            {
                                job = new RecalculateIndicesForTriangleStripInPlaceJob<uint>
                                {
                                    indices = m_Indices.GetIndices32(subMeshIndex),
                                }.Schedule(getIndicesJob.Value);
                            }
                            tmpList.Add(job);
                            break;
                        }
                        case DrawMode.TriangleFan:
                        {
                            JobHandle job;
                            if (indexFormat == IndexFormat.UInt16)
                            {
                                job = new RecalculateIndicesForTriangleFanInPlaceJob<ushort>
                                {
                                    indices = m_Indices.GetIndices16(subMeshIndex),
                                }.Schedule(getIndicesJob.Value);
                            }
                            else
                            {
                                job = new RecalculateIndicesForTriangleFanInPlaceJob<uint>
                                {
                                    indices = m_Indices.GetIndices32(subMeshIndex),
                                }.Schedule(getIndicesJob.Value);
                            }
                            tmpList.Add(job);
                            break;
                        }
                        default:
                            tmpList.Add(getIndicesJob.Value);
                            break;
                    }
                }
                else
                {
                    var vertexCount = buffers.GetAccessor(primitive.attributes.POSITION).count;
                    var indexCount = primitive.mode switch
                    {
                        DrawMode.TriangleStrip or DrawMode.TriangleFan => (vertexCount - 2) * 3,
                        DrawMode.LineLoop => vertexCount + 1,
                        _ => vertexCount
                    };

                    m_Indices.Allocate(subMeshIndex, indexCount);

                    JobHandle job;
                    if (indexFormat == IndexFormat.UInt16)
                    {
                        CalculateIndicesUInt16Job(primitive, m_Indices.GetIndices16(subMeshIndex), out job);
                    }
                    else
                    {
                        CalculateIndicesUInt32Job(primitive, m_Indices.GetIndices32(subMeshIndex), out job);
                    }
                    tmpList.Add(job);
                }
            }

            if (m_MorphTargetsGenerator != null)
            {
                for (var subMeshIndex = 0; subMeshIndex < m_Primitives.Count; subMeshIndex++)
                {
                    var primitive = m_Primitives[subMeshIndex];
                    AddMorphTargets(subMeshIndex, primitive, logger);
                }
                tmpList.Add(m_MorphTargetsGenerator.GetJobHandle());
            }

            await AwaitJobs(tmpList);

            return await CreateMeshResultAsync(logger);
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

        async Task<Mesh> CreateMeshResultAsync(ICodeLogger logger)
        {
            Profiler.BeginSample("CreateMesh");
            var msh = new Mesh
            {
                name = m_MeshName
            };

            m_VertexData.ApplyOnMesh(msh);

            Profiler.BeginSample("SetIndices");
            var indexCount = m_Indices.GetTotalIndexCount();
            Profiler.BeginSample("SetIndexBufferParams");
            msh.SetIndexBufferParams(indexCount, m_Indices.IndexFormat);
            Profiler.EndSample();
            msh.subMeshCount = m_Indices.SubMeshCount;
            indexCount = 0;
            Bounds bounds = default;
            for (var i = 0; i < m_Indices.SubMeshCount; i++)
            {
                Profiler.BeginSample("SetIndexBufferData");
                int subMeshIndexCount;
                if (m_Indices.IndexFormat == IndexFormat.UInt16)
                {
                    var indices = m_Indices.GetIndices16(i);
                    subMeshIndexCount = indices.Length;
                    msh.SetIndexBufferData(indices, 0, indexCount, indices.Length, defaultMeshUpdateFlags);
                }
                else
                {
                    var indices = m_Indices.GetIndices32(i);
                    subMeshIndexCount = indices.Length;
                    msh.SetIndexBufferData(indices, 0, indexCount, indices.Length, defaultMeshUpdateFlags);
                }

                Profiler.EndSample();

                Profiler.BeginSample("SetSubMesh");
                var vertexBufferIndex = m_SubMeshAssignments != null ? m_SubMeshAssignments[i].VertexBufferIndex : i;
                m_VertexData.GetVertexRange(vertexBufferIndex, out var baseVertex, out var vertexCount);
                var subMeshBoundsValid = m_VertexData.TryGetBounds(vertexBufferIndex, logger, out var subMeshBounds);
                var subMeshDescriptor = new SubMeshDescriptor
                {
                    indexStart = indexCount,
                    indexCount = subMeshIndexCount,
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
                indexCount += subMeshIndexCount;
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
                m_Indices.Dispose();
            }
        }


        static void GetIndicesUInt16Job(
            AccessorBase accessor,
            ReadOnlyNativeArray<byte> accessorData,
            NativeArray<ushort> indices,
            out JobHandle? jobHandle,
            bool flip,
            ICodeLogger logger
            )
        {
            Profiler.BeginSample("GetIndicesUInt16Job");
            switch (accessor.componentType)
            {
                case GltfComponentType.UnsignedByte:
                {
                    if (flip)
                    {
                        var job8 = new ConvertIndicesUInt8ToUInt16FlippedJob
                        {
                            input = accessorData.Reinterpret<byte3>().AsNativeArrayReadOnly(),
                            result = indices.Reinterpret<ushort3>(UnsafeUtility.SizeOf<ushort>())
                        };
                        jobHandle = job8.Schedule(accessor.count / 3, GltfImportBase.DefaultBatchCount);
                    }
                    else
                    {
                        var job8 = new ConvertIndicesUInt8ToUInt16Job
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
                        var job16 = new ConvertIndicesUInt16ToUInt16FlippedJob
                        {
                            input = accessorData.Reinterpret<ushort3>().AsNativeArrayReadOnly(),
                            result = indices.Reinterpret<ushort3>(UnsafeUtility.SizeOf<ushort>())
                        };
                        jobHandle = job16.Schedule(accessor.count / 3, GltfImportBase.DefaultBatchCount);
                    }
                    else
                    {
                        unsafe
                        {
                            var job = new MemCopyJob
                            {
                                bufferSize = accessorData.Length,
                                input = (byte*)accessorData.GetUnsafeReadOnlyPtr(),
                                result = (byte*)indices.GetUnsafePtr()
                            };
                            jobHandle = job.Schedule();
                        }
                    }
                    break;
                }
                default:
                    logger?.Error(LogCode.IndexFormatInvalid, accessor.componentType.ToString());
                    jobHandle = null;
                    break;
            }
            Profiler.EndSample();
        }

        static void GetIndicesUInt32Job(
            AccessorBase accessor,
            ReadOnlyNativeArray<byte> accessorData,
            NativeArray<uint> indices,
            out JobHandle? jobHandle,
            bool flip,
            ICodeLogger logger
            )
        {
            Profiler.BeginSample("GetIndicesUInt32Job");
            switch (accessor.componentType)
            {
                case GltfComponentType.UnsignedByte:
                {
                    if (flip)
                    {
                        var job8 = new ConvertIndicesUInt8ToUInt32FlippedJob
                        {
                            input = accessorData.Reinterpret<byte3>().AsNativeArrayReadOnly(),
                            result = indices.Reinterpret<uint3>(UnsafeUtility.SizeOf<uint>())
                        };
                        jobHandle = job8.Schedule(accessor.count / 3, GltfImportBase.DefaultBatchCount);
                    }
                    else
                    {
                        var job8 = new ConvertIndicesUInt8ToUInt32Job
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
                        var job16 = new ConvertIndicesUInt16ToUInt32FlippedJob
                        {
                            input = accessorData.Reinterpret<ushort3>().AsNativeArrayReadOnly(),
                            result = indices.Reinterpret<uint3>(UnsafeUtility.SizeOf<uint>())
                        };
                        jobHandle = job16.Schedule(accessor.count / 3, GltfImportBase.DefaultBatchCount);
                    }
                    else
                    {
                        var job16 = new ConvertIndicesUInt16ToUInt32Job
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
                        var job32 = new ConvertIndicesUInt32ToUInt32FlippedJob
                        {
                            input = accessorData.Reinterpret<uint3>().AsNativeArrayReadOnly(),
                            result = indices.Reinterpret<uint3>(UnsafeUtility.SizeOf<uint>())
                        };
                        jobHandle = job32.Schedule(accessor.count / 3, GltfImportBase.DefaultBatchCount);
                    }
                    else
                    {
                        unsafe
                        {
                            Assert.AreEqual(accessor.count * UnsafeUtility.SizeOf<uint>(), accessorData.Length);
                            var job = new MemCopyJob
                            {
                                bufferSize = accessorData.Length,
                                input = (byte*)accessorData.GetUnsafeReadOnlyPtr(),
                                result = (byte*)indices.GetUnsafePtr()
                            };
                            jobHandle = job.Schedule();
                        }
                    }
                    break;
                }
                default:
                    logger?.Error(LogCode.IndexFormatInvalid, accessor.componentType.ToString());
                    jobHandle = null;
                    break;
            }
            Profiler.EndSample();
        }

        static void CalculateIndicesUInt16Job(
            MeshPrimitiveBase primitive,
            NativeArray<ushort> indices,
            out JobHandle jobHandle
            )
        {
            Profiler.BeginSample("CalculateIndicesJob");
            // No indices: calculate them
            switch (primitive.mode)
            {
                case DrawMode.LineLoop:
                {
                    // Set the last index to the first vertex
                    indices[^1] = 0;
                    var job = new CreateIndicesUInt16Job()
                    {
                        result = indices
                    };
                    jobHandle = job.Schedule(indices.Length - 1, GltfImportBase.DefaultBatchCount);
                    break;
                }
                case DrawMode.Triangles:
                {
                    var job = new CreateIndicesUInt16FlippedJob
                    {
                        result = indices
                    };
                    jobHandle = job.Schedule(indices.Length, GltfImportBase.DefaultBatchCount);
                    break;
                }
                case DrawMode.TriangleStrip:
                {
                    var job = new CreateIndicesForTriangleStripUInt16Job
                    {
                        result = indices
                    };
                    jobHandle = job.Schedule(indices.Length, GltfImportBase.DefaultBatchCount);
                    break;
                }
                case DrawMode.TriangleFan:
                    var triangleFanJob = new CreateIndicesForTriangleFanUInt16Job
                    {
                        result = indices
                    };
                    jobHandle = triangleFanJob.Schedule(indices.Length, GltfImportBase.DefaultBatchCount);
                    break;
                default:
                {
                    var job = new CreateIndicesUInt16Job()
                    {
                        result = indices
                    };
                    jobHandle = job.Schedule(indices.Length, GltfImportBase.DefaultBatchCount);
                    break;
                }
            }
            Profiler.EndSample();
        }

        static void CalculateIndicesUInt32Job(
            MeshPrimitiveBase primitive,
            NativeArray<uint> indices,
            out JobHandle jobHandle
            )
        {
            Profiler.BeginSample("CalculateIndicesJob");
            // No indices: calculate them
            switch (primitive.mode)
            {
                case DrawMode.LineLoop:
                {
                    // Set the last index to the first vertex
                    indices[^1] = 0;
                    var job = new CreateIndicesUInt32Job()
                    {
                        result = indices
                    };
                    jobHandle = job.Schedule(indices.Length - 1, GltfImportBase.DefaultBatchCount);
                    break;
                }
                case DrawMode.Triangles:
                {
                    var job = new CreateIndicesUInt32FlippedJob
                    {
                        result = indices
                    };
                    jobHandle = job.Schedule(indices.Length, GltfImportBase.DefaultBatchCount);
                    break;
                }
                case DrawMode.TriangleStrip:
                {
                    var job = new CreateIndicesForTriangleStripUInt32Job
                    {
                        result = indices
                    };
                    jobHandle = job.Schedule(indices.Length, GltfImportBase.DefaultBatchCount);
                    break;
                }
                case DrawMode.TriangleFan:
                    var triangleFanJob = new CreateIndicesForTriangleFanUInt32Job
                    {
                        result = indices
                    };
                    jobHandle = triangleFanJob.Schedule(indices.Length, GltfImportBase.DefaultBatchCount);
                    break;
                default:
                {
                    var job = new CreateIndicesUInt32Job()
                    {
                        result = indices
                    };
                    jobHandle = job.Schedule(indices.Length, GltfImportBase.DefaultBatchCount);
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
