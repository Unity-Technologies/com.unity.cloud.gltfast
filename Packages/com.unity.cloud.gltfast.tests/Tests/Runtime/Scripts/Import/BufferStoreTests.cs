// SPDX-FileCopyrightText: 2026 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Cloud.Gltfast.Logging;
using Unity.Cloud.Gltfast.Objects;
using Unity.Cloud.Gltfast.Text.Json;
using Unity.Collections;
using UnityEngine.TestTools;

namespace Unity.Cloud.Gltfast.Tests.Import
{
    /// <summary>
    /// Covers <see cref="BufferStore"/>'s buffer view and accessor resolution, in particular that
    /// every out of range or malformed request is answered with a <see cref="BufferAccessStatus"/>
    /// instead of an exception or an out of bounds read.
    /// </summary>
    /// <remarks>
    /// The meshopt and download code paths are exercised by the sample asset import tests instead;
    /// they need encoded payloads and a download provider that this fixture does not set up.
    /// </remarks>
    [Category("Import")]
    class BufferStoreTests
    {
        [StructLayout(LayoutKind.Sequential)]
        struct Float3
        {
            public float X;
            public float Y;
            public float Z;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct Float4
        {
            public float X;
            public float Y;
            public float Z;
            public float W;
        }

        /// <summary>
        /// Drives a <see cref="BufferStore"/> over hand written JSON. Buffer 0 is backed by memory
        /// whose length is set independently of the binary chunk's declared range, which is what
        /// glTF-binary looks like: the array is the whole document and only a slice of it is the
        /// buffer.
        /// </summary>
        sealed class Harness : IDisposable
        {
            readonly List<UriValue> m_PendingUris;
            readonly List<IDisposable> m_Tracked = new List<IDisposable>();
            NativeArray<byte> m_Memory;

            public Harness(string json, int memoryLength = 0, int chunkStart = 0, int chunkLength = -1)
            {
                UriValueConverter.BeginCollect();
                try
                {
                    Root = JsonSerializer.Deserialize(
                        Encoding.UTF8.GetBytes(json), GltfJsonContext.Default.Root);
                }
                finally
                {
                    m_PendingUris = UriValueConverter.EndCollect();
                }

                Store = new BufferStore(
                    new ImportContext(null, Logger, null),
                    disposable => m_Tracked.Add(disposable),
                    null);
                Store.Initialize(Root, null);
                Store.StartBufferLoads(CancellationToken.None);

                if (memoryLength <= 0)
                {
                    return;
                }

                m_Memory = new NativeArray<byte>(memoryLength, Allocator.Persistent);
                for (var i = 0; i < memoryLength; i++)
                {
                    m_Memory[i] = (byte)i;
                }
                Store.SetGlbBinChunk(new GlbBinChunk(
                    chunkStart,
                    (uint)(chunkLength < 0 ? memoryLength - chunkStart : chunkLength)));
                Store.AssignGlbBinChunk(m_Memory.AsReadOnly());
            }

            public CollectingLogger Logger { get; } = new CollectingLogger();

            public BufferStore Store { get; }

            public Root Root { get; }

            public BufferView View(int index) => Root.BufferViews[index];

            public void Dispose()
            {
                Store.RequestDispose();
                foreach (var disposable in m_Tracked)
                {
                    disposable.Dispose();
                }
                if (m_PendingUris != null)
                {
                    foreach (var uri in m_PendingUris)
                    {
                        uri.Dispose();
                    }
                }
                if (m_Memory.IsCreated)
                {
                    m_Memory.Dispose();
                }
            }
        }

        // Buffer 0 has no URI, so its memory is supplied as a binary chunk by the harness.
        // View 0 spans all 64 bytes, view 1 is the 16 bytes at offset 16.
        const string k_SimpleJson =
            "{\"asset\":{\"version\":\"2.0\"}," +
            "\"buffers\":[{\"byteLength\":64}]," +
            "\"bufferViews\":[" +
            "{\"buffer\":0,\"byteLength\":64}," +
            "{\"buffer\":0,\"byteOffset\":16,\"byteLength\":16}" +
            "]}";

        // Mirrors the Khronos ClearCoatTest layout: an interleaved view that ends exactly at the
        // buffer's end, whose accessors start partway into the stride. The last element of such an
        // accessor occupies its element size, not a whole stride.
        const string k_InterleavedJson =
            "{\"asset\":{\"version\":\"2.0\"}," +
            "\"buffers\":[{\"byteLength\":384}]," +
            "\"bufferViews\":[{\"buffer\":0,\"byteLength\":384,\"byteStride\":48}]," +
            "\"accessors\":[" +
            "{\"bufferView\":0,\"byteOffset\":12,\"componentType\":5126,\"count\":8,\"type\":\"VEC3\"}," +
            "{\"bufferView\":0,\"byteOffset\":32,\"componentType\":5126,\"count\":8,\"type\":\"VEC4\"}" +
            "]}";

        static Harness Simple(int memoryLength = 64, int chunkStart = 0, int chunkLength = -1)
            => new Harness(k_SimpleJson, memoryLength, chunkStart, chunkLength);

        static bool Logged(Harness harness, LogCode code)
            => harness.Logger.Items?.Any(item => item.Code == code) ?? false;

        #region Buffer view resolution

        [Test]
        public void BufferViewFullRange()
        {
            using var h = Simple();
            Assert.AreEqual(BufferAccessStatus.Success, h.Store.TryGetBufferView(h.View(0), out var data));
            Assert.AreEqual(64, data.Length);
            Assert.AreEqual(0, data[0]);
            Assert.AreEqual(63, data[63]);
        }

        [Test]
        public void BufferViewRespectsByteOffset()
        {
            using var h = Simple();
            Assert.AreEqual(BufferAccessStatus.Success, h.Store.TryGetBufferView(h.View(1), out var data));
            Assert.AreEqual(16, data.Length);
            Assert.AreEqual(16, data[0], "View 1 starts 16 bytes into the buffer.");
        }

        [Test]
        public void BufferViewRespectsChunkStart()
        {
            // The chunk starts 16 bytes into a 96 byte array, so view 0's first byte is memory[16].
            using var h = Simple(memoryLength: 96, chunkStart: 16, chunkLength: 64);
            Assert.AreEqual(BufferAccessStatus.Success, h.Store.TryGetBufferView(h.View(0), out var data));
            Assert.AreEqual(64, data.Length);
            Assert.AreEqual(16, data[0]);
        }

        [Test]
        public void BufferViewSubRange()
        {
            using var h = Simple();
            Assert.AreEqual(
                BufferAccessStatus.Success, h.Store.TryGetBufferView(h.View(0), out var data, 8, 16));
            Assert.AreEqual(16, data.Length);
            Assert.AreEqual(8, data[0]);
        }

        [Test]
        public void BufferViewNegativeOffsetIsRejected()
        {
            using var h = Simple();
            Assert.AreEqual(
                BufferAccessStatus.DataIndexOutOfRange,
                h.Store.TryGetBufferView(h.View(0), out _, -1));
        }

        [Test]
        public void BufferViewRangeBeyondViewIsRejected()
        {
            using var h = Simple();
            Assert.AreEqual(
                BufferAccessStatus.DataIndexOutOfRange,
                h.Store.TryGetBufferView(h.View(1), out _, 0, 32),
                "View 1 is only 16 bytes long.");
            Assert.AreEqual(
                BufferAccessStatus.DataIndexOutOfRange,
                h.Store.TryGetBufferView(h.View(1), out _, 8, 16));
        }

        [Test]
        public void BufferViewOffsetLengthOverflowIsRejected()
        {
            using var h = Simple();
            Assert.AreEqual(
                BufferAccessStatus.DataIndexOutOfRange,
                h.Store.TryGetBufferView(h.View(0), out _, int.MaxValue, int.MaxValue));
        }

        /// <summary>
        /// A view may not reach outside the buffer's own range even when the memory backing it
        /// extends further, as it does for glTF-binary where the array is the entire document.
        /// </summary>
        [Test]
        public void BufferViewCannotEscapeBinaryChunk()
        {
            const string json =
                "{\"asset\":{\"version\":\"2.0\"}," +
                "\"buffers\":[{\"byteLength\":64}]," +
                "\"bufferViews\":[{\"buffer\":0,\"byteOffset\":32,\"byteLength\":64}]}";
            // 128 bytes of memory, but only the first 64 belong to buffer 0.
            using var h = new Harness(json, memoryLength: 128, chunkStart: 0, chunkLength: 64);
            Assert.AreEqual(
                BufferAccessStatus.DataIndexOutOfRange,
                h.Store.TryGetBufferView(h.View(0), out _),
                "Range 32-96 fits the array but not the 64 byte binary chunk.");
        }

        [Test]
        public void BufferViewNegativeByteOffsetIsRejected()
        {
            const string json =
                "{\"asset\":{\"version\":\"2.0\"}," +
                "\"buffers\":[{\"byteLength\":64}]," +
                "\"bufferViews\":[{\"buffer\":0,\"byteOffset\":-16,\"byteLength\":16}]}";
            using var h = new Harness(json, memoryLength: 64);
            Assert.AreEqual(
                BufferAccessStatus.DataIndexOutOfRange, h.Store.TryGetBufferView(h.View(0), out _));

            // With the chunk starting further in, the resolved offset would land inside the array
            // yet before the buffer, so the negative offset has to be rejected on its own merit.
            using var offsetChunk = new Harness(json, memoryLength: 96, chunkStart: 32, chunkLength: 64);
            Assert.AreEqual(
                BufferAccessStatus.DataIndexOutOfRange,
                offsetChunk.Store.TryGetBufferView(offsetChunk.View(0), out _));
        }

        [Test]
        public void BufferViewNegativeByteLengthIsRejected()
        {
            const string json =
                "{\"asset\":{\"version\":\"2.0\"}," +
                "\"buffers\":[{\"byteLength\":64}]," +
                "\"bufferViews\":[{\"buffer\":0,\"byteLength\":-16}]}";
            using var h = new Harness(json, memoryLength: 64);
            Assert.AreEqual(
                BufferAccessStatus.DataIndexOutOfRange, h.Store.TryGetBufferView(h.View(0), out _));
        }

        [Test]
        public void BufferViewMissingBufferIsRejected()
        {
            const string json =
                "{\"asset\":{\"version\":\"2.0\"}," +
                "\"buffers\":[{\"byteLength\":64}]," +
                "\"bufferViews\":[{\"byteLength\":16},{\"buffer\":7,\"byteLength\":16}]}";
            using var h = new Harness(json, memoryLength: 64);
            Assert.AreEqual(
                BufferAccessStatus.ObjectIndexOutOfRange,
                h.Store.TryGetBufferView(h.View(0), out _),
                "A view without a buffer cannot be resolved.");
            Assert.AreEqual(
                BufferAccessStatus.ObjectIndexOutOfRange,
                h.Store.TryGetBufferView(h.View(1), out _),
                "Buffer 7 does not exist.");
        }

        [Test]
        public void BufferViewOnUnloadedBufferIsUnavailable()
        {
            // Buffer 1 has neither a URI nor a binary chunk, so its memory never materializes.
            const string json =
                "{\"asset\":{\"version\":\"2.0\"}," +
                "\"buffers\":[{\"byteLength\":64},{\"byteLength\":64}]," +
                "\"bufferViews\":[{\"buffer\":1,\"byteLength\":16}]}";
            using var h = new Harness(json, memoryLength: 64);
            Assert.AreEqual(
                BufferAccessStatus.BufferUnavailable, h.Store.TryGetBufferView(h.View(0), out _));
        }

        #endregion

        #region Tightly packed accessor data

        [Test]
        public void AccessorDataIsReinterpreted()
        {
            using var h = Simple();
            Assert.AreEqual(
                BufferAccessStatus.Success,
                h.Store.TryGetAccessorData<Float3>(h.View(0), 4, out var data));
            Assert.AreEqual(4, data.Length);
        }

        [Test]
        public void AccessorDataNegativeCountIsRejected()
        {
            using var h = Simple();
            Assert.AreEqual(
                BufferAccessStatus.DataIndexOutOfRange,
                h.Store.TryGetAccessorData<Float3>(h.View(0), -1, out _));
        }

        [Test]
        public void AccessorDataNegativeOffsetIsRejected()
        {
            using var h = Simple();
            Assert.AreEqual(
                BufferAccessStatus.DataIndexOutOfRange,
                h.Store.TryGetAccessorData<Float3>(h.View(0), 1, out _, -4));
        }

        /// <summary>
        /// count * sizeof(T) must not be allowed to wrap into a small or negative byte length.
        /// </summary>
        [Test]
        public void AccessorDataCountOverflowIsRejected()
        {
            using var h = Simple();
            Assert.AreEqual(
                BufferAccessStatus.DataIndexOutOfRange,
                h.Store.TryGetAccessorData<Float4>(h.View(0), int.MaxValue, out _));
            Assert.AreEqual(
                BufferAccessStatus.DataIndexOutOfRange,
                h.Store.TryGetAccessorData<Float3>(h.View(0), (int.MaxValue / 12) + 2, out _));
        }

        /// <summary>
        /// A request must stay inside the buffer view, not merely inside the buffer.
        /// </summary>
        [Test]
        public void AccessorDataBeyondViewIsRejected()
        {
            using var h = Simple();
            Assert.AreEqual(
                BufferAccessStatus.DataIndexOutOfRange,
                h.Store.TryGetAccessorData<Float3>(h.View(1), 4, out _),
                "48 bytes do not fit view 1's 16 bytes, even though the buffer holds 64.");
            Assert.AreEqual(
                BufferAccessStatus.DataIndexOutOfRange,
                h.Store.TryGetAccessorData<Float3>(h.View(1), 1, out _, 8));
        }

        [Test]
        public void AccessorDataZeroCountSucceeds()
        {
            using var h = Simple();
            Assert.AreEqual(
                BufferAccessStatus.Success,
                h.Store.TryGetAccessorData<Float3>(h.View(0), 0, out var data));
            Assert.AreEqual(0, data.Length);
        }

        #endregion

        #region Interleaved accessor data

        /// <summary>
        /// The last element of an interleaved accessor occupies its element size rather than a
        /// whole stride, so a spec conforming view may end before offset + count * byteStride.
        /// </summary>
        [Test]
        public void StridedAccessorLastElementNeedsNoStridePadding()
        {
            using var h = new Harness(k_InterleavedJson, memoryLength: 384);
            // 12 + 7 * 48 + 12 = 360 bytes are needed; 12 + 8 * 48 = 396 would over-report.
            Assert.AreEqual(
                BufferAccessStatus.Success,
                h.Store.TryGetStridedAccessorData<Float3>(h.View(0), 8, out var normals, 12));
            Assert.AreEqual(8, normals.Length);
            Assert.AreEqual(48, normals.ByteStride);

            // The tangents end exactly at the view's last byte: 32 + 7 * 48 + 16 = 384.
            Assert.AreEqual(
                BufferAccessStatus.Success,
                h.Store.TryGetStridedAccessorData<Float4>(h.View(0), 8, out var tangents, 32));
            Assert.AreEqual(8, tangents.Length);
        }

        [Test]
        public void StridedAccessorBeyondViewIsRejected()
        {
            using var h = new Harness(k_InterleavedJson, memoryLength: 384);
            Assert.AreEqual(
                BufferAccessStatus.DataIndexOutOfRange,
                h.Store.TryGetStridedAccessorData<Float4>(h.View(0), 8, out _, 36),
                "36 + 7 * 48 + 16 = 388 exceeds the 384 byte view.");
            Assert.AreEqual(
                BufferAccessStatus.DataIndexOutOfRange,
                h.Store.TryGetStridedAccessorData<Float3>(h.View(0), 9, out _, 12));
        }

        [Test]
        public void StridedAccessorDefaultsToTightPacking()
        {
            using var h = Simple();
            Assert.AreEqual(
                BufferAccessStatus.Success,
                h.Store.TryGetStridedAccessorData<Float3>(h.View(0), 5, out var data));
            Assert.AreEqual(5, data.Length);
            Assert.AreEqual(12, data.ByteStride, "A view without a stride is tightly packed.");
        }

        /// <summary>
        /// A stride below the element size would make consecutive elements overlap.
        /// </summary>
        [Test]
        public void StridedAccessorStrideBelowElementSizeIsRejected()
        {
            const string json =
                "{\"asset\":{\"version\":\"2.0\"}," +
                "\"buffers\":[{\"byteLength\":64}]," +
                "\"bufferViews\":[" +
                "{\"buffer\":0,\"byteLength\":64,\"byteStride\":8}," +
                "{\"buffer\":0,\"byteLength\":64,\"byteStride\":-48}" +
                "]}";
            using var h = new Harness(json, memoryLength: 64);
            Assert.AreEqual(
                BufferAccessStatus.DataIndexOutOfRange,
                h.Store.TryGetStridedAccessorData<Float3>(h.View(0), 2, out _),
                "A stride of 8 cannot hold a 12 byte element.");
            Assert.AreEqual(
                BufferAccessStatus.DataIndexOutOfRange,
                h.Store.TryGetStridedAccessorData<Float3>(h.View(1), 2, out _));
        }

        [Test]
        public void StridedAccessorNegativeCountIsRejected()
        {
            using var h = Simple();
            Assert.AreEqual(
                BufferAccessStatus.DataIndexOutOfRange,
                h.Store.TryGetStridedAccessorData<Float3>(h.View(0), -1, out _));
        }

        [Test]
        public void StridedAccessorCountOverflowIsRejected()
        {
            using var h = new Harness(k_InterleavedJson, memoryLength: 384);
            Assert.AreEqual(
                BufferAccessStatus.DataIndexOutOfRange,
                h.Store.TryGetStridedAccessorData<Float3>(h.View(0), int.MaxValue, out _, 12));
        }

        [Test]
        public void StridedAccessorZeroCountSucceeds()
        {
            using var h = Simple();
            Assert.AreEqual(
                BufferAccessStatus.Success,
                h.Store.TryGetStridedAccessorData<Float3>(h.View(0), 0, out var data));
            Assert.AreEqual(0, data.Length);
        }

        #endregion

        #region Index based overloads

        [Test]
        public void OutOfRangeBufferViewIndexIsReported()
        {
            using var h = Simple();
            foreach (var index in new[] { -1, 2, int.MaxValue, int.MinValue })
            {
                Assert.AreEqual(
                    BufferAccessStatus.ObjectIndexOutOfRange,
                    h.Store.TryGetBufferView(index, out _, out _),
                    $"Buffer view index {index}");
                Assert.AreEqual(
                    BufferAccessStatus.ObjectIndexOutOfRange,
                    h.Store.TryGetAccessorData<Float3>(index, 1, out _),
                    $"Buffer view index {index}");
                Assert.AreEqual(
                    BufferAccessStatus.ObjectIndexOutOfRange,
                    h.Store.TryGetStridedAccessorData<Float3>(index, 1, out _),
                    $"Buffer view index {index}");
            }
        }

        [Test]
        public void BufferViewByIndexReportsStride()
        {
            using var h = new Harness(k_InterleavedJson, memoryLength: 384);
            Assert.AreEqual(
                BufferAccessStatus.Success,
                h.Store.TryGetBufferView(0, out var data, out var byteStride));
            Assert.AreEqual(384, data.Length);
            Assert.AreEqual(48, byteStride);

            using var packed = Simple();
            Assert.AreEqual(
                BufferAccessStatus.Success,
                packed.Store.TryGetBufferView(0, out _, out var noStride));
            Assert.IsNull(noStride);
        }

        [Test]
        public unsafe void BufferViewPointerResolves()
        {
            using var h = Simple();
            Assert.IsTrue(h.Store.TryGetBufferViewPointer(1, 4, out var data, out var byteStride));
            Assert.IsTrue(data != null);
            Assert.IsNull(byteStride);
            // View 1 starts at byte 16, plus the requested 4 byte offset.
            Assert.AreEqual(20, ((byte*)data)[0]);
        }

        [Test]
        public unsafe void BufferViewPointerRejectsBadInput()
        {
            using var h = Simple();
            Assert.IsFalse(
                h.Store.TryGetBufferViewPointer(9, 0, out var data, out _), "No such buffer view.");
            Assert.IsTrue(data == null);
            Assert.IsFalse(
                h.Store.TryGetBufferViewPointer(1, -4, out data, out _), "Negative byte offset.");
            Assert.IsTrue(data == null);
            Assert.IsFalse(
                h.Store.TryGetBufferViewPointer(1, 128, out data, out _), "Byte offset past the view.");
            Assert.IsTrue(data == null);
        }

        [Test]
        public void GetBufferReturnsAssignedMemory()
        {
            using var h = Simple();
            Assert.IsTrue(h.Store.HasGlbBinChunk);
            var buffer = h.Store.GetBuffer(0);
            Assert.IsTrue(buffer.IsCreated);
            Assert.AreEqual(64, buffer.Length);
        }

        #endregion

        #region Lease entry points

        const string k_AccessorJson =
            "{\"asset\":{\"version\":\"2.0\"}," +
            "\"buffers\":[{\"byteLength\":64}]," +
            "\"bufferViews\":[{\"buffer\":0,\"byteLength\":64}]," +
            "\"accessors\":[" +
            "{\"bufferView\":0,\"componentType\":5126,\"count\":4,\"type\":\"VEC3\"}," +
            "{\"componentType\":5126,\"count\":4,\"type\":\"VEC3\"}," +
            "{\"bufferView\":0,\"componentType\":5126,\"count\":2,\"type\":\"VEC3\"," +
            "\"sparse\":{\"count\":1," +
            "\"indices\":{\"bufferView\":0,\"componentType\":5123}," +
            "\"values\":{\"bufferView\":0}}}" +
            "]}";

        [Test]
        public void ReadAccessorDataSucceeds()
        {
            using var h = new Harness(k_AccessorJson, memoryLength: 64);
            Assert.AreEqual(BufferAccessStatus.Success, h.Store.ReadAccessorData<Float3>(0, out var data));
            Assert.AreEqual(4, data.Length);
        }

        [Test]
        public void ReadStridedAccessorDataSucceeds()
        {
            using var h = new Harness(k_AccessorJson, memoryLength: 64);
            Assert.AreEqual(
                BufferAccessStatus.Success, h.Store.ReadStridedAccessorData<Float3>(0, out var data));
            Assert.AreEqual(4, data.Length);
        }

        [Test]
        public void ReadAccessorDataReportsMissingAccessor()
        {
            using var h = new Harness(k_AccessorJson, memoryLength: 64);
            Assert.AreEqual(
                BufferAccessStatus.ObjectIndexOutOfRange, h.Store.ReadAccessorData<Float3>(-1, out _));
            Assert.AreEqual(
                BufferAccessStatus.ObjectIndexOutOfRange,
                h.Store.ReadAccessorData<Float3>(int.MaxValue, out _));
            Assert.AreEqual(
                BufferAccessStatus.ObjectIndexOutOfRange,
                h.Store.ReadStridedAccessorData<Float3>(int.MaxValue, out _));
        }

        [Test]
        public void ReadAccessorDataReportsMissingBufferView()
        {
            using var h = new Harness(k_AccessorJson, memoryLength: 64);
            Assert.AreEqual(
                BufferAccessStatus.ObjectIndexOutOfRange,
                h.Store.ReadAccessorData<Float3>(1, out _),
                "Accessor 1 has no buffer view.");
        }

        [Test]
        public void ReadAccessorDataReportsTypeMismatch()
        {
            using var h = new Harness(k_AccessorJson, memoryLength: 64);
            Assert.AreEqual(BufferAccessStatus.TypeMismatch, h.Store.ReadAccessorData<Float4>(0, out _));
            Assert.AreEqual(
                BufferAccessStatus.TypeMismatch, h.Store.ReadStridedAccessorData<Float4>(0, out _));
        }

        [Test]
        public void ReadAccessorDataReportsSparse()
        {
            using var h = new Harness(k_AccessorJson, memoryLength: 64);
            Assert.AreEqual(
                BufferAccessStatus.SparseUnsupported, h.Store.ReadAccessorData<Float3>(2, out _));
            Assert.AreEqual(
                BufferAccessStatus.SparseUnsupported,
                h.Store.ReadStridedAccessorData<Float3>(2, out _));
        }

        [Test]
        public void ReadBufferViewSucceeds()
        {
            using var h = Simple();
            Assert.AreEqual(BufferAccessStatus.Success, h.Store.ReadBufferView(0, out var data, out _));
            Assert.AreEqual(64, data.Length);
        }

        [Test]
        public void ReadBufferViewReportsMissingBufferView()
        {
            using var h = Simple();
            Assert.AreEqual(
                BufferAccessStatus.ObjectIndexOutOfRange, h.Store.ReadBufferView(-1, out _, out _));
            Assert.AreEqual(
                BufferAccessStatus.ObjectIndexOutOfRange,
                h.Store.ReadBufferView(int.MaxValue, out _, out _));
        }

        /// <summary>
        /// An out of range request must not be flattened into <see cref="BufferAccessStatus.BufferUnavailable"/>,
        /// which would tell a caller the data is gone rather than that the document is broken.
        /// </summary>
        [Test]
        public void ReadBufferViewPropagatesSpecificStatus()
        {
            const string json =
                "{\"asset\":{\"version\":\"2.0\"}," +
                "\"buffers\":[{\"byteLength\":64}]," +
                "\"bufferViews\":[{\"buffer\":0,\"byteOffset\":32,\"byteLength\":64}]}";
            using var h = new Harness(json, memoryLength: 128, chunkStart: 0, chunkLength: 64);
            Assert.AreEqual(
                BufferAccessStatus.DataIndexOutOfRange, h.Store.ReadBufferView(0, out _, out _));
        }

        [Test]
        public void ReadsAfterDisposeAreUnavailable()
        {
            using var h = new Harness(k_AccessorJson, memoryLength: 64);
            h.Store.RequestDispose();
            Assert.AreEqual(BufferAccessStatus.BufferUnavailable, h.Store.ReadBufferView(0, out _, out _));
            Assert.AreEqual(
                BufferAccessStatus.BufferUnavailable, h.Store.ReadAccessorData<Float3>(0, out _));
            Assert.AreEqual(
                BufferAccessStatus.BufferUnavailable,
                h.Store.ReadStridedAccessorData<Float3>(0, out _));
        }

        #endregion

        #region Leases

        [Test]
        public void LeaseDefersMemoryRelease()
        {
            using var h = Simple();
            var lease = h.Store.AcquireLease();
            Assert.AreEqual(1, h.Store.LeaseCount);

            h.Store.RequestDispose();
            Assert.AreEqual(
                BufferAccessStatus.Success,
                h.Store.ReadBufferView(0, out _, out _),
                "An outstanding lease must keep the memory readable.");

            lease.Dispose();
            Assert.AreEqual(0, h.Store.LeaseCount);
            Assert.AreEqual(BufferAccessStatus.BufferUnavailable, h.Store.ReadBufferView(0, out _, out _));
        }

        [Test]
        public void LeaseCountTracksMultipleReaders()
        {
            using var h = Simple();
            var first = h.Store.AcquireLease();
            var second = h.Store.AcquireLease();
            Assert.AreEqual(2, h.Store.LeaseCount);

            first.Dispose();
            Assert.AreEqual(1, h.Store.LeaseCount);
            h.Store.RequestDispose();
            Assert.AreEqual(BufferAccessStatus.Success, h.Store.ReadBufferView(0, out _, out _));

            second.Dispose();
            Assert.AreEqual(BufferAccessStatus.BufferUnavailable, h.Store.ReadBufferView(0, out _, out _));
        }

        [Test]
        public void ReaderDisposeIsIdempotent()
        {
            using var h = Simple();
            var lease = h.Store.AcquireLease();
            lease.Dispose();
            Assert.DoesNotThrow(() => lease.Dispose());
            Assert.AreEqual(0, h.Store.LeaseCount);
        }

        [Test]
        public void ForceDisposeReportsOutstandingLeases()
        {
            using var h = Simple();
            var lease = h.Store.AcquireLease();
            h.Store.ForceDispose();

            Assert.IsTrue(Logged(h, LogCode.BufferDataForceDisposed));
            Assert.AreEqual(BufferAccessStatus.BufferUnavailable, h.Store.ReadBufferView(0, out _, out _));
            Assert.AreEqual(
                BufferAccessStatus.BufferUnavailable,
                lease.GetBufferView(0, out _, out _),
                "The revoked lease must report the memory as gone.");
            lease.Dispose();
        }

        [Test]
        public void DisposeWithoutLeasesReleasesImmediately()
        {
            using var h = Simple();
            ((IDisposable)h.Store).Dispose();
            Assert.AreEqual(BufferAccessStatus.BufferUnavailable, h.Store.ReadBufferView(0, out _, out _));
            Assert.IsFalse(Logged(h, LogCode.BufferDataForceDisposed));
        }

        #endregion

        #region Buffer loading

        [Test]
        public void DataUriBufferIsLoaded()
        {
            // Four bytes, base-64: 0xAD 0x06 0xF0 0x0D.
            const string json =
                "{\"asset\":{\"version\":\"2.0\"}," +
                "\"buffers\":[{\"byteLength\":4," +
                "\"uri\":\"data:application/octet-stream;base64,rQbwDQ==\"}]," +
                "\"bufferViews\":[{\"buffer\":0,\"byteLength\":4}]}";
            using var h = new Harness(json);
            Assert.AreEqual(BufferAccessStatus.Success, h.Store.TryGetBufferView(h.View(0), out var data));
            Assert.AreEqual(4, data.Length);
            Assert.AreEqual(0xAD, data[0]);
            Assert.AreEqual(0x0D, data[3]);
            Assert.IsTrue(Logged(h, LogCode.EmbedSlow));
        }

        [Test]
        public void DataUriWithUnexpectedMimeTypeFails()
        {
            const string json =
                "{\"asset\":{\"version\":\"2.0\"}," +
                "\"buffers\":[{\"byteLength\":4,\"uri\":\"data:text/plain;base64,rQbwDQ==\"}]," +
                "\"bufferViews\":[{\"buffer\":0,\"byteLength\":4}]}";
            using var h = new Harness(json);
            Assert.IsTrue(Logged(h, LogCode.BufferDataUriUnexpectedMimeType));
            Assert.AreEqual(
                BufferAccessStatus.BufferUnavailable, h.Store.TryGetBufferView(h.View(0), out _));
        }

        [Test]
        public void UndersizedDataUriBufferFails()
        {
            // The buffer declares 64 bytes but the payload only holds 4.
            const string json =
                "{\"asset\":{\"version\":\"2.0\"}," +
                "\"buffers\":[{\"byteLength\":64," +
                "\"uri\":\"data:application/octet-stream;base64,rQbwDQ==\"}]," +
                "\"bufferViews\":[{\"buffer\":0,\"byteLength\":4}]}";
            using var h = new Harness(json);
            Assert.IsTrue(Logged(h, LogCode.BufferContentUndersized));
            Assert.AreEqual(
                BufferAccessStatus.BufferUnavailable, h.Store.TryGetBufferView(h.View(0), out _));
        }

        [Test]
        public void FailedDataUriBufferFails()
        {
            // Missing the mime type / encoding delimiter, so the URI cannot be decoded.
            const string json =
                "{\"asset\":{\"version\":\"2.0\"}," +
                "\"buffers\":[{\"byteLength\":4,\"uri\":\"data:rQbwDQ==\"}]," +
                "\"bufferViews\":[{\"buffer\":0,\"byteLength\":4}]}";
            using var h = new Harness(json);
            Assert.IsTrue(Logged(h, LogCode.EmbedBufferLoadFailed));
        }

        [Test]
        public void StartBufferLoadsToleratesAbsentBuffers()
        {
            const string json = "{\"asset\":{\"version\":\"2.0\"}}";
            Assert.DoesNotThrow(() =>
            {
                using var h = new Harness(json);
            });
        }

        [UnityTest]
        public IEnumerator WaitForBufferDownloadsReportsFailure()
        {
            yield return AsyncWrapper.WaitForTask(WaitForBufferDownloadsReportsFailureAsync());
        }

        static async Task WaitForBufferDownloadsReportsFailureAsync()
        {
            const string ok =
                "{\"asset\":{\"version\":\"2.0\"}," +
                "\"buffers\":[{\"byteLength\":4," +
                "\"uri\":\"data:application/octet-stream;base64,rQbwDQ==\"}]," +
                "\"bufferViews\":[{\"buffer\":0,\"byteLength\":4}]}";
            using (var h = new Harness(ok))
            {
                Assert.IsTrue(await h.Store.WaitForBufferDownloads(CancellationToken.None));
            }

            const string broken =
                "{\"asset\":{\"version\":\"2.0\"}," +
                "\"buffers\":[{\"byteLength\":4,\"uri\":\"data:text/plain;base64,rQbwDQ==\"}]," +
                "\"bufferViews\":[{\"buffer\":0,\"byteLength\":4}]}";
            using (var h = new Harness(broken))
            {
                Assert.IsFalse(await h.Store.WaitForBufferDownloads(CancellationToken.None));
            }
        }

        /// <summary>
        /// A buffer that never got a load task must be reported as absent rather than throwing on
        /// a missing dictionary entry.
        /// </summary>
        [UnityTest]
        public IEnumerator BufferViewAsyncHandlesBufferWithoutLoadTask()
        {
            yield return AsyncWrapper.WaitForTask(BufferViewAsyncHandlesBufferWithoutLoadTaskAsync());
        }

        static async Task BufferViewAsyncHandlesBufferWithoutLoadTaskAsync()
        {
            // Buffer 0 loads from a data URI, buffer 1 has no URI at all, so the load task
            // dictionary exists but holds no entry for buffer 1.
            const string json =
                "{\"asset\":{\"version\":\"2.0\"}," +
                "\"buffers\":[" +
                "{\"byteLength\":4,\"uri\":\"data:application/octet-stream;base64,rQbwDQ==\"}," +
                "{\"byteLength\":4}]," +
                "\"bufferViews\":[" +
                "{\"buffer\":0,\"byteLength\":4}," +
                "{\"buffer\":1,\"byteLength\":4}]}";
            using var h = new Harness(json);

            var resolved = await h.Store.GetBufferViewAsync(h.View(0));
            Assert.AreEqual(4, resolved.Length);

            var absent = await h.Store.GetBufferViewAsync(h.View(1));
            Assert.AreEqual(0, absent.Length, "Buffer 1 has no memory and no load task.");
        }

        [UnityTest]
        public IEnumerator BufferViewAsyncReportsMissingBuffer()
        {
            yield return AsyncWrapper.WaitForTask(BufferViewAsyncReportsMissingBufferAsync());
        }

        static async Task BufferViewAsyncReportsMissingBufferAsync()
        {
            const string json =
                "{\"asset\":{\"version\":\"2.0\"}," +
                "\"buffers\":[{\"byteLength\":64}]," +
                "\"bufferViews\":[{\"buffer\":7,\"byteLength\":4}]}";
            using var h = new Harness(json, memoryLength: 64);
            var data = await h.Store.GetBufferViewAsync(h.View(0));
            Assert.AreEqual(0, data.Length);
        }

        #endregion
    }
}
