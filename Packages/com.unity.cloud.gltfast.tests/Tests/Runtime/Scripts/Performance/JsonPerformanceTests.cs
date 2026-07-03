// SPDX-FileCopyrightText: 2024 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GLTFast.Schema;
using Newtonsoft.Json;
using NUnit.Framework;
using Unity.Collections;
using Unity.PerformanceTesting;
using Unity.Profiling;
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
        Dictionary<string, NativeArray<byte>> m_LoadedFiles;

        [OneTimeSetUp]
        public void SetUpTest()
        {
            s_GltfJsonEmpty = new NativeArray<byte>(k_GltfJsonEmptyInput, Allocator.Persistent);
            m_LoadedFiles = new Dictionary<string, NativeArray<byte>>();
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
            if (m_LoadedFiles != null)
            {
                foreach (var arr in m_LoadedFiles.Values)
                    arr.Dispose();
                m_LoadedFiles = null;
            }
        }

#if RUN_PERFORMANCE_TESTS
        public async void Setup()
        {
            await TestGltfGenerator.CertifyPerformanceTestGltfs();
        }
#else
        public void Setup() { }
#endif

        [UnityTest, Performance]
        public IEnumerator Empty()
        {
            var jsonParser = new GltfJsonUtilityParserWrapper();
            yield return RunTest(s_GltfJsonEmpty.AsReadOnly(), "Empty.JsonUtility", jsonParser.ParseJson);
        }

        [UnityTest, Performance]
        public IEnumerator EmptyExtended()
        {
            yield return RunTest(s_GltfJsonEmpty.AsReadOnly(), "Empty.NewtonsoftJson", JsonConvertWrapper);
        }

        [UnityTest, Performance]
        public IEnumerator FlatHierarchy()
        {
#if !RUN_PERFORMANCE_TESTS
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
            yield break;
#else
            var jsonParser = new GltfJsonUtilityParserWrapper();
            yield return RunTest(m_GltfJsonFlatHierarchy.AsReadOnly(), "FlatHierarchy.JsonUtility", jsonParser.ParseJson);
#endif
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

        [UnityTest, Performance]
        public IEnumerator FlatHierarchyExtended()
        {
#if !RUN_PERFORMANCE_TESTS
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
            yield break;
#else
            yield return RunTest(m_GltfJsonFlatHierarchy.AsReadOnly(), "FlatHierarchy.NewtonsoftJson", JsonConvertWrapper);
#endif
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

        [UnityTest, Performance]
        [TestCaseSource(nameof(TestCasesCoroutine))]
        public IEnumerator Run(TestGltfConfiguration config)
            => RunScenario(config.GetPath(), config.name, false);

        [UnityTest, Performance]
        [TestCaseSource(nameof(TestCasesCoroutine))]
        public IEnumerator RunNewtonsoft(TestGltfConfiguration config)
            => RunScenario(config.GetPath(), config.name, true);

        [Test]
        [TestCaseSource(nameof(TestCases))]
        public void Once(TestGltfConfiguration config)
        {
#if !RUN_PERFORMANCE_TESTS
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
#endif
            var data = LoadJson(config.GetPath());

            // Warmup
            JsonConvertWrapper(data);

            Profiler.BeginSample($"ParseOnce-Newtonsoft-{config.name}");
            JsonConvertWrapper(data);
            Profiler.EndSample();

            var jsonParser = new GltfJsonUtilityParserWrapper();
            Profiler.BeginSample($"ParseOnce-JsonUtility-{config.name}");
            jsonParser.ParseJson(data);
            Profiler.EndSample();
        }

        static IEnumerable TestCases()
        {
            foreach (var config in TestGltfJsonGenerator.Configurations)
            {
                var data = new TestCaseData(config);
                data.SetName($"{{m}}-{config.name}");
                yield return data;
            }
        }

        static IEnumerable TestCasesCoroutine()
        {
            return from object config in TestCases() select (config as TestCaseData)!.Returns(null);
        }

        IEnumerator RunScenario(string path, string label, bool useNewtonsoft)
        {
#if !RUN_PERFORMANCE_TESTS
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
            yield break;
#else
            var data = LoadJson(path);
            if (useNewtonsoft)
            {
                yield return RunTest(data, $"{label}.NewtonsoftJson", JsonConvertWrapper);
            }
            else
            {
                var jsonParser = new GltfJsonUtilityParserWrapper();
                yield return RunTest(data, $"{label}.JsonUtility", jsonParser.ParseJson);
            }
#endif
        }

        NativeArray<byte>.ReadOnly LoadJson(string path)
        {
            if (!m_LoadedFiles.TryGetValue(path, out var arr))
            {
                arr = new NativeArray<byte>(File.ReadAllBytes(path), Allocator.Persistent);
                m_LoadedFiles[path] = arr;
            }
            return arr.AsReadOnly();
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

        static IEnumerator RunTest<T>(
            NativeArray<byte>.ReadOnly gltfJson,
            string profilingMarker,
            Func<NativeArray<byte>.ReadOnly, T> jsonParser
            ) where T : RootBase
        {
            // Timing (synchronous; Measure.Method does its own iteration).
            var profilerMarkerName = $"JsonPerf.{profilingMarker}";
            Measure.Method(() =>
                {
                    Profiler.BeginSample(profilerMarkerName);
                    jsonParser(gltfJson);
                    Profiler.EndSample();
                }).GC().Run();

            // Allocation (frame-gated; needs to yield).
            yield return MeasureAllocatedBytes(gltfJson, profilingMarker, jsonParser);
        }

        // Per-frame counter exposed by Unity Profiler. Captures every managed
        // allocation on the main thread during the frame, including transient
        // garbage that was already collected before the frame ended.
        const string k_GcAllocCounter = "GC Allocated In Frame";

        // Number of parser invocations packed into one frame. Engine per-frame
        // overhead (editor repaint, Profiler bookkeeping, ...) is constant per
        // frame so it averages out: noise / k_CallsPerFrame.
        const int k_CallsPerFrame = 20;

        // Distinct frames sampled, each averaging k_CallsPerFrame invocations.
        const int k_AllocationSamples = 5;

        /// <summary>
        /// Records the total managed bytes allocated by one
        /// <paramref name="jsonParser"/> invocation — including transient
        /// garbage freed before the call returns.
        ///
        /// Strategy: <see cref="ProfilerRecorder"/> against
        /// <c>GC Allocated In Frame</c>, which reports the total bytes the
        /// managed allocator handed out during the previous frame on the main
        /// thread. We run <see cref="k_CallsPerFrame"/> parser invocations in
        /// a single frame so the per-frame engine noise (a few KB at most) is
        /// amortized over many invocations. A baseline empty-frame sample is
        /// subtracted to remove that noise floor. Sampled
        /// <see cref="k_AllocationSamples"/> times.
        ///
        /// Why this and not <see cref="GC.TryStartNoGCRegion(long)"/>: Unity
        /// Mono ships TryStartNoGCRegion as a NotImplementedException stub.
        /// <see cref="GC.GetAllocatedBytesForCurrentThread"/> returns 0.
        /// ProfilerRecorder against this counter is the only Mono-compatible
        /// API that exposes per-frame bytes allocated. (On IL2CPP it works
        /// too, so this measurement is the same on every backend.)
        /// </summary>
        static IEnumerator MeasureAllocatedBytes<T>(
            NativeArray<byte>.ReadOnly gltfJson,
            string profilingMarker,
            Func<NativeArray<byte>.ReadOnly, T> jsonParser
            ) where T : RootBase
        {
            // Warm up so JIT, string interning, and lazy type init don't
            // contaminate the first measured iteration.
            GC.KeepAlive(jsonParser(gltfJson));

            var recorder = ProfilerRecorder.StartNew(
                ProfilerCategory.Memory, k_GcAllocCounter);
            try
            {
                if (!recorder.Valid)
                {
                    Debug.LogWarning(
                        $"[JsonPerf] ProfilerRecorder counter \"{k_GcAllocCounter}\" not "
                        + $"available on this Unity build. Allocation measurement skipped "
                        + $"for {profilingMarker}.");
                    yield break;
                }

                // Let the recorder warm up — first frame's LastValue is 0 even
                // if allocations happened, because there's no previous frame.
                yield return null;
                yield return null;

                // Baseline: bytes allocated in an empty frame (everything that
                // is not our parser invocation: editor repaint, Profiler, etc.).
                yield return null;
                yield return null;
                var baseline = recorder.LastValue;

                var sampleGroup = new SampleGroup(
                    $"JsonPerf.{profilingMarker}.AllocatedBytes",
                    SampleUnit.Megabyte);
                var keepAlive = new object[k_CallsPerFrame];

                for (var s = 0; s < k_AllocationSamples; s++)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();

                    // Sync to a clean frame boundary.
                    yield return null;

                    // Pack k_CallsPerFrame invocations into this single frame
                    // so per-frame engine overhead amortizes to ~0 per call.
                    for (var i = 0; i < k_CallsPerFrame; i++)
                        keepAlive[i] = jsonParser(gltfJson);

                    // Yield once so the frame ends and the recorder snapshots
                    // its counter for the just-completed frame.
                    yield return null;

                    var framedBytes = recorder.LastValue;
                    GC.KeepAlive(keepAlive);

                    var net = framedBytes - baseline;
                    if (net < 0) net = framedBytes; // baseline noisier than parser frame
                    var perCall = net / k_CallsPerFrame;
                    Measure.Custom(sampleGroup, perCall / 1_048_576.0);
                }
            }
            finally
            {
                if (recorder.Valid)
                    recorder.Dispose();
            }
        }
    }
}
