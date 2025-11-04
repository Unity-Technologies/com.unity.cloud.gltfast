// SPDX-FileCopyrightText: 2025 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using GLTFast.Materials;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace GLTFast.Tests.Import
{
    /// <summary>
    /// Tests for memory leaks during loading, unloading and cancellation.
    /// </summary>
    [Category("Import")]
    class MemoryTests
    {
        static readonly TestCaseData[] k_SanityChecks =
        {
            GenerateTestCaseData(nameof(Empty), true),
            GenerateTestCaseData(nameof(GameObject_Create), false),
            GenerateTestCaseData(nameof(GameObject_CreateDestroy), true),
            GenerateTestCaseData(nameof(Material_Create), false),
            GenerateTestCaseData(nameof(Material_CreateDestroy), true),
            GenerateTestCaseData(nameof(Mesh_Create), false),
            GenerateTestCaseData(nameof(Mesh_CreateDestroy), true),
            GenerateTestCaseData(nameof(Texture_Create), false),
            GenerateTestCaseData(nameof(Texture_CreateDestroy), true),
        };

        static Object s_TrackedObject;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            // call GetDefaultMaterial here first to avoid it counting as an allocation in the first executed test
            var materialGenerator = MaterialGenerator.GetDefaultMaterialGenerator();
            _ = materialGenerator.GetDefaultMaterial();
        }

        [UnityTest]
        [TestCaseSource(nameof(k_SanityChecks))]
        public IEnumerator SanityChecks(MethodInfo method, bool expectEqual)
        {
            yield return GetAllObjectsTestInternal(InvokeMethod(method), expectEqual);

            // actually destroy the tracked object if it wasn't destroyed during the test
            if (s_TrackedObject != null)
                Object.Destroy(s_TrackedObject);
            yield return null;
        }

        [GltfTestCase("glTF-Sample-Assets", 38, @"glTF(-JPG-PNG)?\/.*\.gltf$")]
        public IEnumerator LoadUnload_SampleAssets(GltfTestCaseSet testCaseSet, GltfTestCase testCase)
        {
            yield return GetAllObjectsTestInternal(LoadFileInternal(testCaseSet, testCase), true);
        }

        [GltfTestCase("glTF-test-models", 56)]
        public IEnumerator LoadUnload_TestModels(GltfTestCaseSet testCaseSet, GltfTestCase testCase)
        {
            yield return GetAllObjectsTestInternal(LoadFileInternal(testCaseSet, testCase), true);
        }

        static IEnumerator LoadFileInternal(GltfTestCaseSet testCaseSet, GltfTestCase testCase)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            Assert.Ignore("Cannot load from StreamingAssets file on Android, as they are in the compressed JAR file.");
#endif
            yield return AsyncWrapper.WaitForTask(AssetsTests.RunTestCase(testCaseSet, testCase));
            yield return null;
        }

        static IEnumerator GetAllObjectsTestInternal(IEnumerator test, bool expectEqual)
        {
            var before = new List<Object>();
            var after = new List<Object>();

            before.AddRange(Object.FindObjectsByType<Object>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID));

            yield return test;

            GC.Collect();
            yield return null;

            after.AddRange(Object.FindObjectsByType<Object>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID));

            if (expectEqual)
                CollectionAssert.AreEquivalent(before, after);
            else
                CollectionAssert.AreNotEquivalent(before, after);
        }

        static TestCaseData GenerateTestCaseData(string methodName, bool expectEqual)
        {
            var method = typeof(MemoryTests).GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(method);
            return new TestCaseData(method, expectEqual).Returns(null).SetName(methodName);
        }

        static IEnumerator InvokeMethod(MethodInfo method)
        {
            method.Invoke(null, null);
            yield return null;
        }

        static void Empty()
        {
            // does nothing
        }

        static void GameObject_Create()
        {
            s_TrackedObject = new GameObject();
        }

        static void GameObject_CreateDestroy()
        {
            s_TrackedObject = new GameObject();
            Object.Destroy(s_TrackedObject);
        }

        static void Material_Create()
        {
            s_TrackedObject = new Material(Shader.Find("Standard"));
        }

        static void Material_CreateDestroy()
        {
            s_TrackedObject = new Material(Shader.Find("Standard"));
            Object.Destroy(s_TrackedObject);
        }

        static void Mesh_Create()
        {
            s_TrackedObject = new Mesh();
        }

        static void Mesh_CreateDestroy()
        {
            s_TrackedObject = new Mesh();
            Object.Destroy(s_TrackedObject);
        }

        static void Texture_Create()
        {
            s_TrackedObject = new Texture2D(1, 1);
        }

        static void Texture_CreateDestroy()
        {
            s_TrackedObject = new Texture2D(1, 1);
            Object.Destroy(s_TrackedObject);
        }
    }
}
