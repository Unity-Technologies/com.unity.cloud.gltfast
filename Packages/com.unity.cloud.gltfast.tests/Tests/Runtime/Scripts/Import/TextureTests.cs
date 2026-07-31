// SPDX-FileCopyrightText: 2026 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections;
using NUnit.Framework;

namespace Unity.Cloud.Gltfast.Tests.Import
{
    [TestFixture, System.ComponentModel.Category("Import")]
    class TextureTests
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

        [GltfTestCase("glTF-test-models", 1, "EightTexcoords.gltf$")]
        public IEnumerator TextureReusedFlipped(GltfTestCaseSet testCaseSet, GltfTestCase testCase)
        {
            yield return AsyncWrapper.WaitForTask(
                m_Runner.Run(testCaseSet, testCase, preInstantiationCallback: PreInstantiationCallback));
            yield break;

            void PreInstantiationCallback(GltfImport gltf)
            {
                Assert.AreEqual(8, gltf.TextureCount);
                for (var i = 0; i < 8; i++)
                {
                    Assert.IsFalse(gltf.IsTextureYFlipped(i), $"Texture {i} is flipped!");
                }
            }
        }

        [GltfTestCase("glTF-test-models", 1, "TextureVariants.gltf$")]
        public IEnumerator TextureVariantsFlipped(GltfTestCaseSet testCaseSet, GltfTestCase testCase)
        {
            yield return AsyncWrapper.WaitForTask(
                m_Runner.Run(testCaseSet, testCase, preInstantiationCallback: PreInstantiationCallback));
            yield break;

            void PreInstantiationCallback(GltfImport gltf)
            {
                Assert.AreEqual(2, gltf.TextureCount);
                for (var i = 0; i < 2; i++)
                {
                    Assert.IsFalse(gltf.IsTextureYFlipped(i), $"Texture {i} is flipped!");
                }
            }
        }

        [GltfTestCase("glTF-test-models", 1, "TextureVariants-KTX.gltf")]
        public IEnumerator TextureVariantsNonFlipped(GltfTestCaseSet testCaseSet, GltfTestCase testCase)
        {
            yield return AsyncWrapper.WaitForTask(
                m_Runner.Run(testCaseSet, testCase, preInstantiationCallback: PreInstantiationCallback));
            yield break;

            void PreInstantiationCallback(GltfImport gltf)
            {
                Assert.AreEqual(2, gltf.TextureCount);
                for (var i = 0; i < 2; i++)
                {
                    Assert.IsTrue(gltf.IsTextureYFlipped(i), $"Texture {i} is not flipped!");
                }
            }
        }
    }
}
