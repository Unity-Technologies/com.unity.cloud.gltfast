// SPDX-FileCopyrightText: 2024 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using GLTFast.Export;
using NUnit.Framework;
using UnityEngine;

namespace GLTFast.Tests.Export
{
    [TestFixture, Category("Export")]
    class GltfShaderGraphMaterialExporterTests : MaterialExportTests
    {
        [Test]
        public void BaseColor()
        {
            CertifyRequirements();
            BaseColorTest(RenderPipeline.Universal);
        }

        [Test]
        public void BaseColorTexture()
        {
            CertifyRequirements();
            BaseColorTextureTest(RenderPipeline.Universal);
        }

        [Test]
        public void BaseColorTextureTranslated()
        {
            CertifyRequirements();
            BaseColorTextureTranslatedTest(RenderPipeline.Universal);
        }

        [Test]
        public void BaseColorTextureScaled()
        {
            CertifyRequirements();
            BaseColorTextureScaledTest(RenderPipeline.Universal);
        }

        [Test]
        public void BaseColorTextureRotated()
        {
            CertifyRequirements();
            BaseColorTextureRotatedTest(RenderPipeline.Universal);
        }

        [Test]
        public void BaseColorTextureCutout()
        {
            CertifyRequirements();
            BaseColorTextureCutoutTest(RenderPipeline.Universal);
        }

        [Test]
        public void BaseColorTextureTransparent()
        {
            CertifyRequirements();
            BaseColorTextureTransparentTest(RenderPipeline.Universal);
        }

        [Test]
        public void RoughnessTexture()
        {
            CertifyRequirements();
            RoughnessTextureTest(RenderPipeline.Universal);
        }

        [Test]
        public void Metallic()
        {
            CertifyRequirements();
            MetallicTest(RenderPipeline.Universal);
        }

        [Test]
        public void MetallicTexture()
        {
            CertifyRequirements();
            MetallicTextureTest(RenderPipeline.Universal);
        }

        [Test]
        public void MetallicRoughnessTexture()
        {
            CertifyRequirements();
            MetallicRoughnessTextureTest(RenderPipeline.Universal);
        }

        [Test]
        public void MetallicRoughnessOcclusionTexture()
        {
            CertifyRequirements();
            MetallicRoughnessOcclusionTextureTest(RenderPipeline.Universal);
        }

        [Test]
        public void OcclusionTexture()
        {
            CertifyRequirements();
            OcclusionTextureTest(RenderPipeline.Universal);
        }

        [Test]
        public void EmissiveFactor()
        {
            CertifyRequirements();
            EmissiveFactorTest(RenderPipeline.Universal);
        }

        [Test]
        public void EmissiveTexture()
        {
            CertifyRequirements();
            EmissiveTextureTest(RenderPipeline.Universal);
        }

        [Test]
        public void EmissiveTextureFactor()
        {
            CertifyRequirements();
            EmissiveTextureFactorTest(RenderPipeline.Universal);
        }

        [Test]
        public void NormalTexture()
        {
            CertifyRequirements();
            NormalTextureTest(RenderPipeline.Universal);
        }

        [Test]
        public void NotGltf()
        {
            CertifyRequirements();
            NotGltfTest(RenderPipeline.Universal);
        }

        [Test]
        public void Omni()
        {
            CertifyRequirements();
            OmniTest(RenderPipeline.Universal);
        }

        [Test]
        public void AddImageFail()
        {
            CertifyRequirements();
            AddImageFailTest(RenderPipeline.Universal);
        }

        [Test]
        public void DoubleSided()
        {
            CertifyRequirements();
            DoubleSidedTest(RenderPipeline.Universal);
        }

        static void CertifyRequirements()
        {
#if !UNITY_SHADER_GRAPH
            Assert.Ignore("Shader Graph package is missing...ignoring tests on Shader Graph based materials.");
#endif
            if (RenderPipeline.Universal != RenderPipelineUtils.RenderPipeline)
            {
                Assert.Ignore("Test requires Universal Render Pipeline.");
            }
        }

        protected override void SetUpExporter()
        {
#if UNITY_SHADER_GRAPH
            m_Exporter = new GltfShaderGraphMaterialExporter();
#endif
        }
    }
}
