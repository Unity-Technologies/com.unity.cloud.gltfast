// SPDX-FileCopyrightText: 2024 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

#nullable enable
using System;
using System.Collections.Generic;
using GLTFast.Schema;

namespace GLTFast
{
    readonly struct VertexBufferDescriptor : IEquatable<VertexBufferDescriptor>
    {
        readonly bool m_HasNormals;
        readonly bool m_HasTangents;

        readonly int m_TexCoordCount;
        readonly bool m_HasColors;

        readonly bool m_HasBones;
        readonly int m_MorphTargetCount;

        // Draco for Unity decodes all primitives of a cluster into one vertex buffer and copies
        // each primitive's decoded bytes verbatim, so it cannot merge primitives whose attributes differ in
        // component type (e.g. a float TEXCOORD_0 next to a quantized one). It logs "Inconsistent ... attribute
        // format in submeshes!" and the copy overruns the buffer (ArgumentOutOfRangeException in GetDracoDataJob).
        // Folding the accessor formats into the cluster key splits such primitives into separate meshes.
        // Always 0 for non-Draco primitives, whose clustering is unchanged (MeshGenerator converts formats itself).
        readonly int m_DracoFormatKey;

        VertexBufferDescriptor(
            bool hasNormals,
            bool hasTangents,
            int texCoordCount,
            bool hasColors,
            bool hasBones,
            int morphTargetCount,
            int dracoFormatKey
            )
        {
            m_HasNormals = hasNormals;
            m_HasTangents = hasTangents;
            m_TexCoordCount = texCoordCount;
            m_HasColors = hasColors;
            m_HasBones = hasBones;
            m_MorphTargetCount = morphTargetCount;
            m_DracoFormatKey = dracoFormatKey;
        }

        public static VertexBufferDescriptor FromPrimitive(
            MeshPrimitiveBase primitive,
            IReadOnlyList<AccessorBase>? accessors = null
            )
        {
            return new VertexBufferDescriptor(
                primitive.attributes.NORMAL >= 0,
                primitive.attributes.TANGENT >= 0,
                primitive.attributes.GetTexCoordsCount(),
                primitive.attributes.COLOR_0 >= 0,
                primitive.attributes.WEIGHTS_0 >= 0 && primitive.attributes.JOINTS_0 >= 0,
                primitive.targets?.Length ?? 0,
                primitive.IsDracoCompressed && accessors != null
                    ?DracoFormatKey(primitive.attributes, accessors)
                    : 0
            );
        }

        static int DracoFormatKey(Attributes attributes, IReadOnlyList<AccessorBase> accessors)
        {
            var key = new HashCode();
            Add(attributes.POSITION);
            Add(attributes.NORMAL);
            Add(attributes.TANGENT);
            Add(attributes.COLOR_0);
            Add(attributes.TEXCOORD_0);
            Add(attributes.TEXCOORD_1);
            Add(attributes.TEXCOORD_2);
            Add(attributes.TEXCOORD_3);
            Add(attributes.TEXCOORD_4);
            Add(attributes.TEXCOORD_5);
            Add(attributes.TEXCOORD_6);
            Add(attributes.TEXCOORD_7);
            return key.ToHashCode();

            void Add(int accessorIndex)
            {
                if (accessorIndex < 0 || accessorIndex >= accessors.Count)
                {
                    key.Add(-1);
                    return;
                }
                var accessor = accessors[accessorIndex];
                key.Add((int)accessor.componentType);
                key.Add(accessor.normalized);
            }
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                m_HasNormals,
                m_HasTangents,
                m_TexCoordCount,
                m_HasColors,
                m_HasBones,
                m_MorphTargetCount,
                m_DracoFormatKey
            );
        }

        public override bool Equals(object? obj) => obj is VertexBufferDescriptor other && Equals(other);

        public bool Equals(VertexBufferDescriptor other)
        {
            return m_HasNormals == other.m_HasNormals
                && m_HasTangents == other.m_HasTangents
                && m_TexCoordCount == other.m_TexCoordCount
                && m_HasColors == other.m_HasColors
                && m_HasBones == other.m_HasBones
                && m_MorphTargetCount == other.m_MorphTargetCount
                && m_DracoFormatKey == other.m_DracoFormatKey;
        }

        public static bool operator ==(VertexBufferDescriptor lhs, VertexBufferDescriptor rhs) => lhs.Equals(rhs);

        public static bool operator !=(VertexBufferDescriptor lhs, VertexBufferDescriptor rhs) => !(lhs == rhs);
    }
}
