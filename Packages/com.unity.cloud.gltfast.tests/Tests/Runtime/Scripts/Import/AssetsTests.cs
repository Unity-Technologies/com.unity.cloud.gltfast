// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using GLTFast.Logging;
using NUnit.Framework;
#if UNITY_ENTITIES_GRAPHICS || UNITY_DOTS_HYBRID
using Unity.Entities;
using Unity.Transforms;
#endif
using UnityEngine;
using Object = UnityEngine.Object;

namespace GLTFast.Tests.Import
{
    [TestFixture, Category("Import")]
    class AssetsTests
    {
        [GltfTestCase("glTF-test-models", 57)]
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

        internal static async Task RunTestCase(GltfTestCaseSet testCaseSet, GltfTestCase testCase)
        {
            var go = new GameObject();
            try
            {
                AssertRequiredExtensions(testCase.requiredExtensions);
                var deferAgent = new UninterruptedDeferAgent();
                var loadLogger = new CollectingLogger();
                var path = Path.Combine(testCaseSet.RootPath, testCase.relativeUri);
                Debug.Log($"Loading {testCase} from {path}");

                using var gltf = new GltfImport(deferAgent: deferAgent, logger: loadLogger);
                var success = await gltf.Load(path);
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
                var instantiator = CreateInstantiator(gltf, instantiateLogger, go.transform);
                success = await gltf.InstantiateMainSceneAsync(instantiator);
                if (!success)
                {
                    instantiateLogger.LogAll();
                    throw new AssertionException("glTF instantiation failed");
                }
                AssertLoggers(new[] { loadLogger, instantiateLogger }, testCase);
            }
            finally
            {
                Object.Destroy(go);
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

        internal static IInstantiator CreateInstantiator(
            IGltfReadable gltf,
            ICodeLogger logger,
            Transform transform
            )
        {
#if UNITY_ENTITIES_GRAPHICS || UNITY_DOTS_HYBRID
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            var sceneArchetype = entityManager.CreateArchetype(
#if UNITY_ENTITIES_GRAPHICS
                typeof(LocalTransform),
                typeof(LocalToWorld)
#else
                typeof(Translation),
                typeof(Rotation),
                typeof(Scale),
                typeof(LocalToWorld)
#endif
            );
            var sceneRoot = entityManager.CreateEntity(sceneArchetype);
            return new EntityInstantiator(gltf, sceneRoot, logger);
#else
            return new GameObjectInstantiator(gltf, transform, logger);
#endif
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
#if !MESHOPT
                    case Extension.MeshoptCompression:
                        Assert.Ignore("Requires meshoptimizer decompression for Unity package to be installed.");
                        break;
#endif
                    default:
                        break;
                }
            }
        }
    }
}
