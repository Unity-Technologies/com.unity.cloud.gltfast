// SPDX-FileCopyrightText: 2025 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using GLTFast.Logging;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace GLTFast.Tests.Import
{
    [Category("Import")]
    class BoundsTests
    {
        const float k_Epsilon = 1E-08f;

        [GltfTestCase("glTF-test-models", 1, @"Bounds\/Bounds\/Bounds.gltf")]
        public IEnumerator BoundsTest(GltfTestCaseSet testCaseSet, GltfTestCase testCase)
        {
            var task = LoadInternal(testCaseSet, testCase);
            yield return Utils.WaitForTask(task);
        }

        [GltfTestCase("glTF-test-models", 1, @"Bounds\/BoundsMissing\/BoundsMissing.gltf")]
        public IEnumerator MissingBoundsTest(GltfTestCaseSet testCaseSet, GltfTestCase testCase)
        {
            var task = LoadInternal(testCaseSet, testCase);
            yield return Utils.WaitForTask(task);
        }

        static async Task LoadInternal(
            GltfTestCaseSet testCaseSet,
            GltfTestCase testCase
            )
        {
            var path = Path.Combine(testCaseSet.RootPath, testCase.relativeUri);
            Debug.Log($"Testing {path}");
            var go = new GameObject();
            var deferAgent = new UninterruptedDeferAgent();
            var logger = new CollectingLogger();
            using var gltf = new GltfImport(deferAgent: deferAgent, logger: logger);
            var success = await gltf.Load(path);
            Assert.IsTrue(success);

            GltfTestCaseRunner.AssertLoggers(new[] { logger }, testCase);

            Assert.AreEqual(2, gltf.Meshes.Count);

            Assert.AreEqual(1, gltf.GetMeshCount(0));
            foreach (var oneTri in gltf.GetMeshes(0))
            {
                Assert.AreEqual("OneTriangle", oneTri.name);
                Assert.AreEqual(1, oneTri.subMeshCount);
                Assert.AreEqual(3, oneTri.vertexCount);

                Utils.AssertNearOrEqual(new float3(-.05f, .05f, -.1f), oneTri.bounds.center, k_Epsilon);
                Utils.AssertNearOrEqual(new float3(.15f, .25f, .2f), oneTri.bounds.extents, k_Epsilon);

                var triSubMesh = oneTri.GetSubMesh(0);
                Utils.AssertNearOrEqual(new float3(-.05f, .05f, -.1f), triSubMesh.bounds.center, k_Epsilon);
                Utils.AssertNearOrEqual(new float3(.15f, .25f, .2f), triSubMesh.bounds.extents, k_Epsilon);
            }

            Assert.AreEqual(1, gltf.GetMeshCount(1));
            foreach (var twoTris in gltf.GetMeshes(1))
            {
                Assert.AreEqual("TwoTriangles", twoTris.name);
                Assert.AreEqual(6, twoTris.vertexCount);
                Assert.AreEqual(2, twoTris.subMeshCount);

                Utils.AssertNearOrEqual(new float3(.1f, .05f, -.1f), twoTris.bounds.center, k_Epsilon);
                Utils.AssertNearOrEqual(new float3(.3f, .25f, .2f), twoTris.bounds.extents, k_Epsilon);

                var twoTrisSubMesh0 = twoTris.GetSubMesh(0);
                Utils.AssertNearOrEqual(new float3(-.05f, .05f, -.1f), twoTrisSubMesh0.bounds.center, k_Epsilon);
                Utils.AssertNearOrEqual(new float3(.15f, .25f, .2f), twoTrisSubMesh0.bounds.extents, k_Epsilon);

                var twoTrisSubMesh1 = twoTris.GetSubMesh(1);
                Utils.AssertNearOrEqual(new float3(.25f, .05f, -.1f), twoTrisSubMesh1.bounds.center, k_Epsilon);
                Utils.AssertNearOrEqual(new float3(.15f, .25f, .2f), twoTrisSubMesh1.bounds.extents, k_Epsilon);
            }

            Object.Destroy(go);
        }
    }
}
