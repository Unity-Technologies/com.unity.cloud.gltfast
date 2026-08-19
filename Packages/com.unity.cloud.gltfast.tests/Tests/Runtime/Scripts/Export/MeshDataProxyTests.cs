// SPDX-FileCopyrightText: 2026 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections;
using NUnit.Framework;
using Unity.Cloud.Gltfast.Export;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Cloud.Gltfast.Tests.Export
{
    [Category("Export")]
    class MeshDataProxyTests
    {
        const int k_Iterations = 20_000;
        const long k_MaxBytes = 256 * 1024;

        Mesh m_Mesh;

        [OneTimeSetUp]
        public void SetUpTest()
        {
            m_Mesh = new Mesh();
            m_Mesh.SetVertices(new[] { Vector3.zero, Vector3.right, Vector3.up });
            m_Mesh.SetTriangles(new[] { 0, 1, 2 }, 0);
        }

        [OneTimeTearDown]
        public void TearDownTest()
        {
            UnityEngine.Object.Destroy(m_Mesh);
        }

        [UnityTest]
        public IEnumerator GetVertexDataCompletesSynchronouslyWithoutAllocating()
        {
            using var meshDataArray = Mesh.AcquireReadOnlyMeshData(m_Mesh);
            var meshData = new MeshDataProxy<ushort>(meshDataArray[0]);
            Assert.IsTrue(meshData.GetVertexDataAsync(0, true).IsCompleted);

            using var recorder = AllocationRecorder.Start();
            recorder.RequireValid();

            var measured = -1L;
            yield return MeasureRepeatedCalls(
                recorder, () => { _ = meshData.GetVertexDataAsync(0, true); }, bytes => measured = bytes);

            AssertWithinBudget(measured);
        }

        [UnityTest]
        public IEnumerator GetIndexDataCompletesSynchronouslyWithoutAllocating()
        {
            using var meshDataArray = Mesh.AcquireReadOnlyMeshData(m_Mesh);
            var meshData = new MeshDataProxy<ushort>(meshDataArray[0]);
            Assert.IsTrue(meshData.GetIndexDataAsync(true).IsCompleted);

            using var recorder = AllocationRecorder.Start();
            recorder.RequireValid();

            var measured = -1L;
            yield return MeasureRepeatedCalls(
                recorder, () => { _ = meshData.GetIndexDataAsync(true); }, bytes => measured = bytes);

            AssertWithinBudget(measured);
        }

        /// <remarks>
        /// Reports rather than asserts: a throw from this nested coroutine would leave the calling test
        /// suspended, so its `using` locals would never be released.
        /// </remarks>
        static IEnumerator MeasureRepeatedCalls(AllocationRecorder recorder, Action call, Action<long> report)
        {
            call();

            yield return null;
            recorder.SampleFrame();
            var idle = recorder.TotalBytes;

            for (var i = 0; i < k_Iterations; i++)
            {
                call();
            }

            yield return null;
            recorder.SampleFrame();

            report(recorder.TotalBytes - idle);
        }

        static void AssertWithinBudget(long measured)
        {
            Assert.GreaterOrEqual(measured, 0, "The measurement never ran.");
            Assert.Less(
                measured,
                k_MaxBytes,
                $"{k_Iterations} calls allocated {measured} bytes (one idle frame included).");
        }
    }
}
