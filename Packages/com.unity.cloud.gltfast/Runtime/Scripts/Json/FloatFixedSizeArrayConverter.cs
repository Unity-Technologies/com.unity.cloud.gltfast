// SPDX-FileCopyrightText: 2026 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using Unity.Gltfast.Text.Json;
using Unity.Gltfast.Text.Json.Serialization;

namespace GLTFast.Schema
{
    class Float2ArrayConverter : FloatFixedSizeArrayConverter { public Float2ArrayConverter() : base(2) {} }
    class Float3ArrayConverter : FloatFixedSizeArrayConverter { public Float3ArrayConverter() : base(3) {} }
    class Float4ArrayConverter : FloatFixedSizeArrayConverter { public Float4ArrayConverter() : base(4) {} }
    class Float16ArrayConverter : FloatFixedSizeArrayConverter { public Float16ArrayConverter() : base(16) {} }

    class FloatFixedSizeArrayConverter : JsonConverter<float[]>
    {
        readonly int m_ExpectedArraySize;

        protected FloatFixedSizeArrayConverter(int expectedArraySize)
        {
            m_ExpectedArraySize = expectedArraySize;
        }

        public override float[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartArray)
            {
                throw new JsonException("Expected StartArray token for float array.");
            }

            reader.Read();

            var floatArray = new float[m_ExpectedArraySize];
            var currentIndex = 0;

            while (reader.TokenType != JsonTokenType.EndArray)
            {
                if (reader.TokenType != JsonTokenType.Number)
                {
                    throw new JsonException($"Expected Number token, but got {reader.TokenType} at index {currentIndex}.");
                }

                if (currentIndex < m_ExpectedArraySize)
                {
                    floatArray[currentIndex] = (float) FloatParser.GetDouble(reader.ValueSpan);
                    currentIndex++;
                }

                reader.Read();
            }

            return currentIndex == m_ExpectedArraySize
                ? floatArray
                : throw new JsonException($"Expected {m_ExpectedArraySize} float elements, but found {currentIndex}.");
        }

        public override void Write(Utf8JsonWriter writer, float[] value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            foreach (var item in value)
            {
                writer.WriteNumberValue(item);
            }
            writer.WriteEndArray();
        }
    }
}
