// SPDX-FileCopyrightText: 2025 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using GLTFast.Logging;
using GLTFast.Tests.Import;
using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace GLTFast.Tests
{
    [Category("Performance")]
    class ImportPerformanceTests : IPrebuildSetup
    {
        const int k_Repetitions = 10;

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

        static async Task RunTest(Func<GltfImportBase, Task<bool>> loadFunction)
        {
            var go = new GameObject();
            var loadLogger = new CollectingLogger();

            using var gltf = new GltfImport(logger: loadLogger);

            var success = await loadFunction(gltf);

            if (!success)
            {
                loadLogger.LogAll();
            }
            Assert.IsTrue(success);

            var instantiateLogger = new CollectingLogger();
            var instantiator = AssetsTests.CreateInstantiator(gltf, instantiateLogger, go.transform);
            success = await gltf.InstantiateMainSceneAsync(instantiator);
            if (!success)
            {
                instantiateLogger.LogAll();
                throw new AssertionException("glTF instantiation failed");
            }
            Object.Destroy(go);
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
