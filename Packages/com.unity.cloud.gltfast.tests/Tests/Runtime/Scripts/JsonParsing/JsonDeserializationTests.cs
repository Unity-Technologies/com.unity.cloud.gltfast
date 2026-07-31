// SPDX-FileCopyrightText: 2026 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System.Text;
using NUnit.Framework;
using Unity.Cloud.Gltfast.Schema;
using Unity.Gltfast.Text.Json;
using Unity.Mathematics;
using CameraType = Unity.Cloud.Gltfast.Schema.CameraType;
using Color = Unity.Cloud.Gltfast.Schema.Color;
using LightType = Unity.Cloud.Gltfast.Schema.LightType;
using SchemaConstants = Unity.Cloud.Gltfast.Schema.Constants;

namespace Unity.Cloud.Gltfast.Tests.JsonParsing
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
            var obj = JsonSerializer.Deserialize(json, GltfJsonContext.Default.MeshPrimitive);
            Assert.AreEqual(expected, obj.Mode);
        }

        [Test]
        [TestCase(null, WrapMode.Repeat)]
        [TestCase(33071, WrapMode.ClampToEdge)]
        [TestCase(33648, WrapMode.MirroredRepeat)]
        [TestCase(10497, WrapMode.Repeat)]
        public void SamplerWrapS(int? written, WrapMode value)
        {
            var json = written.HasValue ? $@"{{""wrapS"":{written}}}" : "{}";
            var obj = JsonSerializer.Deserialize(json, GltfJsonContext.Default.Sampler);
            Assert.AreEqual(value, obj.WrapS);
            Assert.AreEqual(WrapMode.Repeat, obj.WrapT);
        }

        [Test]
        [TestCase(null, WrapMode.Repeat)]
        [TestCase(33071, WrapMode.ClampToEdge)]
        [TestCase(33648, WrapMode.MirroredRepeat)]
        [TestCase(10497, WrapMode.Repeat)]
        public void SamplerWrapT(int? written, WrapMode value)
        {
            var json = written.HasValue ? $@"{{""wrapT"":{written}}}" : "{}";
            var obj = JsonSerializer.Deserialize(json, GltfJsonContext.Default.Sampler);
            Assert.AreEqual(value, obj.WrapT);
            Assert.AreEqual(WrapMode.Repeat, obj.WrapS);
        }

        [Test]
        [TestCase(null, BufferViewTarget.Undefined)]
        [TestCase(34962, BufferViewTarget.ArrayBuffer)]
        [TestCase(34963, BufferViewTarget.ElementArrayBuffer)]
        public void BufferViewTargets(int? written, BufferViewTarget value)
        {
            var json = written.HasValue ? $@"{{""target"":{written}}}" : "{}";
            var obj = JsonSerializer.Deserialize(json, GltfJsonContext.Default.BufferView);
            Assert.AreEqual(value, obj.Target);
        }

        [Test]
        public void SamplerDefault()
        {
            var obj = JsonSerializer.Deserialize("{}", GltfJsonContext.Default.Sampler);
            Assert.IsNotNull(obj);
            Assert.IsNull(obj.Name);
            Assert.AreEqual(MagFilterMode.Undefined, obj.MagFilter);
            Assert.AreEqual(MinFilterMode.Undefined, obj.MinFilter);
            Assert.AreEqual(WrapMode.Repeat, obj.WrapS);
            Assert.AreEqual(WrapMode.Repeat, obj.WrapT);
        }

        [Test]
        public void SamplerName()
        {
            var obj = JsonSerializer.Deserialize(@"{""name"":""s""}", GltfJsonContext.Default.Sampler);
            Assert.AreEqual("s", obj.Name);
        }

        [Test]
        [TestCase(9728, MagFilterMode.Nearest)]
        [TestCase(9729, MagFilterMode.Linear)]
        public void SamplerMagFilter(int written, MagFilterMode expected)
        {
            var obj = JsonSerializer.Deserialize($@"{{""magFilter"":{written}}}", GltfJsonContext.Default.Sampler);
            Assert.AreEqual(expected, obj.MagFilter);
        }

        [Test]
        [TestCase(9728, MinFilterMode.Nearest)]
        [TestCase(9729, MinFilterMode.Linear)]
        [TestCase(9984, MinFilterMode.NearestMipmapNearest)]
        [TestCase(9985, MinFilterMode.LinearMipmapNearest)]
        [TestCase(9986, MinFilterMode.NearestMipmapLinear)]
        [TestCase(9987, MinFilterMode.LinearMipmapLinear)]
        public void SamplerMinFilter(int written, MinFilterMode expected)
        {
            var obj = JsonSerializer.Deserialize($@"{{""minFilter"":{written}}}", GltfJsonContext.Default.Sampler);
            Assert.AreEqual(expected, obj.MinFilter);
        }

        [Test]
        public void BufferViewDefault()
        {
            var obj = JsonSerializer.Deserialize("{}", GltfJsonContext.Default.BufferView);
            Assert.IsNotNull(obj);
            Assert.IsNull(obj.Name);
            Assert.AreEqual(SchemaConstants.UnsetIndex, obj.Buffer);
            Assert.AreEqual(0, obj.ByteOffset);
            Assert.AreEqual(SchemaConstants.UnsetIndex, obj.ByteLength);
            Assert.IsNull(obj.ByteStride);
            Assert.AreEqual(BufferViewTarget.Undefined, obj.Target);
        }

        [Test]
        public void BufferViewName()
        {
            var obj = JsonSerializer.Deserialize(@"{""name"":""v""}", GltfJsonContext.Default.BufferView);
            Assert.AreEqual("v", obj.Name);
        }

        [Test]
        public void BufferViewBuffer()
        {
            var obj = JsonSerializer.Deserialize(@"{""buffer"":3}", GltfJsonContext.Default.BufferView);
            Assert.AreEqual(3, obj.Buffer);
        }

        [Test]
        public void BufferViewByteOffset()
        {
            var obj = JsonSerializer.Deserialize(@"{""byteOffset"":16}", GltfJsonContext.Default.BufferView);
            Assert.AreEqual(16, obj.ByteOffset);
        }

        [Test]
        public void BufferViewByteLength()
        {
            var obj = JsonSerializer.Deserialize(@"{""byteLength"":64}", GltfJsonContext.Default.BufferView);
            Assert.AreEqual(64, obj.ByteLength);
        }

        [Test]
        public void BufferViewByteStride()
        {
            var obj = JsonSerializer.Deserialize(@"{""byteStride"":12}", GltfJsonContext.Default.BufferView);
            Assert.AreEqual(12, obj.ByteStride);
        }

        [Test]
        public void MeshPrimitiveDefault()
        {
            var obj = JsonSerializer.Deserialize("{}", GltfJsonContext.Default.MeshPrimitive);
            Assert.IsNotNull(obj);
            Assert.IsNull(obj.Attributes);
            Assert.IsNull(obj.Indices);
            Assert.IsNull(obj.Material);
            Assert.AreEqual(PrimitiveMode.Triangles, obj.Mode);
            Assert.IsNull(obj.Targets);
            Assert.IsNull(obj.Extensions);
        }

        [Test]
        public void MeshPrimitiveAttributes()
        {
            var obj = JsonSerializer.Deserialize(@"{""attributes"":{""POSITION"":0}}", GltfJsonContext.Default.MeshPrimitive);
            Assert.IsNotNull(obj.Attributes);
            Assert.AreEqual(0, obj.Attributes.Position);
        }

        [Test]
        public void MeshPrimitiveIndices()
        {
            var obj = JsonSerializer.Deserialize(@"{""indices"":3}", GltfJsonContext.Default.MeshPrimitive);
            Assert.AreEqual(3, obj.Indices);
        }

        [Test]
        public void MeshPrimitiveMaterial()
        {
            var obj = JsonSerializer.Deserialize(@"{""material"":7}", GltfJsonContext.Default.MeshPrimitive);
            Assert.AreEqual(7, obj.Material);
        }

        [Test]
        public void MeshPrimitiveTargets()
        {
            var obj = JsonSerializer.Deserialize(@"{""targets"":[{""POSITION"":0}]}", GltfJsonContext.Default.MeshPrimitive);
            Assert.IsNotNull(obj.Targets);
            Assert.AreEqual(1, obj.Targets.Count);
            Assert.AreEqual(0, obj.Targets[0].Position);
        }

        [Test]
        public void MeshPrimitiveExtensions()
        {
            var obj = JsonSerializer.Deserialize(
                @"{""extensions"":{""KHR_materials_variants"":{""mappings"":[]}}}",
                GltfJsonContext.Default.MeshPrimitive);
            Assert.IsNotNull(obj.Extensions);
            Assert.IsNotNull(obj.Extensions.MaterialsVariants);
        }

        [Test]
        public void RootDefault()
        {
            var obj = JsonSerializer.Deserialize("{}", GltfJsonContext.Default.Root);
            Assert.IsNotNull(obj);
            Assert.IsNull(obj.Asset);
            Assert.IsNull(obj.Accessors);
            Assert.IsNull(obj.Buffers);
            Assert.IsNull(obj.BufferViews);
            Assert.IsNull(obj.Cameras);
            Assert.IsNull(obj.Images);
            Assert.IsNull(obj.Materials);
            Assert.IsNull(obj.Meshes);
            Assert.IsNull(obj.Nodes);
            Assert.IsNull(obj.Samplers);
            Assert.IsNull(obj.Scene);
            Assert.IsNull(obj.Scenes);
            Assert.IsNull(obj.Skins);
            Assert.IsNull(obj.Textures);
            Assert.IsNull(obj.Extensions);
            Assert.IsNull(obj.Extras);
            Assert.IsNull(obj.ExtensionsUsed);
            Assert.IsNull(obj.ExtensionsRequired);
        }

        [Test]
        public void RootScene()
        {
            var obj = JsonSerializer.Deserialize(@"{""scene"":3}", GltfJsonContext.Default.Root);
            Assert.AreEqual(3, obj.Scene);
        }

        [Test]
        public void RootAsset()
        {
            var obj = JsonSerializer.Deserialize(@"{""asset"":{""version"":""2.0""}}", GltfJsonContext.Default.Root);
            Assert.IsNotNull(obj.Asset);
            Assert.AreEqual("2.0", obj.Asset.Version);
        }

        [Test]
        public void RootAccessors()
        {
            var obj = JsonSerializer.Deserialize(@"{""accessors"":[{""count"":1}]}", GltfJsonContext.Default.Root);
            Assert.AreEqual(1, obj.Accessors.Count);
            Assert.AreEqual(1, obj.Accessors[0].Count);
        }

        [Test]
        public void RootBuffers()
        {
            var obj = JsonSerializer.Deserialize(@"{""buffers"":[{""byteLength"":8}]}", GltfJsonContext.Default.Root);
            Assert.AreEqual(1, obj.Buffers.Count);
            Assert.AreEqual(8L, obj.Buffers[0].ByteLength);
        }

        [Test]
        public void RootBufferViews()
        {
            var obj = JsonSerializer.Deserialize(@"{""bufferViews"":[{""byteLength"":4}]}", GltfJsonContext.Default.Root);
            Assert.AreEqual(1, obj.BufferViews.Count);
            Assert.AreEqual(4, obj.BufferViews[0].ByteLength);
        }

        [Test]
        public void RootCameras()
        {
            var obj = JsonSerializer.Deserialize(@"{""cameras"":[{""type"":""perspective""}]}", GltfJsonContext.Default.Root);
            Assert.AreEqual(1, obj.Cameras.Count);
            Assert.AreEqual(CameraType.Perspective, obj.Cameras[0].Type.Value);
        }

        [Test]
        public void RootImages()
        {
            var obj = JsonSerializer.Deserialize(@"{""images"":[{""bufferView"":0}]}", GltfJsonContext.Default.Root);
            Assert.AreEqual(1, obj.Images.Count);
            Assert.AreEqual(0, obj.Images[0].BufferView);
        }

        [Test]
        public void RootMaterials()
        {
            var obj = JsonSerializer.Deserialize(@"{""materials"":[{""name"":""m""}]}", GltfJsonContext.Default.Root);
            Assert.AreEqual(1, obj.Materials.Count);
            Assert.AreEqual("m", obj.Materials[0].Name);
        }

        [Test]
        public void RootMeshes()
        {
            var obj = JsonSerializer.Deserialize(@"{""meshes"":[{""name"":""m""}]}", GltfJsonContext.Default.Root);
            Assert.AreEqual(1, obj.Meshes.Count);
            Assert.AreEqual("m", obj.Meshes[0].Name);
        }

        [Test]
        public void RootNodes()
        {
            var obj = JsonSerializer.Deserialize(@"{""nodes"":[{""mesh"":0}]}", GltfJsonContext.Default.Root);
            Assert.AreEqual(1, obj.Nodes.Count);
            Assert.AreEqual(0, obj.Nodes[0].Mesh);
        }

        [Test]
        public void RootSamplers()
        {
            var obj = JsonSerializer.Deserialize(@"{""samplers"":[{""magFilter"":9728}]}", GltfJsonContext.Default.Root);
            Assert.AreEqual(1, obj.Samplers.Count);
            Assert.AreEqual(MagFilterMode.Nearest, obj.Samplers[0].MagFilter);
        }

        [Test]
        public void RootScenes()
        {
            var obj = JsonSerializer.Deserialize(@"{""scenes"":[{""name"":""s""}]}", GltfJsonContext.Default.Root);
            Assert.AreEqual(1, obj.Scenes.Count);
            Assert.AreEqual("s", obj.Scenes[0].Name);
        }

        [Test]
        public void RootSkins()
        {
            var obj = JsonSerializer.Deserialize(@"{""skins"":[{""name"":""k""}]}", GltfJsonContext.Default.Root);
            Assert.AreEqual(1, obj.Skins.Count);
            Assert.AreEqual("k", obj.Skins[0].Name);
        }

        [Test]
        public void RootTextures()
        {
            var obj = JsonSerializer.Deserialize(@"{""textures"":[{""source"":0}]}", GltfJsonContext.Default.Root);
            Assert.AreEqual(1, obj.Textures.Count);
            Assert.AreEqual(0, obj.Textures[0].Source);
        }

        [Test]
        public void RootExtensionsUsed()
        {
            var obj = JsonSerializer.Deserialize(@"{""extensionsUsed"":[""KHR_materials_unlit""]}", GltfJsonContext.Default.Root);
            Assert.AreEqual(1, obj.ExtensionsUsed.Count);
            Assert.AreEqual(Extension.MaterialsUnlit, obj.ExtensionsUsed[0].Value);
        }

        [Test]
        public void RootExtensionsRequired()
        {
            var obj = JsonSerializer.Deserialize(@"{""extensionsRequired"":[""KHR_materials_unlit""]}", GltfJsonContext.Default.Root);
            Assert.AreEqual(1, obj.ExtensionsRequired.Count);
            Assert.AreEqual(Extension.MaterialsUnlit, obj.ExtensionsRequired[0].Value);
        }

        [Test]
        public void RootExtensions()
        {
            var obj = JsonSerializer.Deserialize(
                @"{""extensions"":{""KHR_lights_punctual"":{""lights"":[]}}}",
                GltfJsonContext.Default.Root);
            Assert.IsNotNull(obj.Extensions);
            Assert.IsNotNull(obj.Extensions.LightsPunctual);
        }

        [Test]
        public void RootExtras()
        {
            var obj = JsonSerializer.Deserialize(@"{""extras"":{}}", GltfJsonContext.Default.Root);
            Assert.IsNotNull(obj.Extras);
        }

#if UNITY_ANIMATION || GLTFAST_ANIMATION
        [Test]
        public void RootAnimations()
        {
            var obj = JsonSerializer.Deserialize(@"{""animations"":[{""name"":""a""}]}", GltfJsonContext.Default.Root);
            Assert.AreEqual(1, obj.Animations.Count);
            Assert.AreEqual("a", obj.Animations[0].Name);
        }
#endif

        [Test]
        public void AssetDefault()
        {
            var obj = JsonSerializer.Deserialize("{}", GltfJsonContext.Default.Asset);
            Assert.IsNotNull(obj);
            Assert.IsNull(obj.Copyright);
            Assert.IsNull(obj.Generator);
            Assert.IsNull(obj.Version);
            Assert.IsNull(obj.MinVersion);
            Assert.IsNull(obj.Extensions);
            Assert.IsNull(obj.Extras);
        }

        [Test]
        public void AssetCopyright()
        {
            var obj = JsonSerializer.Deserialize(@"{""copyright"":""C""}", GltfJsonContext.Default.Asset);
            Assert.AreEqual("C", obj.Copyright);
        }

        [Test]
        public void AssetGenerator()
        {
            var obj = JsonSerializer.Deserialize(@"{""generator"":""g""}", GltfJsonContext.Default.Asset);
            Assert.AreEqual("g", obj.Generator);
        }

        [Test]
        public void AssetVersion()
        {
            var obj = JsonSerializer.Deserialize(@"{""version"":""2.0""}", GltfJsonContext.Default.Asset);
            Assert.AreEqual("2.0", obj.Version);
        }

        [Test]
        public void AssetMinVersion()
        {
            var obj = JsonSerializer.Deserialize(@"{""minVersion"":""2.0""}", GltfJsonContext.Default.Asset);
            Assert.AreEqual("2.0", obj.MinVersion);
        }

        [Test]
        public void AssetExtras()
        {
            var obj = JsonSerializer.Deserialize(@"{""extras"":{}}", GltfJsonContext.Default.Asset);
            Assert.IsNotNull(obj.Extras);
        }

        [Test]
        public void AssetExtensions()
        {
            var obj = JsonSerializer.Deserialize(@"{""extensions"":{}}", GltfJsonContext.Default.Asset);
            Assert.IsNotNull(obj.Extensions);
        }

        [Test]
        public void AccessorDefault()
        {
            var obj = JsonSerializer.Deserialize("{}", GltfJsonContext.Default.Accessor);
            Assert.IsNotNull(obj);
            Assert.IsNull(obj.Name);
            Assert.IsNull(obj.Sparse);
            Assert.IsNull(obj.BufferView);
            Assert.AreEqual(0, obj.ByteOffset);
            Assert.AreEqual((AccessorDataType)0, obj.ComponentType);
            Assert.IsFalse(obj.Normalized);
            Assert.AreEqual(SchemaConstants.UnsetIndex, obj.Count);
            Assert.AreEqual(AccessorType.Undefined, obj.Type.Value);
            Assert.IsNull(obj.Type.RawValue);
            Assert.IsNull(obj.Max);
            Assert.IsNull(obj.Min);
            Assert.IsNull(obj.Extras);
            Assert.IsNull(obj.Extensions);
        }

        [Test]
        public void AccessorName()
        {
            var obj = JsonSerializer.Deserialize(@"{""name"":""a""}", GltfJsonContext.Default.Accessor);
            Assert.AreEqual("a", obj.Name);
        }

        [Test]
        public void AccessorBufferView()
        {
            var obj = JsonSerializer.Deserialize(@"{""bufferView"":7}", GltfJsonContext.Default.Accessor);
            Assert.AreEqual(7, obj.BufferView);
        }

        [Test]
        public void AccessorByteOffset()
        {
            var obj = JsonSerializer.Deserialize(@"{""byteOffset"":8}", GltfJsonContext.Default.Accessor);
            Assert.AreEqual(8, obj.ByteOffset);
        }

        [Test]
        [TestCase(5120, AccessorDataType.Byte)]
        [TestCase(5121, AccessorDataType.UnsignedByte)]
        [TestCase(5122, AccessorDataType.Short)]
        [TestCase(5123, AccessorDataType.UnsignedShort)]
        [TestCase(5125, AccessorDataType.UnsignedInt)]
        [TestCase(5126, AccessorDataType.Float)]
        public void AccessorComponentType(int written, AccessorDataType expected)
        {
            var obj = JsonSerializer.Deserialize($@"{{""componentType"":{written}}}", GltfJsonContext.Default.Accessor);
            Assert.AreEqual(expected, obj.ComponentType);
        }

        [Test]
        public void AccessorNormalized()
        {
            var obj = JsonSerializer.Deserialize(@"{""normalized"":true}", GltfJsonContext.Default.Accessor);
            Assert.IsTrue(obj.Normalized);
        }

        [Test]
        public void AccessorCount()
        {
            var obj = JsonSerializer.Deserialize(@"{""count"":42}", GltfJsonContext.Default.Accessor);
            Assert.AreEqual(42, obj.Count);
        }

        [Test]
        public void AccessorTypeTest()
        {
            var obj = JsonSerializer.Deserialize(@"{""type"":""VEC3""}", GltfJsonContext.Default.Accessor);
            Assert.AreEqual(AccessorType.Vector3, obj.Type.Value);
            Assert.IsNull(obj.Type.RawValue);
        }

        [Test]
        public void AccessorTypeRaw()
        {
            var obj = JsonSerializer.Deserialize(@"{""type"":""CUSTOM""}", GltfJsonContext.Default.Accessor);
            Assert.AreEqual(AccessorType.Undefined, obj.Type.Value);
            Assert.AreEqual(Encoding.UTF8.GetBytes("CUSTOM"), obj.Type.RawValue);
        }

        [Test]
        public void AccessorMax()
        {
            var obj = JsonSerializer.Deserialize(@"{""max"":[1,2,3]}", GltfJsonContext.Default.Accessor);
            Assert.AreEqual(new[] { 1f, 2f, 3f }, obj.Max);
        }

        [Test]
        public void AccessorMin()
        {
            var obj = JsonSerializer.Deserialize(@"{""min"":[0,0,0]}", GltfJsonContext.Default.Accessor);
            Assert.AreEqual(new[] { 0f, 0f, 0f }, obj.Min);
        }

        [Test]
        public void AccessorSparse()
        {
            var obj = JsonSerializer.Deserialize(@"{""sparse"":{""count"":2}}", GltfJsonContext.Default.Accessor);
            Assert.IsNotNull(obj.Sparse);
            Assert.AreEqual(2, obj.Sparse.Count);
        }

        [Test]
        public void AccessorExtras()
        {
            var obj = JsonSerializer.Deserialize(@"{""extras"":{}}", GltfJsonContext.Default.Accessor);
            Assert.IsNotNull(obj.Extras);
        }

        [Test]
        public void AccessorExtensions()
        {
            var obj = JsonSerializer.Deserialize(@"{""extensions"":{}}", GltfJsonContext.Default.Accessor);
            Assert.IsNotNull(obj.Extensions);
        }

        [Test]
        public void AccessorSparseDefault()
        {
            var obj = JsonSerializer.Deserialize("{}", GltfJsonContext.Default.AccessorSparse);
            Assert.IsNotNull(obj);
            Assert.AreEqual(SchemaConstants.UnsetIndex, obj.Count);
            Assert.IsNull(obj.Indices);
            Assert.IsNull(obj.Values);
        }

        [Test]
        public void AccessorSparseCount()
        {
            var obj = JsonSerializer.Deserialize(@"{""count"":5}", GltfJsonContext.Default.AccessorSparse);
            Assert.AreEqual(5, obj.Count);
        }

        [Test]
        public void AccessorSparseIndicesProperty()
        {
            var obj = JsonSerializer.Deserialize(@"{""indices"":{""bufferView"":1}}", GltfJsonContext.Default.AccessorSparse);
            Assert.IsNotNull(obj.Indices);
            Assert.AreEqual(1u, obj.Indices.BufferView);
        }

        [Test]
        public void AccessorSparseValuesProperty()
        {
            var obj = JsonSerializer.Deserialize(@"{""values"":{""bufferView"":2}}", GltfJsonContext.Default.AccessorSparse);
            Assert.IsNotNull(obj.Values);
            Assert.AreEqual(2, obj.Values.BufferView);
        }

        [Test]
        public void AccessorSparseIndicesDefault()
        {
            var obj = JsonSerializer.Deserialize("{}", GltfJsonContext.Default.AccessorSparseIndices);
            Assert.IsNotNull(obj);
            Assert.AreEqual(SchemaConstants.UnsetIndex, obj.BufferView);
            Assert.AreEqual(0, obj.ByteOffset);
            Assert.AreEqual((AccessorDataType)0, obj.ComponentType);
        }

        [Test]
        public void AccessorSparseIndicesBufferView()
        {
            var obj = JsonSerializer.Deserialize(@"{""bufferView"":4}", GltfJsonContext.Default.AccessorSparseIndices);
            Assert.AreEqual(4, obj.BufferView);
        }

        [Test]
        public void AccessorSparseIndicesByteOffset()
        {
            var obj = JsonSerializer.Deserialize(@"{""byteOffset"":16}", GltfJsonContext.Default.AccessorSparseIndices);
            Assert.AreEqual(16, obj.ByteOffset);
        }

        [Test]
        public void AccessorSparseIndicesComponentType()
        {
            var obj = JsonSerializer.Deserialize(@"{""componentType"":5123}", GltfJsonContext.Default.AccessorSparseIndices);
            Assert.AreEqual(AccessorDataType.UnsignedShort, obj.ComponentType);
        }

        [Test]
        public void AccessorSparseValuesDefault()
        {
            var obj = JsonSerializer.Deserialize("{}", GltfJsonContext.Default.AccessorSparseValues);
            Assert.IsNotNull(obj);
            Assert.AreEqual(SchemaConstants.UnsetIndex, obj.BufferView);
            Assert.AreEqual(0, obj.ByteOffset);
        }

        [Test]
        public void AccessorSparseValuesBufferView()
        {
            var obj = JsonSerializer.Deserialize(@"{""bufferView"":4}", GltfJsonContext.Default.AccessorSparseValues);
            Assert.AreEqual(4, obj.BufferView);
        }

        [Test]
        public void AccessorSparseValuesByteOffset()
        {
            var obj = JsonSerializer.Deserialize(@"{""byteOffset"":8}", GltfJsonContext.Default.AccessorSparseValues);
            Assert.AreEqual(8, obj.ByteOffset);
        }

#if UNITY_ANIMATION || GLTFAST_ANIMATION
        [Test]
        public void AnimationDefault()
        {
            var obj = JsonSerializer.Deserialize("{}", GltfJsonContext.Default.Animation);
            Assert.IsNotNull(obj);
            Assert.IsNull(obj.Name);
            Assert.IsNull(obj.Channels);
            Assert.IsNull(obj.Samplers);
        }

        [Test]
        public void AnimationName()
        {
            var obj = JsonSerializer.Deserialize(@"{""name"":""a""}", GltfJsonContext.Default.Animation);
            Assert.AreEqual("a", obj.Name);
        }

        [Test]
        public void AnimationChannels()
        {
            var obj = JsonSerializer.Deserialize(@"{""channels"":[{""sampler"":1}]}", GltfJsonContext.Default.Animation);
            Assert.AreEqual(1, obj.Channels.Count);
            Assert.AreEqual(1, obj.Channels[0].Sampler);
        }

        [Test]
        public void AnimationSamplers()
        {
            var obj = JsonSerializer.Deserialize(@"{""samplers"":[{""input"":0,""output"":1}]}", GltfJsonContext.Default.Animation);
            Assert.AreEqual(1, obj.Samplers.Count);
            Assert.AreEqual(0, obj.Samplers[0].Input);
            Assert.AreEqual(1, obj.Samplers[0].Output);
        }

        [Test]
        public void AnimationChannelDefault()
        {
            var obj = JsonSerializer.Deserialize("{}", GltfJsonContext.Default.AnimationChannel);
            Assert.IsNotNull(obj);
            Assert.AreEqual(SchemaConstants.UnsetIndex, obj.Sampler);
            Assert.IsNull(obj.Target);
        }

        [Test]
        public void AnimationChannelSampler()
        {
            var obj = JsonSerializer.Deserialize(@"{""sampler"":3}", GltfJsonContext.Default.AnimationChannel);
            Assert.AreEqual(3, obj.Sampler);
        }

        [Test]
        public void AnimationChannelTarget()
        {
            var obj = JsonSerializer.Deserialize(@"{""target"":{""node"":1}}", GltfJsonContext.Default.AnimationChannel);
            Assert.IsNotNull(obj.Target);
            Assert.AreEqual(1, obj.Target.Node);
        }

        [Test]
        public void AnimationChannelTargetDefault()
        {
            var obj = JsonSerializer.Deserialize("{}", GltfJsonContext.Default.AnimationChannelTarget);
            Assert.IsNotNull(obj);
            Assert.IsNull(obj.Node);
            Assert.AreEqual(AnimationPath.Undefined, obj.Path.Value);
            Assert.IsNull(obj.Path.RawValue);
        }

        [Test]
        public void AnimationChannelTargetNode()
        {
            var obj = JsonSerializer.Deserialize(@"{""node"":7}", GltfJsonContext.Default.AnimationChannelTarget);
            Assert.AreEqual(7, obj.Node);
        }

        [Test]
        [TestCase("translation", AnimationPath.Translation)]
        [TestCase("rotation", AnimationPath.Rotation)]
        [TestCase("scale", AnimationPath.Scale)]
        [TestCase("weights", AnimationPath.Weights)]
        [TestCase("pointer", AnimationPath.Pointer)]
        public void AnimationChannelTargetPath(string written, AnimationPath expected)
        {
            var obj = JsonSerializer.Deserialize($@"{{""path"":""{written}""}}", GltfJsonContext.Default.AnimationChannelTarget);
            Assert.AreEqual(expected, obj.Path.Value);
        }

        [Test]
        public void AnimationSamplerDefault()
        {
            var obj = JsonSerializer.Deserialize("{}", GltfJsonContext.Default.AnimationSampler);
            Assert.IsNotNull(obj);
            Assert.AreEqual(SchemaConstants.UnsetIndex, obj.Input);
            Assert.AreEqual(SchemaConstants.UnsetIndex, obj.Output);
            Assert.AreEqual(Interpolation.Linear, obj.Interpolation.Value);
        }

        [Test]
        public void AnimationSamplerInput()
        {
            var obj = JsonSerializer.Deserialize(@"{""input"":2}", GltfJsonContext.Default.AnimationSampler);
            Assert.AreEqual(2, obj.Input);
        }

        [Test]
        public void AnimationSamplerOutput()
        {
            var obj = JsonSerializer.Deserialize(@"{""output"":4}", GltfJsonContext.Default.AnimationSampler);
            Assert.AreEqual(4, obj.Output);
        }

        [Test]
        [TestCase("STEP", Interpolation.Step)]
        [TestCase("CUBICSPLINE", Interpolation.CubicSpline)]
        [TestCase("LINEAR", Interpolation.Linear)]
        public void AnimationSamplerInterpolation(string written, Interpolation expected)
        {
            var obj = JsonSerializer.Deserialize($@"{{""interpolation"":""{written}""}}", GltfJsonContext.Default.AnimationSampler);
            Assert.AreEqual(expected, obj.Interpolation.Value);
        }
#endif

        [Test]
        public void BufferDefault()
        {
            var obj = JsonSerializer.Deserialize("{}", GltfJsonContext.Default.Buffer);
            Assert.IsNotNull(obj);
            Assert.IsNull(obj.Name);
            Assert.AreEqual(SchemaConstants.UnsetByteLength, obj.ByteLength);
            Assert.IsNull(obj.Uri);
        }

        [Test]
        public void BufferName()
        {
            var obj = JsonSerializer.Deserialize(@"{""name"":""b""}", GltfJsonContext.Default.Buffer);
            Assert.AreEqual("b", obj.Name);
        }

        [Test]
        public void BufferByteLength()
        {
            var obj = JsonSerializer.Deserialize(@"{""byteLength"":256}", GltfJsonContext.Default.Buffer);
            Assert.AreEqual(256L, obj.ByteLength);
        }

        [Test]
        public void BufferUri()
        {
            var obj = JsonSerializer.Deserialize(@"{""uri"":""data.bin""}", GltfJsonContext.Default.Buffer);
            Assert.IsNotNull(obj.Uri);
            Assert.IsTrue(obj.Uri.IsString);
            Assert.AreEqual("data.bin", obj.Uri.AsString());
        }

        [Test]
        public void BufferViewExtensionsDefault()
        {
            var obj = JsonSerializer.Deserialize("{}", GltfJsonContext.Default.BufferViewExtensions);
            Assert.IsNotNull(obj);
        }

#if MESHOPT_IS_RECENT
        [Test]
        public void BufferViewExtensionsMeshopt()
        {
            var obj = JsonSerializer.Deserialize(
                @"{""EXT_meshopt_compression"":{""count"":1}}",
                GltfJsonContext.Default.BufferViewExtensions);
            Assert.IsNotNull(obj.ExtMeshoptCompression);
            Assert.AreEqual(1, obj.ExtMeshoptCompression.Count);
        }

        [Test]
        public void BufferViewMeshoptExtensionDefault()
        {
            var obj = JsonSerializer.Deserialize("{}", GltfJsonContext.Default.BufferViewMeshoptExtension);
            Assert.IsNotNull(obj);
            Assert.AreEqual(SchemaConstants.UnsetIndex, obj.Buffer);
            Assert.AreEqual(0, obj.ByteOffset);
            Assert.AreEqual(0, obj.ByteLength);
            Assert.IsNull(obj.ByteStride);
            Assert.AreEqual(0, obj.Count);
            Assert.AreEqual(MeshoptMode.Undefined, obj.Mode);
            Assert.AreEqual(MeshoptFilter.None, obj.Filter);
        }

        [Test]
        public void BufferViewMeshoptExtensionBuffer()
        {
            var obj = JsonSerializer.Deserialize(@"{""buffer"":1}", GltfJsonContext.Default.BufferViewMeshoptExtension);
            Assert.AreEqual(1, obj.Buffer);
        }

        [Test]
        public void BufferViewMeshoptExtensionByteOffset()
        {
            var obj = JsonSerializer.Deserialize(@"{""byteOffset"":8}", GltfJsonContext.Default.BufferViewMeshoptExtension);
            Assert.AreEqual(8, obj.ByteOffset);
        }

        [Test]
        public void BufferViewMeshoptExtensionByteLength()
        {
            var obj = JsonSerializer.Deserialize(@"{""byteLength"":16}", GltfJsonContext.Default.BufferViewMeshoptExtension);
            Assert.AreEqual(16, obj.ByteLength);
        }

        [Test]
        public void BufferViewMeshoptExtensionByteStride()
        {
            var obj = JsonSerializer.Deserialize(@"{""byteStride"":4}", GltfJsonContext.Default.BufferViewMeshoptExtension);
            Assert.AreEqual(4, obj.ByteStride);
        }

        [Test]
        public void BufferViewMeshoptExtensionCount()
        {
            var obj = JsonSerializer.Deserialize(@"{""count"":12}", GltfJsonContext.Default.BufferViewMeshoptExtension);
            Assert.AreEqual(12, obj.Count);
        }

        [Test]
        [TestCase("ATTRIBUTES", MeshoptMode.Attributes)]
        [TestCase("TRIANGLES", MeshoptMode.Triangles)]
        [TestCase("INDICES", MeshoptMode.Indices)]
        public void BufferViewMeshoptExtensionMode(string written, MeshoptMode expected)
        {
            var obj = JsonSerializer.Deserialize($@"{{""mode"":""{written}""}}", GltfJsonContext.Default.BufferViewMeshoptExtension);
            Assert.AreEqual(expected, obj.Mode);
        }

        [Test]
        [TestCase("NONE", MeshoptFilter.None)]
        [TestCase("OCTAHEDRAL", MeshoptFilter.Octahedral)]
        [TestCase("QUATERNION", MeshoptFilter.Quaternion)]
        [TestCase("EXPONENTIAL", MeshoptFilter.Exponential)]
        public void BufferViewMeshoptExtensionFilter(string written, MeshoptFilter expected)
        {
            var obj = JsonSerializer.Deserialize($@"{{""filter"":""{written}""}}", GltfJsonContext.Default.BufferViewMeshoptExtension);
            Assert.AreEqual(expected, obj.Filter);
        }
#endif

        [Test]
        public void CameraDefault()
        {
            var obj = JsonSerializer.Deserialize("{}", GltfJsonContext.Default.Camera);
            Assert.IsNotNull(obj);
            Assert.IsNull(obj.Name);
            Assert.IsNull(obj.Orthographic);
            Assert.IsNull(obj.Perspective);
            Assert.AreEqual(CameraType.Undefined, obj.Type.Value);
        }

        [Test]
        public void CameraName()
        {
            var obj = JsonSerializer.Deserialize(@"{""name"":""c""}", GltfJsonContext.Default.Camera);
            Assert.AreEqual("c", obj.Name);
        }

        [Test]
        public void CameraOrthographic()
        {
            var obj = JsonSerializer.Deserialize(@"{""orthographic"":{""xmag"":1}}", GltfJsonContext.Default.Camera);
            Assert.IsNotNull(obj.Orthographic);
            Assert.AreEqual(1f, obj.Orthographic.Xmag);
        }

        [Test]
        public void CameraPerspective()
        {
            var obj = JsonSerializer.Deserialize(@"{""perspective"":{""yfov"":1.5}}", GltfJsonContext.Default.Camera);
            Assert.IsNotNull(obj.Perspective);
            Assert.AreEqual(1.5f, obj.Perspective.Yfov);
        }

        [Test]
        [TestCase("orthographic", CameraType.Orthographic)]
        [TestCase("perspective", CameraType.Perspective)]
        public void CameraTypeProperty(string written, CameraType expected)
        {
            var obj = JsonSerializer.Deserialize($@"{{""type"":""{written}""}}", GltfJsonContext.Default.Camera);
            Assert.AreEqual(expected, obj.Type.Value);
        }

        [Test]
        public void CameraOrthographicDefault()
        {
            var obj = JsonSerializer.Deserialize("{}", GltfJsonContext.Default.CameraOrthographic);
            Assert.IsNotNull(obj);
            Assert.AreEqual(0f, obj.Xmag);
            Assert.AreEqual(0f, obj.Ymag);
            Assert.AreEqual(0f, obj.Zfar);
            Assert.AreEqual(0f, obj.Znear);
        }

        [Test]
        public void CameraOrthographicXmag()
        {
            var obj = JsonSerializer.Deserialize(@"{""xmag"":2}", GltfJsonContext.Default.CameraOrthographic);
            Assert.AreEqual(2f, obj.Xmag);
        }

        [Test]
        public void CameraOrthographicYmag()
        {
            var obj = JsonSerializer.Deserialize(@"{""ymag"":3}", GltfJsonContext.Default.CameraOrthographic);
            Assert.AreEqual(3f, obj.Ymag);
        }

        [Test]
        public void CameraOrthographicZfar()
        {
            var obj = JsonSerializer.Deserialize(@"{""zfar"":100}", GltfJsonContext.Default.CameraOrthographic);
            Assert.AreEqual(100f, obj.Zfar);
        }

        [Test]
        public void CameraOrthographicZnear()
        {
            var obj = JsonSerializer.Deserialize(@"{""znear"":0.5}", GltfJsonContext.Default.CameraOrthographic);
            Assert.AreEqual(0.5f, obj.Znear);
        }

        [Test]
        public void CameraPerspectiveDefault()
        {
            var obj = JsonSerializer.Deserialize("{}", GltfJsonContext.Default.CameraPerspective);
            Assert.IsNotNull(obj);
            Assert.IsNull(obj.AspectRatio);
            Assert.AreEqual(0f, obj.Yfov);
            Assert.IsNull(obj.Zfar);
            Assert.AreEqual(0f, obj.Znear);
        }

        [Test]
        public void CameraPerspectiveAspectRatio()
        {
            var obj = JsonSerializer.Deserialize(@"{""aspectRatio"":1.5}", GltfJsonContext.Default.CameraPerspective);
            Assert.AreEqual(1.5f, obj.AspectRatio);
        }

        [Test]
        public void CameraPerspectiveYfov()
        {
            var obj = JsonSerializer.Deserialize(@"{""yfov"":1.25}", GltfJsonContext.Default.CameraPerspective);
            Assert.AreEqual(1.25f, obj.Yfov);
        }

        [Test]
        public void CameraPerspectiveZfar()
        {
            var obj = JsonSerializer.Deserialize(@"{""zfar"":100}", GltfJsonContext.Default.CameraPerspective);
            Assert.AreEqual(100f, obj.Zfar);
        }

        [Test]
        public void CameraPerspectiveZnear()
        {
            var obj = JsonSerializer.Deserialize(@"{""znear"":0.25}", GltfJsonContext.Default.CameraPerspective);
            Assert.AreEqual(0.25f, obj.Znear);
        }

        [Test]
        public void ImageDefault()
        {
            var obj = JsonSerializer.Deserialize("{}", GltfJsonContext.Default.Image);
            Assert.IsNotNull(obj);
            Assert.IsNull(obj.Name);
            Assert.IsNull(obj.Uri);
            Assert.AreEqual(ImageMimeType.Undefined, obj.MimeType.Value);
            Assert.IsNull(obj.BufferView);
        }

        [Test]
        public void ImageName()
        {
            var obj = JsonSerializer.Deserialize(@"{""name"":""i""}", GltfJsonContext.Default.Image);
            Assert.AreEqual("i", obj.Name);
        }

        [Test]
        public void ImageUri()
        {
            var obj = JsonSerializer.Deserialize(@"{""uri"":""texture.png""}", GltfJsonContext.Default.Image);
            Assert.IsNotNull(obj.Uri);
            Assert.AreEqual("texture.png", obj.Uri.AsString());
        }

        [Test]
        [TestCase("image/jpeg", ImageMimeType.Jpeg)]
        [TestCase("image/png", ImageMimeType.Png)]
        [TestCase("image/ktx2", ImageMimeType.Ktx2)]
        [TestCase("image/webp", ImageMimeType.WebP)]
        public void ImageMimeTypeTest(string written, ImageMimeType expected)
        {
            var obj = JsonSerializer.Deserialize($@"{{""mimeType"":""{written}""}}", GltfJsonContext.Default.Image);
            Assert.AreEqual(expected, obj.MimeType.Value);
        }

        [Test]
        public void ImageBufferView()
        {
            var obj = JsonSerializer.Deserialize(@"{""bufferView"":5}", GltfJsonContext.Default.Image);
            Assert.AreEqual(5, obj.BufferView);
        }

        [Test]
        public void MaterialDefault()
        {
            var obj = JsonSerializer.Deserialize("{}", GltfJsonContext.Default.Material);
            Assert.IsNotNull(obj);
            Assert.IsNull(obj.Name);
            Assert.IsNull(obj.Extensions);
            Assert.IsNull(obj.PbrMetallicRoughness);
            Assert.IsNull(obj.NormalTexture);
            Assert.IsNull(obj.OcclusionTexture);
            Assert.IsNull(obj.EmissiveTexture);
            Assert.AreEqual(Color.Black, obj.EmissiveFactor);
            Assert.AreEqual(AlphaMode.Opaque, obj.AlphaMode.Value);
            Assert.AreEqual(0.5f, obj.AlphaCutoff);
            Assert.IsFalse(obj.DoubleSided);
        }

        [Test]
        public void MaterialName()
        {
            var obj = JsonSerializer.Deserialize(@"{""name"":""m""}", GltfJsonContext.Default.Material);
            Assert.AreEqual("m", obj.Name);
        }

        [Test]
        public void MaterialPbrMetallicRoughness()
        {
            var obj = JsonSerializer.Deserialize(@"{""pbrMetallicRoughness"":{}}", GltfJsonContext.Default.Material);
            Assert.IsNotNull(obj.PbrMetallicRoughness);
        }

        [Test]
        public void MaterialNormalTexture()
        {
            var obj = JsonSerializer.Deserialize(@"{""normalTexture"":{""index"":0}}", GltfJsonContext.Default.Material);
            Assert.IsNotNull(obj.NormalTexture);
            Assert.AreEqual(0, obj.NormalTexture.Index);
        }

        [Test]
        public void MaterialOcclusionTexture()
        {
            var obj = JsonSerializer.Deserialize(@"{""occlusionTexture"":{""index"":0}}", GltfJsonContext.Default.Material);
            Assert.IsNotNull(obj.OcclusionTexture);
            Assert.AreEqual(0, obj.OcclusionTexture.Index);
        }

        [Test]
        public void MaterialEmissiveTexture()
        {
            var obj = JsonSerializer.Deserialize(@"{""emissiveTexture"":{""index"":0}}", GltfJsonContext.Default.Material);
            Assert.IsNotNull(obj.EmissiveTexture);
            Assert.AreEqual(0, obj.EmissiveTexture.Index);
        }

        [Test]
        public void MaterialEmissiveFactor()
        {
            var obj = JsonSerializer.Deserialize(@"{""emissiveFactor"":[0.5,0.25,0.75]}", GltfJsonContext.Default.Material);
            Assert.AreEqual(new Color(0.5f, 0.25f, 0.75f), obj.EmissiveFactor);
        }

        [Test]
        [TestCase("OPAQUE", AlphaMode.Opaque)]
        [TestCase("MASK", AlphaMode.Mask)]
        [TestCase("BLEND", AlphaMode.Blend)]
        public void MaterialAlphaMode(string written, AlphaMode expected)
        {
            var obj = JsonSerializer.Deserialize($@"{{""alphaMode"":""{written}""}}", GltfJsonContext.Default.Material);
            Assert.AreEqual(expected, obj.AlphaMode.Value);
        }

        [Test]
        public void MaterialAlphaCutoffDefault()
        {
            var obj = JsonSerializer.Deserialize("{}", GltfJsonContext.Default.Material);
            Assert.AreEqual(0.5f, obj.AlphaCutoff);
        }

        [Test]
        public void MaterialAlphaCutoffCustom()
        {
            var obj = JsonSerializer.Deserialize(@"{""alphaCutoff"":0.25}", GltfJsonContext.Default.Material);
            Assert.AreEqual(0.25f, obj.AlphaCutoff);
        }

        [Test]
        public void MaterialDoubleSided()
        {
            var obj = JsonSerializer.Deserialize(@"{""doubleSided"":true}", GltfJsonContext.Default.Material);
            Assert.IsTrue(obj.DoubleSided);
        }

        [Test]
        public void MaterialExtensions()
        {
            var obj = JsonSerializer.Deserialize(@"{""extensions"":{""KHR_materials_unlit"":{}}}", GltfJsonContext.Default.Material);
            Assert.IsNotNull(obj.Extensions);
            Assert.IsNotNull(obj.Extensions.Unlit);
        }

        [Test]
        public void MaterialExtensionsDefault()
        {
            var obj = JsonSerializer.Deserialize("{}", GltfJsonContext.Default.MaterialExtensions);
            Assert.IsNotNull(obj);
            Assert.IsNull(obj.PbrSpecularGlossiness);
            Assert.IsNull(obj.Unlit);
            Assert.IsNull(obj.Transmission);
            Assert.IsNull(obj.Clearcoat);
            Assert.IsNull(obj.Sheen);
            Assert.IsNull(obj.Specular);
            Assert.IsNull(obj.IndexOfRefraction);
        }

        [Test]
        public void MaterialExtensionsPbrSpecularGlossiness()
        {
            var obj = JsonSerializer.Deserialize(
                @"{""KHR_materials_pbrSpecularGlossiness"":{}}",
                GltfJsonContext.Default.MaterialExtensions);
            Assert.IsNotNull(obj.PbrSpecularGlossiness);
        }

        [Test]
        public void MaterialExtensionsUnlit()
        {
            var obj = JsonSerializer.Deserialize(@"{""KHR_materials_unlit"":{}}", GltfJsonContext.Default.MaterialExtensions);
            Assert.IsNotNull(obj.Unlit);
        }

        [Test]
        public void MaterialExtensionsTransmission()
        {
            var obj = JsonSerializer.Deserialize(@"{""KHR_materials_transmission"":{}}", GltfJsonContext.Default.MaterialExtensions);
            Assert.IsNotNull(obj.Transmission);
        }

        [Test]
        public void MaterialExtensionsClearcoat()
        {
            var obj = JsonSerializer.Deserialize(@"{""KHR_materials_clearcoat"":{}}", GltfJsonContext.Default.MaterialExtensions);
            Assert.IsNotNull(obj.Clearcoat);
        }

        [Test]
        public void MaterialExtensionsSheen()
        {
            var obj = JsonSerializer.Deserialize(@"{""KHR_materials_sheen"":{}}", GltfJsonContext.Default.MaterialExtensions);
            Assert.IsNotNull(obj.Sheen);
        }

        [Test]
        public void MaterialExtensionsSpecular()
        {
            var obj = JsonSerializer.Deserialize(@"{""KHR_materials_specular"":{}}", GltfJsonContext.Default.MaterialExtensions);
            Assert.IsNotNull(obj.Specular);
        }

        [Test]
        public void MaterialExtensionsIor()
        {
            var obj = JsonSerializer.Deserialize(@"{""KHR_materials_ior"":{}}", GltfJsonContext.Default.MaterialExtensions);
            Assert.IsNotNull(obj.IndexOfRefraction);
        }

        [Test]
        public void PbrMetallicRoughnessDefault()
        {
            var obj = JsonSerializer.Deserialize("{}", GltfJsonContext.Default.PbrMetallicRoughness);
            Assert.IsNotNull(obj);
            Assert.IsNull(obj.BaseColorTexture);
            Assert.IsNull(obj.MetallicRoughnessTexture);
            Assert.AreEqual(ColorAlpha.White, obj.BaseColorFactor);
            Assert.AreEqual(1f, obj.MetallicFactor);
            Assert.AreEqual(1f, obj.RoughnessFactor);
        }

        [Test]
        public void PbrMetallicRoughnessBaseColorTexture()
        {
            var obj = JsonSerializer.Deserialize(@"{""baseColorTexture"":{""index"":0}}", GltfJsonContext.Default.PbrMetallicRoughness);
            Assert.IsNotNull(obj.BaseColorTexture);
            Assert.AreEqual(0, obj.BaseColorTexture.Index);
        }

        [Test]
        public void PbrMetallicRoughnessMetallicRoughnessTexture()
        {
            var obj = JsonSerializer.Deserialize(@"{""metallicRoughnessTexture"":{""index"":1}}", GltfJsonContext.Default.PbrMetallicRoughness);
            Assert.IsNotNull(obj.MetallicRoughnessTexture);
            Assert.AreEqual(1, obj.MetallicRoughnessTexture.Index);
        }

        [Test]
        public void PbrMetallicRoughnessBaseColorFactor()
        {
            var obj = JsonSerializer.Deserialize(@"{""baseColorFactor"":[0.5,0.25,0.75,0.5]}", GltfJsonContext.Default.PbrMetallicRoughness);
            Assert.AreEqual(new ColorAlpha(0.5f, 0.25f, 0.75f, 0.5f), obj.BaseColorFactor);
        }

        [Test]
        public void PbrMetallicRoughnessMetallicFactor()
        {
            var obj = JsonSerializer.Deserialize(@"{""metallicFactor"":0.25}", GltfJsonContext.Default.PbrMetallicRoughness);
            Assert.AreEqual(0.25f, obj.MetallicFactor);
        }

        [Test]
        public void PbrMetallicRoughnessRoughnessFactor()
        {
            var obj = JsonSerializer.Deserialize(@"{""roughnessFactor"":0.5}", GltfJsonContext.Default.PbrMetallicRoughness);
            Assert.AreEqual(0.5f, obj.RoughnessFactor);
        }

        [Test]
        public void PbrSpecularGlossinessDefault()
        {
            var obj = JsonSerializer.Deserialize("{}", GltfJsonContext.Default.PbrSpecularGlossiness);
            Assert.IsNotNull(obj);
            Assert.AreEqual(ColorAlpha.White, obj.DiffuseFactor);
            Assert.IsNull(obj.DiffuseTexture);
            Assert.AreEqual(Color.White, obj.SpecularFactor);
            Assert.AreEqual(1f, obj.GlossinessFactor);
            Assert.IsNull(obj.SpecularGlossinessTexture);
        }

        [Test]
        public void PbrSpecularGlossinessDiffuseFactor()
        {
            var obj = JsonSerializer.Deserialize(@"{""diffuseFactor"":[0.5,0.5,0.5,0.5]}", GltfJsonContext.Default.PbrSpecularGlossiness);
            Assert.AreEqual(new ColorAlpha(0.5f, 0.5f, 0.5f, 0.5f), obj.DiffuseFactor);
        }

        [Test]
        public void PbrSpecularGlossinessDiffuseTexture()
        {
            var obj = JsonSerializer.Deserialize(@"{""diffuseTexture"":{""index"":0}}", GltfJsonContext.Default.PbrSpecularGlossiness);
            Assert.IsNotNull(obj.DiffuseTexture);
            Assert.AreEqual(0, obj.DiffuseTexture.Index);
        }

        [Test]
        public void PbrSpecularGlossinessSpecularFactor()
        {
            var obj = JsonSerializer.Deserialize(@"{""specularFactor"":[0.5,0.5,0.5]}", GltfJsonContext.Default.PbrSpecularGlossiness);
            Assert.AreEqual(new Color(0.5f, 0.5f, 0.5f), obj.SpecularFactor);
        }

        [Test]
        public void PbrSpecularGlossinessGlossinessFactor()
        {
            var obj = JsonSerializer.Deserialize(@"{""glossinessFactor"":0.5}", GltfJsonContext.Default.PbrSpecularGlossiness);
            Assert.AreEqual(0.5f, obj.GlossinessFactor);
        }

        [Test]
        public void PbrSpecularGlossinessSpecularGlossinessTexture()
        {
            var obj = JsonSerializer.Deserialize(@"{""specularGlossinessTexture"":{""index"":0}}", GltfJsonContext.Default.PbrSpecularGlossiness);
            Assert.IsNotNull(obj.SpecularGlossinessTexture);
            Assert.AreEqual(0, obj.SpecularGlossinessTexture.Index);
        }

        [Test]
        public void MaterialUnlitDefault()
        {
            var obj = JsonSerializer.Deserialize("{}", GltfJsonContext.Default.MaterialUnlit);
            Assert.IsNotNull(obj);
        }

        [Test]
        public void TransmissionDefault()
        {
            var obj = JsonSerializer.Deserialize("{}", GltfJsonContext.Default.Transmission);
            Assert.IsNotNull(obj);
            Assert.AreEqual(0f, obj.TransmissionFactor);
            Assert.IsNull(obj.TransmissionTexture);
        }

        [Test]
        public void TransmissionFactor()
        {
            var obj = JsonSerializer.Deserialize(@"{""transmissionFactor"":0.5}", GltfJsonContext.Default.Transmission);
            Assert.AreEqual(0.5f, obj.TransmissionFactor);
        }

        [Test]
        public void TransmissionTexture()
        {
            var obj = JsonSerializer.Deserialize(@"{""transmissionTexture"":{""index"":0}}", GltfJsonContext.Default.Transmission);
            Assert.IsNotNull(obj.TransmissionTexture);
            Assert.AreEqual(0, obj.TransmissionTexture.Index);
        }

        [Test]
        public void ClearCoatDefault()
        {
            var obj = JsonSerializer.Deserialize("{}", GltfJsonContext.Default.ClearCoat);
            Assert.IsNotNull(obj);
            Assert.AreEqual(0f, obj.ClearcoatFactor);
            Assert.IsNull(obj.ClearcoatTexture);
            Assert.AreEqual(0f, obj.ClearcoatRoughnessFactor);
            Assert.IsNull(obj.ClearcoatRoughnessTexture);
            Assert.IsNull(obj.ClearcoatNormalTexture);
        }

        [Test]
        public void ClearCoatFactor()
        {
            var obj = JsonSerializer.Deserialize(@"{""clearcoatFactor"":0.5}", GltfJsonContext.Default.ClearCoat);
            Assert.AreEqual(0.5f, obj.ClearcoatFactor);
        }

        [Test]
        public void ClearCoatTexture()
        {
            var obj = JsonSerializer.Deserialize(@"{""clearcoatTexture"":{""index"":0}}", GltfJsonContext.Default.ClearCoat);
            Assert.IsNotNull(obj.ClearcoatTexture);
            Assert.AreEqual(0, obj.ClearcoatTexture.Index);
        }

        [Test]
        public void ClearCoatRoughnessFactor()
        {
            var obj = JsonSerializer.Deserialize(@"{""clearcoatRoughnessFactor"":0.25}", GltfJsonContext.Default.ClearCoat);
            Assert.AreEqual(0.25f, obj.ClearcoatRoughnessFactor);
        }

        [Test]
        public void ClearCoatRoughnessTexture()
        {
            var obj = JsonSerializer.Deserialize(@"{""clearcoatRoughnessTexture"":{""index"":1}}", GltfJsonContext.Default.ClearCoat);
            Assert.IsNotNull(obj.ClearcoatRoughnessTexture);
            Assert.AreEqual(1, obj.ClearcoatRoughnessTexture.Index);
        }

        [Test]
        public void ClearCoatNormalTexture()
        {
            var obj = JsonSerializer.Deserialize(@"{""clearcoatNormalTexture"":{""index"":2}}", GltfJsonContext.Default.ClearCoat);
            Assert.IsNotNull(obj.ClearcoatNormalTexture);
            Assert.AreEqual(2, obj.ClearcoatNormalTexture.Index);
        }

        [Test]
        public void SheenDefault()
        {
            var obj = JsonSerializer.Deserialize("{}", GltfJsonContext.Default.Sheen);
            Assert.IsNotNull(obj);
            Assert.AreEqual(Color.Black, obj.SheenColorFactor);
            Assert.IsNull(obj.SheenColorTexture);
            Assert.AreEqual(0f, obj.SheenRoughnessFactor);
            Assert.IsNull(obj.SheenRoughnessTexture);
        }

        [Test]
        public void SheenColorFactor()
        {
            var obj = JsonSerializer.Deserialize(@"{""sheenColorFactor"":[0.5,0.25,0.75]}", GltfJsonContext.Default.Sheen);
            Assert.AreEqual(new Color(0.5f, 0.25f, 0.75f), obj.SheenColorFactor);
        }

        [Test]
        public void SheenColorTexture()
        {
            var obj = JsonSerializer.Deserialize(@"{""sheenColorTexture"":{""index"":0}}", GltfJsonContext.Default.Sheen);
            Assert.IsNotNull(obj.SheenColorTexture);
            Assert.AreEqual(0, obj.SheenColorTexture.Index);
        }

        [Test]
        public void SheenRoughnessFactor()
        {
            var obj = JsonSerializer.Deserialize(@"{""sheenRoughnessFactor"":0.5}", GltfJsonContext.Default.Sheen);
            Assert.AreEqual(0.5f, obj.SheenRoughnessFactor);
        }

        [Test]
        public void SheenRoughnessTexture()
        {
            var obj = JsonSerializer.Deserialize(@"{""sheenRoughnessTexture"":{""index"":0}}", GltfJsonContext.Default.Sheen);
            Assert.IsNotNull(obj.SheenRoughnessTexture);
            Assert.AreEqual(0, obj.SheenRoughnessTexture.Index);
        }

        [Test]
        public void MaterialSpecularDefault()
        {
            var obj = JsonSerializer.Deserialize("{}", GltfJsonContext.Default.MaterialSpecular);
            Assert.IsNotNull(obj);
            Assert.AreEqual(1f, obj.SpecularFactor);
            Assert.IsNull(obj.SpecularTexture);
            Assert.AreEqual(Color.White, obj.SpecularColorFactor);
            Assert.IsNull(obj.SpecularColorTexture);
        }

        [Test]
        public void MaterialSpecularFactor()
        {
            var obj = JsonSerializer.Deserialize(@"{""specularFactor"":0.5}", GltfJsonContext.Default.MaterialSpecular);
            Assert.AreEqual(0.5f, obj.SpecularFactor);
        }

        [Test]
        public void MaterialSpecularTexture()
        {
            var obj = JsonSerializer.Deserialize(@"{""specularTexture"":{""index"":0}}", GltfJsonContext.Default.MaterialSpecular);
            Assert.IsNotNull(obj.SpecularTexture);
            Assert.AreEqual(0, obj.SpecularTexture.Index);
        }

        [Test]
        public void MaterialSpecularColorFactor()
        {
            var obj = JsonSerializer.Deserialize(@"{""specularColorFactor"":[0.5,0.25,0.75]}", GltfJsonContext.Default.MaterialSpecular);
            Assert.AreEqual(new Color(0.5f, 0.25f, 0.75f), obj.SpecularColorFactor);
        }

        [Test]
        public void MaterialSpecularColorTexture()
        {
            var obj = JsonSerializer.Deserialize(@"{""specularColorTexture"":{""index"":0}}", GltfJsonContext.Default.MaterialSpecular);
            Assert.IsNotNull(obj.SpecularColorTexture);
            Assert.AreEqual(0, obj.SpecularColorTexture.Index);
        }

        [Test]
        public void MaterialIorDefault()
        {
            var obj = JsonSerializer.Deserialize("{}", GltfJsonContext.Default.MaterialIor);
            Assert.IsNotNull(obj);
            Assert.AreEqual(1.5f, obj.Ior);
        }

        [Test]
        public void MaterialIorCustom()
        {
            var obj = JsonSerializer.Deserialize(@"{""ior"":1.4}", GltfJsonContext.Default.MaterialIor);
            Assert.AreEqual(1.4f, obj.Ior);
        }

        [Test]
        public void MeshDefault()
        {
            var obj = JsonSerializer.Deserialize("{}", GltfJsonContext.Default.Mesh);
            Assert.IsNotNull(obj);
            Assert.IsNull(obj.Name);
            Assert.IsNull(obj.Primitives);
            Assert.IsNull(obj.Extras);
            Assert.IsNull(obj.Weights);
            Assert.IsNull(obj.Extensions);
        }

        [Test]
        public void MeshName()
        {
            var obj = JsonSerializer.Deserialize(@"{""name"":""m""}", GltfJsonContext.Default.Mesh);
            Assert.AreEqual("m", obj.Name);
        }

        [Test]
        public void MeshPrimitives()
        {
            var obj = JsonSerializer.Deserialize(@"{""primitives"":[{""indices"":0}]}", GltfJsonContext.Default.Mesh);
            Assert.AreEqual(1, obj.Primitives.Count);
            Assert.AreEqual(0, obj.Primitives[0].Indices);
        }

        [Test]
        public void MeshExtras()
        {
            var obj = JsonSerializer.Deserialize(@"{""extras"":{""targetNames"":[""k1""]}}", GltfJsonContext.Default.Mesh);
            Assert.IsNotNull(obj.Extras);
            Assert.AreEqual(1, obj.Extras.TargetNames.Count);
            Assert.AreEqual("k1", obj.Extras.TargetNames[0]);
        }

        [Test]
        public void MeshWeights()
        {
            var obj = JsonSerializer.Deserialize(@"{""weights"":[0,1]}", GltfJsonContext.Default.Mesh);
            Assert.AreEqual(new[] { 0f, 1f }, obj.Weights);
        }

        [Test]
        public void MeshExtrasDefault()
        {
            var obj = JsonSerializer.Deserialize("{}", GltfJsonContext.Default.MeshExtras);
            Assert.IsNotNull(obj);
            Assert.IsNull(obj.TargetNames);
        }

        [Test]
        public void MeshExtrasTargetNames()
        {
            var obj = JsonSerializer.Deserialize(@"{""targetNames"":[""a"",""b""]}", GltfJsonContext.Default.MeshExtras);
            Assert.AreEqual(new[] { "a", "b" }, obj.TargetNames);
        }

        [Test]
        public void AttributesDefault()
        {
            var obj = JsonSerializer.Deserialize("{}", GltfJsonContext.Default.Attributes);
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
                GltfJsonContext.Default.Attributes);
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
                GltfJsonContext.Default.Attributes);
            Assert.AreEqual(9, obj.TexCoords.Count);
            for (var i = 0; i < 9; i++)
            {
                Assert.AreEqual(10 + i, obj.GetTexCoord(i));
            }

#pragma warning disable CS0618 // Type or member is obsolete
            Assert.IsTrue(obj.TryGetAllUVAccessors(out var oldAccessors, out var limitExceeded));
#pragma warning restore CS0618 // Type or member is obsolete
            Assert.IsTrue(limitExceeded);
            Assert.NotNull(oldAccessors);
            Assert.AreEqual(8, oldAccessors.Length);
            for (var i = 0; i < 8; i++)
            {
                Assert.AreEqual(10 + i, oldAccessors[i]);
            }
        }

        [Test]
        public void AttributesTexCoordsSparse()
        {
            var obj = JsonSerializer.Deserialize(
                @"{""TEXCOORD_2"":7}",
                GltfJsonContext.Default.Attributes);
            Assert.AreEqual(3, obj.TexCoords.Count);
            Assert.IsFalse(obj.GetTexCoord(0).HasValue);
            Assert.IsFalse(obj.GetTexCoord(1).HasValue);
            Assert.AreEqual(7, obj.GetTexCoord(2));

#pragma warning disable CS0618 // Type or member is obsolete
            Assert.IsTrue(obj.TryGetAllUVAccessors(out var oldAccessors, out var limitExceeded));
#pragma warning restore CS0618 // Type or member is obsolete
            Assert.IsFalse(limitExceeded);
            Assert.NotNull(oldAccessors);
            Assert.AreEqual(3, oldAccessors.Length);
            Assert.AreEqual(-1, oldAccessors[0]);
            Assert.AreEqual(-1, oldAccessors[1]);
            Assert.AreEqual(7, oldAccessors[2]);
        }

        [Test]
        public void AttributesMultiInfluenceSkinning()
        {
            var obj = JsonSerializer.Deserialize(
                @"{""COLOR_0"":11,""COLOR_1"":12,""JOINTS_0"":20,""JOINTS_1"":21,""WEIGHTS_0"":30,""WEIGHTS_1"":31}",
                GltfJsonContext.Default.Attributes);
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
                GltfJsonContext.Default.Attributes);
            Assert.AreEqual(13, obj.TexCoords.Count);
            Assert.AreEqual(99, obj.GetTexCoord(12));
        }

        [Test]
        public void AttributesCustomSemantic()
        {
            var obj = JsonSerializer.Deserialize(
                @"{""POSITION"":0,""_NORMALMAP"":5}",
                GltfJsonContext.Default.Attributes);
            Assert.AreEqual(0, obj.Position);
            Assert.IsTrue(obj.TryGetValue("_NORMALMAP", out int accessor));
            Assert.AreEqual(5, accessor);
            Assert.IsFalse(obj.TryGetValue("_MISSING", out int _));
        }

        [Test]
        public void AttributesAllProperties()
        {
            const string json = @"{""POSITION"":0,""NORMAL"":1,""TANGENT"":2,""TEXCOORD_0"":3,""TEXCOORD_1"":4,""TEXCOORD_2"":5,""TEXCOORD_3"":6,""TEXCOORD_4"":7,""TEXCOORD_5"":8,""TEXCOORD_6"":9,""TEXCOORD_7"":10,""TEXCOORD_8"":11,""COLOR_0"":12,""JOINTS_0"":13,""WEIGHTS_0"":14}";
            var obj = JsonSerializer.Deserialize(json, GltfJsonContext.Default.Attributes);
            Assert.AreEqual(0, obj.Position);
            Assert.AreEqual(1, obj.Normal);
            Assert.AreEqual(2, obj.Tangent);
            Assert.NotNull(obj.TexCoords);
            Assert.AreEqual(3, obj.TexCoords[0]);
            Assert.AreEqual(4, obj.TexCoords[1]);
            Assert.AreEqual(5, obj.TexCoords[2]);
            Assert.AreEqual(6, obj.TexCoords[3]);
            Assert.AreEqual(7, obj.TexCoords[4]);
            Assert.AreEqual(8, obj.TexCoords[5]);
            Assert.AreEqual(9, obj.TexCoords[6]);
            Assert.AreEqual(10, obj.TexCoords[7]);
            Assert.AreEqual(11, obj.TexCoords[8]);
            Assert.AreEqual(12, obj.Colors[0]);
            Assert.AreEqual(13, obj.Joints[0]);
            Assert.AreEqual(14, obj.Weights[0]);
        }

        [Test]
        public void MorphTargetDefault()
        {
            var obj = JsonSerializer.Deserialize("{}", GltfJsonContext.Default.MorphTarget);
            Assert.IsNotNull(obj);
            Assert.IsNull(obj.Position);
            Assert.IsNull(obj.Normal);
            Assert.IsNull(obj.Tangent);
        }

        [Test]
        public void MorphTargetAllProperties()
        {
            var obj = JsonSerializer.Deserialize(@"{""POSITION"":1,""NORMAL"":2,""TANGENT"":3}", GltfJsonContext.Default.MorphTarget);
            Assert.AreEqual(1, obj.Position);
            Assert.AreEqual(2, obj.Normal);
            Assert.AreEqual(3, obj.Tangent);
        }

        [Test]
        public void MeshPrimitiveExtensionsDefault()
        {
            var obj = JsonSerializer.Deserialize("{}", GltfJsonContext.Default.MeshPrimitiveExtensions);
            Assert.IsNotNull(obj);
            Assert.IsNull(obj.MaterialsVariants);
        }

        [Test]
        public void MeshPrimitiveExtensionsMaterialsVariants()
        {
            var obj = JsonSerializer.Deserialize(
                @"{""KHR_materials_variants"":{""mappings"":[{""material"":0,""variants"":[0]}]}}",
                GltfJsonContext.Default.MeshPrimitiveExtensions);
            Assert.IsNotNull(obj.MaterialsVariants);
            Assert.AreEqual(1, obj.MaterialsVariants.Mappings.Count);
        }

        [Test]
        public void MaterialsVariantsMeshPrimitiveExtensionDefault()
        {
            var obj = JsonSerializer.Deserialize("{}", GltfJsonContext.Default.MaterialsVariantsMeshPrimitiveExtension);
            Assert.IsNotNull(obj);
            Assert.IsNull(obj.Mappings);
        }

        [Test]
        public void MaterialsVariantsMeshPrimitiveExtensionMappings()
        {
            var obj = JsonSerializer.Deserialize(
                @"{""mappings"":[{""material"":1,""variants"":[0,1]}]}",
                GltfJsonContext.Default.MaterialsVariantsMeshPrimitiveExtension);
            Assert.AreEqual(1, obj.Mappings.Count);
            Assert.AreEqual(1, obj.Mappings[0].Material);
            Assert.AreEqual(new[] { 0, 1 }, obj.Mappings[0].Variants);
        }

        [Test]
        public void MaterialVariantsMappingDefault()
        {
            var obj = JsonSerializer.Deserialize("{}", GltfJsonContext.Default.MaterialVariantsMapping);
            Assert.IsNotNull(obj);
            Assert.AreEqual(SchemaConstants.UnsetIndex, obj.Material);
            Assert.IsNull(obj.Variants);
        }

        [Test]
        public void MaterialVariantsMappingMaterial()
        {
            var obj = JsonSerializer.Deserialize(@"{""material"":2}", GltfJsonContext.Default.MaterialVariantsMapping);
            Assert.AreEqual(2, obj.Material);
        }

        [Test]
        public void MaterialVariantsMappingVariants()
        {
            var obj = JsonSerializer.Deserialize(@"{""variants"":[0,1]}", GltfJsonContext.Default.MaterialVariantsMapping);
            Assert.AreEqual(new[] { 0, 1 }, obj.Variants);
        }

        [Test]
        public void NodeDefault()
        {
            var obj = JsonSerializer.Deserialize("{}", GltfJsonContext.Default.Node);
            Assert.IsNotNull(obj);
            Assert.IsNull(obj.Name);
            Assert.IsNull(obj.Extensions);
            Assert.IsNull(obj.Children);
            Assert.IsNull(obj.Mesh);
            Assert.IsNull(obj.Matrix);
            Assert.IsNull(obj.Rotation);
            Assert.IsNull(obj.Scale);
            Assert.IsNull(obj.Translation);
            Assert.IsNull(obj.Weights);
            Assert.IsNull(obj.Skin);
            Assert.IsNull(obj.Camera);
        }

        [Test]
        public void NodeName()
        {
            var obj = JsonSerializer.Deserialize(@"{""name"":""n""}", GltfJsonContext.Default.Node);
            Assert.AreEqual("n", obj.Name);
        }

        [Test]
        public void NodeChildren()
        {
            var obj = JsonSerializer.Deserialize(@"{""children"":[1,2]}", GltfJsonContext.Default.Node);
            Assert.AreEqual(new uint[] { 1, 2 }, obj.Children);
        }

        [Test]
        public void NodeMesh()
        {
            var obj = JsonSerializer.Deserialize(@"{""mesh"":3}", GltfJsonContext.Default.Node);
            Assert.AreEqual(3, obj.Mesh);
        }

        [Test]
        public void NodeMatrix()
        {
            var obj = JsonSerializer.Deserialize(@"{""matrix"":[1,0,0,0,0,1,0,0,0,0,1,0,1,2,3,1]}", GltfJsonContext.Default.Node);
            Assert.IsTrue(obj.Matrix.HasValue);
            Assert.AreEqual(
                new double4x4(
                    new double4(1, 0, 0, 0),
                    new double4(0, 1, 0, 0),
                    new double4(0, 0, 1, 0),
                    new double4(1, 2, 3, 1)),
                obj.Matrix.Value);
        }

        [Test]
        public void NodeRotation()
        {
            var obj = JsonSerializer.Deserialize(@"{""rotation"":[0,0,0,1]}", GltfJsonContext.Default.Node);
            Assert.IsTrue(obj.Rotation.HasValue);
            Assert.AreEqual(new double4(0, 0, 0, 1), obj.Rotation.Value);
        }

        [Test]
        public void NodeScale()
        {
            var obj = JsonSerializer.Deserialize(@"{""scale"":[1,2,3]}", GltfJsonContext.Default.Node);
            Assert.IsTrue(obj.Scale.HasValue);
            Assert.AreEqual(new double3(1, 2, 3), obj.Scale.Value);
        }

        [Test]
        public void NodeTranslation()
        {
            var obj = JsonSerializer.Deserialize(@"{""translation"":[4,5,6]}", GltfJsonContext.Default.Node);
            Assert.IsTrue(obj.Translation.HasValue);
            Assert.AreEqual(new double3(4, 5, 6), obj.Translation.Value);
        }

        [Test]
        public void NodeWeights()
        {
            var obj = JsonSerializer.Deserialize(@"{""weights"":[0.5]}", GltfJsonContext.Default.Node);
            Assert.AreEqual(new[] { 0.5f }, obj.Weights);
        }

        [Test]
        public void NodeSkin()
        {
            var obj = JsonSerializer.Deserialize(@"{""skin"":1}", GltfJsonContext.Default.Node);
            Assert.AreEqual(1, obj.Skin);
        }

        [Test]
        public void NodeCamera()
        {
            var obj = JsonSerializer.Deserialize(@"{""camera"":2}", GltfJsonContext.Default.Node);
            Assert.AreEqual(2, obj.Camera);
        }

        [Test]
        public void NodeExtensions()
        {
            var obj = JsonSerializer.Deserialize(
                @"{""extensions"":{""KHR_lights_punctual"":{""light"":0}}}",
                GltfJsonContext.Default.Node);
            Assert.IsNotNull(obj.Extensions);
            Assert.IsNotNull(obj.Extensions.LightsPunctual);
            Assert.AreEqual(0, obj.Extensions.LightsPunctual.Light);
        }

        [Test]
        public void NodeExtensionsDefault()
        {
            var obj = JsonSerializer.Deserialize("{}", GltfJsonContext.Default.NodeExtensions);
            Assert.IsNotNull(obj);
            Assert.IsNull(obj.MeshGpuInstancing);
            Assert.IsNull(obj.LightsPunctual);
        }

        [Test]
        public void NodeExtensionsMeshGpuInstancing()
        {
            var obj = JsonSerializer.Deserialize(
                @"{""EXT_mesh_gpu_instancing"":{""attributes"":{""TRANSLATION"":0}}}",
                GltfJsonContext.Default.NodeExtensions);
            Assert.IsNotNull(obj.MeshGpuInstancing);
            Assert.AreEqual(0, obj.MeshGpuInstancing.Attributes.Translation);
        }

        [Test]
        public void NodeExtensionsLightsPunctual()
        {
            var obj = JsonSerializer.Deserialize(@"{""KHR_lights_punctual"":{""light"":4}}", GltfJsonContext.Default.NodeExtensions);
            Assert.IsNotNull(obj.LightsPunctual);
            Assert.AreEqual(4, obj.LightsPunctual.Light);
        }

        [Test]
        public void MeshGpuInstancingDefault()
        {
            var obj = JsonSerializer.Deserialize("{}", GltfJsonContext.Default.MeshGpuInstancing);
            Assert.IsNotNull(obj);
            Assert.IsNull(obj.Attributes);
        }

        [Test]
        public void MeshGpuInstancingAttributes()
        {
            var obj = JsonSerializer.Deserialize(
                @"{""attributes"":{""TRANSLATION"":0,""ROTATION"":1,""SCALE"":2}}",
                GltfJsonContext.Default.MeshGpuInstancing);
            Assert.IsNotNull(obj.Attributes);
            Assert.AreEqual(0, obj.Attributes.Translation);
            Assert.AreEqual(1, obj.Attributes.Rotation);
            Assert.AreEqual(2, obj.Attributes.Scale);
        }

        [Test]
        public void InstancesAttributesDefault()
        {
            var obj = JsonSerializer.Deserialize("{}", GltfJsonContext.Default.InstancesAttributes);
            Assert.IsNotNull(obj);
            Assert.IsNull(obj.Translation);
            Assert.IsNull(obj.Rotation);
            Assert.IsNull(obj.Scale);
        }

        [Test]
        public void InstancesAttributesAllProperties()
        {
            var obj = JsonSerializer.Deserialize(@"{""TRANSLATION"":0,""ROTATION"":1,""SCALE"":2}", GltfJsonContext.Default.InstancesAttributes);
            Assert.AreEqual(0, obj.Translation);
            Assert.AreEqual(1, obj.Rotation);
            Assert.AreEqual(2, obj.Scale);
        }

        [Test]
        public void NodeLightsPunctualDefault()
        {
            var obj = JsonSerializer.Deserialize("{}", GltfJsonContext.Default.NodeLightsPunctual);
            Assert.IsNotNull(obj);
            Assert.IsNull(obj.Light);
        }

        [Test]
        public void NodeLightsPunctualLight()
        {
            var obj = JsonSerializer.Deserialize(@"{""light"":7}", GltfJsonContext.Default.NodeLightsPunctual);
            Assert.AreEqual(7, obj.Light);
        }

        [Test]
        public void RootExtensionsDefault()
        {
            var obj = JsonSerializer.Deserialize("{}", GltfJsonContext.Default.RootExtensions);
            Assert.IsNotNull(obj);
            Assert.IsNull(obj.LightsPunctual);
            Assert.IsNull(obj.MaterialsVariants);
        }

        [Test]
        public void RootExtensionsLightsPunctual()
        {
            var obj = JsonSerializer.Deserialize(@"{""KHR_lights_punctual"":{""lights"":[]}}", GltfJsonContext.Default.RootExtensions);
            Assert.IsNotNull(obj.LightsPunctual);
            Assert.AreEqual(0, obj.LightsPunctual.Lights.Count);
        }

        [Test]
        public void RootExtensionsMaterialsVariants()
        {
            var obj = JsonSerializer.Deserialize(@"{""KHR_materials_variants"":{""variants"":[]}}", GltfJsonContext.Default.RootExtensions);
            Assert.IsNotNull(obj.MaterialsVariants);
            Assert.AreEqual(0, obj.MaterialsVariants.Variants.Count);
        }

        [Test]
        public void LightsPunctualDefault()
        {
            var obj = JsonSerializer.Deserialize("{}", GltfJsonContext.Default.LightsPunctual);
            Assert.IsNotNull(obj);
            Assert.IsNull(obj.Lights);
        }

        [Test]
        public void LightsPunctualLights()
        {
            var obj = JsonSerializer.Deserialize(@"{""lights"":[{""type"":""directional""}]}", GltfJsonContext.Default.LightsPunctual);
            Assert.AreEqual(1, obj.Lights.Count);
            Assert.AreEqual(LightType.Directional, obj.Lights[0].Type.Value);
        }

        [Test]
        public void LightPunctualDefault()
        {
            var obj = JsonSerializer.Deserialize("{}", GltfJsonContext.Default.LightPunctual);
            Assert.IsNotNull(obj);
            Assert.AreEqual(Color.White, obj.Color);
            Assert.AreEqual(1f, obj.Intensity);
            Assert.AreEqual(-1f, obj.Range);
            Assert.IsNull(obj.Spot);
            Assert.AreEqual(LightType.Undefined, obj.Type.Value);
        }

        [Test]
        public void LightPunctualName()
        {
            var obj = JsonSerializer.Deserialize(@"{""name"":""L""}", GltfJsonContext.Default.LightPunctual);
            Assert.AreEqual("L", obj.Name);
        }

        [Test]
        public void LightPunctualColor()
        {
            var obj = JsonSerializer.Deserialize(@"{""color"":[0.5,0.25,0.75]}", GltfJsonContext.Default.LightPunctual);
            Assert.AreEqual(new Color(0.5f, 0.25f, 0.75f), obj.Color);
        }

        [Test]
        public void LightPunctualIntensity()
        {
            var obj = JsonSerializer.Deserialize(@"{""intensity"":2}", GltfJsonContext.Default.LightPunctual);
            Assert.AreEqual(2f, obj.Intensity);
        }

        [Test]
        public void LightPunctualRange()
        {
            var obj = JsonSerializer.Deserialize(@"{""range"":10}", GltfJsonContext.Default.LightPunctual);
            Assert.AreEqual(10f, obj.Range);
        }

        [Test]
        public void LightPunctualSpot()
        {
            var obj = JsonSerializer.Deserialize(@"{""spot"":{}}", GltfJsonContext.Default.LightPunctual);
            Assert.IsNotNull(obj.Spot);
        }

        [Test]
        [TestCase("spot", LightType.Spot)]
        [TestCase("directional", LightType.Directional)]
        [TestCase("point", LightType.Point)]
        public void LightPunctualType(string written, LightType expected)
        {
            var obj = JsonSerializer.Deserialize($@"{{""type"":""{written}""}}", GltfJsonContext.Default.LightPunctual);
            Assert.AreEqual(expected, obj.Type.Value);
        }

        [Test]
        public void SpotLightDefault()
        {
            var obj = JsonSerializer.Deserialize("{}", GltfJsonContext.Default.SpotLight);
            Assert.IsNotNull(obj);
            Assert.AreEqual(0f, obj.InnerConeAngle);
            Assert.AreEqual(System.MathF.PI / 4f, obj.OuterConeAngle);
        }

        [Test]
        public void SpotLightInnerConeAngle()
        {
            var obj = JsonSerializer.Deserialize(@"{""innerConeAngle"":0.25}", GltfJsonContext.Default.SpotLight);
            Assert.AreEqual(0.25f, obj.InnerConeAngle);
        }

        [Test]
        public void SpotLightOuterConeAngle()
        {
            var obj = JsonSerializer.Deserialize(@"{""outerConeAngle"":0.5}", GltfJsonContext.Default.SpotLight);
            Assert.AreEqual(0.5f, obj.OuterConeAngle);
        }

        [Test]
        public void MaterialsVariantsRootExtensionDefault()
        {
            var obj = JsonSerializer.Deserialize("{}", GltfJsonContext.Default.MaterialsVariantsRootExtension);
            Assert.IsNotNull(obj);
            Assert.IsNull(obj.Variants);
        }

        [Test]
        public void MaterialsVariantsRootExtensionVariants()
        {
            var obj = JsonSerializer.Deserialize(
                @"{""variants"":[{""name"":""v""}]}",
                GltfJsonContext.Default.MaterialsVariantsRootExtension);
            Assert.AreEqual(1, obj.Variants.Count);
            Assert.AreEqual("v", obj.Variants[0].Name);
        }

        [Test]
        public void MaterialsVariantDefault()
        {
            var obj = JsonSerializer.Deserialize("{}", GltfJsonContext.Default.MaterialsVariant);
            Assert.IsNotNull(obj);
            Assert.IsNull(obj.Name);
        }

        [Test]
        public void MaterialsVariantName()
        {
            var obj = JsonSerializer.Deserialize(@"{""name"":""x""}", GltfJsonContext.Default.MaterialsVariant);
            Assert.AreEqual("x", obj.Name);
        }

        [Test]
        public void SceneDefault()
        {
            var obj = JsonSerializer.Deserialize("{}", GltfJsonContext.Default.Scene);
            Assert.IsNotNull(obj);
            Assert.IsNull(obj.Name);
            Assert.IsNull(obj.Nodes);
        }

        [Test]
        public void SceneName()
        {
            var obj = JsonSerializer.Deserialize(@"{""name"":""s""}", GltfJsonContext.Default.Scene);
            Assert.AreEqual("s", obj.Name);
        }

        [Test]
        public void SceneNodes()
        {
            var obj = JsonSerializer.Deserialize(@"{""nodes"":[0,1]}", GltfJsonContext.Default.Scene);
            Assert.AreEqual(new uint[] { 0, 1 }, obj.Nodes);
        }

        [Test]
        public void SkinDefault()
        {
            var obj = JsonSerializer.Deserialize("{}", GltfJsonContext.Default.Skin);
            Assert.IsNotNull(obj);
            Assert.IsNull(obj.Name);
            Assert.IsNull(obj.InverseBindMatrices);
            Assert.IsNull(obj.Skeleton);
            Assert.IsNull(obj.Joints);
        }

        [Test]
        public void SkinName()
        {
            var obj = JsonSerializer.Deserialize(@"{""name"":""k""}", GltfJsonContext.Default.Skin);
            Assert.AreEqual("k", obj.Name);
        }

        [Test]
        public void SkinInverseBindMatrices()
        {
            var obj = JsonSerializer.Deserialize(@"{""inverseBindMatrices"":3}", GltfJsonContext.Default.Skin);
            Assert.AreEqual(3, obj.InverseBindMatrices);
        }

        [Test]
        public void SkinSkeleton()
        {
            var obj = JsonSerializer.Deserialize(@"{""skeleton"":5}", GltfJsonContext.Default.Skin);
            Assert.AreEqual(5, obj.Skeleton);
        }

        [Test]
        public void SkinJoints()
        {
            var obj = JsonSerializer.Deserialize(@"{""joints"":[1,2,3]}", GltfJsonContext.Default.Skin);
            Assert.AreEqual(new uint[] { 1, 2, 3 }, obj.Joints);
        }

        [Test]
        public void TextureDefault()
        {
            var obj = JsonSerializer.Deserialize("{}", GltfJsonContext.Default.Texture);
            Assert.IsNotNull(obj);
            Assert.IsNull(obj.Name);
            Assert.IsNull(obj.Extensions);
            Assert.IsNull(obj.Sampler);
            Assert.IsNull(obj.Source);
        }

        [Test]
        public void TextureName()
        {
            var obj = JsonSerializer.Deserialize(@"{""name"":""t""}", GltfJsonContext.Default.Texture);
            Assert.AreEqual("t", obj.Name);
        }

        [Test]
        public void TextureSampler()
        {
            var obj = JsonSerializer.Deserialize(@"{""sampler"":1}", GltfJsonContext.Default.Texture);
            Assert.AreEqual(1, obj.Sampler);
        }

        [Test]
        public void TextureSource()
        {
            var obj = JsonSerializer.Deserialize(@"{""source"":2}", GltfJsonContext.Default.Texture);
            Assert.AreEqual(2, obj.Source);
        }

        [Test]
        public void TextureExtensions()
        {
            var obj = JsonSerializer.Deserialize(
                @"{""extensions"":{""KHR_texture_basisu"":{""source"":3}}}",
                GltfJsonContext.Default.Texture);
            Assert.IsNotNull(obj.Extensions);
            Assert.IsNotNull(obj.Extensions.BasisU);
            Assert.AreEqual(3, obj.Extensions.BasisU.Source);
        }

        [Test]
        public void TextureExtensionsDefault()
        {
            var obj = JsonSerializer.Deserialize("{}", GltfJsonContext.Default.TextureExtensions);
            Assert.IsNotNull(obj);
            Assert.IsNull(obj.BasisU);
        }

        [Test]
        public void TextureExtensionsBasisU()
        {
            var obj = JsonSerializer.Deserialize(@"{""KHR_texture_basisu"":{""source"":1}}", GltfJsonContext.Default.TextureExtensions);
            Assert.IsNotNull(obj.BasisU);
            Assert.AreEqual(1, obj.BasisU.Source);
        }

        [Test]
        public void TextureBasisUniversalDefault()
        {
            var obj = JsonSerializer.Deserialize("{}", GltfJsonContext.Default.TextureBasisUniversal);
            Assert.IsNotNull(obj);
            Assert.IsNull(obj.Source);
        }

        [Test]
        public void TextureBasisUniversalSource()
        {
            var obj = JsonSerializer.Deserialize(@"{""source"":4}", GltfJsonContext.Default.TextureBasisUniversal);
            Assert.AreEqual(4, obj.Source);
        }

        [Test]
        public void TextureInfoDefault()
        {
            var obj = JsonSerializer.Deserialize("{}", GltfJsonContext.Default.TextureInfo);
            Assert.IsNotNull(obj);
            Assert.IsNull(obj.Index);
            Assert.AreEqual(0, obj.TexCoord);
            Assert.IsNull(obj.Extensions);
        }

        [Test]
        public void TextureInfoIndex()
        {
            var obj = JsonSerializer.Deserialize(@"{""index"":3}", GltfJsonContext.Default.TextureInfo);
            Assert.AreEqual(3, obj.Index);
        }

        [Test]
        public void TextureInfoTexCoord()
        {
            var obj = JsonSerializer.Deserialize(@"{""texCoord"":2}", GltfJsonContext.Default.TextureInfo);
            Assert.AreEqual(2, obj.TexCoord);
        }

        [Test]
        public void TextureInfoExtensions()
        {
            var obj = JsonSerializer.Deserialize(
                @"{""extensions"":{""KHR_texture_transform"":{}}}",
                GltfJsonContext.Default.TextureInfo);
            Assert.IsNotNull(obj.Extensions);
            Assert.IsNotNull(obj.Extensions.TextureTransform);
        }

        [Test]
        public void NormalTextureInfoDefault()
        {
            var obj = JsonSerializer.Deserialize("{}", GltfJsonContext.Default.NormalTextureInfo);
            Assert.IsNotNull(obj);
            Assert.AreEqual(1f, obj.Scale);
            Assert.IsNull(obj.Index);
        }

        [Test]
        public void NormalTextureInfoScale()
        {
            var obj = JsonSerializer.Deserialize(@"{""scale"":0.5}", GltfJsonContext.Default.NormalTextureInfo);
            Assert.AreEqual(0.5f, obj.Scale);
        }

        [Test]
        public void NormalTextureInfoIndex()
        {
            var obj = JsonSerializer.Deserialize(@"{""index"":0}", GltfJsonContext.Default.NormalTextureInfo);
            Assert.AreEqual(0, obj.Index);
        }

        [Test]
        public void OcclusionTextureInfoDefault()
        {
            var obj = JsonSerializer.Deserialize("{}", GltfJsonContext.Default.OcclusionTextureInfo);
            Assert.IsNotNull(obj);
            Assert.AreEqual(1f, obj.Strength);
            Assert.IsNull(obj.Index);
        }

        [Test]
        public void OcclusionTextureInfoStrength()
        {
            var obj = JsonSerializer.Deserialize(@"{""strength"":0.5}", GltfJsonContext.Default.OcclusionTextureInfo);
            Assert.AreEqual(0.5f, obj.Strength);
        }

        [Test]
        public void OcclusionTextureInfoIndex()
        {
            var obj = JsonSerializer.Deserialize(@"{""index"":0}", GltfJsonContext.Default.OcclusionTextureInfo);
            Assert.AreEqual(0, obj.Index);
        }

        [Test]
        public void TextureInfoExtensionsDefault()
        {
            var obj = JsonSerializer.Deserialize("{}", GltfJsonContext.Default.TextureInfoExtensions);
            Assert.IsNotNull(obj);
            Assert.IsNull(obj.TextureTransform);
        }

        [Test]
        public void TextureInfoExtensionsTextureTransform()
        {
            var obj = JsonSerializer.Deserialize(@"{""KHR_texture_transform"":{}}", GltfJsonContext.Default.TextureInfoExtensions);
            Assert.IsNotNull(obj.TextureTransform);
        }

        [Test]
        public void TextureTransformDefault()
        {
            var obj = JsonSerializer.Deserialize("{}", GltfJsonContext.Default.TextureTransform);
            Assert.IsNotNull(obj);
            Assert.IsFalse(obj.Offset.HasValue);
            Assert.AreEqual(0f, obj.Rotation);
            Assert.IsFalse(obj.Scale.HasValue);
            Assert.IsNull(obj.TexCoord);
        }

        [Test]
        public void TextureTransformOffset()
        {
            var obj = JsonSerializer.Deserialize(@"{""offset"":[0.5,0.25]}", GltfJsonContext.Default.TextureTransform);
            Assert.AreEqual(new float2(0.5f, 0.25f), obj.Offset);
        }

        [Test]
        public void TextureTransformRotation()
        {
            var obj = JsonSerializer.Deserialize(@"{""rotation"":0.5}", GltfJsonContext.Default.TextureTransform);
            Assert.AreEqual(0.5f, obj.Rotation);
        }

        [Test]
        public void TextureTransformScale()
        {
            var obj = JsonSerializer.Deserialize(@"{""scale"":[0.5,0.5]}", GltfJsonContext.Default.TextureTransform);
            Assert.AreEqual(new float2(0.5f, 0.5f), obj.Scale);
        }

        [Test]
        public void TextureTransformTexCoord()
        {
            var obj = JsonSerializer.Deserialize(@"{""texCoord"":1}", GltfJsonContext.Default.TextureTransform);
            Assert.AreEqual(1, obj.TexCoord);
        }

        [Test]
        public void UnclassifiedDataDefault()
        {
            var obj = JsonSerializer.Deserialize("{}", GltfJsonContext.Default.UnclassifiedData);
            Assert.IsNotNull(obj);
        }
    }
}
