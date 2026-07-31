// SPDX-FileCopyrightText: 2025 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Cloud.Gltfast
{
    /// <summary>
    /// Wraps a managed array and provides a <see cref="ReadOnlyNativeArray{T}"/> for accessing it.
    /// </summary>
    sealed class ReadOnlyNativeArrayFromManagedArray<T> : IDisposable
        where T : unmanaged
    {
        public ReadOnlyNativeArray<T> Array { get; }

        GCHandle m_BufferHandle;
        readonly bool m_Pinned;

        public unsafe ReadOnlyNativeArrayFromManagedArray(T[] original)
        {
            if (original == null)
                throw new ArgumentNullException(nameof(original));

            m_BufferHandle = GCHandle.Alloc(original, GCHandleType.Pinned);
            fixed (void* bufferAddress = &original[0])
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                var safety = AtomicSafetyHandle.Create();
                Array = new ReadOnlyNativeArray<T>(bufferAddress, original.Length, ref safety);
#else
                Array = new ReadOnlyNativeArray<T>(bufferAddress, original.Length);
#endif
            }

            m_Pinned = true;
        }

        /// <summary>
        /// Disposes the managed <see cref="ReadOnlyNativeArray{T}" />.
        /// </summary>
        public void Dispose()
        {
            if (m_Pinned)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
#endif
                m_BufferHandle.Free();
            }
        }
    }
}
