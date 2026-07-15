// SPDX-FileCopyrightText: 2026 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using GLTFast.Export;
using NUnit.Framework;
using ManagedNativeArray = GLTFast.Export.ManagedNativeArray<byte, byte>;

namespace GLTFast.Tests.Export
{
    [Category("Export")]
    class ManagedNativeArrayTests
    {
        [Test]
        public void Dispose_ReleasesPinnedHandle()
        {
            var data = new byte[] { 1, 2, 3, 4 };
            var instance = new ManagedNativeArray(data);

            Assert.IsTrue(instance.IsBufferHandleAllocated, "Pinned handle should be allocated before Dispose.");
            Assert.DoesNotThrow(() => instance.Dispose());
            Assert.IsFalse(instance.IsBufferHandleAllocated, "Pinned handle should be released after Dispose.");
        }

        [Test]
        public void Dispose_SuppressesFinalizer()
        {
            var data = new byte[] { 1, 2, 3, 4 };
            var instance = new ManagedNativeArray(data);

            instance.Dispose();

            // Forcing GC after an explicit Dispose must not trigger a second Free (a finalizer-thread
            // InvalidOperationException) — the finalizer is suppressed and the handle stays released.
            Assert.DoesNotThrow(() =>
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            });
            Assert.IsFalse(instance.IsBufferHandleAllocated);
        }

        [Test]
        public void DoubleDispose_IsNoOp()
        {
            var data = new byte[] { 1, 2, 3, 4 };
            var instance = new ManagedNativeArray(data);

            instance.Dispose();
            Assert.DoesNotThrow(() => instance.Dispose());
            Assert.IsFalse(instance.IsBufferHandleAllocated);
        }

        [Test]
        public void NullOriginal_DisposeIsNoOp()
        {
            var instance = new ManagedNativeArray(null);

            Assert.IsFalse(instance.IsBufferHandleAllocated);
            Assert.DoesNotThrow(() => instance.Dispose());
        }

        [Test]
        public void EmptyArray_ConstructAndDisposeIsSafe()
        {
            // A non-null but EMPTY array throws IndexOutOfRangeException at the `fixed (&original[0])`
            // pin site before any GCHandle is allocated, so a failed construction leaks no pinned
            // handle and disposing the failed instance is a safe no-op.
            var data = Array.Empty<byte>();
            ManagedNativeArray instance = null;
            Assert.Throws<IndexOutOfRangeException>(() => instance = new ManagedNativeArray(data));

            Assert.IsNull(instance, "Construction with an empty array is expected to throw before assignment.");
        }
    }
}
