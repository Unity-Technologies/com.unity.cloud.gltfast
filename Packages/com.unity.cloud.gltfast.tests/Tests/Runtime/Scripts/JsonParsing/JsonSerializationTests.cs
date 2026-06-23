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
    }
}
