// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

#if DRACO_IS_RECENT

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Draco;
using GLTFast.Logging;
using GLTFast.Schema;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
using Mesh = UnityEngine.Mesh;

namespace GLTFast
{

    class DracoMeshGenerator : MeshGeneratorBase
    {
        readonly bool m_NeedsNormals;
        readonly bool m_NeedsTangents;

        readonly bool m_HasMorphTargets;
        JobHandle m_MorphTargetsJobHandle;

        public override bool IsCompleted => base.IsCompleted && (!m_HasMorphTargets || m_MorphTargetsJobHandle.IsCompleted);

        public DracoMeshGenerator(
            IReadOnlyList<MeshPrimitive> primitives,
            IReadOnlyList<string> morphTargetNames,
            string meshName,
            IGltfReadable gltf,
            IGltfBuffers buffers,
            IDeferAgent deferAgent,
            ICodeLogger logger
            )
            : base(meshName)
        {
            var morphTargets = primitives[0].Targets;
            m_HasMorphTargets = morphTargets is { Count: > 0 };

            var vertexCount = 0;
            var primitivesCount = primitives.Count;
            var vertexIntervals = m_HasMorphTargets
                ? new int[primitivesCount + 1]
                : null;

            var bounds = new Bounds[primitivesCount];

            for (var index = 0; index < primitivesCount; index++)
            {
                var primitive = primitives[index];
                Assert.IsTrue(primitive.IsDracoCompressed);

                var posAccessor = buffers.GetAccessor(primitive.Attributes.Position.Value);

                if (m_HasMorphTargets)
                {
                    vertexIntervals[index] = vertexCount;
                }
                vertexCount += posAccessor.Count;

                if (bounds != null)
                {
                    var subMeshBounds = posAccessor.TryGetBounds();

                    if (subMeshBounds.HasValue)
                    {
                        bounds[index] = subMeshBounds.Value;
                    }
                    else
                    {
                        logger?.Error(LogCode.MeshBoundsMissing, primitive.Attributes.Position.ToString());
                        bounds = null;
                    }
                }

                if (!primitive.Material.HasValue)
                {
                    m_NeedsNormals = true;
                }
                else
                {
                    var material = gltf.GetSourceMaterial(primitive.Material.Value);
                    m_NeedsNormals |= material.RequiresNormals;
                    m_NeedsTangents |= material.RequiresTangents;
                }
            }

            if (m_HasMorphTargets)
            {
                vertexIntervals[^1] = vertexCount;
                InitializeMorphTargets(
                    primitives,
                    morphTargetNames,
                    vertexIntervals,
                    vertexCount,
                    morphTargets,
                    buffers,
                    deferAgent
                    );
            }

            m_CreationTask = Decode(primitives, buffers, bounds);
        }

        void InitializeMorphTargets(
            IReadOnlyList<MeshPrimitive> primitives,
            IReadOnlyList<string> morphTargetNames,
            int[] vertexIntervals,
            int vertexCount,
            List<MorphTarget> morphTargets,
            IGltfBuffers buffers,
            IDeferAgent deferAgent
            )
        {
            m_MorphTargetsGenerator = new MorphTargetsGenerator(
                vertexCount,
                primitives.Count,
                morphTargets.Count,
                morphTargetNames,
                morphTargets[0].Normal.HasValue,
                morphTargets[0].Tangent.HasValue,
                buffers,
                deferAgent
            );
            for (var subMesh = 0; subMesh < primitives.Count; subMesh++)
            {
                var primitive = primitives[subMesh];
                for (var morphTargetIndex = 0; morphTargetIndex < primitive.Targets.Count; morphTargetIndex++)
                {
                    var target = primitive.Targets[morphTargetIndex];
                    m_MorphTargetsGenerator.AddMorphTarget(vertexIntervals[subMesh], subMesh, morphTargetIndex, target);
                }
            }
            m_MorphTargetsJobHandle = m_MorphTargetsGenerator.GetJobHandle();
        }

        async Task<Mesh> Decode(
            IReadOnlyList<MeshPrimitive> primitives,
            IGltfBuffers buffers,
            Bounds[] bounds
            )
        {
            var bufferViews = new NativeArray<byte>.ReadOnly[primitives.Count];
            var attributesArray = new Attributes[primitives.Count];

            for (var index = 0; index < primitives.Count; index++)
            {
                var dracoExt = primitives[index].Extensions.DracoMeshCompression;
                bufferViews[index] = buffers.GetBufferView(dracoExt.BufferView, out _).AsNativeArrayReadOnly();
                attributesArray[index] = dracoExt.Attributes;
            }

            var mesh = await StartDecode(bufferViews, attributesArray, bounds == null);

            if (mesh is null)
            {
                return null;
            }

            if (bounds != null)
            {
                UpdateSubMeshBounds(0);
                var overallBounds = bounds[0];
                for (var i = 1; i < mesh.subMeshCount; i++)
                {
                    UpdateSubMeshBounds(i);
                    overallBounds.Encapsulate(bounds[i]);
                }
                mesh.bounds = overallBounds;
            }

            if (m_MorphTargetsGenerator != null)
            {
                while (!m_MorphTargetsJobHandle.IsCompleted)
                    await Task.Yield();
                m_MorphTargetsJobHandle.Complete();
                await m_MorphTargetsGenerator.ApplyOnMeshAndDispose(mesh);
            }

            mesh.name = m_MeshName;

#if GLTFAST_KEEP_MESH_DATA
            mesh.UploadMeshData(false);
#endif

            return mesh;

            void UpdateSubMeshBounds(int i)
            {
                var subMeshDescriptor = mesh.GetSubMesh(i);
                subMeshDescriptor.bounds = bounds[i];
                mesh.SetSubMesh(
                    i,
                    subMeshDescriptor,
                    MeshUpdateFlags.DontValidateIndices
                    | MeshUpdateFlags.DontResetBoneBounds
                    | MeshUpdateFlags.DontNotifyMeshUsers
                    | MeshUpdateFlags.DontRecalculateBounds
                );
            }
        }

        async Task<Mesh> StartDecode(
            NativeArray<byte>.ReadOnly[] data,
            Attributes[] attributesArray,
            bool calculateBounds
            )
        {
            var decodeSettings = DecodeSettings.ConvertSpace;
            if (m_NeedsTangents)
            {
                decodeSettings |= DecodeSettings.RequireNormalsAndTangents;
            }
            else if (m_NeedsNormals)
            {
                decodeSettings |= DecodeSettings.RequireNormals;
            }
            if (m_MorphTargetsGenerator != null)
            {
                decodeSettings |= DecodeSettings.ForceUnityVertexLayout;
            }
            if (!calculateBounds)
            {
                decodeSettings |= DecodeSettings.DontCalculateBounds;
            }

            return await DracoDecoder.DecodeMesh(data, decodeSettings, GenerateAttributeIdMaps(attributesArray));
        }

        static Dictionary<VertexAttribute, int>[] GenerateAttributeIdMaps(Attributes[] attributesArray)
        {
            var results = new Dictionary<VertexAttribute, int>[attributesArray.Length];
            for (var i = 0; i < attributesArray.Length; i++)
            {
                var attributes = attributesArray[i];
                var result = new Dictionary<VertexAttribute, int>();
                results[i] = result;
                if (attributes.Position.HasValue)
                    result[VertexAttribute.Position] = attributes.Position.Value;
                if (attributes.Normal.HasValue)
                    result[VertexAttribute.Normal] = attributes.Normal.Value;
                if (attributes.Tangent.HasValue)
                    result[VertexAttribute.Tangent] = attributes.Tangent.Value;
                if (attributes.Color0.HasValue)
                    result[VertexAttribute.Color] = attributes.Color0.Value;
                if (attributes.TexCoord0.HasValue)
                    result[VertexAttribute.TexCoord0] = attributes.TexCoord0.Value;
                if (attributes.TexCoord1.HasValue)
                    result[VertexAttribute.TexCoord1] = attributes.TexCoord1.Value;
                if (attributes.TexCoord2.HasValue)
                    result[VertexAttribute.TexCoord2] = attributes.TexCoord2.Value;
                if (attributes.TexCoord3.HasValue)
                    result[VertexAttribute.TexCoord3] = attributes.TexCoord3.Value;
                if (attributes.TexCoord4.HasValue)
                    result[VertexAttribute.TexCoord4] = attributes.TexCoord4.Value;
                if (attributes.TexCoord5.HasValue)
                    result[VertexAttribute.TexCoord5] = attributes.TexCoord5.Value;
                if (attributes.TexCoord6.HasValue)
                    result[VertexAttribute.TexCoord6] = attributes.TexCoord6.Value;
                if (attributes.TexCoord7.HasValue)
                    result[VertexAttribute.TexCoord7] = attributes.TexCoord7.Value;
                if (attributes.Weights0.HasValue)
                    result[VertexAttribute.BlendWeight] = attributes.Weights0.Value;
                if (attributes.Joints0.HasValue)
                    result[VertexAttribute.BlendIndices] = attributes.Joints0.Value;
            }

            return results;
        }
    }
}
#endif // DRACO_IS_RECENT
