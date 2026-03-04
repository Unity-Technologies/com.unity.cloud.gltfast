// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GLTFast.Logging;
using NUnit.Framework;
#if UNITY_ENTITIES_GRAPHICS
using Unity.Entities;
#endif
using UnityEngine;

namespace GLTFast.Tests.Import
{
    [TestFixture, Category("Import")]
    class AssetsTests
    {
#if UNITY_ENTITIES_GRAPHICS
        static World s_World;
        static Entity s_SceneRoot;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            s_World = World.DefaultGameObjectInjectionWorld;
            s_SceneRoot = EntityUtils.CreateSceneRootEntity(s_World);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            var entityManager = s_World.EntityManager;
            entityManager.DestroyEntity(s_SceneRoot);
        }
#endif // UNITY_ENTITIES_GRAPHICS

        [GltfTestCase("glTF-test-models", 63)]
        public IEnumerator GltfTestModels(GltfTestCaseSet testCaseSet, GltfTestCase testCase)
        {
            yield return AsyncWrapper.WaitForTask(RunTestCase(testCaseSet, testCase));
        }

        [GltfTestCase("glTF-test-models", 2, @"glTF-Binary\/.*\.glb$")]
        public IEnumerator GltfTestModelsBinary(GltfTestCaseSet testCaseSet, GltfTestCase testCase)
        {
            yield return AsyncWrapper.WaitForTask(RunTestCase(testCaseSet, testCase));
        }

        [GltfTestCase("glTF-test-models", 2, @"glTF-Embedded\/.*\.gltf$")]
        public IEnumerator GltfTestModelsEmbedded(GltfTestCaseSet testCaseSet, GltfTestCase testCase)
        {
            yield return AsyncWrapper.WaitForTask(RunTestCase(testCaseSet, testCase));
        }

        [GltfTestCase("glTF-Sample-Assets", 38, @"glTF(-JPG-PNG)?\/.*\.gltf$")]
        public IEnumerator KhronosGltfSampleAssets(GltfTestCaseSet testCaseSet, GltfTestCase testCase)
        {
            yield return AsyncWrapper.WaitForTask(RunTestCase(testCaseSet, testCase));
        }

        [GltfTestCase("glTF-Sample-Assets", 1, @"glTF-Binary\/.*\.glb$")]
        public IEnumerator KhronosGltfSampleAssetsBinary(GltfTestCaseSet testCaseSet, GltfTestCase testCase)
        {
            yield return AsyncWrapper.WaitForTask(RunTestCase(testCaseSet, testCase));
        }

        [GltfTestCase("glTF-Sample-Assets", 1, @"glTF-Draco\/.*\.gltf$")]
        public IEnumerator KhronosGltfSampleAssetsDraco(GltfTestCaseSet testCaseSet, GltfTestCase testCase)
        {
#if DRACO_IS_RECENT
            yield return AsyncWrapper.WaitForTask(RunTestCase(testCaseSet, testCase));
#else
            Assert.Ignore("Requires Draco for Unity package to be installed.");
            yield break;
#endif
        }

        [GltfTestCase("glTF-test-models", 1, @"FullyTextured\/FullyTextured.gltf$")]
        public IEnumerator KtxMissing(GltfTestCaseSet testCaseSet, GltfTestCase testCase)
        {
#if KTX_IS_RECENT
            yield return null;
            Assert.Ignore("Requires absence of KTX for Unity package.");
#else
            Assert.Contains(Extension.TextureBasisUniversal, testCase.requiredExtensions);
            // Note: testCase.requiredExtensions is not passed on,
            // since we want to certify it correctly rejects the glTF.
            testCase = new GltfTestCase
            {
                relativeUri = testCase.relativeUri,
                expectLoadFail = true,
                expectInstantiationFail = testCase.expectInstantiationFail,
                expectedLogCodes = new[] { LogCode.PackageMissing }
            };
            yield return AsyncWrapper.WaitForTask(RunTestCase(testCaseSet, testCase));
#endif
        }

        [GltfTestCase("glTF-Sample-Assets", 1, @"Box\/glTF-Draco\/Box.gltf$")]
        public IEnumerator DracoMissing(GltfTestCaseSet testCaseSet, GltfTestCase testCase)
        {
#if DRACO_IS_RECENT
            yield return null;
            Assert.Ignore("Requires absence of Draco for Unity package.");
#else
            Assert.Contains(Extension.DracoMeshCompression, testCase.requiredExtensions);
            // Note: testCase.requiredExtensions is not passed on,
            // since we want to certify it correctly rejects the glTF.
            testCase = new GltfTestCase
            {
                relativeUri = testCase.relativeUri,
                expectLoadFail = true,
                expectInstantiationFail = testCase.expectInstantiationFail,
                expectedLogCodes = new[] { LogCode.PackageMissing },
            };
            yield return AsyncWrapper.WaitForTask(RunTestCase(testCaseSet, testCase));
#endif
        }

        [GltfTestCase("glTF-Sample-Assets", 1, @"glTF-Quantized\/.*\.gltf$")]
        public IEnumerator KhronosGltfSampleAssetsQuantized(GltfTestCaseSet testCaseSet, GltfTestCase testCase)
        {
            yield return AsyncWrapper.WaitForTask(RunTestCase(testCaseSet, testCase));
        }

        internal static async Task RunTestCase(GltfTestCaseSet testCaseSet, GltfTestCase testCase, bool logLoadingMessage = true, CancellationToken cancellationToken = default)
        {

            AssertRequiredExtensions(testCase.requiredExtensions);
            var deferAgent = new UninterruptedDeferAgent();
            var loadLogger = new CollectingLogger();
            var path = Path.Combine(testCaseSet.RootPath, testCase.relativeUri);
            if (logLoadingMessage)
                Debug.Log($"Loading {testCase} from {path}");

            using var gltf = new GltfImport(deferAgent: deferAgent, logger: loadLogger);
            var success = await gltf.Load(path, cancellationToken: cancellationToken);
            if (loadLogger.Items?.Any(x => x.Code == LogCode.OperationCanceled) is true)
            {
                if (success)
                    throw new AssertionException("glTF import unexpectedly succeeded despite cancellation!");
                return;
            }
            if (success ^ !testCase.expectLoadFail)
            {
                AssertLoggers(new[] { loadLogger }, testCase);
                if (success)
                {
                    throw new AssertionException("glTF import unexpectedly succeeded!");
                }

                throw new AssertionException("glTF import failed!");
            }

            if (!success)
            {
                AssertLoggers(new[] { loadLogger }, testCase);
                return;
            }
            var instantiateLogger = new CollectingLogger();

#if !UNITY_ENTITIES_GRAPHICS
            var go = new GameObject();
#endif
            try
            {
                var instantiator =
#if UNITY_ENTITIES_GRAPHICS
                    new EntityInstantiator(gltf, s_SceneRoot, instantiateLogger);
#else
                    new GameObjectInstantiator(gltf, go.transform, instantiateLogger);
#endif
                success = await gltf.InstantiateMainSceneAsync(instantiator, cancellationToken);
                if (loadLogger.Items?.Any(x => x.Code == LogCode.OperationCanceled) is true
                    || instantiateLogger.Items?.Any(x => x.Code == LogCode.OperationCanceled) is true)
                {
                    if (success)
                        throw new AssertionException("glTF instantiation unexpectedly succeeded despite cancellation!");
                    return;
                }
                if (!success)
                {
                    instantiateLogger.LogAll();
                    throw new AssertionException("glTF instantiation failed");
                }
                AssertLoggers(new[] { loadLogger, instantiateLogger }, testCase);
#if UNITY_ENTITIES_GRAPHICS
                await Task.Yield();
#endif
            }
            finally
            {
#if !UNITY_ENTITIES_GRAPHICS
#if UNITY_EDITOR
                UnityEngine.Object.DestroyImmediate(go);
#else
                UnityEngine.Object.Destroy(go);
#endif // UNITY_EDITOR
#else
                var entityManager = s_World.EntityManager;
                EntityUtils.DestroyChildren(ref s_SceneRoot, ref entityManager);
#endif
            }
        }

        internal static void AssertLoggers(IEnumerable<CollectingLogger> loggers, GltfTestCase testCase)
        {
            AssertLogItems(IterateLoggerItems(), testCase);
            return;

            IEnumerable<LogItem> IterateLoggerItems()
            {
                foreach (var logger in loggers)
                {
                    if (logger.Count < 1) continue;
                    foreach (var item in logger.Items)
                    {
                        yield return item;
                    }
                }
            }
        }

        internal static void AssertLogItems(IEnumerable<LogItem> logItems, GltfTestCase testCase)
        {
            LoggerTest.AssertLogCodes(logItems, testCase.expectedLogCodes);
        }

        internal static void AssertRequiredExtensions(Extension[] requiredExtensions)
        {
            if (requiredExtensions == null)
                return;
            foreach (var extension in requiredExtensions)
            {
                switch (extension)
                {
#if !KTX_IS_RECENT
                    case Extension.TextureBasisUniversal:
                        Assert.Ignore("Requires KTX for Unity package to be installed.");
                        break;
#endif
#if !DRACO_IS_RECENT
                    case Extension.DracoMeshCompression:
                        Assert.Ignore("Requires Draco for Unity package to be installed.");
                        break;
#endif
#if !MESHOPT_IS_RECENT
                    case Extension.MeshoptCompression:
                        Assert.Ignore("Requires meshoptimizer decompression for Unity package to be installed.");
                        break;
#endif
                    case Extension.TextureWebP:
                        Assert.Ignore("WebP is not generally supported yet.");
                        break;
                    default:
                        break;
                }
            }
        }
    }
}
