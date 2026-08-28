// SPDX-FileCopyrightText: 2024 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Unity.Cloud.Gltfast.Objects;
using Unity.Cloud.Gltfast.Text.Json;
using Unity.Collections;
using Unity.PerformanceTesting;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.TestTools;

namespace Unity.Cloud.Gltfast.Tests.Performance
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
#if !RUN_PERFORMANCE_TESTS
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
#endif
            yield return RunTest(
                s_GltfJsonEmpty.AsReadOnly(),
                "Empty",
                DeserializeWrapper
            );
        }

        [UnityTest, Performance]
        public IEnumerator FlatHierarchy()
        {
#if !RUN_PERFORMANCE_TESTS
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
#endif
            yield return RunTest(
                m_GltfJsonFlatHierarchy.AsReadOnly(),
                "FlatHierarchy",
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

        [UnityTest, Performance]
        [TestCaseSource(nameof(TestCasesCoroutine))]
        public IEnumerator Run(TestGltfConfiguration config)
            => RunScenario(config.GetPath(), config.name);

        [Test]
        [TestCaseSource(nameof(TestCases))]
        public void Once(TestGltfConfiguration config)
        {
#if !RUN_PERFORMANCE_TESTS
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
#endif
            var data = LoadJson(config.GetPath());

            // Warmup
            DeserializeWrapper(data);

            Profiler.BeginSample($"ParseOnce-STJ-{config.name}");
            DeserializeWrapper(data);
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

        IEnumerator RunScenario(string path, string label)
        {
#if !RUN_PERFORMANCE_TESTS
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
            yield break;
#else
            var data = LoadJson(path);
            yield return RunTest(data, $"{label}.STJ", DeserializeWrapper);
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

        static void CheckFlatHierarchy(Root gltf)
        {
            Assert.NotNull(gltf?.Asset);
            Assert.AreEqual("2.0", gltf.Asset.Version);
            Assert.IsFalse(string.IsNullOrEmpty(gltf.Asset.Generator));
            Assert.IsTrue(gltf.Asset.Generator.StartsWith("Unity"));
            Assert.IsTrue(gltf.Asset.Generator.Contains("glTFast"));
            Assert.AreEqual(0, gltf.Scene);
            Assert.AreEqual(10_000, gltf.Nodes.Count);
            Assert.AreEqual("Node-20-14-11", gltf.Nodes[9999].Name);
            Assert.IsTrue(gltf.Nodes[9999].Translation.HasValue);
            Assert.AreEqual(-20.0, gltf.Nodes[9999].Translation.Value.x);
            Assert.AreEqual(14.0, gltf.Nodes[9999].Translation.Value.y);
            Assert.AreEqual(11.0, gltf.Nodes[9999].Translation.Value.z);
            Assert.AreEqual(10_000, gltf.Scenes[0].Nodes.Count);
            Assert.AreEqual(42, gltf.Scenes[0].Nodes[42]);
            Assert.AreEqual(9999, gltf.Scenes[0].Nodes[9999]);
        }

        internal static Root DeserializeWrapper(NativeArray<byte>.ReadOnly json)
        {
            Profiler.BeginSample("JsonPerformanceTests.DeserializeWrapper");
            var result = JsonSerializer.Deserialize(json.AsReadOnlySpan(), GltfJsonContext.Default.Root);
            Profiler.EndSample();
            return result;
        }

        static IEnumerator RunTest<T>(
            NativeArray<byte>.ReadOnly gltfJson,
            string profilingMarker,
            Func<NativeArray<byte>.ReadOnly, T> jsonParser
            ) where T : Root
            => RunWork(() => jsonParser(gltfJson), profilingMarker);

        /// <summary>
        /// Measures one unit of <paramref name="work"/>: timing (via
        /// <see cref="Measure.Method"/>) followed by managed allocation (via
        /// <see cref="MeasureAllocatedBytes"/>). The work's return value is
        /// kept alive so the JIT cannot elide it. Use this to benchmark a step
        /// in isolation — e.g. post-deserialization traversal or conversion —
        /// rather than always folding it into the parse.
        /// </summary>
        internal static IEnumerator RunWork(Func<object> work, string profilingMarker)
        {
            // Timing (synchronous; Measure.Method does its own iteration).
            var profilerMarkerName = $"JsonPerf.{profilingMarker}";
            Measure.Method(() =>
                {
                    Profiler.BeginSample(profilerMarkerName);
                    GC.KeepAlive(work());
                    Profiler.EndSample();
                }).GC().Run();

            // Allocation (frame-gated; needs to yield).
            yield return MeasureAllocatedBytes(work, profilingMarker);
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
        static IEnumerator MeasureAllocatedBytes(
            Func<object> work,
            string profilingMarker
            )
        {
            // Warm up so JIT, string interning, and lazy type init don't
            // contaminate the first measured iteration.
            GC.KeepAlive(work());

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
                        keepAlive[i] = work();

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

        internal static void RunTest<T>(
            NativeArray<byte>.ReadOnly gltfJson,
            string profilingMarker,
            Func<NativeArray<byte>.ReadOnly, T> jsonParser,
            Action<T> resultCallback
        ) where T : Root
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
