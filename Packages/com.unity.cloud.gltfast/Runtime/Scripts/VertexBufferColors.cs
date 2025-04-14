// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
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

    sealed class VertexBufferColors : IDisposable
    {
        readonly ICodeLogger m_Logger;

        NativeArray<float4> m_Data;

        public VertexBufferColors(int vertexCount, ICodeLogger logger)
        {
            m_Logger = logger;
            Profiler.BeginSample("VertexBufferColors.Allocate");
            m_Data = new NativeArray<float4>(vertexCount, VertexBufferGeneratorBase.defaultAllocator);
            Profiler.EndSample();
        }

        public unsafe bool ScheduleVertexColorJob(
            int colorAccessorIndex,
            int offset,
            NativeSlice<JobHandle> handles,
            IGltfBuffers buffers
            )
        {
            Profiler.BeginSample("VertexBufferColors.Schedule");
            buffers.GetAccessorAndData(colorAccessorIndex, out var colorAcc, out var data, out var byteStride);
            if (colorAcc.IsSparse)
            {
                m_Logger?.Error(LogCode.SparseAccessor, "color");
            }

            var h = GetColors32Job(
                data,
                colorAcc.componentType,
                colorAcc.GetAttributeType(),
                byteStride,
                colorAcc.count,
                (float4*)((byte*)m_Data.GetUnsafePtr() + offset * sizeof(float4))
            );
            if (h.HasValue)
            {
                handles[0] = h.Value;
            }
            else
            {
                Profiler.EndSample();
                return false;
            }
            Profiler.EndSample();
            return true;
        }

        public void AddDescriptors(VertexAttributeDescriptor[] dst, int offset, int stream)
        {
            dst[offset] = new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.Float32, 4, stream);
        }

        public void ApplyOnMesh(
            UnityEngine.Mesh msh,
            int stream,
            MeshUpdateFlags flags = MeshGeneratorBase.defaultMeshUpdateFlags
            )
        {
            Profiler.BeginSample("ApplyUVs");
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

        unsafe JobHandle? GetColors32Job(
            void* input,
            GltfComponentType inputType,
            GltfAccessorAttributeType attributeType,
            int inputByteStride,
            int count,
            float4* output
            )
        {
            Profiler.BeginSample("PrepareColors32");
            JobHandle? jobHandle = null;

            if (attributeType == GltfAccessorAttributeType.VEC3)
            {
                switch (inputType)
                {
                    case GltfComponentType.UnsignedByte:
                        {
                            var job = new Jobs.ConvertColorsRgbUInt8ToRGBAFloatJob
                            {
                                input = (byte*)input,
                                inputByteStride = inputByteStride > 0 ? inputByteStride : 3,
                                result = output
                            };
                            jobHandle = job.Schedule(count, GltfImport.DefaultBatchCount);
                        }
                        break;
                    case GltfComponentType.Float:
                        {
                            var job = new Jobs.ConvertColorsRGBFloatToRGBAFloatJob
                            {
                                input = (byte*)input,
                                inputByteStride = inputByteStride > 0 ? inputByteStride : 12,
                                result = output
                            };
                            jobHandle = job.Schedule(count, GltfImport.DefaultBatchCount);
                        }
                        break;
                    case GltfComponentType.UnsignedShort:
                        {
                            var job = new Jobs.ConvertColorsRgbUInt16ToRGBAFloatJob
                            {
                                input = (ushort*)input,
                                inputByteStride = inputByteStride > 0 ? inputByteStride : 6,
                                result = output
                            };
                            jobHandle = job.Schedule(count, GltfImport.DefaultBatchCount);
                        }
                        break;
                    default:
                        m_Logger?.Error(LogCode.ColorFormatUnsupported, attributeType.ToString());
                        break;
                }
            }
            else if (attributeType == GltfAccessorAttributeType.VEC4)
            {
                switch (inputType)
                {
                    case GltfComponentType.UnsignedByte:
                        {
                            var job = new Jobs.ConvertColorsRgbaUInt8ToRGBAFloatJob
                            {
                                input = (byte*)input,
                                inputByteStride = inputByteStride > 0 ? inputByteStride : 4,
                                result = output
                            };
                            jobHandle = job.Schedule(count, GltfImport.DefaultBatchCount);
                        }
                        break;
                    case GltfComponentType.Float:
                        {
                            if (inputByteStride == 16 || inputByteStride <= 0)
                            {
                                var job = new Jobs.MemCopyJob
                                {
                                    bufferSize = count * 16,
                                    input = input,
                                    result = output
                                };
                                jobHandle = job.Schedule();
                            }
                            else
                            {
                                var job = new Jobs.ConvertColorsRGBAFloatToRGBAFloatJob
                                {
                                    input = (byte*)input,
                                    inputByteStride = inputByteStride,
                                    result = output
                                };
#if UNITY_COLLECTIONS
                                jobHandle = job.ScheduleBatch(count,GltfImport.DefaultBatchCount);
#else
                                jobHandle = job.Schedule(count, GltfImport.DefaultBatchCount);
#endif
                            }
                        }
                        break;
                    case GltfComponentType.UnsignedShort:
                        {
                            var job = new Jobs.ConvertColorsRgbaUInt16ToRGBAFloatJob
                            {
                                input = (ushort*)input,
                                inputByteStride = inputByteStride > 0 ? inputByteStride : 8,
                                result = output
                            };
#if UNITY_COLLECTIONS
                            jobHandle = job.ScheduleBatch(count,GltfImport.DefaultBatchCount);
#else
                            jobHandle = job.Schedule(count, GltfImport.DefaultBatchCount);
#endif
                        }
                        break;
                    default:
                        m_Logger?.Error(LogCode.ColorFormatUnsupported, attributeType.ToString());
                        break;
                }
            }
            else
            {
                m_Logger?.Error(LogCode.TypeUnsupported, "color accessor", inputType.ToString());
            }
            Profiler.EndSample();
            return jobHandle;
        }
    }
}
