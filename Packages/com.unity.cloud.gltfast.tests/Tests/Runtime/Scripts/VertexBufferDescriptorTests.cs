// SPDX-FileCopyrightText: 2024 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using GLTFast.Schema;
using NUnit.Framework;
using UnityEngine;

namespace GLTFast.Tests
{
    class VertexBufferDescriptorTests
    {

        [Test]
        public void VertexBufferDescriptorEqualTest()
        {
            var a = VertexBufferDescriptor.FromPrimitive(
                new MeshPrimitive { Attributes = new Attributes { Position = 42 } });
            var b = VertexBufferDescriptor.FromPrimitive(
                new MeshPrimitive { Attributes = new Attributes { Position = 42 } });

            Assert.IsTrue(a == b);

            a = VertexBufferDescriptor.FromPrimitive(
                new MeshPrimitive { Attributes = new Attributes { Position = 41 } });
            b = VertexBufferDescriptor.FromPrimitive(
                new MeshPrimitive { Attributes = new Attributes { Position = 42 } });

            Assert.IsTrue(a == b);
        }
    }
}
