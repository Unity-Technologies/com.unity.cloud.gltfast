// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.TestTools.Utils;

namespace Unity.Cloud.Gltfast.Tests
{
    class MatrixExtensionTest
    {
        static Double3EqualityComparer s_Vector3Comparer;
        static Double4EqualityComparer s_QuaternionComparer;
        // Used for comparing the double3x3.ToQuaternion result against the established
        // Unity.Mathematics float3x3 -> quaternion implementation (single-precision).
        static Double4EqualityComparer s_FloatQuaternionComparer;

        static double4x4 s_UnityMatrix;
        static float4x4 s_Matrix;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            s_Vector3Comparer = new Double3EqualityComparer(10e-6f);
            s_QuaternionComparer = new Double4EqualityComparer(10e-6f);
            // Tolerance suitable for comparing against the float reference implementation.
            s_FloatQuaternionComparer = new Double4EqualityComparer(1e-5);

            // Corner case matrix (90°/0°/45° rotation with -1/-1/-1 scale)
            s_UnityMatrix = new double4x4(
                new double4(-0.7071067811865474, 0, -0.7071067811865477, 0),
                new double4(0.7071067811865477, 0, -0.7071067811865474, 0),
                new double4(0, 1, 0, 0),
                new double4(0, 0, 0, 1)
            );

            s_Matrix = (float4x4)s_UnityMatrix;
        }

        [Test]
        public void MatrixDecomposeTest()
        {
            Profiler.BeginSample("Matrix4x4.DecomposeUnity");
            Assert.IsTrue(s_UnityMatrix.ValidTRS());
            Profiler.EndSample();

            Profiler.BeginSample("Matrix4x4.DecomposeCustom");
            s_UnityMatrix.Decompose(out var t, out var r, out var s);
            Profiler.EndSample();

            Assert.That(t, Is.EqualTo(double3.zero).Using(s_Vector3Comparer));
            Assert.That(r, Is.EqualTo(
                new double4(0.65328151, -0.270598054, 0.270598054, 0.65328151))
                .Using(s_QuaternionComparer)
            );
            Assert.That(s, Is.EqualTo(new double3(-.99999994f, -.99999994f, -1)).Using(s_Vector3Comparer));

            Profiler.BeginSample("double4x4.Decompose");
            s_UnityMatrix.Decompose(out var t3, out var r3, out var s3);
            Profiler.EndSample();

            Assert.That(t3, Is.EqualTo(new double3(0, 0, 0)).Using(s_Vector3Comparer));
            Assert.That(
                r3,
                Is.EqualTo(new double4(0.65328151, -0.270598054, 0.270598054, 0.65328151))
                    .Using(s_QuaternionComparer)
                );
            Assert.That(
                s3,
                Is.EqualTo(new double3(-.99999994f, -.99999994f, -1))
                    .Using(s_Vector3Comparer)
                );
        }

        /// <summary>
        /// Verifies that <see cref="Double3x3Extensions.ToQuaternion"/> produces the same
        /// rotation as the established <c>Unity.Mathematics.quaternion(float3x3)</c>
        /// constructor for a broad set of orthonormal rotation matrices.
        ///
        /// All four branches of the bitwise port are exercised by the test cases:
        ///   1. u.x positive, t positive  (trace large, near identity)
        ///   2. u.x positive, t negative
        ///   3. u.x negative, t positive
        ///   4. u.x negative, t negative  (e.g. 180° rotations around X/Y/Z)
        /// </summary>
        [Test]
        [TestCase(0.0, 0.0, 0.0, TestName = "ToQuaternion_Identity")]
        [TestCase(90.0, 0.0, 0.0, TestName = "ToQuaternion_X90")]
        [TestCase(180.0, 0.0, 0.0, TestName = "ToQuaternion_X180")]
        [TestCase(-90.0, 0.0, 0.0, TestName = "ToQuaternion_Xm90")]
        [TestCase(0.0, 90.0, 0.0, TestName = "ToQuaternion_Y90")]
        [TestCase(0.0, 180.0, 0.0, TestName = "ToQuaternion_Y180")]
        [TestCase(0.0, -90.0, 0.0, TestName = "ToQuaternion_Ym90")]
        [TestCase(0.0, 0.0, 90.0, TestName = "ToQuaternion_Z90")]
        [TestCase(0.0, 0.0, 180.0, TestName = "ToQuaternion_Z180")]
        [TestCase(0.0, 0.0, -90.0, TestName = "ToQuaternion_Zm90")]
        [TestCase(45.0, 30.0, 60.0, TestName = "ToQuaternion_Arbitrary")]
        [TestCase(119.99, 0.0, 0.0, TestName = "ToQuaternion_XBranchBoundaryBelow")]
        [TestCase(120.01, 0.0, 0.0, TestName = "ToQuaternion_XBranchBoundaryAbove")]
        [TestCase(135.0, 135.0, 135.0, TestName = "ToQuaternion_AllNegativeDiagonal")]
        [TestCase(170.0, 10.0, 20.0, TestName = "ToQuaternion_NearXFlip")]
        public void ToQuaternion_MatchesFloatReference(double eulerX, double eulerY, double eulerZ)
        {
            // Build an orthonormal rotation matrix from the given Euler angles.
            var radians = math.radians(new double3(eulerX, eulerY, eulerZ));
            var qDouble = QuaternionFromEulerXYZ(radians);
            var m = Double3x3FromQuaternion(qDouble);

            // Reference: pass through the established float3x3 -> quaternion implementation.
            var mFloat = new float3x3(
                (float3)m.c0,
                (float3)m.c1,
                (float3)m.c2
            );
            var qReference = new quaternion(mFloat).value;

            // Compute via the double3x3 bitwise port.
            var qActual = m.ToQuaternion();

            // Quaternions q and -q represent the same rotation. Align signs before comparing.
            if (math.dot((float4)qActual, qReference) < 0f)
            {
                qActual = -qActual;
            }

            Assert.That(
                qActual,
                Is.EqualTo(new double4(qReference)).Using(s_FloatQuaternionComparer),
                $"double3x3.ToQuaternion disagrees with Unity.Mathematics float3x3 -> quaternion reference for Euler ({eulerX}, {eulerY}, {eulerZ})."
            );

            // Additionally verify that reconstructing the matrix from the resulting
            // quaternion yields the original orthonormal matrix (round-trip check).
            var mReconstructed = Double3x3FromQuaternion(qActual);
            AssertMatrixEquals(m, mReconstructed, 1e-12, $"Round-trip failed for Euler ({eulerX}, {eulerY}, {eulerZ}).");
        }

        /// <summary>
        /// Stress tests the double conversion against random orthonormal rotation matrices
        /// and confirms a clean round-trip (matrix -> quaternion -> matrix).
        /// </summary>
        [Test]
        public void ToQuaternion_RandomRoundTrip()
        {
            const int sampleCount = 1024;
            var rng = new Unity.Mathematics.Random(0x5EED1234u);

            for (var i = 0; i < sampleCount; i++)
            {
                // Generate a uniformly random unit quaternion.
                var q = math.normalize(rng.NextDouble4(new double4(-1.0), new double4(1.0)));
                var m = Double3x3FromQuaternion(q);

                var qActual = m.ToQuaternion();
                var mReconstructed = Double3x3FromQuaternion(qActual);

                AssertMatrixEquals(m, mReconstructed, 1e-12, $"Round-trip failed for random sample #{i} (q={q}).");
            }
        }

        /// <summary>
        /// Verifies that <see cref="Mathematics.Decompose(double4x4, out double3, out double4, out double3)"/>
        /// returns a rotation that matches the established single-precision implementation
        /// for a broad set of TRS matrices, including those with negative scale that exercise
        /// the IsNegative branch in the decomposition.
        ///
        /// Note: TRS decomposition is ambiguous in the presence of negative scale (e.g. a single
        /// negative axis is indistinguishable from a 180° rotation combined with negating the
        /// other two axes). The contract of <see cref="Mathematics.Decompose(double4x4, out double3, out double4, out double3)"/>
        /// is therefore validated by:
        ///   1. ensuring the recomposed TRS matrix matches the input (round-trip), and
        ///   2. ensuring the resulting rotation matches what the established single-precision
        ///      decomposition produces for the same matrix.
        /// </summary>
        [Test]
        [TestCase(1.0, 1.0, 1.0, 0.0, 0.0, 0.0, TestName = "Decompose_Identity")]
        [TestCase(2.0, 3.0, 4.0, 30.0, 45.0, 60.0, TestName = "Decompose_PositiveScaleArbitraryRot")]
        [TestCase(-1.0, 1.0, 1.0, 0.0, 90.0, 0.0, TestName = "Decompose_NegativeXScale")]
        [TestCase(1.0, -1.0, 1.0, 90.0, 0.0, 45.0, TestName = "Decompose_NegativeYScale")]
        [TestCase(1.0, 1.0, -1.0, 45.0, 30.0, 60.0, TestName = "Decompose_NegativeZScale")]
        [TestCase(-1.0, -1.0, -1.0, 90.0, 0.0, 45.0, TestName = "Decompose_AllNegativeScaleCornerCase")]
        [TestCase(0.5, 0.5, 0.5, 180.0, 0.0, 0.0, TestName = "Decompose_X180")]
        [TestCase(0.5, 0.5, 0.5, 0.0, 180.0, 0.0, TestName = "Decompose_Y180")]
        [TestCase(0.5, 0.5, 0.5, 0.0, 0.0, 180.0, TestName = "Decompose_Z180")]
        public void Decompose_RotationMatchesFloatReference(
            double sx, double sy, double sz,
            double eulerX, double eulerY, double eulerZ)
        {
            var radians = math.radians(new double3(eulerX, eulerY, eulerZ));
            var q = QuaternionFromEulerXYZ(radians);
            var scale = new double3(sx, sy, sz);
            var translation = new double3(1.5, -2.25, 3.75);

            var rotMatrix = Double3x3FromQuaternion(q);
            var rotScale = new double3x3(
                rotMatrix.c0 * scale.x,
                rotMatrix.c1 * scale.y,
                rotMatrix.c2 * scale.z
            );
            var m = new double4x4(
                new double4(rotScale.c0, 0),
                new double4(rotScale.c1, 0),
                new double4(rotScale.c2, 0),
                new double4(translation, 1)
            );

            m.Decompose(out var t, out var rDouble, out var sOut);

            // Reference: run the same decomposition logic in single precision using
            // Unity.Mathematics' established quaternion(float3x3) constructor.
            var rFloatReference = DecomposeFloatReference((float4x4)m, out _, out var sFloatReference);

            // Align signs before comparing (-q == q for rotations).
            if (math.dot((float4)rDouble, rFloatReference) < 0f)
            {
                rDouble = -rDouble;
            }

            Assert.That(t, Is.EqualTo(translation).Using(s_Vector3Comparer));
            Assert.That(
                rDouble,
                Is.EqualTo(new double4(rFloatReference)).Using(s_FloatQuaternionComparer),
                $"Decomposed rotation disagrees with float reference for scale=({sx},{sy},{sz}), euler=({eulerX},{eulerY},{eulerZ})."
            );
            // The scale decomposition is ambiguous with negative scale; verify that the double
            // implementation produces the same scale as the float reference (i.e. consistent
            // behavior), and that the TRS round-trip still reconstructs the original matrix.
            Assert.That(
                sOut,
                Is.EqualTo(new double3(sFloatReference)).Using(s_Vector3Comparer),
                $"Decomposed scale disagrees with float reference for scale=({sx},{sy},{sz}), euler=({eulerX},{eulerY},{eulerZ})."
            );

            // Recompose and verify round-trip.
            var qRot = Double3x3FromQuaternion(rDouble);
            var recomposed = new double4x4(
                new double4(qRot.c0 * sOut.x, 0),
                new double4(qRot.c1 * sOut.y, 0),
                new double4(qRot.c2 * sOut.z, 0),
                new double4(t, 1)
            );
            AssertMatrix4x4Equals(m, recomposed, 1e-12, "TRS round-trip failed.");
        }

        /// <summary>
        /// Builds a column-major orthonormal rotation matrix from a unit quaternion stored
        /// as <c>double4 (x, y, z, w)</c>. Mirrors Unity.Mathematics' <c>float3x3(quaternion)</c>.
        /// </summary>
        static double3x3 Double3x3FromQuaternion(double4 q)
        {
            double x = q.x, y = q.y, z = q.z, w = q.w;
            var x2 = x + x;
            var y2 = y + y;
            var z2 = z + z;
            var xx = x * x2;
            var xy = x * y2;
            var xz = x * z2;
            var yy = y * y2;
            var yz = y * z2;
            var zz = z * z2;
            var wx = w * x2;
            var wy = w * y2;
            var wz = w * z2;
            return new double3x3(
                new double3(1.0 - (yy + zz), xy + wz, xz - wy),
                new double3(xy - wz, 1.0 - (xx + zz), yz + wx),
                new double3(xz + wy, yz - wx, 1.0 - (xx + yy))
            );
        }

        /// <summary>
        /// Computes a unit quaternion from XYZ Euler angles in radians,
        /// matching <c>quaternion.EulerXYZ</c> from Unity.Mathematics (double precision).
        /// </summary>
        static double4 QuaternionFromEulerXYZ(double3 radians)
        {
            math.sincos(0.5 * radians, out var s, out var c);
            // Same composition as Unity.Mathematics quaternion.EulerXYZ.
            return new double4(
                s.x * c.y * c.z - s.y * s.z * c.x,
                s.y * c.x * c.z + s.x * s.z * c.y,
                s.z * c.x * c.y - s.x * s.y * c.z,
                c.x * c.y * c.z + s.y * s.z * s.x
            );
        }

        /// <summary>
        /// Single-precision reference TRS decomposition that mirrors the logic in
        /// <see cref="Mathematics.Decompose(double4x4, out double3, out double4, out double3)"/>
        /// but uses the well-established <c>Unity.Mathematics.quaternion(float3x3)</c> constructor
        /// for the rotation step. Used to validate the new double-precision implementation.
        /// </summary>
        static float4 DecomposeFloatReference(float4x4 m, out float3 translation, out float3 scale)
        {
            var rotScale = new float3x3(m.c0.xyz, m.c1.xyz, m.c2.xyz);
            var lenC0 = math.length(rotScale.c0);
            var lenC1 = math.length(rotScale.c1);
            var lenC2 = math.length(rotScale.c2);

            float3x3 rotationMatrix;
            rotationMatrix.c0 = rotScale.c0 / lenC0;
            rotationMatrix.c1 = rotScale.c1 / lenC1;
            rotationMatrix.c2 = rotScale.c2 / lenC2;

            scale = new float3(lenC0, lenC1, lenC2);

            var cross = math.cross(rotationMatrix.c0, rotationMatrix.c1);
            if (math.dot(cross, rotationMatrix.c2) < 0f)
            {
                rotationMatrix *= -1f;
                scale *= -1f;
            }

            rotationMatrix.c0 = math.normalize(rotationMatrix.c0);
            rotationMatrix.c1 = math.normalize(rotationMatrix.c1);
            rotationMatrix.c2 = math.normalize(rotationMatrix.c2);

            translation = m.c3.xyz;
            return new quaternion(rotationMatrix).value;
        }

        static void AssertMatrixEquals(double3x3 expected, double3x3 actual, double tolerance, string message)
        {
            for (var col = 0; col < 3; col++)
            {
                var e = expected[col];
                var a = actual[col];
                for (var row = 0; row < 3; row++)
                {
                    Assert.That(
                        a[row],
                        Is.EqualTo(e[row]).Within(tolerance),
                        $"{message} Mismatch at column {col}, row {row}.");
                }
            }
        }

        static void AssertMatrix4x4Equals(double4x4 expected, double4x4 actual, double tolerance, string message)
        {
            for (var col = 0; col < 4; col++)
            {
                var e = expected[col];
                var a = actual[col];
                for (var row = 0; row < 4; row++)
                {
                    Assert.That(
                        a[row],
                        Is.EqualTo(e[row]).Within(tolerance),
                        $"{message} Mismatch at column {col}, row {row}.");
                }
            }
        }

        [Test]
        public void MatrixDouble4X4DecomposeTest()
        {
            Profiler.BeginSample("double4x4.Decompose");
            s_UnityMatrix.Decompose(out var translation, out var rotationValues, out var scale);
            Profiler.EndSample();

            Assert.That(translation, Is.EqualTo(new double3(0, 0, 0)).Using(s_Vector3Comparer));
            Assert.That(
                rotationValues,
                Is.EqualTo(new double4(0.65328151, -0.270598054, 0.270598054, 0.65328151))
                    .Using(s_QuaternionComparer)
                );
            Assert.That(
                scale,
                Is.EqualTo(new double3(-.99999994f, -.99999994f, -1))
                    .Using(s_Vector3Comparer)
                );
        }

        // [Test]
        // public void VertexStructTest() {
        //     var v = new VPosNormTan {
        //         position = new float3(1, 2, 3),
        //         normal = new float3(1, 0, 0),
        //         tangent = new float4(0, 1, 0,1),
        //     };
        //
        //     var vPosNor = new VPosNorm {
        //         position = new float3(1, 2, 3),
        //         normal = new float3(1, 0, 0),
        //     };
        //
        //     var vPos = new VPos {
        //         position = new float3(1, 2, 3)
        //     };
        //
        //     var uv1 = new VTexCoord1 {
        //         uv0 = new float2(1, 2),
        //     };
        //
        //     var uv2 = new VTexCoord2 {
        //         uv0 = new float2(1, 2),
        //         uv1 = new float2(1, 2),
        //     };
        //
        //     var uv3 = new VTexCoord3 {
        //         uv0 = new float2(1, 2),
        //         uv1 = new float2(1, 2),
        //         uv2 = new float2(1, 2),
        //     };
        //
        //     var uv4 = new VTexCoord4 {
        //         uv0 = new float2(1, 2),
        //         uv1 = new float2(1, 2),
        //         uv2 = new float2(1, 2),
        //         uv3 = new float2(1, 2),
        //     };
        //
        //     var uv5 = new VTexCoord5 {
        //         uv0 = new float2(1, 2),
        //         uv1 = new float2(1, 2),
        //         uv2 = new float2(1, 2),
        //         uv3 = new float2(1, 2),
        //         uv4 = new float2(1, 2),
        //     };
        //
        //     var uv6 = new VTexCoord6 {
        //         uv0 = new float2(1, 2),
        //         uv1 = new float2(1, 2),
        //         uv2 = new float2(1, 2),
        //         uv3 = new float2(1, 2),
        //         uv4 = new float2(1, 2),
        //         uv5 = new float2(1, 2),
        //     };
        //
        //     var uv7 = new VTexCoord7 {
        //         uv0 = new float2(1, 2),
        //         uv1 = new float2(1, 2),
        //         uv2 = new float2(1, 2),
        //         uv3 = new float2(1, 2),
        //         uv4 = new float2(1, 2),
        //         uv5 = new float2(1, 2),
        //         uv6 = new float2(1, 2),
        //     };
        //
        //     var uv8 = new VTexCoord8 {
        //         uv0 = new float2(1, 2),
        //         uv1 = new float2(1, 2),
        //         uv2 = new float2(1, 2),
        //         uv3 = new float2(1, 2),
        //         uv4 = new float2(1, 2),
        //         uv5 = new float2(1, 2),
        //         uv6 = new float2(1, 2),
        //         uv7 = new float2(1, 2),
        //     };
        // }
    }
}
