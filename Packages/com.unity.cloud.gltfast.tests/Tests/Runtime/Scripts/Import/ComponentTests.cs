// SPDX-FileCopyrightText: 2024 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Cloud.Gltfast.Tests.Import
{
    [Category("Import")]
    class ComponentTests
    {
        const string k_TestAsset = @"glTF\/FormatVariants.gltf$";
        const string k_AnimatedTestAsset = @"RainbowCuboid\/original\/RainbowCuboid.gltf$";

        [GltfTestCase("glTF-test-models", 1, k_TestAsset)]
        public IEnumerator GltfAsset(GltfTestCaseSet testCaseSet, GltfTestCase testCase)
        {
            var task = LoadGltfViaComponent<GltfAsset>(
                Path.Combine(testCaseSet.RootPath, testCase.relativeUri),
                asset => asset.LoadOnStartup = false
                );
            yield return Utils.WaitForTask(task);
        }

        [GltfTestCase("glTF-test-models", 1, k_AnimatedTestAsset)]
        public IEnumerator GltfAssetAnimatedTwice(GltfTestCaseSet testCaseSet, GltfTestCase testCase)
        {
            var task = LoadGltfViaComponent<GltfAsset>(
                Path.Combine(testCaseSet.RootPath, testCase.relativeUri),
                asset => asset.LoadOnStartup = false,
                repetitions: 2
            );
            yield return Utils.WaitForTask(task);
        }

        [GltfTestCase("glTF-test-models", 1, k_AnimatedTestAsset)]
        public IEnumerator GltfAssetTweak(GltfTestCaseSet testCaseSet, GltfTestCase testCase)
        {
            var task = LoadGltfViaComponent<GltfAsset>(
                Path.Combine(testCaseSet.RootPath, testCase.relativeUri),
                asset =>
                {
                    Assert.AreEqual(-1, asset.SceneId);
                    Assert.IsTrue(asset.PlayAutomatically);
                    asset.SceneId = 0;
                    asset.PlayAutomatically = false;
                    asset.LoadOnStartup = false;
                }
            );
            yield return Utils.WaitForTask(task);
        }

        [GltfTestCase("glTF-test-models", 1, k_TestAsset)]
        public IEnumerator GltfBoundsAsset(GltfTestCaseSet testCaseSet, GltfTestCase testCase)
        {
#if !UNITY_PHYSICS
            LogAssert.Expect(LogType.Error, "GltfBoundsAsset requires the built-in Physics package to be enabled (in the Package Manager)");
#endif
            var task = LoadGltfViaComponent<GltfBoundsAsset>(
                Path.Combine(testCaseSet.RootPath, testCase.relativeUri),
                asset => asset.LoadOnStartup = false
                );
            yield return Utils.WaitForTask(task);
        }

        [GltfTestCase("glTF-test-models", 1, k_TestAsset)]
        public IEnumerator GltfEntityAsset(GltfTestCaseSet testCaseSet, GltfTestCase testCase)
        {
#if UNITY_ENTITIES_GRAPHICS
            var task = LoadGltfViaComponent<GltfEntityAsset>(
                Path.Combine(testCaseSet.RootPath, testCase.relativeUri),
                asset => asset.LoadOnStartup = false
            );
            yield return Utils.WaitForTask(task);
#else
            Assert.Ignore("Requires Entities package to be installed.");
            yield break;
#endif
        }

        static async Task<T> LoadGltfViaComponent<T>(string uri, Action<T> setupCallback, int repetitions = 0) where T : GltfAssetBase
        {
            var gltf = new GameObject("glTF").AddComponent<T>();
            setupCallback(gltf);
            // gltf.Url = uri;
            Debug.Log($"Loading {uri}");
            var result = await gltf.Load(uri);
            Assert.IsTrue(result, $"Failed to load {uri}.");
            for (var i = 0; i < repetitions; i++)
            {
                gltf.ClearScenes();
                gltf.Dispose();
                result = await gltf.Load(uri);
                Assert.IsTrue(result, $"Failed to load {uri} on repetition {repetitions}");
            }
            return gltf;
        }
    }
}
