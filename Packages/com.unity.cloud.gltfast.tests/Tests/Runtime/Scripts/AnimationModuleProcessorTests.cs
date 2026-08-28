// SPDX-FileCopyrightText: 2026 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using GLTFast.Animations;
using GLTFast.Schema;
using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Mesh = UnityEngine.Mesh;

namespace GLTFast.Tests
{
    class AnimationModuleProcessorTests
    {
        [Test]
        public void AddRotationCurvesWithDefaultValuesLinear()
        {
            AddRotationCurvesWithDefaultValues(InterpolationType.Linear);
        }

        [Test]
        public void AddRotationCurvesWithDefaultValuesCubicSpline()
        {
            AddRotationCurvesWithDefaultValues(InterpolationType.CubicSpline);
        }

        [Test]
        public void AddRotationCurvesWithDefaultValuesStep()
        {
            AddRotationCurvesWithDefaultValues(InterpolationType.Step);
        }

        [Test]
        public void AddVec3CurvesWithDefaultValuesLinear()
        {
            AddVec3CurvesWithDefaultValues(InterpolationType.Linear);
        }

        [Test]
        public void AddVec3CurvesWithDefaultValuesCubicSpline()
        {
            AddVec3CurvesWithDefaultValues(InterpolationType.CubicSpline);
        }

        [Test]
        public void AddVec3CurvesWithDefaultValuesStep()
        {
            AddVec3CurvesWithDefaultValues(InterpolationType.Step);
        }

        [Test]
        public void AddMorphTargetWeightCurvesWithDefaultValuesLinear()
        {
            AddMorphTargetWeightCurvesWithDefaultValues(InterpolationType.Linear);
        }

        [Test]
        public void AddMorphTargetWeightCurvesWithDefaultValuesCubicSpline()
        {
            AddMorphTargetWeightCurvesWithDefaultValues(InterpolationType.CubicSpline);
        }

        [Test]
        public void AddMorphTargetWeightCurvesWithDefaultValuesStep()
        {
            AddMorphTargetWeightCurvesWithDefaultValues(InterpolationType.Step);
        }

        [TestCase(InterpolationType.Linear)]
        [TestCase(InterpolationType.CubicSpline)]
        [TestCase(InterpolationType.Step)]
        public void AddVec3CurvesClampsBeforeFirstKey(InterpolationType interpolationType)
        {
#if UNITY_ANIMATION
            using var times = CreateNonZeroStartTimes();
            using var values = CreateVec3Values(interpolationType, new float3(1f, 2f, 3f), new float3(4f, 5f, 6f));
            var hierarchy = new NodeHierarchyInfo(new[] { "Target" }, new[] { -1 });

            using var anim = new AnimationModuleProcessor(1, true);
            anim.AddClip(0, "TestClip");
            anim.AddTranslationCurves(0, 0, hierarchy, times.AsReadOnly(), values.AsReadOnly(), interpolationType);
            anim.AddScaleCurves(0, 0, hierarchy, times.AsReadOnly(), values.AsReadOnly(), interpolationType);

            var parent = new GameObject("Parent");
            var go = new GameObject("Target");
            go.transform.SetParent(parent.transform);
            SampleAtZero(anim.AnimationClips[0], parent);

            AssertVector3AreEqual(new Vector3(1f, 2f, 3f), go.transform.localPosition, "Expected local position to clamp to the first key before the first key time.");
            AssertVector3AreEqual(new Vector3(1f, 2f, 3f), go.transform.localScale, "Expected local scale to clamp to the first key before the first key time.");
            Object.Destroy(parent);
#else
            Assert.Ignore("UNITY_ANIMATION is not defined; AnimationModuleUtils is not compiled.");
#endif
        }

        [TestCase(InterpolationType.Linear)]
        [TestCase(InterpolationType.CubicSpline)]
        [TestCase(InterpolationType.Step)]
        public void AddRotationCurvesClampsBeforeFirstKey(InterpolationType interpolationType)
        {
#if UNITY_ANIMATION
            using var times = CreateNonZeroStartTimes();
            var expectedRotation = quaternion.EulerXYZ(math.radians(new float3(10f, 20f, 30f)));
            using var values = CreateQuaternionValues(
                interpolationType,
                expectedRotation,
                quaternion.EulerXYZ(math.radians(new float3(70f, 80f, 90f))));
            var hierarchy = new NodeHierarchyInfo(new[] { "Target" }, new[] { -1 });

            using var anim = new AnimationModuleProcessor(1, true);
            anim.AddClip(0, "TestClip");
            anim.AddRotationCurves(0, 0, hierarchy, times.AsReadOnly(), values.AsReadOnly(), interpolationType);

            var parent = new GameObject("Parent");
            var go = new GameObject("Target");
            go.transform.SetParent(parent.transform);
            SampleAtZero(anim.AnimationClips[0], parent);

            Assert.AreEqual(
                0f,
                Quaternion.Angle(new Quaternion(expectedRotation.value.x, expectedRotation.value.y, expectedRotation.value.z, expectedRotation.value.w), go.transform.localRotation),
                1e-3f,
                "Expected local rotation to clamp to the first key before the first key time.");
            Object.Destroy(parent);
#else
            Assert.Ignore("UNITY_ANIMATION is not defined; AnimationModuleUtils is not compiled.");
#endif
        }

        [TestCase(InterpolationType.Linear)]
        [TestCase(InterpolationType.CubicSpline)]
        [TestCase(InterpolationType.Step)]
        public void AddMorphTargetWeightCurvesClampsBeforeFirstKey(InterpolationType interpolationType)
        {
#if UNITY_ANIMATION
            var morphTargetNames = new[] { "Shape0" };
            using var times = CreateNonZeroStartTimes();
            using var values = CreateScalarValues(interpolationType, 25f, 75f);
            var hierarchy = new NodeHierarchyInfo(new[] { "Target" }, new[] { -1 });

            using var anim = new AnimationModuleProcessor(1, true);
            anim.AddClip(0, "TestClip");
            anim.AddMorphTargetWeightCurves(
                0, 0, 0, null, hierarchy, times.AsReadOnly(), values.AsReadOnly(), interpolationType, morphTargetNames);

            var parent = new GameObject("Parent");
            CreateSkinnedTargetWithBlendShape(parent.transform, "Shape0", out var mainRenderer, out _);
            SampleAtZero(anim.AnimationClips[0], parent);

            Assert.AreEqual(25f, mainRenderer.GetBlendShapeWeight(0), 1e-3f, "Expected blend shape weight to clamp to the first key before the first key time.");
            Object.Destroy(parent);
#else
            Assert.Ignore("UNITY_ANIMATION is not defined; AnimationModuleUtils is not compiled.");
#endif
        }

        static void AddRotationCurvesWithDefaultValues(InterpolationType interpolationType)
        {
#if UNITY_ANIMATION
            using var times = new NativeArray<float>(new[] { 0f, 1f }, Allocator.Temp);
            NativeArray<quaternion>.ReadOnly values = default;
            var hierarchy = new NodeHierarchyInfo(new[] { "Target" }, new[] { -1 });

            using var anim = new AnimationModuleProcessor(1, true);
            anim.AddClip(0, "TestClip");
            anim.AddRotationCurves(0, 0, hierarchy, times.AsReadOnly(), values, interpolationType);

            var clip = anim.AnimationClips[0];
            Assert.IsFalse(clip.empty, "Expected rotation curves to be registered on the clip.");
            Assert.AreEqual(1f, clip.length, 1e-6f, "Clip length should match the last key time.");

            var parent = new GameObject("Parent");
            var go = new GameObject("Target");
            go.transform.SetParent(parent.transform);
            go.transform.rotation = Quaternion.Euler(45, 45, 45); // Set to a non-default rotation to verify that the curve overrides it
            clip.SampleAnimation(parent, .5f);
            Assert.AreEqual(new Vector3(0, 0, 0), go.transform.rotation.eulerAngles, "Expected default rotation to be (0, 0, 0) when values are not provided.");
#else
            Assert.Ignore("UNITY_ANIMATION is not defined; AnimationModuleUtils is not compiled.");
#endif
        }

        static void AddVec3CurvesWithDefaultValues(InterpolationType interpolationType)
        {
#if UNITY_ANIMATION
            using var times = new NativeArray<float>(new[] { 0f, 1f }, Allocator.Temp);
            NativeArray<float3>.ReadOnly values = default;
            var hierarchy = new NodeHierarchyInfo(new[] { "Target" }, new[] { -1 });

            using var anim = new AnimationModuleProcessor(1, true);
            anim.AddClip(0, "TestClip");
            anim.AddTranslationCurves(0, 0, hierarchy, times.AsReadOnly(), values, interpolationType);
            anim.AddScaleCurves(0, 0, hierarchy, times.AsReadOnly(), values, interpolationType);

            var clip = anim.AnimationClips[0];
            Assert.IsFalse(clip.empty, "Expected translation curves to be registered on the clip.");
            Assert.AreEqual(1f, clip.length, 1e-6f, "Clip length should match the last key time.");

            var parent = new GameObject("Parent");
            var go = new GameObject("Target");
            go.transform.SetParent(parent.transform);
            go.transform.localPosition = new Vector3(1f, 2f, 3f);
            clip.SampleAnimation(parent, .5f);
            Assert.AreEqual(Vector3.zero, go.transform.localPosition, "Expected default local position to be (0, 0, 0) when values are not provided.");
            Assert.AreEqual(Vector3.zero, go.transform.localScale, "Expected default scale to be (0, 0, 0) when values are not provided.");
#else
            Assert.Ignore("UNITY_ANIMATION is not defined; AnimationModuleUtils is not compiled.");
#endif
        }

        static void AddMorphTargetWeightCurvesWithDefaultValues(InterpolationType interpolationType)
        {
#if UNITY_ANIMATION
            // With default (uncreated) values, morph target count must come from morphTargetNames
            // values.Length is zero and would otherwise yield no curves.
            var morphTargetNames = new[] { "Shape0" };

            using var times = new NativeArray<float>(new[] { 0f, 1f }, Allocator.Temp);
            NativeArray<float>.ReadOnly values = default;

            var hierarchy = new NodeHierarchyInfo(new[] { "Target", "Submesh" }, new[] { -1, 0 });

            using var anim = new AnimationModuleProcessor(1, true);
            anim.AddClip(0, "TestClip");
            anim.AddMorphTargetWeightCurves(
                0, 0, 0, null, hierarchy, times.AsReadOnly(), values, interpolationType, morphTargetNames);
            anim.AddMorphTargetWeightCurves(
                0, 0, 0, "Submesh", hierarchy, times.AsReadOnly(), values, interpolationType, morphTargetNames);

            var clip = anim.AnimationClips[0];
            Assert.IsFalse(clip.empty, "Expected morph target weight curves to be registered on the clip.");
            Assert.AreEqual(1f, clip.length, 1e-6f, "Clip length should match the last key time.");

            var clip2 = anim.AnimationClips[0];
            Assert.IsFalse(clip2.empty, "Expected morph target weight curves to be registered on the clip.");
            Assert.AreEqual(1f, clip2.length, 1e-6f, "Clip length should match the last key time.");

            var parent = new GameObject("Parent");
            CreateSkinnedTargetWithBlendShape(parent.transform, "Shape0", out var mainRenderer, out var submeshRenderer);
            clip.SampleAnimation(parent, .5f);
            Assert.AreEqual(0f, mainRenderer.GetBlendShapeWeight(0), 1e-3f, "Expected default blend shape weight to be 0 when values are not provided.");
            Assert.AreEqual(0f, submeshRenderer.GetBlendShapeWeight(0), 1e-3f, "Expected default blend shape weight to be 0 when values are not provided.");
            Object.Destroy(parent);
#else
            Assert.Ignore("UNITY_ANIMATION is not defined; AnimationModuleUtils is not compiled.");
#endif
        }

#if UNITY_ANIMATION
        static NativeArray<float> CreateNonZeroStartTimes()
        {
            return new NativeArray<float>(new[] { 0.033333335f, 1f }, Allocator.Temp);
        }

        static NativeArray<float3> CreateVec3Values(InterpolationType interpolationType, float3 firstValue, float3 secondValue)
        {
            return interpolationType == InterpolationType.CubicSpline
                ? new NativeArray<float3>(new[] { float3.zero, firstValue, float3.zero, float3.zero, secondValue, float3.zero }, Allocator.Temp)
                : new NativeArray<float3>(new[] { firstValue, secondValue }, Allocator.Temp);
        }

        static NativeArray<quaternion> CreateQuaternionValues(
            InterpolationType interpolationType,
            quaternion firstValue,
            quaternion secondValue
            )
        {
            var zeroTangent = new quaternion(new float4(0f));
            return interpolationType == InterpolationType.CubicSpline
                ? new NativeArray<quaternion>(new[]
                {
                    zeroTangent, firstValue, zeroTangent,
                    zeroTangent, secondValue, zeroTangent
                }, Allocator.Temp)
                : new NativeArray<quaternion>(new[] { firstValue, secondValue }, Allocator.Temp);
        }

        static NativeArray<float> CreateScalarValues(InterpolationType interpolationType, float firstValue, float secondValue)
        {
            return interpolationType == InterpolationType.CubicSpline
                ? new NativeArray<float>(new[] { 0f, firstValue, 0f, 0f, secondValue, 0f }, Allocator.Temp)
                : new NativeArray<float>(new[] { firstValue, secondValue }, Allocator.Temp);
        }

        static void SampleAtZero(AnimationClip clip, GameObject parent)
        {
            clip.SampleAnimation(parent, 0f);
        }

        static void AssertVector3AreEqual(Vector3 expected, Vector3 actual, string message)
        {
            Assert.AreEqual(expected.x, actual.x, 1e-3f, message);
            Assert.AreEqual(expected.y, actual.y, 1e-3f, message);
            Assert.AreEqual(expected.z, actual.z, 1e-3f, message);
        }

        static void CreateSkinnedTargetWithBlendShape(
            Transform parent,
            string blendShapeName,
            out SkinnedMeshRenderer mainRenderer,
            out SkinnedMeshRenderer submeshRenderer
            )
        {
            var go = new GameObject("Target");
            go.transform.SetParent(parent.transform);
            mainRenderer = GenerateSkinnedMeshRenderer(go);

            var submeshGo = new GameObject("Submesh");
            submeshGo.transform.SetParent(go.transform, false);
            submeshRenderer = GenerateSkinnedMeshRenderer(submeshGo);

            return;
            SkinnedMeshRenderer GenerateSkinnedMeshRenderer(GameObject target)
            {
                var smr = target.AddComponent<SkinnedMeshRenderer>();
                var mesh = new Mesh { name = "AnimationModuleUtilsTestMesh" };
                var vertices = new[] { Vector3.zero, Vector3.right, Vector3.up };
                mesh.vertices = vertices;
                mesh.triangles = new[] { 0, 1, 2 };
                mesh.bindposes = new[] { Matrix4x4.identity };
                var deltas = new Vector3[vertices.Length];
                mesh.AddBlendShapeFrame(blendShapeName, 100f, deltas, null, null);
                mesh.RecalculateBounds();
                smr.sharedMesh = mesh;
                smr.bones = new[] { target.transform };
                smr.rootBone = target.transform;
                smr.SetBlendShapeWeight(0, 100f);
                return smr;
            }
        }
#endif
    }
}
