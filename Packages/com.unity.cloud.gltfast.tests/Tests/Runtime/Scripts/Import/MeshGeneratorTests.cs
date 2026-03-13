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
using Material = UnityEngine.Material;

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
                mode = DrawMode.TriangleFan,
                attributes = new Attributes
                {
                    POSITION = 0
                },
                indices = 1
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
            throw new System.NotImplementedException();
        }
        public Task<Material> GetMaterialAsync(int index)
        {
            throw new System.NotImplementedException();
        }
        public Task<Material> GetMaterialAsync(int index, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }
        public Task<Material> GetDefaultMaterialAsync()
        {
            throw new System.NotImplementedException();
        }
        public Task<Material> GetDefaultMaterialAsync(CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }
        public IMaterialsVariantsSlot[] GetMaterialsVariantsSlots(int meshIndex, int meshNumeration)
        {
            throw new System.NotImplementedException();
        }
        public int MaterialCount { get; }
        public int ImageCount { get; }
        public int TextureCount { get; }
        public Material GetMaterial(int index = 0)
        {
            throw new System.NotImplementedException();
        }
        public Material GetDefaultMaterial()
        {
            throw new System.NotImplementedException();
        }
        public Texture2D GetImage(int index = 0)
        {
            throw new System.NotImplementedException();
        }
        public Texture2D GetTexture(int index = 0)
        {
            throw new System.NotImplementedException();
        }
        public bool IsTextureYFlipped(int index = 0)
        {
            throw new System.NotImplementedException();
        }
        public CameraBase GetSourceCamera(uint index)
        {
            throw new System.NotImplementedException();
        }
        public MaterialBase GetSourceMaterial(int index = 0)
        {
            throw new System.NotImplementedException();
        }
        public MeshBase GetSourceMesh(int meshIndex)
        {
            throw new System.NotImplementedException();
        }
        public MeshPrimitiveBase GetSourceMeshPrimitive(int meshIndex, int primitiveIndex)
        {
            throw new System.NotImplementedException();
        }
        public NodeBase GetSourceNode(int index = 0)
        {
            throw new System.NotImplementedException();
        }
        public Scene GetSourceScene(int index = 0)
        {
            throw new System.NotImplementedException();
        }
        public TextureBase GetSourceTexture(int index = 0)
        {
            throw new System.NotImplementedException();
        }
        public Image GetSourceImage(int index = 0)
        {
            throw new System.NotImplementedException();
        }
        public LightPunctual GetSourceLightPunctual(uint index)
        {
            throw new System.NotImplementedException();
        }
        public Matrix4x4[] GetBindPoses(int skinId)
        {
            throw new System.NotImplementedException();
        }
        public NativeSlice<byte> GetAccessor(int accessorIndex)
        {
            throw new System.NotImplementedException();
        }
        public NativeSlice<byte> GetAccessorData(int accessorIndex)
        {
            throw new System.NotImplementedException();
        }
    }

    sealed class GltfBuffersMock : IGltfBuffers, IDisposable
    {
        List<IDisposable> m_Disposables = new();

        public AccessorBase GetAccessor(int index)
        {
            switch (index)
            {
                case 0:
                {
                    var accessor = new Accessor
                    {
                        bufferView = 0,
                        byteOffset = 0,
                        componentType = GltfComponentType.Float,
                        count = 3
                    };
                    accessor.SetAttributeType(GltfAccessorAttributeType.VEC3);
                    return accessor;
                }
                case 1:
                {
                    var accessor = new Accessor
                    {
                        bufferView = 0,
                        byteOffset = 0,
                        componentType = GltfComponentType.UnsignedShort,
                        count = 2
                    };
                    accessor.SetAttributeType(GltfAccessorAttributeType.SCALAR);
                    return accessor;
                }
            }
            throw new System.NotImplementedException();
        }

        public unsafe void GetAccessorAndData(int index, out AccessorBase accessor, out void* data, out int byteStride)
        {
            throw new System.NotImplementedException();
        }
        public unsafe void GetAccessorSparseIndices(AccessorSparseIndices sparseIndices, out void* data)
        {
            throw new System.NotImplementedException();
        }
        public unsafe void GetAccessorSparseValues(AccessorSparseValues sparseValues, out void* data)
        {
            throw new System.NotImplementedException();
        }
        public ReadOnlyNativeArray<byte> GetBufferView(int bufferViewIndex, out int byteStride, int offset = 0, int length = 0)
        {
            var indices = new NativeArray<ushort>(3, Allocator.Persistent);
            m_Disposables.Add(indices);
            byteStride = 2;
            return new ReadOnlyNativeArray<byte>(indices.Reinterpret<byte>(sizeof(ushort)));
        }
        public ReadOnlyNativeArray<T> GetAccessorData<T>(int bufferViewIndex, int count, int offset = 0) where T : unmanaged
        {
            throw new System.NotImplementedException();
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
