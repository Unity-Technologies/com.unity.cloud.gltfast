// SPDX-FileCopyrightText: 2024 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using GLTFast.Schema;
using Newtonsoft.Json;
using NUnit.Framework;
using Unity.Collections;
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
            var jsonParser = new GltfJsonUtilityParserWrapper();
            RunTest(
                s_GltfJsonEmpty.AsReadOnly(),
                "Empty.JsonUtility",
                jsonParser.ParseJson
            );
        }

        [Test, Performance]
        public void EmptyExtended()
        {
            RunTest(
                s_GltfJsonEmpty.AsReadOnly(),
                "Empty.NewtonsoftJson",
                JsonConvertWrapper
            );
        }

        [Test, Performance]
        public void FlatHierarchy()
        {
#if !RUN_PERFORMANCE_TESTS
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
#endif
            var jsonParser = new GltfJsonUtilityParserWrapper();
            RunTest(
                m_GltfJsonFlatHierarchy.AsReadOnly(),
                "FlatHierarchy.JsonUtility",
                jsonParser.ParseJson
            );
        }

        [Test]
        public void FlatHierarchyCheck()
        {
#if !RUN_PERFORMANCE_TESTS
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
#endif
            var jsonParser = new GltfJsonUtilityParser();
            Profiler.BeginSample("UTF-Conversion");
            var json = System.Text.Encoding.UTF8.GetString(m_GltfJsonFlatHierarchy);
            Profiler.EndSample();
            var gltf = jsonParser.ParseJson(json);
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
                "FlatHierarchy.NewtonsoftJson",
                JsonConvertWrapper
            );
        }

        [Test]
        public void FlatHierarchyExtendedCheck()
        {
#if !RUN_PERFORMANCE_TESTS
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
#endif
            Profiler.BeginSample("UTF-Conversion");
            var jsonString = System.Text.Encoding.UTF8.GetString(m_GltfJsonFlatHierarchy);
            Profiler.EndSample();
            var gltf = JsonConvert.DeserializeObject<Newtonsoft.Schema.Root>(jsonString);
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

        class GltfJsonUtilityParserWrapper
        {
            GltfJsonUtilityParser m_Parser = new();

            public RootBase ParseJson(NativeArray<byte>.ReadOnly json)
            {
                Profiler.BeginSample("UTF-Conversion");
                var jsonString = System.Text.Encoding.UTF8.GetString(json);
                Profiler.EndSample();
                return m_Parser.ParseJson(jsonString);
            }
        }

        static Newtonsoft.Schema.Root JsonConvertWrapper(NativeArray<byte>.ReadOnly json)
        {
            Profiler.BeginSample("UTF-Conversion");
            var jsonString = System.Text.Encoding.UTF8.GetString(json);
            Profiler.EndSample();
            return JsonConvert.DeserializeObject<Newtonsoft.Schema.Root>(jsonString);
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
    }
}
