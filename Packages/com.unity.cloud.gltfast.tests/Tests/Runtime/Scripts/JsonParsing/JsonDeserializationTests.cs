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
    }
}
