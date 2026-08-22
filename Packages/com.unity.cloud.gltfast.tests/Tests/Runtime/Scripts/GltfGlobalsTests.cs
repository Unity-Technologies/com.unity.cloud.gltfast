// SPDX-FileCopyrightText: 2026 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using NUnit.Framework;
using Unity.Collections;

namespace GLTFast.Tests
{
    class GltfGlobalsTests
    {
        [Test]
        public void IsGltfBinaryEmpty()
        {
            using var data = new NativeArray<byte>(0, Allocator.Temp);
            Assert.IsFalse(GltfGlobals.IsGltfBinary(data.AsReadOnly()));
        }

        [Test]
        public void IsGltfBinaryUncreated()
        {
            Assert.IsFalse(GltfGlobals.IsGltfBinary(default(NativeArray<byte>.ReadOnly)));
        }

        [Test]
        public void IsGltfBinaryTooShort()
        {
            using var data = new NativeArray<byte>(new byte[] { 0x67, 0x6c, 0x54 }, Allocator.Temp);
            Assert.IsFalse(GltfGlobals.IsGltfBinary(data.AsReadOnly()));
        }

        [Test]
        public void IsGltfBinaryMagic()
        {
            using var data = new NativeArray<byte>(new byte[] { 0x67, 0x6c, 0x54, 0x46 }, Allocator.Temp);
            Assert.IsTrue(GltfGlobals.IsGltfBinary(data.AsReadOnly()));
        }
    }
}
