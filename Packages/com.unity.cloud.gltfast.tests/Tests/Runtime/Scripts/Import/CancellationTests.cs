// SPDX-FileCopyrightText: 2025 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using NUnit.Framework;
using Debug = UnityEngine.Debug;

namespace GLTFast.Tests.Import
{
    /// <summary>
    /// Tests for cancellation during loading.
    /// </summary>
    [Category("Import")]
    class CancellationTests
    {
        const int k_MaxIterations = 100;

        GltfTestCaseRunner m_Runner;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            m_Runner = new GltfTestCaseRunner();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            m_Runner.Dispose();
        }

        [GltfTestCase("glTF-Sample-Assets", 38, @"glTF(-JPG-PNG)?\/.*\.gltf$")]
        public IEnumerator CancelImport_SampleAssets(GltfTestCaseSet testCaseSet, GltfTestCase testCase)
        {
            yield return CancellationTestInternal(testCaseSet, testCase, k_MaxIterations);
        }

        [GltfTestCase("glTF-test-models", 65)]
        public IEnumerator CancelImport_TestModels(GltfTestCaseSet testCaseSet, GltfTestCase testCase)
        {
            yield return CancellationTestInternal(testCaseSet, testCase, k_MaxIterations);
        }

        IEnumerator CancellationTestInternal(GltfTestCaseSet testCaseSet, GltfTestCase testCase, int maxIterations)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            Assert.Ignore("Cannot load from StreamingAssets file on Android, as they are in the compressed JAR file.");
#endif

            for (var i = 0; i < maxIterations; i++)
            {
                using var cts = new CancellationTokenSource();
                var action = SetCancellationAfterIteration(cts, i);
                yield return AsyncWrapper.WaitForTask(m_Runner.Run(testCaseSet, testCase, i == 0, cts.Token));
                // exit once we reach max iterations or when asset is successfully instantiated without any cancellation
                if (!cts.IsCancellationRequested || i == maxIterations - 1)
                {
                    CancellationTokenExtension.s_OnCancellationCheck -= action;
                    Debug.Log($"{testCase.Filename} successfully canceled {m_Sources.Count} times in the following methods:\n\t{string.Join("\n\t", m_Sources)}");
                    m_Sources.Clear();
                    break;
                }
            }
        }

        readonly HashSet<string> m_Sources = new();

        Action SetCancellationAfterIteration(CancellationTokenSource cts, int iteration)
        {
            CancellationTokenExtension.s_OnCancellationCheck += Action;
            return Action;

            void Action()
            {
                var stackTrace = new StackTrace();
                // frame 0 is this test method, frame 1 is the cancellation method, frame 2 is the target caller
                var source = stackTrace.GetFrame(2).GetMethod() is MethodInfo methodInfo
                    ? $"{methodInfo.DeclaringType}.{methodInfo.Name}"
                    : "unknown";
                // skips redundant cancellation attempts by tracking caller sources
                m_Sources.Add(source);

                if (m_Sources.Count > iteration)
                {
                    CancellationTokenExtension.s_OnCancellationCheck -= Action;
                    m_Sources.Clear();
                    cts.Cancel();
                }
            }
        }
    }
}
