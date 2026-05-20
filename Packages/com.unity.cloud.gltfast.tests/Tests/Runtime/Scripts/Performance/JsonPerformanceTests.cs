// SPDX-FileCopyrightText: 2024 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using GLTFast.Schema;
using NUnit.Framework;
using Unity.Collections;
using Unity.Gltfast.Text.Json;
using Unity.PerformanceTesting;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.TestTools;

namespace GLTFast.Tests.Performance
{
    [TestFixture]
    [Category("Performance")]
    class JsonPerformanceTests : IPrebuildSetup
    {
        /// <summary>"{}" UTF-8 encoded.</summary>
        static readonly byte[] k_GltfJsonEmptyInput = { 0x7b, 0x7d };
        static NativeArray<byte> s_GltfJsonEmpty;

        NativeArray<byte> m_GltfJsonFlatHierarchy;

        [OneTimeSetUp]
        public void SetUpTest()
        {
            s_GltfJsonEmpty = new NativeArray<byte>(k_GltfJsonEmptyInput, Allocator.Persistent);
#if RUN_PERFORMANCE_TESTS
            m_GltfJsonFlatHierarchy = new NativeArray<byte>(File.ReadAllBytes(TestGltfGenerator.FlatHierarchyPath), Allocator.Persistent);
#endif
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            s_GltfJsonEmpty.Dispose();
#if RUN_PERFORMANCE_TESTS
            m_GltfJsonFlatHierarchy.Dispose();
#endif
        }

#if RUN_PERFORMANCE_TESTS
        public async void Setup()
        {
            await TestGltfGenerator.CertifyPerformanceTestGltfs();
        }
#else
        public void Setup() { }
#endif

        [Test, Performance]
        public void Empty()
        {
            RunTest(
                s_GltfJsonEmpty.AsReadOnly(),
                "Empty.JsonUtility",
                DeserializeWrapper
            );
        }

        [Test, Performance]
        public void EmptyExtended()
        {
            RunTest(
                s_GltfJsonEmpty.AsReadOnly(),
                "Empty.Extended",
                DeserializeWrapper
            );
        }

        [Test, Performance]
        public void FlatHierarchy()
        {
#if !RUN_PERFORMANCE_TESTS
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
#endif
            RunTest(
                m_GltfJsonFlatHierarchy.AsReadOnly(),
                "FlatHierarchy.JsonUtility",
                DeserializeWrapper
            );
        }

        [Test]
        public void FlatHierarchyCheck()
        {
#if !RUN_PERFORMANCE_TESTS
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
#endif
            var gltf = DeserializeWrapper(m_GltfJsonFlatHierarchy.AsReadOnly());
            CheckFlatHierarchy(gltf);
        }

        [Test, Performance]
        public void FlatHierarchyExtended()
        {
#if !RUN_PERFORMANCE_TESTS
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
#endif
            RunTest(
                m_GltfJsonFlatHierarchy.AsReadOnly(),
                "FlatHierarchy.Extended",
                DeserializeWrapper
            );
        }

        [Test]
        public void FlatHierarchyExtendedCheck()
        {
#if !RUN_PERFORMANCE_TESTS
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
#endif
            var gltf = DeserializeWrapper(m_GltfJsonFlatHierarchy.AsReadOnly());
            CheckFlatHierarchy(gltf);
        }

        static void CheckFlatHierarchy(RootBase gltf)
        {
            Assert.NotNull(gltf?.Asset);
            Assert.AreEqual("2.0", gltf.Asset.version);
            Assert.IsFalse(string.IsNullOrEmpty(gltf.Asset.generator));
            Assert.IsTrue(gltf.Asset.generator.StartsWith("Unity"));
            Assert.IsTrue(gltf.Asset.generator.Contains("glTFast"));
            Assert.AreEqual(0, gltf.scene);
            Assert.AreEqual(10_000, gltf.Nodes.Count);
            Assert.AreEqual("Node-20-14-11", gltf.Nodes[9999].name);
            Assert.AreEqual(-20f, gltf.Nodes[9999].translation[0]);
            Assert.AreEqual(14f, gltf.Nodes[9999].translation[1]);
            Assert.AreEqual(11f, gltf.Nodes[9999].translation[2]);
            Assert.AreEqual(10_000, gltf.Scenes[0].nodes.Length);
            Assert.AreEqual(42, gltf.Scenes[0].nodes[42]);
            Assert.AreEqual(9999, gltf.Scenes[0].nodes[9999]);
        }

        internal static Root DeserializeWrapper(NativeArray<byte>.ReadOnly json)
        {
            Profiler.BeginSample("JsonPerformanceTests.DeserializeWrapper");
            var result = JsonSerializer.Deserialize(json.AsReadOnlySpan(), GltfRootSourceGenerator.Default.Root);
            Profiler.EndSample();
            return result;
        }

        static void RunTest<T>(
            NativeArray<byte>.ReadOnly gltfJson,
            string profilingMarker,
            Func<NativeArray<byte>.ReadOnly, T> jsonParser
            ) where T : RootBase
        {
            var profilerMarkerName = $"JsonPerf.{profilingMarker}";
            var measure = Measure.Method(() =>
                {
                    Profiler.BeginSample(profilerMarkerName);
                    jsonParser(gltfJson);
                    Profiler.EndSample();
                }).GC();
            measure.Run();
        }

        internal static void RunTest<T>(
            NativeArray<byte>.ReadOnly gltfJson,
            string profilingMarker,
            Func<NativeArray<byte>.ReadOnly, T> jsonParser,
            Action<T> resultCallback
        ) where T : RootBase
        {
            var profilerMarkerName = $"JsonPerf.{profilingMarker}";
            var measure = Measure.Method(() =>
            {
                Profiler.BeginSample(profilerMarkerName);
                resultCallback(jsonParser(gltfJson));
                Profiler.EndSample();
            }).GC();
            measure.Run();
        }
    }
}
