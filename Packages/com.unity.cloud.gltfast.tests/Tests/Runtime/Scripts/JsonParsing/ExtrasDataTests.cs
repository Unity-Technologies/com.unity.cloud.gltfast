// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Cloud.Gltfast.Schema;
using Unity.Gltfast.Text.Json;

namespace Unity.Cloud.Gltfast.Tests.JsonParsing
{
    [Category("JsonParsing")]
    class ExtrasDataTests
    {
        const string k_ExtrasDataJson = @"
{
    ""asset"" : {
        ""copyright"" : ""(c) 2022 Andreas Atteneder, CC BY 4.0."",
        ""generator"" : ""Khronos glTF Blender I/O v3.3.27"",
        ""version"" : ""2.0""
    },
    ""scene"" : 0,
    ""scenes"" : [
        {
            ""name"" : ""Scene"",
            ""nodes"" : [
                0
            ]
        }
    ],
    ""nodes"" : [
        {
            ""extras"" : {
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
            },
            ""mesh"" : 0,
            ""name"" : ""Cube""
        }
    ],
    ""materials"" : [
        {
            ""doubleSided"" : true,
            ""name"" : ""Material"",
            ""pbrMetallicRoughness"" : {
                ""baseColorFactor"" : [
                    0.800000011920929,
                    0.800000011920929,
                    0.800000011920929,
                    1
                ],
                ""metallicFactor"" : 0,
                ""roughnessFactor"" : 0.5
            }
        }
    ],
    ""meshes"" : [
        {
            ""name"" : ""Cube"",
            ""primitives"" : [
                {
                    ""attributes"" : {
                        ""POSITION"" : 0,
                        ""NORMAL"" : 1
                    },
                    ""indices"" : 2,
                    ""material"" : 0
                }
            ]
        }
    ],
    ""accessors"" : [
        {
            ""bufferView"" : 0,
            ""componentType"" : 5126,
            ""count"" : 3,
            ""max"" : [
                0,
                2,
                1
            ],
            ""min"" : [
                0,
                0,
                -1
            ],
            ""type"" : ""VEC3""
        },
        {
            ""bufferView"" : 1,
            ""componentType"" : 5126,
            ""count"" : 3,
            ""type"" : ""VEC3""
        },
        {
            ""bufferView"" : 2,
            ""componentType"" : 5123,
            ""count"" : 3,
            ""type"" : ""SCALAR""
        }
    ],
    ""bufferViews"" : [
        {
            ""buffer"" : 0,
            ""byteLength"" : 36,
            ""byteOffset"" : 0,
            ""target"" : 34962
        },
        {
            ""buffer"" : 0,
            ""byteLength"" : 36,
            ""byteOffset"" : 36,
            ""target"" : 34962
        },
        {
            ""buffer"" : 0,
            ""byteLength"" : 6,
            ""byteOffset"" : 72,
            ""target"" : 34963
        }
    ],
    ""buffers"" : [
        {
            ""byteLength"" : 80,
            ""uri"" : ""ExtrasData.bin""
        }
    ]
}
";

        [Test]
        public void ExtrasDataTest()
        {
            var gltf = JsonSerializer.Deserialize(k_ExtrasDataJson, GltfJsonContext.Default.Root);

            Assert.NotNull(gltf);
            Assert.NotNull(gltf.Nodes);
            Assert.GreaterOrEqual(gltf.Nodes.Count, 1);
            Assert.NotNull(gltf.Nodes[0]);

            AssertResultExtendedJson(gltf);
        }

        static void AssertResultExtendedJson(Root gltf)
        {
            Assert.NotNull(gltf);
            var e = gltf.Nodes[0].Extras;
            Assert.IsNotNull(e);

            Assert.IsTrue(e.TryGetValue("floatProp", out float floatProp));
            Assert.NotNull(floatProp);
            Assert.AreEqual(3.4700000286102295f, floatProp, "JSON value mismatch");

            Assert.IsTrue(e.TryGetValue("intProp", out int intProp));
            Assert.NotNull(intProp);
            Assert.AreEqual(42, intProp, "JSON value mismatch");

            Assert.IsTrue(e.TryGetValue("stringProp", out string stringProp));
            Assert.NotNull(stringProp);
            Assert.AreEqual("Yadiya", stringProp, "JSON value mismatch");

            Assert.IsTrue(e.TryGetValue("eulerAngles", out float[] eulerValues));
            Assert.AreEqual(3, eulerValues.Length);
            Assert.AreEqual(1.0f, eulerValues[0]);
            Assert.AreEqual(1.0f, eulerValues[1]);
            Assert.AreEqual(1.0f, eulerValues[2]);

            Assert.IsTrue(e.TryGetValue("intArrayProp", out int[] intValues));
            Assert.AreEqual(3, intValues.Length);
            Assert.AreEqual(1, intValues[0]);
            Assert.AreEqual(1, intValues[1]);
            Assert.AreEqual(1, intValues[2]);

            Assert.IsTrue(e.TryGetValue("rotation", out float[] rotationValues));
            Assert.AreEqual(4, rotationValues.Length);
            Assert.AreEqual(0.8199999928474426f, rotationValues[0]);
            Assert.AreEqual(0.8199999928474426f, rotationValues[1]);
            Assert.AreEqual(0.8199999928474426f, rotationValues[2]);
            Assert.AreEqual(0.8199999928474426f, rotationValues[3]);

            Assert.IsTrue(e.TryGetValue("color", out float[] colorValues));
            Assert.AreEqual(4, colorValues.Length);
            Assert.AreEqual(1.0f, colorValues[0]);
            Assert.AreEqual(1.0f, colorValues[1]);
            Assert.AreEqual(1.0f, colorValues[2]);
            Assert.AreEqual(1.0f, colorValues[3]);


            Assert.AreEqual(7, e.Count);
            var fp = e["floatProp"];
            Assert.AreEqual(ValueKind.Number, fp.Kind);
            Assert.IsTrue(fp.TryGetDouble(out var floatValue));
            Assert.AreEqual(3.4700000286102295, floatValue);

            var iav = e["intArrayProp"];
            Assert.AreEqual(ValueKind.Array, iav.Kind);
            var arrayValue = iav[0];
            Assert.AreEqual(ValueKind.Number, arrayValue.Kind);
            Assert.IsTrue(arrayValue.TryGetInt64(out var intValue));
            Assert.AreEqual(1, intValue);

            Assert.IsTrue(iav.TryGetValue<int[]>(out var intArray));
            Assert.NotNull(intArray);
            Assert.IsFalse(iav.TryGetValue<string[]>(out _));
        }

        [Test]
        public void ObjectTest()
        {
            const string json = @"
{
    ""nodes"" : [
        {
            ""extras"" : {
                ""objValue"": {
                    ""intProp"" : 42,
                    ""nested"" : {
                        ""intProp"" : 43
                    }
                }
            }
        }
    ]
}
";

            var gltf = JsonSerializer.Deserialize(json, GltfJsonContext.Default.Root);
            var e = gltf?.Nodes?[0]?.Extras;
            Assert.NotNull(e);

            var obj = e["objValue"];
            Assert.AreEqual(ValueKind.Object, obj.Kind);
            Assert.IsTrue(obj["intProp"].TryGetInt64(out var intValue));
            Assert.AreEqual(42, intValue);

            var nested = obj["nested"];
            Assert.AreEqual(ValueKind.Object, nested.Kind);
            Assert.IsTrue(nested["intProp"].TryGetInt64(out var nestedIntValue));
            Assert.AreEqual(43, nestedIntValue);

            Assert.IsFalse(e.ContainsKey("foo"));
            Assert.Throws<KeyNotFoundException>(() => _ = e["foo"]);
        }

        static AdditionalPropertyContainer DeserializeNodeExtras(string extrasJson)
        {
            var json = $@"{{""nodes"":[{{""extras"":{extrasJson}}}]}}";
            var gltf = JsonSerializer.Deserialize(json, GltfJsonContext.Default.Root);
            var e = gltf?.Nodes?[0]?.Extras;
            Assert.NotNull(e);
            return e;
        }

        [Test]
        public void StringValueTest()
        {
            var e = DeserializeNodeExtras(@"{""stringProp"":""Yadiya""}");
            var sp = e["stringProp"];
            Assert.AreEqual(ValueKind.String, sp.Kind);
            Assert.AreEqual("Yadiya", sp.GetString());
        }

        [Test]
        public void BooleanValueTest()
        {
            var e = DeserializeNodeExtras(@"{""t"":true,""f"":false}");

            var t = e["t"];
            Assert.AreEqual(ValueKind.True, t.Kind);
            Assert.IsTrue(t.GetBoolean());

            var f = e["f"];
            Assert.AreEqual(ValueKind.False, f.Kind);
            Assert.IsFalse(f.GetBoolean());
        }

        [Test]
        public void NullValueTest()
        {
            var e = DeserializeNodeExtras(@"{""n"":null}");
            var n = e["n"];
            Assert.AreEqual(ValueKind.Null, n.Kind);
            Assert.IsNull(n.GetString());
            Assert.IsTrue(n.TryGetValue(out string stringValue));
            Assert.IsNull(stringValue);
        }

        [Test]
        public void ArrayLengthTest()
        {
            var e = DeserializeNodeExtras(@"{""a"":[1,2,3],""b"":[]}");
            Assert.AreEqual(3, e["a"].ArrayLength);
            Assert.AreEqual(0, e["b"].ArrayLength);
        }

        [Test]
        public void ValueTryGetValueTest()
        {
            var e = DeserializeNodeExtras(@"{""obj"":{""intProp"":42}}");
            var obj = e["obj"];

            Assert.IsTrue(obj.TryGetValue("intProp", out var value));
            Assert.IsTrue(value.TryGetInt64(out var intValue));
            Assert.AreEqual(42, intValue);

            Assert.IsFalse(obj.TryGetValue("missing", out _));
        }

        [Test]
        public void EnumerateObjectTest()
        {
            var e = DeserializeNodeExtras(@"{""obj"":{""intProp"":42,""stringProp"":""foo""}}");
            var obj = e["obj"];

            var keys = new List<string>();
            foreach (var property in obj.EnumerateObject())
            {
                keys.Add(property.Key);
                if (property.Key == "intProp")
                {
                    Assert.IsTrue(property.Value.TryGetInt64(out var intValue));
                    Assert.AreEqual(42, intValue);
                }
            }
            CollectionAssert.AreEquivalent(new[] { "intProp", "stringProp" }, keys);
        }

        [Test]
        public void ContainerEnumeratorTest()
        {
            var e = DeserializeNodeExtras(@"{""intProp"":42,""stringProp"":""foo""}");

            var keys = new List<string>();
            foreach (var property in e)
            {
                keys.Add(property.Key);
                if (property.Key == "intProp")
                {
                    Assert.IsTrue(property.Value.TryGetInt64(out var intValue));
                    Assert.AreEqual(42, intValue);
                }
            }
            CollectionAssert.AreEquivalent(new[] { "intProp", "stringProp" }, keys);
        }

        [Test]
        public void ContainerEnumeratorEmpty()
        {
            var e = DeserializeNodeExtras("{}");
            foreach (var _ in e)
            {
                Assert.Fail("Expected no properties.");
            }
        }


        [Test]
        public void SetAndSerializePrimitives()
        {
            var node = new Node { Extras = new AdditionalPropertyContainer() };
            var extras = node.Extras;
            extras.Set("stringProp", "Yadiya");
            extras.Set("intProp", 42L);
            extras.Set("floatProp", 3.47);
            extras.Set("boolProp", true);
            extras.Set("intArrayProp", new[] { 1, 2, 3 });

            var json = JsonSerializer.Serialize(node, GltfJsonContext.Default.Node);
            var restored = JsonSerializer.Deserialize(json, GltfJsonContext.Default.Node);

            var e = restored.Extras;
            Assert.IsNotNull(e);
            Assert.AreEqual(5, e.Count);

            Assert.IsTrue(e.TryGetValue("stringProp", out string stringProp));
            Assert.AreEqual("Yadiya", stringProp);
            Assert.IsTrue(e.TryGetValue("intProp", out int intProp));
            Assert.AreEqual(42, intProp);
            Assert.IsTrue(e.TryGetValue("floatProp", out double floatProp));
            Assert.AreEqual(3.47, floatProp);
            Assert.IsTrue(e.TryGetValue("boolProp", out bool boolProp));
            Assert.IsTrue(boolProp);
            Assert.IsTrue(e.TryGetValue("intArrayProp", out int[] intArray));
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, intArray);
        }

        [Test]
        public void SetAndSerializeObject()
        {
            var node = new Node { Extras = new AdditionalPropertyContainer() };
            node.Extras.Set("meta", new MetaData { name = "foo", count = 7 });

            var json = JsonSerializer.Serialize(node, GltfJsonContext.Default.Node);
            var restored = JsonSerializer.Deserialize(json, GltfJsonContext.Default.Node);

            Assert.IsTrue(restored.Extras.TryGetValue("meta", out MetaData meta));
            Assert.AreEqual("foo", meta.name);
            Assert.AreEqual(7, meta.count);
        }

        [Test]
        public void SetOverwriteRemoveClear()
        {
            var data = new AdditionalPropertyContainer();
            data.Set("a", 1L);
            data.Set("a", 2L);
            Assert.AreEqual(1, data.Count);
            Assert.IsTrue(data.TryGetValue("a", out long a));
            Assert.AreEqual(2, a);

            data.Set("b", "x");
            Assert.AreEqual(2, data.Count);
            Assert.IsTrue(data.Remove("a"));
            Assert.IsFalse(data.Remove("missing"));
            Assert.AreEqual(1, data.Count);

            data.Clear();
            Assert.AreEqual(0, data.Count);
        }

        [Test]
        public void SetTypedOverloadsViaInterface()
        {
            // The string/long/double/bool overloads are default-interface-methods,
            // reachable only through an IPropertyContainer reference (a call on the
            // concrete type binds to the generic Set<T> instead).
            IPropertyContainer data = new AdditionalPropertyContainer();
            data.Set("s", "hello");
            data.Set("l", 5L);
            data.Set("d", 2.5);
            data.Set("b", true);

            Assert.AreEqual(4, data.Count);

            Assert.AreEqual(ValueKind.String, data["s"].Kind);
            Assert.AreEqual("hello", data["s"].GetString());

            Assert.AreEqual(ValueKind.Number, data["l"].Kind);
            Assert.IsTrue(data["l"].TryGetInt64(out var l));
            Assert.AreEqual(5, l);

            Assert.AreEqual(ValueKind.Number, data["d"].Kind);
            Assert.IsTrue(data["d"].TryGetDouble(out var d));
            Assert.AreEqual(2.5, d);

            Assert.AreEqual(ValueKind.True, data["b"].Kind);
            Assert.IsTrue(data["b"].GetBoolean());
        }

        [Test]
        public void AdditionalPropertiesCount()
        {
            var data = new AdditionalPropertyContainer();
            Assert.AreEqual(0, data.Count);
            data.Set("a", 1L);
            Assert.AreEqual(1, data.Count);
            data.Clear();
            Assert.AreEqual(0, data.Count);
        }

        [Test]
        public void AdditionalPropertiesCountEmpty()
        {
            IAdditionalPropertyContainer node = new Node();
            Assert.AreEqual(0, node.AdditionalProperties.Count);
        }

        class MetaData
        {
            public string name { get; set; }
            public int count { get; set; }
        }
    }
}
