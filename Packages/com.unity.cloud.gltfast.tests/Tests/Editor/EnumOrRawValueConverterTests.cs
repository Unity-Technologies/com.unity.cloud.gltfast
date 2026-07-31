// SPDX-FileCopyrightText: 2026 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Unity.Cloud.Gltfast.Schema;
using Unity.Gltfast.Text.Json;
using Unity.Gltfast.Text.Json.Serialization;

namespace Unity.Cloud.Gltfast.Editor.Tests
{
    class EnumOrRawValueConverterTests
    {
        [Test]
        [TestCaseSource(nameof(TestCaseProvider))]
        public void AllEnumValuesAreCovered(Type converterType)
        {
            var verifyMethod = typeof(EnumOrRawValueConverterTests)
                .GetMethod(nameof(VerifyConverter), BindingFlags.Static | BindingFlags.NonPublic);

            var enumType = GetConvertedEnumType(converterType);
            var converter = Activator.CreateInstance(converterType);
            Debug.Assert(verifyMethod != null, nameof(verifyMethod) + " != null");
            verifyMethod.MakeGenericMethod(enumType).Invoke(null, new[] { converter });
        }

        static IEnumerable TestCaseProvider()
        {
            var converterTypes = typeof(EnumOrRawValueConverter<>).Assembly.GetTypes()
                .Where(t => !t.IsAbstract && GetConvertedEnumType(t) != null)
                .ToArray();

            Assert.IsNotEmpty(converterTypes, "No EnumOrRawValueConverter derivatives found.");


            foreach (var converterType in converterTypes)
            {
                yield return converterType;
            }
        }

        static void VerifyConverter<TEnum>(JsonConverter converter) where TEnum : struct, Enum
        {
            var enumType = typeof(TEnum);
            var converterName = converter.GetType().Name;
            var options = new JsonSerializerOptions();
            options.Converters.Add(converter);

            foreach (TEnum value in Enum.GetValues(enumType))
            {
                var jsonName = GetJsonName(enumType, value);

                if (jsonName == null)
                {
                    // The converters intentionally don't map the "not specified" sentinel. Only the default (0)
                    // value may lack a name; any other value without one is a real value missing from JSON.
                    Assert.AreEqual(
                        0L,
                        Convert.ToInt64(value),
                        $"{enumType.Name}.{value} has no [JsonStringEnumMemberName]. Only the default sentinel " +
                        $"may lack one — add the attribute and cover the value in {converterName}.");
                    continue;
                }

                var expectedJson = $"\"{jsonName}\"";

                try
                {
                    var actualJson = JsonSerializer.Serialize(new EnumOrRawValue<TEnum>(value), options);
                    Assert.AreEqual(
                        expectedJson,
                        actualJson,
                        $"{converterName} does not serialize {enumType.Name}.{value} to \"{jsonName}\". " +
                        "Update its WriteEnum method.");
                }
                catch (ArgumentOutOfRangeException)
                {
                    throw new AssertionException($"{converterName} does not serialize {enumType.Name}.{value} to " +
                        $"\"{jsonName}\". Update its WriteEnum method.");
                }

                var parsed = JsonSerializer.Deserialize<EnumOrRawValue<TEnum>>(expectedJson, options);
                Assert.IsNull(
                    parsed.RawValue,
                    $"{converterName} did not recognize \"{jsonName}\" and fell back to a raw value. " +
                    "Update its TryReadEnum method.");
                Assert.AreEqual(
                    value,
                    parsed.Value,
                    $"{converterName} deserializes \"{jsonName}\" to the wrong {enumType.Name} value.");
            }
        }

        static Type GetConvertedEnumType(Type converterType)
        {
            for (var type = converterType; type != null && type != typeof(object); type = type.BaseType)
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(EnumOrRawValueConverter<>))
                {
                    return type.GetGenericArguments()[0];
                }
            }

            return null;
        }

        static string GetJsonName(Type enumType, object value)
        {
            var name = Enum.GetName(enumType, value);
            var field = enumType.GetField(name);
            return field?.GetCustomAttribute<JsonStringEnumMemberNameAttribute>()?.Name;
        }
    }
}
