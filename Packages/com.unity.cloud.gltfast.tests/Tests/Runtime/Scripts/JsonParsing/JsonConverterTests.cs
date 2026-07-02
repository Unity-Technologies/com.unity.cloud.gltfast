// SPDX-FileCopyrightText: 2025 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.IO;
using System.Text;
using GLTFast.Schema;
using NUnit.Framework;
using Unity.Gltfast.Text.Json;
using Unity.Gltfast.Text.Json.Serialization;
using Unity.Mathematics;
using UnityEngine;

namespace GLTFast.Tests.JsonParsing
{
    [Category("JsonParsing")]
    class JsonConverterTests
    {
        const string k_TooFewJson = "[1.1]";
        const string k_TooManyJson = "[1.1,2.2,3.3,4.4,5.5,6.6,7.7,8.8,9.9,10.10,11.11,12.12,13.13,14.14,15.15,16.16,17.17,18.18]";
        const string k_InvalidStringJson = "[1.1,2.2,\"string\"]";
        const string k_InvalidArrayJson = "[1.1,2.2,[3.3,4.4],5.5]";
        const string k_InvalidObjectJson = "[1.1,2.2,3.3,{\"sub-array\":[4.4,5.5],\"foo\":42}]";

        [Test]
        public static void Double4x4ConverterTest()
        {
            var data = new double4x4(1.1, 2.2, 3.3, 4.4, 5.5, 6.6, 7.7, 8.8,
                9.9, 10.10, 11.11, 12.12, 13.13, 14.14, 15.15, 16.16);
            var converter = new Double4x4Converter();
            var json = WriteToConverter(converter, data);

            var result = ReadFromConverter(json, converter);
            Assert.NotNull(result);
            Assert.That(
                result,
                Is.EqualTo(
                    new double4x4(
                        new double4(1.1, 5.5, 9.9, 13.13),
                        new double4(2.2, 6.6, 10.10, 14.14),
                        new double4(3.3, 7.7, 11.11, 15.15),
                        new double4(4.4, 8.8, 12.12, 16.16)))
                    .Using(new Double4x4EqualityComparer()));
        }

        [Test]
        public static void Double4x4ConverterInvalidStartTest()
        {
            var converter = new Double4x4Converter();
            Assert.Throws<JsonException>(delegate
            {
                ReadFromConverter("9", converter);
            });
        }

        [Test]
        public static void Double4x4ConverterInvalidTypeTest()
        {
            const string json = "[1.1,\"string\"]";
            var converter = new Double4x4Converter();
            Assert.Throws<JsonException>(delegate
            {
                ReadFromConverter(json, converter);
            });
        }

        [Test]
        public static void Double4x4ConverterTooFewTest()
        {
            var converter = new Double4x4Converter();
            Assert.Throws<JsonException>(delegate
            {
                ReadFromConverter(k_TooFewJson, converter);
            });
        }

        [Test]
        public static void Double4x4ConverterTooManyTest()
        {
            var converter = new Double4x4Converter();
            Assert.Throws<JsonException>(delegate
            {
                ReadFromConverter(k_TooManyJson, converter);
            });
        }

        static T ReadFromConverter<T>(string json, JsonConverter<T> converter)
        {
            var jsonUtf8 = Encoding.UTF8.GetBytes(json);
            var reader = new Utf8JsonReader(jsonUtf8);
            reader.Read();
            var result = converter.Read(ref reader, typeof(float[]), null!);
            Assert.AreEqual(jsonUtf8.Length, reader.BytesConsumed);
            return result;
        }

        static string WriteToConverter<T>(JsonConverter<T> converter, T data)
        {
            var stream = new MemoryStream(1000);
            var writer = new Utf8JsonWriter(stream);
            converter.Write(writer, data, JsonSerializerOptions.Default);
            writer.Flush();
            stream.Seek(0, SeekOrigin.Begin);
            var rdr = new StreamReader(stream, Encoding.UTF8);
            return rdr.ReadToEnd();
        }

        [Test]
        public static void FloatListConverterTest()
        {
            var data = new List<float> { 1.1f, 2.2f, 3.3f, 4.4f, 5.5f };
            var converter = new FloatListConverter();
            var json = WriteToConverter(converter, data);

            var result = ReadFromConverter(json, converter);
            Assert.NotNull(result);
            Assert.AreEqual(5, result.Count);
            Assert.AreEqual(1.1f, result[0]);
            Assert.AreEqual(2.2f, result[1]);
            Assert.AreEqual(3.3f, result[2]);
            Assert.AreEqual(4.4f, result[3]);
            Assert.AreEqual(5.5f, result[4]);
        }

        [Test]
        public static void FloatListConverterEmptyTest()
        {
            var converter = new FloatListConverter();
            var result = ReadFromConverter("[]", converter);
            Assert.NotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public static void FloatListConverterLargeTest()
        {
            const int count = 1024;
            var data = new List<float>(count);
            for (var i = 0; i < count; i++)
            {
                data.Add(i * 0.5f);
            }
            var converter = new FloatListConverter();
            var json = WriteToConverter(converter, data);

            var result = ReadFromConverter(json, converter);
            Assert.NotNull(result);
            Assert.AreEqual(count, result.Count);
            for (var i = 0; i < count; i++)
            {
                Assert.AreEqual(i * 0.5f, result[i]);
            }
        }

        [Test]
        public static void FloatListConverterInvalidStartTest()
        {
            var converter = new FloatListConverter();
            Assert.Throws<JsonException>(() => ReadFromConverter("9", converter));
        }

        [Test]
        public static void FloatListConverterInvalidTypeTest()
        {
            var converter = new FloatListConverter();
            Assert.Throws<JsonException>(() => ReadFromConverter("[1.1,\"string\"]", converter));
        }

        [Test]
        public static void FloatListConverterInvalidArrayTest()
        {
            var converter = new FloatListConverter();
            Assert.Throws<JsonException>(() => ReadFromConverter(k_InvalidArrayJson, converter));
        }

        [Test]
        public static void FloatListConverterInvalidObjectTest()
        {
            var converter = new FloatListConverter();
            Assert.Throws<JsonException>(() => ReadFromConverter(k_InvalidObjectJson, converter));
        }

        static List<float> ReadFromConverter(string json, FloatListConverter converter)
        {
            var jsonUtf8 = Encoding.UTF8.GetBytes(json);
            var reader = new Utf8JsonReader(jsonUtf8);
            reader.Read();
            var result = converter.Read(ref reader, typeof(List<float>), null!);
            Assert.AreEqual(jsonUtf8.Length, reader.BytesConsumed);
            return result;
        }

        static string WriteToConverter(FloatListConverter converter, List<float> data)
        {
            var stream = new MemoryStream(1000);
            var writer = new Utf8JsonWriter(stream);
            converter.Write(writer, data, JsonSerializerOptions.Default);
            writer.Flush();
            stream.Seek(0, SeekOrigin.Begin);
            var rdr = new StreamReader(stream, Encoding.UTF8);
            return rdr.ReadToEnd();
        }
    }
}
