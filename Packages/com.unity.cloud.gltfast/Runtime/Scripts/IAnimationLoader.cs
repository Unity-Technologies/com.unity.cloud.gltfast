// SPDX-FileCopyrightText: 2026 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using GLTFast.Schema;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace GLTFast
{
    /// <summary>
    /// Interface for loading animation data into an animation system.
    /// </summary>
    public interface IAnimationLoader
    {
        /// <summary>
        /// Initializes loading all animation clips.
        /// </summary>
        /// <param name="clipCount">Total number of animation clips.</param>
        void Init(int clipCount);

        /// <summary>
        /// Initialize new animation clip with the given name and index.
        /// </summary>
        /// <param name="index">glTF animation clip index.</param>
        /// <param name="name">glTF animation clip name.</param>
        void AddClip(int index, string name);

        /// <summary>
        /// Adds a translation animation curve.
        /// </summary>
        /// <param name="clipIndex">glTF animation clip index.</param>
        /// <param name="targetNode">glTF index of the targeted node.</param>
        /// <param name="nodeHierarchyInfo">Can be used to query hierarchical information and
        /// build an animation path string.</param>
        /// <param name="times">Time values.</param>
        /// <param name="values">Output translation values.</param>
        /// <param name="interpolationType">Interpolation type.</param>
        void AddTranslationCurves(
            int clipIndex,
            int targetNode,
            INodeHierarchyInfo nodeHierarchyInfo,
            NativeArray<float>.ReadOnly times,
            NativeArray<float3>.ReadOnly values,
            InterpolationType interpolationType
        );

        /// <summary>
        /// Adds a rotation animation curve.
        /// </summary>
        /// <param name="clipIndex">glTF animation clip index.</param>
        /// <param name="targetNode">glTF index of the targeted node.</param>
        /// <param name="nodeHierarchyInfo">Can be used to query hierarchical information and
        /// build an animation path string.</param>
        /// <param name="times">Time values.</param>
        /// <param name="values">Output rotation values.</param>
        /// <param name="interpolationType">Interpolation type.</param>
        void AddRotationCurves(
            int clipIndex,
            int targetNode,
            INodeHierarchyInfo nodeHierarchyInfo,
            NativeArray<float>.ReadOnly times,
            NativeArray<quaternion>.ReadOnly values,
            InterpolationType interpolationType
        );

        /// <summary>
        /// Adds a local scale animation curve.
        /// </summary>
        /// <param name="clipIndex">glTF animation clip index.</param>
        /// <param name="targetNode">glTF index of the targeted node.</param>
        /// <param name="nodeHierarchyInfo">Can be used to query hierarchical information and
        /// build an animation path string.</param>
        /// <param name="times">Time values.</param>
        /// <param name="values">Output scale values.</param>
        /// <param name="interpolationType">Interpolation type.</param>
        void AddScaleCurves(
            int clipIndex,
            int targetNode,
            INodeHierarchyInfo nodeHierarchyInfo,
            NativeArray<float>.ReadOnly times,
            NativeArray<float3>.ReadOnly values,
            InterpolationType interpolationType
        );

        /// <summary>
        /// Adds a morph target weight animation curve.
        /// </summary>
        /// <param name="clipIndex">glTF animation clip index.</param>
        /// <param name="targetNode">glTF index of the targeted node.</param>
        /// <param name="meshNumeration">Target mesh number. A glTF mesh is converted into one or more
        /// <see cref="MeshResult"/> which are numbered consecutively.
        /// <see cref="IInstantiator.AddPrimitive"/> is called once for each of those MeshResults
        /// and the meshNumeration matches.</param>
        /// <param name="meshName">Name of the targeted Unity mesh.</param>
        /// <param name="nodeHierarchyInfo">Can be used to query hierarchical information and
        /// build an animation path string.</param>
        /// <param name="times">Time values.</param>
        /// <param name="values">Output morph target weight values.</param>
        /// <param name="interpolationType">Interpolation type.</param>
        /// <param name="morphTargetNames">Morph targets' names.</param>
        void AddMorphTargetWeightCurves(
            int clipIndex,
            int targetNode,
            int meshNumeration,
            string meshName,
            INodeHierarchyInfo nodeHierarchyInfo,
            NativeArray<float>.ReadOnly times,
            NativeArray<float>.ReadOnly values,
            InterpolationType interpolationType,
            string[] morphTargetNames = null
        );

        /// <summary>
        /// Called when all animation curves have been added and the loader can finalize the loading process.
        /// </summary>
        void Finish();
    }
}
