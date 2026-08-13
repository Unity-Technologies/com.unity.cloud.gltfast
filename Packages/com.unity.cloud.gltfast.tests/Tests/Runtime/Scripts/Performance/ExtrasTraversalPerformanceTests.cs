// SPDX-FileCopyrightText: 2026 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;
using NUnit.Framework;
using Unity.Cloud.Gltfast.Objects;
using Unity.Cloud.Gltfast.Tests;
using Unity.Cloud.Gltfast.Tests.Performance;
using Unity.Cloud.Gltfast.Text.Json;
using Unity.Collections;
using Unity.PerformanceTesting;
using UnityEngine.TestTools;

namespace GLTFast.Tests.Performance
{
    /// <summary>
    /// Benchmarks the step that happens *after* JSON deserialization: walking
    /// the additional properties captured on <c>extras</c>/<c>extensions</c>
    /// objects and converting them to user types. <see cref="JsonPerformanceTests"/>
    /// measures the parse; these tests measure traversal and conversion in
    /// isolation, against an already-deserialized <see cref="Root"/>.
    /// </summary>
    [TestFixture]
    [Category("Performance")]
    class ExtrasTraversalPerformanceTests : IPrebuildSetup
    {
        // Real "extras" perf scenario: 1111 nodes, each with 2..20 heterogeneous
        // additional properties (strings, numbers, bools, and arrays thereof).
        NativeArray<byte> m_ExtrasBytes;
        Root m_ExtrasGltf;

        // Synthetic payload whose every node carries a single typed "meta"
        // object, used to benchmark TryGetValue<T> conversion in isolation.
        NativeArray<byte> m_MetadataBytes;
        Root m_MetadataGltf;

        long m_Checksum;

        [OneTimeSetUp]
        public void SetUpTest()
        {
#if RUN_PERFORMANCE_TESTS
#if UNITY_EDITOR
            TestGltfJsonGenerator.CreateMissing();
#endif
            m_ExtrasBytes = new NativeArray<byte>(
                File.ReadAllBytes(GetConfigPath("extras")), Allocator.Persistent);
            m_ExtrasGltf = JsonPerformanceTests.DeserializeWrapper(m_ExtrasBytes.AsReadOnly());

            m_MetadataBytes = new NativeArray<byte>(
                Encoding.UTF8.GetBytes(GenerateMetadataExtrasJson(1000, 8)), Allocator.Persistent);
            m_MetadataGltf = JsonPerformanceTests.DeserializeWrapper(m_MetadataBytes.AsReadOnly());
#endif
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            if (m_ExtrasBytes.IsCreated)
                m_ExtrasBytes.Dispose();
            if (m_MetadataBytes.IsCreated)
                m_MetadataBytes.Dispose();
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
        public IEnumerator TraverseExtras()
        {
#if !RUN_PERFORMANCE_TESTS
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
            yield break;
#else
            yield return JsonPerformanceTests.RunWork(
                () => { m_Checksum += TraverseAllExtras(m_ExtrasGltf); return null; },
                "Extras.Traverse");
#endif
        }

        [UnityTest, Performance]
        public IEnumerator ConvertExtras()
        {
#if !RUN_PERFORMANCE_TESTS
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
            yield break;
#else
            yield return JsonPerformanceTests.RunWork(
                () => { m_Checksum += ConvertAllMetadata(m_MetadataGltf); return null; },
                "Extras.Convert");
#endif
        }

        [Test]
        public void TraverseCheck()
        {
            var gltf = Deserialize(
                @"{""nodes"":[{""extras"":{""a"":1,""b"":""xy"",""c"":[1,2,3],""d"":true}}]}");
            // a=1, b=len("xy")=2, c=1+2+3=6, d=true=1 => 10
            Assert.AreEqual(10, TraverseAllExtras(gltf));
        }

        [Test]
        public void ConvertCheck()
        {
            var gltf = Deserialize(
                @"{""nodes"":[{""extras"":{""meta"":{""id"":7,""label"":""abc"",""weights"":[0.0,0.5]}}}]}");
            var extras = gltf.Nodes[0].Extras;
            Assert.IsTrue(extras.TryGetValue("meta", out NodeMetadata meta));
            Assert.AreEqual(7, meta.id);
            Assert.AreEqual("abc", meta.label);
            Assert.AreEqual(2, meta.weights.Length);
        }

        static Root Deserialize(string json)
            => JsonSerializer.Deserialize(
                (ReadOnlySpan<byte>)Encoding.UTF8.GetBytes(json), GltfJsonContext.Default.Root);

        static long TraverseAllExtras(Root gltf)
        {
            var sum = 0L;
            var nodes = gltf.Nodes;
            for (var i = 0; i < nodes.Count; i++)
            {
                var extras = nodes[i].Extras;
                if (extras == null)
                    continue;
                foreach (var property in extras)
                    sum += Accumulate(property.Value);
            }
            return sum;
        }

        static long Accumulate(Value value)
        {
            switch (value.Kind)
            {
                case ValueKind.Number:
                    return value.TryGetInt64(out var l) ? l
                        : value.TryGetDouble(out var d) ? (long)d : 0L;
                case ValueKind.String:
                    return value.GetString()?.Length ?? 0;
                case ValueKind.True:
                case ValueKind.False:
                    return value.GetBoolean() ? 1L : 0L;
                case ValueKind.Array:
                {
                    var sum = 0L;
                    var length = value.ArrayLength;
                    for (var i = 0; i < length; i++)
                        sum += Accumulate(value[i]);
                    return sum;
                }
                case ValueKind.Object:
                {
                    var sum = 0L;
                    foreach (var property in value.EnumerateObject())
                        sum += Accumulate(property.Value);
                    return sum;
                }
                default:
                    return 0L;
            }
        }

        static long ConvertAllMetadata(Root gltf)
        {
            var sum = 0L;
            var nodes = gltf.Nodes;
            for (var i = 0; i < nodes.Count; i++)
            {
                var extras = nodes[i].Extras;
                if (extras != null && extras.TryGetValue("meta", out NodeMetadata meta))
                    sum += meta.id + (meta.label?.Length ?? 0) + (meta.weights?.Length ?? 0);
            }
            return sum;
        }

        static string GetConfigPath(string name)
        {
            foreach (var config in TestGltfJsonGenerator.Configurations)
            {
                if (config.name == name)
                    return config.GetPath();
            }
            throw new InvalidOperationException($"No performance configuration named '{name}'.");
        }

        static string GenerateMetadataExtrasJson(int nodeCount, int weightsPerNode)
        {
            var sb = new StringBuilder();
            sb.Append("{\"nodes\":[");
            for (var i = 0; i < nodeCount; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append("{\"extras\":{\"meta\":{\"id\":").Append(i)
                    .Append(",\"label\":\"node_").Append(i).Append("\",\"weights\":[");
                for (var w = 0; w < weightsPerNode; w++)
                {
                    if (w > 0) sb.Append(',');
                    sb.Append((w * 0.5f).ToString(CultureInfo.InvariantCulture));
                }
                sb.Append("]}}}");
            }
            sb.Append("]}");
            return sb.ToString();
        }
    }

    struct NodeMetadata
    {
        public int id;
        public string label;
        public float[] weights;
    }
}
