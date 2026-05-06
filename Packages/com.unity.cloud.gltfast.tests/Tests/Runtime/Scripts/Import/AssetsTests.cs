// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections;
using GLTFast.Logging;
using NUnit.Framework;
using UnityEngine;

namespace GLTFast.Tests.Import
{
    [TestFixture, Category("Import")]
    class AssetsTests
    {
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

        [GltfTestCase("glTF-test-models", 65)]
        public IEnumerator GltfTestModels(GltfTestCaseSet testCaseSet, GltfTestCase testCase)
        {
            yield return AsyncWrapper.WaitForTask(m_Runner.Run(testCaseSet, testCase));
        }

        [GltfTestCase("glTF-test-models", 2, @"glTF-Binary\/.*\.glb$")]
        public IEnumerator GltfTestModelsBinary(GltfTestCaseSet testCaseSet, GltfTestCase testCase)
        {
            yield return AsyncWrapper.WaitForTask(m_Runner.Run(testCaseSet, testCase));
        }

        [GltfTestCase("glTF-test-models", 2, @"glTF-Embedded\/.*\.gltf$")]
        public IEnumerator GltfTestModelsEmbedded(GltfTestCaseSet testCaseSet, GltfTestCase testCase)
        {
            yield return AsyncWrapper.WaitForTask(m_Runner.Run(testCaseSet, testCase));
        }

        [GltfTestCase("glTF-Sample-Assets", 38, @"glTF(-JPG-PNG)?\/.*\.gltf$")]
        public IEnumerator KhronosGltfSampleAssets(GltfTestCaseSet testCaseSet, GltfTestCase testCase)
        {
            yield return AsyncWrapper.WaitForTask(m_Runner.Run(testCaseSet, testCase));
        }

        [GltfTestCase("glTF-Sample-Assets", 1, @"glTF-Binary\/.*\.glb$")]
        public IEnumerator KhronosGltfSampleAssetsBinary(GltfTestCaseSet testCaseSet, GltfTestCase testCase)
        {
            yield return AsyncWrapper.WaitForTask(m_Runner.Run(testCaseSet, testCase));
        }

        [GltfTestCase("glTF-Sample-Assets", 1, @"glTF-Draco\/.*\.gltf$")]
        public IEnumerator KhronosGltfSampleAssetsDraco(GltfTestCaseSet testCaseSet, GltfTestCase testCase)
        {
#if DRACO_IS_RECENT
            yield return AsyncWrapper.WaitForTask(m_Runner.Run(testCaseSet, testCase));
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
            yield return AsyncWrapper.WaitForTask(m_Runner.Run(testCaseSet, testCase));
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
            yield return AsyncWrapper.WaitForTask(m_Runner.Run(testCaseSet, testCase));
#endif
        }

        [GltfTestCase("glTF-Sample-Assets", 1, @"glTF-Quantized\/.*\.gltf$")]
        public IEnumerator KhronosGltfSampleAssetsQuantized(GltfTestCaseSet testCaseSet, GltfTestCase testCase)
        {
            yield return AsyncWrapper.WaitForTask(m_Runner.Run(testCaseSet, testCase));
        }
    }
}
