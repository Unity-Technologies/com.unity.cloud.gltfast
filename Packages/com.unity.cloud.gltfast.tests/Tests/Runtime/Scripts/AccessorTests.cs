// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using GLTFast.Schema;
using NUnit.Framework;
using UnityEngine;

namespace GLTFast.Tests
{
    class AccessorTests
    {
        [Test]
        public void GetAccessorAttributeType()
        {
            Assert.AreEqual(AccessorType.Scalar, Accessor.GetAccessorAttributeType(1));
            Assert.AreEqual(AccessorType.Vector2, Accessor.GetAccessorAttributeType(2));
            Assert.AreEqual(AccessorType.Vector3, Accessor.GetAccessorAttributeType(3));
            Assert.AreEqual(AccessorType.Vector4, Accessor.GetAccessorAttributeType(4));

            Assert.That(() => Accessor.GetAccessorAttributeType(0),
                Throws.TypeOf<ArgumentOutOfRangeException>());

            Assert.That(() => Accessor.GetAccessorAttributeType(5),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void GetAccessorAttributeTypeLength()
        {
            Assert.AreEqual(1, Accessor.GetAccessorAttributeTypeLength(AccessorType.Scalar));
            Assert.AreEqual(2, Accessor.GetAccessorAttributeTypeLength(AccessorType.Vector2));
            Assert.AreEqual(3, Accessor.GetAccessorAttributeTypeLength(AccessorType.Vector3));
            Assert.AreEqual(4, Accessor.GetAccessorAttributeTypeLength(AccessorType.Vector4));
            Assert.AreEqual(4, Accessor.GetAccessorAttributeTypeLength(AccessorType.Matrix2x2));
            Assert.AreEqual(9, Accessor.GetAccessorAttributeTypeLength(AccessorType.Matrix3x3));
            Assert.AreEqual(16, Accessor.GetAccessorAttributeTypeLength(AccessorType.Matrix4x4));

            Assert.That(() => Accessor.GetAccessorAttributeTypeLength(AccessorType.Undefined),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }
    }
}
