// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

namespace GLTFast
{

    using Logging;
    using Schema;

    abstract class VertexBufferTexCoordsBase
    {

        protected ICodeLogger m_Logger;

        protected VertexBufferTexCoordsBase(ICodeLogger logger)
        {
            m_Logger = logger;
        }

        public int UVSetCount { get; protected set; }
        public abstract bool ScheduleVertexUVJobs(IGltfBuffers buffers, int[] uvAccessorIndices, int vertexCount, NativeSlice<JobHandle> handles);
        public abstract void AddDescriptors(VertexAttributeDescriptor[] dst, ref int offset, int stream);
        public abstract void ApplyOnMesh(UnityEngine.Mesh msh, int stream, MeshUpdateFlags flags = MeshResultGeneratorBase.defaultMeshUpdateFlags);
        public abstract void Dispose();
    }

    class VertexBufferTexCoords<T> : VertexBufferTexCoordsBase where T : struct
    {
        NativeArray<T> m_Data;

        public VertexBufferTexCoords(ICodeLogger logger) : base(logger) { }

        public override unsafe bool ScheduleVertexUVJobs(IGltfBuffers buffers, int[] uvAccessorIndices, int vertexCount, NativeSlice<JobHandle> handles)
        {
            Profiler.BeginSample("ScheduleVertexUVJobs");
            Profiler.BeginSample("AllocateNativeArray");
            m_Data = new NativeArray<T>(vertexCount, VertexBufferGeneratorBase.defaultAllocator);
            var vDataPtr = (byte*)NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(m_Data);
            Profiler.EndSample();
            UVSetCount = uvAccessorIndices.Length;
            int outputByteStride = uvAccessorIndices.Length * 8;

            for (int i = 0; i < uvAccessorIndices.Length; i++)
            {
                var accIndex = uvAccessorIndices[i];
                buffers.GetAccessorAndData(accIndex, out var uvAcc, out var data, out var byteStride);
                if (uvAcc.IsSparse)
                {
                    m_Logger?.Error(LogCode.SparseAccessor, "UVs");
                    Profiler.EndSample();
                    return false;
                }
                var h = GetUvsJob(
                    data,
                    uvAcc.count,
                    uvAcc.componentType,
                    byteStride,
                    (float2*)(vDataPtr + (i * 8)),
                    outputByteStride,
                    uvAcc.normalized
                );
                if (h.HasValue)
                {
                    handles[i] = h.Value;
                }
                else
                {
                    Profiler.EndSample();
                    return false;
                }
            }
            Profiler.EndSample();
            return true;
        }

        public override void AddDescriptors(VertexAttributeDescriptor[] dst, ref int offset, int stream)
        {
            for (int i = 0; i < UVSetCount; i++)
            {
                var vertexAttribute = (VertexAttribute)((int)VertexAttribute.TexCoord0 + i);
                dst[offset] = new VertexAttributeDescriptor(vertexAttribute, VertexAttributeFormat.Float32, 2, stream);
                offset++;
            }
        }

        public override void ApplyOnMesh(UnityEngine.Mesh msh, int stream, MeshUpdateFlags flags = MeshResultGeneratorBase.defaultMeshUpdateFlags)
        {
            Profiler.BeginSample("ApplyUVs");
            msh.SetVertexBufferData(m_Data, 0, 0, m_Data.Length, stream, flags);
            Profiler.EndSample();
        }

        public override void Dispose()
        {
            if (m_Data.IsCreated)
            {
                m_Data.Dispose();
            }
        }

        unsafe JobHandle? GetUvsJob(
            void* input,
            int count,
            GltfComponentType inputType,
            int inputByteStride,
            float2* output,
            int outputByteStride,
            bool normalized = false
            )
        {
            Profiler.BeginSample("PrepareUVs");
            JobHandle? jobHandle = null;

            switch (inputType)
            {
                case GltfComponentType.Float:
                    {
                        var jobUv = new Jobs.ConvertUVsFloatToFloatInterleavedJob
                        {
                            inputByteStride = (inputByteStride > 0) ? inputByteStride : 8,
                            input = (byte*)input,
                            outputByteStride = outputByteStride,
                            result = output
                        };
                        jobHandle = jobUv.ScheduleBatch(count, GltfImportBase.DefaultBatchCount);
                    }
                    break;
                case GltfComponentType.UnsignedByte:
                    if (normalized)
                    {
                        var jobUv = new Jobs.ConvertUVsUInt8ToFloatInterleavedNormalizedJob
                        {
                            inputByteStride = (inputByteStride > 0) ? inputByteStride : 2,
                            input = (byte*)input,
                            outputByteStride = outputByteStride,
                            result = output
                        };
                        jobHandle = jobUv.Schedule(count, GltfImportBase.DefaultBatchCount);
                    }
                    else
                    {
                        var jobUv = new Jobs.ConvertUVsUInt8ToFloatInterleavedJob
                        {
                            inputByteStride = (inputByteStride > 0) ? inputByteStride : 2,
                            input = (byte*)input,
                            outputByteStride = outputByteStride,
                            result = output
                        };
                        jobHandle = jobUv.ScheduleBatch(count, GltfImportBase.DefaultBatchCount);
                    }
                    break;
                case GltfComponentType.UnsignedShort:
                    if (normalized)
                    {
                        var jobUv = new Jobs.ConvertUVsUInt16ToFloatInterleavedNormalizedJob
                        {
                            inputByteStride = (inputByteStride > 0) ? inputByteStride : 4,
                            input = (byte*)input,
                            outputByteStride = outputByteStride,
                            result = output
                        };
                        jobHandle = jobUv.Schedule(count, GltfImportBase.DefaultBatchCount);
                    }
                    else
                    {
                        var jobUv = new Jobs.ConvertUVsUInt16ToFloatInterleavedJob
                        {
                            inputByteStride = (inputByteStride > 0) ? inputByteStride : 4,
                            input = (byte*)input,
                            outputByteStride = outputByteStride,
                            result = output
                        };
                        jobHandle = jobUv.ScheduleBatch(count, GltfImportBase.DefaultBatchCount);
                    }
                    break;
                case GltfComponentType.Short:
                    if (normalized)
                    {
                        var job = new Jobs.ConvertUVsInt16ToFloatInterleavedNormalizedJob
                        {
                            inputByteStride = inputByteStride > 0 ? inputByteStride : 4,
                            input = (System.Int16*)input,
                            outputByteStride = outputByteStride,
                            result = output
                        };
                        jobHandle = job.ScheduleBatch(count, GltfImportBase.DefaultBatchCount);
                    }
                    else
                    {
                        var job = new Jobs.ConvertUVsInt16ToFloatInterleavedJob
                        {
                            inputByteStride = inputByteStride > 0 ? inputByteStride : 4,
                            input = (System.Int16*)input,
                            outputByteStride = outputByteStride,
                            result = output
                        };
                        jobHandle = job.ScheduleBatch(count, GltfImportBase.DefaultBatchCount);
                    }
                    break;
                case GltfComponentType.Byte:
                    if (normalized)
                    {
                        var jobInt8 = new Jobs.ConvertUVsInt8ToFloatInterleavedNormalizedJob
                        {
                            inputByteStride = inputByteStride > 0 ? inputByteStride : 2,
                            input = (sbyte*)input,
                            outputByteStride = outputByteStride,
                            result = output
                        };
                        jobHandle = jobInt8.ScheduleBatch(count, GltfImportBase.DefaultBatchCount);
                    }
                    else
                    {
                        var jobInt8 = new Jobs.ConvertUVsInt8ToFloatInterleavedJob
                        {
                            inputByteStride = inputByteStride > 0 ? inputByteStride : 2,
                            input = (sbyte*)input,
                            outputByteStride = outputByteStride,
                            result = output
                        };
                        jobHandle = jobInt8.ScheduleBatch(count, GltfImportBase.DefaultBatchCount);
                    }
                    break;
                default:
                    m_Logger?.Error(LogCode.TypeUnsupported, "UV", inputType.ToString());
                    break;
            }
            Profiler.EndSample();
            return jobHandle;
        }
    }
}
