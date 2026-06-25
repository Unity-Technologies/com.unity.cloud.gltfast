// SPDX-FileCopyrightText: 2026 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using GLTFast.Schema;
using NUnit.Framework;
using Unity.Gltfast.Text.Json;
using UnityEngine;
using Color = GLTFast.Schema.Color;
using Material = GLTFast.Schema.Material;

namespace GLTFast.Tests.JsonParsing
{
    [TestFixture]
    [Category("JsonParsing")]
    class ColorConverterTests
    {
        [Test]
        public void ColorDeserialization()
        {
            var material = JsonSerializer.Deserialize(
                @"{""emissiveFactor"":[0.1,0.2,0.3]}",
                GltfRootSourceGenerator.Default.Material);
            Assert.AreEqual(new Color(0.1f, 0.2f, 0.3f), material.EmissiveFactor);
        }

        [Test]
        public void ColorDeserializationDefault()
        {
            var material = JsonSerializer.Deserialize("{}", GltfRootSourceGenerator.Default.Material);
            Assert.AreEqual(Color.Black, material.EmissiveFactor);
        }

        [Test]
        public void ColorDeserializationTooFew()
        {
            Assert.Throws<JsonException>(() =>
                JsonSerializer.Deserialize(@"{""emissiveFactor"":[0.1,0.2]}",
                    GltfRootSourceGenerator.Default.Material));
        }

        [Test]
        public void ColorDeserializationTooMany()
        {
            Assert.Throws<JsonException>(() =>
                JsonSerializer.Deserialize(@"{""emissiveFactor"":[0.1,0.2,0.3,0.4]}",
                    GltfRootSourceGenerator.Default.Material));
        }

        [Test]
        public void ColorDeserializationInvalidStart()
        {
            Assert.Throws<JsonException>(() =>
                JsonSerializer.Deserialize(@"{""emissiveFactor"":0.1}",
                    GltfRootSourceGenerator.Default.Material));
        }

        [Test]
        public void ColorDeserializationInvalidType()
        {
            Assert.Throws<JsonException>(() =>
                JsonSerializer.Deserialize(@"{""emissiveFactor"":[0.1,""string"",0.3]}",
                    GltfRootSourceGenerator.Default.Material));
        }

        [Test]
        public void ColorSerialization()
        {
            var material = new Material { EmissiveFactor = new Color(0.5f, 0.25f, 0.75f) };
            var json = JsonSerializer.Serialize(material, GltfRootSourceGenerator.Default.Material);
            Assert.AreEqual(@"{""emissiveFactor"":[0.5,0.25,0.75]}", json);
        }

        [Test]
        public void ColorSerializationDefault()
        {
            var material = new Material();
            var json = JsonSerializer.Serialize(material, GltfRootSourceGenerator.Default.Material);
            Assert.AreEqual("{}", json);
        }

        [Test]
        public void ColorAlphaDeserialization()
        {
            var pbr = JsonSerializer.Deserialize(
                @"{""baseColorFactor"":[0.1,0.2,0.3,0.4]}",
                GltfRootSourceGenerator.Default.PbrMetallicRoughness);
            Assert.AreEqual(new ColorAlpha(0.1f, 0.2f, 0.3f, 0.4f), pbr.BaseColorFactor);
        }

        [Test]
        public void ColorAlphaDeserializationDefaultsAlpha()
        {
            var pbr = JsonSerializer.Deserialize(
                @"{""baseColorFactor"":[0.1,0.2,0.3]}",
                GltfRootSourceGenerator.Default.PbrMetallicRoughness);
            Assert.AreEqual(new ColorAlpha(0.1f, 0.2f, 0.3f, 1f), pbr.BaseColorFactor);
        }

        [Test]
        public void ColorAlphaDeserializationDefault()
        {
            var pbr = JsonSerializer.Deserialize("{}", GltfRootSourceGenerator.Default.PbrMetallicRoughness);
            Assert.AreEqual(ColorAlpha.White, pbr.BaseColorFactor);
        }

        [Test]
        public void ColorAlphaDeserializationTooFew()
        {
            Assert.Throws<JsonException>(() =>
                JsonSerializer.Deserialize(@"{""baseColorFactor"":[0.1,0.2]}",
                    GltfRootSourceGenerator.Default.PbrMetallicRoughness));
        }

        [Test]
        public void ColorAlphaDeserializationTooMany()
        {
            Assert.Throws<JsonException>(() =>
                JsonSerializer.Deserialize(@"{""baseColorFactor"":[0.1,0.2,0.3,0.4,0.5]}",
                    GltfRootSourceGenerator.Default.PbrMetallicRoughness));
        }

        [Test]
        public void ColorAlphaDeserializationInvalidStart()
        {
            Assert.Throws<JsonException>(() =>
                JsonSerializer.Deserialize(@"{""baseColorFactor"":0.1}",
                    GltfRootSourceGenerator.Default.PbrMetallicRoughness));
        }

        [Test]
        public void ColorAlphaDeserializationInvalidType()
        {
            Assert.Throws<JsonException>(() =>
                JsonSerializer.Deserialize(@"{""baseColorFactor"":[0.1,0.2,0.3,""string""]}",
                    GltfRootSourceGenerator.Default.PbrMetallicRoughness));
        }

        [Test]
        public void ColorAlphaSerialization()
        {
            var pbr = new PbrMetallicRoughness
            {
                BaseColorFactor = new ColorAlpha(0.5f, 0.25f, 0.75f, 0.125f)
            };
            var json = JsonSerializer.Serialize(pbr, GltfRootSourceGenerator.Default.PbrMetallicRoughness);
            Assert.AreEqual(@"{""baseColorFactor"":[0.5,0.25,0.75,0.125]}", json);
        }

        [Test]
        public void ColorAlphaSerializationWhite()
        {
            var pbr = new PbrMetallicRoughness();
            var json = JsonSerializer.Serialize(pbr, GltfRootSourceGenerator.Default.PbrMetallicRoughness);
            Assert.AreEqual("{}", json);
        }

        [Test]
        public void ColorEquals()
        {
            var a = new Color(0.5f, 0.25f, 0.75f);
            var b = new Color(0.5f, 0.25f, 0.75f);
            var c = new Color(0.5f, 0.25f, 0.5f);

            Assert.IsTrue(a.Equals(b));
            Assert.IsFalse(a.Equals(c));
            Assert.IsTrue(a.Equals((object)b));
            Assert.IsFalse(a.Equals((object)c));
            Assert.IsFalse(a.Equals(null));
            Assert.IsFalse(a.Equals("not a color"));
        }

        [Test]
        public void ColorEqualsWithinEpsilon()
        {
            var a = new Color(0.5f, 0.5f, 0.5f);
            var withinEpsilon = new Color(0.5f + 0.0005f, 0.5f - 0.0005f, 0.5f);
            var beyondEpsilon = new Color(0.5f + 0.01f, 0.5f, 0.5f);

            Assert.IsTrue(a.Equals(withinEpsilon));
            Assert.IsFalse(a.Equals(beyondEpsilon));
        }

        [Test]
        public void ColorOperatorEquals()
        {
            var a = new Color(0.5f, 0.25f, 0.75f);
            var b = new Color(0.5f, 0.25f, 0.75f);
            var c = new Color(0.5f, 0.25f, 0.5f);

            Assert.IsTrue(a == b);
            Assert.IsFalse(a != b);
            Assert.IsFalse(a == c);
            Assert.IsTrue(a != c);
        }

        [Test]
        public void ColorGetHashCode()
        {
            var a = new Color(0.5f, 0.25f, 0.75f);
            var b = new Color(0.5f, 0.25f, 0.75f);
            var c = new Color(0.5f, 0.25f, 0.5f);

            Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
            Assert.AreNotEqual(a.GetHashCode(), c.GetHashCode());
        }

        [Test]
        [TestCase(1f, 0.5f, 0.25f, 1f)]
        [TestCase(0.25f, 1f, 0.5f, 1f)]
        [TestCase(0.25f, 0.5f, 1f, 1f)]
        [TestCase(0f, 0f, 0f, 0f)]
        [TestCase(-1f, -2f, -3f, -1f)]
        public void ColorMaxColorComponent(float r, float g, float b, float expected)
        {
            var color = new Color(r, g, b);
            Assert.AreEqual(expected, color.MaxColorComponent);
        }

        [Test]
        public void ColorAlphaEquals()
        {
            var a = new ColorAlpha(0.5f, 0.25f, 0.75f, 0.125f);
            var b = new ColorAlpha(0.5f, 0.25f, 0.75f, 0.125f);
            var c = new ColorAlpha(0.5f, 0.25f, 0.75f, 0.5f);

            Assert.IsTrue(a.Equals(b));
            Assert.IsFalse(a.Equals(c));
            Assert.IsTrue(a.Equals((object)b));
            Assert.IsFalse(a.Equals((object)c));
            Assert.IsFalse(a.Equals(null));
            Assert.IsFalse(a.Equals("not a color"));
        }

        [Test]
        public void ColorAlphaEqualsWithinEpsilon()
        {
            var a = new ColorAlpha(0.5f, 0.5f, 0.5f, 0.5f);
            var withinEpsilon = new ColorAlpha(0.5f + 0.0005f, 0.5f, 0.5f, 0.5f - 0.0005f);
            var beyondEpsilon = new ColorAlpha(0.5f, 0.5f, 0.5f, 0.5f + 0.01f);

            Assert.IsTrue(a.Equals(withinEpsilon));
            Assert.IsFalse(a.Equals(beyondEpsilon));
        }

        [Test]
        public void ColorAlphaOperatorEquals()
        {
            var a = new ColorAlpha(0.5f, 0.25f, 0.75f, 0.125f);
            var b = new ColorAlpha(0.5f, 0.25f, 0.75f, 0.125f);
            var c = new ColorAlpha(0.5f, 0.25f, 0.75f, 0.5f);

            Assert.IsTrue(a == b);
            Assert.IsFalse(a != b);
            Assert.IsFalse(a == c);
            Assert.IsTrue(a != c);
        }

        [Test]
        public void ColorAlphaGetHashCode()
        {
            var a = new ColorAlpha(0.5f, 0.25f, 0.75f, 0.125f);
            var b = new ColorAlpha(0.5f, 0.25f, 0.75f, 0.125f);
            var c = new ColorAlpha(0.5f, 0.25f, 0.75f, 0.5f);

            Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
            Assert.AreNotEqual(a.GetHashCode(), c.GetHashCode());
        }
    }
}
