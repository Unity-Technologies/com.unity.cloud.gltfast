// SPDX-FileCopyrightText: 2026 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using GLTFast.Schema;
using NUnit.Framework;
using Unity.Gltfast.Text.Json;

namespace GLTFast.Tests.JsonParsing
{
    class JsonSerializationTests
    {
        [Test]
        public void PrimitiveDefault()
        {
            var obj = new MeshPrimitive();
            var json = JsonSerializer.Serialize(obj, GltfRootSourceGenerator.Default.MeshPrimitive);
            Assert.AreEqual("{}", json);
        }

        [Test]
        [TestCase(0, PrimitiveMode.Points)]
        [TestCase(1, PrimitiveMode.Lines)]
        [TestCase(2, PrimitiveMode.LineLoop)]
        [TestCase(3, PrimitiveMode.LineStrip)]
        [TestCase(null, PrimitiveMode.Triangles)]
        [TestCase(5, PrimitiveMode.TriangleStrip)]
        [TestCase(6, PrimitiveMode.TriangleFan)]
        public void PrimitiveModes(int? expected, PrimitiveMode value)
        {
            var obj = new MeshPrimitive
            {
                Mode = value,
            };
            var json = JsonSerializer.Serialize(obj, GltfRootSourceGenerator.Default.MeshPrimitive);
            Assert.AreEqual(expected.HasValue ? $@"{{""mode"":{expected}}}" : "{}", json);
        }

        [Test]
        public void SamplerDefault()
        {
            var obj = new Sampler();
            var json = JsonSerializer.Serialize(obj, GltfRootSourceGenerator.Default.Sampler);
            Assert.AreEqual("{}", json);
        }

        [Test]
        [TestCase(null, Sampler.WrapMode.Undefined)]
        [TestCase(null, Sampler.WrapMode.Repeat)]
        [TestCase(33071, Sampler.WrapMode.ClampToEdge)]
        [TestCase(33648, Sampler.WrapMode.MirroredRepeat)]
        public void SamplerWrapS(int? expected, Sampler.WrapMode value)
        {
            var obj = new Sampler { WrapS = value };
            var json = JsonSerializer.Serialize(obj, GltfRootSourceGenerator.Default.Sampler);
            Assert.AreEqual(expected.HasValue ? $@"{{""wrapS"":{expected}}}" : "{}", json);
        }

        [Test]
        [TestCase(null, Sampler.WrapMode.Undefined)]
        [TestCase(null, Sampler.WrapMode.Repeat)]
        [TestCase(33071, Sampler.WrapMode.ClampToEdge)]
        [TestCase(33648, Sampler.WrapMode.MirroredRepeat)]
        public void SamplerWrapT(int? expected, Sampler.WrapMode value)
        {
            var obj = new Sampler { WrapT = value };
            var json = JsonSerializer.Serialize(obj, GltfRootSourceGenerator.Default.Sampler);
            Assert.AreEqual(expected.HasValue ? $@"{{""wrapT"":{expected}}}" : "{}", json);
        }

        [Test]
        public void BufferViewDefault()
        {
            var obj = new BufferView();
            var json = JsonSerializer.Serialize(obj, GltfRootSourceGenerator.Default.BufferView);
            Assert.AreEqual("{}", json);
        }

        [Test]
        [TestCase(null, Schema.BufferViewTarget.Undefined)]
        [TestCase(34962, Schema.BufferViewTarget.ArrayBuffer)]
        [TestCase(34963, Schema.BufferViewTarget.ElementArrayBuffer)]
        public void BufferViewTarget(int? expected, BufferViewTarget value)
        {
            var obj = new BufferView { Target = value };
            var json = JsonSerializer.Serialize(obj, GltfRootSourceGenerator.Default.BufferView);
            Assert.AreEqual(expected.HasValue ? $@"{{""target"":{expected}}}" : "{}", json);
        }

        [Test]
        public void AttributesDefault()
        {
            var json = JsonSerializer.Serialize(new Attributes(), GltfRootSourceGenerator.Default.Attributes);
            Assert.AreEqual("{}", json);
        }

        [Test]
        public void AttributesScalars()
        {
            var json = JsonSerializer.Serialize(
                new Attributes { Position = 1, Normal = 2, Tangent = 3 },
                GltfRootSourceGenerator.Default.Attributes);
            Assert.AreEqual(@"{""POSITION"":1,""NORMAL"":2,""TANGENT"":3}", json);
        }

        [Test]
        public void AttributesTexCoordsContiguous()
        {
            var attrs = new Attributes();
            for (var i = 0; i < 9; i++) attrs.SetTexCoord(i, 10 + i);
            var json = JsonSerializer.Serialize(attrs, GltfRootSourceGenerator.Default.Attributes);
            Assert.AreEqual(
                @"{""TEXCOORD_0"":10,""TEXCOORD_1"":11,""TEXCOORD_2"":12,""TEXCOORD_3"":13,""TEXCOORD_4"":14,""TEXCOORD_5"":15,""TEXCOORD_6"":16,""TEXCOORD_7"":17,""TEXCOORD_8"":18}",
                json);
        }

        [Test]
        public void AttributesTexCoordsSparse()
        {
            var attrs = new Attributes();
            attrs.SetTexCoord(2, 7);
            var json = JsonSerializer.Serialize(attrs, GltfRootSourceGenerator.Default.Attributes);
            Assert.AreEqual(@"{""TEXCOORD_2"":7}", json);
            Assert.AreEqual(3, attrs.TexCoords.Count);
            Assert.IsFalse(attrs.GetTexCoord(0).HasValue);
            Assert.IsFalse(attrs.GetTexCoord(1).HasValue);
        }

        [Test]
        public void AttributesMultiInfluenceSkinning()
        {
            var attrs = new Attributes();
            attrs.SetColor(0, 11);
            attrs.SetColor(1, 12);
            attrs.SetJoint(0, 20);
            attrs.SetJoint(1, 21);
            attrs.SetWeight(0, 30);
            attrs.SetWeight(1, 31);
            var json = JsonSerializer.Serialize(attrs, GltfRootSourceGenerator.Default.Attributes);
            Assert.AreEqual(
                @"{""COLOR_0"":11,""COLOR_1"":12,""JOINTS_0"":20,""JOINTS_1"":21,""WEIGHTS_0"":30,""WEIGHTS_1"":31}",
                json);
        }

        [Test]
        public void AttributesHighIndex()
        {
            var attrs = new Attributes();
            attrs.SetTexCoord(12, 99);
            var json = JsonSerializer.Serialize(attrs, GltfRootSourceGenerator.Default.Attributes);
            Assert.AreEqual(@"{""TEXCOORD_12"":99}", json);
        }

        [Test]
        public void AttributesCustomSemantic()
        {
            // TODO: Construct attrs via constructor once attrs.ExtensionsData becomes writable.
            var attrs = JsonSerializer.Deserialize(
                @"{""POSITION"":0,""_TEMPERATURE"":5}",
                GltfRootSourceGenerator.Default.Attributes);
            var json = JsonSerializer.Serialize(attrs, GltfRootSourceGenerator.Default.Attributes);
            Assert.AreEqual(@"{""POSITION"":0,""_TEMPERATURE"":5}", json);
        }
    }
}
