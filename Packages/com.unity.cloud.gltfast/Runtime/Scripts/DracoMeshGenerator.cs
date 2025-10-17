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

    class DracoMeshGenerator : MeshGeneratorBase {

        Bounds? m_Bounds;

        readonly bool m_NeedsNormals;
        readonly bool m_NeedsTangents;

        readonly bool m_HasMorphTargets;
        JobHandle m_MorphTargetsJobHandle;

        public override bool IsCompleted => base.IsCompleted && (!m_HasMorphTargets || m_MorphTargetsJobHandle.IsCompleted);

        public DracoMeshGenerator(
            IReadOnlyList<MeshPrimitiveBase> primitives,
            SubMeshAssignment[] subMeshAssignments,
            string[] morphTargetNames,
            string meshName,
            GltfImportBase gltfImport
            )
            : base(meshName)
        {
            // TODO: Add support for decoding multiple primitives into one mesh with sub-meshes.
            Assert.IsTrue(
                primitives.Count == 1,
                "Draco-compressed, multi primitives/sub-mesh meshes are not supported."
                );

            Assert.IsNull(
                subMeshAssignments,
                "Draco-compressed, multi primitives/sub-mesh meshes are not supported."
                );

            var morphTargets = primitives[0].targets;
            m_HasMorphTargets = morphTargets != null && morphTargets.Length > 0;

            var vertexCount = 0;
            var vertexIntervals = m_HasMorphTargets
                ? new int[primitives.Count + 1]
                : null;

            for (var index = 0; index < primitives.Count; index++)
            {
                var primitive = primitives[index];
                Assert.IsTrue(primitive.IsDracoCompressed);

                var posAccessor = ((IGltfBuffers)gltfImport).GetAccessor(primitive.attributes.POSITION);

                if (m_HasMorphTargets)
                {
                    vertexIntervals[index] = vertexCount;
                }
                vertexCount += posAccessor.count;

                var bounds = posAccessor.TryGetBounds();

                if (bounds.HasValue)
                {
                    m_Bounds = bounds.Value;
                }
                else
                {
                    gltfImport.Logger?.Error(LogCode.MeshBoundsMissing, primitive.attributes.POSITION.ToString());
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
                InitializeMorphTargets(
                    primitives,
                    morphTargetNames,
                    vertexIntervals,
                    vertexCount,
                    morphTargets,
                    gltfImport
                    );
            }

            m_CreationTask = Decode(primitives, gltfImport);
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
            vertexIntervals[vertexIntervals.Length-1] = vertexCount;
            m_MorphTargetsGenerator = new MorphTargetsGenerator(
                vertexCount,
                1,
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
                    m_MorphTargetsGenerator.AddMorphTarget( 0, morphTargetIndex, target);
                }
            }
            m_MorphTargetsJobHandle = m_MorphTargetsGenerator.GetJobHandle();
        }

        async Task<Mesh> Decode(IReadOnlyList<MeshPrimitiveBase> primitives, IGltfBuffers buffers)
        {
            Mesh mesh = null;
            foreach (var primitive in primitives)
            {
                var dracoExt = primitive.Extensions.KHR_draco_mesh_compression;
                var buffer = buffers.GetBufferView(dracoExt.bufferView, out _);
                mesh = await StartDecode(buffer.AsNativeArrayReadOnly(), dracoExt.attributes);
            }

            if (mesh is null) {
                return null;
            }

            if (m_Bounds.HasValue) {
                mesh.bounds = m_Bounds.Value;

                // Setting the sub-meshes' bounds to the overall bounds
                // Calculating the actual sub-mesh bounds (by iterating the verts referenced
                // by the sub-mesh indices) would be slow. Also, hardly any glTFs re-use
                // the same vertex buffer across primitives of a node (which is the
                // only way a mesh can have sub-meshes)
                for (var i = 0; i < mesh.subMeshCount; i++) {
                    var subMeshDescriptor = mesh.GetSubMesh(i);
                    subMeshDescriptor.bounds = m_Bounds.Value;
                    mesh.SetSubMesh(
                        i,
                        subMeshDescriptor,
                        MeshUpdateFlags.DontValidateIndices
                        | MeshUpdateFlags.DontResetBoneBounds
                        | MeshUpdateFlags.DontNotifyMeshUsers
                        | MeshUpdateFlags.DontRecalculateBounds
                        );
                }
            } else {
                mesh.RecalculateBounds();
            }

            if (m_MorphTargetsGenerator != null) {
                await m_MorphTargetsGenerator.ApplyOnMeshAndDispose(mesh);
            }

            mesh.name = m_MeshName;

#if GLTFAST_KEEP_MESH_DATA
            mesh.UploadMeshData(false);
#endif

            return mesh;
        }

        async Task<Mesh> StartDecode(NativeArray<byte>.ReadOnly data, Attributes dracoAttributes)
        {
            var flags = DecodeSettings.ConvertSpace;
            if (m_NeedsTangents)
            {
                flags |= DecodeSettings.RequireNormalsAndTangents;
            }
            else if (m_NeedsNormals)
            {
                flags |= DecodeSettings.RequireNormals;
            }
            if (m_MorphTargetsGenerator != null)
            {
                flags |= DecodeSettings.ForceUnityVertexLayout;
            }

            return await DracoDecoder.DecodeMesh(data, flags, GenerateAttributeIdMap(dracoAttributes));
        }

        static Dictionary<VertexAttribute, int> GenerateAttributeIdMap(Attributes attributes)
        {
            var result = new Dictionary<VertexAttribute, int>();
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
            return result;
        }
    }
}
#endif // DRACO_IS_RECENT
