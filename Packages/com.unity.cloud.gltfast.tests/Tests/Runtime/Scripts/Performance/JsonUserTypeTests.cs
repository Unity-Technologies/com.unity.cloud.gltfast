// SPDX-FileCopyrightText: 2026 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Text;
using GLTFast.Schema;
using NUnit.Framework;
using Unity.Collections;
using Unity.Gltfast.Text.Json.Serialization;
using Unity.PerformanceTesting;

namespace GLTFast.Tests.Performance
{
    [TestFixture]
    class JsonUserTypeTests
    {
        NativeArray<byte> m_HeavyExtensionData;

        [OneTimeSetUp]
        public void SetUpTest()
        {
            var heavyExtensionData = GenerateHeavyExtensionJson(10, 50);
            m_HeavyExtensionData = new NativeArray<byte>(
                Encoding.UTF8.GetBytes(heavyExtensionData),
                Allocator.Persistent
            );
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            m_HeavyExtensionData.Dispose();
        }

        [Test, Performance]
        public void HeavyExtensionDataTest()
        {
#if RUN_PERFORMANCE_TESTS
            JsonPerformanceTests.RunTest(
                m_HeavyExtensionData.AsReadOnly(),
                "FlatHierarchy.Extended",
                JsonPerformanceTests.DeserializeWrapper,
                gltf =>
                {
                    foreach (var node in gltf.Nodes)
                    {
                        Assert.IsTrue(node.Extensions.TryGetValue(
                            "HeavyExtension",
                            out HeavyExtension heavyExt
                        ));
                        Assert.AreEqual(3.1415f, heavyExt.value);
                    }
                }
            );
#else
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
#endif
        }

        [Test]
        public void HeavyExtensionDataCheck()
        {
            var options = GltfRootSourceGenerator.Default.Options;
            var typeInfo = options.TypeInfoResolver?.GetTypeInfo(typeof(HeavyExtension), options);
            Assert.IsNull(typeInfo);

            var gltf = JsonPerformanceTests.DeserializeWrapper(m_HeavyExtensionData.AsReadOnly());
            var ext = gltf?.Nodes?[0]?.Extensions;
            Assert.NotNull(ext);
            Assert.IsTrue(ext.TryGetValue("HeavyExtension", out HeavyExtension heavyExtension));
            Assert.AreEqual(3.1415f, heavyExtension.value);
        }

        /// <summary>
        /// Generates a glTF JSON string with <paramref name="nodeCount"/> nodes, each carrying a
        /// <c>HeavyExtension</c> populated with <paramref name="matricesPerNode"/> 4x4 float matrices.
        /// </summary>
        static string GenerateHeavyExtensionJson(int nodeCount, int matricesPerNode)
        {
            var sb = new StringBuilder();
            sb.Append("{\"nodes\":[");
            for (var i = 0; i < nodeCount; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append("{\"extensions\":{\"HeavyExtension\":{\"value\":3.1415,\"matrices\":[");
                for (var m = 0; m < matricesPerNode; m++)
                {
                    if (m > 0) sb.Append(',');
                    sb.Append("{\"values\":[");
                    for (var v = 0; v < 16; v++)
                    {
                        if (v > 0) sb.Append(',');
                        sb.Append(v + 1);
                    }
                    sb.Append("]}");
                }
                sb.Append("]}}}");
            }
            sb.Append("]}");
            return sb.ToString();
        }
    }

    [Serializable]
    struct HeavyExtension
    {
        public float value;
        public Matrix[] matrices;
    }

    struct Matrix
    {
        [JsonConverter(typeof(Float16ArrayConverter))]
        public float[] values;
    }
}
