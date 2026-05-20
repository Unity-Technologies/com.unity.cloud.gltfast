// SPDX-FileCopyrightText: 2025 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Unity.Gltfast.Text.Json;

namespace GLTFast.Tests.Export
{
    public static class GltfJsonComparer
    {
        const string k_ColorPropertyPattern = @"\.(?<property>\w*[cC]olor\w*)(\[\d+\])?$";
        static readonly Regex k_ColorPropertyRegex = new Regex(k_ColorPropertyPattern, RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));

        // Compare two JSON files
        public static void Compare(string json1, string json2, string currentPath = "")
        {
            using var doc1 = JsonDocument.Parse(json1);
            using var doc2 = JsonDocument.Parse(json2);
            CompareElements(
                doc1.RootElement,
                doc2.RootElement,
                currentPath
                );
        }

        static void CompareElements(JsonElement element1, JsonElement element2, string currentPath)
        {
            if (currentPath == "asset.generator")
            {
                // asset.generator usually contains differing Unity and glTFast versions, so we ignore its value
                return;
            }

            Assert.AreEqual(element1.ValueKind, element2.ValueKind);

            switch (element1.ValueKind)
            {
                case JsonValueKind.Object:
                    CompareObjects(element1, element2, currentPath);
                    break;
                case JsonValueKind.Array:
                    CompareArrays(element1, element2, currentPath);
                    break;
                case JsonValueKind.String:
                    Assert.AreEqual(element1.GetString(), element2.GetString());
                    break;
                case JsonValueKind.Number:
                    var isColor = k_ColorPropertyRegex.Match(currentPath).Success;
                    // Colors usually undergo a gamma to linear conversion, hence a bit more tolerance.
                    var tolerance = isColor ? 6E-06f : 6E-08f;
                    Assert.That(
                        element1.GetDouble(),
                        Is.EqualTo(element2.GetDouble()).Within(tolerance),
                        $"Value mismatch at {currentPath}."
                        );
                    break;
                case JsonValueKind.True:
                case JsonValueKind.False:
                    Assert.AreEqual(element1.GetBoolean(), element2.GetBoolean());
                    break;
                case JsonValueKind.Null:
                    break; // Both are null
                default:
                    throw new NotSupportedException($"Unsupported JSON value kind: {element1.ValueKind}");
            }
        }

        static void CompareObjects(JsonElement obj1, JsonElement obj2, string currentPath)
        {
            var props2 = new Dictionary<string, JsonElement>();
            foreach (var prop in obj2.EnumerateObject())
                props2[prop.Name] = prop.Value;

            var visited = new HashSet<string>();
            foreach (var prop in obj1.EnumerateObject())
            {
                if (!props2.TryGetValue(prop.Name, out var value2))
                    Assert.Fail($"Property '{prop.Name}' missing in second object at '{currentPath}'.");

                var newPath = string.IsNullOrEmpty(currentPath)
                    ? prop.Name
                    : $"{currentPath}.{prop.Name}";

                CompareElements(prop.Value, value2, newPath);
                visited.Add(prop.Name);
            }

            foreach (var key in props2.Keys)
            {
                Assert.IsTrue(visited.Contains(key), $"Property '{key}' missing in first object at '{currentPath}'.");
            }
        }

        static void CompareArrays(JsonElement array1, JsonElement array2, string currentPath)
        {
            Assert.AreEqual(array1.GetArrayLength(), array2.GetArrayLength());

            for (var i = 0; i < array1.GetArrayLength(); i++)
            {
                var newPath = $"{currentPath}[{i}]";
                CompareElements(array1[i], array2[i], newPath);
            }
        }
    }
}
