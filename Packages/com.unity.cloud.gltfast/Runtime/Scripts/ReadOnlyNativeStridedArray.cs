// SPDX-FileCopyrightText: 2025 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;

namespace GLTFast
{
    [NativeContainer]
    [NativeContainerIsReadOnly]
    [DebuggerDisplay("Length = {m_Count}")]
    unsafe struct ReadOnlyNativeStridedArray<T> where T : unmanaged
    {
        [NativeDisableUnsafePtrRestriction]
        void* m_Buffer;
        readonly int m_Count;
        readonly int m_ByteStride;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        AtomicSafetyHandle m_Safety;
#endif

        internal ReadOnlyNativeStridedArray(void* buffer, int byteLength, int offset, int count, int byteStride
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            , ref AtomicSafetyHandle safety
#endif
            )
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset), "offset must be >= 0");
            if (offset + (count - 1) * byteStride + sizeof(T) > byteLength)
                throw new ArgumentOutOfRangeException(nameof(count), $"accessor range is outside the range of the native array 0-{(object)(byteLength - 1)}");
#endif
            m_Buffer = (byte*)buffer + offset;
            m_Count = count;
            m_ByteStride = byteStride;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            m_Safety = safety;
#endif
        }

        public T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                CheckReadIndex(index);
                return UnsafeUtility.ReadArrayElementWithStride<T>(m_Buffer, index, m_ByteStride);
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void CheckReadIndex(int index)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
            if (index < 0 || index >= m_Count)
                throw new IndexOutOfRangeException($"Index {index} is out of range (must be between 0 and {m_Count - 1}).");
        }
    }
}
