// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Unity.Cloud.Gltfast.Objects;
using Unity.Cloud.Gltfast.Text.Json;
using Unity.Cloud.Gltfast.Text.Json.Serialization;

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

        static ExtrasContainer DeserializeNodeExtras(string extrasJson)
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
            Assert.AreEqual(ValueKind.Object, e.Kind);
        }

        [Test]
        public void UndefinedValueTryGetValueReturnsFalse()
        {
            // A Value can be Undefined without any misuse: RawValue of object-form extras, the result
            // of a failed lookup, or default. TryGetValue<T> is the only kind-agnostic member, so it
            // reports that as a failed conversion instead of throwing.
            var e = DeserializeNodeExtras(@"{""obj"":{""a"":1}}");
            Assert.AreEqual(ValueKind.Undefined, e.RawValue.Kind);
            Assert.IsFalse(e.RawValue.TryGetValue(out int _));
            Assert.IsFalse(e.RawValue.TryGetValue(out string _));

            Assert.IsFalse(e["obj"].TryGetValue("missing", out var missing));
            Assert.AreEqual(ValueKind.Undefined, missing.Kind);
            Assert.IsFalse(missing.TryGetValue(out int _));

            Assert.IsFalse(default(Value).TryGetValue(out int _));
        }

        [Test]
        public void UndefinedValueKindSpecificMembersThrow()
        {
            // Every other member mirrors JsonElement: it requires a specific kind and throws for any
            // other, Undefined included. Guarding only Undefined would be arbitrary, since the same
            // calls throw on a number or a string too.
            var e = DeserializeNodeExtras(@"{""num"":42}");
            Assert.AreEqual(ValueKind.Undefined, e.RawValue.Kind);

            Assert.Throws<InvalidOperationException>(() => e.RawValue.TryGetValue("any", out _));
            Assert.Throws<InvalidOperationException>(() => e.RawValue.GetString());
            Assert.Throws<InvalidOperationException>(() => _ = e.RawValue.ArrayLength);

            // Same operation, same exception, on a defined but wrong kind.
            Assert.Throws<InvalidOperationException>(() => e["num"].TryGetValue("any", out _));
            Assert.Throws<InvalidOperationException>(() => _ = e["num"].ArrayLength);
        }


        [Test]
        public void SetAndSerializePrimitives()
        {
            var node = new Node { Extras = new ExtrasContainer() };
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
            var node = new Node { Extras = new ExtrasContainer() };
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

        [Test]
        public void TryGetValueTypeMismatch()
        {
            var e = DeserializeNodeExtras(@"{""stringProp"":""Yadiya"",""intProp"":42}");

            Assert.IsFalse(e.TryGetValue("stringProp", out int notAnInt));
            Assert.AreEqual(default(int), notAnInt);
            Assert.IsFalse(e.TryGetValue("intProp", out int[] notAnArray));
            Assert.IsNull(notAnArray);

            // A matching type still succeeds.
            Assert.IsTrue(e.TryGetValue("intProp", out int intProp));
            Assert.AreEqual(42, intProp);
        }

        [Test]
        public void TryGetValueUnsupportedTargetType()
        {
            var e = DeserializeNodeExtras(@"{""num"":42,""obj"":{""a"":1}}");

            // Delegates are refused outright, whatever the value is.
            Assert.IsFalse(e.TryGetValue("num", out Action _));
            Assert.IsFalse(e.TryGetValue("obj", out Action _));

            // Interfaces are only refused for object values, so without this the same target type
            // would report false for one document and throw for another.
            Assert.IsFalse(e.TryGetValue("num", out IDisposable _));
            Assert.IsFalse(e.TryGetValue("obj", out IDisposable _));
        }

        [Test]
        public void RawValueTryGetValueUnsupportedTargetType()
        {
            var e = DeserializeNodeExtras("42");
            Assert.AreEqual(ValueKind.Number, e.Kind);

            Assert.IsFalse(e.RawValue.TryGetValue(out Action _));
            Assert.IsFalse(e.RawValue.TryGetValue(out IDisposable _));

            var obj = DeserializeNodeExtras(@"{""obj"":{""a"":1}}")["obj"];
            Assert.IsFalse(obj.TryGetValue(out IDisposable _));
        }

        [Test]
        public void AdditionalPropertiesTryGetValueUnsupportedTargetType()
        {
            var gltf = JsonSerializer.Deserialize(
                @"{""nodes"":[{""unknownNum"":42,""unknownObj"":{""a"":1}}]}",
                GltfJsonContext.Default.Root);

            IAdditionalPropertyContainer node = gltf.Nodes[0];
            Assert.IsFalse(node.AdditionalProperties.TryGetValue("unknownNum", out Action _));
            Assert.IsFalse(node.AdditionalProperties.TryGetValue("unknownObj", out IDisposable _));
        }

        [Test]
        public void AdditionalPropertiesTryGetValueTypeMismatch()
        {
            var gltf = JsonSerializer.Deserialize(
                @"{""nodes"":[{""unknownProp"":""Yadiya""}]}",
                GltfJsonContext.Default.Root);

            IAdditionalPropertyContainer node = gltf.Nodes[0];
            Assert.IsFalse(node.AdditionalProperties.TryGetValue("unknownProp", out int notAnInt));
            Assert.AreEqual(default(int), notAnInt);
            Assert.IsTrue(node.AdditionalProperties.TryGetValue("unknownProp", out string stringProp));
            Assert.AreEqual("Yadiya", stringProp);
        }

        // The glTF specification allows "extras" to be any JSON value, not just an object.
        // https://registry.khronos.org/glTF/specs/2.0/glTF-2.0.html#reference-extras

        [Test]
        [TestCase("42", ValueKind.Number)]
        [TestCase("-3.5", ValueKind.Number)]
        [TestCase(@"""Yadiya""", ValueKind.String)]
        [TestCase("true", ValueKind.True)]
        [TestCase("false", ValueKind.False)]
        [TestCase("[1,2,3]", ValueKind.Array)]
        [TestCase("[]", ValueKind.Array)]
        [TestCase("{}", ValueKind.Object)]
        public void NonObjectExtrasKind(string extrasJson, ValueKind expected)
        {
            var e = DeserializeNodeExtras(extrasJson);
            Assert.AreEqual(expected, e.Kind);
        }

        [Test]
        public void NonObjectExtrasNumber()
        {
            var e = DeserializeNodeExtras("42");
            Assert.AreEqual(ValueKind.Number, e.Kind);
            Assert.IsTrue(e.RawValue.TryGetInt64(out var value));
            Assert.AreEqual(42, value);
        }

        [Test]
        public void NonObjectExtrasString()
        {
            var e = DeserializeNodeExtras(@"""Yadiya""");
            Assert.AreEqual(ValueKind.String, e.Kind);
            Assert.AreEqual("Yadiya", e.RawValue.GetString());
        }

        [Test]
        public void NonObjectExtrasBoolean()
        {
            Assert.IsTrue(DeserializeNodeExtras("true").RawValue.GetBoolean());
            Assert.IsFalse(DeserializeNodeExtras("false").RawValue.GetBoolean());
        }

        [Test]
        public void NonObjectExtrasArray()
        {
            var e = DeserializeNodeExtras("[1,2,3]");
            Assert.AreEqual(ValueKind.Array, e.Kind);

            var raw = e.RawValue;
            Assert.AreEqual(3, raw.ArrayLength);
            Assert.IsTrue(raw[1].TryGetInt64(out var second));
            Assert.AreEqual(2, second);
        }

        [Test]
        public void NonObjectExtrasNestedArray()
        {
            var e = DeserializeNodeExtras(@"[{""a"":[1,2]},null,3]");
            Assert.AreEqual(ValueKind.Array, e.Kind);

            var raw = e.RawValue;
            Assert.AreEqual(3, raw.ArrayLength);
            Assert.AreEqual(ValueKind.Object, raw[0].Kind);
            Assert.AreEqual(ValueKind.Null, raw[1].Kind);
            Assert.AreEqual(2, raw[0]["a"].ArrayLength);
        }

        [Test]
        public void NonObjectExtrasDeserializesToUserType()
        {
            var e = DeserializeNodeExtras("[1.0,0.5,0.25]");
            Assert.IsTrue(e.RawValue.TryGetValue(out float[] values));
            CollectionAssert.AreEqual(new[] { 1.0f, 0.5f, 0.25f }, values);
        }

        [Test]
        public void NonObjectExtrasHasNoProperties()
        {
            var e = DeserializeNodeExtras("42");
            Assert.AreEqual(0, e.Count);
            CollectionAssert.IsEmpty(e.Keys);
            Assert.IsFalse(e.ContainsKey("anything"));
            Assert.IsFalse(e.TryGetValue("anything", out int _));
            foreach (var _ in e)
            {
                Assert.Fail("Expected no properties.");
            }
        }

        [Test]
        public void ObjectExtrasKindIsObject()
        {
            var e = DeserializeNodeExtras(@"{""intProp"":42}");
            Assert.AreEqual(ValueKind.Object, e.Kind);
            Assert.AreEqual(ValueKind.Undefined, e.RawValue.Kind);
            Assert.AreEqual(1, e.Count);
        }

        [Test]
        public void NullExtrasIsNull()
        {
            var gltf = JsonSerializer.Deserialize(@"{""nodes"":[{""extras"":null}]}", GltfJsonContext.Default.Root);
            Assert.IsNull(gltf.Nodes[0].Extras);
        }

        [Test]
        public void MeshNonObjectExtras()
        {
            var mesh = JsonSerializer.Deserialize(@"{""extras"":99}", GltfJsonContext.Default.Mesh);
            Assert.IsNotNull(mesh.Extras);
            Assert.AreEqual(ValueKind.Number, mesh.Extras.Kind);
            Assert.IsTrue(mesh.Extras.RawValue.TryGetInt64(out var value));
            Assert.AreEqual(99, value);
            Assert.IsNull(mesh.Extras.TargetNames);
        }

        [Test]
        public void MeshObjectExtrasStillPopulatesTargetNames()
        {
            var mesh = JsonSerializer.Deserialize(
                @"{""extras"":{""targetNames"":[""k1"",""k2""],""custom"":7}}",
                GltfJsonContext.Default.Mesh);

            Assert.AreEqual(ValueKind.Object, mesh.Extras.Kind);
            CollectionAssert.AreEqual(new[] { "k1", "k2" }, mesh.Extras.TargetNames);
            Assert.IsTrue(mesh.Extras.TryGetValue("custom", out int custom));
            Assert.AreEqual(7, custom);
        }

        [Test]
        [TestCase("42")]
        [TestCase(@"""Yadiya""")]
        [TestCase("true")]
        [TestCase("false")]
        [TestCase("[1,2,3]")]
        [TestCase("[]")]
        [TestCase(@"{""a"":1}")]
        [TestCase("{}")]
        public void NonObjectExtrasRoundTrip(string extrasJson)
        {
            var json = $@"{{""nodes"":[{{""extras"":{extrasJson}}}]}}";
            var gltf = JsonSerializer.Deserialize(json, GltfJsonContext.Default.Root);
            Assert.AreEqual(json, JsonSerializer.Serialize(gltf, GltfJsonContext.Default.Root));
        }

        [Test]
        public void SetOnNonObjectExtrasDiscardsRawValue()
        {
            var e = DeserializeNodeExtras("42");
            e.Set("a", 1L);

            Assert.AreEqual(ValueKind.Object, e.Kind);
            Assert.AreEqual(1, e.Count);

            var node = new Node { Extras = e };
            Assert.AreEqual(
                @"{""extras"":{""a"":1}}",
                JsonSerializer.Serialize(node, GltfJsonContext.Default.Node));
        }

        [Test]
        public void SetTargetNamesOnNonObjectExtrasDiscardsRawValue()
        {
            var mesh = JsonSerializer.Deserialize(@"{""extras"":42}", GltfJsonContext.Default.Mesh);
            Assert.AreEqual(ValueKind.Number, mesh.Extras.Kind);

            mesh.Extras.TargetNames = new List<string> { "a", "b" };

            Assert.AreEqual(ValueKind.Object, mesh.Extras.Kind);
            Assert.AreEqual(
                @"{""extras"":{""targetNames"":[""a"",""b""]}}",
                JsonSerializer.Serialize(mesh, GltfJsonContext.Default.Mesh));
        }

        [Test]
        public void SetOnNonObjectMeshExtrasDiscardsRawValue()
        {
            var mesh = JsonSerializer.Deserialize(@"{""extras"":42}", GltfJsonContext.Default.Mesh);
            mesh.Extras.Set("uuid", "abc");

            Assert.AreEqual(ValueKind.Object, mesh.Extras.Kind);
            Assert.AreEqual(
                @"{""extras"":{""uuid"":""abc""}}",
                JsonSerializer.Serialize(mesh, GltfJsonContext.Default.Mesh));
        }

        [Test]
        public void ClearOnNonObjectExtrasDiscardsRawValue()
        {
            var e = DeserializeNodeExtras(@"""Yadiya""");
            e.Clear();
            Assert.AreEqual(ValueKind.Object, e.Kind);
            Assert.AreEqual(0, e.Count);
        }

        [Test]
        public void ExtrasContainerHasNoSerializedMembers()
        {
            // ExtrasConverter reads the JSON object directly rather than delegating to the generated
            // converter, which is only equivalent while ExtrasContainer declares nothing to
            // (de-)serialize. A member added without a [JsonIgnore] would silently end up among the
            // additional properties instead. Use MeshExtrasConverter's delegating approach for a
            // container that needs declared members.
            var serialized = typeof(ExtrasContainer)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(property => property.GetIndexParameters().Length == 0
                    && property.GetCustomAttribute<JsonIgnoreAttribute>() == null)
                .Select(property => property.Name)
                .ToArray();

            CollectionAssert.IsEmpty(
                serialized,
                "ExtrasContainer must not declare serialized members; see ExtrasConverter.");
        }

        [Test]
        public void NonObjectExtensionsThrows()
        {
            // Unlike extras, extensions must be an object per specification.
            Assert.Throws<JsonException>(
                () => JsonSerializer.Deserialize(@"{""extensions"":5}", GltfJsonContext.Default.Node));
        }

        class MetaData
        {
            public string name { get; set; }
            public int count { get; set; }
        }
    }
}
