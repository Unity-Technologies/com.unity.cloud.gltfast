// SPDX-FileCopyrightText: 2025 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using Unity.Cloud.Gltfast.Logging;
using Unity.Cloud.Gltfast.Tests.Import;
using NUnit.Framework;
#if UNITY_ENTITIES_GRAPHICS
using Unity.Entities;
#endif
using Unity.PerformanceTesting;
using UnityEngine;
using UnityEngine.TestTools;
#if !UNITY_ENTITIES_GRAPHICS
using Object = UnityEngine.Object;
#endif

namespace Unity.Cloud.Gltfast.Tests
{
    [Category("Performance")]
    class ImportPerformanceTests : IPrebuildSetup
    {
        const int k_Repetitions = 10;

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

        [UnityTest, Performance]
        public IEnumerator FlatHierarchy()
        {
#if !RUN_PERFORMANCE_TESTS
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
#endif
            yield return AsyncWrapper.WaitForTask(TestWrapper(() => RunTest(
                TestGltfGenerator.FlatHierarchyPath), k_Repetitions)
            );
        }

        [UnityTest, Performance]
        public IEnumerator FlatHierarchyBinary()
        {
#if !RUN_PERFORMANCE_TESTS
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
#endif
            yield return AsyncWrapper.WaitForTask(TestWrapper(() => RunTest(
                    TestGltfGenerator.FlatHierarchyBinaryPath), k_Repetitions)
            );
        }

        [UnityTest, Performance]
        public IEnumerator FlatHierarchyMemory()
        {
#if !RUN_PERFORMANCE_TESTS
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
#endif
            yield return AsyncWrapper.WaitForTask(RunTestFromMemory(
                    TestGltfGenerator.FlatHierarchyPath));
        }

        [UnityTest, Performance]
        public IEnumerator BigCylinder()
        {
#if !RUN_PERFORMANCE_TESTS
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
#endif
            yield return AsyncWrapper.WaitForTask(TestWrapper(() => RunTest(
                TestGltfGenerator.BigCylinderPath), k_Repetitions, 3)
            );
        }

        [UnityTest, Performance]
        public IEnumerator BigCylinderBinary()
        {
#if !RUN_PERFORMANCE_TESTS
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
#endif
            yield return AsyncWrapper.WaitForTask(TestWrapper(() => RunTest(
                TestGltfGenerator.BigCylinderBinaryPath), k_Repetitions, 3)
            );
        }

        [UnityTest, Performance]
        public IEnumerator BigCylinderBinaryMemory()
        {
#if !RUN_PERFORMANCE_TESTS
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
#endif
            yield return AsyncWrapper.WaitForTask(RunTestFromMemory(TestGltfGenerator.BigCylinderBinaryPath));
        }

#if RUN_PERFORMANCE_TESTS
        public async void Setup()
        {
            await TestGltfGenerator.CertifyPerformanceTestGltfs();
        }
#else
        public void Setup() { }
#endif

        static async Task RunTestFromMemory(string path)
        {
            using var data = await LoadTests.ReadNativeArrayAsync(path);
            await TestWrapper(() => RunTest(gltf =>
            {
                Debug.Log($"Loading {path}");
                return gltf.Load(data.AsReadOnly(), new Uri(path));
            }), k_Repetitions);
        }

        static async Task RunTest(string path)
        {
            await RunTest(gltf =>
            {
                Debug.Log($"Loading {path}");
                return gltf.Load(path);
            });
        }

        static async Task RunTest(Func<GltfImport, Task<bool>> loadFunction)
        {
#if !UNITY_ENTITIES_GRAPHICS
            var go = new GameObject();
#endif
            var loadLogger = new CollectingLogger();

            using var gltf = new GltfImport(logger: loadLogger);

            var success = await loadFunction(gltf);

            if (!success)
            {
                loadLogger.LogAll();
            }
            Assert.IsTrue(success);

            var instantiateLogger = new CollectingLogger();
            var instantiator =
#if UNITY_ENTITIES_GRAPHICS
                new EntityInstantiator(gltf, s_SceneRoot, instantiateLogger);
#else
                new GameObjectInstantiator(gltf, go.transform, instantiateLogger);
#endif
            try
            {
                success = await gltf.InstantiateMainSceneAsync(instantiator);
                if (!success)
                {
                    instantiateLogger.LogAll();
                    throw new AssertionException("glTF instantiation failed");
                }
            }
            finally
            {
#if UNITY_ENTITIES_GRAPHICS
                await Task.Yield();
                var entityManager = s_World.EntityManager;
                EntityUtils.DestroyChildren(ref s_SceneRoot, ref entityManager);
#else
                Object.Destroy(go);
#endif
            }
        }

        static async Task TestWrapper(Func<Task> action, int repeat, int warmup = 1)
        {
            for (var i = 0; i < warmup; i++)
            {
                await action();
            }

            for (var i = 0; i < repeat; i++)
            {
                using (Measure.Scope())
                {
                    await action();
                }
            }
        }
    }
}
