// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

namespace GLTFast
{
    using Jobs;
    using Logging;
    using Schema;

    abstract class VertexBufferGeneratorBase : IDisposable
    {
        /// <summary>Maximum number of texture coordinate sets Unity supports.</summary>
        /// <seealso href="https://docs.unity3d.com/6000.2/Documentation/ScriptReference/Mesh.html"/>
        public const int maxUvSetCount = 8;

        public const Allocator defaultAllocator = Allocator.Persistent;

        protected Attributes[] m_Attributes;
        protected int m_AttributeCount;

        public bool calculateNormals = false;
        public bool calculateTangents = false;

        protected VertexAttributeDescriptor[] m_Descriptors;
        protected IGltfBuffers m_Buffers;
        protected ICodeLogger m_Logger;

        protected VertexBufferGeneratorBase(int primitiveCount, IGltfBuffers buffers, ICodeLogger logger)
        {
            m_Attributes = new Attributes[primitiveCount];
            m_Buffers = buffers;
            m_Logger = logger;
        }

        public abstract void AddPrimitive(Attributes att);
        public abstract void Initialize();
        public abstract Task<bool> CreateVertexBuffer();

        public abstract void ApplyOnMesh(UnityEngine.Mesh msh, MeshUpdateFlags flags = MeshGeneratorBase.defaultMeshUpdateFlags);
        public abstract int VertexCount { get; }
        public abstract int[] VertexIntervals { get; protected set; }
        public abstract void GetVertexRange(int subMesh, out int baseVertex, out int vertexCount);
        public abstract bool TryGetBounds(int subMesh, ICodeLogger logger, out Bounds bounds);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected abstract void Dispose(bool disposing);

        /// <summary>
        /// Schedules a job that converts input data into float3 arrays.
        /// </summary>
        /// <param name="buffers">Buffer provider</param>
        /// <param name="accessor">glTF accessor</param>
        /// <param name="output">Points at the destination buffer in memory</param>
        /// <param name="outputByteStride">Output byte stride</param>
        /// <param name="normalized">If true, integer values have to be normalized</param>
        /// <param name="ensureUnitLength">If true, normalized values will be scaled to have unit length again (only if <see cref="normalized"/>is true)</param>
        /// <returns></returns>
        public static unsafe JobHandle? GetVector3Job(
            IGltfBuffers buffers,
            AccessorBase accessor,
            float3* output,
            int outputByteStride,
            bool normalized = false,
            bool ensureUnitLength = true
        )
        {
            JobHandle? jobHandle;

            Profiler.BeginSample("GetVector3Job");
            if (accessor.componentType == GltfComponentType.Float)
            {
                var input = buffers.GetStridedAccessorData<float3>(
                    accessor.bufferView,
                    accessor.count,
                    accessor.byteOffset
                );
                var job = new ConvertVector3FloatToFloatInterleavedJob
                {
                    input = input,
                    outputByteStride = outputByteStride,
                    result = output
                };
#if UNITY_COLLECTIONS
                jobHandle = job.ScheduleBatch(accessor.count,GltfImport.DefaultBatchCount);
#else
                jobHandle = job.Schedule(accessor.count, GltfImport.DefaultBatchCount);
#endif
            }
            else
            if (accessor.componentType == GltfComponentType.UnsignedShort)
            {
                var input = buffers.GetStridedAccessorData<ushort3>(
                    accessor.bufferView,
                    accessor.count,
                    accessor.byteOffset
                );
                if (normalized)
                {
                    var job = new ConvertPositionsUInt16ToFloatInterleavedNormalizedJob
                    {
                        input = input,
                        outputByteStride = outputByteStride,
                        result = output
                    };
#if UNITY_COLLECTIONS
                    jobHandle = job.ScheduleBatch(accessor.count,GltfImport.DefaultBatchCount);
#else
                    jobHandle = job.Schedule(accessor.count, GltfImport.DefaultBatchCount);
#endif
                }
                else
                {
                    var job = new ConvertPositionsUInt16ToFloatInterleavedJob
                    {
                        input = input,
                        outputByteStride = outputByteStride,
                        result = output
                    };
#if UNITY_COLLECTIONS
                    jobHandle = job.ScheduleBatch(accessor.count,GltfImport.DefaultBatchCount);
#else
                    jobHandle = job.Schedule(accessor.count, GltfImport.DefaultBatchCount);
#endif
                }
            }
            else
            if (accessor.componentType == GltfComponentType.Short)
            {
                var input = buffers.GetStridedAccessorData<short3>(
                    accessor.bufferView,
                    accessor.count,
                    accessor.byteOffset
                );
                if (normalized)
                {
                    if (ensureUnitLength)
                    {
                        // TODO: test. did not have test files
                        var job = new ConvertNormalsInt16ToFloatInterleavedNormalizedJob
                        {
                            input = input,
                            outputByteStride = outputByteStride,
                            result = output
                        };
#if UNITY_COLLECTIONS
                        jobHandle = job.ScheduleBatch(accessor.count, GltfImport.DefaultBatchCount);
#else
                        jobHandle = job.Schedule(accessor.count, GltfImport.DefaultBatchCount);
#endif
                    }
                    else
                    {
                        var job = new ConvertVector3Int16ToFloatInterleavedNormalizedJob
                        {
                            input = input,
                            outputByteStride = outputByteStride,
                            result = output
                        };
#if UNITY_COLLECTIONS
                        jobHandle = job.ScheduleBatch(accessor.count, GltfImport.DefaultBatchCount);
#else
                        jobHandle = job.Schedule(accessor.count, GltfImport.DefaultBatchCount);
#endif
                    }
                }
                else
                {
                    var job = new ConvertPositionsInt16ToFloatInterleavedJob
                    {
                        input = input,
                        outputByteStride = outputByteStride,
                        result = output
                    };
#if UNITY_COLLECTIONS
                    jobHandle = job.ScheduleBatch(accessor.count, GltfImport.DefaultBatchCount);
#else
                    jobHandle = job.Schedule(accessor.count, GltfImport.DefaultBatchCount);
#endif
                }
            }
            else
            if (accessor.componentType == GltfComponentType.Byte)
            {
                var input = buffers.GetStridedAccessorData<sbyte3>(
                    accessor.bufferView,
                    accessor.count,
                    accessor.byteOffset
                );
                if (normalized)
                {
                    if (ensureUnitLength)
                    {
                        var job = new ConvertNormalsInt8ToFloatInterleavedNormalizedJob
                        {
                            input = input,
                            outputByteStride = outputByteStride,
                            result = output
                        };
#if UNITY_COLLECTIONS
                        jobHandle = job.ScheduleBatch(accessor.count, GltfImport.DefaultBatchCount);
#else
                        jobHandle = job.Schedule(accessor.count, GltfImport.DefaultBatchCount);
#endif
                    }
                    else
                    {
                        var job = new ConvertVector3Int8ToFloatInterleavedNormalizedJob()
                        {
                            input = input,
                            outputByteStride = outputByteStride,
                            result = output
                        };
#if UNITY_COLLECTIONS
                        jobHandle = job.ScheduleBatch(accessor.count, GltfImport.DefaultBatchCount);
#else
                        jobHandle = job.Schedule(accessor.count, GltfImport.DefaultBatchCount);
#endif
                    }
                }
                else
                {
                    // TODO: test positions. did not have test files
                    var job = new ConvertPositionsInt8ToFloatInterleavedJob
                    {
                        input = input,
                        outputByteStride = outputByteStride,
                        result = output
                    };
#if UNITY_COLLECTIONS
                    jobHandle = job.ScheduleBatch(accessor.count, GltfImport.DefaultBatchCount);
#else
                    jobHandle = job.Schedule(accessor.count, GltfImport.DefaultBatchCount);
#endif
                }
            }
            else
            if (accessor.componentType == GltfComponentType.UnsignedByte)
            {
                var input = buffers.GetStridedAccessorData<byte3>(
                    accessor.bufferView,
                    accessor.count,
                    accessor.byteOffset
                );
                // TODO: test. did not have test files
                if (normalized)
                {
                    var job = new ConvertPositionsUInt8ToFloatInterleavedNormalizedJob
                    {
                        input = input,
                        outputByteStride = outputByteStride,
                        result = output
                    };
#if UNITY_COLLECTIONS
                    jobHandle = job.ScheduleBatch(accessor.count, GltfImport.DefaultBatchCount);
#else
                    jobHandle = job.Schedule(accessor.count, GltfImport.DefaultBatchCount);
#endif
                }
                else
                {
                    var job = new ConvertPositionsUInt8ToFloatInterleavedJob
                    {
                        input = input,
                        outputByteStride = outputByteStride,
                        result = output
                    };
#if UNITY_COLLECTIONS
                    jobHandle = job.ScheduleBatch(accessor.count, GltfImport.DefaultBatchCount);
#else
                    jobHandle = job.Schedule(accessor.count, GltfImport.DefaultBatchCount);
#endif
                }
            }
            else
            {
                Debug.LogError("Unknown componentType");
                jobHandle = null;
            }
            Profiler.EndSample();
            return jobHandle;
        }

        protected unsafe JobHandle? GetTangentsJob(
            void* input,
            int count,
            GltfComponentType inputType,
            int inputByteStride,
            float4* output,
            int outputByteStride,
            bool normalized = false
            )
        {
            Profiler.BeginSample("GetTangentsJob");
            JobHandle? jobHandle;
            switch (inputType)
            {
                case GltfComponentType.Float:
                {
                    var jobTangent = new ConvertTangentsFloatToFloatInterleavedJob
                    {
                        inputByteStride = inputByteStride > 0 ? inputByteStride : 16,
                        input = (byte*)input,
                        outputByteStride = outputByteStride,
                        result = output
                    };
#if UNITY_COLLECTIONS
                    jobHandle = jobTangent.ScheduleBatch(count,GltfImport.DefaultBatchCount);
#else
                    jobHandle = jobTangent.Schedule(count, GltfImport.DefaultBatchCount);
#endif
                    break;
                }
                case GltfComponentType.Short:
                {
                    Assert.IsTrue(normalized);
                    var jobTangent = new ConvertTangentsInt16ToFloatInterleavedNormalizedJob
                    {
                        inputByteStride = inputByteStride > 0 ? inputByteStride : 8,
                        input = (short*)input,
                        outputByteStride = outputByteStride,
                        result = output
                    };
#if UNITY_COLLECTIONS
                    jobHandle = jobTangent.ScheduleBatch(count,GltfImport.DefaultBatchCount);
#else
                    jobHandle = jobTangent.Schedule(count, GltfImport.DefaultBatchCount);
#endif
                    break;
                }
                case GltfComponentType.Byte:
                {
                    Assert.IsTrue(normalized);
                    var jobTangent = new ConvertTangentsInt8ToFloatInterleavedNormalizedJob
                    {
                        inputByteStride = inputByteStride > 0 ? inputByteStride : 4,
                        input = (sbyte*)input,
                        outputByteStride = outputByteStride,
                        result = output
                    };
#if UNITY_COLLECTIONS
                    jobHandle = jobTangent.ScheduleBatch(count,GltfImport.DefaultBatchCount);
#else
                    jobHandle = jobTangent.Schedule(count, GltfImport.DefaultBatchCount);
#endif
                    break;
                }
                default:
                    m_Logger?.Error(LogCode.TypeUnsupported, "Tangent", inputType.ToString());
                    jobHandle = null;
                    break;
            }

            Profiler.EndSample();
            return jobHandle;
        }

        public static unsafe JobHandle? GetVector3SparseJob(
            void* indexBuffer,
            void* valueBuffer,
            int sparseCount,
            GltfComponentType indexType,
            GltfComponentType valueType,
            float3* output,
            int outputByteStride,
            ref JobHandle? dependsOn,
            bool normalized = false
        )
        {
            Profiler.BeginSample("GetVector3SparseJob");
            var job = new ConvertVector3SparseJob
            {
                indexBuffer = (ushort*)indexBuffer,
                indexConverter = CachedFunction.GetIndexConverter(indexType),
                inputByteStride = 3 * AccessorBase.GetComponentTypeSize(valueType),
                input = valueBuffer,
                valueConverter = CachedFunction.GetPositionConverter(valueType, normalized),
                outputByteStride = outputByteStride,
                result = output,
            };

            JobHandle? jobHandle = job.Schedule(
                sparseCount,
                GltfImport.DefaultBatchCount,
                dependsOn: dependsOn ?? default(JobHandle)
            );
            Profiler.EndSample();
            return jobHandle;
        }
    }
}
