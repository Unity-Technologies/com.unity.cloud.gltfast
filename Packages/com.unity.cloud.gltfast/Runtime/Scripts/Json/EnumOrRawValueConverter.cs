// SPDX-FileCopyrightText: 2026 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using Unity.Gltfast.Text.Json;
using Unity.Gltfast.Text.Json.Serialization;
using Unity.Gltfast.Text.Json.Serialization.Metadata;

namespace GLTFast.Schema
{
    abstract class EnumOrRawValueConverter<TEnum> : JsonConverter<EnumOrRawValue<TEnum>> where TEnum : struct, Enum
    {
        protected abstract JsonTypeInfo<TEnum> TypeInfo { get; }

        public override EnumOrRawValue<TEnum> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException("Expected string token.");
            }

            var readerCopy = reader;
            try
            {
                var result = JsonSerializer.Deserialize(ref reader, TypeInfo);
                return new EnumOrRawValue<TEnum>(result);
            }
            catch (JsonException)
            {
                reader = readerCopy;
                // Unescaped UTF-8 is always ≤ source UTF-8, so the source length is a safe upper bound.
                var maxByteCount = reader.HasValueSequence
                    ? checked((int)reader.ValueSequence.Length)
                    : reader.ValueSpan.Length;
                var buffer = new byte[maxByteCount];
                var bytesWritten = reader.CopyString(buffer);
                if (bytesWritten != maxByteCount)
                {
                    Array.Resize(ref buffer, bytesWritten);
                }
                reader.Skip(); // Correctly advance past the string token
                return new EnumOrRawValue<TEnum>(buffer);
            }
        }

        public override void Write(Utf8JsonWriter writer, EnumOrRawValue<TEnum> value, JsonSerializerOptions options)
        {
            if (value.RawValue == null)
            {
                JsonSerializer.Serialize(writer, value.Value, TypeInfo);
            }
            else
            {
                writer.WriteStringValue(value.RawValue);
            }
        }
    }

    class AccessorTypeValueConverter : EnumOrRawValueConverter<AccessorType>
    {
        protected override JsonTypeInfo<AccessorType> TypeInfo => GltfRootSourceGenerator.Default.AccessorType;
    }
}
