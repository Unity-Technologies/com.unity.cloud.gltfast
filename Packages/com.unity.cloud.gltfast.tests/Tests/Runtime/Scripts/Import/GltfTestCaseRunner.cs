// SPDX-FileCopyrightText: 2026 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
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
    sealed class GltfTestCaseRunner : IDisposable
    {
#if UNITY_ENTITIES_GRAPHICS
        World m_World;
        Entity m_SceneRoot;

        public GltfTestCaseRunner()
        {
            m_World = World.DefaultGameObjectInjectionWorld;
            m_SceneRoot = EntityUtils.CreateSceneRootEntity(m_World);
        }
#endif // UNITY_ENTITIES_GRAPHICS

        public async Task Run(
            GltfTestCaseSet testCaseSet,
            GltfTestCase testCase,
            bool logLoadingMessage = true,
            CancellationToken cancellationToken = default,
            Action<GltfImport> preLoadCallback = null
            )
        {
            AssertRequiredExtensions(testCase.requiredExtensions);
            var deferAgent = new UninterruptedDeferAgent();
            var loadLogger = new CollectingLogger();
            var path = Path.Combine(testCaseSet.RootPath, testCase.relativeUri);
            if (logLoadingMessage)
                Debug.Log($"Loading {testCase} from {path}");

            using var gltf = new GltfImport(deferAgent: deferAgent, logger: loadLogger);
            preLoadCallback?.Invoke(gltf);
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
                    new EntityInstantiator(gltf, m_SceneRoot, instantiateLogger);
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
                var entityManager = m_World.EntityManager;
                EntityUtils.DestroyChildren(ref m_SceneRoot, ref entityManager);
#endif
            }
        }

        public static void AssertRequiredExtensions(Extension[] requiredExtensions)
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

        public static void AssertLoggers(IEnumerable<CollectingLogger> loggers, GltfTestCase testCase)
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

        public static void AssertLogItems(IEnumerable<LogItem> logItems, GltfTestCase testCase)
        {
            LoggerTest.AssertLogCodes(logItems, testCase.expectedLogCodes);
        }

        public void Dispose()
        {
#if UNITY_ENTITIES_GRAPHICS
            var entityManager = m_World.EntityManager;
            entityManager.DestroyEntity(m_SceneRoot);
#endif
        }
    }
}
