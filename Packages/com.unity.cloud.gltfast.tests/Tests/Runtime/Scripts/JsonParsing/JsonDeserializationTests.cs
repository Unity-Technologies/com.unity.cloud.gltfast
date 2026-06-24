// SPDX-FileCopyrightText: 2026 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using GLTFast.Schema;
using NUnit.Framework;
using Unity.Gltfast.Text.Json;

namespace GLTFast.Tests.JsonParsing
{
    class JsonDeserializationTests
    {
        [Test]
        [TestCase(PrimitiveMode.Triangles, null)]
        [TestCase(PrimitiveMode.Points, 0)]
        [TestCase(PrimitiveMode.TriangleFan, 6)]
        public void PrimitiveModes(PrimitiveMode expected, int? value)
        {
            var json = value.HasValue ? $@"{{""mode"":{value}}}" : "{}";
            var obj = JsonSerializer.Deserialize(json, GltfRootSourceGenerator.Default.MeshPrimitive);
            Assert.AreEqual(expected, obj.Mode);
        }

        [Test]
        [TestCase(null, Sampler.WrapMode.Repeat)]
        [TestCase(33071, Sampler.WrapMode.ClampToEdge)]
        [TestCase(33648, Sampler.WrapMode.MirroredRepeat)]
        [TestCase(10497, Sampler.WrapMode.Repeat)]
        public void SamplerWrapS(int? written, Sampler.WrapMode value)
        {
            var json = written.HasValue ? $@"{{""wrapS"":{written}}}" : "{}";
            var obj = JsonSerializer.Deserialize(json, GltfRootSourceGenerator.Default.Sampler);
            Assert.AreEqual(value, obj.WrapS);
            Assert.AreEqual(Sampler.WrapMode.Repeat, obj.WrapT);
        }

        [Test]
        [TestCase(null, Sampler.WrapMode.Repeat)]
        [TestCase(33071, Sampler.WrapMode.ClampToEdge)]
        [TestCase(33648, Sampler.WrapMode.MirroredRepeat)]
        [TestCase(10497, Sampler.WrapMode.Repeat)]
        public void SamplerWrapT(int? written, Sampler.WrapMode value)
        {
            var json = written.HasValue ? $@"{{""wrapT"":{written}}}" : "{}";
            var obj = JsonSerializer.Deserialize(json, GltfRootSourceGenerator.Default.Sampler);
            Assert.AreEqual(value, obj.WrapT);
            Assert.AreEqual(Sampler.WrapMode.Repeat, obj.WrapS);
        }

        [Test]
        [TestCase(null, BufferViewTarget.Undefined)]
        [TestCase(34962, BufferViewTarget.ArrayBuffer)]
        [TestCase(34963, BufferViewTarget.ElementArrayBuffer)]
        public void BufferViewTargets(int? written, BufferViewTarget value)
        {
            var json = written.HasValue ? $@"{{""target"":{written}}}" : "{}";
            var obj = JsonSerializer.Deserialize(json, GltfRootSourceGenerator.Default.BufferView);
            Assert.AreEqual(value, obj.Target);
        }

        [Test]
        public void AttributesDefault()
        {
            var obj = JsonSerializer.Deserialize("{}", GltfRootSourceGenerator.Default.Attributes);
            Assert.IsFalse(obj.Position.HasValue);
            Assert.IsNull(obj.TexCoords);
            Assert.IsNull(obj.Colors);
            Assert.IsNull(obj.Joints);
            Assert.IsNull(obj.Weights);
            Assert.IsFalse(obj.TryGetValue("anything", out int _));
        }

        [Test]
        public void AttributesScalars()
        {
            var obj = JsonSerializer.Deserialize(
                @"{""POSITION"":1,""NORMAL"":2,""TANGENT"":3}",
                GltfRootSourceGenerator.Default.Attributes);
            Assert.AreEqual(1, obj.Position);
            Assert.AreEqual(2, obj.Normal);
            Assert.AreEqual(3, obj.Tangent);
            Assert.IsNull(obj.TexCoords);
        }

        [Test]
        public void AttributesTexCoordsContiguous()
        {
            var obj = JsonSerializer.Deserialize(
                @"{""TEXCOORD_0"":10,""TEXCOORD_1"":11,""TEXCOORD_2"":12,""TEXCOORD_3"":13,""TEXCOORD_4"":14,""TEXCOORD_5"":15,""TEXCOORD_6"":16,""TEXCOORD_7"":17,""TEXCOORD_8"":18}",
                GltfRootSourceGenerator.Default.Attributes);
            Assert.AreEqual(9, obj.TexCoords.Count);
            for (var i = 0; i < 9; i++)
            {
                Assert.AreEqual(10 + i, obj.GetTexCoord(i));
            }
        }

        [Test]
        public void AttributesTexCoordsSparse()
        {
            var obj = JsonSerializer.Deserialize(
                @"{""TEXCOORD_2"":7}",
                GltfRootSourceGenerator.Default.Attributes);
            Assert.AreEqual(3, obj.TexCoords.Count);
            Assert.IsFalse(obj.GetTexCoord(0).HasValue);
            Assert.IsFalse(obj.GetTexCoord(1).HasValue);
            Assert.AreEqual(7, obj.GetTexCoord(2));
        }

        [Test]
        public void AttributesMultiInfluenceSkinning()
        {
            var obj = JsonSerializer.Deserialize(
                @"{""COLOR_0"":11,""COLOR_1"":12,""JOINTS_0"":20,""JOINTS_1"":21,""WEIGHTS_0"":30,""WEIGHTS_1"":31}",
                GltfRootSourceGenerator.Default.Attributes);
            Assert.AreEqual(2, obj.Colors.Count);
            Assert.AreEqual(11, obj.GetColor(0));
            Assert.AreEqual(12, obj.GetColor(1));
            Assert.AreEqual(20, obj.GetJoint(0));
            Assert.AreEqual(21, obj.GetJoint(1));
            Assert.AreEqual(30, obj.GetWeight(0));
            Assert.AreEqual(31, obj.GetWeight(1));
        }

        [Test]
        public void AttributesHighIndex()
        {
            var obj = JsonSerializer.Deserialize(
                @"{""TEXCOORD_12"":99}",
                GltfRootSourceGenerator.Default.Attributes);
            Assert.AreEqual(13, obj.TexCoords.Count);
            Assert.AreEqual(99, obj.GetTexCoord(12));
        }

        [Test]
        public void AttributesCustomSemantic()
        {
            var obj = JsonSerializer.Deserialize(
                @"{""POSITION"":0,""_NORMALMAP"":5}",
                GltfRootSourceGenerator.Default.Attributes);
            Assert.AreEqual(0, obj.Position);
            Assert.IsTrue(obj.TryGetValue("_NORMALMAP", out int accessor));
            Assert.AreEqual(5, accessor);
            Assert.IsFalse(obj.TryGetValue("_MISSING", out int _));
        }
    }
}
