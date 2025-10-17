// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using GLTFast.Jobs;
using GLTFast.Logging;
using GLTFast.Schema;
using GLTFast.Vertex;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

namespace GLTFast
{
    sealed class VertexBufferBones : IDisposable
    {
        readonly ICodeLogger m_Logger;

        NativeArray<VBones> m_Data;

        public VertexBufferBones(int vertexCount, ICodeLogger logger)
        {
            m_Logger = logger;
            Profiler.BeginSample("AllocateNativeArray");
            m_Data = new NativeArray<VBones>(vertexCount, VertexBufferGeneratorBase.defaultAllocator);
            Profiler.EndSample();
        }

        public unsafe JobHandle? ScheduleVertexBonesJob(
            int weightsAccessorIndex,
            int jointsAccessorIndex,
            int offset,
            IGltfBuffers buffers
        )
        {
            Profiler.BeginSample("ScheduleVertexBonesJob");

            buffers.GetAccessorAndData(weightsAccessorIndex, out var weightsAcc, out var weightsData, out var weightsByteStride);
            if (weightsAcc.IsSparse)
            {
                m_Logger?.Error(LogCode.SparseAccessor, "bone weights");
            }
            // Ignoring NativeArray safety here, so that multiple jobs (one per sub-mesh) can write to the same array.
            var vDataPtr = (byte*)NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(m_Data);

            JobHandle weightsHandle;
            JobHandle jointsHandle;

            {
                var h = GetWeightsJob(
                    weightsData,
                    weightsAcc.count,
                    weightsAcc.componentType,
                    weightsByteStride,
                    (float4*)(vDataPtr + offset * sizeof(VBones)),
                    32
                );
                if (h.HasValue)
                {
                    weightsHandle = h.Value;
                }
                else
                {
                    Profiler.EndSample();
                    return null;
                }
            }

            {
                buffers.GetAccessorAndData(jointsAccessorIndex, out var jointsAcc, out var jointsData, out var jointsByteStride);
                if (jointsAcc.IsSparse)
                {
                    m_Logger?.Error(LogCode.SparseAccessor, "bone joints");
                }
                var h = GetJointsJob(
                    jointsData,
                    jointsAcc.count,
                    jointsAcc.componentType,
                    jointsByteStride,
                    (uint4*)(vDataPtr + offset * sizeof(VBones) + sizeof(float4)),
                    32,
                    m_Logger
                );
                if (h.HasValue)
                {
                    jointsHandle = h.Value;
                }
                else
                {
                    Profiler.EndSample();
                    return null;
                }
            }

            Profiler.EndSample();
            return JobHandle.CombineDependencies(weightsHandle, jointsHandle);
        }

        public JobHandle ScheduleSortAndNormalizeBoneWeightsJob(JobHandle dependsOn)
        {
            var skinWeights = (int)QualitySettings.skinWeights;

#if UNITY_EDITOR
            // If this is design-time import, fix and import all weights.
            if(!UnityEditor.EditorApplication.isPlaying || skinWeights < 4) {
                if (!UnityEditor.EditorApplication.isPlaying) {
                    skinWeights = 4;
                }
#else
            if (skinWeights < 4)
            {
#endif
                return new SortAndNormalizeBoneWeightsJob
                {
                    bones = m_Data,
                    skinWeights = math.max(1, skinWeights)
                }.Schedule(m_Data.Length, GltfImport.DefaultBatchCount, dependsOn);
            }
#if GLTFAST_SAFE
            else {
                // Re-normalizing alone is sufficient
                return new RenormalizeBoneWeightsJob {
                    bones = m_Data,
                }.Schedule(m_Data.Length, GltfImport.DefaultBatchCount, dependsOn);
            }
#endif
            return dependsOn;
        }

        public void AddDescriptors(VertexAttributeDescriptor[] dst, int offset, int stream)
        {
            dst[offset] = new VertexAttributeDescriptor(VertexAttribute.BlendWeight, VertexAttributeFormat.Float32, 4, stream);
            dst[offset + 1] = new VertexAttributeDescriptor(VertexAttribute.BlendIndices, VertexAttributeFormat.UInt32, 4, stream);
        }

        public void ApplyOnMesh(
            UnityEngine.Mesh msh,
            int stream,
            MeshUpdateFlags flags = MeshGeneratorBase.defaultMeshUpdateFlags
            )
        {
            Profiler.BeginSample("ApplyBones");
            msh.SetVertexBufferData(m_Data, 0, 0, m_Data.Length, stream, flags);
            Profiler.EndSample();
        }

        public void Dispose()
        {
            if (m_Data.IsCreated)
            {
                m_Data.Dispose();
            }
        }

        unsafe JobHandle? GetWeightsJob(
            void* input,
            int count,
            GltfComponentType inputType,
            int inputByteStride,
            float4* output,
            int outputByteStride
            )
        {
            Profiler.BeginSample("GetWeightsJob");
            JobHandle? jobHandle;
            switch (inputType)
            {
                case GltfComponentType.Float:
                    var jobTangentI = new ConvertBoneWeightsFloatToFloatInterleavedJob();
                    jobTangentI.inputByteStride = inputByteStride > 0 ? inputByteStride : 16;
                    jobTangentI.input = (byte*)input;
                    jobTangentI.outputByteStride = outputByteStride;
                    jobTangentI.result = output;
#if UNITY_COLLECTIONS
                    jobHandle = jobTangentI.ScheduleBatch(count,GltfImport.DefaultBatchCount);
#else
                    jobHandle = jobTangentI.Schedule(count, GltfImport.DefaultBatchCount);
#endif
                    break;
                case GltfComponentType.UnsignedShort:
                {
                    var job = new ConvertBoneWeightsUInt16ToFloatInterleavedJob
                    {
                        inputByteStride = inputByteStride > 0 ? inputByteStride : 8,
                        input = (byte*)input,
                        outputByteStride = outputByteStride,
                        result = output
                    };
#if UNITY_COLLECTIONS
                    jobHandle = job.ScheduleBatch(count,GltfImport.DefaultBatchCount);
#else
                    jobHandle = job.Schedule(count, GltfImport.DefaultBatchCount);
#endif
                    break;
                }
                case GltfComponentType.UnsignedByte:
                {
                    var job = new ConvertBoneWeightsUInt8ToFloatInterleavedJob
                    {
                        inputByteStride = inputByteStride > 0 ? inputByteStride : 4,
                        input = (byte*)input,
                        outputByteStride = outputByteStride,
                        result = output
                    };
#if UNITY_COLLECTIONS
                    jobHandle = job.ScheduleBatch(count,GltfImport.DefaultBatchCount);
#else
                    jobHandle = job.Schedule(count, GltfImport.DefaultBatchCount);
#endif
                    break;
                }
                default:
                    m_Logger?.Error(LogCode.TypeUnsupported, "Weights", inputType.ToString());
                    jobHandle = null;
                    break;
            }

            Profiler.EndSample();
            return jobHandle;
        }

        static unsafe JobHandle? GetJointsJob(
            void* input,
            int count,
            GltfComponentType inputType,
            int inputByteStride,
            uint4* output,
            int outputByteStride,
            ICodeLogger logger
        )
        {
            Profiler.BeginSample("GetJointsJob");
            JobHandle? jobHandle;
            switch (inputType)
            {
                case GltfComponentType.UnsignedByte:
                    var jointsUInt8Job = new ConvertBoneJointsUInt8ToUInt32Job();
                    jointsUInt8Job.inputByteStride = inputByteStride > 0 ? inputByteStride : 4;
                    jointsUInt8Job.input = (byte*)input;
                    jointsUInt8Job.outputByteStride = outputByteStride;
                    jointsUInt8Job.result = output;
                    jobHandle = jointsUInt8Job.Schedule(count, GltfImport.DefaultBatchCount);
                    break;
                case GltfComponentType.UnsignedShort:
                    var jointsUInt16Job = new ConvertBoneJointsUInt16ToUInt32Job();
                    jointsUInt16Job.inputByteStride = inputByteStride > 0 ? inputByteStride : 8;
                    jointsUInt16Job.input = (byte*)input;
                    jointsUInt16Job.outputByteStride = outputByteStride;
                    jointsUInt16Job.result = output;
                    jobHandle = jointsUInt16Job.Schedule(count, GltfImport.DefaultBatchCount);
                    break;
                default:
                    logger?.Error(LogCode.TypeUnsupported, "Joints", inputType.ToString());
                    jobHandle = null;
                    break;
            }

            Profiler.EndSample();
            return jobHandle;
        }
    }
}
