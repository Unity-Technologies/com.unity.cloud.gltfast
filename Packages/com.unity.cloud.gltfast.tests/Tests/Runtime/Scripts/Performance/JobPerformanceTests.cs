// SPDX-FileCopyrightText: 2024 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using GLTFast.Schema;
using NUnit.Framework;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.PerformanceTesting;
using UnityEngine;

namespace GLTFast.Tests.Performance.Jobs
{
    [TestFixture]
    public class Vector3Jobs
    {

        const int k_Length = 50_000;

        NativeArray<float3> m_Input;
        NativeArray<ushort> m_InputUInt16;
        NativeArray<short> m_InputInt16;
        NativeArray<byte> m_InputUInt8;
        NativeArray<sbyte> m_InputInt8;
        NativeArray<float3> m_Output;

        [OneTimeSetUp]
        public void SetUpTest()
        {
            m_Input = new NativeArray<float3>(k_Length, Allocator.Persistent);
            m_InputUInt16 = new NativeArray<ushort>(k_Length * 3, Allocator.Persistent);
            m_InputInt16 = new NativeArray<short>(k_Length * 3, Allocator.Persistent);
            m_InputUInt8 = new NativeArray<byte>(k_Length * 3, Allocator.Persistent);
            m_InputInt8 = new NativeArray<sbyte>(k_Length * 3, Allocator.Persistent);
            m_Output = new NativeArray<float3>(k_Length, Allocator.Persistent);
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            m_Input.Dispose();
            m_InputUInt16.Dispose();
            m_InputInt16.Dispose();
            m_InputUInt8.Dispose();
            m_InputInt8.Dispose();
            m_Output.Dispose();
        }

        [Test, Performance]
        public unsafe void ConvertVector3FloatToFloatInterleavedJob()
        {
#if !RUN_PERFORMANCE_TESTS
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
#endif
            var input = Utils.GetStridedArray(m_Input);
            var job = new GLTFast.Jobs.ConvertVector3FloatToFloatInterleavedJob
            {
                input = input,
                outputByteStride = 12,
                result = (float3*)m_Output.GetUnsafePtr()
            };
            Measure.Method(() => job.RunBatch(m_Input.Length))
                .WarmupCount(1)
                .DynamicMeasurementCount()
                .Run();
        }

        [Test, Performance]
        public void ConvertVector3FloatToFloatJob()
        {
#if !RUN_PERFORMANCE_TESTS
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
#endif
            var input = Utils.GetStridedArray(m_Input);
            var job = new GLTFast.Jobs.ConvertVector3FloatToFloatJob
            {
                input = input,
                result = m_Output
            };
            Measure.Method(() => job.Run(m_Input.Length))
                .WarmupCount(1)
                .DynamicMeasurementCount()
                .Run();
        }

        [Test, Performance]
        public unsafe void ConvertVector3Int16ToFloatInterleavedNormalizedJob()
        {
#if !RUN_PERFORMANCE_TESTS
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
#endif
            var input = Utils.GetStridedArray<short, short3>(m_InputInt16);
            var job = new GLTFast.Jobs.ConvertVector3Int16ToFloatInterleavedNormalizedJob
            {
                input = input,
                result = (float3*)m_Output.GetUnsafePtr(),
                outputByteStride = 12
            };
            Measure.Method(() => job.RunBatch(m_Output.Length))
                .WarmupCount(1)
                .DynamicMeasurementCount()
                .Run();
        }

        [Test, Performance]
        public unsafe void ConvertVector3Int8ToFloatInterleavedNormalizedJob()
        {
#if !RUN_PERFORMANCE_TESTS
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
#endif
            var input = Utils.GetStridedArray<sbyte, sbyte3>(m_InputInt8);
            var job = new GLTFast.Jobs.ConvertVector3Int8ToFloatInterleavedNormalizedJob
            {
                input = input,
                result = (float3*)m_Output.GetUnsafePtr(),
                outputByteStride = 12
            };
            Measure.Method(() => job.RunBatch(m_Output.Length))
                .WarmupCount(1)
                .DynamicMeasurementCount()
                .Run();
        }

        [Test, Performance]
        public unsafe void ConvertPositionsUInt16ToFloatInterleavedJob()
        {
#if !RUN_PERFORMANCE_TESTS
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
#endif
            var input = Utils.GetStridedArray<ushort, ushort3>(m_InputUInt16);
            var job = new GLTFast.Jobs.ConvertPositionsUInt16ToFloatInterleavedJob
            {
                input = input,
                result = (float3*)m_Output.GetUnsafePtr(),
                outputByteStride = 12
            };
            Measure.Method(() => job.RunBatch(m_Output.Length))
                .WarmupCount(1)
                .DynamicMeasurementCount()
                .Run();
        }

        [Test, Performance]
        public unsafe void ConvertPositionsUInt16ToFloatInterleavedNormalizedJob()
        {
#if !RUN_PERFORMANCE_TESTS
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
#endif
            var input = Utils.GetStridedArray<ushort, ushort3>(m_InputUInt16);
            var job = new GLTFast.Jobs.ConvertPositionsUInt16ToFloatInterleavedNormalizedJob
            {
                input = input,
                result = (float3*)m_Output.GetUnsafePtr(),
                outputByteStride = 12
            };
            Measure.Method(() => job.RunBatch(m_Output.Length))
                .WarmupCount(1)
                .DynamicMeasurementCount()
                .Run();
        }

        [Test, Performance]
        public unsafe void ConvertPositionsInt16ToFloatInterleavedJob()
        {
#if !RUN_PERFORMANCE_TESTS
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
#endif
            var input = Utils.GetStridedArray<short, short3>(m_InputInt16);
            var job = new GLTFast.Jobs.ConvertPositionsInt16ToFloatInterleavedJob
            {
                input = input,
                result = (float3*)m_Output.GetUnsafePtr(),
                outputByteStride = 12
            };
            Measure.Method(() => job.RunBatch(m_Output.Length))
                .WarmupCount(1)
                .DynamicMeasurementCount()
                .Run();
        }

        [Test, Performance]
        public unsafe void ConvertPositionsInt8ToFloatInterleavedJob()
        {
#if !RUN_PERFORMANCE_TESTS
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
#endif
            var input = Utils.GetStridedArray<sbyte, sbyte3>(m_InputInt8);
            var job = new GLTFast.Jobs.ConvertPositionsInt8ToFloatInterleavedJob
            {
                input = input,
                result = (float3*)m_Output.GetUnsafePtr(),
                outputByteStride = 12
            };
            Measure.Method(() => job.RunBatch(m_Output.Length))
                .WarmupCount(1)
                .DynamicMeasurementCount()
                .Run();
        }

        [Test, Performance]
        public unsafe void ConvertPositionsUInt8ToFloatInterleavedJob()
        {
#if !RUN_PERFORMANCE_TESTS
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
#endif
            var input = Utils.GetStridedArray<byte, byte3>(m_InputUInt8);
            var job = new GLTFast.Jobs.ConvertPositionsUInt8ToFloatInterleavedJob
            {
                input = input,
                result = (float3*)m_Output.GetUnsafePtr(),
                outputByteStride = 12
            };
            Measure.Method(() => job.RunBatch(m_Output.Length))
                .WarmupCount(1)
                .DynamicMeasurementCount()
                .Run();
        }

        [Test, Performance]
        public unsafe void ConvertPositionsUInt8ToFloatInterleavedNormalizedJob()
        {
#if !RUN_PERFORMANCE_TESTS
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
#endif
            var input = Utils.GetStridedArray<byte, byte3>(m_InputUInt8);
            var job = new GLTFast.Jobs.ConvertPositionsUInt8ToFloatInterleavedNormalizedJob
            {
                input = input,
                result = (float3*)m_Output.GetUnsafePtr(),
                outputByteStride = 12
            };
            Measure.Method(() => job.RunBatch(m_Output.Length))
                .WarmupCount(1)
                .DynamicMeasurementCount()
                .Run();
        }

        [Test, Performance]
        public unsafe void ConvertNormalsInt16ToFloatInterleavedNormalizedJob()
        {
#if !RUN_PERFORMANCE_TESTS
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
#endif
            var input = Utils.GetStridedArray<short, short3>(m_InputInt16);
            var job = new GLTFast.Jobs.ConvertNormalsInt16ToFloatInterleavedNormalizedJob
            {
                input = input,
                result = (float3*)m_Output.GetUnsafePtr(),
                outputByteStride = 12
            };
            Measure.Method(() => job.RunBatch(m_Output.Length))
                .WarmupCount(1)
                .DynamicMeasurementCount()
                .Run();
        }

        [Test, Performance]
        public unsafe void ConvertNormalsInt8ToFloatInterleavedNormalizedJob()
        {
#if !RUN_PERFORMANCE_TESTS
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
#endif
            var input = Utils.GetStridedArray<sbyte, sbyte3>(m_InputInt8);
            var job = new GLTFast.Jobs.ConvertNormalsInt8ToFloatInterleavedNormalizedJob
            {
                input = input,
                result = (float3*)m_Output.GetUnsafePtr(),
                outputByteStride = 12
            };
            Measure.Method(() => job.RunBatch(m_Output.Length))
                .WarmupCount(1)
                .DynamicMeasurementCount()
                .Run();
        }
    }

    [TestFixture]
    public class PositionSparseJobs
    {
        const int k_Length = 10_000;

        NativeArray<int> m_Indices;
        NativeArray<float3> m_Input;
        NativeArray<float3> m_Output;

        [OneTimeSetUp]
        public void SetUpTest()
        {
            m_Indices = new NativeArray<int>(k_Length, Allocator.Persistent);
            m_Input = new NativeArray<float3>(k_Length, Allocator.Persistent);
            m_Output = new NativeArray<float3>(k_Length * 2, Allocator.Persistent);

            for (int i = 0; i < k_Length; i++)
            {
                m_Indices[i] = i * 2;
                m_Input[i] = new float3(i, k_Length - 1, 42);
            }
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            m_Input.Dispose();
            m_Output.Dispose();
            m_Indices.Dispose();
        }

        [Test, Performance]
        public unsafe void ConvertPositionsSparseJob()
        {
#if !RUN_PERFORMANCE_TESTS
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
#endif
            const GltfComponentType indexType = GltfComponentType.UnsignedInt;
            const GltfComponentType valueType = GltfComponentType.Float;
            const bool normalized = false;

            var job = new GLTFast.Jobs.ConvertVector3SparseJob
            {
                indexBuffer = m_Indices.GetUnsafeReadOnlyPtr(),
                indexConverter = GLTFast.Jobs.CachedFunction.GetIndexConverter(indexType),
                inputByteStride = 3 * AccessorBase.GetComponentTypeSize(valueType),
                input = m_Input.GetUnsafeReadOnlyPtr(),
                valueConverter = GLTFast.Jobs.CachedFunction.GetPositionConverter(valueType, normalized),
                outputByteStride = 12,
                result = (float3*)m_Output.GetUnsafePtr(),
            };
            Measure.Method(() => job.Run(m_Indices.Length))
                .WarmupCount(1)
                .DynamicMeasurementCount()
                .Run();
        }
    }

    [TestFixture]
    public class UVJobs
    {
        const int k_UVLength = 100_000;

        NativeArray<float2> m_UVInput;
        NativeArray<ushort> m_InputUInt16;
        NativeArray<short> m_InputInt16;
        NativeArray<byte> m_InputUInt8;
        NativeArray<sbyte> m_InputInt8;
        NativeArray<float2> m_UVOutput;

        [OneTimeSetUp]
        public void SetUpTest()
        {
            m_UVInput = new NativeArray<float2>(k_UVLength, Allocator.Persistent);
            m_InputUInt16 = new NativeArray<ushort>(k_UVLength * 2, Allocator.Persistent);
            m_InputInt16 = new NativeArray<short>(k_UVLength * 2, Allocator.Persistent);
            m_InputUInt8 = new NativeArray<byte>(k_UVLength * 2, Allocator.Persistent);
            m_InputInt8 = new NativeArray<sbyte>(k_UVLength * 2, Allocator.Persistent);
            m_UVOutput = new NativeArray<float2>(k_UVLength, Allocator.Persistent);
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            m_UVInput.Dispose();
            m_InputUInt16.Dispose();
            m_InputInt16.Dispose();
            m_InputUInt8.Dispose();
            m_InputInt8.Dispose();
            m_UVOutput.Dispose();
        }

        // [Test, Performance]
        // public unsafe void ConvertUVsUInt8ToFloatJob() {
        //     Measure.Method(() => {
        //             var job = new GLTFast.Jobs.ConvertUVsUInt8ToFloatJob {
        //                 input = (byte*)m_UVInput.GetUnsafeReadOnlyPtr(),
        //                 result = (Vector2*)m_UVOutput.GetUnsafePtr()
        //             };
        //             job.Run(m_UVOutput.Length);
        //         })
        //         .WarmupCount(1)
        //         .DynamicMeasurementCount()
        //         .Run();
        // }
        //
        // [Test, Performance]
        // public unsafe void ConvertUVsUInt8ToFloatNormalizedJob() {
        //     Measure.Method(() => {
        //             var job = new GLTFast.Jobs.ConvertUVsUInt8ToFloatNormalizedJob {
        //                 input = (byte*)m_UVInput.GetUnsafeReadOnlyPtr(),
        //                 result = (Vector2*)m_UVOutput.GetUnsafePtr()
        //             };
        //             job.Run(m_UVOutput.Length);
        //         })
        //         .WarmupCount(1)
        //         .DynamicMeasurementCount()
        //         .Run();
        // }
        //
        // [Test, Performance]
        // public unsafe void ConvertUVsUInt16ToFloatNormalizedJob() {
        //     Measure.Method(() => {
        //             var job = new GLTFast.Jobs.ConvertUVsUInt16ToFloatNormalizedJob {
        //                 input = (ushort*)m_UVInput.GetUnsafeReadOnlyPtr(),
        //                 result = (Vector2*)m_UVOutput.GetUnsafePtr()
        //             };
        //             job.Run(m_UVOutput.Length);
        //         })
        //         .WarmupCount(1)
        //         .DynamicMeasurementCount()
        //         .Run();
        // }
        //
        // [Test, Performance]
        // public unsafe void ConvertUVsUInt16ToFloatJob() {
        //     Measure.Method(() => {
        //             var job = new GLTFast.Jobs.ConvertUVsUInt16ToFloatJob {
        //                 input = (ushort*)m_UVInput.GetUnsafeReadOnlyPtr(),
        //                 result = (Vector2*)m_UVOutput.GetUnsafePtr()
        //             };
        //             job.Run(m_UVOutput.Length);
        //         })
        //         .WarmupCount(1)
        //         .DynamicMeasurementCount()
        //         .Run();
        // }
        //
        // [Test, Performance]
        // public unsafe void ConvertUVsFloatToFloatJob() {
        //     Measure.Method(() => {
        //             var job = new GLTFast.Jobs.ConvertUVsFloatToFloatJob {
        //                 input = (float*)m_UVInput.GetUnsafeReadOnlyPtr(),
        //                 result = (Vector2*)m_UVOutput.GetUnsafePtr()
        //             };
        //             job.Run(m_UVOutput.Length);
        //         })
        //         .WarmupCount(1)
        //         .DynamicMeasurementCount()
        //         .Run();
        // }

        [Test, Performance]
        public unsafe void ConvertUVsUInt8ToFloatInterleavedJob()
        {
#if !RUN_PERFORMANCE_TESTS
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
#endif
            var job = new GLTFast.Jobs.ConvertUVsUInt8ToFloatInterleavedJob
            {
                input = (byte*)m_InputUInt8.GetUnsafeReadOnlyPtr(),
                inputByteStride = 2,
                result = (float2*)m_UVOutput.GetUnsafePtr(),
                outputByteStride = 8
            };
            Measure.Method(() => job.RunBatch(m_UVOutput.Length))
                .WarmupCount(1)
                .DynamicMeasurementCount()
                .Run();
        }

        [Test, Performance]
        public unsafe void ConvertUVsUInt8ToFloatInterleavedNormalizedJob()
        {
#if !RUN_PERFORMANCE_TESTS
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
#endif
            var job = new GLTFast.Jobs.ConvertUVsUInt8ToFloatInterleavedNormalizedJob
            {
                input = (byte*)m_InputUInt8.GetUnsafeReadOnlyPtr(),
                inputByteStride = 2,
                result = (float2*)m_UVOutput.GetUnsafePtr(),
                outputByteStride = 8
            };
            Measure.Method(() => job.Run(m_UVOutput.Length))
                .WarmupCount(1)
                .DynamicMeasurementCount()
                .Run();
        }

        [Test, Performance]
        public unsafe void ConvertUVsUInt16ToFloatInterleavedJob()
        {
#if !RUN_PERFORMANCE_TESTS
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
#endif
            var job = new GLTFast.Jobs.ConvertUVsUInt16ToFloatInterleavedJob
            {
                input = (byte*)m_InputUInt16.GetUnsafeReadOnlyPtr(),
                inputByteStride = 4,
                result = (float2*)m_UVOutput.GetUnsafePtr(),
                outputByteStride = 8
            };
            Measure.Method(() => job.RunBatch(m_UVOutput.Length))
                .WarmupCount(1)
                .DynamicMeasurementCount()
                .Run();
        }

        [Test, Performance]
        public unsafe void ConvertUVsUInt16ToFloatInterleavedNormalizedJob()
        {
#if !RUN_PERFORMANCE_TESTS
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
#endif
            var job = new GLTFast.Jobs.ConvertUVsUInt16ToFloatInterleavedNormalizedJob
            {
                input = (byte*)m_InputUInt16.GetUnsafeReadOnlyPtr(),
                inputByteStride = 4,
                result = (float2*)m_UVOutput.GetUnsafePtr(),
                outputByteStride = 8
            };
            Measure.Method(() => job.Run(m_UVOutput.Length))
                .WarmupCount(1)
                .DynamicMeasurementCount()
                .Run();
        }

        [Test, Performance]
        public unsafe void ConvertUVsInt16ToFloatInterleavedJob()
        {
#if !RUN_PERFORMANCE_TESTS
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
#endif
            var job = new GLTFast.Jobs.ConvertUVsInt16ToFloatInterleavedJob
            {
                input = (short*)m_InputInt16.GetUnsafeReadOnlyPtr(),
                inputByteStride = 4,
                result = (float2*)m_UVOutput.GetUnsafePtr(),
                outputByteStride = 8
            };
            Measure.Method(() => job.RunBatch(m_UVOutput.Length))
                .WarmupCount(1)
                .DynamicMeasurementCount()
                .Run();
        }

        [Test, Performance]
        public unsafe void ConvertUVsInt16ToFloatInterleavedNormalizedJob()
        {
#if !RUN_PERFORMANCE_TESTS
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
#endif
            var job = new GLTFast.Jobs.ConvertUVsInt16ToFloatInterleavedNormalizedJob
            {
                input = (short*)m_InputInt16.GetUnsafeReadOnlyPtr(),
                inputByteStride = 4,
                result = (float2*)m_UVOutput.GetUnsafePtr(),
                outputByteStride = 8
            };
            Measure.Method(() => job.RunBatch(m_UVOutput.Length))
                .WarmupCount(1)
                .DynamicMeasurementCount()
                .Run();
        }

        [Test, Performance]
        public unsafe void ConvertUVsInt8ToFloatInterleavedJob()
        {
#if !RUN_PERFORMANCE_TESTS
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
#endif
            var job = new GLTFast.Jobs.ConvertUVsInt8ToFloatInterleavedJob
            {
                input = (sbyte*)m_InputInt8.GetUnsafeReadOnlyPtr(),
                inputByteStride = 2,
                result = (float2*)m_UVOutput.GetUnsafePtr(),
                outputByteStride = 8
            };
            Measure.Method(() => job.RunBatch(m_UVOutput.Length))
                .WarmupCount(1)
                .DynamicMeasurementCount()
                .Run();
        }

        [Test, Performance]
        public unsafe void ConvertUVsInt8ToFloatInterleavedNormalizedJob()
        {
#if !RUN_PERFORMANCE_TESTS
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
#endif
            var job = new GLTFast.Jobs.ConvertUVsInt8ToFloatInterleavedNormalizedJob
            {
                input = (sbyte*)m_InputInt8.GetUnsafeReadOnlyPtr(),
                inputByteStride = 2,
                result = (float2*)m_UVOutput.GetUnsafePtr(),
                outputByteStride = 8
            };
            Measure.Method(() => job.RunBatch(m_UVOutput.Length))
                .WarmupCount(1)
                .DynamicMeasurementCount()
                .Run();
        }

        [Test, Performance]
        public unsafe void ConvertUVsFloatToFloatInterleavedJob()
        {
#if !RUN_PERFORMANCE_TESTS
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
#endif
            var job = new GLTFast.Jobs.ConvertUVsFloatToFloatInterleavedJob
            {
                input = (byte*)m_UVInput.GetUnsafeReadOnlyPtr(),
                inputByteStride = 8,
                result = (float2*)m_UVOutput.GetUnsafePtr(),
                outputByteStride = 8
            };
            Measure.Method(() => job.RunBatch(m_UVOutput.Length))
                .WarmupCount(1)
                .DynamicMeasurementCount()
                .Run();
        }
    }

    [TestFixture]
    public class Vector4Jobs
    {
        const int k_RotationLength = 100_000;

        NativeArray<float4> m_RotInput;
        NativeArray<ushort> m_InputUInt16;
        NativeArray<short> m_InputInt16;
        NativeArray<byte> m_InputUInt8;
        NativeArray<sbyte> m_InputInt8;
        NativeArray<quaternion> m_RotOutput;

        [OneTimeSetUp]
        public void SetUpTest()
        {
            m_RotInput = new NativeArray<float4>(k_RotationLength, Allocator.Persistent);
            m_InputUInt16 = new NativeArray<ushort>(k_RotationLength * 4, Allocator.Persistent);
            m_InputInt16 = new NativeArray<short>(k_RotationLength * 4, Allocator.Persistent);
            m_InputUInt8 = new NativeArray<byte>(k_RotationLength * 4, Allocator.Persistent);
            m_InputInt8 = new NativeArray<sbyte>(k_RotationLength * 4, Allocator.Persistent);
            m_RotOutput = new NativeArray<quaternion>(k_RotationLength, Allocator.Persistent);
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            m_RotInput.Dispose();
            m_InputUInt16.Dispose();
            m_InputInt16.Dispose();
            m_InputUInt8.Dispose();
            m_InputInt8.Dispose();
            m_RotOutput.Dispose();
        }

        [Test, Performance]
        public void ConvertRotationsFloatToFloatJob()
        {
#if !RUN_PERFORMANCE_TESTS
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
#endif
            var job = new GLTFast.Jobs.ConvertRotationsFloatToFloatJob
            {
                input = m_RotInput.AsReadOnly(),
                result = m_RotOutput
            };
            Measure.Method(() => job.Run(m_RotOutput.Length))
                .WarmupCount(1)
                .DynamicMeasurementCount()
                .Run();
        }

        [Test, Performance]
        public void ConvertRotationsInt16ToFloatJob()
        {
#if !RUN_PERFORMANCE_TESTS
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
#endif
            var job = new GLTFast.Jobs.ConvertRotationsInt16ToFloatJob
            {
                input = m_InputInt16.Reinterpret<short4>(UnsafeUtility.SizeOf<short>()).AsReadOnly(),
                result = m_RotOutput
            };
            Measure.Method(() => job.Run(m_RotOutput.Length))
                .WarmupCount(1)
                .DynamicMeasurementCount()
                .Run();
        }

        [Test, Performance]
        public void ConvertRotationsInt8ToFloatJob()
        {
#if !RUN_PERFORMANCE_TESTS
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
#endif
            m_InputInt8[0] = sbyte.MinValue;
            m_InputInt8[1] = -64;
            m_InputInt8[2] = 64;
            m_InputInt8[3] = sbyte.MaxValue;

            var job = new GLTFast.Jobs.ConvertRotationsInt8ToFloatJob
            {
                input = m_InputInt8.Reinterpret<sbyte4>(UnsafeUtility.SizeOf<sbyte>()).AsReadOnly(),
                result = m_RotOutput
            };
            Measure.Method(() => job.Run(m_RotOutput.Length))
                .WarmupCount(1)
                .DynamicMeasurementCount()
                .Run();
        }

        [Test, Performance]
        public unsafe void ConvertTangentsFloatToFloatInterleavedJob()
        {
#if !RUN_PERFORMANCE_TESTS
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
#endif
            var job = new GLTFast.Jobs.ConvertTangentsFloatToFloatInterleavedJob
            {
                input = (byte*)m_RotInput.GetUnsafeReadOnlyPtr(),
                inputByteStride = 16,
                result = (float4*)m_RotOutput.GetUnsafePtr(),
                outputByteStride = 16
            };
            Measure.Method(() => job.RunBatch(m_RotOutput.Length))
                .WarmupCount(1)
                .DynamicMeasurementCount()
                .Run();
        }

        [Test, Performance]
        public unsafe void ConvertBoneWeightsFloatToFloatInterleavedJob()
        {
#if !RUN_PERFORMANCE_TESTS
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
#endif
            var job = new GLTFast.Jobs.ConvertBoneWeightsFloatToFloatInterleavedJob
            {
                input = (byte*)m_RotInput.GetUnsafeReadOnlyPtr(),
                inputByteStride = 16,
                result = (float4*)m_RotOutput.GetUnsafePtr(),
                outputByteStride = 16
            };
            Measure.Method(() => job.RunBatch(m_RotOutput.Length))
                .WarmupCount(1)
                .DynamicMeasurementCount()
                .Run();
        }

        // TODO: Test ConvertBoneWeightsUInt8ToFloatInterleavedJob
        // TODO: Test ConvertBoneWeightsUInt16ToFloatInterleavedJob

        [Test, Performance]
        public unsafe void ConvertTangentsInt16ToFloatInterleavedNormalizedJob()
        {
#if !RUN_PERFORMANCE_TESTS
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
#endif
            var job = new GLTFast.Jobs.ConvertTangentsInt16ToFloatInterleavedNormalizedJob
            {
                input = (short*)m_InputInt16.GetUnsafeReadOnlyPtr(),
                inputByteStride = 8,
                result = (float4*)m_RotOutput.GetUnsafePtr(),
                outputByteStride = 16
            };
            Measure.Method(() => job.RunBatch(m_RotOutput.Length))
                .WarmupCount(1)
                .DynamicMeasurementCount()
                .Run();
        }

        [Test, Performance]
        public unsafe void ConvertTangentsInt8ToFloatInterleavedNormalizedJob()
        {
#if !RUN_PERFORMANCE_TESTS
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
#endif
            var job = new GLTFast.Jobs.ConvertTangentsInt8ToFloatInterleavedNormalizedJob
            {
                input = (sbyte*)m_InputInt8.GetUnsafeReadOnlyPtr(),
                inputByteStride = 4,
                result = (float4*)m_RotOutput.GetUnsafePtr(),
                outputByteStride = 16
            };
            Measure.Method(() => job.RunBatch(m_RotOutput.Length))
                .WarmupCount(1)
                .DynamicMeasurementCount()
                .Run();
        }
    }

    [TestFixture]
    public class ColorJobs
    {
        const int k_ColorLength = 300_000;
        Color m_ReferenceRGB = new Color(.13f, .42f, .95f, 1f);
        Color m_ReferenceRGBA = new Color(.42f, .95f, .5f, .24f);

        NativeArray<float> m_ColorInput;
        NativeArray<ushort> m_InputUInt16;
        NativeArray<byte> m_InputUInt8;
        NativeArray<float4> m_ColorOutput;

        [OneTimeSetUp]
        public void SetUpTest()
        {
            m_ColorInput = new NativeArray<float>(k_ColorLength * 4, Allocator.Persistent);
            m_InputUInt16 = new NativeArray<ushort>(k_ColorLength * 4, Allocator.Persistent);
            m_InputUInt8 = new NativeArray<byte>(k_ColorLength * 4, Allocator.Persistent);
            m_ColorOutput = new NativeArray<float4>(k_ColorLength, Allocator.Persistent);

            m_ColorInput[3] = m_ReferenceRGB.r;
            m_ColorInput[4] = m_ReferenceRGB.g;
            m_ColorInput[5] = m_ReferenceRGB.b;
            m_ColorInput[6] = m_ReferenceRGBA.b;
            m_ColorInput[7] = m_ReferenceRGBA.a;

            m_InputUInt8[3] = (byte)(byte.MaxValue * m_ReferenceRGB.r);
            m_InputUInt8[4] = (byte)(byte.MaxValue * m_ReferenceRGB.g);
            m_InputUInt8[5] = (byte)(byte.MaxValue * m_ReferenceRGB.b);
            m_InputUInt8[6] = (byte)(byte.MaxValue * m_ReferenceRGBA.b);
            m_InputUInt8[7] = (byte)(byte.MaxValue * m_ReferenceRGBA.a);

            m_InputUInt16[3] = (ushort)(ushort.MaxValue * m_ReferenceRGB.r);
            m_InputUInt16[4] = (ushort)(ushort.MaxValue * m_ReferenceRGB.g);
            m_InputUInt16[5] = (ushort)(ushort.MaxValue * m_ReferenceRGB.b);
            m_InputUInt16[6] = (ushort)(ushort.MaxValue * m_ReferenceRGBA.b);
            m_InputUInt16[7] = (ushort)(ushort.MaxValue * m_ReferenceRGBA.a);
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            m_ColorInput.Dispose();
            m_InputUInt8.Dispose();
            m_InputUInt16.Dispose();
            m_ColorOutput.Dispose();
        }

        [Test, Performance]
        public unsafe void ConvertColorsRGBFloatToRGBAFloatJob()
        {
#if !RUN_PERFORMANCE_TESTS
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
#endif
            var job = new GLTFast.Jobs.ConvertColorsRGBFloatToRGBAFloatJob
            {
                input = (byte*)m_ColorInput.GetUnsafeReadOnlyPtr(),
                inputByteStride = 12,
                result = m_ColorOutput
            };
            Measure.Method(() => job.Run(m_ColorOutput.Length))
                .WarmupCount(1)
                .DynamicMeasurementCount()
                .Run();
        }

        [Test, Performance]
        public unsafe void ConvertColorsRGBUInt8ToRGBAFloatJob()
        {
#if !RUN_PERFORMANCE_TESTS
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
#endif
            var job = new GLTFast.Jobs.ConvertColorsRgbUInt8ToRGBAFloatJob
            {
                input = (byte*)m_InputUInt8.GetUnsafeReadOnlyPtr(),
                inputByteStride = 3,
                result = m_ColorOutput
            };
            Measure.Method(() => job.Run(m_ColorOutput.Length))
                .WarmupCount(1)
                .DynamicMeasurementCount()
                .Run();
        }

        [Test, Performance]
        public unsafe void ConvertColorsRGBUInt16ToRGBAFloatJob()
        {
#if !RUN_PERFORMANCE_TESTS
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
#endif
            var job = new GLTFast.Jobs.ConvertColorsRgbUInt16ToRGBAFloatJob
            {
                input = (ushort*)m_InputUInt16.GetUnsafeReadOnlyPtr(),
                inputByteStride = 6,
                result = m_ColorOutput
            };
            Measure.Method(() => job.Run(m_ColorOutput.Length))
                .WarmupCount(1)
                .DynamicMeasurementCount()
                .Run();
        }

        [Test, Performance]
        public unsafe void ConvertColorsRGBAUInt16ToRGBAFloatJob()
        {
#if !RUN_PERFORMANCE_TESTS
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
#endif
            var job = new GLTFast.Jobs.ConvertColorsRgbaUInt16ToRGBAFloatJob
            {
                input = (ushort*)m_InputUInt16.GetUnsafeReadOnlyPtr(),
                inputByteStride = 8,
                result = m_ColorOutput
            };
            Measure.Method(() => job.RunBatch(m_ColorOutput.Length))
                .WarmupCount(1)
                .DynamicMeasurementCount()
                .Run();
        }

        [Test, Performance]
        public unsafe void ConvertColorsRGBAFloatToRGBAFloatJob()
        {
#if !RUN_PERFORMANCE_TESTS
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
#endif
            var job = new GLTFast.Jobs.ConvertColorsRGBAFloatToRGBAFloatJob
            {
                input = (byte*)m_ColorInput.GetUnsafeReadOnlyPtr(),
                inputByteStride = 16,
                result = m_ColorOutput
            };
            Measure.Method(() => job.RunBatch(m_ColorOutput.Length))
                .WarmupCount(1)
                .DynamicMeasurementCount()
                .Run();
        }

        [Test, Performance]
        public unsafe void ConvertColorsRGBAUInt8ToRGBAFloatJob()
        {
#if !RUN_PERFORMANCE_TESTS
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
#endif
            var job = new GLTFast.Jobs.ConvertColorsRgbaUInt8ToRGBAFloatJob
            {
                input = (byte*)m_InputUInt8.GetUnsafeReadOnlyPtr(),
                inputByteStride = 4,
                result = m_ColorOutput
            };
            Measure.Method(() => job.Run(m_ColorOutput.Length))
                .WarmupCount(1)
                .DynamicMeasurementCount()
                .Run();
        }
    }

    [TestFixture]
    public class BoneIndexJobs
    {
        const int k_BoneIndexLength = 500_000;
        uint4 m_Reference = new uint4(2, 3, 4, 5);
        NativeArray<byte> m_InputUInt8;
        NativeArray<ushort> m_InputUInt16;
        NativeArray<uint4> m_BoneIndexOutput;

        [OneTimeSetUp]
        public void SetUpTest()
        {
            m_InputUInt8 = new NativeArray<byte>(k_BoneIndexLength * 4, Allocator.Persistent);
            m_InputUInt16 = new NativeArray<ushort>(k_BoneIndexLength * 4, Allocator.Persistent);
            m_BoneIndexOutput = new NativeArray<uint4>(k_BoneIndexLength, Allocator.Persistent);

            m_InputUInt8[4] = (byte)m_Reference.x;
            m_InputUInt8[5] = (byte)m_Reference.y;
            m_InputUInt8[6] = (byte)m_Reference.z;
            m_InputUInt8[7] = (byte)m_Reference.w;

            m_InputUInt16[4] = (ushort)m_Reference.x;
            m_InputUInt16[5] = (ushort)m_Reference.y;
            m_InputUInt16[6] = (ushort)m_Reference.z;
            m_InputUInt16[7] = (ushort)m_Reference.w;
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            m_InputUInt16.Dispose();
            m_InputUInt8.Dispose();
            m_BoneIndexOutput.Dispose();
        }

        [Test, Performance]
        public unsafe void ConvertBoneJointsUInt8ToUInt32Job()
        {
#if !RUN_PERFORMANCE_TESTS
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
#endif
            var job = new GLTFast.Jobs.ConvertBoneJointsUInt8ToUInt32Job
            {
                input = (byte*)m_InputUInt8.GetUnsafeReadOnlyPtr(),
                inputByteStride = 4,
                result = (uint4*)m_BoneIndexOutput.GetUnsafePtr(),
                outputByteStride = 16
            };
            Measure.Method(() => job.Run(m_BoneIndexOutput.Length))
                .WarmupCount(1)
                .DynamicMeasurementCount()
                .Run();
        }

        [Test, Performance]
        public unsafe void ConvertBoneJointsUInt16ToUInt32Job()
        {
#if !RUN_PERFORMANCE_TESTS
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
#endif
            var job = new GLTFast.Jobs.ConvertBoneJointsUInt16ToUInt32Job
            {
                input = (byte*)m_InputUInt16.GetUnsafeReadOnlyPtr(),
                inputByteStride = 8,
                result = (uint4*)m_BoneIndexOutput.GetUnsafePtr(),
                outputByteStride = 16
            };
            Measure.Method(() => job.Run(m_BoneIndexOutput.Length))
                .WarmupCount(1)
                .DynamicMeasurementCount()
                .Run();
        }
    }

    [TestFixture]
    public class MatrixJobs
    {
        const int k_MatrixLength = 80_000;
        static readonly float4x4 k_Reference = new Matrix4x4(
            new float4(1, -5, -9, 13),
            new float4(-2, 6, 10, 14),
            new float4(-3, 7, 11, 15),
            new float4(-4, 8, 12, 16)
        );
        NativeArray<float4x4> m_MatrixInput;
        NativeArray<float4x4> m_MatrixOutput;

        [OneTimeSetUp]
        public void SetUpTest()
        {
            m_MatrixInput = new NativeArray<float4x4>(k_MatrixLength, Allocator.Persistent);
            m_MatrixOutput = new NativeArray<float4x4>(k_MatrixLength, Allocator.Persistent);

            m_MatrixInput[1] = new float4x4(
                1, 2, 3, 4,
                5, 6, 7, 8,
                9, 10, 11, 12,
                13, 14, 15, 16
                );
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            m_MatrixInput.Dispose();
            m_MatrixOutput.Dispose();
        }

        [Test, Performance]
        public void ConvertMatricesJob()
        {
#if !RUN_PERFORMANCE_TESTS
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
#endif
            var job = new GLTFast.Jobs.ConvertMatricesJob
            {
                input = m_MatrixInput.AsReadOnly(),
                result = m_MatrixOutput
            };
            Measure.Method(() => job.Run(m_MatrixOutput.Length))
                .WarmupCount(1)
                .DynamicMeasurementCount()
                .Run();

            Assert.AreEqual(k_Reference, m_MatrixOutput[1]);
        }
    }

    [TestFixture]
    public class IndexJobs
    {
        const int k_IndexLength = 1_200_000; // has to be a multiple of 3!
        NativeArray<byte> m_InputUInt8;
        NativeArray<ushort> m_InputUInt16;
        NativeArray<uint> m_InputUInt32;
        NativeArray<ushort> m_IndexOutputUInt16;
        NativeArray<uint> m_IndexOutput;

        [OneTimeSetUp]
        public void SetUpTest()
        {
            m_InputUInt8 = new NativeArray<byte>(k_IndexLength, Allocator.Persistent);
            m_InputUInt16 = new NativeArray<ushort>(k_IndexLength, Allocator.Persistent);
            m_InputUInt32 = new NativeArray<uint>(k_IndexLength, Allocator.Persistent);
            m_IndexOutputUInt16 = new NativeArray<ushort>(k_IndexLength, Allocator.Persistent);
            m_IndexOutput = new NativeArray<uint>(k_IndexLength, Allocator.Persistent);

            for (int i = 0; i < 6; i++)
            {
                m_InputUInt8[i] = (byte)i;
                m_InputUInt16[i] = (ushort)i;
                m_InputUInt32[i] = (uint)i;
            }
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            m_InputUInt8.Dispose();
            m_InputUInt16.Dispose();
            m_InputUInt32.Dispose();
            m_IndexOutputUInt16.Dispose();
            m_IndexOutput.Dispose();
        }

        [Test, Performance]
        public void CreateIndicesUInt16Job()
        {
#if !RUN_PERFORMANCE_TESTS
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
#endif
            Assert.IsTrue(m_IndexOutputUInt16.Length % 3 == 0);
            var job = new GLTFast.Jobs.CreateIndicesUInt16Job
            {
                result = m_IndexOutputUInt16
            };
            Measure.Method(() => job.Run(m_IndexOutputUInt16.Length))
                .WarmupCount(1)
                .DynamicMeasurementCount()
                .Run();
        }

        [Test, Performance]
        public void CreateIndicesUInt32Job()
        {
#if !RUN_PERFORMANCE_TESTS
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
#endif
            Assert.IsTrue(m_IndexOutput.Length % 3 == 0);
            var job = new GLTFast.Jobs.CreateIndicesUInt32Job
            {
                result = m_IndexOutput
            };
            Measure.Method(() => job.Run(m_IndexOutput.Length))
                .WarmupCount(1)
                .DynamicMeasurementCount()
                .Run();
        }


        [Test, Performance]
        public void CreateIndicesUInt16FlippedJob()
        {
#if !RUN_PERFORMANCE_TESTS
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
#endif
            Assert.IsTrue(m_IndexOutputUInt16.Length % 3 == 0);
            var job = new GLTFast.Jobs.CreateIndicesUInt16FlippedJob
            {
                result = m_IndexOutputUInt16
            };
            Measure.Method(() => job.Run(m_IndexOutputUInt16.Length))
                .WarmupCount(1)
                .DynamicMeasurementCount()
                .Run();
        }

        [Test, Performance]
        public void CreateIndicesUInt32FlippedJob()
        {
#if !RUN_PERFORMANCE_TESTS
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
#endif
            Assert.IsTrue(m_IndexOutput.Length % 3 == 0);
            var job = new GLTFast.Jobs.CreateIndicesUInt32FlippedJob
            {
                result = m_IndexOutput
            };
            Measure.Method(() => job.Run(m_IndexOutput.Length))
                .WarmupCount(1)
                .DynamicMeasurementCount()
                .Run();

            Assert.AreEqual(2, m_IndexOutput[0]);
            Assert.AreEqual(1, m_IndexOutput[1]);
            Assert.AreEqual(0, m_IndexOutput[2]);
            Assert.AreEqual(5, m_IndexOutput[3]);
            Assert.AreEqual(4, m_IndexOutput[4]);
            Assert.AreEqual(3, m_IndexOutput[5]);
        }

        [Test, Performance]
        public void CreateIndicesForTriangleStripUInt16Job()
        {
#if !RUN_PERFORMANCE_TESTS
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
#endif
            Assert.IsTrue(m_IndexOutputUInt16.Length > 3);
            var job = new GLTFast.Jobs.CreateIndicesForTriangleStripUInt16Job
            {
                result = m_IndexOutputUInt16
            };
            Measure.Method(() => job.Run(m_IndexOutputUInt16.Length))
                .WarmupCount(1)
                .DynamicMeasurementCount()
                .Run();
        }

        [Test, Performance]
        public void CreateIndicesForTriangleStripUInt32Job()
        {
#if !RUN_PERFORMANCE_TESTS
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
#endif
            Assert.IsTrue(m_IndexOutput.Length > 3);
            var job = new GLTFast.Jobs.CreateIndicesForTriangleStripUInt32Job
            {
                result = m_IndexOutput
            };
            Measure.Method(() => job.Run(m_IndexOutput.Length))
                .WarmupCount(1)
                .DynamicMeasurementCount()
                .Run();
        }

        [Test, Performance]
        public void CreateIndicesForTriangleFanUInt16Job()
        {
#if !RUN_PERFORMANCE_TESTS
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
#endif
            Assert.IsTrue(m_IndexOutputUInt16.Length > 3);
            var job = new GLTFast.Jobs.CreateIndicesForTriangleFanUInt16Job
            {
                result = m_IndexOutputUInt16
            };
            Measure.Method(() => job.Run(m_IndexOutputUInt16.Length))
                .WarmupCount(1)
                .DynamicMeasurementCount()
                .Run();
        }

        [Test, Performance]
        public void CreateIndicesForTriangleFanUInt32Job()
        {
#if !RUN_PERFORMANCE_TESTS
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
#endif
            Assert.IsTrue(m_IndexOutput.Length > 3);
            var job = new GLTFast.Jobs.CreateIndicesForTriangleFanUInt32Job
            {
                result = m_IndexOutput
            };
            Measure.Method(() => job.Run(m_IndexOutput.Length))
                .WarmupCount(1)
                .DynamicMeasurementCount()
                .Run();
            Assert.AreEqual(2, m_IndexOutput[0]);
            Assert.AreEqual(1, m_IndexOutput[1]);
            Assert.AreEqual(0, m_IndexOutput[2]);
            Assert.AreEqual(3, m_IndexOutput[3]);
            Assert.AreEqual(2, m_IndexOutput[4]);
            Assert.AreEqual(0, m_IndexOutput[5]);
            Assert.AreEqual(4, m_IndexOutput[6]);
            Assert.AreEqual(3, m_IndexOutput[7]);
            Assert.AreEqual(0, m_IndexOutput[8]);
        }

        [Test, Performance]
        public void ConvertIndicesUInt8ToUInt16Job()
        {
#if !RUN_PERFORMANCE_TESTS
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
#endif
            Assert.IsTrue(m_IndexOutputUInt16.Length % 3 == 0);
            var job = new GLTFast.Jobs.ConvertIndicesUInt8ToUInt16Job
            {
                input = m_InputUInt8.AsReadOnly(),
                result = m_IndexOutputUInt16
            };
            Measure.Method(() => job.Run(m_IndexOutputUInt16.Length))
                .WarmupCount(1)
                .DynamicMeasurementCount()
                .Run();
        }

        [Test, Performance]
        public void ConvertIndicesUInt8ToUInt32Job()
        {
#if !RUN_PERFORMANCE_TESTS
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
#endif
            Assert.IsTrue(m_IndexOutput.Length % 3 == 0);
            var job = new GLTFast.Jobs.ConvertIndicesUInt8ToUInt32Job
            {
                input = m_InputUInt8.AsReadOnly(),
                result = m_IndexOutput
            };
            Measure.Method(() => job.Run(m_IndexOutput.Length))
                .WarmupCount(1)
                .DynamicMeasurementCount()
                .Run();
        }

        [Test, Performance]
        public void ConvertIndicesUInt8ToUInt16FlippedJob()
        {
#if !RUN_PERFORMANCE_TESTS
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
#endif
            Assert.IsTrue(m_IndexOutputUInt16.Length % 3 == 0);
            var job = new GLTFast.Jobs.ConvertIndicesUInt8ToUInt16FlippedJob
            {
                input = m_InputUInt8.Reinterpret<byte3>(UnsafeUtility.SizeOf<byte>()).AsReadOnly(),
                result = m_IndexOutputUInt16.Reinterpret<ushort3>(sizeof(ushort))
            };
            Measure.Method(() => job.Run(m_IndexOutputUInt16.Length / 3))
                .WarmupCount(1)
                .DynamicMeasurementCount()
                .Run();
        }

        [Test, Performance]
        public void ConvertIndicesUInt8ToUInt32FlippedJob()
        {
#if !RUN_PERFORMANCE_TESTS
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
#endif
            Assert.IsTrue(m_IndexOutput.Length % 3 == 0);
            var job = new GLTFast.Jobs.ConvertIndicesUInt8ToUInt32FlippedJob
            {
                input = m_InputUInt8.Reinterpret<byte3>(UnsafeUtility.SizeOf<byte>()).AsReadOnly(),
                result = m_IndexOutput.Reinterpret<uint3>(sizeof(uint))
            };
            Measure.Method(() => job.Run(m_IndexOutput.Length / 3))
                .WarmupCount(1)
                .DynamicMeasurementCount()
                .Run();
        }

        [Test, Performance]
        public void ConvertIndicesUInt16ToUInt16FlippedJob()
        {
#if !RUN_PERFORMANCE_TESTS
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
#endif
            Assert.IsTrue(m_IndexOutputUInt16.Length % 3 == 0);
            var job = new GLTFast.Jobs.ConvertIndicesUInt16ToUInt16FlippedJob
            {
                input = m_InputUInt16.Reinterpret<ushort3>(UnsafeUtility.SizeOf<ushort>()).AsReadOnly(),
                result = m_IndexOutputUInt16.Reinterpret<ushort3>(sizeof(ushort))
            };
            Measure.Method(() => job.Run(m_IndexOutputUInt16.Length / 3))
                .WarmupCount(1)
                .DynamicMeasurementCount()
                .Run();
        }

        [Test, Performance]
        public void ConvertIndicesUInt16ToUInt32FlippedJob()
        {
#if !RUN_PERFORMANCE_TESTS
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
#endif
            Assert.IsTrue(m_IndexOutput.Length % 3 == 0);
            var job = new GLTFast.Jobs.ConvertIndicesUInt16ToUInt32FlippedJob
            {
                input = m_InputUInt16.Reinterpret<ushort3>(UnsafeUtility.SizeOf<ushort>()).AsReadOnly(),
                result = m_IndexOutput.Reinterpret<uint3>(sizeof(uint))
            };
            Measure.Method(() => job.Run(m_IndexOutput.Length / 3))
                .WarmupCount(1)
                .DynamicMeasurementCount()
                .Run();
        }

        [Test, Performance]
        public void ConvertIndicesUInt16ToUInt32Job()
        {
#if !RUN_PERFORMANCE_TESTS
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
#endif
            Assert.IsTrue(m_IndexOutput.Length % 3 == 0);
            var job = new GLTFast.Jobs.ConvertIndicesUInt16ToUInt32Job
            {
                input = m_InputUInt16.AsReadOnly(),
                result = m_IndexOutput
            };
            Measure.Method(() => job.Run(m_IndexOutput.Length))
                .WarmupCount(1)
                .DynamicMeasurementCount()
                .Run();
        }

        [Test, Performance]
        public void ConvertIndicesUInt32ToUInt32FlippedJob()
        {
#if !RUN_PERFORMANCE_TESTS
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
#endif
            Assert.IsTrue(m_IndexOutput.Length % 3 == 0);
            var job = new GLTFast.Jobs.ConvertIndicesUInt32ToUInt32FlippedJob
            {
                input = m_InputUInt32.Reinterpret<uint3>(UnsafeUtility.SizeOf<uint>()).AsReadOnly(),
                result = m_IndexOutput.Reinterpret<uint3>(sizeof(uint))
            };
            Measure.Method(() => job.Run(m_IndexOutput.Length / 3))
                .WarmupCount(1)
                .DynamicMeasurementCount()
                .Run();
        }

        [Test, Performance]
        public void RecalculateIndicesForTriangleFanInPlaceJob()
        {
#if !RUN_PERFORMANCE_TESTS
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
#endif
            Assert.IsTrue(m_IndexOutput.Length > 3);
            var job = new GLTFast.Jobs.RecalculateIndicesForTriangleFanInPlaceJob<uint>
            {
                indices = m_IndexOutput
            };
            Measure.Method(() => job.Run())
                .WarmupCount(1)
                .DynamicMeasurementCount()
                .Run();
        }

        [Test, Performance]
        public void RecalculateIndicesForTriangleStripInPlaceJob()
        {
#if !RUN_PERFORMANCE_TESTS
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
#endif
            Assert.IsTrue(m_IndexOutput.Length > 3);
            var job = new GLTFast.Jobs.RecalculateIndicesForTriangleStripInPlaceJob<uint>
            {
                indices = m_IndexOutput
            };
            Measure.Method(() => job.Run())
                .WarmupCount(1)
                .DynamicMeasurementCount()
                .Run();
        }
    }

    [TestFixture]
    public class ScalarJobs
    {
        const int k_ScalarLength = 500_000;
        NativeArray<sbyte> m_InputInt8;
        NativeArray<byte> m_InputUInt8;
        NativeArray<short> m_InputInt16;
        NativeArray<ushort> m_InputUInt16;
        NativeArray<float> m_ScalarOutput;

        [OneTimeSetUp]
        public void SetUpTest()
        {
            m_InputInt8 = new NativeArray<sbyte>(k_ScalarLength, Allocator.Persistent);
            m_InputUInt8 = new NativeArray<byte>(k_ScalarLength, Allocator.Persistent);
            m_InputInt16 = new NativeArray<short>(k_ScalarLength, Allocator.Persistent);
            m_InputUInt16 = new NativeArray<ushort>(k_ScalarLength, Allocator.Persistent);
            m_ScalarOutput = new NativeArray<float>(k_ScalarLength, Allocator.Persistent);

            m_InputInt8[0] = sbyte.MaxValue;
            m_InputUInt8[0] = byte.MaxValue;
            m_InputInt16[0] = short.MaxValue;
            m_InputUInt16[0] = ushort.MaxValue;

            m_InputInt8[1] = 0;
            m_InputUInt8[1] = 0;
            m_InputInt16[1] = 0;
            m_InputUInt16[1] = 0;

            m_InputInt8[2] = sbyte.MinValue;
            m_InputUInt8[2] = byte.MinValue;
            m_InputInt16[2] = short.MinValue;
            m_InputUInt16[2] = ushort.MinValue;

            m_InputInt8[3] = sbyte.MaxValue / 2;
            m_InputUInt8[3] = byte.MaxValue / 2;
            m_InputInt16[3] = short.MaxValue / 2;
            m_InputUInt16[3] = ushort.MaxValue / 2;
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            m_InputInt8.Dispose();
            m_InputUInt8.Dispose();
            m_InputInt16.Dispose();
            m_InputUInt16.Dispose();
            m_ScalarOutput.Dispose();
        }

        [Test, Performance]
        public void ConvertScalarInt8ToFloatNormalizedJob()
        {
#if !RUN_PERFORMANCE_TESTS
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
#endif
            var job = new GLTFast.Jobs.ConvertScalarInt8ToFloatNormalizedJob
            {
                input = m_InputInt8.AsReadOnly(),
                result = m_ScalarOutput,
            };
            Measure.Method(() => job.Run(m_ScalarOutput.Length))
                .WarmupCount(1)
                .DynamicMeasurementCount()
                .Run();
        }

        [Test, Performance]
        public void ConvertScalarUInt8ToFloatNormalizedJob()
        {
#if !RUN_PERFORMANCE_TESTS
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
#endif
            var job = new GLTFast.Jobs.ConvertScalarUInt8ToFloatNormalizedJob
            {
                input = m_InputUInt8.AsReadOnly(),
                result = m_ScalarOutput,
            };
            Measure.Method(() => job.Run(m_ScalarOutput.Length))
                .WarmupCount(1)
                .DynamicMeasurementCount()
                .Run();
        }

        [Test, Performance]
        public void ConvertScalarInt16ToFloatNormalizedJob()
        {
#if !RUN_PERFORMANCE_TESTS
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
#endif
            var job = new GLTFast.Jobs.ConvertScalarInt16ToFloatNormalizedJob
            {
                input = m_InputInt16.AsReadOnly(),
                result = m_ScalarOutput,
            };
            Measure.Method(() => job.Run(m_ScalarOutput.Length))
                .WarmupCount(1)
                .DynamicMeasurementCount()
                .Run();
        }

        [Test, Performance]
        public void ConvertScalarUInt16ToFloatNormalizedJob()
        {
#if !RUN_PERFORMANCE_TESTS
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
#endif
            var job = new GLTFast.Jobs.ConvertScalarUInt16ToFloatNormalizedJob
            {
                input = m_InputUInt16.AsReadOnly(),
                result = m_ScalarOutput,
            };
            Measure.Method(() => job.Run(m_ScalarOutput.Length))
                .WarmupCount(1)
                .DynamicMeasurementCount()
                .Run();
        }
    }
}
