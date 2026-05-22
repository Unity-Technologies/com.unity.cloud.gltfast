// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using GLTFast.Schema;
using NUnit.Framework;
#if UNITY_6000_5_OR_NEWER
using System.Text.Json;
#else
using Unity.Gltfast.Text.Json;
#endif
using UnityEngine;

namespace GLTFast.Tests.JsonParsing
{
    [TestFixture]
    [Category("JsonParsing")]
    class ValueArrayTests
    {
        Root m_Gltf;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            m_Gltf = JsonSerializer.Deserialize(k_ValueArraysJson, GltfRootSourceGenerator.Default.Root);
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
            CheckFloatArray(gltf.Accessors[0].max, 3, 1, 2, 3);
            CheckFloatArray(gltf.Accessors[0].min, 3, -1, -2, -3);
        }

        static void CheckResultLightPunctualColor(Root gltf)
        {
            var lights = gltf?.Extensions?.KHR_lights_punctual?.lights;
            Assert.NotNull(lights);
            Assert.AreEqual(1, lights.Length);
            Assert.AreEqual(new Color(.1f, .2f, .3f), lights[0].LightColor);
        }

        static void CheckResultMaterialValues(Root gltf)
        {
            Assert.NotNull(gltf);
            Assert.NotNull(gltf.Materials);
            Assert.AreEqual(1, gltf.Materials.Count);
            var mat = gltf.Materials[0];
            Assert.AreEqual(new Color(.1f, .2f, .3f), mat.Emissive);
            Assert.NotNull(mat.PbrMetallicRoughness);
            Assert.AreEqual(new Color(.1f, .2f, .3f, .4f), mat.PbrMetallicRoughness.BaseColor);
            var transform = mat.NormalTexture?.Extensions?.KHR_texture_transform;
            Assert.NotNull(transform);
            CheckFloatArray(transform.offset, 2, 1, 2);
            CheckFloatArray(transform.scale, 2, 3, 4);
            var ext = mat.Extensions;
            Assert.NotNull(ext?.KHR_materials_sheen);
            Assert.AreEqual(new Color(.1f, .2f, .3f), ext.KHR_materials_sheen.SheenColor);
            Assert.NotNull(ext.KHR_materials_pbrSpecularGlossiness);
            Assert.AreEqual(new Color(.1f, .2f, .3f, .4f), ext.KHR_materials_pbrSpecularGlossiness.DiffuseColor);
            Assert.AreEqual(new Color(.1f, .2f, .3f), ext.KHR_materials_pbrSpecularGlossiness.SpecularColor);
        }

        static void CheckResultMeshWeights(Root gltf)
        {
            Assert.NotNull(gltf?.Meshes);
            Assert.AreEqual(1, gltf.Meshes.Count);
            var mesh = gltf.Meshes[0];
            CheckFloatArray(mesh.weights, 5, 1, 2, 3, 4, 5);
        }

        static void CheckResultNodeValues(Root gltf)
        {
            Assert.NotNull(gltf?.Nodes);
            Assert.AreEqual(1, gltf.Nodes.Count);
            var node = gltf.Nodes[0];
            CheckFloatArray(node.matrix, 16, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);
            CheckFloatArray(node.rotation, 4, 1, 2, 3, 4);
            CheckFloatArray(node.scale, 3, 1, 2, 3);
            CheckFloatArray(node.translation, 3, 1, 2, 3);
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
