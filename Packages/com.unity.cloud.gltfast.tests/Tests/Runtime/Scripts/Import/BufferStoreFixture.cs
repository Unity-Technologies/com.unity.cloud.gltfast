// SPDX-FileCopyrightText: 2026 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using Unity.Cloud.Gltfast.Logging;
using Unity.Cloud.Gltfast.Objects;
using Unity.Collections;

namespace Unity.Cloud.Gltfast.Tests
{
    /// <summary>
    /// Builds a <see cref="BufferStore"/> over synthetic buffer memory, for tests that need real
    /// accessor data without loading a glTF asset.
    /// </summary>
    /// <remarks>
    /// Memory is supplied through the glTF-binary buffer chunk path, which needs neither a download
    /// provider nor data URIs. The fixture owns the backing array; the store only views it.
    /// </remarks>
    sealed class BufferStoreFixture : IDisposable
    {
        NativeArray<byte> m_Memory;

        public BufferStore Store { get; }

        /// <summary>
        /// Creates a store over <paramref name="root"/>. When <paramref name="bufferBytes"/> is
        /// null, no memory is attached, which is enough for paths that fail before reading any.
        /// </summary>
        public BufferStoreFixture(Root root, byte[] bufferBytes = null, ICodeLogger logger = null)
        {
            Store = new BufferStore(new ImportContext(null, logger, null), _ => { }, null);
            Store.Initialize(root, null);

            if (bufferBytes == null)
            {
                return;
            }

            Store.StartBufferLoads(default);
            m_Memory = new NativeArray<byte>(bufferBytes, Allocator.Persistent);
            Store.SetGlbBinChunk(new GlbBinChunk(0, (uint)m_Memory.Length));
            Store.AssignGlbBinChunk(m_Memory.AsReadOnly());
        }

        public void Dispose()
        {
            Store.ForceDispose();
            if (m_Memory.IsCreated)
            {
                m_Memory.Dispose();
            }
        }
    }
}
