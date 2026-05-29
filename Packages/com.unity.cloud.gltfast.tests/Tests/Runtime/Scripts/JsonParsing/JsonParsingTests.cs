// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using GLTFast.Schema;
using NUnit.Framework;
using Unity.Gltfast.Text.Json;
using UnityEngine;

namespace GLTFast.Tests.JsonParsing
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

        static void Parse(string gltf, Action<Root> validationCallback)
        {
            var root = JsonSerializer.Deserialize(gltf, GltfRootSourceGenerator.Default.Root);
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
            Assert.IsNull(empty.Extensions.KHR_materials_unlit);
            Assert.IsNull(empty.Extensions.KHR_materials_pbrSpecularGlossiness);
            Assert.IsNull(empty.Extensions.KHR_materials_clearcoat);
            Assert.IsNull(empty.Extensions.KHR_materials_sheen);
            Assert.IsNull(empty.Extensions.KHR_materials_transmission);
            Assert.IsNull(empty.Extensions.KHR_materials_ior);
            Assert.IsNull(empty.Extensions.KHR_materials_specular);

            var unlit = gltf.Materials[2];
            Assert.NotNull(unlit);
            Assert.AreEqual("unlit", unlit.Name);
            Assert.NotNull(unlit.Extensions);
            Assert.NotNull(unlit.Extensions.KHR_materials_unlit);
            Assert.IsNull(unlit.Extensions.KHR_materials_pbrSpecularGlossiness);
            Assert.IsNull(unlit.Extensions.KHR_materials_clearcoat);
            Assert.IsNull(unlit.Extensions.KHR_materials_sheen);
            Assert.IsNull(unlit.Extensions.KHR_materials_transmission);
            Assert.IsNull(unlit.Extensions.KHR_materials_ior);
            Assert.IsNull(unlit.Extensions.KHR_materials_specular);

            var specGloss = gltf.Materials[3];
            Assert.NotNull(specGloss);
            Assert.AreEqual("specularGlossiness", specGloss.Name);
            Assert.NotNull(specGloss.Extensions);
            Assert.IsNull(specGloss.Extensions.KHR_materials_unlit);
            Assert.NotNull(specGloss.Extensions.KHR_materials_pbrSpecularGlossiness);
            Assert.IsNull(specGloss.Extensions.KHR_materials_clearcoat);
            Assert.IsNull(specGloss.Extensions.KHR_materials_sheen);
            Assert.IsNull(specGloss.Extensions.KHR_materials_transmission);
            Assert.IsNull(specGloss.Extensions.KHR_materials_ior);
            Assert.IsNull(specGloss.Extensions.KHR_materials_specular);

            var transmission = gltf.Materials[4];
            Assert.NotNull(transmission);
            Assert.AreEqual("transmission", transmission.Name);
            Assert.NotNull(transmission.Extensions);
            Assert.IsNull(transmission.Extensions.KHR_materials_unlit);
            Assert.IsNull(transmission.Extensions.KHR_materials_pbrSpecularGlossiness);
            Assert.IsNull(transmission.Extensions.KHR_materials_clearcoat);
            Assert.IsNull(transmission.Extensions.KHR_materials_sheen);
            Assert.NotNull(transmission.Extensions.KHR_materials_transmission);
            Assert.IsNull(transmission.Extensions.KHR_materials_ior);
            Assert.IsNull(transmission.Extensions.KHR_materials_specular);

            var clearcoat = gltf.Materials[5];
            Assert.NotNull(clearcoat);
            Assert.AreEqual("clearcoat", clearcoat.Name);
            Assert.NotNull(clearcoat.Extensions);
            Assert.IsNull(clearcoat.Extensions.KHR_materials_unlit);
            Assert.IsNull(clearcoat.Extensions.KHR_materials_pbrSpecularGlossiness);
            Assert.NotNull(clearcoat.Extensions.KHR_materials_clearcoat);
            Assert.IsNull(clearcoat.Extensions.KHR_materials_sheen);
            Assert.IsNull(clearcoat.Extensions.KHR_materials_transmission);
            Assert.IsNull(clearcoat.Extensions.KHR_materials_ior);
            Assert.IsNull(clearcoat.Extensions.KHR_materials_specular);

            var sheen = gltf.Materials[6];
            Assert.NotNull(sheen);
            Assert.AreEqual("sheen", sheen.Name);
            Assert.NotNull(sheen.Extensions);
            Assert.IsNull(sheen.Extensions.KHR_materials_unlit);
            Assert.IsNull(sheen.Extensions.KHR_materials_pbrSpecularGlossiness);
            Assert.IsNull(sheen.Extensions.KHR_materials_clearcoat);
            Assert.NotNull(sheen.Extensions.KHR_materials_sheen);
            Assert.IsNull(sheen.Extensions.KHR_materials_transmission);
            Assert.IsNull(sheen.Extensions.KHR_materials_ior);
            Assert.IsNull(sheen.Extensions.KHR_materials_specular);


            var ior = gltf.Materials[7];
            Assert.NotNull(ior);
            Assert.AreEqual("ior", ior.Name);
            Assert.NotNull(ior.Extensions);
            Assert.IsNull(ior.Extensions.KHR_materials_unlit);
            Assert.IsNull(ior.Extensions.KHR_materials_pbrSpecularGlossiness);
            Assert.IsNull(ior.Extensions.KHR_materials_clearcoat);
            Assert.IsNull(ior.Extensions.KHR_materials_sheen);
            Assert.IsNull(ior.Extensions.KHR_materials_transmission);
            Assert.NotNull(ior.Extensions.KHR_materials_ior);
            Assert.IsNull(ior.Extensions.KHR_materials_specular);

            var specular = gltf.Materials[8];
            Assert.NotNull(specular);
            Assert.AreEqual("specular", specular.Name);
            Assert.NotNull(specular.Extensions);
            Assert.IsNull(specular.Extensions.KHR_materials_unlit);
            Assert.IsNull(specular.Extensions.KHR_materials_pbrSpecularGlossiness);
            Assert.IsNull(specular.Extensions.KHR_materials_clearcoat);
            Assert.IsNull(specular.Extensions.KHR_materials_sheen);
            Assert.IsNull(specular.Extensions.KHR_materials_transmission);
            Assert.IsNull(specular.Extensions.KHR_materials_ior);
            Assert.NotNull(specular.Extensions.KHR_materials_specular);

            var all = gltf.Materials[9];
            Assert.NotNull(all);
            Assert.AreEqual("all", all.Name);
            Assert.NotNull(all.Extensions);
            Assert.NotNull(all.Extensions.KHR_materials_unlit);
            Assert.NotNull(all.Extensions.KHR_materials_pbrSpecularGlossiness);
            Assert.NotNull(all.Extensions.KHR_materials_clearcoat);
            Assert.NotNull(all.Extensions.KHR_materials_sheen);
            Assert.NotNull(all.Extensions.KHR_materials_transmission);
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
            Assert.AreEqual(GltfComponentType.UnsignedShort, sparse.Sparse.Indices.ComponentType);
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
            Assert.AreEqual(2, mat.Extras.TargetNames.Length, "Invalid targetNames quantity");
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
            Assert.AreEqual(Sampler.MagFilterMode.None, sampler0.magFilter);
            Assert.AreEqual(Sampler.MinFilterMode.None, sampler0.minFilter);

            var sampler1 = gltf.Samplers[1];
            Assert.NotNull(sampler1);
            Assert.AreEqual((Sampler.MagFilterMode)100, sampler1.magFilter);
            Assert.AreEqual((Sampler.MinFilterMode)100, sampler1.minFilter);

            var sampler2 = gltf.Samplers[2];
            Assert.NotNull(sampler2);
            Assert.AreEqual(Sampler.MagFilterMode.Nearest, sampler2.magFilter);
            Assert.AreEqual(Sampler.MinFilterMode.Nearest, sampler2.minFilter);

            var sampler3 = gltf.Samplers[3];
            Assert.NotNull(sampler3);
            Assert.AreEqual(Sampler.MagFilterMode.Linear, sampler3.magFilter);
            Assert.AreEqual(Sampler.MinFilterMode.Linear, sampler3.minFilter);

            var sampler4 = gltf.Samplers[4];
            Assert.NotNull(sampler4);
            Assert.AreEqual(Sampler.MagFilterMode.None, sampler4.magFilter);
            Assert.AreEqual(Sampler.MinFilterMode.NearestMipmapNearest, sampler4.minFilter);

            var sampler5 = gltf.Samplers[5];
            Assert.NotNull(sampler5);
            Assert.AreEqual(Sampler.MagFilterMode.None, sampler5.magFilter);
            Assert.AreEqual(Sampler.MinFilterMode.LinearMipmapNearest, sampler5.minFilter);

            var sampler6 = gltf.Samplers[6];
            Assert.NotNull(sampler6);
            Assert.AreEqual(Sampler.MagFilterMode.None, sampler6.magFilter);
            Assert.AreEqual(Sampler.MinFilterMode.NearestMipmapLinear, sampler6.minFilter);

            var sampler7 = gltf.Samplers[7];
            Assert.NotNull(sampler7);
            Assert.AreEqual(Sampler.MagFilterMode.None, sampler7.magFilter);
            Assert.AreEqual(Sampler.MinFilterMode.LinearMipmapLinear, sampler7.minFilter);
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
                if (node2.Extensions.ExtensionsData.TryGetValue(
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
            Assert.NotNull(node3.Extensions.EXT_mesh_gpu_instancing);
            Assert.NotNull(node3.Extensions.EXT_mesh_gpu_instancing.attributes);
            Assert.AreEqual(42, node3.Extensions.EXT_mesh_gpu_instancing.attributes.TRANSLATION);
            Assert.IsNull(node3.Extensions.KHR_lights_punctual);

            var node4 = gltf.Nodes[4];
            Assert.NotNull(node4);
            Assert.NotNull(node4.Extensions);
            Assert.IsNull(node4.Extensions.EXT_mesh_gpu_instancing);
            Assert.NotNull(node4.Extensions.KHR_lights_punctual);
            Assert.AreEqual(42, node4.Extensions.KHR_lights_punctual.light);

            var node5 = gltf.Nodes[5];
            Assert.NotNull(node5);
            Assert.NotNull(node5.Extensions);
            Assert.NotNull(node5.Extensions.EXT_mesh_gpu_instancing);
            Assert.NotNull(node5.Extensions.EXT_mesh_gpu_instancing.attributes);
            Assert.AreEqual(13, node5.Extensions.EXT_mesh_gpu_instancing.attributes.TRANSLATION);
            Assert.NotNull(node5.Extensions.KHR_lights_punctual);
            Assert.AreEqual(42, node5.Extensions.KHR_lights_punctual.light);
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
            Assert.NotNull(texture3.Extensions.KHR_texture_basisu);
            Assert.AreEqual(42, texture3.Extensions.KHR_texture_basisu.source);

            var texture4 = gltf.Textures[4];
            Assert.NotNull(texture4);
            Assert.NotNull(texture4.Extensions);
            Assert.NotNull(texture4.Extensions.KHR_texture_basisu);
            Assert.AreEqual(42, texture4.Extensions.KHR_texture_basisu.source);
        }
    }
}
