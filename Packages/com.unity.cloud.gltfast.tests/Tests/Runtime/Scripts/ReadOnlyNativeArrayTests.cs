// SPDX-FileCopyrightText: 2025 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections;
using NUnit.Framework;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Cloud.Gltfast.Tests
{
    static class ReadOnlyNativeArrayTests
    {
        struct TestJob : IJob
        {
            public NativeArray<byte> data;

            public void Execute()
            {
                data[0] = 42;
            }
        }

        [Test]
        public static void Access()
        {
            var array = new NativeArray<byte>(new byte[] { 1, 2, 3, 42 }, Allocator.TempJob);
            var buffer = new ReadOnlyNativeArray<byte>(array);
            Assert.AreEqual(1, buffer[0]);
            Assert.AreEqual(2, buffer[1]);
            Assert.AreEqual(3, buffer[2]);
            Assert.AreEqual(42, buffer[3]);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            Assert.Throws<IndexOutOfRangeException>(() => Debug.Log(buffer[-1]));
            Assert.Throws<IndexOutOfRangeException>(() => Debug.Log(buffer[4]));
#endif
            array.Dispose();
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            Assert.Throws<ObjectDisposedException>(() => Debug.Log(buffer[0]));
#endif
        }

        [UnityTest]
        public static IEnumerator ConcurrencyConflict()
        {
            using var array = new NativeArray<byte>(new byte[] { 1, 2, 3, 42 }, Allocator.TempJob);
            var buffer = new ReadOnlyNativeArray<byte>(array);
            var job = new TestJob { data = array };
            var handle = job.Schedule();
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            Assert.Throws<InvalidOperationException>(() => Debug.Log(buffer[0]));
#endif
            while (!handle.IsCompleted)
            {
                yield return null;
            }
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            Assert.Throws<InvalidOperationException>(() => Debug.Log(buffer[0]));
#endif
            handle.Complete();
            Assert.AreEqual(42, buffer[0]);
        }

        [Test]
        public static void CheckGetSubArrayArguments()
        {
            using var array = new NativeArray<byte>(new byte[] { 1, 2, 3, 42 }, Allocator.Temp);
            var buffer = new ReadOnlyNativeArray<byte>(array);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            Assert.Throws<ArgumentOutOfRangeException>(() => buffer.GetSubArray(-1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => buffer.GetSubArray(1, 100));
            Assert.Throws<ArgumentException>(() => buffer.GetSubArray(int.MaxValue, 3));
#endif
        }

        [Test]
        public static void CheckReinterpretSize()
        {
            using var array = new NativeArray<byte>(new byte[] { 1, 2, 3, 4, 5 }, Allocator.Temp);
            var buffer = new ReadOnlyNativeArray<byte>(array);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            Assert.Throws<InvalidOperationException>(() => buffer.Reinterpret<int>());
#endif
        }

        [Test]
        public static void CopyTo()
        {
            using var array = new NativeArray<byte>(new byte[] { 1, 2, 3, 4, 5 }, Allocator.Temp);
            var buffer = new ReadOnlyNativeArray<byte>(array);

            {
                using var destination = new NativeArray<byte>(buffer.Length, Allocator.Temp);
                buffer.CopyTo(destination);
                Assert.AreEqual(1, destination[0]);
                Assert.AreEqual(2, destination[1]);
                Assert.AreEqual(3, destination[2]);
                Assert.AreEqual(4, destination[3]);
                Assert.AreEqual(5, destination[4]);
            }

            {
                using var destination = new NativeArray<byte>(6, Allocator.Temp);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                Assert.Throws<ArgumentException>(() => buffer.CopyTo(destination));
#endif
            }
        }

        [Test]
        public static void ManagedInvalid()
        {
            ReadOnlyNativeArrayFromManagedArray<byte> nativeArrayFrom;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            Assert.Throws<ArgumentNullException>(() => nativeArrayFrom = new ReadOnlyNativeArrayFromManagedArray<byte>(null));
#endif
        }

        [Test]
        public static void ReadOnlyNativeStridedArray()
        {
            var array = new NativeArray<byte>(new byte[] { 1, 2, 3, 4, 5, 6 }, Allocator.TempJob);
            var buffer = new ReadOnlyNativeArray<byte>(array);
            var bufferStrided = buffer.ToStrided<byte>(1, 3, 2);
            Assert.AreEqual(2, bufferStrided[0]);
            Assert.AreEqual(4, bufferStrided[1]);
            Assert.AreEqual(6, bufferStrided[2]);

            array.Dispose();
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            Assert.Throws<ObjectDisposedException>(() => Debug.Log(buffer[0]));
            Assert.Throws<ObjectDisposedException>(() => Debug.Log(bufferStrided[0]));
#endif
        }

        [Test]
        public static void ReadOnlyNativeStridedArrayOddStride()
        {
            using var array = new NativeArray<byte>(new byte[]
            {
                42, 1, 0, 0, 0,
                42, 2, 0, 0, 0,
                42, 164, 1, 0, 0,
                42, 0xBE, 0xEF, 0xFA, 0xDE,
            }, Allocator.TempJob);
            var buffer = new ReadOnlyNativeArray<byte>(array).ToStrided<uint>(1, 4, 5);
            Assert.AreEqual(1, buffer[0]);
            Assert.AreEqual(2, buffer[1]);
            Assert.AreEqual(420, buffer[2]);
            Assert.AreEqual(3740987326, buffer[3]);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            Assert.Throws<IndexOutOfRangeException>(() => Debug.Log(buffer[-1]));
            Assert.Throws<IndexOutOfRangeException>(() => Debug.Log(buffer[4]));
#endif
        }
    }
}
