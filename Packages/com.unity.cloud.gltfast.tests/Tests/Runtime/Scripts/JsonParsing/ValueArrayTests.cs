// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Cloud.Gltfast.Objects;
using Unity.Cloud.Gltfast.Text.Json;
using Unity.Mathematics;
using UnityEngine;
using Color = UnityEngine.Color;

namespace Unity.Cloud.Gltfast.Tests.JsonParsing
{
    [TestFixture]
    [Category("JsonParsing")]
    class ValueArrayTests
    {
        Root m_Gltf;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            m_Gltf = JsonSerializer.Deserialize(k_ValueArraysJson, GltfJsonContext.Default.Root);
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
        public void LightPunctualColor()
        {
            CheckResultLightPunctualColor(m_Gltf);
        }

        [Test]
        public void LightPunctualColorExtended()
        {
            CheckResultLightPunctualColor(m_Gltf);
        }

        [Test]
        public void MaterialValues()
        {
            CheckResultMaterialValues(m_Gltf);
        }

        [Test]
        public void MaterialValuesExtended()
        {
            CheckResultMaterialValues(m_Gltf);
        }

        [Test]
        public void MeshWeights()
        {
            CheckResultMeshWeights(m_Gltf);
        }

        [Test]
        public void MeshWeightsExtended()
        {
            CheckResultMeshWeights(m_Gltf);
        }

        [Test]
        public void NodeValues()
        {
            CheckResultNodeValues(m_Gltf);
        }

        [Test]
        public void NodeValuesExtended()
        {
            CheckResultNodeValues(m_Gltf);
        }

        static void CheckResultAccessor(Root gltf)
        {
            Assert.NotNull(gltf);
            Assert.NotNull(gltf.Accessors);
            Assert.AreEqual(1, gltf.Accessors.Count);
            CheckFloatArray(gltf.Accessors[0].Max, 3, 1, 2, 3);
            CheckFloatArray(gltf.Accessors[0].Min, 3, -1, -2, -3);
        }

        static void CheckResultLightPunctualColor(Root gltf)
        {
            var lights = gltf?.Extensions?.LightsPunctual?.Lights;
            Assert.NotNull(lights);
            Assert.AreEqual(1, lights.Count);
            Assert.AreEqual(new Color(.1f, .2f, .3f), (Color)lights[0].Color);
        }

        static void CheckResultMaterialValues(Root gltf)
        {
            Assert.NotNull(gltf);
            Assert.NotNull(gltf.Materials);
            Assert.AreEqual(1, gltf.Materials.Count);
            var mat = gltf.Materials[0];
            Assert.AreEqual(new Color(.1f, .2f, .3f), (Color)mat.EmissiveFactor);
            Assert.NotNull(mat.PbrMetallicRoughness);
            Assert.AreEqual(new Color(.1f, .2f, .3f, .4f), (Color)mat.PbrMetallicRoughness.BaseColorFactor);
            var transform = mat.NormalTexture?.Extensions?.TextureTransform;
            Assert.NotNull(transform);
            Assert.AreEqual(new float2(1, 2), transform.Offset);
            Assert.AreEqual(new float2(3, 4), transform.Scale);
            var ext = mat.Extensions;
            Assert.NotNull(ext?.Sheen);
            Assert.AreEqual(new Color(.1f, .2f, .3f), (Color)ext.Sheen.SheenColorFactor);
            Assert.NotNull(ext.PbrSpecularGlossiness);
            Assert.AreEqual(new Color(.1f, .2f, .3f, .4f), (Color)ext.PbrSpecularGlossiness.DiffuseFactor);
            Assert.AreEqual(new Color(.1f, .2f, .3f), (Color)ext.PbrSpecularGlossiness.SpecularFactor);
        }

        static void CheckResultMeshWeights(Root gltf)
        {
            Assert.NotNull(gltf?.Meshes);
            Assert.AreEqual(1, gltf.Meshes.Count);
            var mesh = gltf.Meshes[0];
            CheckFloatArray(mesh.Weights, 5, 1, 2, 3, 4, 5);
        }

        static void CheckResultNodeValues(Root gltf)
        {
            Assert.NotNull(gltf?.Nodes);
            Assert.AreEqual(1, gltf.Nodes.Count);
            var node = gltf.Nodes[0];
            Assert.IsTrue(node.Matrix.HasValue);
            Assert.AreEqual(
                new double4x4(
                    new double4(1, 2, 3, 4),
                    new double4(5, 6, 7, 8),
                    new double4(9, 10, 11, 12),
                    new double4(13, 14, 15, 16)),
                node.Matrix.Value);
            Assert.IsTrue(node.Rotation.HasValue);
            Assert.AreEqual(new double4(1, 2, 3, 4), node.Rotation.Value);
            Assert.IsTrue(node.Scale.HasValue);
            Assert.AreEqual(new double3(1, 2, 3), node.Scale.Value);
            Assert.IsTrue(node.Translation.HasValue);
            Assert.AreEqual(new double3(1, 2, 3), node.Translation.Value);
        }

        static void CheckFloatArray(IReadOnlyList<float> actual, int expectedLength, params float[] expected)
        {
            if (actual == null && expected == null && expectedLength == 0)
            {
                return;
            }
            Assert.NotNull(actual);
            Assert.NotNull(expected);
            Assert.AreEqual(expectedLength, actual.Count);
            Assert.AreEqual(expectedLength, expected.Length);
            for (var i = 0; i < expectedLength; i++)
            {
                Assert.AreEqual(expected[i], actual[i]);
            }
        }

        static void CheckFloatArray(IReadOnlyList<double> actual, int expectedLength, params float[] expected)
        {
            if (actual == null && expected == null && expectedLength == 0)
            {
                return;
            }
            Assert.NotNull(actual);
            Assert.NotNull(expected);
            Assert.AreEqual(expectedLength, actual.Count);
            Assert.AreEqual(expectedLength, expected.Length);
            for (var i = 0; i < expectedLength; i++)
            {
                Assert.AreEqual(expected[i], actual[i]);
            }
        }

        const string k_ValueArraysJson = @"
{
    ""accessors"": [{
        ""max"": [1,2,3],
        ""min"": [-1,-2,-3]
    }],
    ""extensions"": {
        ""KHR_lights_punctual"": {
            ""lights"":[{
                ""color"": [0.1,0.2,0.3]
            }]
        }
    },
    ""materials"": [{
        ""emissiveFactor"": [0.1,0.2,0.3],
        ""pbrMetallicRoughness"": {
            ""baseColorFactor"": [0.1,0.2,0.3,0.4]
        },
        ""normalTexture"": {
            ""extensions"": {
                ""KHR_texture_transform"": {
                    ""offset"": [1,2],
                    ""scale"": [3,4]
                }
            }
        },
        ""extensions"": {
            ""KHR_materials_sheen"": {
                ""sheenColorFactor"":[0.1,0.2,0.3]
            },
            ""KHR_materials_pbrSpecularGlossiness"": {
                ""diffuseFactor"":[0.1,0.2,0.3,0.4],
                ""specularFactor"":[0.1,0.2,0.3]
            }
        }
    }],
    ""meshes"": [{
        ""weights"": [1,2,3,4,5],
        ""primitives"":[{}]
    }],
    ""nodes"": [{
        ""matrix"": [1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16],
        ""rotation"": [1,2,3,4],
        ""translation"": [1,2,3],
        ""scale"": [1,2,3]
    }]
}";
    }
}
