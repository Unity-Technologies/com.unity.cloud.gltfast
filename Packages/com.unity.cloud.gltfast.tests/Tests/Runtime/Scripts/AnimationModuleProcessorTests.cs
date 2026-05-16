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
