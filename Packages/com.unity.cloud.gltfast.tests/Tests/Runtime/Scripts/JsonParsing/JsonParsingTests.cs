// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using NUnit.Framework;
using Unity.Cloud.Gltfast.Schema;
using Unity.Cloud.Gltfast.Text.Json;
using UnityEngine;

namespace Unity.Cloud.Gltfast.Tests.JsonParsing
{
    [Category("JsonParsing")]
    class JsonParsingTests
    {

        static readonly string k_MaterialExtensionGltf = @"
{
    ""materials"" : [
        {
            ""name"" : ""noExtension""
        },
        {
            ""name"" : ""emptyExtension"",
            ""extensions"": {
                ""dummy"": ""value""
            }
        },
        {
            ""name"" : ""unlit"",
            ""extensions"": {
                ""KHR_materials_unlit"": {}
            }
        },
        {
            ""name"" : ""specularGlossiness"",
            ""extensions"": {
                ""KHR_materials_pbrSpecularGlossiness"": {
                    ""diffuseTexture"": {
                        ""index"": 5
                    }
                }
            }
        },
        {
            ""name"" : ""transmission"",
            ""extensions"": {
                ""KHR_materials_transmission"": {}
            }
        },
        {
            ""name"" : ""clearcoat"",
            ""extensions"": {
                ""KHR_materials_clearcoat"": {}
            }
        },
        {
            ""name"" : ""sheen"",
            ""extensions"": {
                ""KHR_materials_sheen"": {}
            }
        },
        {
            ""name"" : ""ior"",
            ""extensions"": {
                ""KHR_materials_ior"": {}
            }
        },
        {
            ""name"" : ""specular"",
            ""extensions"": {
                ""KHR_materials_specular"": {}
            }
        },
        {
            ""name"" : ""all"",
            ""extensions"": {
                ""KHR_materials_unlit"": {},
                ""KHR_materials_pbrSpecularGlossiness"": {},
                ""KHR_materials_transmission"": {},
                ""KHR_materials_clearcoat"": {},
                ""KHR_materials_sheen"": {},
                ""KHR_materials_ior"": {},
                ""KHR_materials_specular"": {}
            }
        }
    ]
}
";

        static readonly string k_SparseAccessorsGltf = @"
{
    ""accessors"" : [ {
        ""bufferView"" : 0,
        ""byteOffset"" : 0,
        ""componentType"" : 5123,
        ""count"" : 36,
        ""type"" : ""SCALAR"",
        ""max"" : [ 13 ],
        ""min"" : [ 0 ]
      }, {
        ""bufferView"" : 1,
        ""byteOffset"" : 0,
        ""componentType"" : 5126,
        ""count"" : 14,
        ""type"" : ""VEC3"",
        ""max"" : [ 6.0, 4.0, 0.0 ],
        ""min"" : [ 0.0, 0.0, 0.0 ],
        ""sparse"" : {
          ""count"" : 3,
          ""indices"" : {
            ""bufferView"" : 2,
            ""byteOffset"" : 0,
            ""componentType"" : 5123
          },
          ""values"" : {
            ""bufferView"" : 3,
            ""byteOffset"" : 0
          }
        }
    }, {
        ""bufferView"" : 1,
        ""byteOffset"" : 0,
        ""componentType"" : 5126,
        ""count"" : 14,
        ""type"" : ""VEC3"",
        ""max"" : [ 6.0, 4.0, 0.0 ],
        ""min"" : [ 0.0, 0.0, 0.0 ],
        ""sparse"" : {}
        } ]
}
";
        static readonly string k_MeshTargetNamesGltf = @"
{
    ""meshes"": [
        {
            ""extras"": {
                ""targetNames"": [
                    ""Key 1"",""Key 2""
                ]
            }
        },
        {
            ""extras"": {
                ""different"": ""content""
            }
        }
    ]
}
";

        static readonly string k_MinMagFilter = @"
{
    ""samplers"": [{
        },{
        ""magFilter"": 100,
        ""minFilter"": 100
        },{
        ""magFilter"": 9728,
        ""minFilter"": 9728
        },{
        ""magFilter"": 9729,
        ""minFilter"": 9729
        },{
        ""minFilter"": 9984
        },{
        ""minFilter"": 9985
        },{
        ""minFilter"": 9986
        },{
        ""minFilter"": 9987
        }
    ]
}
";

        static readonly string k_UnknownNodeExtension = @"
{
    ""nodes"": [
        {
            ""name"": ""Node0""
        },
        {
            ""extensions"": {},
            ""name"": ""Node1""
        },
        {
            ""extensions"": {
                ""MOZ_hubs_components"": {
                    ""morph-audio-feedback"": {
                        ""name"": ""mouthOpen"",
                        ""minValue"": 0.0,
                        ""maxValue"": 1.0
                    }
                }
            },
            ""name"": ""Node2""
        },
        {
            ""extensions"": {
                ""EXT_mesh_gpu_instancing"": {
                    ""attributes"": {
                        ""TRANSLATION"": 42
                    }
                }
            },
            ""name"": ""Node3""
        },
        {
            ""extensions"": {
                ""KHR_lights_punctual"": {
                    ""light"": 42
                }
            },
            ""name"": ""Node4""
        },
        {
            ""extensions"": {
                ""EXT_mesh_gpu_instancing"": {
                    ""attributes"": {
                        ""TRANSLATION"": 13
                    }
                },
                ""KHR_lights_punctual"": {
                    ""light"": 42
                }
            },
            ""name"": ""Node5""
        }
    ]
}
";

        static readonly string k_UnknownTextureExtension = @"
{
    ""textures"": [
        {
            ""name"": ""Texture0""
        },
        {
            ""extensions"": {},
            ""name"": ""Texture1""
        },
        {
            ""extensions"": {
                ""EXT_texture_webp"": {
                    ""source"": 42
                }
            },
            ""name"": ""Texture2""
        },
        {
            ""extensions"": {
                ""KHR_texture_basisu"": {
                    ""source"": 42
                }
            },
            ""name"": ""Texture3""
        },
        {
            ""extensions"": {
                ""KHR_texture_basisu"": {
                    ""source"": 42
                },
                ""EXT_texture_webp"": {
                    ""source"": 43
                }
            },
            ""name"": ""Texture4""
        }
    ]
}
";

        [Test]
        [TestCase(AlphaMode.Opaque, "{}", null)]
        [TestCase(AlphaMode.Blend, @"{""alphaMode"":""BLEND""}", null)]
        [TestCase(AlphaMode.Mask, @"{""alphaMode"":""MASK""}", null)]
        [TestCase(AlphaMode.Opaque, @"{""alphaMode"":""OPAQUE""}", null)]
        [TestCase(AlphaMode.Opaque, @"{""alphaMode"":""Invalid""}", "Invalid")]

        public void MaterialAlphaMode(AlphaMode expected, string value, string expectedValue)
        {
            var material = JsonSerializer.Deserialize(value, GltfJsonContext.Default.Material);
            Assert.AreEqual(expected, material.AlphaMode.Value);
            if (expectedValue != null)
            {
                Assert.AreEqual(System.Text.Encoding.UTF8.GetBytes(expectedValue), material.AlphaMode.RawValue);
            }
        }

        [Test]
        public void MaterialExtensions()
        {
            Parse(k_MaterialExtensionGltf, AssertMaterialExtensionResult);
        }

        [Test]
        public void SparseAccessors()
        {
            Parse(k_SparseAccessorsGltf, AssertSparseAccessorsResult);
        }

        [Test]
        public void MeshTargetNames()
        {
            Parse(k_MeshTargetNamesGltf, AssertMeshTargetNamesResult);
        }

        [Test]
        public void MinMagFilter()
        {
            Parse(k_MinMagFilter, AssertMinMagFilterResult);
        }

        [Test]
        public void UnknownNodeExtension()
        {
            Parse(k_UnknownNodeExtension, AssertUnknownNodeExtensionResultStrict);
        }

        [Test]
        public void UnknownTextureExtension()
        {
            Parse(k_UnknownTextureExtension, AssertUnknownTextureExtensionResultStrict);
        }

        [Test]
        public void ParseGarbage()
        {
            Assert.Throws<JsonException>(() => Parse("", Assert.IsNull));
            Assert.Throws<JsonException>(() => Parse("garbage", Assert.IsNull));
        }

        [Test]
        public void Camera()
        {
            var camera = JsonSerializer.Deserialize(
                @"{""perspective"":{},""type"":""perspective""}", GltfJsonContext.Default.Camera);
            Assert.AreEqual(null, camera.Perspective.Zfar);

            camera = JsonSerializer.Deserialize(
                @"{""perspective"":{""zfar"":42},""type"":""perspective""}", GltfJsonContext.Default.Camera);
            Assert.AreEqual(42f, camera.Perspective.Zfar);
        }

        static void Parse(string gltf, Action<Root> validationCallback)
        {
            var root = JsonSerializer.Deserialize(gltf, GltfJsonContext.Default.Root);
            validationCallback(root);
        }

        static void AssertMaterialExtensionResult(Root gltf)
        {
            Assert.NotNull(gltf);
            Assert.NotNull(gltf.Materials, "No materials");
            Assert.AreEqual(10, gltf.Materials.Count, "Invalid material quantity");

            var none = gltf.Materials[0];
            Assert.NotNull(none);
            Assert.AreEqual("noExtension", none.Name);
            Assert.IsNull(none.Extensions);

            var empty = gltf.Materials[1];
            Assert.NotNull(empty);
            Assert.AreEqual("emptyExtension", empty.Name);
            Assert.NotNull(empty.Extensions);
            Assert.IsNull(empty.Extensions.Unlit);
            Assert.IsNull(empty.Extensions.PbrSpecularGlossiness);
            Assert.IsNull(empty.Extensions.Clearcoat);
            Assert.IsNull(empty.Extensions.Sheen);
            Assert.IsNull(empty.Extensions.Transmission);
            Assert.IsNull(empty.Extensions.IndexOfRefraction);
            Assert.IsNull(empty.Extensions.Specular);

            var unlit = gltf.Materials[2];
            Assert.NotNull(unlit);
            Assert.AreEqual("unlit", unlit.Name);
            Assert.NotNull(unlit.Extensions);
            Assert.NotNull(unlit.Extensions.Unlit);
            Assert.IsNull(unlit.Extensions.PbrSpecularGlossiness);
            Assert.IsNull(unlit.Extensions.Clearcoat);
            Assert.IsNull(unlit.Extensions.Sheen);
            Assert.IsNull(unlit.Extensions.Transmission);
            Assert.IsNull(unlit.Extensions.IndexOfRefraction);
            Assert.IsNull(unlit.Extensions.Specular);

            var specGloss = gltf.Materials[3];
            Assert.NotNull(specGloss);
            Assert.AreEqual("specularGlossiness", specGloss.Name);
            Assert.NotNull(specGloss.Extensions);
            Assert.IsNull(specGloss.Extensions.Unlit);
            Assert.NotNull(specGloss.Extensions.PbrSpecularGlossiness);
            Assert.IsNull(specGloss.Extensions.Clearcoat);
            Assert.IsNull(specGloss.Extensions.Sheen);
            Assert.IsNull(specGloss.Extensions.Transmission);
            Assert.IsNull(specGloss.Extensions.IndexOfRefraction);
            Assert.IsNull(specGloss.Extensions.Specular);

            var transmission = gltf.Materials[4];
            Assert.NotNull(transmission);
            Assert.AreEqual("transmission", transmission.Name);
            Assert.NotNull(transmission.Extensions);
            Assert.IsNull(transmission.Extensions.Unlit);
            Assert.IsNull(transmission.Extensions.PbrSpecularGlossiness);
            Assert.IsNull(transmission.Extensions.Clearcoat);
            Assert.IsNull(transmission.Extensions.Sheen);
            Assert.NotNull(transmission.Extensions.Transmission);
            Assert.IsNull(transmission.Extensions.IndexOfRefraction);
            Assert.IsNull(transmission.Extensions.Specular);

            var clearcoat = gltf.Materials[5];
            Assert.NotNull(clearcoat);
            Assert.AreEqual("clearcoat", clearcoat.Name);
            Assert.NotNull(clearcoat.Extensions);
            Assert.IsNull(clearcoat.Extensions.Unlit);
            Assert.IsNull(clearcoat.Extensions.PbrSpecularGlossiness);
            Assert.NotNull(clearcoat.Extensions.Clearcoat);
            Assert.IsNull(clearcoat.Extensions.Sheen);
            Assert.IsNull(clearcoat.Extensions.Transmission);
            Assert.IsNull(clearcoat.Extensions.IndexOfRefraction);
            Assert.IsNull(clearcoat.Extensions.Specular);

            var sheen = gltf.Materials[6];
            Assert.NotNull(sheen);
            Assert.AreEqual("sheen", sheen.Name);
            Assert.NotNull(sheen.Extensions);
            Assert.IsNull(sheen.Extensions.Unlit);
            Assert.IsNull(sheen.Extensions.PbrSpecularGlossiness);
            Assert.IsNull(sheen.Extensions.Clearcoat);
            Assert.NotNull(sheen.Extensions.Sheen);
            Assert.IsNull(sheen.Extensions.Transmission);
            Assert.IsNull(sheen.Extensions.IndexOfRefraction);
            Assert.IsNull(sheen.Extensions.Specular);


            var ior = gltf.Materials[7];
            Assert.NotNull(ior);
            Assert.AreEqual("ior", ior.Name);
            Assert.NotNull(ior.Extensions);
            Assert.IsNull(ior.Extensions.Unlit);
            Assert.IsNull(ior.Extensions.PbrSpecularGlossiness);
            Assert.IsNull(ior.Extensions.Clearcoat);
            Assert.IsNull(ior.Extensions.Sheen);
            Assert.IsNull(ior.Extensions.Transmission);
            Assert.NotNull(ior.Extensions.IndexOfRefraction);
            Assert.IsNull(ior.Extensions.Specular);

            var specular = gltf.Materials[8];
            Assert.NotNull(specular);
            Assert.AreEqual("specular", specular.Name);
            Assert.NotNull(specular.Extensions);
            Assert.IsNull(specular.Extensions.Unlit);
            Assert.IsNull(specular.Extensions.PbrSpecularGlossiness);
            Assert.IsNull(specular.Extensions.Clearcoat);
            Assert.IsNull(specular.Extensions.Sheen);
            Assert.IsNull(specular.Extensions.Transmission);
            Assert.IsNull(specular.Extensions.IndexOfRefraction);
            Assert.NotNull(specular.Extensions.Specular);

            var all = gltf.Materials[9];
            Assert.NotNull(all);
            Assert.AreEqual("all", all.Name);
            Assert.NotNull(all.Extensions);
            Assert.NotNull(all.Extensions.Unlit);
            Assert.NotNull(all.Extensions.PbrSpecularGlossiness);
            Assert.NotNull(all.Extensions.Clearcoat);
            Assert.NotNull(all.Extensions.Sheen);
            Assert.NotNull(all.Extensions.Transmission);
        }

        static void AssertSparseAccessorsResult(Root gltf)
        {
            Assert.NotNull(gltf);
            Assert.NotNull(gltf.Accessors, "No accessors");
            Assert.AreEqual(3, gltf.Accessors.Count, "Invalid accessor quantity");

            var regular = gltf.Accessors[0];
            Assert.NotNull(regular);
            Assert.IsNull(regular.Sparse);

            var sparse = gltf.Accessors[1];
            Assert.NotNull(sparse);
            Assert.AreEqual(14, sparse.Count);
            Assert.NotNull(sparse.Sparse);
            Assert.AreEqual(3, sparse.Sparse.Count);
            Assert.NotNull(sparse.Sparse.Indices);
            Assert.AreEqual(2, sparse.Sparse.Indices.BufferView);
            Assert.AreEqual(0, sparse.Sparse.Indices.ByteOffset);
            Assert.AreEqual(AccessorDataType.UnsignedShort, sparse.Sparse.Indices.ComponentType);
            Assert.NotNull(sparse.Sparse.Values);
            Assert.AreEqual(3, sparse.Sparse.Values.BufferView);
            Assert.AreEqual(0, sparse.Sparse.Values.ByteOffset);

            var invalid = gltf.Accessors[2];
            Assert.NotNull(invalid);
            Assert.NotNull(invalid.Sparse);
            Assert.IsNull(invalid.Sparse.Indices);
            Assert.IsNull(invalid.Sparse.Values);
        }

        static void AssertMeshTargetNamesResult(Root gltf)
        {
            Assert.NotNull(gltf);
            Assert.NotNull(gltf.Meshes, "No materials");
            Assert.AreEqual(2, gltf.Meshes.Count, "Invalid materials quantity");

            var mat = gltf.Meshes[0];
            Assert.NotNull(mat);
            Assert.NotNull(mat.Extras);
            Assert.NotNull(mat.Extras.TargetNames);
            Assert.NotNull(mat.Extras.TargetNames);
            Assert.AreEqual(2, mat.Extras.TargetNames.Count, "Invalid targetNames quantity");
            Assert.AreEqual("Key 1", mat.Extras.TargetNames[0]);
            Assert.AreEqual("Key 2", mat.Extras.TargetNames[1]);

            mat = gltf.Meshes[1];
            Assert.NotNull(mat);
            Assert.NotNull(mat.Extras);
            Assert.IsNull(mat.Extras.TargetNames);
        }

        static void AssertMinMagFilterResult(Root gltf)
        {
            Assert.NotNull(gltf);
            Assert.NotNull(gltf.Samplers, "No samplers");
            Assert.AreEqual(8, gltf.Samplers.Count, "Invalid samplers quantity");

            var sampler0 = gltf.Samplers[0];
            Assert.NotNull(sampler0);
            Assert.AreEqual(MagFilterMode.Undefined, sampler0.MagFilter);
            Assert.AreEqual(MinFilterMode.Undefined, sampler0.MinFilter);

            var sampler1 = gltf.Samplers[1];
            Assert.NotNull(sampler1);
            Assert.AreEqual((MagFilterMode)100, sampler1.MagFilter);
            Assert.AreEqual((MinFilterMode)100, sampler1.MinFilter);

            var sampler2 = gltf.Samplers[2];
            Assert.NotNull(sampler2);
            Assert.AreEqual(MagFilterMode.Nearest, sampler2.MagFilter);
            Assert.AreEqual(MinFilterMode.Nearest, sampler2.MinFilter);

            var sampler3 = gltf.Samplers[3];
            Assert.NotNull(sampler3);
            Assert.AreEqual(MagFilterMode.Linear, sampler3.MagFilter);
            Assert.AreEqual(MinFilterMode.Linear, sampler3.MinFilter);

            var sampler4 = gltf.Samplers[4];
            Assert.NotNull(sampler4);
            Assert.AreEqual(MagFilterMode.Undefined, sampler4.MagFilter);
            Assert.AreEqual(MinFilterMode.NearestMipmapNearest, sampler4.MinFilter);

            var sampler5 = gltf.Samplers[5];
            Assert.NotNull(sampler5);
            Assert.AreEqual(MagFilterMode.Undefined, sampler5.MagFilter);
            Assert.AreEqual(MinFilterMode.LinearMipmapNearest, sampler5.MinFilter);

            var sampler6 = gltf.Samplers[6];
            Assert.NotNull(sampler6);
            Assert.AreEqual(MagFilterMode.Undefined, sampler6.MagFilter);
            Assert.AreEqual(MinFilterMode.NearestMipmapLinear, sampler6.MinFilter);

            var sampler7 = gltf.Samplers[7];
            Assert.NotNull(sampler7);
            Assert.AreEqual(MagFilterMode.Undefined, sampler7.MagFilter);
            Assert.AreEqual(MinFilterMode.LinearMipmapLinear, sampler7.MinFilter);
        }

        static void AssertUnknownNodeExtensionResult(Root gltf)
        {
            AssertUnknownNodeExtensionResult(gltf, true);
        }

        static void AssertUnknownNodeExtensionResultStrict(Root gltf)
        {
            AssertUnknownNodeExtensionResult(gltf, false);
        }

        static void AssertUnknownNodeExtensionResult(Root gltf, bool discardEmptyExtensions)
        {
            Assert.NotNull(gltf);
            Assert.NotNull(gltf.Nodes, "No nodes");
            Assert.AreEqual(6, gltf.Nodes.Count, "Invalid nodes quantity");

            var node0 = gltf.Nodes[0];
            Assert.NotNull(node0);
            Assert.IsNull(node0.Extensions);

            var node1 = gltf.Nodes[1];
            Assert.NotNull(node1);
            if (discardEmptyExtensions)
                Assert.IsNull(node1.Extensions);
            else
                Assert.NotNull(node1.Extensions);

            var node2 = gltf.Nodes[2];
            Assert.NotNull(node2);
            if (discardEmptyExtensions)
                Assert.IsNull(node2.Extensions);
            else
            {
                Assert.NotNull(node2.Extensions);
                if (node2.Extensions.ExtensionData.TryGetValue(
                        "MOZ_hubs_components",
                        out var mozExtensionData)
                    )
                {
                    // // TODO: Finalize checking that extension data. Include schema class either here or make it
                    // // available generally.
                    // var mozExtension = mozExtensionData.Deserialize(
                    //     MozillaHubsComponentsSourceGenerator.Default.MozillaHubsComponents
                    // );
                    // Assert.NotNull(mozExtension);
                }
                else
                {
                    Assert.Fail("Couldn't find extension MOZ_hubs_components");
                }
            }

            var node3 = gltf.Nodes[3];
            Assert.NotNull(node3);
            Assert.NotNull(node3.Extensions);
            Assert.NotNull(node3.Extensions.MeshGpuInstancing);
            Assert.NotNull(node3.Extensions.MeshGpuInstancing.Attributes);
            Assert.AreEqual(42, node3.Extensions.MeshGpuInstancing.Attributes.Translation);
            Assert.IsNull(node3.Extensions.LightsPunctual);

            var node4 = gltf.Nodes[4];
            Assert.NotNull(node4);
            Assert.NotNull(node4.Extensions);
            Assert.IsNull(node4.Extensions.MeshGpuInstancing);
            Assert.NotNull(node4.Extensions.LightsPunctual);
            Assert.AreEqual(42, node4.Extensions.LightsPunctual.Light);

            var node5 = gltf.Nodes[5];
            Assert.NotNull(node5);
            Assert.NotNull(node5.Extensions);
            Assert.NotNull(node5.Extensions.MeshGpuInstancing);
            Assert.NotNull(node5.Extensions.MeshGpuInstancing.Attributes);
            Assert.AreEqual(13, node5.Extensions.MeshGpuInstancing.Attributes.Translation);
            Assert.NotNull(node5.Extensions.LightsPunctual);
            Assert.AreEqual(42, node5.Extensions.LightsPunctual.Light);
        }

        static void AssertUnknownTextureExtensionResult(Root gltf)
        {
            AssertUnknownTextureExtensionResult(gltf, true);
        }

        static void AssertUnknownTextureExtensionResultStrict(Root gltf)
        {
            AssertUnknownTextureExtensionResult(gltf, false);
        }

        static void AssertUnknownTextureExtensionResult(Root gltf, bool discardEmptyExtensions)
        {
            Assert.NotNull(gltf);
            Assert.NotNull(gltf.Textures, "No textures");
            Assert.AreEqual(5, gltf.Textures.Count, "Invalid texture quantity");

            var texture0 = gltf.Textures[0];
            Assert.NotNull(texture0);
            Assert.IsNull(texture0.Extensions);

            var texture1 = gltf.Textures[1];
            Assert.NotNull(texture1);
            if (discardEmptyExtensions)
                Assert.IsNull(texture1.Extensions);
            else
                Assert.NotNull(texture1.Extensions);

            var texture2 = gltf.Textures[2];
            Assert.NotNull(texture2);
            if (discardEmptyExtensions)
                Assert.IsNull(texture2.Extensions);
            else
                Assert.NotNull(texture2.Extensions);

            var texture3 = gltf.Textures[3];
            Assert.NotNull(texture3);
            Assert.NotNull(texture3.Extensions);
            Assert.NotNull(texture3.Extensions.BasisU);
            Assert.AreEqual(42, texture3.Extensions.BasisU.Source);

            var texture4 = gltf.Textures[4];
            Assert.NotNull(texture4);
            Assert.NotNull(texture4.Extensions);
            Assert.NotNull(texture4.Extensions.BasisU);
            Assert.AreEqual(42, texture4.Extensions.BasisU.Source);
        }
    }
}
