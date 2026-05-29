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
        public void AccessorExtended()
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
        public void AnimationExtended()
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
        public void CameraExtended()
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
        public void MeshoptExtended()
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
        public void RootExtensionsExtended()
        {
            CheckResultRootExtensions(m_Gltf);
        }

        [Test]
        public void Materials()
        {
            CheckResultMaterials(m_Gltf);
        }

        [Test]
        public void MaterialsExtended()
        {
            CheckResultMaterials(m_Gltf);
        }

        [Test]
        public void Samplers()
        {
            CheckResultSamplers(m_Gltf);
        }

        [Test]
        public void SamplersExtended()
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
        public void AnimationChannelTargetEnumCasting()
        {
#if UNITY_ANIMATION
#pragma warning disable CS0618 // Type or member is obsolete
            var obj = new AnimationChannelTarget
            {
                path = null
            };
            Assert.AreEqual(AnimationChannel.Path.Invalid, obj.GetPath());
            Assert.IsNull(obj.path);
            // Second time to test cached value
            Assert.AreEqual(AnimationChannel.Path.Invalid, obj.GetPath());

            obj = new AnimationChannelTarget
            {
                path = "Pointer"
            };
            Assert.AreEqual(AnimationChannel.Path.Pointer, obj.GetPath());
            Assert.IsNull(obj.path);
#pragma warning restore CS0618 // Type or member is obsolete
#else
            Assert.Ignore("Requires Animation module to be enabled.");
#endif
        }

        [Test]
        public void AnimationSamplerEnumCasting()
        {
#if UNITY_ANIMATION
#pragma warning disable CS0618 // Type or member is obsolete
            var obj = new AnimationSampler
            {
                interpolation = null
            };
            Assert.AreEqual(InterpolationType.Linear, obj.GetInterpolationType());
            Assert.IsNull(obj.interpolation);
            // Second time to test cached value
            Assert.AreEqual(InterpolationType.Linear, obj.GetInterpolationType());

            obj = new AnimationSampler
            {
                interpolation = "CubicSpline"
            };
            Assert.AreEqual(InterpolationType.CubicSpline, obj.GetInterpolationType());
            Assert.IsNull(obj.interpolation);
#pragma warning restore CS0618 // Type or member is obsolete
#else
            Assert.Ignore("Requires Animation module to be enabled.");
#endif
        }

#if MESHOPT_IS_RECENT
        [Test]
        public void BufferViewEnumCasting()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var obj = new BufferViewMeshoptExtension
            {
                filter = "Octahedral",
                mode = "Nonsense"
            };

            Assert.AreEqual(Filter.Octahedral, obj.GetFilter());
            Assert.IsNull(obj.filter);
            // Second time to test cached value
            Assert.AreEqual(Filter.Octahedral, obj.GetFilter());

            Assert.AreEqual(Mode.Undefined, obj.GetMode());
            Assert.IsNull(obj.mode);
            obj.mode = "Indices";
            Assert.AreEqual(Mode.Indices, obj.GetMode());
            Assert.IsNull(obj.mode);
            // Second time to test cached value
            Assert.AreEqual(Mode.Indices, obj.GetMode());

            obj = new BufferViewMeshoptExtension
            {
                filter = "Nonsense"
            };
            Assert.AreEqual(Filter.None, obj.GetFilter());
            Assert.IsNull(obj.filter);
#pragma warning restore CS0618 // Type or member is obsolete
        }
#endif

        [Test]
        public void CameraEnumCasting()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var obj = new Camera
            {
                type = "Orthographic"
            };
            Assert.AreEqual(Schema.Camera.Type.Orthographic, obj.GetCameraType());
            Assert.IsNull(obj.type);
            // Second time to test cached value
            Assert.AreEqual(Schema.Camera.Type.Orthographic, obj.GetCameraType());

            obj = new Camera();
            Assert.AreEqual(Schema.Camera.Type.Perspective, obj.GetCameraType());
            obj = new Camera
            {
                Orthographic = new CameraOrthographic()
            };
            Assert.AreEqual(Schema.Camera.Type.Orthographic, obj.GetCameraType());
            obj = new Camera
            {
                Perspective = new CameraPerspective()
            };
            Assert.AreEqual(Schema.Camera.Type.Perspective, obj.GetCameraType());
#pragma warning restore CS0618 // Type or member is obsolete
        }


        [Test]
        public void LightPunctualEnumCasting()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var obj = new LightPunctual
            {
                type = "Directional"
            };
            Assert.AreEqual(LightPunctual.Type.Directional, obj.GetLightType());
            Assert.IsNull(obj.type);
            // Second time to test cached value
            Assert.AreEqual(LightPunctual.Type.Directional, obj.GetLightType());

            obj = new LightPunctual
            {
                type = "Nonsense"
            };
            Assert.AreEqual(LightPunctual.Type.Unknown, obj.GetLightType());
            Assert.IsNull(obj.type);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        [Test]
        public void MaterialAlphaModeEnumCasting()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var obj = new Material
            {
                alphaMode = "BLEND"
            };
            Assert.AreEqual(Material.AlphaMode.Blend, obj.GetAlphaMode());
            Assert.IsNull(obj.alphaMode);
            // Second time to test cached value
            Assert.AreEqual(Material.AlphaMode.Blend, obj.GetAlphaMode());

            obj = new Material
            {
                alphaMode = "Nonsense"
            };
            Assert.AreEqual(Material.AlphaMode.Opaque, obj.GetAlphaMode());
            Assert.IsNull(obj.alphaMode);
#pragma warning restore CS0618 // Type or member is obsolete
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
            Assert.AreEqual(AnimationChannel.Path.Weights, gltf.Animations[0].Channels[0].Target.GetPath());
            Assert.NotNull(gltf.Animations[0].Samplers);
            Assert.AreEqual(1, gltf.Animations[0].Samplers.Count);
            Assert.AreEqual(InterpolationType.CubicSpline, gltf.Animations[0].Samplers[0].GetInterpolationType());
        }
#endif

        static void CheckResultCamera(Root gltf)
        {
            Assert.NotNull(gltf);
            Assert.NotNull(gltf.Cameras);
            Assert.AreEqual(1, gltf.Cameras.Count);
            Assert.AreEqual(Schema.Camera.Type.Orthographic, gltf.Cameras[0].GetCameraType());
        }

#if MESHOPT_IS_RECENT
        static void CheckResultMeshopt(Root gltf)
        {
            Assert.NotNull(gltf);
            Assert.NotNull(gltf.BufferViews);
            Assert.AreEqual(1, gltf.BufferViews.Count);
            Assert.NotNull(gltf.BufferViews[0].Extensions?.EXT_meshopt_compression);
            Assert.AreEqual(Mode.Triangles, gltf.BufferViews[0].Extensions?.EXT_meshopt_compression.GetMode());
            Assert.AreEqual(Filter.Exponential, gltf.BufferViews[0].Extensions?.EXT_meshopt_compression.GetFilter());
        }
#endif
        static void CheckResultRootExtensions(Root gltf)
        {
            Assert.NotNull(gltf);
            Assert.NotNull(gltf.Extensions);
            Assert.NotNull(gltf.Extensions.KHR_lights_punctual);
            Assert.NotNull(gltf.Extensions.KHR_lights_punctual.lights);
            Assert.AreEqual(1, gltf.Extensions.KHR_lights_punctual.lights.Length);
            Assert.AreEqual(LightPunctual.Type.Directional, gltf.Extensions.KHR_lights_punctual.lights[0].GetLightType());
        }

        static void CheckResultMaterials(Root gltf)
        {
            Assert.NotNull(gltf);
            Assert.NotNull(gltf.Materials);
            Assert.AreEqual(1, gltf.Materials.Count);
            Assert.AreEqual(Material.AlphaMode.Mask, gltf.Materials[0].GetAlphaMode());
            Assert.NotNull(gltf.Meshes);
            Assert.AreEqual(1, gltf.Meshes.Count);
            Assert.NotNull(gltf.Meshes[0].Primitives);
            Assert.AreEqual(1, gltf.Meshes[0].Primitives.Count);
            Assert.AreEqual(DrawMode.LineStrip, gltf.Meshes[0].Primitives[0].Mode);
        }

        static void CheckResultSamplers(Root gltf)
        {
            Assert.NotNull(gltf);
            Assert.NotNull(gltf.Samplers);
            Assert.AreEqual(1, gltf.Samplers.Count);
            Assert.AreEqual(Sampler.MagFilterMode.Nearest, gltf.Samplers[0].magFilter);
            Assert.AreEqual(Sampler.WrapMode.MirroredRepeat, gltf.Samplers[0].wrapS);
            Assert.AreEqual(Sampler.WrapMode.None, gltf.Samplers[0].wrapT);
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
                ""path"": ""Weights""
            }
        }],
        ""samplers"": [{
            ""interpolation"": ""CubicSpline""
        }]
    }],
    ""bufferViews"": [{
        ""extensions"": {
            ""EXT_meshopt_compression"": {
                ""mode"": ""Triangles"",
                ""filter"": ""Exponential""
            }
        }
    }],
    ""cameras"": [{
        ""type"": ""Orthographic""
    }],
    ""extensions"": {
        ""KHR_lights_punctual"": {
            ""lights"":[{
                ""type"": ""Directional""
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
