// SPDX-FileCopyrightText: 2026 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GLTFast.Logging;
using GLTFast.Schema;
using NUnit.Framework;
using Unity.Collections;
using UnityEngine;
using UnityEngine.TestTools;
using Camera = GLTFast.Schema.Camera;
using GltfMaterial = GLTFast.Schema.Material;
using Material = UnityEngine.Material;
using Mesh = GLTFast.Schema.Mesh;
using Texture = GLTFast.Schema.Texture;

namespace GLTFast.Tests.Import
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
            using var buffers = new GltfBuffersMock();
            using var mg = new MeshGenerator(
                primitives,
                null,
                null,
                "meshName",
                new GltfReadableMock(),
                buffers,
                new UninterruptedDeferAgent(),
                logger
                );

            using var tokenSource = new CancellationTokenSource();
            var mesh = await mg.CreateMeshResult(tokenSource.Token);
            Assert.IsNull(mesh);
            var message = logger.Items.First();
            Assert.AreEqual(LogCode.IndexCountInvalid, message.Code);
            Assert.AreEqual("Invalid index count 2", message.ToString());
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

    sealed class GltfBuffersMock : IGltfBuffers, IDisposable
    {
        List<IDisposable> m_Disposables = new();

        public Accessor GetAccessor(int index)
        {
            switch (index)
            {
                case 0:
                {
                    var accessor = new Accessor
                    {
                        BufferView = 0,
                        ByteOffset = 0,
                        ComponentType = AccessorDataType.Float,
                        Count = 3
                    };
                    accessor.Type = new EnumOrRawValue<AccessorType>(AccessorType.Vector3);
                    return accessor;
                }
                case 1:
                {
                    var accessor = new Accessor
                    {
                        BufferView = 0,
                        ByteOffset = 0,
                        ComponentType = AccessorDataType.UnsignedShort,
                        Count = 2
                    };
                    accessor.Type = new EnumOrRawValue<AccessorType>(AccessorType.Scalar);
                    return accessor;
                }
            }
            throw new NotImplementedException();
        }

        public unsafe void GetAccessorAndData(int index, out Accessor accessor, out void* data, out int? byteStride)
        {
            throw new NotImplementedException();
        }
        public unsafe void GetAccessorSparseIndices(AccessorSparseIndices sparseIndices, out void* data)
        {
            throw new NotImplementedException();
        }
        public unsafe void GetAccessorSparseValues(AccessorSparseValues sparseValues, out void* data)
        {
            throw new NotImplementedException();
        }
        public ReadOnlyNativeArray<byte> GetBufferView(int bufferViewIndex, out int? byteStride, int offset = 0, int length = 0)
        {
            var indices = new NativeArray<ushort>(3, Allocator.Persistent);
            m_Disposables.Add(indices);
            byteStride = 2;
            return new ReadOnlyNativeArray<byte>(indices.Reinterpret<byte>(sizeof(ushort)));
        }
        public ReadOnlyNativeArray<T> GetAccessorData<T>(int bufferViewIndex, int count, int offset = 0) where T : unmanaged
        {
            throw new NotImplementedException();
        }
        public ReadOnlyNativeStridedArray<T> GetStridedAccessorData<T>(int bufferViewIndex, int count, int offset = 0) where T : unmanaged
        {
            var buffer = new NativeArray<T>(3, Allocator.Persistent);
            m_Disposables.Add(buffer);
            return new ReadOnlyNativeArray<T>(buffer).ToStrided<T>(bufferViewIndex, count, 12);
        }

        public void Dispose()
        {
            foreach (var disposable in m_Disposables)
            {
                disposable.Dispose();
            }
        }
    }
}
