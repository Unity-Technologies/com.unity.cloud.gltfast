// SPDX-FileCopyrightText: 2026 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

#if MESHOPT_IS_RECENT
#define MESHOPT_IS_ENABLED
#endif

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Unity.Cloud.Gltfast.Logging;
using Unity.Cloud.Gltfast.Objects;
#if MESHOPT_IS_ENABLED
using Meshoptimizer;
#endif
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine.Assertions;
using UnityEngine.Profiling;
using Buffer = Unity.Cloud.Gltfast.Objects.Buffer;

namespace Unity.Cloud.Gltfast
{
    /// <summary>
    /// Owns a glTF asset's raw buffer memory and resolves buffer views and accessors into it.
    /// </summary>
    /// <remarks>
    /// Buffer memory is not owned directly: <see cref="m_Buffers"/> holds non-owning views. The
    /// objects that own the memory (downloads, pinned managed arrays, <see cref="UriValue"/>s) are
    /// handed to the tracking callback and disposed by the owner of this store.
    /// </remarks>
    class BufferStore : IDisposable
    {
        readonly ImportContext m_Context;
        readonly Action<IDisposable> m_TrackDisposable;

        Root m_Root;
        Uri m_BaseUri;

        ReadOnlyNativeArray<byte>[] m_Buffers;
        GlbBinChunk[] m_BinChunks;
        Dictionary<int, Task<bool>> m_BufferLoadTasks;

        /// optional glTF-binary buffer
        /// https://github.com/KhronosGroup/glTF/tree/master/specification/2.0#binary-buffer
        GlbBinChunk? m_GlbBinChunk;

#if MESHOPT_IS_ENABLED
        Dictionary<int, NativeArray<byte>> m_MeshoptBufferViews;
        NativeArray<int> m_MeshoptReturnValues;
        JobHandle m_MeshoptJobHandle;
#endif

        ICodeLogger Logger => m_Context.Logger;

        public BufferStore(ImportContext context, Action<IDisposable> trackDisposable)
        {
            m_Context = context;
            m_TrackDisposable = trackDisposable;
        }

        /// <summary>
        /// Supplies the de-serialized document. Not a constructor parameter because the root is
        /// assigned during JSON parsing, which also starts the buffer loads.
        /// </summary>
        public void Initialize(Root root, Uri baseUri)
        {
            m_Root = root;
            m_BaseUri = baseUri;
        }

        public bool HasGlbBinChunk => m_GlbBinChunk.HasValue;

        public void SetGlbBinChunk(GlbBinChunk chunk)
        {
            Assert.IsFalse(m_GlbBinChunk.HasValue); // There can only be one binary chunk
            m_GlbBinChunk = chunk;
        }

        /// <summary>
        /// Points buffer 0 at the glTF-binary buffer chunk within the document's own memory.
        /// </summary>
        public void AssignGlbBinChunk(NativeArray<byte>.ReadOnly bytes)
        {
            if (!m_GlbBinChunk.HasValue || m_BinChunks == null)
            {
                return;
            }
            m_BinChunks[0] = m_GlbBinChunk.Value;
            var wrapper = new ReadOnlyNativeArrayFromNativeArray<byte>(bytes);
            m_Buffers[0] = wrapper.Array;
        }

        /// <summary>
        /// Allocates buffer storage and starts loading every buffer that has a URI.
        /// </summary>
        public void StartBufferLoads(CancellationToken cancellationToken)
        {
            if (m_Root.Buffers == null)
            {
                return;
            }

            var bufferCount = m_Root.Buffers.Count;
            if (bufferCount > 0)
            {
                m_Buffers = new ReadOnlyNativeArray<byte>[bufferCount];
                m_BinChunks = new GlbBinChunk[bufferCount];
            }

            for (var i = 0; i < bufferCount; i++)
            {
                cancellationToken.ThrowIfCancellationRequestedWithTracking();

                var buffer = m_Root.Buffers[i];
                if (buffer.Uri != null)
                {
                    m_BufferLoadTasks ??= new Dictionary<int, Task<bool>>();
                    if (buffer.Uri.IsData || buffer.Uri.IsFailed)
                    {
                        Logger?.Warning(LogCode.EmbedSlow);
                        m_BufferLoadTasks[i] = LoadBufferFromDataUri(i, buffer, cancellationToken);
                    }
                    else
                    {
                        m_BufferLoadTasks[i] = LoadBufferFromUriAsync(
                            i, UriHelper.GetUriString(buffer.Uri.AsString(), m_BaseUri));
                    }
                }
            }
        }

        public async Task<bool> WaitForBufferDownloads(CancellationToken cancellationToken)
        {
            if (m_BufferLoadTasks != null)
            {
                foreach (var loadTaskPair in m_BufferLoadTasks)
                {
                    cancellationToken.ThrowIfCancellationRequestedWithTracking();
                    if (!await loadTaskPair.Value)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

#pragma warning disable CS1998 // async method lacking 'await' is intentional: this stays a Task to match LoadBufferFromUriAsync.
        async Task<bool> LoadBufferFromDataUri(int bufferIndex, Buffer buffer, CancellationToken cancellationToken)
#pragma warning restore CS1998
        {
            cancellationToken.ThrowIfCancellationRequestedWithTracking();

            if (buffer.Uri.IsFailed)
            {
                Logger?.Error(LogCode.EmbedBufferLoadFailed);
                return false;
            }

            var mimeType = buffer.Uri.MimeType;
            if (!mimeType.StartsWith("application/", StringComparison.Ordinal)
                || !(
                    mimeType.AsSpan(12).SequenceEqual("octet-stream")
                    || mimeType.AsSpan(12).SequenceEqual("gltf-buffer")
                    )
                )
            {
                Logger?.Error(
                    LogCode.BufferDataUriUnexpectedMimeType,
                    bufferIndex.ToString(),
                    mimeType
                    );
                return false;
            }

            if (!buffer.Uri.TryGetData(out var data))
            {
                Logger?.Error(LogCode.EmbedBufferLoadFailed);
                return false;
            }

            if (data.Length < buffer.ByteLength)
            {
                Logger?.Error(
                    LogCode.BufferContentUndersized,
                    bufferIndex.ToString(),
                    buffer.ByteLength.ToString(),
                    data.Length.ToString()
                    );
                return false;
            }

            // The UriValue (tracked by the owner, which drained the converter's pending list)
            // retains ownership of the NativeArray.
            m_Buffers[bufferIndex] = new ReadOnlyNativeArray<byte>(data);
            if (bufferIndex != 0 || !m_GlbBinChunk.HasValue)
            {
                m_BinChunks[bufferIndex] = new GlbBinChunk(0, (uint)m_Buffers[bufferIndex].Length);
            }
            return true;
        }

        async Task<bool> LoadBufferFromUriAsync(int index, Uri uri)
        {
            var request = m_Context.DownloadProvider.RequestAsync(uri);
            var download = await request;
            if (download.Success)
            {
                Profiler.BeginSample("GetData");

                var wrapper = new ReadOnlyNativeArrayFromNativeArray<byte>(download.Data);
                m_Buffers[index] = wrapper.Array;

                m_TrackDisposable(download);

                Profiler.EndSample();

                if (index != 0 || !m_GlbBinChunk.HasValue)
                {
                    m_BinChunks[index] = new GlbBinChunk(0, (uint)m_Buffers[index].Length);
                }

                return true;
            }

            Logger?.Error(LogCode.BufferLoadFailed, download.Error, index.ToString());
            return false;
        }

        public ReadOnlyNativeArray<byte> GetBuffer(int index)
        {
            return m_Buffers[index];
        }

        public ReadOnlyNativeArray<byte> GetBufferView(
            int bufferViewIndex,
            out int? byteStride,
            int offset = 0,
            int length = 0
            )
        {
            var bufferView = m_Root.BufferViews[bufferViewIndex];
#if MESHOPT_IS_ENABLED
            if (bufferView.Extensions?.ExtMeshoptCompression != null)
            {
                byteStride = bufferView.Extensions.ExtMeshoptCompression.ByteStride;
                var entireBuffer = m_MeshoptBufferViews[bufferViewIndex];
                if (offset == 0 && length <= 0)
                {
                    return new ReadOnlyNativeArray<byte>(entireBuffer);
                }
                Assert.IsTrue(offset >= 0);
                if (length <= 0)
                {
                    length = entireBuffer.Length - offset;
                }
                Assert.IsTrue(offset + length <= entireBuffer.Length);
                return new ReadOnlyNativeArray<byte>(entireBuffer.GetSubArray(offset, length));
            }
#endif
            byteStride = bufferView.ByteStride;
            return GetBufferView(bufferView, offset, length);
        }

        public ReadOnlyNativeArray<T> GetAccessorData<T>(
            int bufferViewIndex,
            int count,
            int offset = 0
            ) where T : unmanaged
        {
            var bufferView = m_Root.BufferViews[bufferViewIndex];
#if MESHOPT_IS_ENABLED
            if (bufferView.Extensions?.ExtMeshoptCompression != null)
            {
                var fullSlice = m_MeshoptBufferViews[bufferViewIndex];
                if (offset == 0 && (count <= 0 || count * UnsafeUtility.SizeOf(typeof(T)) == fullSlice.Length))
                {
                    return new ReadOnlyNativeArray<byte>(fullSlice).Reinterpret<T>();
                }
                Assert.IsTrue(offset >= 0);
                Assert.IsTrue(count > 0);
                Assert.IsTrue(offset + count * UnsafeUtility.SizeOf(typeof(T)) <= fullSlice.Length);
                return new ReadOnlyNativeArray<byte>(fullSlice).GetSubArray(offset, count).Reinterpret<T>();
            }
#endif
            return GetAccessorData<T>(bufferView, count, offset);
        }

        public ReadOnlyNativeStridedArray<T> GetStridedAccessorData<T>(
            int bufferViewIndex,
            int count,
            int offset = 0
            ) where T : unmanaged
        {
            var bufferView = m_Root.BufferViews[bufferViewIndex];
#if MESHOPT_IS_ENABLED
            if (bufferView.Extensions?.ExtMeshoptCompression != null)
            {
                unsafe
                {
                    var fullSlice = m_MeshoptBufferViews[bufferViewIndex];
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    var safety = NativeArrayUnsafeUtility.GetAtomicSafetyHandle(fullSlice);
#endif
                    return new ReadOnlyNativeStridedArray<T>(
                        fullSlice.GetUnsafeReadOnlyPtr(),
                        fullSlice.Length,
                        offset,
                        count,
                        bufferView.ByteStride ?? 0
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                        , ref safety
#endif
                        );
                }
            }
#endif
            return GetStridedAccessorData<T>(bufferView, count, offset);
        }

        /// <summary>
        /// Resolves a buffer view (meshopt-aware) to a pointer into buffer memory.
        /// </summary>
        public unsafe bool TryGetBufferViewPointer(
            int bufferViewIndex,
            int byteOffset,
            out void* data,
            out int? byteStride
            )
        {
            var bufferView = m_Root.BufferViews[bufferViewIndex];
#if MESHOPT_IS_ENABLED
            var meshopt = bufferView.Extensions?.ExtMeshoptCompression;
            if (meshopt != null)
            {
                byteStride = meshopt.ByteStride;
                data = (byte*)m_MeshoptBufferViews[bufferViewIndex].GetUnsafeReadOnlyPtr() + byteOffset;
                return true;
            }
#endif
            byteStride = bufferView.ByteStride;
            if (!GltfIndex.TryGetIndex(bufferView.Buffer, m_Buffers?.Length ?? 0, out var bufferIndex))
            {
                data = null;
                return false;
            }
            data = (byte*)m_Buffers[bufferIndex].GetUnsafeReadOnlyPtr()
                + (byteOffset + bufferView.ByteOffset + m_BinChunks[bufferIndex].Start);
            return true;
        }

        public ReadOnlyNativeArray<T> GetAccessorData<T>(
            IBufferView bufferView,
            int count,
            int offset = 0
        ) where T : unmanaged
        {
            Assert.IsTrue(offset >= 0);
            if (!GltfIndex.TryGetIndex(bufferView.Buffer, m_Buffers?.Length ?? 0, out var bufferIndex))
            {
                return default;
            }
            Assert.IsTrue(m_Buffers[bufferIndex].IsCreated);
            var chunk = m_BinChunks[bufferIndex];
            var totalOffset = chunk.Start + bufferView.ByteOffset + offset;
            Assert.IsTrue(bufferView.ByteOffset + offset <= chunk.Length);
            return m_Buffers[bufferIndex].GetSubArray(totalOffset, count * UnsafeUtility.SizeOf<T>()).Reinterpret<T>();
        }

        public ReadOnlyNativeStridedArray<T> GetStridedAccessorData<T>(
            IBufferView bufferView,
            int count,
            int offset = 0
        ) where T : unmanaged
        {
            Assert.IsTrue(offset >= 0);
            if (!GltfIndex.TryGetIndex(bufferView.Buffer, m_Buffers?.Length ?? 0, out var bufferIndex))
            {
                return default;
            }
            Assert.IsTrue(m_Buffers[bufferIndex].IsCreated);
            var chunk = m_BinChunks[bufferIndex];
            var totalOffset = chunk.Start + bufferView.ByteOffset + offset;
            Assert.IsTrue(bufferView.ByteOffset + offset <= chunk.Length);
            var byteStride = bufferView.ByteStride ?? UnsafeUtility.SizeOf(typeof(T));
            return m_Buffers[bufferIndex].ToStrided<T>(totalOffset, count, byteStride);
        }

        public async Task<NativeArray<byte>.ReadOnly> GetBufferViewAsync(
            IBufferView bufferView,
            int offset = 0,
            int length = 0
            )
        {
            if (!GltfIndex.TryGetIndex(bufferView.Buffer, m_Buffers?.Length ?? 0, out var bufferIndex))
            {
                return default;
            }
            if (!m_Buffers[bufferIndex].IsCreated)
            {
                var download = m_BufferLoadTasks?[bufferIndex];
                if (download != null)
                {
                    return await download
                        ? GetBufferView(bufferView, offset, length).AsNativeArrayReadOnly()
                        : default;
                }
            }

            return GetBufferView(bufferView, offset, length).AsNativeArrayReadOnly();
        }

        public ReadOnlyNativeArray<byte> GetBufferView(
            IBufferView bufferView,
            int offset = 0,
            int length = 0
            )
        {
            Assert.IsTrue(offset >= 0);
            if (length <= 0)
            {
                length = bufferView.ByteLength - offset;
            }
            Assert.IsTrue(offset + length <= bufferView.ByteLength);

            if (!GltfIndex.TryGetIndex(bufferView.Buffer, m_Buffers?.Length ?? 0, out var bufferIndex))
            {
                return default;
            }
            Assert.IsTrue(m_Buffers[bufferIndex].IsCreated);

            var chunk = m_BinChunks[bufferIndex];
            var nativeBuffer = m_Buffers[bufferIndex];
            var totalOffset = chunk.Start + bufferView.ByteOffset + offset;
            Assert.IsTrue(bufferView.ByteOffset + offset <= chunk.Length);
            Assert.IsTrue(totalOffset + length <= nativeBuffer.Length);
            return m_Buffers[bufferIndex].GetSubArray(totalOffset, length);
        }

#if MESHOPT_IS_ENABLED
        public void MeshoptDecode()
        {
            if (m_Root.BufferViews != null)
            {
                List<JobHandle> jobHandlesList = null;
                for (var i = 0; i < m_Root.BufferViews.Count; i++)
                {
                    var bufferView = m_Root.BufferViews[i];
                    if (bufferView.Extensions?.ExtMeshoptCompression != null)
                    {
                        var meshopt = bufferView.Extensions.ExtMeshoptCompression;
                        if (jobHandlesList == null)
                        {
                            m_MeshoptBufferViews = new Dictionary<int, NativeArray<byte>>();
                            jobHandlesList = new List<JobHandle>(m_Root.BufferViews.Count);
                            m_MeshoptReturnValues = new NativeArray<int>(m_Root.BufferViews.Count, Allocator.TempJob);
                        }

                        if (!meshopt.ByteStride.HasValue)
                        {
                            Logger?.Error(LogCode.TypeUnsupported, "Meshopt", "Missing byteStride");
                            continue;
                        }

                        var byteStride = meshopt.ByteStride.Value;

                        var arr = new NativeArray<byte>(meshopt.Count * byteStride, Allocator.Persistent);

                        var origBufferView = GetBufferView(meshopt);

                        var jobHandle = Decode.DecodeGltfBuffer(
                            m_MeshoptReturnValues.GetSubArray(i, 1),
                            arr,
                            meshopt.Count,
                            byteStride,
                            origBufferView.AsNativeArrayReadOnly(),
                            meshopt.Mode.ToMeshoptimizerMode(),
                            meshopt.Filter.ToMeshoptimizerFilter()
                        );
                        jobHandlesList.Add(jobHandle);
                        m_MeshoptBufferViews[i] = arr;
                    }
                }

                if (jobHandlesList != null)
                {
                    using (var jobHandles = new NativeArray<JobHandle>(jobHandlesList.ToArray(), Allocator.Temp))
                    {
                        m_MeshoptJobHandle = JobHandle.CombineDependencies(jobHandles);
                    }
                }
            }
        }

        public async Task<bool> WaitForMeshoptDecode()
        {
            var success = true;
            if (m_MeshoptBufferViews != null)
            {
                while (!m_MeshoptJobHandle.IsCompleted)
                {
                    await Task.Yield();
                }
                m_MeshoptJobHandle.Complete();

                foreach (var returnValue in m_MeshoptReturnValues)
                {
                    success &= returnValue == 0;
                }
                m_MeshoptReturnValues.Dispose();
            }
            return success;
        }
#endif // MESHOPT_IS_ENABLED

        /// <summary>
        /// Drops all buffer storage. The memory itself is released by whoever disposes the tracked
        /// disposables, so this has to run before those are disposed.
        /// </summary>
        public void Dispose()
        {
            m_Buffers = null;
            m_BinChunks = null;
            m_BufferLoadTasks = null;
            m_GlbBinChunk = null;

#if MESHOPT_IS_ENABLED
            if (m_MeshoptBufferViews != null)
            {
                foreach (var nativeBuffer in m_MeshoptBufferViews.Values)
                {
                    nativeBuffer.Dispose();
                }
                m_MeshoptBufferViews = null;
            }
            if (m_MeshoptReturnValues.IsCreated)
            {
                m_MeshoptReturnValues.Dispose();
            }
#endif
        }
    }
}
