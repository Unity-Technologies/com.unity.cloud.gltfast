// SPDX-FileCopyrightText: 2025 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.Cloud.Gltfast.Tests
{
    class VectorTests
    {
        [Test]
        public void Byte3()
        {
            var v = new byte3(128, 64, 32);
            Assert.AreEqual(new float3(-128, 64, 32), v.GltfToUnityFloat3());
            Utils.AssertNearOrEqual(
                new float3(-.5f, .25f, .125f), v.GltfToUnityNormalizedFloat3(), Constants.epsilonUInt8);
            Assert.AreEqual(new ushort3(128, 32, 64), v.GltfToUnityTriangleIndicesUInt16());
            Assert.AreEqual(new uint3(128, 32, 64), v.GltfToUnityTriangleIndices());
        }

        [Test]
        public void Short3()
        {
            var v = new short3(16384, 8192, 4096);
            Assert.AreEqual(new float3(-16384, 8192, 4096), v.GltfToUnityFloat3());
            Utils.AssertNearOrEqual(
                new float3(-.5f, .25f, .125f), v.GltfToUnityNormalizedFloat3(), Constants.epsilonInt16);

            const short p = (short)(short.MaxValue * 0.57735027);
            v = new short3(p, p, p);
            Utils.AssertNearOrEqual(
                new float3(-0.57735027f, 0.57735027f, 0.57735027f),
                v.GltfNormalToUnityFloat3(),
                Constants.epsilonUInt8
                );
        }

        [Test]
        public void SByte3()
        {
            var v = new sbyte3(127, 64, 32);
            Assert.AreEqual(new float3(-127, 64, 32), v.GltfToUnityFloat3());
            Utils.AssertNearOrEqual(
                new float3(-1, .5f, .25f), v.GltfToUnityNormalizedFloat3(), Constants.epsilonInt8);
        }

        [Test]
        public void SByte4()
        {
            var v = new sbyte4(127, 64, 32, 16);
            Utils.AssertNearOrEqual(
                new quaternion(1, -.5f, -.25f, .125f), v.GltfToUnityRotation(), Constants.epsilonInt8);
        }

        [Test]
        public void UShort3()
        {
            var v = new ushort3(128, 64, 32);
            Assert.AreEqual(new uint3(128, 32, 64), v.GltfToUnityTriangleIndices());
        }

    }
}
