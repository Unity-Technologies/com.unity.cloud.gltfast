// SPDX-FileCopyrightText: 2026 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using Unity.Cloud.Gltfast.Objects;
using Unity.Cloud.Gltfast.Text.Json;
using Unity.Mathematics;
using Camera = Unity.Cloud.Gltfast.Objects.Camera;
using CameraType = Unity.Cloud.Gltfast.Objects.CameraType;
using Color = Unity.Cloud.Gltfast.Objects.Color;
using LightType = Unity.Cloud.Gltfast.Objects.LightType;
using Material = Unity.Cloud.Gltfast.Objects.Material;
using Mesh = Unity.Cloud.Gltfast.Objects.Mesh;
using Texture = Unity.Cloud.Gltfast.Objects.Texture;

namespace Unity.Cloud.Gltfast.Tests.JsonParsing
{
    class JsonSerializationTests
    {
        [Test]
        public void PrimitiveDefault()
        {
            var obj = new MeshPrimitive();
            var json = JsonSerializer.Serialize(obj, GltfJsonContext.Default.MeshPrimitive);
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
            var json = JsonSerializer.Serialize(obj, GltfJsonContext.Default.MeshPrimitive);
            Assert.AreEqual(expected.HasValue ? $@"{{""mode"":{expected}}}" : "{}", json);
        }

        [Test]
        public void SamplerDefault()
        {
            var obj = new Sampler();
            var json = JsonSerializer.Serialize(obj, GltfJsonContext.Default.Sampler);
            Assert.AreEqual("{}", json);
        }

        [Test]
        [TestCase(null, WrapMode.Undefined)]
        [TestCase(null, WrapMode.Repeat)]
        [TestCase(33071, WrapMode.ClampToEdge)]
        [TestCase(33648, WrapMode.MirroredRepeat)]
        public void SamplerWrapS(int? expected, WrapMode value)
        {
            var obj = new Sampler { WrapS = value };
            var json = JsonSerializer.Serialize(obj, GltfJsonContext.Default.Sampler);
            Assert.AreEqual(expected.HasValue ? $@"{{""wrapS"":{expected}}}" : "{}", json);
        }

        [Test]
        [TestCase(null, WrapMode.Undefined)]
        [TestCase(null, WrapMode.Repeat)]
        [TestCase(33071, WrapMode.ClampToEdge)]
        [TestCase(33648, WrapMode.MirroredRepeat)]
        public void SamplerWrapT(int? expected, WrapMode value)
        {
            var obj = new Sampler { WrapT = value };
            var json = JsonSerializer.Serialize(obj, GltfJsonContext.Default.Sampler);
            Assert.AreEqual(expected.HasValue ? $@"{{""wrapT"":{expected}}}" : "{}", json);
        }

        [Test]
        public void SamplerName()
        {
            var obj = new Sampler { Name = "s" };
            var json = JsonSerializer.Serialize(obj, GltfJsonContext.Default.Sampler);
            Assert.AreEqual(@"{""name"":""s""}", json);
        }

        [Test]
        [TestCase(MagFilterMode.Undefined, "{}")]
        [TestCase(MagFilterMode.Nearest, @"{""magFilter"":9728}")]
        [TestCase(MagFilterMode.Linear, @"{""magFilter"":9729}")]
        public void SamplerMagFilter(MagFilterMode value, string expected)
        {
            var obj = new Sampler { MagFilter = value };
            var json = JsonSerializer.Serialize(obj, GltfJsonContext.Default.Sampler);
            Assert.AreEqual(expected, json);
        }

        [Test]
        [TestCase(MinFilterMode.Undefined, "{}")]
        [TestCase(MinFilterMode.Nearest, @"{""minFilter"":9728}")]
        [TestCase(MinFilterMode.Linear, @"{""minFilter"":9729}")]
        [TestCase(MinFilterMode.NearestMipmapNearest, @"{""minFilter"":9984}")]
        [TestCase(MinFilterMode.LinearMipmapNearest, @"{""minFilter"":9985}")]
        [TestCase(MinFilterMode.NearestMipmapLinear, @"{""minFilter"":9986}")]
        [TestCase(MinFilterMode.LinearMipmapLinear, @"{""minFilter"":9987}")]
        public void SamplerMinFilter(MinFilterMode value, string expected)
        {
            var obj = new Sampler { MinFilter = value };
            var json = JsonSerializer.Serialize(obj, GltfJsonContext.Default.Sampler);
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void BufferViewDefault()
        {
            var obj = new BufferView();
            var json = JsonSerializer.Serialize(obj, GltfJsonContext.Default.BufferView);
            Assert.AreEqual("{}", json);
        }

        [Test]
        [TestCase(null, Objects.BufferViewTarget.Undefined)]
        [TestCase(34962, Objects.BufferViewTarget.ArrayBuffer)]
        [TestCase(34963, Objects.BufferViewTarget.ElementArrayBuffer)]
        public void BufferViewTarget(int? expected, BufferViewTarget value)
        {
            var obj = new BufferView { Target = value };
            var json = JsonSerializer.Serialize(obj, GltfJsonContext.Default.BufferView);
            Assert.AreEqual(expected.HasValue ? $@"{{""target"":{expected}}}" : "{}", json);
        }

        [Test]
        public void BufferViewName()
        {
            var obj = new BufferView { Name = "v" };
            var json = JsonSerializer.Serialize(obj, GltfJsonContext.Default.BufferView);
            Assert.AreEqual(@"{""name"":""v""}", json);
        }

        [Test]
        public void BufferViewBuffer()
        {
            var obj = new BufferView { Buffer = 3 };
            var json = JsonSerializer.Serialize(obj, GltfJsonContext.Default.BufferView);
            Assert.AreEqual(@"{""buffer"":3}", json);
        }

        [Test]
        public void BufferViewByteOffset()
        {
            var obj = new BufferView { ByteOffset = 16 };
            var json = JsonSerializer.Serialize(obj, GltfJsonContext.Default.BufferView);
            Assert.AreEqual(@"{""byteOffset"":16}", json);
        }

        [Test]
        public void BufferViewByteLength()
        {
            var obj = new BufferView { ByteLength = 64 };
            var json = JsonSerializer.Serialize(obj, GltfJsonContext.Default.BufferView);
            Assert.AreEqual(@"{""byteLength"":64}", json);
        }

        [Test]
        public void BufferViewByteStride()
        {
            var obj = new BufferView { ByteStride = 12 };
            var json = JsonSerializer.Serialize(obj, GltfJsonContext.Default.BufferView);
            Assert.AreEqual(@"{""byteStride"":12}", json);
        }

        [Test]
        public void RootDefault()
        {
            var json = JsonSerializer.Serialize(new Root(), GltfJsonContext.Default.Root);
            Assert.AreEqual("{}", json);
        }

        [Test]
        public void RootScene()
        {
            var json = JsonSerializer.Serialize(new Root { Scene = 3 }, GltfJsonContext.Default.Root);
            Assert.AreEqual(@"{""scene"":3}", json);
        }

        [Test]
        public void RootAsset()
        {
            var json = JsonSerializer.Serialize(
                new Root { Asset = new Asset { Version = "2.0" } },
                GltfJsonContext.Default.Root);
            Assert.AreEqual(@"{""asset"":{""version"":""2.0""}}", json);
        }

        [Test]
        public void RootAccessors()
        {
            var json = JsonSerializer.Serialize(
                new Root { Accessors = new List<Accessor> { new() { Count = 1 } } },
                GltfJsonContext.Default.Root);
            Assert.AreEqual(@"{""accessors"":[{""count"":1}]}", json);
        }

        [Test]
        public void RootBuffers()
        {
            var json = JsonSerializer.Serialize(
                new Root { Buffers = new List<Objects.Buffer> { new() { ByteLength = 8 } } },
                GltfJsonContext.Default.Root);
            Assert.AreEqual(@"{""buffers"":[{""byteLength"":8}]}", json);
        }

        [Test]
        public void RootBufferViews()
        {
            var json = JsonSerializer.Serialize(
                new Root { BufferViews = new List<BufferView> { new() { ByteLength = 4 } } },
                GltfJsonContext.Default.Root);
            Assert.AreEqual(@"{""bufferViews"":[{""byteLength"":4}]}", json);
        }

        [Test]
        public void RootCameras()
        {
            var json = JsonSerializer.Serialize(
                new Root { Cameras = new List<Camera> { new() { Type = CameraType.Perspective } } },
                GltfJsonContext.Default.Root);
            Assert.AreEqual(@"{""cameras"":[{""type"":""perspective""}]}", json);
        }

        [Test]
        public void RootImages()
        {
            var json = JsonSerializer.Serialize(
                new Root { Images = new List<Image> { new() { BufferView = 0 } } },
                GltfJsonContext.Default.Root);
            Assert.AreEqual(@"{""images"":[{""bufferView"":0}]}", json);
        }

        [Test]
        public void RootMaterials()
        {
            var json = JsonSerializer.Serialize(
                new Root { Materials = new List<Material> { new() { Name = "m" } } },
                GltfJsonContext.Default.Root);
            Assert.AreEqual(@"{""materials"":[{""name"":""m""}]}", json);
        }

        [Test]
        public void RootMeshes()
        {
            var json = JsonSerializer.Serialize(
                new Root { Meshes = new List<Mesh> { new() { Name = "m" } } },
                GltfJsonContext.Default.Root);
            Assert.AreEqual(@"{""meshes"":[{""name"":""m""}]}", json);
        }

        [Test]
        public void RootNodes()
        {
            var json = JsonSerializer.Serialize(
                new Root { Nodes = new List<Node> { new() { Mesh = 0 } } },
                GltfJsonContext.Default.Root);
            Assert.AreEqual(@"{""nodes"":[{""mesh"":0}]}", json);
        }

        [Test]
        public void RootSamplers()
        {
            var json = JsonSerializer.Serialize(
                new Root { Samplers = new List<Sampler> { new() { MagFilter = MagFilterMode.Nearest } } },
                GltfJsonContext.Default.Root);
            Assert.AreEqual(@"{""samplers"":[{""magFilter"":9728}]}", json);
        }

        [Test]
        public void RootScenes()
        {
            var json = JsonSerializer.Serialize(
                new Root { Scenes = new List<Scene> { new() { Name = "s" } } },
                GltfJsonContext.Default.Root);
            Assert.AreEqual(@"{""scenes"":[{""name"":""s""}]}", json);
        }

        [Test]
        public void RootSkins()
        {
            var json = JsonSerializer.Serialize(
                new Root { Skins = new List<Skin> { new() { Name = "k" } } },
                GltfJsonContext.Default.Root);
            Assert.AreEqual(@"{""skins"":[{""name"":""k""}]}", json);
        }

        [Test]
        public void RootTextures()
        {
            var json = JsonSerializer.Serialize(
                new Root { Textures = new List<Texture> { new() { Source = 0 } } },
                GltfJsonContext.Default.Root);
            Assert.AreEqual(@"{""textures"":[{""source"":0}]}", json);
        }

        [Test]
        public void RootExtensionsUsed()
        {
            var json = JsonSerializer.Serialize(
                new Root { ExtensionsUsed = new List<EnumOrRawValue<Extension>> { new(Extension.MaterialsUnlit) } },
                GltfJsonContext.Default.Root);
            Assert.AreEqual(@"{""extensionsUsed"":[""KHR_materials_unlit""]}", json);
        }

        [Test]
        public void RootExtensionsRequired()
        {
            var json = JsonSerializer.Serialize(
                new Root { ExtensionsRequired = new List<EnumOrRawValue<Extension>> { new(Extension.MaterialsUnlit) } },
                GltfJsonContext.Default.Root);
            Assert.AreEqual(@"{""extensionsRequired"":[""KHR_materials_unlit""]}", json);
        }

        [Test]
        public void RootExtensions()
        {
            var json = JsonSerializer.Serialize(
                new Root { Extensions = new RootExtensions { LightsPunctual = new LightsPunctual { Lights = new List<LightPunctual>() } } },
                GltfJsonContext.Default.Root);
            Assert.AreEqual(@"{""extensions"":{""KHR_lights_punctual"":{""lights"":[]}}}", json);
        }

        [Test]
        public void RootExtras()
        {
            var json = JsonSerializer.Serialize(
                new Root { Extras = new ExtrasContainer() },
                GltfJsonContext.Default.Root);
            Assert.AreEqual(@"{""extras"":{}}", json);
        }

#if UNITY_ANIMATION || GLTFAST_ANIMATION
        [Test]
        public void RootAnimations()
        {
            var json = JsonSerializer.Serialize(
                new Root { Animations = new List<Animation> { new() { Name = "a" } } },
                GltfJsonContext.Default.Root);
            Assert.AreEqual(@"{""animations"":[{""name"":""a""}]}", json);
        }
#endif

        [Test]
        public void AssetDefault()
        {
            var json = JsonSerializer.Serialize(new Asset(), GltfJsonContext.Default.Asset);
            Assert.AreEqual("{}", json);
        }

        [Test]
        public void AssetCopyright()
        {
            var json = JsonSerializer.Serialize(new Asset { Copyright = "C" }, GltfJsonContext.Default.Asset);
            Assert.AreEqual(@"{""copyright"":""C""}", json);
        }

        [Test]
        public void AssetGenerator()
        {
            var json = JsonSerializer.Serialize(new Asset { Generator = "g" }, GltfJsonContext.Default.Asset);
            Assert.AreEqual(@"{""generator"":""g""}", json);
        }

        [Test]
        public void AssetVersion()
        {
            var json = JsonSerializer.Serialize(new Asset { Version = "2.0" }, GltfJsonContext.Default.Asset);
            Assert.AreEqual(@"{""version"":""2.0""}", json);
        }

        [Test]
        public void AssetMinVersion()
        {
            var json = JsonSerializer.Serialize(new Asset { MinVersion = "2.0" }, GltfJsonContext.Default.Asset);
            Assert.AreEqual(@"{""minVersion"":""2.0""}", json);
        }

        [Test]
        public void AssetExtras()
        {
            var json = JsonSerializer.Serialize(new Asset { Extras = new ExtrasContainer() }, GltfJsonContext.Default.Asset);
            Assert.AreEqual(@"{""extras"":{}}", json);
        }

        [Test]
        public void AssetExtensions()
        {
            var json = JsonSerializer.Serialize(new Asset { Extensions = new Objects.AssetExtensions() }, GltfJsonContext.Default.Asset);
            Assert.AreEqual(@"{""extensions"":{}}", json);
        }

        [Test]
        public void AccessorDefault()
        {
            var json = JsonSerializer.Serialize(new Accessor(), GltfJsonContext.Default.Accessor);
            Assert.AreEqual("{}", json);
        }

        [Test]
        public void AccessorName()
        {
            var json = JsonSerializer.Serialize(new Accessor { Name = "a" }, GltfJsonContext.Default.Accessor);
            Assert.AreEqual(@"{""name"":""a""}", json);
        }

        [Test]
        public void AccessorBufferView()
        {
            var json = JsonSerializer.Serialize(new Accessor { BufferView = 7 }, GltfJsonContext.Default.Accessor);
            Assert.AreEqual(@"{""bufferView"":7}", json);
        }

        [Test]
        public void AccessorByteOffset()
        {
            var json = JsonSerializer.Serialize(new Accessor { ByteOffset = 8 }, GltfJsonContext.Default.Accessor);
            Assert.AreEqual(@"{""byteOffset"":8}", json);
        }

        [Test]
        [TestCase(AccessorDataType.Byte, 5120)]
        [TestCase(AccessorDataType.UnsignedByte, 5121)]
        [TestCase(AccessorDataType.Short, 5122)]
        [TestCase(AccessorDataType.UnsignedShort, 5123)]
        [TestCase(AccessorDataType.UnsignedInt, 5125)]
        [TestCase(AccessorDataType.Float, 5126)]
        public void AccessorComponentType(AccessorDataType value, int expected)
        {
            var json = JsonSerializer.Serialize(new Accessor { ComponentType = value }, GltfJsonContext.Default.Accessor);
            Assert.AreEqual($@"{{""componentType"":{expected}}}", json);
        }

        [Test]
        public void AccessorNormalized()
        {
            var json = JsonSerializer.Serialize(new Accessor { Normalized = true }, GltfJsonContext.Default.Accessor);
            Assert.AreEqual(@"{""normalized"":true}", json);
        }

        [Test]
        public void AccessorCount()
        {
            var json = JsonSerializer.Serialize(new Accessor { Count = 42 }, GltfJsonContext.Default.Accessor);
            Assert.AreEqual(@"{""count"":42}", json);
        }

        [Test]
        public void AccessorTypeTest()
        {
            var json = JsonSerializer.Serialize(new Accessor { Type = AccessorType.Vector3 }, GltfJsonContext.Default.Accessor);
            Assert.AreEqual(@"{""type"":""VEC3""}", json);
        }

        [Test]
        public void AccessorTypeRaw()
        {
            var raw = Encoding.UTF8.GetBytes("CUSTOM");
            var json = JsonSerializer.Serialize(new Accessor { Type = new EnumOrRawValue<AccessorType>(raw) }, GltfJsonContext.Default.Accessor);
            Assert.AreEqual(@"{""type"":""CUSTOM""}", json);
        }

        [Test]
        public void AccessorMax()
        {
            var json = JsonSerializer.Serialize(new Accessor { Max = new List<double> { 1f, 2f, 3f } }, GltfJsonContext.Default.Accessor);
            Assert.AreEqual(@"{""max"":[1,2,3]}", json);
        }

        [Test]
        public void AccessorMin()
        {
            var json = JsonSerializer.Serialize(new Accessor { Min = new List<double> { 0f, 0f, 0f } }, GltfJsonContext.Default.Accessor);
            Assert.AreEqual(@"{""min"":[0,0,0]}", json);
        }

        [Test]
        public void AccessorSparse()
        {
            var json = JsonSerializer.Serialize(new Accessor { Sparse = new AccessorSparse { Count = 2 } }, GltfJsonContext.Default.Accessor);
            Assert.AreEqual(@"{""sparse"":{""count"":2}}", json);
        }

        [Test]
        public void AccessorExtras()
        {
            var json = JsonSerializer.Serialize(new Accessor { Extras = new ExtrasContainer() }, GltfJsonContext.Default.Accessor);
            Assert.AreEqual(@"{""extras"":{}}", json);
        }

        [Test]
        public void AccessorExtensions()
        {
            var json = JsonSerializer.Serialize(new Accessor { Extensions = new Objects.AccessorExtensions() }, GltfJsonContext.Default.Accessor);
            Assert.AreEqual(@"{""extensions"":{}}", json);
        }

        [Test]
        public void AccessorSparseDefault()
        {
            var json = JsonSerializer.Serialize(new AccessorSparse(), GltfJsonContext.Default.AccessorSparse);
            Assert.AreEqual("{}", json);
        }

        [Test]
        public void AccessorSparseCount()
        {
            var json = JsonSerializer.Serialize(new AccessorSparse { Count = 5 }, GltfJsonContext.Default.AccessorSparse);
            Assert.AreEqual(@"{""count"":5}", json);
        }

        [Test]
        public void AccessorSparseIndicesProperty()
        {
            var json = JsonSerializer.Serialize(
                new AccessorSparse { Indices = new AccessorSparseIndices { BufferView = 1 } },
                GltfJsonContext.Default.AccessorSparse);
            Assert.AreEqual(@"{""indices"":{""bufferView"":1}}", json);
        }

        [Test]
        public void AccessorSparseValuesProperty()
        {
            var json = JsonSerializer.Serialize(
                new AccessorSparse { Values = new AccessorSparseValues { BufferView = 2 } },
                GltfJsonContext.Default.AccessorSparse);
            Assert.AreEqual(@"{""values"":{""bufferView"":2}}", json);
        }

        [Test]
        public void AccessorSparseIndicesDefault()
        {
            var json = JsonSerializer.Serialize(new AccessorSparseIndices(), GltfJsonContext.Default.AccessorSparseIndices);
            Assert.AreEqual("{}", json);
        }

        [Test]
        public void AccessorSparseIndicesBufferView()
        {
            var json = JsonSerializer.Serialize(new AccessorSparseIndices { BufferView = 4 }, GltfJsonContext.Default.AccessorSparseIndices);
            Assert.AreEqual(@"{""bufferView"":4}", json);
        }

        [Test]
        public void AccessorSparseIndicesByteOffset()
        {
            var json = JsonSerializer.Serialize(new AccessorSparseIndices { ByteOffset = 16 }, GltfJsonContext.Default.AccessorSparseIndices);
            Assert.AreEqual(@"{""byteOffset"":16}", json);
        }

        [Test]
        public void AccessorSparseIndicesComponentType()
        {
            var json = JsonSerializer.Serialize(new AccessorSparseIndices { ComponentType = AccessorDataType.UnsignedShort }, GltfJsonContext.Default.AccessorSparseIndices);
            Assert.AreEqual(@"{""componentType"":5123}", json);
        }

        [Test]
        public void AccessorSparseValuesDefault()
        {
            var json = JsonSerializer.Serialize(new AccessorSparseValues(), GltfJsonContext.Default.AccessorSparseValues);
            Assert.AreEqual("{}", json);
        }

        [Test]
        public void AccessorSparseValuesBufferView()
        {
            var json = JsonSerializer.Serialize(new AccessorSparseValues { BufferView = 4 }, GltfJsonContext.Default.AccessorSparseValues);
            Assert.AreEqual(@"{""bufferView"":4}", json);
        }

        [Test]
        public void AccessorSparseValuesByteOffset()
        {
            var json = JsonSerializer.Serialize(new AccessorSparseValues { ByteOffset = 8 }, GltfJsonContext.Default.AccessorSparseValues);
            Assert.AreEqual(@"{""byteOffset"":8}", json);
        }

#if UNITY_ANIMATION || GLTFAST_ANIMATION
        [Test]
        public void AnimationDefault()
        {
            var json = JsonSerializer.Serialize(new Animation(), GltfJsonContext.Default.Animation);
            Assert.AreEqual("{}", json);
        }

        [Test]
        public void AnimationName()
        {
            var json = JsonSerializer.Serialize(new Animation { Name = "a" }, GltfJsonContext.Default.Animation);
            Assert.AreEqual(@"{""name"":""a""}", json);
        }

        [Test]
        public void AnimationChannels()
        {
            var json = JsonSerializer.Serialize(
                new Animation { Channels = new List<AnimationChannel> { new() { Sampler = 1 } } },
                GltfJsonContext.Default.Animation);
            Assert.AreEqual(@"{""channels"":[{""sampler"":1}]}", json);
        }

        [Test]
        public void AnimationSamplers()
        {
            var json = JsonSerializer.Serialize(
                new Animation { Samplers = new List<AnimationSampler> { new() { Input = 0, Output = 1 } } },
                GltfJsonContext.Default.Animation);
            Assert.AreEqual(@"{""samplers"":[{""input"":0,""output"":1}]}", json);
        }

        [Test]
        public void AnimationChannelDefault()
        {
            var json = JsonSerializer.Serialize(new AnimationChannel(), GltfJsonContext.Default.AnimationChannel);
            Assert.AreEqual("{}", json);
        }

        [Test]
        public void AnimationChannelSampler()
        {
            var json = JsonSerializer.Serialize(new AnimationChannel { Sampler = 3 }, GltfJsonContext.Default.AnimationChannel);
            Assert.AreEqual(@"{""sampler"":3}", json);
        }

        [Test]
        public void AnimationChannelTarget()
        {
            var json = JsonSerializer.Serialize(
                new AnimationChannel { Target = new AnimationChannelTarget { Node = 1 } },
                GltfJsonContext.Default.AnimationChannel);
            Assert.AreEqual(@"{""target"":{""node"":1}}", json);
        }

        [Test]
        public void AnimationChannelTargetDefault()
        {
            var json = JsonSerializer.Serialize(new AnimationChannelTarget(), GltfJsonContext.Default.AnimationChannelTarget);
            Assert.AreEqual("{}", json);
        }

        [Test]
        public void AnimationChannelTargetNode()
        {
            var json = JsonSerializer.Serialize(new AnimationChannelTarget { Node = 7 }, GltfJsonContext.Default.AnimationChannelTarget);
            Assert.AreEqual(@"{""node"":7}", json);
        }

        [Test]
        public void AnimationChannelTargetNodeZero()
        {
            var json = JsonSerializer.Serialize(new AnimationChannelTarget { Node = 0 }, GltfJsonContext.Default.AnimationChannelTarget);
            Assert.AreEqual(@"{""node"":0}", json);
        }

        [Test]
        [TestCase(AnimationPath.Translation, "translation")]
        [TestCase(AnimationPath.Rotation, "rotation")]
        [TestCase(AnimationPath.Scale, "scale")]
        [TestCase(AnimationPath.Weights, "weights")]
        [TestCase(AnimationPath.Pointer, "pointer")]
        public void AnimationChannelTargetPath(AnimationPath value, string expected)
        {
            var json = JsonSerializer.Serialize(new AnimationChannelTarget { Path = value }, GltfJsonContext.Default.AnimationChannelTarget);
            Assert.AreEqual($@"{{""path"":""{expected}""}}", json);
        }

        [Test]
        public void AnimationSamplerDefault()
        {
            var json = JsonSerializer.Serialize(new AnimationSampler(), GltfJsonContext.Default.AnimationSampler);
            Assert.AreEqual("{}", json);
        }

        [Test]
        public void AnimationSamplerInput()
        {
            var json = JsonSerializer.Serialize(new AnimationSampler { Input = 2 }, GltfJsonContext.Default.AnimationSampler);
            Assert.AreEqual(@"{""input"":2}", json);
        }

        [Test]
        public void AnimationSamplerOutput()
        {
            var json = JsonSerializer.Serialize(new AnimationSampler { Output = 4 }, GltfJsonContext.Default.AnimationSampler);
            Assert.AreEqual(@"{""output"":4}", json);
        }

        [Test]
        [TestCase(Interpolation.Step, "STEP")]
        [TestCase(Interpolation.CubicSpline, "CUBICSPLINE")]
        public void AnimationSamplerInterpolation(Interpolation value, string expected)
        {
            var json = JsonSerializer.Serialize(new AnimationSampler { Interpolation = value }, GltfJsonContext.Default.AnimationSampler);
            Assert.AreEqual($@"{{""interpolation"":""{expected}""}}", json);
        }

        /// <summary>
        /// Regression: accessor index 0 (typical time accessor) must round-trip; not be dropped.
        /// </summary>
        [Test]
        public void AnimationSamplerInputOutputZero()
        {
            var obj = new AnimationSampler { Input = 0, Output = 0 };
            var json = JsonSerializer.Serialize(obj, GltfJsonContext.Default.AnimationSampler);
            Assert.AreEqual(@"{""input"":0,""output"":0}", json);
        }

        /// <summary>
        /// Extension-relaxation scenario: an empty JSON object deserializes to absent values
        /// and must re-serialize back to an empty object (no spurious "input"/"output" emitted).
        /// </summary>
        [Test]
        public void AnimationSamplerAbsentRoundTrip()
        {
            var obj = JsonSerializer.Deserialize("{}", GltfJsonContext.Default.AnimationSampler);
            Assert.IsNull(obj.Input);
            Assert.IsNull(obj.Output);
            var json = JsonSerializer.Serialize(obj, GltfJsonContext.Default.AnimationSampler);
            Assert.AreEqual("{}", json);
        }
#endif

        [Test]
        public void BufferDefault()
        {
            var json = JsonSerializer.Serialize(new Objects.Buffer(), GltfJsonContext.Default.Buffer);
            Assert.AreEqual("{}", json);
        }

        [Test]
        public void BufferName()
        {
            var json = JsonSerializer.Serialize(new Objects.Buffer { Name = "b" }, GltfJsonContext.Default.Buffer);
            Assert.AreEqual(@"{""name"":""b""}", json);
        }

        [Test]
        public void BufferByteLength()
        {
            var json = JsonSerializer.Serialize(new Objects.Buffer { ByteLength = 256 }, GltfJsonContext.Default.Buffer);
            Assert.AreEqual(@"{""byteLength"":256}", json);
        }

        [Test]
        public void BufferUri()
        {
            var json = JsonSerializer.Serialize(new Objects.Buffer { Uri = new UriValue("data.bin") }, GltfJsonContext.Default.Buffer);
            Assert.AreEqual(@"{""uri"":""data.bin""}", json);
        }

        [Test]
        public void BufferViewExtensionsDefault()
        {
            var json = JsonSerializer.Serialize(new BufferViewExtensions(), GltfJsonContext.Default.BufferViewExtensions);
            Assert.AreEqual("{}", json);
        }

#if MESHOPT_IS_RECENT
        [Test]
        public void BufferViewExtensionsMeshopt()
        {
            var json = JsonSerializer.Serialize(
                new BufferViewExtensions { ExtMeshoptCompression = new BufferViewMeshoptExtension { Count = 1 } },
                GltfJsonContext.Default.BufferViewExtensions);
            Assert.AreEqual(@"{""EXT_meshopt_compression"":{""count"":1}}", json);
        }

        [Test]
        public void BufferViewMeshoptExtensionDefault()
        {
            var json = JsonSerializer.Serialize(new BufferViewMeshoptExtension(), GltfJsonContext.Default.BufferViewMeshoptExtension);
            Assert.AreEqual("{}", json);
        }

        [Test]
        public void BufferViewMeshoptExtensionBuffer()
        {
            var json = JsonSerializer.Serialize(new BufferViewMeshoptExtension { Buffer = 1 }, GltfJsonContext.Default.BufferViewMeshoptExtension);
            Assert.AreEqual(@"{""buffer"":1}", json);
        }

        [Test]
        public void BufferViewMeshoptExtensionBufferZero()
        {
            var json = JsonSerializer.Serialize(new BufferViewMeshoptExtension { Buffer = 0 }, GltfJsonContext.Default.BufferViewMeshoptExtension);
            Assert.AreEqual(@"{""buffer"":0}", json);
        }

        [Test]
        public void BufferViewMeshoptExtensionByteOffset()
        {
            var json = JsonSerializer.Serialize(new BufferViewMeshoptExtension { ByteOffset = 8 }, GltfJsonContext.Default.BufferViewMeshoptExtension);
            Assert.AreEqual(@"{""byteOffset"":8}", json);
        }

        [Test]
        public void BufferViewMeshoptExtensionByteLength()
        {
            var json = JsonSerializer.Serialize(new BufferViewMeshoptExtension { ByteLength = 16 }, GltfJsonContext.Default.BufferViewMeshoptExtension);
            Assert.AreEqual(@"{""byteLength"":16}", json);
        }

        [Test]
        public void BufferViewMeshoptExtensionByteStride()
        {
            var json = JsonSerializer.Serialize(new BufferViewMeshoptExtension { ByteStride = 4 }, GltfJsonContext.Default.BufferViewMeshoptExtension);
            Assert.AreEqual(@"{""byteStride"":4}", json);
        }

        [Test]
        public void BufferViewMeshoptExtensionCount()
        {
            var json = JsonSerializer.Serialize(new BufferViewMeshoptExtension { Count = 12 }, GltfJsonContext.Default.BufferViewMeshoptExtension);
            Assert.AreEqual(@"{""count"":12}", json);
        }

        [Test]
        [TestCase(MeshoptMode.Attributes, "ATTRIBUTES")]
        [TestCase(MeshoptMode.Triangles, "TRIANGLES")]
        [TestCase(MeshoptMode.Indices, "INDICES")]
        public void BufferViewMeshoptExtensionMode(MeshoptMode value, string expected)
        {
            var json = JsonSerializer.Serialize(new BufferViewMeshoptExtension { Mode = value }, GltfJsonContext.Default.BufferViewMeshoptExtension);
            Assert.AreEqual($@"{{""mode"":""{expected}""}}", json);
        }

        [Test]
        [TestCase(MeshoptFilter.None, null)]
        [TestCase(MeshoptFilter.Octahedral, "OCTAHEDRAL")]
        [TestCase(MeshoptFilter.Quaternion, "QUATERNION")]
        [TestCase(MeshoptFilter.Exponential, "EXPONENTIAL")]
        public void BufferViewMeshoptExtensionFilter(MeshoptFilter value, string expected)
        {
            var json = JsonSerializer.Serialize(new BufferViewMeshoptExtension { Filter = value }, GltfJsonContext.Default.BufferViewMeshoptExtension);
            if (expected != null)
            {
                Assert.AreEqual($@"{{""filter"":""{expected}""}}", json);
            }
            else
            {
                Assert.AreEqual("{}", json);
            }
        }
#endif

        [Test]
        public void CameraDefault()
        {
            var json = JsonSerializer.Serialize(new Camera(), GltfJsonContext.Default.Camera);
            Assert.AreEqual("{}", json);
        }

        [Test]
        public void CameraName()
        {
            var json = JsonSerializer.Serialize(new Camera { Name = "c" }, GltfJsonContext.Default.Camera);
            Assert.AreEqual(@"{""name"":""c""}", json);
        }

        [Test]
        public void CameraOrthographic()
        {
            var json = JsonSerializer.Serialize(
                new Camera { Orthographic = new CameraOrthographic { Xmag = 1f } },
                GltfJsonContext.Default.Camera);
            Assert.AreEqual(@"{""orthographic"":{""xmag"":1}}", json);
        }

        [Test]
        public void CameraPerspective()
        {
            var json = JsonSerializer.Serialize(
                new Camera { Perspective = new CameraPerspective { Yfov = 1.5f } },
                GltfJsonContext.Default.Camera);
            Assert.AreEqual(@"{""perspective"":{""yfov"":1.5}}", json);
        }

        [Test]
        [TestCase(CameraType.Orthographic, "orthographic")]
        [TestCase(CameraType.Perspective, "perspective")]
        public void CameraTypeProperty(CameraType value, string expected)
        {
            var json = JsonSerializer.Serialize(new Camera { Type = value }, GltfJsonContext.Default.Camera);
            Assert.AreEqual($@"{{""type"":""{expected}""}}", json);
        }

        [Test]
        public void CameraOrthographicDefault()
        {
            var json = JsonSerializer.Serialize(new CameraOrthographic(), GltfJsonContext.Default.CameraOrthographic);
            Assert.AreEqual("{}", json);
        }

        [Test]
        public void CameraOrthographicXmag()
        {
            var json = JsonSerializer.Serialize(new CameraOrthographic { Xmag = 2f }, GltfJsonContext.Default.CameraOrthographic);
            Assert.AreEqual(@"{""xmag"":2}", json);
        }

        [Test]
        public void CameraOrthographicYmag()
        {
            var json = JsonSerializer.Serialize(new CameraOrthographic { Ymag = 3f }, GltfJsonContext.Default.CameraOrthographic);
            Assert.AreEqual(@"{""ymag"":3}", json);
        }

        [Test]
        public void CameraOrthographicZfar()
        {
            var json = JsonSerializer.Serialize(new CameraOrthographic { Zfar = 100f }, GltfJsonContext.Default.CameraOrthographic);
            Assert.AreEqual(@"{""zfar"":100}", json);
        }

        [Test]
        public void CameraOrthographicZnear()
        {
            var json = JsonSerializer.Serialize(new CameraOrthographic { Znear = 0.5f }, GltfJsonContext.Default.CameraOrthographic);
            Assert.AreEqual(@"{""znear"":0.5}", json);
        }

        [Test]
        public void CameraPerspectiveDefault()
        {
            var json = JsonSerializer.Serialize(new CameraPerspective(), GltfJsonContext.Default.CameraPerspective);
            Assert.AreEqual("{}", json);
        }

        [Test]
        public void CameraPerspectiveAspectRatio()
        {
            var json = JsonSerializer.Serialize(new CameraPerspective { AspectRatio = 1.5f }, GltfJsonContext.Default.CameraPerspective);
            Assert.AreEqual(@"{""aspectRatio"":1.5}", json);
        }

        [Test]
        public void CameraPerspectiveYfov()
        {
            var json = JsonSerializer.Serialize(new CameraPerspective { Yfov = 1.25f }, GltfJsonContext.Default.CameraPerspective);
            Assert.AreEqual(@"{""yfov"":1.25}", json);
        }

        [Test]
        public void CameraPerspectiveZfar()
        {
            var json = JsonSerializer.Serialize(new CameraPerspective { Zfar = 100f }, GltfJsonContext.Default.CameraPerspective);
            Assert.AreEqual(@"{""zfar"":100}", json);
        }

        [Test]
        public void CameraPerspectiveZnear()
        {
            var json = JsonSerializer.Serialize(new CameraPerspective { Znear = 0.25f }, GltfJsonContext.Default.CameraPerspective);
            Assert.AreEqual(@"{""znear"":0.25}", json);
        }

        [Test]
        public void ImageDefault()
        {
            var json = JsonSerializer.Serialize(new Image(), GltfJsonContext.Default.Image);
            Assert.AreEqual("{}", json);
        }

        [Test]
        public void ImageName()
        {
            var json = JsonSerializer.Serialize(new Image { Name = "i" }, GltfJsonContext.Default.Image);
            Assert.AreEqual(@"{""name"":""i""}", json);
        }

        [Test]
        public void ImageUri()
        {
            var json = JsonSerializer.Serialize(new Image { Uri = new UriValue("texture.png") }, GltfJsonContext.Default.Image);
            Assert.AreEqual(@"{""uri"":""texture.png""}", json);
        }

        [Test]
        [TestCase(ImageMimeType.Jpeg, "image/jpeg")]
        [TestCase(ImageMimeType.Png, "image/png")]
        [TestCase(ImageMimeType.Ktx2, "image/ktx2")]
        [TestCase(ImageMimeType.WebP, "image/webp")]
        public void ImageMimeTypeTest(ImageMimeType value, string expected)
        {
            var json = JsonSerializer.Serialize(new Image { MimeType = value }, GltfJsonContext.Default.Image);
            Assert.AreEqual($@"{{""mimeType"":""{expected}""}}", json);
        }

        [Test]
        public void ImageBufferView()
        {
            var json = JsonSerializer.Serialize(new Image { BufferView = 5 }, GltfJsonContext.Default.Image);
            Assert.AreEqual(@"{""bufferView"":5}", json);
        }

        [Test]
        public void MaterialDefault()
        {
            var json = JsonSerializer.Serialize(new Material(), GltfJsonContext.Default.Material);
            Assert.AreEqual("{}", json);
        }

        [Test]
        public void MaterialName()
        {
            var json = JsonSerializer.Serialize(new Material { Name = "m" }, GltfJsonContext.Default.Material);
            Assert.AreEqual(@"{""name"":""m""}", json);
        }

        [Test]
        public void MaterialPbrMetallicRoughness()
        {
            var json = JsonSerializer.Serialize(
                new Material { PbrMetallicRoughness = new PbrMetallicRoughness() },
                GltfJsonContext.Default.Material);
            Assert.AreEqual(@"{""pbrMetallicRoughness"":{}}", json);
        }

        [Test]
        public void MaterialNormalTexture()
        {
            var json = JsonSerializer.Serialize(
                new Material { NormalTexture = new NormalTextureInfo { Index = 0 } },
                GltfJsonContext.Default.Material);
            Assert.AreEqual(@"{""normalTexture"":{""index"":0}}", json);
        }

        [Test]
        public void MaterialOcclusionTexture()
        {
            var json = JsonSerializer.Serialize(
                new Material { OcclusionTexture = new OcclusionTextureInfo { Index = 0 } },
                GltfJsonContext.Default.Material);
            Assert.AreEqual(@"{""occlusionTexture"":{""index"":0}}", json);
        }

        [Test]
        public void MaterialEmissiveTexture()
        {
            var json = JsonSerializer.Serialize(
                new Material { EmissiveTexture = new TextureInfo { Index = 0 } },
                GltfJsonContext.Default.Material);
            Assert.AreEqual(@"{""emissiveTexture"":{""index"":0}}", json);
        }

        [Test]
        public void MaterialEmissiveFactor()
        {
            var json = JsonSerializer.Serialize(
                new Material { EmissiveFactor = new Color(0.5f, 0.25f, 0.75f) },
                GltfJsonContext.Default.Material);
            Assert.AreEqual(@"{""emissiveFactor"":[0.5,0.25,0.75]}", json);
        }

        [Test]
        [TestCase(AlphaMode.Mask, "MASK")]
        [TestCase(AlphaMode.Blend, "BLEND")]
        public void MaterialAlphaMode(AlphaMode value, string expected)
        {
            var json = JsonSerializer.Serialize(new Material { AlphaMode = value }, GltfJsonContext.Default.Material);
            Assert.AreEqual($@"{{""alphaMode"":""{expected}""}}", json);
        }

        [Test]
        public void MaterialAlphaCutoffDefault()
        {
            var json = JsonSerializer.Serialize(new Material { AlphaCutoff = 0.5f }, GltfJsonContext.Default.Material);
            Assert.AreEqual("{}", json);
        }

        [Test]
        public void MaterialAlphaCutoffCustom()
        {
            var json = JsonSerializer.Serialize(new Material { AlphaCutoff = 0.25f }, GltfJsonContext.Default.Material);
            Assert.AreEqual(@"{""alphaCutoff"":0.25}", json);
        }

        [Test]
        public void MaterialDoubleSided()
        {
            var json = JsonSerializer.Serialize(new Material { DoubleSided = true }, GltfJsonContext.Default.Material);
            Assert.AreEqual(@"{""doubleSided"":true}", json);
        }

        [Test]
        public void MaterialExtensions()
        {
            var json = JsonSerializer.Serialize(
                new Material { Extensions = new MaterialExtensions { Unlit = new MaterialUnlit() } },
                GltfJsonContext.Default.Material);
            Assert.AreEqual(@"{""extensions"":{""KHR_materials_unlit"":{}}}", json);
        }

        [Test]
        public void MaterialExtensionsDefault()
        {
            var json = JsonSerializer.Serialize(new MaterialExtensions(), GltfJsonContext.Default.MaterialExtensions);
            Assert.AreEqual("{}", json);
        }

        [Test]
        public void MaterialExtensionsPbrSpecularGlossiness()
        {
            var json = JsonSerializer.Serialize(
                new MaterialExtensions { PbrSpecularGlossiness = new PbrSpecularGlossiness() },
                GltfJsonContext.Default.MaterialExtensions);
            Assert.AreEqual(@"{""KHR_materials_pbrSpecularGlossiness"":{}}", json);
        }

        [Test]
        public void MaterialExtensionsUnlit()
        {
            var json = JsonSerializer.Serialize(
                new MaterialExtensions { Unlit = new MaterialUnlit() },
                GltfJsonContext.Default.MaterialExtensions);
            Assert.AreEqual(@"{""KHR_materials_unlit"":{}}", json);
        }

        [Test]
        public void MaterialExtensionsTransmission()
        {
            var json = JsonSerializer.Serialize(
                new MaterialExtensions { Transmission = new Transmission() },
                GltfJsonContext.Default.MaterialExtensions);
            Assert.AreEqual(@"{""KHR_materials_transmission"":{}}", json);
        }

        [Test]
        public void MaterialExtensionsClearcoat()
        {
            var json = JsonSerializer.Serialize(
                new MaterialExtensions { Clearcoat = new ClearCoat() },
                GltfJsonContext.Default.MaterialExtensions);
            Assert.AreEqual(@"{""KHR_materials_clearcoat"":{}}", json);
        }

        [Test]
        public void MaterialExtensionsSheen()
        {
            var json = JsonSerializer.Serialize(
                new MaterialExtensions { Sheen = new Sheen() },
                GltfJsonContext.Default.MaterialExtensions);
            Assert.AreEqual(@"{""KHR_materials_sheen"":{}}", json);
        }

        [Test]
        public void MaterialExtensionsSpecular()
        {
            var json = JsonSerializer.Serialize(
                new MaterialExtensions { Specular = new MaterialSpecular() },
                GltfJsonContext.Default.MaterialExtensions);
            Assert.AreEqual(@"{""KHR_materials_specular"":{}}", json);
        }

        [Test]
        public void MaterialExtensionsIor()
        {
            var json = JsonSerializer.Serialize(
                new MaterialExtensions { IndexOfRefraction = new MaterialIor() },
                GltfJsonContext.Default.MaterialExtensions);
            Assert.AreEqual(@"{""KHR_materials_ior"":{}}", json);
        }

        [Test]
        public void PbrMetallicRoughnessDefault()
        {
            var json = JsonSerializer.Serialize(new PbrMetallicRoughness(), GltfJsonContext.Default.PbrMetallicRoughness);
            Assert.AreEqual("{}", json);
        }

        [Test]
        public void PbrMetallicRoughnessBaseColorTexture()
        {
            var json = JsonSerializer.Serialize(
                new PbrMetallicRoughness { BaseColorTexture = new TextureInfo { Index = 0 } },
                GltfJsonContext.Default.PbrMetallicRoughness);
            Assert.AreEqual(@"{""baseColorTexture"":{""index"":0}}", json);
        }

        [Test]
        public void PbrMetallicRoughnessMetallicRoughnessTexture()
        {
            var json = JsonSerializer.Serialize(
                new PbrMetallicRoughness { MetallicRoughnessTexture = new TextureInfo { Index = 1 } },
                GltfJsonContext.Default.PbrMetallicRoughness);
            Assert.AreEqual(@"{""metallicRoughnessTexture"":{""index"":1}}", json);
        }

        [Test]
        public void PbrMetallicRoughnessBaseColorFactor()
        {
            var json = JsonSerializer.Serialize(
                new PbrMetallicRoughness { BaseColorFactor = new ColorAlpha(0.5f, 0.25f, 0.75f, 0.5f) },
                GltfJsonContext.Default.PbrMetallicRoughness);
            Assert.AreEqual(@"{""baseColorFactor"":[0.5,0.25,0.75,0.5]}", json);
        }

        [Test]
        public void PbrMetallicRoughnessMetallicFactor()
        {
            var json = JsonSerializer.Serialize(
                new PbrMetallicRoughness { MetallicFactor = 0.25f },
                GltfJsonContext.Default.PbrMetallicRoughness);
            Assert.AreEqual(@"{""metallicFactor"":0.25}", json);
        }

        [Test]
        public void PbrMetallicRoughnessRoughnessFactor()
        {
            var json = JsonSerializer.Serialize(
                new PbrMetallicRoughness { RoughnessFactor = 0.5f },
                GltfJsonContext.Default.PbrMetallicRoughness);
            Assert.AreEqual(@"{""roughnessFactor"":0.5}", json);
        }

        [Test]
        public void PbrSpecularGlossinessDefault()
        {
            var json = JsonSerializer.Serialize(new PbrSpecularGlossiness(), GltfJsonContext.Default.PbrSpecularGlossiness);
            Assert.AreEqual("{}", json);
        }

        [Test]
        public void PbrSpecularGlossinessDiffuseFactor()
        {
            var json = JsonSerializer.Serialize(
                new PbrSpecularGlossiness { DiffuseFactor = new ColorAlpha(0.5f, 0.5f, 0.5f, 0.5f) },
                GltfJsonContext.Default.PbrSpecularGlossiness);
            Assert.AreEqual(@"{""diffuseFactor"":[0.5,0.5,0.5,0.5]}", json);
        }

        [Test]
        public void PbrSpecularGlossinessDiffuseTexture()
        {
            var json = JsonSerializer.Serialize(
                new PbrSpecularGlossiness { DiffuseTexture = new TextureInfo { Index = 0 } },
                GltfJsonContext.Default.PbrSpecularGlossiness);
            Assert.AreEqual(@"{""diffuseTexture"":{""index"":0}}", json);
        }

        [Test]
        public void PbrSpecularGlossinessSpecularFactor()
        {
            var json = JsonSerializer.Serialize(
                new PbrSpecularGlossiness { SpecularFactor = new Color(0.5f, 0.5f, 0.5f) },
                GltfJsonContext.Default.PbrSpecularGlossiness);
            Assert.AreEqual(@"{""specularFactor"":[0.5,0.5,0.5]}", json);
        }

        [Test]
        public void PbrSpecularGlossinessGlossinessFactor()
        {
            var json = JsonSerializer.Serialize(
                new PbrSpecularGlossiness { GlossinessFactor = 0.5f },
                GltfJsonContext.Default.PbrSpecularGlossiness);
            Assert.AreEqual(@"{""glossinessFactor"":0.5}", json);
        }

        [Test]
        public void PbrSpecularGlossinessSpecularGlossinessTexture()
        {
            var json = JsonSerializer.Serialize(
                new PbrSpecularGlossiness { SpecularGlossinessTexture = new TextureInfo { Index = 0 } },
                GltfJsonContext.Default.PbrSpecularGlossiness);
            Assert.AreEqual(@"{""specularGlossinessTexture"":{""index"":0}}", json);
        }

        [Test]
        public void MaterialUnlitDefault()
        {
            var json = JsonSerializer.Serialize(new MaterialUnlit(), GltfJsonContext.Default.MaterialUnlit);
            Assert.AreEqual("{}", json);
        }

        [Test]
        public void TransmissionDefault()
        {
            var json = JsonSerializer.Serialize(new Transmission(), GltfJsonContext.Default.Transmission);
            Assert.AreEqual("{}", json);
        }

        [Test]
        public void TransmissionFactor()
        {
            var json = JsonSerializer.Serialize(new Transmission { TransmissionFactor = 0.5f }, GltfJsonContext.Default.Transmission);
            Assert.AreEqual(@"{""transmissionFactor"":0.5}", json);
        }

        [Test]
        public void TransmissionTexture()
        {
            var json = JsonSerializer.Serialize(
                new Transmission { TransmissionTexture = new TextureInfo { Index = 0 } },
                GltfJsonContext.Default.Transmission);
            Assert.AreEqual(@"{""transmissionTexture"":{""index"":0}}", json);
        }

        [Test]
        public void ClearCoatDefault()
        {
            var json = JsonSerializer.Serialize(new ClearCoat(), GltfJsonContext.Default.ClearCoat);
            Assert.AreEqual("{}", json);
        }

        [Test]
        public void ClearCoatFactor()
        {
            var json = JsonSerializer.Serialize(new ClearCoat { ClearcoatFactor = 0.5f }, GltfJsonContext.Default.ClearCoat);
            Assert.AreEqual(@"{""clearcoatFactor"":0.5}", json);
        }

        [Test]
        public void ClearCoatTexture()
        {
            var json = JsonSerializer.Serialize(
                new ClearCoat { ClearcoatTexture = new TextureInfo { Index = 0 } },
                GltfJsonContext.Default.ClearCoat);
            Assert.AreEqual(@"{""clearcoatTexture"":{""index"":0}}", json);
        }

        [Test]
        public void ClearCoatRoughnessFactor()
        {
            var json = JsonSerializer.Serialize(new ClearCoat { ClearcoatRoughnessFactor = 0.25f }, GltfJsonContext.Default.ClearCoat);
            Assert.AreEqual(@"{""clearcoatRoughnessFactor"":0.25}", json);
        }

        [Test]
        public void ClearCoatRoughnessTexture()
        {
            var json = JsonSerializer.Serialize(
                new ClearCoat { ClearcoatRoughnessTexture = new TextureInfo { Index = 1 } },
                GltfJsonContext.Default.ClearCoat);
            Assert.AreEqual(@"{""clearcoatRoughnessTexture"":{""index"":1}}", json);
        }

        [Test]
        public void ClearCoatNormalTexture()
        {
            var json = JsonSerializer.Serialize(
                new ClearCoat { ClearcoatNormalTexture = new NormalTextureInfo { Index = 2 } },
                GltfJsonContext.Default.ClearCoat);
            Assert.AreEqual(@"{""clearcoatNormalTexture"":{""index"":2}}", json);
        }

        [Test]
        public void SheenDefault()
        {
            var json = JsonSerializer.Serialize(new Sheen(), GltfJsonContext.Default.Sheen);
            Assert.AreEqual("{}", json);
        }

        [Test]
        public void SheenColorFactor()
        {
            var json = JsonSerializer.Serialize(new Sheen { SheenColorFactor = new Color(0.5f, 0.25f, 0.75f) }, GltfJsonContext.Default.Sheen);
            Assert.AreEqual(@"{""sheenColorFactor"":[0.5,0.25,0.75]}", json);
        }

        [Test]
        public void SheenColorTexture()
        {
            var json = JsonSerializer.Serialize(
                new Sheen { SheenColorFactor = Color.White, SheenColorTexture = new TextureInfo { Index = 0 } },
                GltfJsonContext.Default.Sheen);
            Assert.AreEqual(@"{""sheenColorFactor"":[1,1,1],""sheenColorTexture"":{""index"":0}}", json);
        }

        [Test]
        public void SheenRoughnessFactor()
        {
            var json = JsonSerializer.Serialize(new Sheen { SheenRoughnessFactor = 0.5f }, GltfJsonContext.Default.Sheen);
            Assert.AreEqual(@"{""sheenRoughnessFactor"":0.5}", json);
        }

        [Test]
        public void SheenRoughnessTexture()
        {
            var json = JsonSerializer.Serialize(
                new Sheen { SheenRoughnessTexture = new TextureInfo { Index = 0 } },
                GltfJsonContext.Default.Sheen);
            Assert.AreEqual(@"{""sheenRoughnessTexture"":{""index"":0}}", json);
        }

        [Test]
        public void MaterialSpecularDefault()
        {
            var json = JsonSerializer.Serialize(new MaterialSpecular(), GltfJsonContext.Default.MaterialSpecular);
            Assert.AreEqual("{}", json);
        }

        [Test]
        public void MaterialSpecularFactor()
        {
            var json = JsonSerializer.Serialize(new MaterialSpecular { SpecularFactor = 0.5f }, GltfJsonContext.Default.MaterialSpecular);
            Assert.AreEqual(@"{""specularFactor"":0.5}", json);
        }

        [Test]
        public void MaterialSpecularTexture()
        {
            var json = JsonSerializer.Serialize(
                new MaterialSpecular { SpecularTexture = new TextureInfo { Index = 0 } },
                GltfJsonContext.Default.MaterialSpecular);
            Assert.AreEqual(@"{""specularTexture"":{""index"":0}}", json);
        }

        [Test]
        public void MaterialSpecularColorFactor()
        {
            var json = JsonSerializer.Serialize(
                new MaterialSpecular { SpecularColorFactor = new Color(0.5f, 0.25f, 0.75f) },
                GltfJsonContext.Default.MaterialSpecular);
            Assert.AreEqual(@"{""specularColorFactor"":[0.5,0.25,0.75]}", json);
        }

        [Test]
        public void MaterialSpecularColorTexture()
        {
            var json = JsonSerializer.Serialize(
                new MaterialSpecular { SpecularColorTexture = new TextureInfo { Index = 0 } },
                GltfJsonContext.Default.MaterialSpecular);
            Assert.AreEqual(@"{""specularColorTexture"":{""index"":0}}", json);
        }

        [Test]
        public void MaterialIorDefault()
        {
            var json = JsonSerializer.Serialize(new MaterialIor(), GltfJsonContext.Default.MaterialIor);
            Assert.AreEqual("{}", json);
        }

        [Test]
        public void MaterialIorCustom()
        {
            var json = JsonSerializer.Serialize(new MaterialIor { Ior = 1.25f }, GltfJsonContext.Default.MaterialIor);
            Assert.AreEqual(@"{""ior"":1.25}", json);
        }

        [Test]
        public void MeshDefault()
        {
            var json = JsonSerializer.Serialize(new Mesh(), GltfJsonContext.Default.Mesh);
            Assert.AreEqual("{}", json);
        }

        [Test]
        public void MeshName()
        {
            var json = JsonSerializer.Serialize(new Mesh { Name = "m" }, GltfJsonContext.Default.Mesh);
            Assert.AreEqual(@"{""name"":""m""}", json);
        }

        [Test]
        public void MeshPrimitives()
        {
            var json = JsonSerializer.Serialize(
                new Mesh { Primitives = new List<MeshPrimitive> { new() { Indices = 0 } } },
                GltfJsonContext.Default.Mesh);
            Assert.AreEqual(@"{""primitives"":[{""indices"":0}]}", json);
        }

        [Test]
        public void MeshExtras()
        {
            var json = JsonSerializer.Serialize(
                new Mesh { Extras = new MeshExtras { TargetNames = new List<string> { "k1" } } },
                GltfJsonContext.Default.Mesh);
            Assert.AreEqual(@"{""extras"":{""targetNames"":[""k1""]}}", json);
        }

        [Test]
        public void MeshWeights()
        {
            var json = JsonSerializer.Serialize(
                new Mesh { Weights = new List<float> { 0f, 1f } },
                GltfJsonContext.Default.Mesh);
            Assert.AreEqual(@"{""weights"":[0,1]}", json);
        }

        [Test]
        public void MeshExtrasDefault()
        {
            var json = JsonSerializer.Serialize(new MeshExtras(), GltfJsonContext.Default.MeshExtras);
            Assert.AreEqual("{}", json);
        }

        [Test]
        public void MeshExtrasTargetNames()
        {
            var json = JsonSerializer.Serialize(
                new MeshExtras { TargetNames = new List<string> { "a", "b" } },
                GltfJsonContext.Default.MeshExtras);
            Assert.AreEqual(@"{""targetNames"":[""a"",""b""]}", json);
        }

        [Test]
        public void MeshPrimitiveAttributes()
        {
            var json = JsonSerializer.Serialize(
                new MeshPrimitive { Attributes = new Attributes { Position = 0 } },
                GltfJsonContext.Default.MeshPrimitive);
            Assert.AreEqual(@"{""attributes"":{""POSITION"":0}}", json);
        }

        [Test]
        public void MeshPrimitiveIndices()
        {
            var json = JsonSerializer.Serialize(new MeshPrimitive { Indices = 3 }, GltfJsonContext.Default.MeshPrimitive);
            Assert.AreEqual(@"{""indices"":3}", json);
        }

        [Test]
        public void MeshPrimitiveMaterial()
        {
            var json = JsonSerializer.Serialize(new MeshPrimitive { Material = 7 }, GltfJsonContext.Default.MeshPrimitive);
            Assert.AreEqual(@"{""material"":7}", json);
        }

        [Test]
        public void MeshPrimitiveTargets()
        {
            var json = JsonSerializer.Serialize(
                new MeshPrimitive { Targets = new List<MorphTarget> { new() { Position = 0 } } },
                GltfJsonContext.Default.MeshPrimitive);
            Assert.AreEqual(@"{""targets"":[{""POSITION"":0}]}", json);
        }

        [Test]
        public void MeshPrimitiveExtensions()
        {
            var json = JsonSerializer.Serialize(
                new MeshPrimitive { Extensions = new MeshPrimitiveExtensions() },
                GltfJsonContext.Default.MeshPrimitive);
            Assert.AreEqual(@"{""extensions"":{}}", json);
        }

        [Test]
        public void AttributesDefault()
        {
            var json = JsonSerializer.Serialize(new Attributes(), GltfJsonContext.Default.Attributes);
            Assert.AreEqual("{}", json);
        }

        [Test]
        public void AttributesAllProperties()
        {
            var obj = new Attributes
            {
                Position = 0,
                Normal = 1,
                Tangent = 2,
                TexCoords = new List<int?> { 3, 4, 5, 6, 7, 8, 9, 10, 11 },
                Colors = new List<int?> { 12 },
                Joints = new List<int?> { 13 },
                Weights = new List<int?> { 14 },
            };
            var json = JsonSerializer.Serialize(obj, GltfJsonContext.Default.Attributes);
            Assert.AreEqual(
                @"{""POSITION"":0,""NORMAL"":1,""TANGENT"":2,""TEXCOORD_0"":3,""TEXCOORD_1"":4,""TEXCOORD_2"":5,""TEXCOORD_3"":6,""TEXCOORD_4"":7,""TEXCOORD_5"":8,""TEXCOORD_6"":9,""TEXCOORD_7"":10,""TEXCOORD_8"":11,""COLOR_0"":12,""JOINTS_0"":13,""WEIGHTS_0"":14}",
                json);
        }

        [Test]
        public void MorphTargetDefault()
        {
            var json = JsonSerializer.Serialize(new MorphTarget(), GltfJsonContext.Default.MorphTarget);
            Assert.AreEqual("{}", json);
        }

        [Test]
        public void MorphTargetAllProperties()
        {
            var obj = new MorphTarget { Position = 1, Normal = 2, Tangent = 3 };
            var json = JsonSerializer.Serialize(obj, GltfJsonContext.Default.MorphTarget);
            Assert.AreEqual(@"{""POSITION"":1,""NORMAL"":2,""TANGENT"":3}", json);
        }

        [Test]
        public void MeshPrimitiveExtensionsDefault()
        {
            var json = JsonSerializer.Serialize(new MeshPrimitiveExtensions(), GltfJsonContext.Default.MeshPrimitiveExtensions);
            Assert.AreEqual("{}", json);
        }

        [Test]
        public void MeshPrimitiveExtensionsMaterialsVariants()
        {
            var json = JsonSerializer.Serialize(
                new MeshPrimitiveExtensions
                {
                    MaterialsVariants = new MaterialsVariantsMeshPrimitiveExtension
                    {
                        Mappings = new List<MaterialVariantsMapping> { new() { Material = 0, Variants = new List<int> { 0 } } }
                    }
                },
                GltfJsonContext.Default.MeshPrimitiveExtensions);
            Assert.AreEqual(@"{""KHR_materials_variants"":{""mappings"":[{""material"":0,""variants"":[0]}]}}", json);
        }

        [Test]
        public void MaterialsVariantsMeshPrimitiveExtensionDefault()
        {
            var json = JsonSerializer.Serialize(
                new MaterialsVariantsMeshPrimitiveExtension(),
                GltfJsonContext.Default.MaterialsVariantsMeshPrimitiveExtension);
            Assert.AreEqual("{}", json);
        }

        [Test]
        public void MaterialsVariantsMeshPrimitiveExtensionMappings()
        {
            var json = JsonSerializer.Serialize(
                new MaterialsVariantsMeshPrimitiveExtension
                {
                    Mappings = new List<MaterialVariantsMapping> { new() { Material = 1, Variants = new List<int> { 0, 1 } } }
                },
                GltfJsonContext.Default.MaterialsVariantsMeshPrimitiveExtension);
            Assert.AreEqual(@"{""mappings"":[{""material"":1,""variants"":[0,1]}]}", json);
        }

        [Test]
        public void MaterialVariantsMappingDefault()
        {
            var json = JsonSerializer.Serialize(new MaterialVariantsMapping(), GltfJsonContext.Default.MaterialVariantsMapping);
            Assert.AreEqual("{}", json);
        }

        [Test]
        public void MaterialVariantsMappingMaterial()
        {
            var json = JsonSerializer.Serialize(new MaterialVariantsMapping { Material = 2 }, GltfJsonContext.Default.MaterialVariantsMapping);
            Assert.AreEqual(@"{""material"":2}", json);
        }

        [Test]
        public void MaterialVariantsMappingVariants()
        {
            var json = JsonSerializer.Serialize(
                new MaterialVariantsMapping { Variants = new List<int> { 0, 1 } },
                GltfJsonContext.Default.MaterialVariantsMapping);
            Assert.AreEqual(@"{""variants"":[0,1]}", json);
        }

        [Test]
        public void NodeDefault()
        {
            var json = JsonSerializer.Serialize(new Node(), GltfJsonContext.Default.Node);
            Assert.AreEqual("{}", json);
        }

        [Test]
        public void NodeName()
        {
            var json = JsonSerializer.Serialize(new Node { Name = "n" }, GltfJsonContext.Default.Node);
            Assert.AreEqual(@"{""name"":""n""}", json);
        }

        [Test]
        public void NodeChildren()
        {
            var json = JsonSerializer.Serialize(new Node { Children = new List<uint> { 1, 2 } }, GltfJsonContext.Default.Node);
            Assert.AreEqual(@"{""children"":[1,2]}", json);
        }

        [Test]
        public void NodeMesh()
        {
            var json = JsonSerializer.Serialize(new Node { Mesh = 3 }, GltfJsonContext.Default.Node);
            Assert.AreEqual(@"{""mesh"":3}", json);
        }

        [Test]
        public void NodeMatrix()
        {
            var matrix = new double4x4(
                new double4(1, 0, 0, 0),
                new double4(0, 1, 0, 0),
                new double4(0, 0, 1, 0),
                new double4(1, 2, 3, 1)
            );
            var json = JsonSerializer.Serialize(new Node { Matrix = matrix }, GltfJsonContext.Default.Node);
            Assert.AreEqual(@"{""matrix"":[1,0,0,0,0,1,0,0,0,0,1,0,1,2,3,1]}", json);
        }

        [Test]
        public void NodeRotation()
        {
            var json = JsonSerializer.Serialize(new Node { Rotation = new double4(0, 0, 0, 1) }, GltfJsonContext.Default.Node);
            Assert.AreEqual(@"{""rotation"":[0,0,0,1]}", json);
        }

        [Test]
        public void NodeScale()
        {
            var json = JsonSerializer.Serialize(new Node { Scale = new double3(1, 2, 3) }, GltfJsonContext.Default.Node);
            Assert.AreEqual(@"{""scale"":[1,2,3]}", json);
        }

        [Test]
        public void NodeTranslation()
        {
            var json = JsonSerializer.Serialize(new Node { Translation = new double3(4, 5, 6) }, GltfJsonContext.Default.Node);
            Assert.AreEqual(@"{""translation"":[4,5,6]}", json);
        }

        [Test]
        public void NodeWeights()
        {
            var json = JsonSerializer.Serialize(new Node { Weights = new List<float> { 0.5f } }, GltfJsonContext.Default.Node);
            Assert.AreEqual(@"{""weights"":[0.5]}", json);
        }

        [Test]
        public void NodeSkin()
        {
            var json = JsonSerializer.Serialize(new Node { Skin = 1 }, GltfJsonContext.Default.Node);
            Assert.AreEqual(@"{""skin"":1}", json);
        }

        [Test]
        public void NodeCamera()
        {
            var json = JsonSerializer.Serialize(new Node { Camera = 2 }, GltfJsonContext.Default.Node);
            Assert.AreEqual(@"{""camera"":2}", json);
        }

        [Test]
        public void NodeExtensions()
        {
            var json = JsonSerializer.Serialize(
                new Node { Extensions = new NodeExtensions { LightsPunctual = new NodeLightsPunctual { Light = 0 } } },
                GltfJsonContext.Default.Node);
            Assert.AreEqual(@"{""extensions"":{""KHR_lights_punctual"":{""light"":0}}}", json);
        }

        [Test]
        public void MeshExtrasCustomProperty()
        {
            var mesh = new Mesh { Extras = new MeshExtras() };
            mesh.Extras.Set("uuid", "3fb0442f-0bd4-4947-bc88-b99b0adb16d0");
            mesh.Extras.Set("value", 12.3);
            var json = JsonSerializer.Serialize(mesh, GltfJsonContext.Default.Mesh);
            Assert.AreEqual(@"{""extras"":{""uuid"":""3fb0442f-0bd4-4947-bc88-b99b0adb16d0"",""value"":12.300000000000001}}", json);
        }

        [Test]
        public void MeshExtrasCustomPropertyConflict()
        {
            var mesh = new Mesh { Extras = new MeshExtras() };
            mesh.Extras.TargetNames = new List<string> { "a", "b", "c" };
            mesh.Extras.Set("targetNames", new List<string> { "1", "2", "3" });
            var json = JsonSerializer.Serialize(mesh, GltfJsonContext.Default.Mesh);
            // Note that there's two members with name "targetNames". That's expected. Users need to make sure they
            // don't run into conflicts. See glTF specification about JSON encoding.
            // https://registry.khronos.org/glTF/specs/2.0/glTF-2.0.html#json-encoding
            // > 4. Property names (keys) within JSON objects SHOULD be unique. glTF client implementations SHOULD
            // >    override lexically preceding values for the same key.
            Assert.AreEqual(@"{""extras"":{""targetNames"":[""a"",""b"",""c""],""targetNames"":[""1"",""2"",""3""]}}", json);
        }

        [Test]
        public void NodeCustomExtensions()
        {
            var node = new Node { Extensions = new NodeExtensions() };
            var extension = new Dictionary<string, object>
            {
                ["property"] = "value",
                ["value"] = 42.0
            };
            node.Extensions.Set("MY_custom_extension", extension);
            var json = JsonSerializer.Serialize(node, GltfJsonContext.Default.Node);
            Assert.AreEqual(@"{""extensions"":{""MY_custom_extension"":{""property"":""value"",""value"":42}}}", json);
        }

        [Test]
        public void NodeExtensionsDefault()
        {
            var json = JsonSerializer.Serialize(new NodeExtensions(), GltfJsonContext.Default.NodeExtensions);
            Assert.AreEqual("{}", json);
        }

        [Test]
        public void NodeExtensionsMeshGpuInstancing()
        {
            var json = JsonSerializer.Serialize(
                new NodeExtensions { MeshGpuInstancing = new MeshGpuInstancing { Attributes = new InstancesAttributes { Translation = 0 } } },
                GltfJsonContext.Default.NodeExtensions);
            Assert.AreEqual(@"{""EXT_mesh_gpu_instancing"":{""attributes"":{""TRANSLATION"":0}}}", json);
        }

        [Test]
        public void NodeExtensionsLightsPunctual()
        {
            var json = JsonSerializer.Serialize(
                new NodeExtensions { LightsPunctual = new NodeLightsPunctual { Light = 4 } },
                GltfJsonContext.Default.NodeExtensions);
            Assert.AreEqual(@"{""KHR_lights_punctual"":{""light"":4}}", json);
        }

        [Test]
        public void MeshGpuInstancingDefault()
        {
            var json = JsonSerializer.Serialize(new MeshGpuInstancing(), GltfJsonContext.Default.MeshGpuInstancing);
            Assert.AreEqual("{}", json);
        }

        [Test]
        public void AttributesScalars()
        {
            var json = JsonSerializer.Serialize(
                new Attributes { Position = 1, Normal = 2, Tangent = 3 },
                GltfJsonContext.Default.Attributes);
            Assert.AreEqual(@"{""POSITION"":1,""NORMAL"":2,""TANGENT"":3}", json);
        }

        [Test]
        public void AttributesTexCoordsContiguous()
        {
            var attrs = new Attributes();
            for (var i = 0; i < 9; i++) attrs.SetTexCoord(i, 10 + i);
            var json = JsonSerializer.Serialize(attrs, GltfJsonContext.Default.Attributes);
            Assert.AreEqual(
                @"{""TEXCOORD_0"":10,""TEXCOORD_1"":11,""TEXCOORD_2"":12,""TEXCOORD_3"":13,""TEXCOORD_4"":14,""TEXCOORD_5"":15,""TEXCOORD_6"":16,""TEXCOORD_7"":17,""TEXCOORD_8"":18}",
                json);
        }

        [Test]
        public void AttributesTexCoordsSparse()
        {
            var attrs = new Attributes();
            attrs.SetTexCoord(2, 7);
            var json = JsonSerializer.Serialize(attrs, GltfJsonContext.Default.Attributes);
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
            var json = JsonSerializer.Serialize(attrs, GltfJsonContext.Default.Attributes);
            Assert.AreEqual(
                @"{""COLOR_0"":11,""COLOR_1"":12,""JOINTS_0"":20,""JOINTS_1"":21,""WEIGHTS_0"":30,""WEIGHTS_1"":31}",
                json);
        }

        [Test]
        public void AttributesHighIndex()
        {
            var attrs = new Attributes();
            attrs.SetTexCoord(12, 99);
            var json = JsonSerializer.Serialize(attrs, GltfJsonContext.Default.Attributes);
            Assert.AreEqual(@"{""TEXCOORD_12"":99}", json);
        }

        [Test]
        public void AttributesCustomSemantic()
        {
            // TODO: Construct attrs via constructor once attrs.ExtensionData becomes writable.
            var attrs = JsonSerializer.Deserialize(
                @"{""POSITION"":0,""_TEMPERATURE"":5}",
                GltfJsonContext.Default.Attributes);
            var json = JsonSerializer.Serialize(attrs, GltfJsonContext.Default.Attributes);
            Assert.AreEqual(@"{""POSITION"":0,""_TEMPERATURE"":5}", json);
        }

        [Test]
        public void MeshGpuInstancingAttributes()
        {
            var json = JsonSerializer.Serialize(
                new MeshGpuInstancing { Attributes = new InstancesAttributes { Translation = 0, Rotation = 1, Scale = 2 } },
                GltfJsonContext.Default.MeshGpuInstancing);
            Assert.AreEqual(@"{""attributes"":{""TRANSLATION"":0,""ROTATION"":1,""SCALE"":2}}", json);
        }

        [Test]
        public void InstancesAttributesDefault()
        {
            var json = JsonSerializer.Serialize(new InstancesAttributes(), GltfJsonContext.Default.InstancesAttributes);
            Assert.AreEqual("{}", json);
        }

        [Test]
        public void InstancesAttributesAllProperties()
        {
            var obj = new InstancesAttributes { Translation = 0, Rotation = 1, Scale = 2 };
            var json = JsonSerializer.Serialize(obj, GltfJsonContext.Default.InstancesAttributes);
            Assert.AreEqual(@"{""TRANSLATION"":0,""ROTATION"":1,""SCALE"":2}", json);
        }

        [Test]
        public void NodeLightsPunctualDefault()
        {
            var json = JsonSerializer.Serialize(new NodeLightsPunctual(), GltfJsonContext.Default.NodeLightsPunctual);
            Assert.AreEqual("{}", json);
        }

        [Test]
        public void NodeLightsPunctualLight()
        {
            var json = JsonSerializer.Serialize(new NodeLightsPunctual { Light = 7 }, GltfJsonContext.Default.NodeLightsPunctual);
            Assert.AreEqual(@"{""light"":7}", json);
        }

        [Test]
        public void RootExtensionsDefault()
        {
            var json = JsonSerializer.Serialize(new RootExtensions(), GltfJsonContext.Default.RootExtensions);
            Assert.AreEqual("{}", json);
        }

        [Test]
        public void RootExtensionsLightsPunctual()
        {
            var json = JsonSerializer.Serialize(
                new RootExtensions { LightsPunctual = new LightsPunctual { Lights = new List<LightPunctual>() } },
                GltfJsonContext.Default.RootExtensions);
            Assert.AreEqual(@"{""KHR_lights_punctual"":{""lights"":[]}}", json);
        }

        [Test]
        public void RootExtensionsMaterialsVariants()
        {
            var json = JsonSerializer.Serialize(
                new RootExtensions { MaterialsVariants = new MaterialsVariantsRootExtension { Variants = new List<MaterialsVariant>() } },
                GltfJsonContext.Default.RootExtensions);
            Assert.AreEqual(@"{""KHR_materials_variants"":{""variants"":[]}}", json);
        }

        [Test]
        public void LightsPunctualDefault()
        {
            var json = JsonSerializer.Serialize(new LightsPunctual(), GltfJsonContext.Default.LightsPunctual);
            Assert.AreEqual("{}", json);
        }

        [Test]
        public void LightsPunctualLights()
        {
            var json = JsonSerializer.Serialize(
                new LightsPunctual { Lights = new List<LightPunctual> { new() { Type = LightType.Directional } } },
                GltfJsonContext.Default.LightsPunctual);
            Assert.AreEqual(@"{""lights"":[{""type"":""directional""}]}", json);
        }

        [Test]
        public void LightPunctualDefault()
        {
            var json = JsonSerializer.Serialize(new LightPunctual(), GltfJsonContext.Default.LightPunctual);
            Assert.AreEqual("{}", json);
        }

        [Test]
        public void LightPunctualName()
        {
            var json = JsonSerializer.Serialize(new LightPunctual { Name = "L" }, GltfJsonContext.Default.LightPunctual);
            Assert.AreEqual(@"{""name"":""L""}", json);
        }

        [Test]
        public void LightPunctualColor()
        {
            var json = JsonSerializer.Serialize(new LightPunctual { Color = new Color(0.5f, 0.25f, 0.75f) }, GltfJsonContext.Default.LightPunctual);
            Assert.AreEqual(@"{""color"":[0.5,0.25,0.75]}", json);
        }

        [Test]
        public void LightPunctualIntensity()
        {
            var json = JsonSerializer.Serialize(new LightPunctual { Intensity = 2f }, GltfJsonContext.Default.LightPunctual);
            Assert.AreEqual(@"{""intensity"":2}", json);
        }

        [Test]
        public void LightPunctualRange()
        {
            var json = JsonSerializer.Serialize(new LightPunctual { Range = 10f }, GltfJsonContext.Default.LightPunctual);
            Assert.AreEqual(@"{""range"":10}", json);
        }

        [Test]
        public void LightPunctualSpot()
        {
            var json = JsonSerializer.Serialize(new LightPunctual { Spot = new SpotLight() }, GltfJsonContext.Default.LightPunctual);
            Assert.AreEqual(@"{""spot"":{}}", json);
        }

        [Test]
        [TestCase(LightType.Spot, "spot")]
        [TestCase(LightType.Directional, "directional")]
        [TestCase(LightType.Point, "point")]
        public void LightPunctualType(LightType value, string expected)
        {
            var json = JsonSerializer.Serialize(new LightPunctual { Type = value }, GltfJsonContext.Default.LightPunctual);
            Assert.AreEqual($@"{{""type"":""{expected}""}}", json);
        }

        [Test]
        public void SpotLightDefault()
        {
            var json = JsonSerializer.Serialize(new SpotLight(), GltfJsonContext.Default.SpotLight);
            Assert.AreEqual("{}", json);
        }

        [Test]
        public void SpotLightInnerConeAngle()
        {
            var json = JsonSerializer.Serialize(new SpotLight { InnerConeAngle = 0.25f }, GltfJsonContext.Default.SpotLight);
            Assert.AreEqual(@"{""innerConeAngle"":0.25}", json);
        }

        [Test]
        public void SpotLightOuterConeAngle()
        {
            var json = JsonSerializer.Serialize(new SpotLight { OuterConeAngle = 0.5f }, GltfJsonContext.Default.SpotLight);
            Assert.AreEqual(@"{""outerConeAngle"":0.5}", json);
        }

        [Test]
        public void MaterialsVariantsRootExtensionDefault()
        {
            var json = JsonSerializer.Serialize(new MaterialsVariantsRootExtension(), GltfJsonContext.Default.MaterialsVariantsRootExtension);
            Assert.AreEqual("{}", json);
        }

        [Test]
        public void MaterialsVariantsRootExtensionVariants()
        {
            var json = JsonSerializer.Serialize(
                new MaterialsVariantsRootExtension { Variants = new List<MaterialsVariant> { new() { Name = "v" } } },
                GltfJsonContext.Default.MaterialsVariantsRootExtension);
            Assert.AreEqual(@"{""variants"":[{""name"":""v""}]}", json);
        }

        [Test]
        public void MaterialsVariantDefault()
        {
            var json = JsonSerializer.Serialize(new MaterialsVariant(), GltfJsonContext.Default.MaterialsVariant);
            Assert.AreEqual("{}", json);
        }

        [Test]
        public void MaterialsVariantName()
        {
            var json = JsonSerializer.Serialize(new MaterialsVariant { Name = "x" }, GltfJsonContext.Default.MaterialsVariant);
            Assert.AreEqual(@"{""name"":""x""}", json);
        }

        [Test]
        public void SceneDefault()
        {
            var json = JsonSerializer.Serialize(new Scene(), GltfJsonContext.Default.Scene);
            Assert.AreEqual("{}", json);
        }

        [Test]
        public void SceneName()
        {
            var json = JsonSerializer.Serialize(new Scene { Name = "s" }, GltfJsonContext.Default.Scene);
            Assert.AreEqual(@"{""name"":""s""}", json);
        }

        [Test]
        public void SceneNodes()
        {
            var json = JsonSerializer.Serialize(new Scene { Nodes = new List<uint> { 0, 1 } }, GltfJsonContext.Default.Scene);
            Assert.AreEqual(@"{""nodes"":[0,1]}", json);
        }

        [Test]
        public void SkinDefault()
        {
            var json = JsonSerializer.Serialize(new Skin(), GltfJsonContext.Default.Skin);
            Assert.AreEqual("{}", json);
        }

        [Test]
        public void SkinName()
        {
            var json = JsonSerializer.Serialize(new Skin { Name = "k" }, GltfJsonContext.Default.Skin);
            Assert.AreEqual(@"{""name"":""k""}", json);
        }

        [Test]
        public void SkinInverseBindMatrices()
        {
            var json = JsonSerializer.Serialize(new Skin { InverseBindMatrices = 3 }, GltfJsonContext.Default.Skin);
            Assert.AreEqual(@"{""inverseBindMatrices"":3}", json);
        }

        [Test]
        public void SkinSkeleton()
        {
            var json = JsonSerializer.Serialize(new Skin { Skeleton = 5 }, GltfJsonContext.Default.Skin);
            Assert.AreEqual(@"{""skeleton"":5}", json);
        }

        [Test]
        public void SkinJoints()
        {
            var json = JsonSerializer.Serialize(new Skin { Joints = new List<uint> { 1, 2, 3 } }, GltfJsonContext.Default.Skin);
            Assert.AreEqual(@"{""joints"":[1,2,3]}", json);
        }

        [Test]
        public void TextureDefault()
        {
            var json = JsonSerializer.Serialize(new Texture(), GltfJsonContext.Default.Texture);
            Assert.AreEqual("{}", json);
        }

        [Test]
        public void TextureName()
        {
            var json = JsonSerializer.Serialize(new Texture { Name = "t" }, GltfJsonContext.Default.Texture);
            Assert.AreEqual(@"{""name"":""t""}", json);
        }

        [Test]
        public void TextureSampler()
        {
            var json = JsonSerializer.Serialize(new Texture { Sampler = 1 }, GltfJsonContext.Default.Texture);
            Assert.AreEqual(@"{""sampler"":1}", json);
        }

        [Test]
        public void TextureSource()
        {
            var json = JsonSerializer.Serialize(new Texture { Source = 2 }, GltfJsonContext.Default.Texture);
            Assert.AreEqual(@"{""source"":2}", json);
        }

        [Test]
        public void TextureExtensions()
        {
            var json = JsonSerializer.Serialize(
                new Texture { Extensions = new TextureExtensions { BasisU = new TextureBasisUniversal { Source = 3 } } },
                GltfJsonContext.Default.Texture);
            Assert.AreEqual(@"{""extensions"":{""KHR_texture_basisu"":{""source"":3}}}", json);
        }

        [Test]
        public void TextureExtensionsDefault()
        {
            var json = JsonSerializer.Serialize(new TextureExtensions(), GltfJsonContext.Default.TextureExtensions);
            Assert.AreEqual("{}", json);
        }

        [Test]
        public void TextureExtensionsBasisU()
        {
            var json = JsonSerializer.Serialize(
                new TextureExtensions { BasisU = new TextureBasisUniversal { Source = 1 } },
                GltfJsonContext.Default.TextureExtensions);
            Assert.AreEqual(@"{""KHR_texture_basisu"":{""source"":1}}", json);
        }

        [Test]
        public void TextureBasisUniversalDefault()
        {
            var json = JsonSerializer.Serialize(new TextureBasisUniversal(), GltfJsonContext.Default.TextureBasisUniversal);
            Assert.AreEqual("{}", json);
        }

        [Test]
        public void TextureBasisUniversalSource()
        {
            var json = JsonSerializer.Serialize(new TextureBasisUniversal { Source = 4 }, GltfJsonContext.Default.TextureBasisUniversal);
            Assert.AreEqual(@"{""source"":4}", json);
        }

        [Test]
        public void TextureInfoDefault()
        {
            var json = JsonSerializer.Serialize(new TextureInfo(), GltfJsonContext.Default.TextureInfo);
            Assert.AreEqual("{}", json);
        }

        [Test]
        public void TextureInfoIndex()
        {
            var json = JsonSerializer.Serialize(new TextureInfo { Index = 3 }, GltfJsonContext.Default.TextureInfo);
            Assert.AreEqual(@"{""index"":3}", json);
        }

        [Test]
        public void TextureInfoTexCoord()
        {
            var json = JsonSerializer.Serialize(new TextureInfo { TexCoord = 2 }, GltfJsonContext.Default.TextureInfo);
            Assert.AreEqual(@"{""texCoord"":2}", json);
        }

        [Test]
        public void TextureInfoExtensions()
        {
            var json = JsonSerializer.Serialize(
                new TextureInfo { Extensions = new TextureInfoExtensions { TextureTransform = new TextureTransform() } },
                GltfJsonContext.Default.TextureInfo);
            Assert.AreEqual(@"{""extensions"":{""KHR_texture_transform"":{}}}", json);
        }

        [Test]
        public void NormalTextureInfoDefault()
        {
            var json = JsonSerializer.Serialize(new NormalTextureInfo(), GltfJsonContext.Default.NormalTextureInfo);
            Assert.AreEqual("{}", json);
        }

        [Test]
        public void NormalTextureInfoScale()
        {
            var json = JsonSerializer.Serialize(new NormalTextureInfo { Scale = 0.5f }, GltfJsonContext.Default.NormalTextureInfo);
            Assert.AreEqual(@"{""scale"":0.5}", json);
        }

        [Test]
        public void NormalTextureInfoIndex()
        {
            var json = JsonSerializer.Serialize(new NormalTextureInfo { Index = 0 }, GltfJsonContext.Default.NormalTextureInfo);
            Assert.AreEqual(@"{""index"":0}", json);
        }

        [Test]
        public void OcclusionTextureInfoDefault()
        {
            var json = JsonSerializer.Serialize(new OcclusionTextureInfo(), GltfJsonContext.Default.OcclusionTextureInfo);
            Assert.AreEqual("{}", json);
        }

        [Test]
        public void OcclusionTextureInfoStrength()
        {
            var json = JsonSerializer.Serialize(new OcclusionTextureInfo { Strength = 0.5f }, GltfJsonContext.Default.OcclusionTextureInfo);
            Assert.AreEqual(@"{""strength"":0.5}", json);
        }

        [Test]
        public void OcclusionTextureInfoIndex()
        {
            var json = JsonSerializer.Serialize(new OcclusionTextureInfo { Index = 0 }, GltfJsonContext.Default.OcclusionTextureInfo);
            Assert.AreEqual(@"{""index"":0}", json);
        }

        [Test]
        public void TextureInfoExtensionsDefault()
        {
            var json = JsonSerializer.Serialize(new TextureInfoExtensions(), GltfJsonContext.Default.TextureInfoExtensions);
            Assert.AreEqual("{}", json);
        }

        [Test]
        public void TextureInfoExtensionsTextureTransform()
        {
            var json = JsonSerializer.Serialize(
                new TextureInfoExtensions { TextureTransform = new TextureTransform() },
                GltfJsonContext.Default.TextureInfoExtensions);
            Assert.AreEqual(@"{""KHR_texture_transform"":{}}", json);
        }

        [Test]
        public void TextureTransformDefault()
        {
            var json = JsonSerializer.Serialize(new TextureTransform(), GltfJsonContext.Default.TextureTransform);
            Assert.AreEqual(@"{}", json);
        }

        [Test]
        public void TextureTransformOffset()
        {
            var json = JsonSerializer.Serialize(
                new TextureTransform { Offset = new float2(0.5f, 0.25f) },
                GltfJsonContext.Default.TextureTransform);
            Assert.AreEqual(@"{""offset"":[0.5,0.25]}", json);
        }

        [Test]
        public void TextureTransformRotation()
        {
            var json = JsonSerializer.Serialize(
                new TextureTransform { Rotation = 0.5f },
                GltfJsonContext.Default.TextureTransform);
            Assert.AreEqual(@"{""rotation"":0.5}", json);
        }

        [Test]
        public void TextureTransformScale()
        {
            var json = JsonSerializer.Serialize(
                new TextureTransform { Scale = new float2(0.5f, 0.5f) },
                GltfJsonContext.Default.TextureTransform);
            Assert.AreEqual(@"{""scale"":[0.5,0.5]}", json);
        }

        [Test]
        public void TextureTransformTexCoord()
        {
            var json = JsonSerializer.Serialize(
                new TextureTransform { TexCoord = 1 },
                GltfJsonContext.Default.TextureTransform);
            Assert.AreEqual(@"{""texCoord"":1}", json);
        }

        [Test]
        public void ExtrasContainerDefault()
        {
            var json = JsonSerializer.Serialize(new ExtrasContainer(), GltfJsonContext.Default.ExtrasContainer);
            Assert.AreEqual("{}", json);
        }
    }
}
