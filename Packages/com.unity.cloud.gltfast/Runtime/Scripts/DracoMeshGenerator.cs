// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

#if DRACO_IS_RECENT

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Collections;
using Draco;
using GLTFast.Logging;
using GLTFast.Schema;
using Unity.Jobs;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
using Mesh = UnityEngine.Mesh;

namespace GLTFast {

    class DracoMeshGenerator : MeshGeneratorBase
    {
        readonly bool m_NeedsNormals;
        readonly bool m_NeedsTangents;

        readonly bool m_HasMorphTargets;
        JobHandle m_MorphTargetsJobHandle;

        public override bool IsCompleted => base.IsCompleted && (!m_HasMorphTargets || m_MorphTargetsJobHandle.IsCompleted);

        public DracoMeshGenerator(
            IReadOnlyList<MeshPrimitiveBase> primitives,
            string[] morphTargetNames,
            string meshName,
            GltfImportBase gltfImport
            )
            : base(meshName)
        {
            var morphTargets = primitives[0].targets;
            m_HasMorphTargets = morphTargets is { Length: > 0 };

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

                var posAccessor = ((IGltfBuffers)gltfImport).GetAccessor(primitive.attributes.POSITION);

                if (m_HasMorphTargets)
                {
                    vertexIntervals[index] = vertexCount;
                }
                vertexCount += posAccessor.count;

                if (bounds != null)
                {
                    var subMeshBounds = posAccessor.TryGetBounds();

                    if (subMeshBounds.HasValue)
                    {
                        bounds[index] = subMeshBounds.Value;
                    }
                    else
                    {
                        gltfImport.Logger?.Error(LogCode.MeshBoundsMissing, primitive.attributes.POSITION.ToString());
                        bounds = null;
                    }
                }

                if (primitive.material < 0)
                {
                    m_NeedsNormals = true;
                }
                else
                {
                    var material = gltfImport.GetSourceMaterial(primitive.material);
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
                    gltfImport
                    );
            }

            m_CreationTask = Decode(primitives, gltfImport, bounds);
        }

        void InitializeMorphTargets(
            IReadOnlyList<MeshPrimitiveBase> primitives,
            string[] morphTargetNames,
            int[] vertexIntervals,
            int vertexCount,
            MorphTarget[] morphTargets,
            GltfImportBase gltfImport
            )
        {
            m_MorphTargetsGenerator = new MorphTargetsGenerator(
                vertexCount,
                primitives.Count,
                morphTargets.Length,
                morphTargetNames,
                morphTargets[0].NORMAL >= 0,
                morphTargets[0].TANGENT >= 0,
                gltfImport
            );
            for (var subMesh = 0; subMesh < primitives.Count; subMesh++)
            {
                var primitive = primitives[subMesh];
                for (var morphTargetIndex = 0; morphTargetIndex < primitive.targets.Length; morphTargetIndex++)
                {
                    var target = primitive.targets[morphTargetIndex];
                    m_MorphTargetsGenerator.AddMorphTarget(vertexIntervals[subMesh], subMesh, morphTargetIndex, target);
                }
            }
            m_MorphTargetsJobHandle = m_MorphTargetsGenerator.GetJobHandle();
        }

        async Task<Mesh> Decode(
            IReadOnlyList<MeshPrimitiveBase> primitives,
            IGltfBuffers buffers,
            Bounds[] bounds
            )
        {
            var bufferViews = new NativeArray<byte>.ReadOnly[primitives.Count];
            var attributesArray = new Attributes[primitives.Count];

            for (var index = 0; index < primitives.Count; index++)
            {
                var dracoExt = primitives[index].Extensions.KHR_draco_mesh_compression;
                bufferViews[index] = buffers.GetBufferView(dracoExt.bufferView, out _).AsNativeArrayReadOnly();
                attributesArray[index] = dracoExt.attributes;
            }

            var mesh = await StartDecode(bufferViews, attributesArray, bounds==null);

            if (mesh is null) {
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
                if (attributes.POSITION >= 0)
                    result[VertexAttribute.Position] = attributes.POSITION;
                if (attributes.NORMAL >= 0)
                    result[VertexAttribute.Normal] = attributes.NORMAL;
                if (attributes.TANGENT >= 0)
                    result[VertexAttribute.Tangent] = attributes.TANGENT;
                if (attributes.COLOR_0 >= 0)
                    result[VertexAttribute.Color] = attributes.COLOR_0;
                if (attributes.TEXCOORD_0 >= 0)
                    result[VertexAttribute.TexCoord0] = attributes.TEXCOORD_0;
                if (attributes.TEXCOORD_1 >= 0)
                    result[VertexAttribute.TexCoord1] = attributes.TEXCOORD_1;
                if (attributes.TEXCOORD_2 >= 0)
                    result[VertexAttribute.TexCoord2] = attributes.TEXCOORD_2;
                if (attributes.TEXCOORD_3 >= 0)
                    result[VertexAttribute.TexCoord3] = attributes.TEXCOORD_3;
                if (attributes.TEXCOORD_4 >= 0)
                    result[VertexAttribute.TexCoord4] = attributes.TEXCOORD_4;
                if (attributes.TEXCOORD_5 >= 0)
                    result[VertexAttribute.TexCoord5] = attributes.TEXCOORD_5;
                if (attributes.TEXCOORD_6 >= 0)
                    result[VertexAttribute.TexCoord6] = attributes.TEXCOORD_6;
                if (attributes.TEXCOORD_7 >= 0)
                    result[VertexAttribute.TexCoord7] = attributes.TEXCOORD_7;
                if (attributes.WEIGHTS_0 >= 0)
                    result[VertexAttribute.BlendWeight] = attributes.WEIGHTS_0;
                if (attributes.JOINTS_0 >= 0)
                    result[VertexAttribute.BlendIndices] = attributes.JOINTS_0;
            }

            return results;
        }
    }
}
#endif // DRACO_IS_RECENT
