// SPDX-FileCopyrightText: 2026 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using Unity.Gltfast.Text.Json;
using Unity.Gltfast.Text.Json.Serialization;

namespace GLTFast.Schema
{
    class Double2ArrayConverter : DoubleFixedSizeArrayConverter { public Double2ArrayConverter() : base(2) { } }
    class Double3ArrayConverter : DoubleFixedSizeArrayConverter { public Double3ArrayConverter() : base(3) { } }
    class Double4ArrayConverter : DoubleFixedSizeArrayConverter { public Double4ArrayConverter() : base(4) { } }
    class Double16ArrayConverter : DoubleFixedSizeArrayConverter { public Double16ArrayConverter() : base(16) { } }

    class DoubleFixedSizeArrayConverter : JsonConverter<double[]>
    {
        readonly int m_ExpectedArraySize;

        protected DoubleFixedSizeArrayConverter(int expectedArraySize)
        {
            m_ExpectedArraySize = expectedArraySize;
        }

        public override double[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartArray)
            {
                throw new JsonException("Expected StartArray token for double array.");
            }

            reader.Read();

            var doubleArray = new double[m_ExpectedArraySize];
            var currentIndex = 0;

            while (reader.TokenType != JsonTokenType.EndArray)
            {
                if (reader.TokenType != JsonTokenType.Number)
                {
                    throw new JsonException($"Expected Number token, but got {reader.TokenType} at index {currentIndex}.");
                }

                if (currentIndex < m_ExpectedArraySize)
                {
                    doubleArray[currentIndex] = FloatParser.GetDouble(reader.ValueSpan);
                    currentIndex++;
                }
                else
                {
                    throw new JsonException($"Expected array length {m_ExpectedArraySize} exceeded at index {currentIndex}.");
                }

                reader.Read();
            }

            return currentIndex == m_ExpectedArraySize
                ? doubleArray
                : throw new JsonException($"Expected {m_ExpectedArraySize} double elements, only found {currentIndex}.");
        }

        public override void Write(Utf8JsonWriter writer, double[] value, JsonSerializerOptions options)
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
