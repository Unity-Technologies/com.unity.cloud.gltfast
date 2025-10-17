// SPDX-FileCopyrightText: 2024 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using GLTFast.Logging;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GLTFast.Tests.Import
{
    [Category("Import")]
    class SubMeshTests
    {
        const string k_TestAsset = @"SubMesh\/glTF\/SubMesh.gltf";

        [GltfTestCase("glTF-test-models", 1, k_TestAsset)]
        public IEnumerator SubMeshTest(GltfTestCaseSet testCaseSet, GltfTestCase testCase)
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
            var logger = new ConsoleLogger();
            using var gltf = new GltfImport(deferAgent: deferAgent, logger: logger);
            var success = await gltf.Load(path);
            Assert.IsTrue(success);

            // The glTF has 3 meshes, but two of them are identical and should get merged.
            // Every mesh consists of two primitives, which should result in a single Unity mesh
            // with two sub-meshes each.
            Assert.AreEqual(2, gltf.Meshes.Count);

            Assert.AreEqual(1, gltf.GetMeshCount(0));
            foreach (var cube in gltf.GetMeshes(0))
            {
                Assert.AreEqual("Cube", cube.name);
                Assert.AreEqual(2, cube.subMeshCount);
                Assert.AreEqual(24, cube.vertexCount);

                Utils.AssertNearOrEqual(float3.zero, cube.bounds.center);
                Utils.AssertNearOrEqual(new float3(1f), cube.bounds.extents);

                var cubeSubMesh0 = cube.GetSubMesh(0);
                Assert.AreEqual(12, cubeSubMesh0.vertexCount);
                Assert.AreEqual(18, cubeSubMesh0.indexCount);
                Assert.AreEqual(0, cubeSubMesh0.indexStart);
                Assert.AreEqual(0, cubeSubMesh0.baseVertex);
                Assert.AreEqual(0, cubeSubMesh0.firstVertex);

                var cubeSubMesh1 = cube.GetSubMesh(1);
                Assert.AreEqual(12, cubeSubMesh1.vertexCount);
                Assert.AreEqual(18, cubeSubMesh1.indexCount);
                Assert.AreEqual(18, cubeSubMesh1.indexStart);
                Assert.AreEqual(12, cubeSubMesh1.baseVertex);
                Assert.AreEqual(12, cubeSubMesh1.firstVertex);
            }

            Assert.AreEqual(1, gltf.GetMeshCount(2));
            foreach (var plane in gltf.GetMeshes(2))
            {
                Assert.AreEqual("Plane", plane.name);
                Assert.AreEqual(6, plane.vertexCount);
                Assert.AreEqual(2, plane.subMeshCount);

                Utils.AssertNearOrEqual(float3.zero, plane.bounds.center);
                Utils.AssertNearOrEqual(new float3(.5f, 0, .5f), plane.bounds.extents);

                var planeSubMesh0 = plane.GetSubMesh(0);
                Assert.AreEqual(3, planeSubMesh0.vertexCount);
                Assert.AreEqual(3, planeSubMesh0.indexCount);
                Assert.AreEqual(0, planeSubMesh0.indexStart);
                Assert.AreEqual(0, planeSubMesh0.baseVertex);
                Assert.AreEqual(0, planeSubMesh0.firstVertex);

                var planeSubMesh1 = plane.GetSubMesh(1);
                Assert.AreEqual(3, planeSubMesh1.vertexCount);
                Assert.AreEqual(3, planeSubMesh1.indexCount);
                Assert.AreEqual(3, planeSubMesh1.indexStart);
                Assert.AreEqual(3, planeSubMesh1.baseVertex);
                Assert.AreEqual(3, planeSubMesh1.firstVertex);
            }

            Object.Destroy(go);
        }
    }
}
