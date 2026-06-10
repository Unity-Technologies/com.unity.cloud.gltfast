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
        public void AccessorTypeDeserialization()
        {
            Assert.AreEqual(AccessorType.Scalar,
                JsonSerializer.Deserialize(@"""SCALAR""", GltfRootSourceGenerator.Default.AccessorType));

            Assert.AreEqual(AccessorType.Vector2,
                JsonSerializer.Deserialize(@"""VEC2""", GltfRootSourceGenerator.Default.AccessorType));

            Assert.AreEqual(AccessorType.Vector3,
                JsonSerializer.Deserialize(@"""VEC3""", GltfRootSourceGenerator.Default.AccessorType));

            Assert.AreEqual(AccessorType.Vector4,
                JsonSerializer.Deserialize(@"""VEC4""", GltfRootSourceGenerator.Default.AccessorType));

            Assert.AreEqual(AccessorType.Matrix2x2,
                JsonSerializer.Deserialize(@"""MAT2""", GltfRootSourceGenerator.Default.AccessorType));

            Assert.AreEqual(AccessorType.Matrix3x3,
                JsonSerializer.Deserialize(@"""MAT3""", GltfRootSourceGenerator.Default.AccessorType));

            Assert.AreEqual(AccessorType.Matrix4x4,
                JsonSerializer.Deserialize(@"""MAT4""", GltfRootSourceGenerator.Default.AccessorType));

            Assert.Throws<JsonException>(() =>
                JsonSerializer.Deserialize(@"""foo""", GltfRootSourceGenerator.Default.AccessorType));
        }

        [Test]
        public void AnimationPathDeserialization()
        {
#if UNITY_ANIMATION
            var target = JsonSerializer.Deserialize(
                "{\"path\":\"pointer\"}", GltfRootSourceGenerator.Default.AnimationChannelTarget);
            Assert.AreEqual(AnimationPath.Pointer, target.Path);

            target = JsonSerializer.Deserialize(
                "{\"path\":\"translation\"}", GltfRootSourceGenerator.Default.AnimationChannelTarget);
            Assert.AreEqual(AnimationPath.Translation, target.Path);

            target = JsonSerializer.Deserialize(
                "{\"path\":\"rotation\"}", GltfRootSourceGenerator.Default.AnimationChannelTarget);
            Assert.AreEqual(AnimationPath.Rotation, target.Path);

            target = JsonSerializer.Deserialize(
                "{\"path\":\"scale\"}", GltfRootSourceGenerator.Default.AnimationChannelTarget);
            Assert.AreEqual(AnimationPath.Scale, target.Path);

            target = JsonSerializer.Deserialize(
                "{\"path\":\"weights\"}", GltfRootSourceGenerator.Default.AnimationChannelTarget);
            Assert.AreEqual(AnimationPath.Weights, target.Path);

            Assert.Throws<JsonException>(() =>
                JsonSerializer.Deserialize("{\"path\":\"foo\"}", GltfRootSourceGenerator.Default.AnimationChannelTarget));
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
                JsonSerializer.Deserialize(json, GltfRootSourceGenerator.Default.AnimationSampler).Interpolation);
#else
            Assert.Ignore("Requires Animation module to be enabled.");
#endif
        }

        [Test]
        public void InterpolationDeserializationException()
        {
#if UNITY_ANIMATION
            Assert.Throws<JsonException>(() =>
                JsonSerializer.Deserialize(@"""foo""", GltfRootSourceGenerator.Default.AnimationSampler));
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
                Orthographic = new CameraOrthographic()
            };
            var json = JsonSerializer.Serialize(obj, GltfRootSourceGenerator.Default.Camera);
            Assert.AreEqual("{\"orthographic\":{},\"type\":\"orthographic\"}", json);

            obj = new Camera();
            json = JsonSerializer.Serialize(obj, GltfRootSourceGenerator.Default.Camera);
            Assert.AreEqual("{}", json);

            obj = new Camera
            {
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
        public void LightTypeDeserialization()
        {
            var light = JsonSerializer.Deserialize(
                "{\"type\":\"spot\"}", GltfRootSourceGenerator.Default.LightPunctual);
            Assert.AreEqual(LightType.Spot, light.Type);

            light = JsonSerializer.Deserialize(
                "{\"type\":\"directional\"}", GltfRootSourceGenerator.Default.LightPunctual);
            Assert.AreEqual(LightType.Directional, light.Type);

            light = JsonSerializer.Deserialize(
                "{\"type\":\"point\"}", GltfRootSourceGenerator.Default.LightPunctual);
            Assert.AreEqual(LightType.Point, light.Type);

            Assert.Throws<JsonException>(() =>
                JsonSerializer.Deserialize("{\"type\":\"foo\"}", GltfRootSourceGenerator.Default.LightPunctual));
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
            Assert.AreEqual(AnimationPath.Weights, gltf.Animations[0].Channels[0].Target.Path);
            Assert.NotNull(gltf.Animations[0].Samplers);
            Assert.AreEqual(1, gltf.Animations[0].Samplers.Count);
            Assert.AreEqual(Interpolation.CubicSpline, gltf.Animations[0].Samplers[0].Interpolation);
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
            Assert.AreEqual(1, gltf.Extensions.LightsPunctual.Lights.Length);
            Assert.AreEqual(LightType.Directional, gltf.Extensions.LightsPunctual.Lights[0].Type);
        }

        static void CheckResultMaterials(Root gltf)
        {
            Assert.NotNull(gltf);
            Assert.NotNull(gltf.Materials);
            Assert.AreEqual(1, gltf.Materials.Count);
            Assert.AreEqual(AlphaMode.Mask, gltf.Materials[0].AlphaMode);
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
            Assert.AreEqual(Sampler.MagFilterMode.Nearest, gltf.Samplers[0].MagFilter);
            Assert.AreEqual(Sampler.WrapMode.MirroredRepeat, gltf.Samplers[0].WrapS);
            Assert.AreEqual(Sampler.WrapMode.Undefined, gltf.Samplers[0].WrapT);
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
