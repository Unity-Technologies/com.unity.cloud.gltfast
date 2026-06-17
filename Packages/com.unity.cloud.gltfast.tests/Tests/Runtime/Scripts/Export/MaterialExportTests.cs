// SPDX-FileCopyrightText: 2024 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using GLTFast.Export;
using GLTFast.Logging;
using GLTFast.Schema;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Utils;
using Color = UnityEngine.Color;
using GltfMaterial = GLTFast.Schema.Material;
using Material = UnityEngine.Material;

namespace GLTFast.Tests.Export
{
    [Category("Export")]
    abstract class MaterialExportTests
    {
        const string k_ResourcePath = "Export/Materials/";

        static ColorEqualityComparer s_ColorComparer = new(10e-5f);

        protected IMaterialExport m_Exporter;

        protected void BaseColorTest(RenderPipeline renderPipeline)
        {
            var material = ConvertMaterial("BaseColor", out _, renderPipeline);

            Assert.IsNotNull(material.PbrMetallicRoughness);
            Assert.That(new Color(.2f, .5f, .75f, 1f),
                Is.EqualTo((Color)material.PbrMetallicRoughness.BaseColorFactor).Using(s_ColorComparer));
        }

        protected void BaseColorTextureTest(RenderPipeline renderPipeline)
        {
#if UNITY_IMAGECONVERSION
            var texture = BaseColorTextureTest(
                "BaseColorTexture",
                out var gltfWriter,
                renderPipeline,
                out _);
            Assert.IsNull(texture.Extensions);
            Assert.AreEqual(0, gltfWriter.extensions.Count);
#else
            Assert.Ignore("Texture export is disabled! " + LogMessages.GetFullMessage(LogCode.ImageConversionNotEnabled));
#endif
        }

        protected void BaseColorTextureTranslatedTest(RenderPipeline renderPipeline)
        {
#if UNITY_IMAGECONVERSION
            var texture = BaseColorTextureTest(
                "BaseColorTextureTranslated",
                out var gltfWriter,
                renderPipeline,
                out _);
            Assert.IsTrue(gltfWriter.extensions[Extension.TextureTransform]);
            var transform = texture.Extensions?.TextureTransform;
            Assert.IsNotNull(transform);
            Assert.AreEqual(new[] { 0.4f, 0.6f }, transform.Offset);
            Assert.AreEqual(new[] { 1f, 1f }, transform.Scale);
            Assert.AreEqual(0, transform.Rotation);
#else
            Assert.Ignore("Texture export is disabled! " + LogMessages.GetFullMessage(LogCode.ImageConversionNotEnabled));
#endif
        }


        protected void BaseColorTextureScaledTest(RenderPipeline renderPipeline)
        {
#if UNITY_IMAGECONVERSION
            var texture = BaseColorTextureTest(
                "BaseColorTextureScaled",
                out var gltfWriter,
                renderPipeline,
                out _);
            Assert.IsTrue(gltfWriter.extensions[Extension.TextureTransform]);
            var transform = texture.Extensions?.TextureTransform;
            Assert.IsNotNull(transform);
            Assert.AreEqual(new[] { 1.2f, 1.3f }, transform.Scale);
            Assert.AreEqual(new[] { 0f, 0f }, transform.Offset);
            Assert.AreEqual(0, transform.Rotation);
#else
            Assert.Ignore("Texture export is disabled! " + LogMessages.GetFullMessage(LogCode.ImageConversionNotEnabled));
#endif
        }

        protected void BaseColorTextureCutoutTest(RenderPipeline renderPipeline)
        {
#if UNITY_IMAGECONVERSION
            BaseColorTextureTest(
                "BaseColorTextureCutout",
                out _,
                renderPipeline,
                out var material);
            Assert.AreEqual(AlphaMode.Mask, material.AlphaMode.Value);
            Assert.AreEqual(.6f, material.AlphaCutoff);
#else
            Assert.Ignore("Texture export is disabled! " + LogMessages.GetFullMessage(LogCode.ImageConversionNotEnabled));
#endif
        }

        protected void BaseColorTextureTransparentTest(RenderPipeline renderPipeline)
        {
#if UNITY_IMAGECONVERSION
            BaseColorTextureTest(
                "BaseColorTextureTransparent",
                out _,
                renderPipeline,
                out var material);
            Assert.AreEqual(AlphaMode.Blend, material.AlphaMode.Value);
#else
            Assert.Ignore("Texture export is disabled! " + LogMessages.GetFullMessage(LogCode.ImageConversionNotEnabled));
#endif
        }

        protected void BaseColorTextureRotatedTest(RenderPipeline renderPipeline)
        {
#if UNITY_IMAGECONVERSION
            var texture = BaseColorTextureTest(
                "BaseColorTextureRotated",
                out var gltfWriter,
                renderPipeline,
                out _);
            Assert.IsTrue(gltfWriter.extensions[Extension.TextureTransform]);
            var transform = texture.Extensions?.TextureTransform;
            Assert.IsNotNull(transform);
            Assert.AreEqual(45, transform.Rotation);
            Assert.AreEqual(new[] { 0f, 0f }, transform.Offset);
            var comparer = new FloatEqualityComparer(10e-8f);
            Assert.That(transform.Scale, Is.EquivalentTo(new[] { 1f, 1f }).Using(comparer));
#else
            Assert.Ignore("Texture export is disabled! " + LogMessages.GetFullMessage(LogCode.ImageConversionNotEnabled));
#endif
        }

        protected void RoughnessTextureTest(RenderPipeline renderPipeline)
        {
#if UNITY_IMAGECONVERSION
            var material = ConvertMaterial("RoughnessTexture", out var gltfWriter, renderPipeline);

            Assert.AreEqual(1, gltfWriter.imageExports.Count);
            Assert.AreEqual(1, gltfWriter.samplers.Count);
            Assert.AreEqual(1, gltfWriter.textures.Count);
            Assert.IsInstanceOf<ImageExport>(gltfWriter.imageExports[0]);

            var mrTexture = material.PbrMetallicRoughness?.MetallicRoughnessTexture;
            Assert.IsNotNull(mrTexture);
            Assert.IsNull(mrTexture.Extensions);

            Assert.AreEqual(.89f, material.PbrMetallicRoughness.RoughnessFactor);
            Assert.AreEqual(0f, material.PbrMetallicRoughness.MetallicFactor);
#else
            Assert.Ignore("Texture export is disabled! " + LogMessages.GetFullMessage(LogCode.ImageConversionNotEnabled));
#endif
        }

        protected void MetallicTest(RenderPipeline renderPipeline)
        {
            var material = ConvertMaterial("Metallic", out _, renderPipeline);
            Assert.IsNotNull(material.PbrMetallicRoughness);
            Assert.AreEqual(.89f, material.PbrMetallicRoughness.MetallicFactor);
        }

        protected void MetallicTextureTest(RenderPipeline renderPipeline)
        {
#if UNITY_IMAGECONVERSION
            var material = ConvertMaterial("MetallicTexture", out var gltfWriter, renderPipeline);

            Assert.AreEqual(1, gltfWriter.imageExports.Count);
            Assert.AreEqual(0, gltfWriter.samplers.Count);
            Assert.AreEqual(1, gltfWriter.textures.Count);
            Assert.IsInstanceOf<ImageExport>(gltfWriter.imageExports[0]);

            Assert.IsNotNull(material.PbrMetallicRoughness);
            Assert.AreEqual(1f, material.PbrMetallicRoughness.MetallicFactor);

            var mrTexture = material.PbrMetallicRoughness?.MetallicRoughnessTexture;
            Assert.NotNull(mrTexture);

            var transform = mrTexture.Extensions?.TextureTransform;
            Assert.IsNotNull(transform);
            Assert.AreEqual(new[] { 2f, 2f }, transform.Scale);
#else
            Assert.Ignore("Texture export is disabled! " + LogMessages.GetFullMessage(LogCode.ImageConversionNotEnabled));
#endif
        }

        protected void MetallicRoughnessTextureTest(RenderPipeline renderPipeline)
        {
#if UNITY_IMAGECONVERSION
            var material = ConvertMaterial("MetallicRoughnessTexture", out var gltfWriter, renderPipeline);

            Assert.AreEqual(1, gltfWriter.imageExports.Count);
            Assert.AreEqual(0, gltfWriter.samplers.Count);
            Assert.AreEqual(1, gltfWriter.textures.Count);
            Assert.IsInstanceOf<ImageExport>(gltfWriter.imageExports[0]);

            Assert.IsNotNull(material.PbrMetallicRoughness);
            Assert.AreEqual(1f, material.PbrMetallicRoughness.MetallicFactor);
            Assert.AreEqual(1f, material.PbrMetallicRoughness.RoughnessFactor);

            var mrTexture = material.PbrMetallicRoughness?.MetallicRoughnessTexture;
            Assert.NotNull(mrTexture);
#else
            Assert.Ignore("Texture export is disabled! " + LogMessages.GetFullMessage(LogCode.ImageConversionNotEnabled));
#endif
        }

        protected void MetallicRoughnessOcclusionTextureTest(RenderPipeline renderPipeline)
        {
#if UNITY_IMAGECONVERSION
            var material = ConvertMaterial("MetallicRoughnessOcclusionTexture", out var gltfWriter, renderPipeline);

            Assert.AreEqual(1, gltfWriter.imageExports.Count);
            Assert.AreEqual(0, gltfWriter.samplers.Count);
            Assert.AreEqual(1, gltfWriter.textures.Count);
            Assert.IsInstanceOf<ImageExport>(gltfWriter.imageExports[0]);

            var mrTexture = material.PbrMetallicRoughness?.MetallicRoughnessTexture;
            Assert.NotNull(mrTexture);

            var oTexture = material.OcclusionTexture;
            Assert.NotNull(oTexture);
            Assert.AreEqual(.8f, oTexture.Strength);
#else
            Assert.Ignore("Texture export is disabled! " + LogMessages.GetFullMessage(LogCode.ImageConversionNotEnabled));
#endif
        }

        protected void OcclusionTextureTest(RenderPipeline renderPipeline)
        {
#if UNITY_IMAGECONVERSION
            var material = ConvertMaterial("OcclusionTexture", out var gltfWriter, renderPipeline);

            Assert.AreEqual(1, gltfWriter.imageExports.Count);
            Assert.AreEqual(0, gltfWriter.samplers.Count);
            Assert.AreEqual(1, gltfWriter.textures.Count);
            Assert.IsInstanceOf<ImageExport>(gltfWriter.imageExports[0]);

            Assert.IsNotNull(material.PbrMetallicRoughness);
            Assert.AreEqual(0f, material.PbrMetallicRoughness.MetallicFactor);

            var oTexture = material.OcclusionTexture;
            Assert.NotNull(oTexture);
            Assert.AreEqual(.8f, oTexture.Strength);

            var transform = oTexture.Extensions?.TextureTransform;
            Assert.IsNotNull(transform);
            Assert.AreEqual(new[] { 2f, 2f }, transform.Scale);
#else
            Assert.Ignore("Texture export is disabled! " + LogMessages.GetFullMessage(LogCode.ImageConversionNotEnabled));
#endif
        }

        protected void EmissiveFactorTest(RenderPipeline renderPipeline)
        {
            var material = ConvertMaterial("EmissiveFactor", out _, renderPipeline);

            Assert.AreEqual(new Color(1, 1, 0), (Color)material.EmissiveFactor);
            Assert.IsNull(material.EmissiveTexture);
        }

        protected void EmissiveTextureTest(RenderPipeline renderPipeline)
        {
#if UNITY_IMAGECONVERSION
            var material = ConvertMaterial("EmissiveTexture", out var gltfWriter, renderPipeline);

            Assert.AreEqual(1, gltfWriter.imageExports.Count);
            Assert.AreEqual(0, gltfWriter.samplers.Count);
            Assert.AreEqual(1, gltfWriter.textures.Count);
            Assert.IsInstanceOf<ImageExport>(gltfWriter.imageExports[0]);

            Assert.AreEqual(Color.white, (Color)material.EmissiveFactor);

            var texture = material.EmissiveTexture;
            Assert.NotNull(texture);

            Assert.IsNull(texture.Extensions?.TextureTransform);
#else
            Assert.Ignore("Texture export is disabled! " + LogMessages.GetFullMessage(LogCode.ImageConversionNotEnabled));
#endif
        }

        protected void EmissiveTextureFactorTest(RenderPipeline renderPipeline)
        {
#if UNITY_IMAGECONVERSION
            var material = ConvertMaterial("EmissiveTextureFactor", out var gltfWriter, renderPipeline);

            Assert.AreEqual(1, gltfWriter.imageExports.Count);
            Assert.AreEqual(0, gltfWriter.samplers.Count);
            Assert.AreEqual(1, gltfWriter.textures.Count);
            Assert.IsInstanceOf<ImageExport>(gltfWriter.imageExports[0]);

            Assert.AreEqual(new Color(1, .7353569f, 0), (Color)material.EmissiveFactor);

            var texture = material.EmissiveTexture;
            Assert.NotNull(texture);

            var transform = texture.Extensions?.TextureTransform;
            Assert.IsNotNull(transform);
            Assert.AreEqual(new[] { 2f, 3f }, transform.Scale);
#else
            Assert.Ignore("Texture export is disabled! " + LogMessages.GetFullMessage(LogCode.ImageConversionNotEnabled));
#endif
        }

        protected void NormalTextureTest(RenderPipeline renderPipeline)
        {
#if UNITY_IMAGECONVERSION
            var material = ConvertMaterial("NormalTexture", out var gltfWriter, renderPipeline);

            Assert.AreEqual(1, gltfWriter.imageExports.Count);
            Assert.AreEqual(0, gltfWriter.samplers.Count);
            Assert.AreEqual(1, gltfWriter.textures.Count);
            Assert.IsInstanceOf<ImageExport>(gltfWriter.imageExports[0]);

            Assert.IsNotNull(material.PbrMetallicRoughness);

            var texture = material.NormalTexture;
            Assert.NotNull(texture);
            Assert.AreEqual(1.1f, texture.Scale);

            var transform = texture.Extensions?.TextureTransform;
            Assert.IsNotNull(transform);
            Assert.AreEqual(new[] { 1.5f, 1.2f }, transform.Scale);
            Assert.AreEqual(new[] { 0f, 0f }, transform.Offset);
            Assert.AreEqual(0, transform.Rotation);
#else
            Assert.Ignore("Texture export is disabled! " + LogMessages.GetFullMessage(LogCode.ImageConversionNotEnabled));
#endif
        }

        protected void NotGltfTest(RenderPipeline renderPipeline)
        {
            var material = ConvertMaterial("NotGltf", out _, renderPipeline);
            Assert.IsNotNull(material);
        }

        protected void OmniTest(RenderPipeline renderPipeline)
        {
#if UNITY_IMAGECONVERSION
            var material = ConvertMaterial("Omni", out var gltfWriter, renderPipeline);

            Assert.AreEqual(4, gltfWriter.imageExports.Count);
            Assert.AreEqual(0, gltfWriter.samplers.Count);
            Assert.AreEqual(4, gltfWriter.textures.Count);
            Assert.IsInstanceOf<ImageExport>(gltfWriter.imageExports[0]);

            Assert.IsNotNull(material.PbrMetallicRoughness);
            Assert.That(new Color(0.787412345f, 0.603827417f, 0.447988421f, 1),
                Is.EqualTo((Color)material.PbrMetallicRoughness.BaseColorFactor).Using(s_ColorComparer));
            var baseColorTexture = material.PbrMetallicRoughness.BaseColorTexture;
            Assert.IsNotNull(baseColorTexture);
            Assert.AreEqual(0, baseColorTexture.Index);
            Assert.AreEqual(0, baseColorTexture.TexCoord);

            Assert.AreEqual(1, gltfWriter.extensions.Count);

            var baseColorTransform = baseColorTexture.Extensions?.TextureTransform;
            Assert.IsNotNull(baseColorTransform);
            Assert.AreEqual(new[] { 0.4f, 0.6f }, baseColorTransform.Offset);
            Assert.AreEqual(new[] { 1f, 1f }, baseColorTransform.Scale);
            Assert.AreEqual(0, baseColorTransform.Rotation);

            var mrTexture = material.PbrMetallicRoughness?.MetallicRoughnessTexture;
            Assert.NotNull(mrTexture);

            var mrTransform = mrTexture.Extensions?.TextureTransform;
            Assert.IsNotNull(mrTransform);
            Assert.AreEqual(new[] { 1.11f, 1.21f }, mrTransform.Scale);

            var normalTexture = material.NormalTexture;
            Assert.NotNull(normalTexture);
            Assert.AreEqual(0.42f, normalTexture.Scale);

            var normalTransform = normalTexture.Extensions?.TextureTransform;
            Assert.IsNotNull(normalTransform);
            Assert.AreEqual(new[] { 1.5f, 1.2f }, normalTransform.Scale);
            Assert.AreEqual(new[] { 0f, 0f }, normalTransform.Offset);
            Assert.AreEqual(0, normalTransform.Rotation);

            var oTexture = material.OcclusionTexture;
            Assert.NotNull(oTexture);
            Assert.AreEqual(.58f, oTexture.Strength);

            var oTransform = oTexture.Extensions?.TextureTransform;
            Assert.IsNotNull(oTransform);
            Assert.AreEqual(new[] { 1.1f, 1.2f }, oTransform.Scale);
#else
            Assert.Ignore("Texture export is disabled! " + LogMessages.GetFullMessage(LogCode.ImageConversionNotEnabled));
#endif
        }

        protected void AddImageFailTest(RenderPipeline renderPipeline)
        {
            const string name = "Omni";
            var gltfWriter = new GltfWritableMock(false);
            var uMaterial = Resources.Load<Material>($"{GetResourcePath(renderPipeline)}{name}");
            Assert.IsNotNull(uMaterial);
            var logger = new CollectingLogger();
            m_Exporter.ConvertMaterial(uMaterial, out var material, gltfWriter, logger);
            Assert.IsNotNull(material);
            LoggerTest.AssertLogger(logger);
        }

        protected void DoubleSidedTest(RenderPipeline renderPipeline)
        {
            var material = ConvertMaterial("DoubleSided", out _, renderPipeline);
            Assert.AreEqual(true, material.DoubleSided);
        }

        TextureInfo BaseColorTextureTest(
            string name,
            out GltfWritableMock gltfWriter,
            RenderPipeline renderPipeline,
            out Schema.Material material
            )
        {
            material = ConvertMaterial(name, out gltfWriter, renderPipeline);

            Assert.AreEqual(1, gltfWriter.imageExports.Count);
            Assert.AreEqual(0, gltfWriter.samplers.Count);
            Assert.AreEqual(1, gltfWriter.textures.Count);
            Assert.IsInstanceOf<ImageExport>(gltfWriter.imageExports[0]);

            Assert.IsNotNull(material.PbrMetallicRoughness);
            Assert.That(new Color(0.787412345f, 0.603827417f, 0.447988421f, 1),
                Is.EqualTo((Color)material.PbrMetallicRoughness.BaseColorFactor).Using(s_ColorComparer));
            var texture = material.PbrMetallicRoughness.BaseColorTexture;
            Assert.IsNotNull(texture);
            Assert.AreEqual(0, texture.Index);
            Assert.AreEqual(0, texture.TexCoord);
            return texture;
        }

        [OneTimeSetUp]
        public void SetUp()
        {
            SetUpExporter();
        }

        protected abstract void SetUpExporter();

        Schema.Material ConvertMaterial(
            string name,
            out GltfWritableMock gltfWriter,
            RenderPipeline renderPipeline
            )
        {
            gltfWriter = new GltfWritableMock();
            var uMaterial = Resources.Load<Material>($"{GetResourcePath(renderPipeline)}{name}");
            Assert.IsNotNull(uMaterial);
            var logger = new CollectingLogger();
            m_Exporter.ConvertMaterial(uMaterial, out var material, gltfWriter, logger);
            Assert.IsNotNull(material);
            LoggerTest.AssertLogger(logger);
            return material;
        }

        static string GetResourcePath(RenderPipeline renderPipeline)
        {
            switch (renderPipeline)
            {
                case RenderPipeline.BuiltIn:
                    return $"{k_ResourcePath}Built-In/";
                case RenderPipeline.Universal:
                    return $"{k_ResourcePath}URP/";
                case RenderPipeline.HighDefinition:
                    return $"{k_ResourcePath}HDRP/";
                case RenderPipeline.Unknown:
                default:
                    throw new ArgumentOutOfRangeException(nameof(renderPipeline), renderPipeline, null);
            }
        }
    }
}
