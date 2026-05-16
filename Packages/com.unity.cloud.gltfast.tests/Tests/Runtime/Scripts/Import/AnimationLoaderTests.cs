// SPDX-FileCopyrightText: 2026 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections;
using System.Collections.Generic;
using GLTFast.Addons;
using GLTFast.Animations;
using GLTFast.Schema;
using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace GLTFast.Tests.Import
{
    [Category("Import")]
    class AnimationLoaderTests
    {
        GltfTestCaseRunner m_Runner;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            m_Runner = new GltfTestCaseRunner();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            m_Runner.Dispose();
        }

        [GltfTestCase("glTF-Sample-Assets", 1, "AnimatedTriangle.gltf$")]
        public IEnumerator AnimationTest(GltfTestCaseSet testCaseSet, GltfTestCase testCase)
        {
#if UNITY_ANIMATION || GLTFAST_ANIMATION
            yield return AsyncWrapper.WaitForTask(
                m_Runner.Run(
                    testCaseSet,
                    testCase,
                    preLoadCallback: gltf =>
            {
                var animation = new MyAnimationAddon();
                animation.Inject(gltf);
            })
                );
#else
            Assert.Ignore("Animation is not available in current project setup.");
            yield break;
#endif
        }
    }

    class MyAnimationAddon : ImportAddonInstance, IAnimationProcessorFactory
    {
        public IAnimationProcessor CreateAnimationProcessor(int clipCount) => new MyAnimationProcessor(clipCount);

        public override bool SupportsGltfExtension(string extensionName) => false;

        public override void Inject(GltfImportBase gltfImport)
        {
            gltfImport.AddImportAddonInstance(this);
        }

        public override void Inject(IInstantiator instantiator) { }

        public override void Dispose() { }
    }

    sealed class MyAnimationProcessor : IAnimationProcessor
    {
        readonly MyAnimationClip[] m_Clips;

        public MyAnimationProcessor(int clipCount)
        {
            m_Clips = new MyAnimationClip[clipCount];
        }

        public IDataInstanceApplierFactory Complete()
        {
            return new MyAnimationApplierFactory(m_Clips);
        }

        public void AddClip(int index, string name)
        {
            m_Clips[index] = new MyAnimationClip(name);
        }

        public void AddTranslationCurves(
            int clipIndex,
            int targetNode,
            INodeHierarchyInfo nodeHierarchyInfo,
            NativeArray<float>.ReadOnly times,
            NativeArray<float3>.ReadOnly values,
            InterpolationType interpolationType
            )
        {
            m_Clips[clipIndex].AddTranslationCurve(
                targetNode,
                times,
                values,
                interpolationType
                );
        }

        public void AddRotationCurves(
            int clipIndex,
            int targetNode,
            INodeHierarchyInfo nodeHierarchyInfo,
            NativeArray<float>.ReadOnly times,
            NativeArray<quaternion>.ReadOnly values,
            InterpolationType interpolationType
            )
        {
            m_Clips[clipIndex].AddRotationCurve(
                targetNode,
                times,
                values,
                interpolationType
                );
        }

        public void AddScaleCurves(
            int clipIndex,
            int targetNode,
            INodeHierarchyInfo nodeHierarchyInfo,
            NativeArray<float>.ReadOnly times,
            NativeArray<float3>.ReadOnly values,
            InterpolationType interpolationType)
        {
            m_Clips[clipIndex].AddScaleCurve(
                targetNode,
                times,
                values,
                interpolationType);
        }

        public void AddMorphTargetWeightCurves(
            int clipIndex,
            int targetNode,
            int meshNumeration,
            string meshName,
            INodeHierarchyInfo nodeHierarchyInfo,
            NativeArray<float>.ReadOnly times,
            NativeArray<float>.ReadOnly values,
            InterpolationType interpolationType, string[] morphTargetNames = null)
        {
            m_Clips[clipIndex].AddMorphTargetWeightCurve(targetNode, times, values, interpolationType);
        }

        public void Dispose() { }
    }

    sealed class MyAnimationApplierFactory : DataInstanceApplierFactory<MyAnimationClip[], GameObjectInstantiator>
    {
        public MyAnimationApplierFactory(MyAnimationClip[] animationClips)
            : base(animationClips) { }

        protected override IInstanceApplier CreateInstanceApplier(GameObjectInstantiator instantiator)
        {
            return new MyAnimationApplier(instantiator, Data);
        }

        protected override void Dispose(bool disposing) { }
    }

    sealed class MyAnimationApplier : IInstanceApplier
    {
        readonly GameObjectInstantiator m_Instantiator;
        readonly MyAnimationClip[] m_AnimationClips;

        public MyAnimationApplier(GameObjectInstantiator instantiator, MyAnimationClip[] animationClips)
        {
            m_Instantiator = instantiator;
            m_AnimationClips = animationClips;

            m_Instantiator.EndSceneCompleted += OnEndScene;
        }

        public void OnEndScene()
        {
            m_Instantiator.EndSceneCompleted -= OnEndScene;
            // Assign the m_AnimationClips to the scene instance here.
        }
    }

    readonly struct MyAnimationClip
    {
        string Name { get; }

        List<MyCurve<float3>> TranslationCurves { get; }
        List<MyCurve<quaternion>> RotationCurves { get; }
        List<MyCurve<float3>> ScaleCurves { get; }
        List<MyCurve<float>> MorphTargetWeightCurves { get; }

        public MyAnimationClip(string name)
        {
            Name = name;
            TranslationCurves = new List<MyCurve<float3>>();
            RotationCurves = new List<MyCurve<quaternion>>();
            ScaleCurves = new List<MyCurve<float3>>();
            MorphTargetWeightCurves = new List<MyCurve<float>>();
        }

        public void AddTranslationCurve(
            int targetNode,
            NativeArray<float>.ReadOnly times,
            NativeArray<float3>.ReadOnly values,
            InterpolationType interpolationType
            )
        {
            TranslationCurves.Add(new MyCurve<float3>(targetNode, times, values, interpolationType));
        }

        public void AddScaleCurve(
            int targetNode,
            NativeArray<float>.ReadOnly times,
            NativeArray<float3>.ReadOnly values,
            InterpolationType interpolationType
            )
        {
            ScaleCurves.Add(new MyCurve<float3>(targetNode, times, values, interpolationType));
        }

        public void AddRotationCurve(
            int targetNode,
            NativeArray<float>.ReadOnly times,
            NativeArray<quaternion>.ReadOnly values,
            InterpolationType interpolationType
            )
        {
            RotationCurves.Add(new MyCurve<quaternion>(targetNode, times, values, interpolationType));
        }

        public void AddMorphTargetWeightCurve(
            int targetNode,
            NativeArray<float>.ReadOnly times,
            NativeArray<float>.ReadOnly values,
            InterpolationType interpolationType
            )
        {
            MorphTargetWeightCurves.Add(new MyCurve<float>(targetNode, times, values, interpolationType));
        }
    }

    struct MyCurve<T> where T : struct
    {
        public int TargetNode { get; }
        public NativeArray<float>.ReadOnly Times { get; }
        public NativeArray<T>.ReadOnly Values { get; }
        public InterpolationType InterpolationType { get; }

        public MyCurve(
            int targetNode,
            NativeArray<float>.ReadOnly times,
            NativeArray<T>.ReadOnly values,
            InterpolationType interpolationType
            )
        {
            TargetNode = targetNode;
            Times = times;
            Values = values;
            InterpolationType = interpolationType;
        }
    }
}
