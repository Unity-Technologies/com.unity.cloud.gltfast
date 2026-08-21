// SPDX-FileCopyrightText: 2026 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Cloud.Gltfast.Addons;
using Unity.Cloud.Gltfast.Logging;
using UnityEngine.TestTools;

namespace Unity.Cloud.Gltfast.Tests.Import
{
    [Category("Import")]
    class BufferDataTests
    {
        // One buffer of four bytes, one buffer view over all of it, one SCALAR/UNSIGNED_BYTE
        // accessor of four elements. Element byte size is 1.
        const string k_MinimalGltfJson =
            "{\"asset\":{\"version\":\"2.0\"}," +
            "\"buffers\":[{\"byteLength\":4,\"uri\":\"data:application/octet-stream;base64,AQIDBA==\"}]," +
            "\"bufferViews\":[{\"buffer\":0,\"byteLength\":4}]," +
            "\"accessors\":[{\"bufferView\":0,\"componentType\":5121,\"count\":4,\"type\":\"SCALAR\"}]}";

        /// <summary>
        /// Runs an assertion against a lease while the glTF's buffers are still loaded.
        /// </summary>
        class BufferDataProbe : ImportAddonInstance, IBufferDataConsumer
        {
            readonly System.Action<IGltfBufferData> m_Probe;

            public BufferDataProbe(System.Action<IGltfBufferData> probe)
            {
                m_Probe = probe;
            }

            public bool Invoked { get; private set; }

            public Task<bool> ConsumeBufferDataAsync(IGltfBufferData bufferData, CancellationToken cancellationToken)
            {
                Invoked = true;
                m_Probe(bufferData);
                return Task.FromResult(true);
            }

            public override bool SupportsGltfExtension(string extensionName) => false;
            public override void Inject(GltfImport gltfImport) => gltfImport.AddImportAddonInstance(this);
            public override void Inject(IInstantiator instantiator) { }
            public override void Dispose() { }
        }

        static async Task<BufferDataProbe> LoadWithProbeAsync(System.Action<IGltfBufferData> probe)
        {
            using var gltf = new GltfImport(logger: new CollectingLogger());
            var instance = new BufferDataProbe(probe);
            instance.Inject(gltf);
            var success = await gltf.LoadGltfJsonAsync(k_MinimalGltfJson);
            Assert.IsTrue(success, "Import failed.");
            Assert.IsTrue(instance.Invoked, "IBufferDataConsumer was never called.");
            return instance;
        }

        [UnityTest]
        public IEnumerator AccessorDataIsProvided()
        {
            yield return AsyncWrapper.WaitForTask(LoadWithProbeAsync(bufferData =>
            {
                var status = bufferData.GetAccessorData<byte>(0, out var data);
                Assert.AreEqual(BufferAccessStatus.Success, status);
                Assert.AreEqual(4, data.Length);
                Assert.AreEqual(1, data[0]);
                Assert.AreEqual(4, data[3]);
            }));
        }

        [UnityTest]
        public IEnumerator BufferViewIsProvided()
        {
            yield return AsyncWrapper.WaitForTask(LoadWithProbeAsync(bufferData =>
            {
                var status = bufferData.GetBufferView(0, out var data, out var byteStride);
                Assert.AreEqual(BufferAccessStatus.Success, status);
                Assert.AreEqual(4, data.Length);
                Assert.IsNull(byteStride, "Tightly packed buffer view should report no stride.");
            }));
        }

        [UnityTest]
        public IEnumerator InvalidIndexIsReported()
        {
            yield return AsyncWrapper.WaitForTask(LoadWithProbeAsync(bufferData =>
            {
                Assert.AreEqual(BufferAccessStatus.ObjectIndexOutOfRange, bufferData.GetAccessorData<byte>(-1, out _));
                Assert.AreEqual(BufferAccessStatus.ObjectIndexOutOfRange, bufferData.GetAccessorData<byte>(int.MaxValue, out _));
                Assert.AreEqual(BufferAccessStatus.ObjectIndexOutOfRange, bufferData.GetBufferView(-1, out _, out _));
                Assert.AreEqual(BufferAccessStatus.ObjectIndexOutOfRange, bufferData.GetBufferView(int.MaxValue, out _, out _));
            }));
        }

        [UnityTest]
        public IEnumerator TypeMismatchIsReported()
        {
            yield return AsyncWrapper.WaitForTask(LoadWithProbeAsync(bufferData =>
            {
                // The accessor's element byte size is 1, so a four byte element type cannot match.
                Assert.AreEqual(BufferAccessStatus.TypeMismatch, bufferData.GetAccessorData<float>(0, out _));
                Assert.AreEqual(BufferAccessStatus.TypeMismatch, bufferData.GetStridedAccessorData<float>(0, out _));
            }));
        }

        [UnityTest]
        public IEnumerator DataIsUnavailableAfterImport()
        {
            yield return AsyncWrapper.WaitForTask(DataIsUnavailableAfterImportAsync());
        }

        static async Task DataIsUnavailableAfterImportAsync()
        {
            using var gltf = new GltfImport(logger: new CollectingLogger());
            Assert.IsTrue(await gltf.LoadGltfJsonAsync(k_MinimalGltfJson), "Import failed.");

            using var bufferData = gltf.LeaseBufferData();
            Assert.AreEqual(BufferAccessStatus.BufferUnavailable, bufferData.GetAccessorData<byte>(0, out _));
        }

        [UnityTest]
        public IEnumerator LeaseKeepsDataAliveBeyondImport()
        {
            yield return AsyncWrapper.WaitForTask(LeaseKeepsDataAliveBeyondImportAsync());
        }

        static async Task LeaseKeepsDataAliveBeyondImportAsync()
        {
            using var gltf = new GltfImport(logger: new CollectingLogger());
            var probe = new RetainingProbe(gltf);
            probe.Inject(gltf);
            Assert.IsTrue(await gltf.LoadGltfJsonAsync(k_MinimalGltfJson), "Import failed.");
            var retained = probe.Retained;
            Assert.IsNotNull(retained, "No lease was retained.");

            // The import is done, yet the retained lease keeps the buffer data readable.
            Assert.AreEqual(BufferAccessStatus.Success, retained.GetAccessorData<byte>(0, out var data));
            Assert.AreEqual(4, data.Length);
            Assert.AreEqual(1, data[0]);

            retained.Dispose();
            Assert.AreEqual(BufferAccessStatus.BufferUnavailable, retained.GetAccessorData<byte>(0, out _));
        }

#if MESHOPT_IS_RECENT && UNITY_EDITOR
        /// <summary>
        /// The meshopt decode jobs write into the buffer views they decode into, so an add-on must
        /// not be handed a lease before they completed. Reading such a buffer view mid-decode is
        /// rejected by Unity's job safety system, and its data is not decoded yet either.
        /// </summary>
        [UnityTest]
        public IEnumerator MeshoptDataIsDecodedBeforeConsumers()
        {
            yield return AsyncWrapper.WaitForTask(MeshoptDataIsDecodedBeforeConsumersAsync());
        }

        static async Task MeshoptDataIsDecodedBeforeConsumersAsync()
        {
            // Buffer views 0 to 9 of this asset are EXT_meshopt_compression compressed.
            const int meshoptBufferViewCount = 10;

            var path = System.IO.Path.Combine(
                System.IO.Path.GetFullPath("Packages/com.unity.cloud.gltfast.tests/Assets~"),
                "Unity", "RainbowCuboid", "meshopt-c", "RainbowCuboid-meshopt-c.gltf");
            Assert.IsTrue(System.IO.File.Exists(path), $"Test asset is missing: {path}");

            var logger = new CollectingLogger();
            using var gltf = new GltfImport(logger: logger);
            var inspected = 0;
            var probe = new BufferDataProbe(bufferData =>
            {
                for (var i = 0; i < meshoptBufferViewCount; i++)
                {
                    Assert.AreEqual(
                        BufferAccessStatus.Success,
                        bufferData.GetBufferView(i, out var data, out _),
                        $"Buffer view {i} was not provided.");
                    Assert.Greater(data.Length, 0, $"Buffer view {i} is empty.");

                    // Indexing is what the job safety system rejects while a job still writes to
                    // the array, and an un-decoded buffer would read as all zero.
                    var decoded = false;
                    for (var b = 0; b < data.Length && !decoded; b++)
                    {
                        decoded = data[b] != 0;
                    }
                    Assert.IsTrue(decoded, $"Buffer view {i} was not decoded.");
                    inspected++;
                }
            });
            probe.Inject(gltf);

            var success = await gltf.LoadAsync(new System.Uri(path));
            if (!success)
            {
                logger.LogAll();
            }
            Assert.IsTrue(success, "Import failed.");
            Assert.IsTrue(probe.Invoked, "IBufferDataConsumer was never called.");
            Assert.AreEqual(meshoptBufferViewCount, inspected);
        }
#endif

        class RetainingProbe : ImportAddonInstance, IBufferDataConsumer
        {
            readonly GltfImport m_GltfImport;

            public RetainingProbe(GltfImport gltfImport)
            {
                m_GltfImport = gltfImport;
            }

            public IGltfBufferData Retained { get; private set; }

            public Task<bool> ConsumeBufferDataAsync(IGltfBufferData bufferData, CancellationToken cancellationToken)
            {
                Retained = m_GltfImport.LeaseBufferData();
                return Task.FromResult(true);
            }

            public override bool SupportsGltfExtension(string extensionName) => false;
            public override void Inject(GltfImport gltfImport) => gltfImport.AddImportAddonInstance(this);
            public override void Inject(IInstantiator instantiator) { }
            public override void Dispose() { }
        }
    }
}
