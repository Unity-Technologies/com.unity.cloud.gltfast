// SPDX-FileCopyrightText: 2024 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using GLTFast.Export;
using NUnit.Framework;
using UnityEngine;

namespace GLTFast.Tests.Export
{
    [TestFixture, Category("Export")]
    class GltfHdrpMaterialExporterTests : MaterialExportTests
    {
        [Test]
        public void BaseColor()
        {
            CertifyRequirements();
            BaseColorTest(RenderPipeline.HighDefinition);
        }

        [Test]
        public void BaseColorTexture()
        {
            CertifyRequirements();
            BaseColorTextureTest(RenderPipeline.HighDefinition);
        }

        [Test]
        public void BaseColorTextureTranslated()
        {
            CertifyRequirements();
            BaseColorTextureTranslatedTest(RenderPipeline.HighDefinition);
        }

        [Test]
        public void BaseColorTextureScaled()
        {
            CertifyRequirements();
            BaseColorTextureScaledTest(RenderPipeline.HighDefinition);
        }

        [Test]
        public void BaseColorTextureRotated()
        {
            CertifyRequirements();
            BaseColorTextureRotatedTest(RenderPipeline.HighDefinition);
        }

        [Test]
        public void BaseColorTextureCutout()
        {
            CertifyRequirements();
            BaseColorTextureCutoutTest(RenderPipeline.HighDefinition);
        }

        [Test]
        public void BaseColorTextureTransparent()
        {
            CertifyRequirements();
            BaseColorTextureTransparentTest(RenderPipeline.HighDefinition);
        }

        [Test]
        public void RoughnessTexture()
        {
            CertifyRequirements();
            RoughnessTextureTest(RenderPipeline.HighDefinition);
        }

        [Test]
        public void Metallic()
        {
            CertifyRequirements();
            MetallicTest(RenderPipeline.HighDefinition);
        }

        [Test]
        public void MetallicTexture()
        {
            CertifyRequirements();
            MetallicTextureTest(RenderPipeline.HighDefinition);
        }

        [Test]
        public void MetallicRoughnessTexture()
        {
            CertifyRequirements();
            MetallicRoughnessTextureTest(RenderPipeline.HighDefinition);
        }

        [Test]
        public void MetallicRoughnessOcclusionTexture()
        {
            CertifyRequirements();
            MetallicRoughnessOcclusionTextureTest(RenderPipeline.HighDefinition);
        }

        [Test]
        public void OcclusionTexture()
        {
            CertifyRequirements();
            OcclusionTextureTest(RenderPipeline.HighDefinition);
        }

        [Test]
        public void EmissiveFactor()
        {
            CertifyRequirements();
            EmissiveFactorTest(RenderPipeline.HighDefinition);
        }

        [Test]
        public void EmissiveTexture()
        {
            CertifyRequirements();
            EmissiveTextureTest(RenderPipeline.HighDefinition);
        }

        [Test]
        public void EmissiveTextureFactor()
        {
            CertifyRequirements();
            EmissiveTextureFactorTest(RenderPipeline.HighDefinition);
        }

        [Test]
        public void NormalTexture()
        {
            CertifyRequirements();
            NormalTextureTest(RenderPipeline.HighDefinition);
        }

        [Test]
        public void NotGltf()
        {
            CertifyRequirements();
            NotGltfTest(RenderPipeline.HighDefinition);
        }

        [Test]
        public void Omni()
        {
            CertifyRequirements();
            OmniTest(RenderPipeline.HighDefinition);
        }

        [Test]
        public void AddImageFail()
        {
            CertifyRequirements();
            AddImageFailTest(RenderPipeline.HighDefinition);
        }

        [Test]
        public void DoubleSided()
        {
            CertifyRequirements();
            DoubleSidedTest(RenderPipeline.HighDefinition);
        }

        static void CertifyRequirements()
        {
#if !UNITY_SHADER_GRAPH
            Assert.Ignore("Shader Graph package is missing...ignoring tests on Shader Graph based materials.");
#endif
            if (RenderPipeline.HighDefinition != RenderPipelineUtils.RenderPipeline)
            {
                Assert.Ignore("Test requires Universal Render Pipeline.");
            }
        }

        protected override void SetUpExporter()
        {
#if USING_HDRP
            m_Exporter = new GltfHdrpMaterialExporter();
#endif
        }
    }
}
