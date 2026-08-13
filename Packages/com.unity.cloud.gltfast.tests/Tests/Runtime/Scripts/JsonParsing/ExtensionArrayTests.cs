// SPDX-FileCopyrightText: 2026 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using Unity.Cloud.Gltfast.Objects;
using Unity.Cloud.Gltfast.Text.Json;

namespace Unity.Cloud.Gltfast.Tests.JsonParsing
{
    [TestFixture]
    [Category("JsonParsing")]
    class ExtensionArrayTests
    {
        [Test]
        public void DeserializeKnownExtensions()
        {
            const string json =
                @"{""extensionsUsed"":[""KHR_materials_unlit"",""KHR_draco_mesh_compression""]}";
            var gltf = JsonSerializer.Deserialize(json, GltfJsonContext.Default.Root);
            Assert.NotNull(gltf);
            Assert.NotNull(gltf.ExtensionsUsed);
            Assert.AreEqual(2, gltf.ExtensionsUsed.Count);

            Assert.AreEqual(Extension.MaterialsUnlit, gltf.ExtensionsUsed[0].Value);
            Assert.IsNull(gltf.ExtensionsUsed[0].RawValue);

            Assert.AreEqual(Extension.DracoMeshCompression, gltf.ExtensionsUsed[1].Value);
            Assert.IsNull(gltf.ExtensionsUsed[1].RawValue);
        }

        [Test]
        public void DeserializeUnknownExtension()
        {
            const string json =
                @"{""extensionsRequired"":[""KHR_some_future_extension""]}";
            var gltf = JsonSerializer.Deserialize(json, GltfJsonContext.Default.Root);
            Assert.NotNull(gltf);
            Assert.NotNull(gltf.ExtensionsRequired);
            Assert.AreEqual(1, gltf.ExtensionsRequired.Count);

            var entry = gltf.ExtensionsRequired[0];
            Assert.NotNull(entry.RawValue);
            CollectionAssert.AreEqual(
                Encoding.UTF8.GetBytes("KHR_some_future_extension"),
                entry.RawValue);
        }

        [Test]
        public void DeserializeMixedKnownAndUnknown()
        {
            const string json =
                @"{""extensionsUsed"":[""KHR_materials_unlit"",""CUSTOM_my_extension""]}";
            var gltf = JsonSerializer.Deserialize(json, GltfJsonContext.Default.Root);
            Assert.NotNull(gltf.ExtensionsUsed);
            Assert.AreEqual(2, gltf.ExtensionsUsed.Count);

            Assert.AreEqual(Extension.MaterialsUnlit, gltf.ExtensionsUsed[0].Value);
            Assert.IsNull(gltf.ExtensionsUsed[0].RawValue);

            CollectionAssert.AreEqual(
                Encoding.UTF8.GetBytes("CUSTOM_my_extension"),
                gltf.ExtensionsUsed[1].RawValue);
        }

        [Test]
        public void RoundtripKnownExtensions()
        {
            var input = new List<EnumOrRawValue<Extension>>
            {
                new EnumOrRawValue<Extension>(Extension.MaterialsUnlit),
                new EnumOrRawValue<Extension>(Extension.TextureTransform),
            };

            var converter = new ExtensionListConverter();
            var json = WriteList(converter, input);
            Assert.AreEqual(
                @"[""KHR_materials_unlit"",""KHR_texture_transform""]",
                json);

            var roundtrip = ReadList(converter, json);
            Assert.AreEqual(input.Count, roundtrip.Count);
            for (var i = 0; i < input.Count; i++)
            {
                Assert.AreEqual(input[i].Value, roundtrip[i].Value);
                Assert.IsNull(roundtrip[i].RawValue);
            }
        }

        [Test]
        public void RoundtripUnknownExtensions()
        {
            var input = new List<EnumOrRawValue<Extension>>
            {
                new EnumOrRawValue<Extension>(Encoding.UTF8.GetBytes("CUSTOM_one")),
                new EnumOrRawValue<Extension>(Encoding.UTF8.GetBytes("CUSTOM_two")),
            };

            var converter = new ExtensionListConverter();
            var json = WriteList(converter, input);
            Assert.AreEqual(@"[""CUSTOM_one"",""CUSTOM_two""]", json);

            var roundtrip = ReadList(converter, json);
            Assert.AreEqual(input.Count, roundtrip.Count);
            for (var i = 0; i < input.Count; i++)
            {
                CollectionAssert.AreEqual(input[i].RawValue, roundtrip[i].RawValue);
            }
        }

        [Test]
        public void DriftGuard_EveryExtensionNameMatchesSerialization()
        {
            foreach (Extension value in Enum.GetValues(typeof(Extension)))
            {
                var expected = value.GetName();
                Assert.NotNull(expected, $"ExtensionName.GetName returned null for {value}");
                var serialized = JsonSerializer.Serialize(
                    value, GltfJsonContext.Default.Extension);
                Assert.AreEqual(
                    $"\"{expected}\"",
                    serialized,
                    $"Drift between Extension.{value} JSON serialization and ExtensionName.{value}.");
            }
        }

        static string WriteList(ExtensionListConverter converter, List<EnumOrRawValue<Extension>> data)
        {
            using var stream = new System.IO.MemoryStream();
            using (var writer = new Utf8JsonWriter(stream))
            {
                converter.Write(writer, data, JsonSerializerOptions.Default);
            }
            return Encoding.UTF8.GetString(stream.ToArray());
        }

        static List<EnumOrRawValue<Extension>> ReadList(ExtensionListConverter converter, string json)
        {
            var bytes = Encoding.UTF8.GetBytes(json);
            var reader = new Utf8JsonReader(bytes);
            reader.Read();
            return converter.Read(ref reader, typeof(List<EnumOrRawValue<Extension>>), null!);
        }
    }
}
