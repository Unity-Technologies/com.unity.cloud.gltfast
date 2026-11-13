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
using Unity.Collections.LowLevel.Unsafe;
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
            IReadOnlyList<MeshPrimitiveBase> primitives,
            string[] morphTargetNames,
            string meshName,
            IGltfReadable gltf,
            IGltfBuffers buffers,
            IDeferAgent deferAgent,
            ICodeLogger logger
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

                var posAccessor = buffers.GetAccessor(primitive.attributes.POSITION);

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
                        logger?.Error(LogCode.MeshBoundsMissing, primitive.attributes.POSITION.ToString());
                        bounds = null;
                    }
                }

                if (primitive.material < 0)
                {
                    m_NeedsNormals = true;
                }
                else
                {
                    var material = gltf.GetSourceMaterial(primitive.material);
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
            IReadOnlyList<MeshPrimitiveBase> primitives,
            string[] morphTargetNames,
            int[] vertexIntervals,
            int vertexCount,
            MorphTarget[] morphTargets,
            IGltfBuffers buffers,
            IDeferAgent deferAgent
            )
        {
            m_MorphTargetsGenerator = new MorphTargetsGenerator(
                vertexCount,
                primitives.Count,
                morphTargets.Length,
                morphTargetNames,
                morphTargets[0].NORMAL >= 0,
                morphTargets[0].TANGENT >= 0,
                buffers,
                deferAgent
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

            var mesh = await StartDecode(bufferViews, attributesArray, bounds == null);

            if (mesh is null)
            {
                return null;
            }

            if (ApplyNormalizedFormats(mesh, primitives[0].attributes, buffers)
                && bounds == null)
            {
                // Draco measured the bounds from the raw integers. Mesh.RecalculateBounds refuses any position
                // format but Float32 ("Can't recalculate bounds ..."), so measure the converted vertices instead.
                RecalculateBoundsFromVertices(mesh);
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

        /// <summary>
        /// Draco for Unity resolves attributes it is handed by id (which glTFast always does) without
        /// their `normalized` flag, so a KHR_mesh_quantization attribute declared normalized (UNSIGNED_SHORT UVs,
        /// BYTE normals, SHORT positions) lands in the vertex buffer as a raw integer format. The GPU then reads UVs
        /// of 0..65535 instead of 0..1, and every texture, palette colour and alpha sampled through them collapses to
        /// one texel. The decoded bytes are already right; only the format label is wrong, so this relabels the
        /// affected attributes (UInt16 -> UNorm16 etc.) per the glTF accessors and re-uploads the same bytes.
        /// The cluster key in <see cref="VertexBufferDescriptor"/> guarantees every primitive of this mesh shares the
        /// first one's accessor formats.
        /// </summary>
        /// <returns>True if any attribute was relabelled.</returns>
        static bool ApplyNormalizedFormats(Mesh mesh, Attributes attributes, IGltfBuffers buffers)
        {
            var descriptors = mesh.GetVertexAttributes();
            var changed = false;
            List<VertexAttributeDescriptor> flippedTexCoords = null;
            for (var i = 0; i < descriptors.Length; i++)
            {
                var accessorIndex = AccessorIndex(attributes, descriptors[i].attribute);
                if (accessorIndex < 0 || !buffers.GetAccessor(accessorIndex).normalized)
                {
                    continue;
                }
                var normalizedFormat = ToNormalized(descriptors[i].format);
                if (normalizedFormat != descriptors[i].format)
                {
                    if (IsTexCoord(descriptors[i].attribute)
                        && (descriptors[i].format == VertexAttributeFormat.UInt8
                            || descriptors[i].format == VertexAttributeFormat.UInt16))
                    {
                        flippedTexCoords ??= new List<VertexAttributeDescriptor>();
                        flippedTexCoords.Add(descriptors[i]);
                    }
                    descriptors[i].format = normalizedFormat;
                    changed = true;
                }
            }

            if (!changed)
            {
                return false;
            }

            var vertexCount = mesh.vertexCount;
            NativeArray<byte>[] streams;
            using (var meshData = Mesh.AcquireReadOnlyMeshData(mesh))
            {
                streams = new NativeArray<byte>[meshData[0].vertexBufferCount];
                for (var stream = 0; stream < streams.Length; stream++)
                {
                    streams[stream] = new NativeArray<byte>(meshData[0].GetVertexData<byte>(stream), Allocator.Temp);
                }
            }

            // Draco's ConvertSpace (always on, see StartDecode) flips V as `1 - v` in the attribute's own integer
            // type, so a raw UNSIGNED_SHORT v comes out as (1 - v) mod 65536 instead of 65535 - v: one step off
            // everywhere, and v = 0 wraps to 1 (the opposite edge of the texture) instead of 65535. Verified
            // against Google's draco3d: raw + flipped == 1 (mod 65536) for every vertex.
            // The correct value is the flipped one minus 2, modulo the type's range. U is not touched by Draco.
            // Signed normalized UVs (BYTE/SHORT) are left as decoded: 1 - v does not fit their range at all.
            if (flippedTexCoords != null)
            {
                foreach (var texCoord in flippedTexCoords)
                {
                    FixFlippedV(
                        streams[texCoord.stream],
                        mesh.GetVertexBufferStride(texCoord.stream),
                        mesh.GetVertexAttributeOffset(texCoord.attribute),
                        texCoord.format == VertexAttributeFormat.UInt16,
                        vertexCount
                    );
                }
            }

            // Same strides and offsets (each normalized format is its integer twin's size), so the bytes fit as-is.
            // Shrinking to zero vertices first stops Unity from trying to convert the old data into the new
            // layout, which it can't do between integer and normalized formats: it would log
            // "Unsupported conversion of vertex data" once per attribute. Submeshes, indices and bone weights survive.
            mesh.SetVertexBufferParams(0, descriptors);
            mesh.SetVertexBufferParams(vertexCount, descriptors);
            const MeshUpdateFlags flags = MeshUpdateFlags.DontValidateIndices
                | MeshUpdateFlags.DontResetBoneBounds
                | MeshUpdateFlags.DontNotifyMeshUsers
                | MeshUpdateFlags.DontRecalculateBounds;
            for (var stream = 0; stream < streams.Length; stream++)
            {
                mesh.SetVertexBufferData(streams[stream], 0, 0, streams[stream].Length, stream, flags);
                streams[stream].Dispose();
            }
            return true;
        }

        static void RecalculateBoundsFromVertices(Mesh mesh)
        {
            var vertices = new List<Vector3>(mesh.vertexCount);
            mesh.GetVertices(vertices);
            if (vertices.Count == 0)
            {
                return;
            }
            const MeshUpdateFlags flags = MeshUpdateFlags.DontValidateIndices
                | MeshUpdateFlags.DontResetBoneBounds
                | MeshUpdateFlags.DontNotifyMeshUsers
                | MeshUpdateFlags.DontRecalculateBounds;
            var overall = new Bounds(vertices[0], Vector3.zero);
            for (var subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
            {
                var descriptor = mesh.GetSubMesh(subMesh);
                if (descriptor.vertexCount <= 0)
                {
                    continue;
                }
                var subBounds = new Bounds(vertices[descriptor.firstVertex], Vector3.zero);
                for (var i = descriptor.firstVertex; i < descriptor.firstVertex + descriptor.vertexCount; i++)
                {
                    subBounds.Encapsulate(vertices[i]);
                }
                descriptor.bounds = subBounds;
                mesh.SetSubMesh(subMesh, descriptor, flags);
                overall.Encapsulate(subBounds);
            }
            mesh.bounds = overall;
        }

        static unsafe void FixFlippedV(NativeArray<byte> stream, int stride, int offset, bool is16Bit, int vertexCount)
        {
            var data = (byte*)NativeArrayUnsafeUtility.GetUnsafePtr(stream);
            if (is16Bit)
            {
                // V is the second component: two bytes past U.
                for (var i = 0; i < vertexCount; i++)
                {
                    var v = (ushort*)(data + i * stride + offset + 2);
                    *v = (ushort)(*v - 2);
                }
            }
            else
            {
                for (var i = 0; i < vertexCount; i++)
                {
                    var v = data + i * stride + offset + 1;
                    *v = (byte)(*v - 2);
                }
            }
        }

        static bool IsTexCoord(VertexAttribute attribute)
        {
            return attribute >= VertexAttribute.TexCoord0 && attribute <= VertexAttribute.TexCoord7;
        }

        static VertexAttributeFormat ToNormalized(VertexAttributeFormat format)
        {
            switch (format)
            {
                case VertexAttributeFormat.UInt8: return VertexAttributeFormat.UNorm8;
                case VertexAttributeFormat.SInt8: return VertexAttributeFormat.SNorm8;
                case VertexAttributeFormat.UInt16: return VertexAttributeFormat.UNorm16;
                case VertexAttributeFormat.SInt16: return VertexAttributeFormat.SNorm16;
                default: return format;
            }
        }

        static int AccessorIndex(Attributes attributes, VertexAttribute attribute)
        {
            switch (attribute)
            {
                case VertexAttribute.Position: return attributes.POSITION;
                case VertexAttribute.Normal: return attributes.NORMAL;
                case VertexAttribute.Tangent: return attributes.TANGENT;
                case VertexAttribute.Color: return attributes.COLOR_0;
                case VertexAttribute.TexCoord0: return attributes.TEXCOORD_0;
                case VertexAttribute.TexCoord1: return attributes.TEXCOORD_1;
                case VertexAttribute.TexCoord2: return attributes.TEXCOORD_2;
                case VertexAttribute.TexCoord3: return attributes.TEXCOORD_3;
                case VertexAttribute.TexCoord4: return attributes.TEXCOORD_4;
                case VertexAttribute.TexCoord5: return attributes.TEXCOORD_5;
                case VertexAttribute.TexCoord6: return attributes.TEXCOORD_6;
                case VertexAttribute.TexCoord7: return attributes.TEXCOORD_7;
                // Joints/weights are left alone: JOINTS_0 is never normalized, and the bone data does not live in
                // the vertex buffer once Draco has applied it.
                default: return -1;
            }
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
