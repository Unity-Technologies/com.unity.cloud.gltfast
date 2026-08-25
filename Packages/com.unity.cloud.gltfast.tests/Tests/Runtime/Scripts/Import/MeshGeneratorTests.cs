// SPDX-FileCopyrightText: 2026 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Cloud.Gltfast.Logging;
using Unity.Cloud.Gltfast.Objects;
using Unity.Collections;
using UnityEngine;
using UnityEngine.TestTools;
using Camera = Unity.Cloud.Gltfast.Objects.Camera;
using GltfMaterial = Unity.Cloud.Gltfast.Objects.Material;
using Material = UnityEngine.Material;
using Mesh = Unity.Cloud.Gltfast.Objects.Mesh;
using Texture = Unity.Cloud.Gltfast.Objects.Texture;

namespace Unity.Cloud.Gltfast.Tests.Import
{
    [Category("Import")]
    class MeshGeneratorTests
    {
        [UnityTest]
        public IEnumerator IndexCountInvalid()
        {
            yield return AsyncWrapper.WaitForTask(IndexCountInvalidAsync());
        }

        static async Task IndexCountInvalidAsync()
        {
            var primitives = new[] { new MeshPrimitive
            {
                Mode = PrimitiveMode.TriangleFan,
                Attributes = new Attributes
                {
                    Position = 0
                },
                Indices = 1
            } };

            var logger = new CollectingLogger();
            using var fixture = new BufferStoreFixture(CreateRoot(), new byte[k_BufferByteLength]);
            using var mg = new MeshGenerator(
                primitives,
                null,
                null,
                "meshName",
                new GltfReadableMock(),
                fixture.Store,
                new UninterruptedDeferAgent(),
                logger
                );

            using var tokenSource = new CancellationTokenSource();
            var mesh = await mg.CreateMeshResultAsync(tokenSource.Token);
            Assert.IsNull(mesh);
            var message = logger.Items.First();
            Assert.AreEqual(LogCode.IndexCountInvalid, message.Code);
            Assert.AreEqual("Invalid index count 2", message.ToString());
        }

        // Three float3 positions, then two ushort indices.
        const int k_PositionByteLength = 3 * 12;
        const int k_IndexByteLength = 2 * 2;
        const int k_BufferByteLength = k_PositionByteLength + k_IndexByteLength;

        static Root CreateRoot()
        {
            var positions = new Accessor
            {
                BufferView = 0,
                ByteOffset = 0,
                ComponentType = AccessorDataType.Float,
                Count = 3
            };
            positions.Type = new EnumOrRawValue<AccessorType>(AccessorType.Vector3);

            var indices = new Accessor
            {
                BufferView = 1,
                ByteOffset = 0,
                ComponentType = AccessorDataType.UnsignedShort,
                Count = 2
            };
            indices.Type = new EnumOrRawValue<AccessorType>(AccessorType.Scalar);

            return new Root
            {
                Buffers = new List<Objects.Buffer> { new() { ByteLength = k_BufferByteLength } },
                BufferViews = new List<BufferView>
                {
                    new() { Buffer = 0, ByteOffset = 0, ByteLength = k_PositionByteLength, ByteStride = 12 },
                    new() { Buffer = 0, ByteOffset = k_PositionByteLength, ByteLength = k_IndexByteLength }
                },
                Accessors = new List<Accessor> { positions, indices }
            };
        }
    }

    class GltfReadableMock : IGltfReadable
    {
        public int MaterialsVariantsCount { get; }
        public string GetMaterialsVariantName(int index)
        {
            throw new NotImplementedException();
        }
        public Task<Material> GetMaterialAsync(int index)
        {
            throw new NotImplementedException();
        }
        public Task<Material> GetMaterialAsync(int index, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
        public Task<Material> GetDefaultMaterialAsync()
        {
            throw new NotImplementedException();
        }
        public Task<Material> GetDefaultMaterialAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
        public IMaterialsVariantsSlot[] GetMaterialsVariantsSlots(int meshIndex, int meshNumeration)
        {
            throw new NotImplementedException();
        }

        public Root Root => throw new NotImplementedException();

        public int MaterialCount { get; }
        public int ImageCount { get; }
        public int TextureCount { get; }
        public Material GetMaterial(int index = 0)
        {
            throw new NotImplementedException();
        }
        public Material GetDefaultMaterial()
        {
            throw new NotImplementedException();
        }
        public Texture2D GetImage(int index = 0)
        {
            throw new NotImplementedException();
        }
        public Texture2D GetTexture(int index = 0)
        {
            throw new NotImplementedException();
        }
        public bool IsTextureYFlipped(int index = 0)
        {
            throw new NotImplementedException();
        }
        public Camera GetSourceCamera(uint index)
        {
            throw new NotImplementedException();
        }
        public GltfMaterial GetSourceMaterial(int index = 0)
        {
            throw new NotImplementedException();
        }
        public Mesh GetSourceMesh(int meshIndex)
        {
            throw new NotImplementedException();
        }
        public MeshPrimitive GetSourceMeshPrimitive(int meshIndex, int primitiveIndex)
        {
            throw new NotImplementedException();
        }
        public Node GetSourceNode(int index = 0)
        {
            throw new NotImplementedException();
        }
        public Scene GetSourceScene(int index = 0)
        {
            throw new NotImplementedException();
        }
        public Texture GetSourceTexture(int index = 0)
        {
            throw new NotImplementedException();
        }
        public Image GetSourceImage(int index = 0)
        {
            throw new NotImplementedException();
        }
        public LightPunctual GetSourceLightPunctual(uint index)
        {
            throw new NotImplementedException();
        }
        public Matrix4x4[] GetBindPoses(int skinId)
        {
            throw new NotImplementedException();
        }
        public NativeArray<byte>.ReadOnly GetAccessor(int accessorIndex)
        {
            throw new NotImplementedException();
        }
        public NativeArray<byte>.ReadOnly GetAccessorData(int accessorIndex)
        {
            throw new NotImplementedException();
        }
    }

}
