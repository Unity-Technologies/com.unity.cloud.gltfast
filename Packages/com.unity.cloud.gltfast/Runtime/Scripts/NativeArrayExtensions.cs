// SPDX-FileCopyrightText: 2025 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System.IO;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Assertions;

namespace GLTFast
{
    static class NativeArrayExtensions
    {
        internal static unsafe UnmanagedMemoryStream ToUnmanagedMemoryStream(this NativeArray<byte> data)
        {
            return new UnmanagedMemoryStream(
                (byte*)data.GetUnsafePtr(),
                data.Length,
                data.Length,
                FileAccess.Write
            );
        }

        internal static unsafe UnmanagedMemoryStream ToUnmanagedMemoryStream(this NativeArray<byte>.ReadOnly data, uint start, uint count)
        {
            Assert.IsTrue(start + count <= data.Length);
            return new UnmanagedMemoryStream(
                (byte*)data.GetUnsafeReadOnlyPtr() + start,
                count,
                count,
                FileAccess.Read
            );
        }

        internal static unsafe uint ReadUInt32(this NativeArray<byte>.ReadOnly data, long offset)
        {
            var ptr = (uint*)((byte*)data.GetUnsafeReadOnlyPtr() + offset);
            return *ptr;
        }

        internal static unsafe ulong ReadUInt64(this NativeArray<byte>.ReadOnly data, long offset)
        {
            var ptr = (ulong*)((byte*)data.GetUnsafeReadOnlyPtr() + offset);
            return *ptr;
        }
    }
}
