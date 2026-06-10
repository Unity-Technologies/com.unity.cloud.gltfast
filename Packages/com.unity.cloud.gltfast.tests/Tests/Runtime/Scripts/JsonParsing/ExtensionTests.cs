// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;

using GLTFast.Schema;
using NUnit.Framework;
using Unity.Gltfast.Text.Json;
using UnityEngine;
using LightType = GLTFast.Schema.LightType;

namespace GLTFast.Tests.JsonParsing
{
    [Category("JsonParsing")]
    class ExtensionTests
    {
        const string k_CustomContent = @"
{
    ""floatProp"" : 3.4700000286102295,
    ""intProp"" : 42,
    ""stringProp"" : ""Yadiya"",
    ""eulerAngles"" : [
        1.0,
        1.0,
        1.0
    ],
    ""intArrayProp"" : [
        1,
        1,
        1
    ],
    ""rotation"" : [
        0.8199999928474426,
        0.8199999928474426,
        0.8199999928474426,
        0.8199999928474426
    ],
    ""color"" : [
        1.0,
        1.0,
        1.0,
        1.0
    ],
    ""subObject"":{
        ""floatProp"" : 3.4700000286102295,
        ""intProp"" : 42,
        ""stringProp"" : ""Yadiya"",
        ""eulerAngles"" : [
            1.0,
            1.0,
            1.0
        ],
        ""intArrayProp"" : [
            1,
            1,
            1
        ],
        ""rotation"" : [
            0.8199999928474426,
            0.8199999928474426,
            0.8199999928474426,
            0.8199999928474426
        ],
        ""color"" : [
            1.0,
            1.0,
            1.0,
            1.0
        ]
    }
}
";

        static readonly string k_CustomExtensionJson =
$@"
{{
    ""extensions"": {{
        ""CUSTOM_my_extension"":{k_CustomContent},
        ""KHR_lights_punctual"":
        {{
            ""lights"":[{{
                ""type"": ""directional""
            }}]
        }}
    }}
}}";

        class MyExtension : SubClass
        {
            public SubClass subObject;
        }

        class SubClass
        {
            public float floatProp;
            public int intProp;
            public string stringProp;
            public float[] eulerAngles;
            public int[] intArrayProp;
            public float[] rotation;
            public float[] color;
        }

        [Test]
        public void KnownExtensionOnly()
        {
            const string json = @"
            {
                ""extensions"": {
                    ""KHR_lights_punctual"":
                    {
                        ""lights"":[{
                            ""type"": ""directional""
                        }]
                    }
                }
            }";

            var gltf = JsonSerializer.Deserialize(json, GltfRootSourceGenerator.Default.Root);
            Assert.NotNull(gltf);
            Assert.NotNull(gltf.Extensions);
            Assert.NotNull(gltf.Extensions.LightsPunctual);
            Assert.NotNull(gltf.Extensions.LightsPunctual.Lights);
            Assert.AreEqual(1, gltf.Extensions.LightsPunctual.Lights.Length);
            Assert.AreEqual(LightType.Directional, gltf.Extensions.LightsPunctual.Lights[0].Type);
            Assert.IsFalse(gltf.Extensions.TryGetValue<MyExtension>("CUSTOM_my_extension", out var ext));
            Assert.IsNull(ext);
        }

        [Test]
        public void CustomExtensionOnly()
        {
            var json = $@"
            {{
                ""extensions"": {{
                    ""CUSTOM_my_extension"":{k_CustomContent}
                }}
            }}";

            var gltf = JsonSerializer.Deserialize(json, GltfRootSourceGenerator.Default.Root);
            Assert.NotNull(gltf);
            Assert.NotNull(gltf.Extensions);
            Assert.IsNull(gltf.Extensions.LightsPunctual);
            Assert.IsTrue(gltf.Extensions.TryGetValue<MyExtension>("CUSTOM_my_extension", out var ext));
            CertifyCustomExtensions(gltf.Extensions);
        }

        [Test]
        public void CustomExtension()
        {
            var gltf = JsonSerializer.Deserialize(k_CustomExtensionJson, GltfRootSourceGenerator.Default.Root);
            Assert.NotNull(gltf);
            Assert.NotNull(gltf.Extensions);
            Assert.NotNull(gltf.Extensions.LightsPunctual);
            Assert.NotNull(gltf.Extensions.LightsPunctual.Lights);
            Assert.AreEqual(1, gltf.Extensions.LightsPunctual.Lights.Length);
            Assert.AreEqual(LightType.Directional, gltf.Extensions.LightsPunctual.Lights[0].Type);
        }

        [Test]
        public void CustomExtensionExtras()
        {
            var gltf = JsonSerializer.Deserialize(k_CustomExtensionJson, GltfRootSourceGenerator.Default.Root);
            Assert.NotNull(gltf);
            Assert.NotNull(gltf.Extensions);
            Assert.NotNull(gltf.Extensions.LightsPunctual);
            Assert.NotNull(gltf.Extensions.LightsPunctual.Lights);
            Assert.AreEqual(1, gltf.Extensions.LightsPunctual.Lights.Length);
            Assert.AreEqual(LightType.Directional, gltf.Extensions.LightsPunctual.Lights[0].Type);
            Assert.IsTrue(gltf.Extensions.TryGetValue<MyExtension>("CUSTOM_my_extension", out var ext));
            CertifyCustomExtensions(gltf.Extensions);
        }

        [Test]
        public void CustomExtensionEverywhere()
        {
            var json = $@"
{{
    ""accessors"": [{{
        ""customProperty"": 42,
        ""extras"": {{""myKey"": ""myValue""}},
        ""extensions"": {{""CUSTOM_my_extension"": {k_CustomContent}}},
        ""sparse"":{{
            ""customProperty"": 420,
            ""extras"": {{""myKey"": ""myValue""}},
            ""extensions"": {{""CUSTOM_my_extension"": {k_CustomContent}}},
            ""indices"": {{
                ""customProperty"": 4200,
                ""extras"": {{""myKey"": ""myValue""}},
                ""extensions"": {{""CUSTOM_my_extension"": {k_CustomContent}}}
            }},
            ""values"": {{
                ""customProperty"": 4201,
                ""extras"": {{""myKey"": ""myValue""}},
                ""extensions"": {{""CUSTOM_my_extension"": {k_CustomContent}}}
            }}
        }}
    }}],
    ""animations"": [{{
        ""customProperty"": 43,
        ""extras"": {{""myKey"": ""myValue""}},
        ""extensions"": {{""CUSTOM_my_extension"": {k_CustomContent}}},
        ""channels"": [{{
            ""customProperty"": 430,
            ""extras"": {{""myKey"": ""myValue""}},
            ""extensions"": {{""CUSTOM_my_extension"": {k_CustomContent}}},
            ""target"": {{
                ""customProperty"": 4300,
                ""extras"": {{""myKey"": ""myValue""}},
                ""extensions"": {{""CUSTOM_my_extension"": {k_CustomContent}}}
            }}
        }}],
        ""samplers"": [{{
            ""customProperty"": 431,
            ""extras"": {{""myKey"": ""myValue""}},
            ""extensions"": {{""CUSTOM_my_extension"": {k_CustomContent}}}
        }}]
    }}],
    ""asset"": {{
        ""customProperty"": 44,
        ""extras"": {{""myKey"": ""myValue""}},
        ""extensions"": {{""CUSTOM_my_extension"": {k_CustomContent}}},
        ""version"": ""2.0""
    }},
    ""buffers"": [{{
        ""customProperty"": 45,
        ""extras"": {{""myKey"": ""myValue""}},
        ""extensions"": {{""CUSTOM_my_extension"": {k_CustomContent}}}
    }}],
    ""bufferViews"": [{{
        ""customProperty"": 46,
        ""extras"": {{""myKey"": ""myValue""}},
        ""extensions"": {{""CUSTOM_my_extension"": {k_CustomContent}}}
    }}],
    ""cameras"": [{{
        ""customProperty"": 47,
        ""extras"": {{""myKey"": ""myValue""}},
        ""extensions"": {{""CUSTOM_my_extension"": {k_CustomContent}}},
        ""orthographic"": {{
            ""customProperty"": 470,
            ""extras"": {{""myKey"": ""myValue""}},
            ""extensions"": {{""CUSTOM_my_extension"": {k_CustomContent}}}
        }},
        ""perspective"": {{
            ""customProperty"": 471,
            ""extras"": {{""myKey"": ""myValue""}},
            ""extensions"": {{""CUSTOM_my_extension"": {k_CustomContent}}}
        }}
    }}],
    ""customProperty"": 48,
    ""extensions"": {{""CUSTOM_my_extension"": {k_CustomContent}}},
    ""extensionsRequired"": [""CUSTOM_my_extension""],
    ""extensionsUsed"": [""CUSTOM_my_extension""],
    ""extras"": {{""myKey"": ""myValue""}},
    ""images"": [{{
        ""customProperty"": 49,
        ""extras"": {{""myKey"": ""myValue""}},
        ""extensions"": {{""CUSTOM_my_extension"": {k_CustomContent}}}
    }}],
    ""materials"": [{{
        ""customProperty"": 50,
        ""extras"": {{""myKey"": ""myValue""}},
        ""extensions"": {{""CUSTOM_my_extension"": {k_CustomContent}}},
        ""pbrMetallicRoughness"": {{
            ""customProperty"": 500,
            ""extras"": {{""myKey"": ""myValue""}},
            ""extensions"": {{""CUSTOM_my_extension"": {k_CustomContent}}},
            ""baseColorTexture"": {{
                ""customProperty"": 5000,
                ""extras"": {{""myKey"": ""myValue""}},
                ""extensions"": {{""CUSTOM_my_extension"": {k_CustomContent}}}
            }},
            ""metallicRoughnessTexture"": {{
                ""customProperty"": 5001,
                ""extras"": {{""myKey"": ""myValue""}},
                ""extensions"": {{""CUSTOM_my_extension"": {k_CustomContent}}}
            }}
        }},
        ""emissiveTexture"": {{
            ""customProperty"": 501,
            ""extras"": {{""myKey"": ""myValue""}},
            ""extensions"": {{""CUSTOM_my_extension"": {k_CustomContent}}}
        }},
        ""normalTexture"": {{
            ""customProperty"": 502,
            ""extras"": {{""myKey"": ""myValue""}},
            ""extensions"": {{""CUSTOM_my_extension"": {k_CustomContent}}}
        }},
        ""occlusionTexture"": {{
            ""customProperty"": 503,
            ""extras"": {{""myKey"": ""myValue""}},
            ""extensions"": {{""CUSTOM_my_extension"": {k_CustomContent}}}
        }}
    }}],
    ""meshes"": [{{
        ""primitives"": [{{
            ""customProperty"": 510,
            ""extras"": {{""myKey"": ""myValue""}},
            ""extensions"": {{""CUSTOM_my_extension"": {k_CustomContent}}}
        }}],
        ""customProperty"": 51,
        ""extras"": {{""myKey"": ""myValue""}},
        ""extensions"": {{""CUSTOM_my_extension"": {k_CustomContent}}}
    }}],
    ""nodes"": [{{
        ""customProperty"": 52,
        ""extras"": {{""myKey"": ""myValue""}},
        ""extensions"": {{""CUSTOM_my_extension"": {k_CustomContent}}}
    }}],
    ""samplers"": [{{
        ""customProperty"": 53,
        ""extras"": {{""myKey"": ""myValue""}},
        ""extensions"": {{""CUSTOM_my_extension"": {k_CustomContent}}}
    }}],
    ""scene"": 0,
    ""scenes"": [{{
        ""customProperty"": 54,
        ""extras"": {{""myKey"": ""myValue""}},
        ""extensions"": {{""CUSTOM_my_extension"": {k_CustomContent}}}
    }}],
    ""skins"": [{{
        ""customProperty"": 55,
        ""extras"": {{""myKey"": ""myValue""}},
        ""extensions"": {{""CUSTOM_my_extension"": {k_CustomContent}}}
    }}],
    ""textures"": [{{
        ""customProperty"": 56,
        ""extras"": {{""myKey"": ""myValue""}},
        ""extensions"": {{""CUSTOM_my_extension"": {k_CustomContent}}}
    }}]
}}";

            var gltf = JsonSerializer.Deserialize(json, GltfRootSourceGenerator.Default.Root);

            CertifyCustomData(gltf, 48);
            CertifyCustomExtensions(gltf.Extensions);
            CertifyCustomExtras(gltf.Extras);

            CertifyCustomData(gltf.Accessors[0], 42);
            CertifyCustomExtensions(gltf.Accessors[0].Extensions);
            CertifyCustomExtras(gltf.Accessors[0].Extras);

            CertifyCustomData(gltf.Accessors[0].Sparse, 420);
            CertifyCustomExtensions(gltf.Accessors[0].Sparse.Extensions);
            CertifyCustomExtras(gltf.Accessors[0].Sparse.Extras);

            CertifyCustomData(gltf.Accessors[0].Sparse.Indices, 4200);
            CertifyCustomExtensions(gltf.Accessors[0].Sparse.Indices.Extensions);
            CertifyCustomExtras(gltf.Accessors[0].Sparse.Indices.Extras);

            CertifyCustomData(gltf.Accessors[0].Sparse.Values, 4201);
            CertifyCustomExtensions(gltf.Accessors[0].Sparse.Values.Extensions);
            CertifyCustomExtras(gltf.Accessors[0].Sparse.Values.Extras);

#if UNITY_ANIMATION
            CertifyCustomData(gltf.Animations[0], 43);
            CertifyCustomExtensions(gltf.Animations[0].Extensions);
            CertifyCustomExtras(gltf.Animations[0].Extras);

            CertifyCustomData(gltf.Animations[0].Channels[0], 430);
            CertifyCustomExtensions(gltf.Animations[0].Channels[0].Extensions);
            CertifyCustomExtras(gltf.Animations[0].Channels[0].Extras);

            CertifyCustomData(gltf.Animations[0].Channels[0].Target, 4300);
            CertifyCustomExtensions(gltf.Animations[0].Channels[0].Target.Extensions);
            CertifyCustomExtras(gltf.Animations[0].Channels[0].Target.Extras);

            CertifyCustomData(gltf.Animations[0].Samplers[0], 431);
            CertifyCustomExtensions(gltf.Animations[0].Samplers[0].Extensions);
            CertifyCustomExtras(gltf.Animations[0].Samplers[0].Extras);
#endif

            CertifyCustomData(gltf.Asset, 44);
            CertifyCustomExtensions(gltf.Asset.Extensions);
            CertifyCustomExtras(gltf.Asset.Extras);

            CertifyCustomData(gltf.Buffers[0], 45);
            CertifyCustomExtensions(gltf.Buffers[0].Extensions);
            CertifyCustomExtras(gltf.Buffers[0].Extras);

            CertifyCustomData(gltf.BufferViews[0], 46);
            CertifyCustomExtensions(gltf.BufferViews[0].Extensions);
            CertifyCustomExtras(gltf.BufferViews[0].Extras);

            CertifyCustomData(gltf.Cameras[0], 47);
            CertifyCustomExtensions(gltf.Cameras[0].Extensions);
            CertifyCustomExtras(gltf.Cameras[0].Extras);

            CertifyCustomData(gltf.Cameras[0].Orthographic, 470);
            CertifyCustomExtensions(gltf.Cameras[0].Orthographic.Extensions);
            CertifyCustomExtras(gltf.Cameras[0].Orthographic.Extras);

            CertifyCustomData(gltf.Cameras[0].Perspective, 471);
            CertifyCustomExtensions(gltf.Cameras[0].Perspective.Extensions);
            CertifyCustomExtras(gltf.Cameras[0].Perspective.Extras);

            CertifyCustomData(gltf.Images[0], 49);
            CertifyCustomExtensions(gltf.Images[0].Extensions);
            CertifyCustomExtras(gltf.Images[0].Extras);

            CertifyCustomData(gltf.Materials[0], 50);
            CertifyCustomExtensions(gltf.Materials[0].Extensions);
            CertifyCustomExtras(gltf.Materials[0].Extras);

            CertifyCustomData(gltf.Materials[0].PbrMetallicRoughness, 500);
            CertifyCustomExtensions(gltf.Materials[0].PbrMetallicRoughness.Extensions);
            CertifyCustomExtras(gltf.Materials[0].PbrMetallicRoughness.Extras);

            CertifyCustomData(gltf.Materials[0].PbrMetallicRoughness.BaseColorTexture, 5000);
            CertifyCustomExtensions(gltf.Materials[0].PbrMetallicRoughness.BaseColorTexture.Extensions);
            CertifyCustomExtras(gltf.Materials[0].PbrMetallicRoughness.BaseColorTexture.Extras);

            CertifyCustomData(gltf.Materials[0].PbrMetallicRoughness.MetallicRoughnessTexture, 5001);
            CertifyCustomExtensions(gltf.Materials[0].PbrMetallicRoughness.MetallicRoughnessTexture.Extensions);
            CertifyCustomExtras(gltf.Materials[0].PbrMetallicRoughness.MetallicRoughnessTexture.Extras);

            CertifyCustomData(gltf.Materials[0].EmissiveTexture, 501);
            CertifyCustomExtensions(gltf.Materials[0].EmissiveTexture.Extensions);
            CertifyCustomExtras(gltf.Materials[0].EmissiveTexture.Extras);

            CertifyCustomData(gltf.Materials[0].NormalTexture, 502);
            CertifyCustomExtensions(gltf.Materials[0].NormalTexture.Extensions);
            CertifyCustomExtras(gltf.Materials[0].NormalTexture.Extras);

            CertifyCustomData(gltf.Materials[0].OcclusionTexture, 503);
            CertifyCustomExtensions(gltf.Materials[0].OcclusionTexture.Extensions);
            CertifyCustomExtras(gltf.Materials[0].OcclusionTexture.Extras);

            CertifyCustomData(gltf.Meshes[0], 51);
            CertifyCustomExtensions(gltf.Meshes[0].Extensions);
            CertifyCustomExtras(gltf.Meshes[0].Extras);

            CertifyCustomData(gltf.Meshes[0].Primitives[0], 510);
            CertifyCustomExtensions(gltf.Meshes[0].Primitives[0].Extensions);
            CertifyCustomExtras(gltf.Meshes[0].Primitives[0].Extras);

            CertifyCustomData(gltf.Nodes[0], 52);
            CertifyCustomExtensions(gltf.Nodes[0].Extensions);
            CertifyCustomExtras(gltf.Nodes[0].Extras);

            CertifyCustomData(gltf.Samplers[0], 53);
            CertifyCustomExtensions(gltf.Samplers[0].Extensions);
            CertifyCustomExtras(gltf.Samplers[0].Extras);

            CertifyCustomData(gltf.Scenes[0], 54);
            CertifyCustomExtensions(gltf.Scenes[0].Extensions);
            CertifyCustomExtras(gltf.Scenes[0].Extras);

            CertifyCustomData(gltf.Skins[0], 55);
            CertifyCustomExtensions(gltf.Skins[0].Extensions);
            CertifyCustomExtras(gltf.Skins[0].Extras);

            CertifyCustomData(gltf.Textures[0], 56);
            CertifyCustomExtensions(gltf.Textures[0].Extensions);
            CertifyCustomExtras(gltf.Textures[0].Extras);
        }

        static void CertifyCustomData(IGltfObject gltf, int expected)
        {
            Assert.NotNull(gltf);
            Assert.IsTrue(gltf.TryGetValue("customProperty", out int prop));
            Assert.AreEqual(expected, prop);
        }

        static void CertifyCustomExtensions(IGltfObject extensions)
        {
            Assert.NotNull(extensions);
            Assert.IsFalse(extensions.TryGetValue<MyExtension>("NO_MATCH", out _));
            Assert.IsTrue(extensions.TryGetValue<MyExtension>("CUSTOM_my_extension", out var ext));
            CertifySubClass(ext);
            CertifySubClass(ext.subObject);
            return;

            void CertifySubClass(SubClass sub)
            {
                Assert.NotNull(sub);
                Assert.AreEqual(3.4700000286102295f, sub.floatProp, "JSON value mismatch");
                Assert.AreEqual(42, sub.intProp, "JSON value mismatch");
                Assert.AreEqual("Yadiya", sub.stringProp, "JSON value mismatch");
                Assert.AreEqual(3, sub.eulerAngles.Length);
                Assert.AreEqual(1.0f, sub.eulerAngles[0]);
                Assert.AreEqual(1.0f, sub.eulerAngles[1]);
                Assert.AreEqual(1.0f, sub.eulerAngles[2]);
                Assert.AreEqual(3, sub.intArrayProp.Length);
                Assert.AreEqual(1, sub.intArrayProp[0]);
                Assert.AreEqual(1, sub.intArrayProp[1]);
                Assert.AreEqual(1, sub.intArrayProp[2]);
                Assert.AreEqual(4, sub.rotation.Length);
                Assert.AreEqual(0.8199999928474426f, sub.rotation[0]);
                Assert.AreEqual(0.8199999928474426f, sub.rotation[1]);
                Assert.AreEqual(0.8199999928474426f, sub.rotation[2]);
                Assert.AreEqual(0.8199999928474426f, sub.rotation[3]);
                Assert.AreEqual(4, sub.color.Length);
                Assert.AreEqual(1.0f, sub.color[0]);
                Assert.AreEqual(1.0f, sub.color[1]);
                Assert.AreEqual(1.0f, sub.color[2]);
                Assert.AreEqual(1.0f, sub.color[3]);
            }
        }

        static void CertifyCustomExtras(IGltfObject extras)
        {
            Assert.NotNull(extras);
            Assert.IsFalse(extras.TryGetValue("NoMatch", out int _));
            Assert.IsTrue(extras.TryGetValue("myKey", out string value));
            Assert.AreEqual("myValue", value);

            // incorrect destination type int (actually is a string)
            Assert.Throws<JsonException>(
                () => extras.TryGetValue("myKey", out int intValue));
        }

        [Test]
        public void CustomExtensionNowhere()
        {
            const string json = @"
{
    ""accessors"": [{}],
    ""animations"": [{}],
    ""asset"": {
        ""version"": ""2.0""},
    ""buffers"": [{}],
    ""bufferViews"": [{}],
    ""cameras"": [{}],
    ""images"": [{}],
    ""materials"": [{}],
    ""meshes"": [{}],
    ""nodes"": [{}],
    ""samplers"": [{}],
    ""scenes"": [{}],
    ""skins"": [{}],
    ""textures"": [{}]
}
            ";

            var gltf = JsonSerializer.Deserialize(json, GltfRootSourceGenerator.Default.Root);

            Assert.NotNull(gltf);
            Assert.IsNull(gltf.Extras);
            Assert.IsNull(gltf.Extensions);

            Assert.IsNull(gltf.Accessors[0].Extras);
            Assert.IsNull(gltf.Accessors[0].Extensions);

#if UNITY_ANIMATION
            Assert.IsNull(gltf.Animations[0].Extras);
            Assert.IsNull(gltf.Animations[0].Extensions);
#endif

            Assert.IsNull(gltf.Asset.Extras);
            Assert.IsNull(gltf.Asset.Extensions);

            Assert.IsNull(gltf.Buffers[0].Extras);
            Assert.IsNull(gltf.Buffers[0].Extensions);

            Assert.IsNull(gltf.BufferViews[0].Extras);
            Assert.IsNull(gltf.BufferViews[0].Extensions);

            Assert.IsNull(gltf.Cameras[0].Extras);
            Assert.IsNull(gltf.Cameras[0].Extensions);

            Assert.IsNull(gltf.Images[0].Extras);
            Assert.IsNull(gltf.Images[0].Extensions);

            Assert.IsNull(gltf.Materials[0].Extras);
            Assert.IsNull(gltf.Materials[0].Extensions);

            Assert.IsNull(gltf.Meshes[0].Extras);
            Assert.IsNull(gltf.Meshes[0].Extensions);

            Assert.IsNull(gltf.Nodes[0].Extras);
            Assert.IsNull(gltf.Nodes[0].Extensions);

            Assert.IsNull(gltf.Samplers[0].Extras);
            Assert.IsNull(gltf.Samplers[0].Extensions);

            Assert.IsNull(gltf.Scenes[0].Extras);
            Assert.IsNull(gltf.Scenes[0].Extensions);

            Assert.IsNull(gltf.Skins[0].Extras);
            Assert.IsNull(gltf.Skins[0].Extensions);

            Assert.IsNull(gltf.Textures[0].Extras);
            Assert.IsNull(gltf.Textures[0].Extensions);
        }
    }
}
