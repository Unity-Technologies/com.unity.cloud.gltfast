// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using GLTFast.Schema;
#if MESHOPT_IS_RECENT
using Meshoptimizer;
#endif
using NUnit.Framework;
using Unity.Gltfast.Text.Json;
using UnityEngine;
using Camera = GLTFast.Schema.Camera;
using CameraType = GLTFast.Schema.CameraType;
using LightType = GLTFast.Schema.LightType;
using Material = GLTFast.Schema.Material;
using WrapMode = GLTFast.Schema.WrapMode;

namespace GLTFast.Tests.JsonParsing
{
    [TestFixture]
    [Category("JsonParsing")]
    class EnumTypeTests
    {
        Root m_Gltf;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            m_Gltf = JsonSerializer.Deserialize(k_EnumTypesJson, GltfRootSourceGenerator.Default.Root);
        }

        [Test]
        public void Accessor()
        {
            CheckResultAccessor(m_Gltf);
        }

        [Test]
        public void Animation()
        {
#if UNITY_ANIMATION
            CheckResultAnimation(m_Gltf);
#else
            Assert.Ignore("Requires Animation module to be enabled.");
#endif
        }

        [Test]
        public void Camera()
        {
            CheckResultCamera(m_Gltf);
        }

        [Test]
        public void Meshopt()
        {
#if MESHOPT_IS_RECENT
            CheckResultMeshopt(m_Gltf);
#else
            Assert.Ignore("Requires meshoptimizer decompression for Unity package to be installed.");
#endif
        }

        [Test]
        public void RootExtensions()
        {
            CheckResultRootExtensions(m_Gltf);
        }

        [Test]
        public void Materials()
        {
            CheckResultMaterials(m_Gltf);
        }

        [Test]
        public void Samplers()
        {
            CheckResultSamplers(m_Gltf);
        }

        [Test]
        [TestCase(AccessorType.Scalar, "SCALAR")]
        [TestCase(AccessorType.Vector2, "VEC2")]
        [TestCase(AccessorType.Vector3, "VEC3")]
        [TestCase(AccessorType.Vector4, "VEC4")]
        [TestCase(AccessorType.Matrix2x2, "MAT2")]
        [TestCase(AccessorType.Matrix3x3, "MAT3")]
        [TestCase(AccessorType.Matrix4x4, "MAT4")]
        public void AccessorTypeDeserialization(AccessorType expected, string value)
        {
            var accessor = JsonSerializer.Deserialize($@"{{""type"":""{value}""}}", GltfRootSourceGenerator.Default.Accessor);
            Assert.AreEqual(expected, accessor.Type.Value);
            Assert.IsNull(accessor.Type.RawValue);
        }

        [Test]
        public void AccessorTypeUnknownDeserialization()
        {
            var accessor = JsonSerializer.Deserialize(@"{""type"":""Unknown\u00B9Type""}", GltfRootSourceGenerator.Default.Accessor);
            Assert.AreEqual(AccessorType.Undefined, accessor.Type.Value);
            Assert.AreEqual(System.Text.Encoding.UTF8.GetBytes("Unknown¹Type"), accessor.Type.RawValue);

            Assert.Throws<JsonException>(() =>
                JsonSerializer.Deserialize(@"{""type"":42}", GltfRootSourceGenerator.Default.Accessor));
        }

        [Test]
        [TestCase(AccessorType.Scalar, "SCALAR")]
        [TestCase(AccessorType.Vector2, "VEC2")]
        [TestCase(AccessorType.Vector3, "VEC3")]
        [TestCase(AccessorType.Vector4, "VEC4")]
        [TestCase(AccessorType.Matrix2x2, "MAT2")]
        [TestCase(AccessorType.Matrix3x3, "MAT3")]
        [TestCase(AccessorType.Matrix4x4, "MAT4")]
        public void AccessorTypeSerialization(AccessorType value, string expected)
        {
            var json = JsonSerializer.Serialize(value, GltfRootSourceGenerator.Default.AccessorType);
            Assert.AreEqual($@"""{expected}""", json);

            var accessor = new Accessor { Type = new EnumOrRawValue<AccessorType>(value) };
            json = JsonSerializer.Serialize(accessor, GltfRootSourceGenerator.Default.Accessor);
            Assert.AreEqual($@"{{""type"":""{expected}""}}", json);
        }

        [Test]
        public void AccessorTypeUnknownSerialization()
        {
            const string type = "UnknownType";
            var typeBytes = System.Text.Encoding.UTF8.GetBytes(type);
            var accessor = new Accessor { Type = new EnumOrRawValue<AccessorType>(typeBytes) };
            var json = JsonSerializer.Serialize(accessor, GltfRootSourceGenerator.Default.Accessor);
            Assert.AreEqual($@"{{""type"":""UnknownType""}}", json);
        }

#if UNITY_ANIMATION
        [Test]
        [TestCase(AnimationPath.Pointer, "pointer")]
        [TestCase(AnimationPath.Translation, "translation")]
        [TestCase(AnimationPath.Rotation, "rotation")]
        [TestCase(AnimationPath.Scale, "scale")]
        [TestCase(AnimationPath.Weights, "weights")]
        public void AnimationPathDeserialization(AnimationPath expected, string value)
        {
            var target = JsonSerializer.Deserialize(
                $"{{\"path\":\"{value}\"}}", GltfRootSourceGenerator.Default.AnimationChannelTarget);
            Assert.AreEqual(expected, target.Path.Value);
            Assert.IsNull(target.Path.RawValue);
        }
#endif

        [Test]
        public void AnimationPathUnknownDeserialization()
        {
#if UNITY_ANIMATION
            var target = JsonSerializer.Deserialize(
                "{\"path\":\"Unknown\\u00B9Path\"}", GltfRootSourceGenerator.Default.AnimationChannelTarget);
            Assert.AreEqual(AnimationPath.Undefined, target.Path.Value);
            Assert.AreEqual(System.Text.Encoding.UTF8.GetBytes("Unknown¹Path"), target.Path.RawValue);
#else
            Assert.Ignore("Requires Animation module to be enabled.");
#endif
        }

        [Test]
        [TestCase(null, Interpolation.Linear)]
        [TestCase("LINEAR", Interpolation.Linear)]
        [TestCase("STEP", Interpolation.Step)]
        [TestCase("CUBICSPLINE", Interpolation.CubicSpline)]
        public void InterpolationDeserialization(string value, Interpolation expected)
        {
#if UNITY_ANIMATION
            var json = value == null ? "{}" : $@"{{""interpolation"":""{value}""}}";
            Assert.AreEqual(expected,
                JsonSerializer.Deserialize(json, GltfRootSourceGenerator.Default.AnimationSampler).Interpolation.Value);
#else
            Assert.Ignore("Requires Animation module to be enabled.");
#endif
        }

        [Test]
        public void InterpolationUnknownDeserialization()
        {
#if UNITY_ANIMATION
            var sampler = JsonSerializer.Deserialize(
                @"{""interpolation"":""Unknown¹Interpolation""}",
                GltfRootSourceGenerator.Default.AnimationSampler);
            Assert.AreEqual(Interpolation.Linear, sampler.Interpolation.Value);
            Assert.AreEqual(System.Text.Encoding.UTF8.GetBytes("Unknown¹Interpolation"), sampler.Interpolation.RawValue);
#else
            Assert.Ignore("Requires Animation module to be enabled.");
#endif
        }

        [Test]
        [TestCase(MeshoptMode.Attributes, "ATTRIBUTES")]
        [TestCase(MeshoptMode.Triangles, "TRIANGLES")]
        [TestCase(MeshoptMode.Indices, "INDICES")]
        [TestCase(MeshoptMode.Undefined, "foo")]
        public void MeshoptModeDeserialization(MeshoptMode expected, string value)
        {
#if MESHOPT_IS_RECENT
            Assert.AreEqual(expected, JsonSerializer.Deserialize($@"{{""mode"":""{value}""}}",
                GltfRootSourceGenerator.Default.BufferViewMeshoptExtension).Mode);
#else
            Assert.Ignore("Requires meshoptimizer decompression for Unity package to be installed.");
#endif
        }

        [Test]
        [TestCase(@"{""mode"":""ATTRIBUTES""}", MeshoptMode.Attributes)]
        [TestCase(@"{""mode"":""TRIANGLES""}", MeshoptMode.Triangles)]
        [TestCase(@"{""mode"":""INDICES""}", MeshoptMode.Indices)]
        [TestCase("{}", MeshoptMode.Undefined)]
        public void MeshoptModeSerialization(string expected, MeshoptMode mode)
        {
#if MESHOPT_IS_RECENT
            Assert.AreEqual(expected,
                JsonSerializer.Serialize(new BufferViewMeshoptExtension { Mode = mode },
                    GltfRootSourceGenerator.Default.BufferViewMeshoptExtension));
#else
            Assert.Ignore("Requires meshoptimizer decompression for Unity package to be installed.");
#endif
        }

        [Test]
        [TestCase(MeshoptFilter.Octahedral, "OCTAHEDRAL")]
        [TestCase(MeshoptFilter.Quaternion, "QUATERNION")]
        [TestCase(MeshoptFilter.Exponential, "EXPONENTIAL")]
        [TestCase(MeshoptFilter.None, "foo")]
        public void MeshoptFilterDeserialization(MeshoptFilter expected, string value)
        {
#if MESHOPT_IS_RECENT
            Assert.AreEqual(expected, JsonSerializer.Deserialize($@"{{""filter"":""{value}""}}",
                GltfRootSourceGenerator.Default.BufferViewMeshoptExtension).Filter);
#else
            Assert.Ignore("Requires meshoptimizer decompression for Unity package to be installed.");
#endif
        }

        [Test]
        [TestCase(@"{""filter"":""OCTAHEDRAL""}", MeshoptFilter.Octahedral)]
        [TestCase(@"{""filter"":""QUATERNION""}", MeshoptFilter.Quaternion)]
        [TestCase(@"{""filter"":""EXPONENTIAL""}", MeshoptFilter.Exponential)]
        [TestCase("{}", MeshoptFilter.None)]
        public void MeshoptFilterSerialization(string expected, MeshoptFilter value)
        {
#if MESHOPT_IS_RECENT
            Assert.AreEqual(expected,
                JsonSerializer.Serialize(new BufferViewMeshoptExtension { Filter = value },
                    GltfRootSourceGenerator.Default.BufferViewMeshoptExtension));
#else
            Assert.Ignore("Requires meshoptimizer decompression for Unity package to be installed.");
#endif
        }

        [Test]
        public void CameraTypeSerialization()
        {
            var obj = new Camera
            {
                Type = CameraType.Orthographic,
                Orthographic = new CameraOrthographic()
            };
            var json = JsonSerializer.Serialize(obj, GltfRootSourceGenerator.Default.Camera);
            Assert.AreEqual("{\"orthographic\":{},\"type\":\"orthographic\"}", json);

            obj = new Camera();
            json = JsonSerializer.Serialize(obj, GltfRootSourceGenerator.Default.Camera);
            Assert.AreEqual("{}", json);

            obj = new Camera
            {
                Type = CameraType.Perspective,
                Perspective = new CameraPerspective()
            };
            json = JsonSerializer.Serialize(obj, GltfRootSourceGenerator.Default.Camera);
            Assert.AreEqual("{\"perspective\":{},\"type\":\"perspective\"}", json);
        }

        [Test]
        [TestCase(null, PrimitiveMode.Triangles)]
        [TestCase(0, PrimitiveMode.Points)]
        [TestCase(1, PrimitiveMode.Lines)]
        [TestCase(2, PrimitiveMode.LineLoop)]
        [TestCase(3, PrimitiveMode.LineStrip)]
        [TestCase(4, PrimitiveMode.Triangles)]
        [TestCase(5, PrimitiveMode.TriangleStrip)]
        [TestCase(6, PrimitiveMode.TriangleFan)]
        [TestCase(666, (PrimitiveMode)666)]
        [TestCase(-1, (PrimitiveMode)(-1))]
        public void PrimitiveModeDeserialization(int? value, PrimitiveMode? expected)
        {
            var json = value == null ? "{}" : $@"{{""mode"":{value}}}";
            var primitive = JsonSerializer.Deserialize(json, GltfRootSourceGenerator.Default.MeshPrimitive);
            Assert.AreEqual(expected, primitive.Mode);
        }

        [Test]
        [TestCase(LightType.Spot, "spot")]
        [TestCase(LightType.Directional, "directional")]
        [TestCase(LightType.Point, "point")]
        public void LightTypeDeserialization(LightType expected, string value)
        {
            var light = JsonSerializer.Deserialize(
                $"{{\"type\":\"{value}\"}}", GltfRootSourceGenerator.Default.LightPunctual);
            Assert.AreEqual(expected, light.Type.Value);
            Assert.IsNull(light.Type.RawValue);
        }

        [Test]
        public void LightTypeUnknownDeserialization()
        {
            var light = JsonSerializer.Deserialize(
                "{\"type\":\"Unknown\\u00B9Light\"}", GltfRootSourceGenerator.Default.LightPunctual);
            Assert.AreEqual(LightType.Undefined, light.Type.Value);
            Assert.AreEqual(System.Text.Encoding.UTF8.GetBytes("Unknown¹Light"), light.Type.RawValue);
        }

        [Test]
        [TestCase(ImageMimeType.Jpeg, "image/jpeg")]
        [TestCase(ImageMimeType.Png, "image/png")]
        [TestCase(ImageMimeType.Ktx2, "image/ktx2")]
        [TestCase(ImageMimeType.WebP, "image/webp")]
        public void ImageMimeTypeDeserialization(ImageMimeType expected, string value)
        {
            var image = JsonSerializer.Deserialize(
                $@"{{""mimeType"":""{value}""}}", GltfRootSourceGenerator.Default.Image);
            Assert.AreEqual(expected, image.MimeType.Value);
            Assert.IsNull(image.MimeType.RawValue);
        }

        [Test]
        public void ImageMimeTypeUnknownDeserialization()
        {
            var image = JsonSerializer.Deserialize(
                @"{""mimeType"":""image/avif""}", GltfRootSourceGenerator.Default.Image);
            Assert.AreEqual(ImageMimeType.Undefined, image.MimeType.Value);
            Assert.AreEqual(System.Text.Encoding.UTF8.GetBytes("image/avif"), image.MimeType.RawValue);
        }

        [Test]
        public void ImageMimeTypeLegacyKtxDeserialization()
        {
            var image = JsonSerializer.Deserialize(
                @"{""mimeType"":""image/ktx""}", GltfRootSourceGenerator.Default.Image);
            Assert.AreEqual(ImageMimeType.Undefined, image.MimeType.Value);
            Assert.AreEqual(System.Text.Encoding.UTF8.GetBytes("image/ktx"), image.MimeType.RawValue);
            Assert.AreEqual(ImageFormat.Unknown, ImageFormatExtensions.FromMimeType(image.MimeType));
        }

        [Test]
        public void ImageMimeTypeAbsentDeserialization()
        {
            var image = JsonSerializer.Deserialize("{}", GltfRootSourceGenerator.Default.Image);
            Assert.AreEqual(default(EnumOrRawValue<ImageMimeType>), image.MimeType);
            Assert.AreEqual(ImageMimeType.Undefined, image.MimeType.Value);
            Assert.IsNull(image.MimeType.RawValue);

            var json = JsonSerializer.Serialize(image, GltfRootSourceGenerator.Default.Image);
            Assert.AreEqual("{}", json);
        }

        [Test]
        [TestCase(ImageMimeType.Jpeg, "image/jpeg")]
        [TestCase(ImageMimeType.Png, "image/png")]
        [TestCase(ImageMimeType.Ktx2, "image/ktx2")]
        [TestCase(ImageMimeType.WebP, "image/webp")]
        public void ImageMimeTypeSerialization(ImageMimeType value, string expected)
        {
            var image = new Image { MimeType = value };
            var json = JsonSerializer.Serialize(image, GltfRootSourceGenerator.Default.Image);
            Assert.AreEqual($@"{{""mimeType"":""{expected}""}}", json);
        }

        [Test]
        public void ImageMimeTypeUnknownSerialization()
        {
            const string mime = "image/avif";
            var image = new Image
            {
                MimeType = new EnumOrRawValue<ImageMimeType>(System.Text.Encoding.UTF8.GetBytes(mime))
            };
            var json = JsonSerializer.Serialize(image, GltfRootSourceGenerator.Default.Image);
            Assert.AreEqual($@"{{""mimeType"":""{mime}""}}", json);
        }

        [Test]
        [TestCase("image/jpeg")]
        [TestCase("image/png")]
        [TestCase("image/ktx2")]
        [TestCase("image/webp")]
        [TestCase("image/avif")]
        [TestCase("image/ktx")]
        public void ImageMimeTypeRoundtrip(string mime)
        {
            var image = JsonSerializer.Deserialize(
                $@"{{""mimeType"":""{mime}""}}", GltfRootSourceGenerator.Default.Image);
            var json = JsonSerializer.Serialize(image, GltfRootSourceGenerator.Default.Image);
            Assert.AreEqual($@"{{""mimeType"":""{mime}""}}", json);
        }

        [Test]
        public void AccessorDataTypeDeserialization()
        {
            // Known Value
            Assert.AreEqual(
                AccessorDataType.Byte,
                JsonSerializer.Deserialize(
                    "5120",
                    GltfRootSourceGenerator.Default.AccessorDataType)
            );

            // Unknown Value
            Assert.AreEqual(
                (AccessorDataType)42,
                JsonSerializer.Deserialize(
                    "42",
                    GltfRootSourceGenerator.Default.AccessorDataType)
                );
        }

        static void CheckResultAccessor(Root gltf)
        {
            Assert.NotNull(gltf);
            Assert.NotNull(gltf.Accessors);
            Assert.AreEqual(1, gltf.Accessors.Count);
            Assert.AreEqual(AccessorDataType.UnsignedShort, gltf.Accessors[0].ComponentType);
            Assert.AreEqual(AccessorType.Matrix3x3, gltf.Accessors[0].Type);
        }

#if UNITY_ANIMATION
        static void CheckResultAnimation(Root gltf)
        {
            Assert.NotNull(gltf);
            Assert.NotNull(gltf.Animations);
            Assert.AreEqual(1, gltf.Animations.Count);
            Assert.NotNull(gltf.Animations[0].Channels);
            Assert.AreEqual(1, gltf.Animations[0].Channels.Count);
            Assert.NotNull(gltf.Animations[0].Channels[0].Target);
            Assert.AreEqual(AnimationPath.Weights, gltf.Animations[0].Channels[0].Target.Path.Value);
            Assert.NotNull(gltf.Animations[0].Samplers);
            Assert.AreEqual(1, gltf.Animations[0].Samplers.Count);
            Assert.AreEqual(Interpolation.CubicSpline, gltf.Animations[0].Samplers[0].Interpolation.Value);
        }
#endif

        static void CheckResultCamera(Root gltf)
        {
            Assert.NotNull(gltf);
            Assert.NotNull(gltf.Cameras);
            Assert.AreEqual(1, gltf.Cameras.Count);
            Assert.AreEqual(CameraType.Orthographic, gltf.Cameras[0].Type);
        }

#if MESHOPT_IS_RECENT
        static void CheckResultMeshopt(Root gltf)
        {
            Assert.NotNull(gltf);
            Assert.NotNull(gltf.BufferViews);
            Assert.AreEqual(1, gltf.BufferViews.Count);
            Assert.NotNull(gltf.BufferViews[0].Extensions?.ExtMeshoptCompression);
            Assert.AreEqual(MeshoptMode.Triangles, gltf.BufferViews[0].Extensions?.ExtMeshoptCompression.Mode);
            Assert.AreEqual(MeshoptFilter.Exponential, gltf.BufferViews[0].Extensions?.ExtMeshoptCompression.Filter);
        }
#endif
        static void CheckResultRootExtensions(Root gltf)
        {
            Assert.NotNull(gltf);
            Assert.NotNull(gltf.Extensions);
            Assert.NotNull(gltf.Extensions.LightsPunctual);
            Assert.NotNull(gltf.Extensions.LightsPunctual.Lights);
            Assert.AreEqual(1, gltf.Extensions.LightsPunctual.Lights.Count);
            Assert.AreEqual(LightType.Directional, gltf.Extensions.LightsPunctual.Lights[0].Type.Value);
        }

        static void CheckResultMaterials(Root gltf)
        {
            Assert.NotNull(gltf);
            Assert.NotNull(gltf.Materials);
            Assert.AreEqual(1, gltf.Materials.Count);
            Assert.AreEqual(AlphaMode.Mask, gltf.Materials[0].AlphaMode.Value);
            Assert.NotNull(gltf.Meshes);
            Assert.AreEqual(1, gltf.Meshes.Count);
            Assert.NotNull(gltf.Meshes[0].Primitives);
            Assert.AreEqual(1, gltf.Meshes[0].Primitives.Count);
            Assert.AreEqual(PrimitiveMode.LineStrip, gltf.Meshes[0].Primitives[0].Mode);
        }

        static void CheckResultSamplers(Root gltf)
        {
            Assert.NotNull(gltf);
            Assert.NotNull(gltf.Samplers);
            Assert.AreEqual(1, gltf.Samplers.Count);
            Assert.AreEqual(MagFilterMode.Nearest, gltf.Samplers[0].MagFilter);
            Assert.AreEqual(WrapMode.MirroredRepeat, gltf.Samplers[0].WrapS);
            Assert.AreEqual(WrapMode.Undefined, gltf.Samplers[0].WrapT);
        }

        const string k_EnumTypesJson = @"
{
    ""accessors"": [{
        ""type"": ""MAT3"",
        ""componentType"": 5123
    }],
    ""animations"": [{
        ""channels"": [{
            ""target"": {
                ""path"": ""weights""
            }
        }],
        ""samplers"": [{
            ""interpolation"": ""CUBICSPLINE""
        }]
    }],
    ""bufferViews"": [{
        ""extensions"": {
            ""EXT_meshopt_compression"": {
                ""mode"": ""TRIANGLES"",
                ""filter"": ""EXPONENTIAL""
            }
        }
    }],
    ""cameras"": [{
        ""type"": ""orthographic"",
        ""orthographic"": {}
    }],
    ""extensions"": {
        ""KHR_lights_punctual"": {
            ""lights"":[{
                ""type"": ""directional""
            }]
        }
    },
    ""materials"": [{
        ""alphaMode"": ""MASK""
    }],
    ""meshes"": [{
        ""primitives"": [{
            ""mode"": 3
        }]
    }],
    ""samplers"": [{
        ""magFilter"": 9728,
        ""minFilter"": 9984,
        ""wrapS"": 33648,
        ""wrapT"": 0
    }]
}";
    }
}
