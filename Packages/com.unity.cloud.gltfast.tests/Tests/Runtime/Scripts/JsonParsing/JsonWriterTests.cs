// SPDX-FileCopyrightText: 2024 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using GLTFast.Schema;
using NUnit.Framework;
using Unity.Gltfast.Text.Json;
using UnityEngine;
using UnityEngine.TestTools.Utils;

namespace GLTFast.Tests.JsonParsing
{
    class JsonWriterTests
    {
        const string k_ValueWithAllSpecialChars = "this\fone\\is\not even\remotely\"\tight\"";
        [Test]
        public void Start()
        {
            var json = CreateJsonTest(writer =>
            {
                writer.AddProperty("objectKey");
                writer.AddObject();
                writer.AddProperty("stringKey", "validValue");
                writer.AddProperty("intKey", 42);
                writer.AddProperty("floatKey", 42.42f);
                writer.AddProperty("booleanKey", true);
                writer.Close();
                writer.AddArrayProperty("arrayKey", new int[] { 1, 2, 3 });
            });

            Assert.AreEqual(
                @"{""objectKey"":{""stringKey"":""validValue"",""intKey"":42,""floatKey"":42.42,""booleanKey"":true},""arrayKey"":[1,2,3]}",
                json
                );
        }

        [Test]
        public void InvalidStringKey()
        {
            Assert.Throws<UnityEngine.Assertions.AssertionException>(() =>
                CreateJsonTest(writer =>
                {
                    writer.AddProperty("string\nKey", "validValue");
                })
            );
        }

        [Test]
        public void InvalidIntegerKey()
        {
            Assert.Throws<UnityEngine.Assertions.AssertionException>(() =>
                CreateJsonTest(writer =>
                {
                    writer.AddProperty("int\\Key", 42);
                })
            );
        }

        [Test]
        public void InvalidFloatKey()
        {
            Assert.Throws<UnityEngine.Assertions.AssertionException>(() =>
                CreateJsonTest(writer =>
                {
                    writer.AddProperty("float\tKey", 42.42f);
                })
            );
        }

        [Test]
        public void InvalidBoolString()
        {
            Assert.Throws<UnityEngine.Assertions.AssertionException>(() =>
                CreateJsonTest(writer =>
                {
                    writer.AddProperty("bool\rKey", false);
                })
            );
        }

        [Test]
        public void InvalidObjectKey()
        {
            Assert.Throws<UnityEngine.Assertions.AssertionException>(() =>
                CreateJsonTest(writer =>
                {
                    writer.AddProperty("object\fKey");
                })
            );
        }

        [Test]
        public void InvalidArrayKey()
        {
            Assert.Throws<UnityEngine.Assertions.AssertionException>(() =>
                CreateJsonTest(writer =>
                {
                    writer.AddArrayProperty("array\"Key", new[] { 1, 2, 3 });
                })
            );
        }

        [Test]
        public void InvalidStringValue()
        {
            Assert.Throws<UnityEngine.Assertions.AssertionException>(() =>
                CreateJsonTest(writer =>
                {
                    writer.AddProperty("key", "string\nvalue");
                })
            );
        }

        [Test]
        public void InvalidArrayValue()
        {
            Assert.Throws<UnityEngine.Assertions.AssertionException>(() =>
                CreateJsonTest(writer =>
                {
                    writer.AddArrayProperty("array", new[] { "this one is valid", "this one\nis not" });
                })
            );
        }

        [Test]
        public void EscapedStringValue()
        {
            var json = CreateJsonTest(writer =>
            {
                writer.AddPropertySafe("key", k_ValueWithAllSpecialChars);
            });

            Assert.AreEqual(
                @"{""key"":""this\fone\\is\not even\remotely\""\tight\""""}",
                json
            );
        }

        [Test]
        public void EscapedArrayValue()
        {
            var json = CreateJsonTest(writer =>
            {
                writer.AddArrayPropertySafe("array", new[] { "this one is valid", k_ValueWithAllSpecialChars });
            });

            Assert.AreEqual(
                @"{""array"":[""this one is valid"",""this\fone\\is\not even\remotely\""\tight\""""]}",
                json
            );
        }

        [Test]
        public void ExtMaterialSheen()
        {
            var sheenColor = new Color(.2f, .5f, .7f);
            var ext = new Sheen
            {
                SheenColor = sheenColor,
                SheenRoughnessFactor = .42f,
                SheenColorTexture = new TextureInfo
                {
                    Index = 42,
                    TexCoord = 1,
                },
                SheenRoughnessTexture = new TextureInfo
                {
                    Index = 43,
                    TexCoord = 2,
                }
            };

            Assert.That(ext.SheenColor, Is.EqualTo(sheenColor).Using(ColorEqualityComparer.Instance));

            var json = CreateJsonTest(writer =>
            {
                writer.AddProperty("ext");
                ext.GltfSerialize(writer);
            });

            Assert.AreEqual(
                @"{""ext"":{""sheenColorFactor"":[0.2,0.5,0.7],""sheenColorTexture"":{""index"":42,""texCoord"":1},""sheenRoughnessFactor"":0.42,""sheenRoughnessTexture"":{""index"":43,""texCoord"":2}}}",
                json
            );
        }

        [Test]
        public void ExtMaterialSpecular()
        {
            var specularColor = new Color(.2f, .5f, .7f);
            var ext = new MaterialSpecular()
            {
                SpecularFactor = .42f,
                SpecularTexture = new TextureInfo
                {
                    Index = 42,
                    TexCoord = 1,
                },
                SpecularColor = specularColor,
                SpecularColorTexture = new TextureInfo
                {
                    Index = 43,
                    TexCoord = 2,
                }
            };

            Assert.That(ext.SpecularColor, Is.EqualTo(specularColor).Using(ColorEqualityComparer.Instance));

            var json = CreateJsonTest(writer =>
            {
                writer.AddProperty("ext");
                ext.GltfSerialize(writer);
            });

            Assert.AreEqual(
                @"{""ext"":{""specularFactor"":0.42,""specularTexture"":{""index"":42,""texCoord"":1},""specularColorFactor"":[0.2,0.5,0.7],""specularColorTexture"":{""index"":43,""texCoord"":2}}}",
                json
            );
        }

        [Test]
        public void ExtMaterialIor()
        {
            var ext = new MaterialIor()
            {
                Ior = 1.42f
            };

            var json = CreateJsonTest(writer =>
            {
                writer.AddProperty("ext");
                ext.GltfSerialize(writer);
            });

            Assert.AreEqual(
                @"{""ext"":{""ior"":1.42}}",
                json
            );
        }

        [Test]
        public void ExtMaterialExtensions()
        {
            var ext = new MaterialExtensions
            {
                Sheen = new Sheen
                {
                    SheenRoughnessFactor = .42f
                },
                Specular = new MaterialSpecular
                {
                    SpecularFactor = .43f
                },
                IndexOfRefraction = new MaterialIor
                {
                    Ior = 1.42f
                },
            };

            var json = CreateJsonTest(writer =>
            {
                writer.AddProperty("ext");
                ext.GltfSerialize(writer);
            });

            Assert.AreEqual(
                @"{""ext"":{""KHR_materials_sheen"":{""sheenRoughnessFactor"":0.42},""KHR_materials_specular"":{""specularFactor"":0.43},""KHR_materials_ior"":{""ior"":1.42}}}",
                json
            );
        }

        static string CreateJsonTest(Action<JsonWriter> func)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            var jsonWriter = new JsonWriter(writer);

            func(jsonWriter);

            jsonWriter.Close();
            writer.Flush();
            stream.Seek(0, SeekOrigin.Begin);
            var reader = new StreamReader(stream);
            var result = reader.ReadToEnd();
            writer.Close();

            var jsonDocument = JsonDocument.Parse(result);
            Assert.NotNull(jsonDocument);

            return result;
        }
    }
}
