// SPDX-FileCopyrightText: 2026 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using NUnit.Framework;

namespace Unity.Cloud.Gltfast.Tests
{
    static class GltfIndexTests
    {
        static List<string> Items => new() { "a", "b", "c" };

        [Test]
        public static void TryGetElementFirst()
        {
            Assert.IsTrue(GltfIndex.TryGetElement(Items, 0, out var element));
            Assert.AreEqual("a", element);
        }

        [Test]
        public static void TryGetElementLast()
        {
            Assert.IsTrue(GltfIndex.TryGetElement(Items, 2, out var element));
            Assert.AreEqual("c", element);
        }

        [Test]
        public static void TryGetElementNullIndex()
        {
            Assert.IsFalse(GltfIndex.TryGetElement(Items, null, out var element));
            Assert.IsNull(element);
        }

        [Test]
        public static void TryGetElementNegativeIndex()
        {
            Assert.IsFalse(GltfIndex.TryGetElement(Items, -1, out var element));
            Assert.IsNull(element);
        }

        [Test]
        public static void TryGetElementIndexBeyondCount()
        {
            Assert.IsFalse(GltfIndex.TryGetElement(Items, 3, out var element));
            Assert.IsNull(element);
        }

        [Test]
        public static void TryGetElementNullList()
        {
            Assert.IsFalse(GltfIndex.TryGetElement<string>(null, 0, out var element));
            Assert.IsNull(element);
        }

        [Test]
        public static void TryGetElementEmptyList()
        {
            Assert.IsFalse(GltfIndex.TryGetElement(new List<string>(), 0, out var element));
            Assert.IsNull(element);
        }

        [Test]
        public static void TryGetElementValueType()
        {
            Assert.IsFalse(GltfIndex.TryGetElement(new List<int> { 1, 2 }, null, out var element));
            Assert.AreEqual(0, element);
        }

        [Test]
        public static void TryGetIndexValid()
        {
            Assert.IsTrue(GltfIndex.TryGetIndex(0, 1, out var index));
            Assert.AreEqual(0, index);
        }

        [TestCase(null)]
        [TestCase(-1)]
        [TestCase(3)]
        public static void TryGetIndexInvalid(int? value)
        {
            Assert.IsFalse(GltfIndex.TryGetIndex(value, 3, out var index));
            Assert.AreEqual(0, index);
        }

        [Test]
        public static void TryGetIndexZeroCount()
        {
            Assert.IsFalse(GltfIndex.TryGetIndex(0, 0, out _));
        }

        [Test]
        public static void DescribeValue()
        {
            Assert.AreEqual("0", GltfIndex.Describe(0));
            Assert.AreEqual("-1", GltfIndex.Describe(-1));
        }

        [Test]
        public static void DescribeNull()
        {
            Assert.AreEqual("null", GltfIndex.Describe(null));
        }
    }
}
