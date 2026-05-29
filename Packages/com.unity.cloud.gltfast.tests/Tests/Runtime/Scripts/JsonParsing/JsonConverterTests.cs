// SPDX-FileCopyrightText: 2025 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System.IO;
using System.Text;
using GLTFast.Schema;
using NUnit.Framework;
using Unity.Gltfast.Text.Json;
using Unity.Gltfast.Text.Json.Serialization;
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
        public static void Float2ArrayConverterTest()
        {
            var data = new[] { 1.1f, 2.2f };
            var converter = new Float2ArrayConverter();
            var json = WriteToConverter(converter, data);

            var result = ReadFromConverter(json, converter);
            Assert.NotNull(result);
            Assert.AreEqual(2, result.Length);
            Assert.AreEqual(1.1f, result[0]);
            Assert.AreEqual(2.2f, result[1]);
        }

        [Test]
        public static void Float2ArrayConverterInvalidStartTest()
        {
            var converter = new Float2ArrayConverter();
            Assert.Throws<JsonException>(delegate
            {
                ReadFromConverter("9", converter);
            });
        }

        [Test]
        public static void Float2ArrayConverterInvalidTypeTest()
        {
            const string json = "[1.1,\"string\"]";
            var converter = new Float2ArrayConverter();
            Assert.Throws<JsonException>(delegate
            {
                ReadFromConverter(json, converter);
            });
        }

        [Test]
        public static void Float2ArrayConverterTooFewTest()
        {
            var converter = new Float2ArrayConverter();
            Assert.Throws<JsonException>(delegate
            {
                ReadFromConverter(k_TooFewJson, converter);
            });
        }

        [Test]
        public static void Float2ArrayConverterTooManyTest()
        {
            var converter = new Float2ArrayConverter();
            Assert.Throws<JsonException>(delegate
            {
                ReadFromConverter(k_TooManyJson, converter);
            });
        }

        [Test]
        public static void Float2ArrayConverterInvalidString()
        {
            var converter = new Float2ArrayConverter();
            Assert.Throws<JsonException>(() => ReadFromConverter(k_InvalidStringJson, converter));
        }

        [Test]
        public static void Float2ArrayConverterInvalidArray()
        {
            var converter = new Float2ArrayConverter();
            Assert.Throws<JsonException>(() => ReadFromConverter(k_InvalidArrayJson, converter));
        }

        [Test]
        public static void Float2ArrayConverterInvalidObject()
        {
            var converter = new Float2ArrayConverter();
            Assert.Throws<JsonException>(() => ReadFromConverter(k_InvalidObjectJson, converter));
        }

        [Test]
        public static void Float3ArrayConverterInvalidString()
        {
            var converter = new Float3ArrayConverter();
            Assert.Throws<JsonException>(() => ReadFromConverter(k_InvalidStringJson, converter));
        }

        [Test]
        public static void Float3ArrayConverterInvalidArray()
        {
            var converter = new Float3ArrayConverter();
            Assert.Throws<JsonException>(() => ReadFromConverter(k_InvalidArrayJson, converter));
        }

        [Test]
        public static void Float3ArrayConverterInvalidObject()
        {
            var converter = new Float3ArrayConverter();
            Assert.Throws<JsonException>(() => ReadFromConverter(k_InvalidObjectJson, converter));
        }

        [Test]
        public static void Float3ArrayConverterTest()
        {
            var data = new[] { 1.1f, 2.2f, 3.3f };
            var converter = new Float3ArrayConverter();
            var json = WriteToConverter(converter, data);

            var result = ReadFromConverter(json, converter);
            Assert.NotNull(result);
            Assert.AreEqual(3, result.Length);
            Assert.AreEqual(1.1f, result[0]);
            Assert.AreEqual(2.2f, result[1]);
            Assert.AreEqual(3.3f, result[2]);
        }

        [Test]
        public static void Float3ArrayConverterInvalidStartTest()
        {
            var converter = new Float3ArrayConverter();
            Assert.Throws<JsonException>(delegate
            {
                ReadFromConverter("9", converter);
            });
        }

        [Test]
        public static void Float3ArrayConverterInvalidTypeTest()
        {
            const string json = "[1.1,\"string\"]";
            var converter = new Float3ArrayConverter();
            Assert.Throws<JsonException>(delegate
            {
                ReadFromConverter(json, converter);
            });
        }

        [Test]
        public static void Float3ArrayConverterTooFewTest()
        {
            var converter = new Float3ArrayConverter();
            Assert.Throws<JsonException>(delegate
            {
                ReadFromConverter(k_TooFewJson, converter);
            });
        }

        [Test]
        public static void Float3ArrayConverterTooManyTest()
        {
            var converter = new Float3ArrayConverter();
            Assert.Throws<JsonException>(delegate
            {
                ReadFromConverter(k_TooManyJson, converter);
            });
        }

        [Test]
        public static void Float4ArrayConverterTest()
        {
            var data = new[] { 1.1f, 2.2f, 3.3f, 4.4f };
            var converter = new Float4ArrayConverter();
            var json = WriteToConverter(converter, data);

            var result = ReadFromConverter(json, converter);
            Assert.NotNull(result);
            Assert.AreEqual(4, result.Length);
            Assert.AreEqual(1.1f, result[0]);
            Assert.AreEqual(2.2f, result[1]);
            Assert.AreEqual(3.3f, result[2]);
            Assert.AreEqual(4.4f, result[3]);
        }

        [Test]
        public static void Float4ArrayConverterInvalidStartTest()
        {
            var converter = new Float4ArrayConverter();
            Assert.Throws<JsonException>(delegate
            {
                ReadFromConverter("9", converter);
            });
        }

        [Test]
        public static void Float4ArrayConverterInvalidTypeTest()
        {
            const string json = "[1.1,\"string\"]";
            var converter = new Float4ArrayConverter();
            Assert.Throws<JsonException>(delegate
            {
                ReadFromConverter(json, converter);
            });
        }

        [Test]
        public static void Float4ArrayConverterTooFewTest()
        {
            var converter = new Float4ArrayConverter();
            Assert.Throws<JsonException>(delegate
            {
                ReadFromConverter(k_TooFewJson, converter);
            });
        }

        [Test]
        public static void Float4ArrayConverterTooManyTest()
        {
            var converter = new Float4ArrayConverter();
            Assert.Throws<JsonException>(delegate
            {
                ReadFromConverter(k_TooManyJson, converter);
            });
        }

        [Test]
        public static void Float16ArrayConverterTest()
        {
            var data = new[] { 1.1f, 2.2f, 3.3f, 4.4f, 5.5f, 6.6f, 7.7f, 8.8f,
                9.9f, 10.10f, 11.11f, 12.12f, 13.13f, 14.14f, 15.15f, 16.16f };
            var converter = new Float16ArrayConverter();
            var json = WriteToConverter(converter, data);

            var result = ReadFromConverter(json, converter);
            Assert.NotNull(result);
            Assert.AreEqual(16, result.Length);
            Assert.AreEqual(1.1f, result[0]);
            Assert.AreEqual(2.2f, result[1]);
            Assert.AreEqual(3.3f, result[2]);
            Assert.AreEqual(4.4f, result[3]);
            Assert.AreEqual(5.5f, result[4]);
            Assert.AreEqual(6.6f, result[5]);
            Assert.AreEqual(7.7f, result[6]);
            Assert.AreEqual(8.8f, result[7]);
            Assert.AreEqual(9.9f, result[8]);
            Assert.AreEqual(10.10f, result[9]);
            Assert.AreEqual(11.11f, result[10]);
            Assert.AreEqual(12.12f, result[11]);
            Assert.AreEqual(13.13f, result[12]);
            Assert.AreEqual(14.14f, result[13]);
            Assert.AreEqual(15.15f, result[14]);
            Assert.AreEqual(16.16f, result[15]);
        }

        [Test]
        public static void Float16ArrayConverterInvalidStartTest()
        {
            var converter = new Float16ArrayConverter();
            Assert.Throws<JsonException>(delegate
            {
                ReadFromConverter("9", converter);
            });
        }

        [Test]
        public static void Float16ArrayConverterInvalidTypeTest()
        {
            const string json = "[1.1,\"string\"]";
            var converter = new Float16ArrayConverter();
            Assert.Throws<JsonException>(delegate
            {
                ReadFromConverter(json, converter);
            });
        }

        [Test]
        public static void Float16ArrayConverterTooFewTest()
        {
            var converter = new Float16ArrayConverter();
            Assert.Throws<JsonException>(delegate
            {
                ReadFromConverter(k_TooFewJson, converter);
            });
        }

        [Test]
        public static void Float16ArrayConverterTooManyTest()
        {
            var converter = new Float16ArrayConverter();
            Assert.Throws<JsonException>(delegate
            {
                ReadFromConverter(k_TooManyJson, converter);
            });
        }

        static float[] ReadFromConverter(string json, JsonConverter<float[]> converter)
        {
            var jsonUtf8 = Encoding.UTF8.GetBytes(json);
            var reader = new Utf8JsonReader(jsonUtf8);
            reader.Read();
            var result = converter.Read(ref reader, typeof(float[]), null!);
            Assert.AreEqual(jsonUtf8.Length, reader.BytesConsumed);
            return result;
        }

        static string WriteToConverter(JsonConverter<float[]> converter, float[] data)
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
