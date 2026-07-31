// SPDX-FileCopyrightText: 2025 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;

namespace Unity.Cloud.Gltfast.Tests
{
    static class TestMeshGenerator
    {
        internal static Mesh GenerateCylinderMesh(uint triangleCount, float height = 1f, float radius = .5f)
        {
            triangleCount += triangleCount % 2;
            Assert.IsTrue(triangleCount >= 6);
            var m = new Mesh
            {
                name = "Cylinder"
            };

            // Arrays to hold mesh data
            var vertexCount = triangleCount + 2;
            var positions = new Vector3[vertexCount];
            var normals = new Vector3[vertexCount];
            var uv = new Vector2[vertexCount];
            var indices = new int[triangleCount * 3];

            m.indexFormat = vertexCount > ushort.MaxValue ? IndexFormat.UInt32 : IndexFormat.UInt16;

            for (var i = 0; i < triangleCount; i++)
            {
                var top = (i & 1) == 0;
                float y;
                indices[i * 3] = i;
                var angle = (float)(i * 2 * math.PI_DBL / triangleCount);
                if (top)
                {
                    y = 1;
                    indices[i * 3 + 1] = i + 2;
                    indices[i * 3 + 2] = i + 1;
                }
                else
                {
                    y = 0;
                    indices[i * 3 + 1] = i + 1;
                    indices[i * 3 + 2] = i + 2;
                }

                var x = math.cos(angle);
                var z = math.sin(angle);
                positions[i] = new Vector3(x * radius, y, z * radius);
                normals[i] = new Vector3(x, 0, z);
                uv[i] = new Vector2(i / (float)triangleCount, y);
            }

            positions[triangleCount] = positions[0];
            normals[triangleCount] = normals[0];
            uv[triangleCount] = new Vector2(1, 1);

            positions[triangleCount + 1] = positions[1];
            normals[triangleCount + 1] = normals[1];
            uv[triangleCount + 1] = new Vector2((triangleCount + 1) / (float)triangleCount, 0);

            // Assign arrays to mesh
            m.vertices = positions;
            m.normals = normals;
            m.uv = uv;
            m.triangles = indices;

            m.RecalculateTangents();
            m.RecalculateBounds();

            return m;
        }

        internal static Mesh GenerateSubMeshCube(IndexFormat indexFormat = IndexFormat.UInt16)
        {
            const float lengthHalf = .5f;
            const MeshTopology topology = MeshTopology.Quads;
            var quads = topology == MeshTopology.Quads;

            var m = new Mesh
            {
                name = "Cube"
            };

            var indicesPerFace = quads ? 4 : 6;
            var positions = new Vector3[24];
            var normals = new Vector3[24];
            var uv = new Vector2[24];

            for (var i = 0; i < 8; i++)
            {
                var a = (i & 0b100) == 0;
                var b = (i & 0b10) == 0;
                var c = ((i + 1) & 0b10) == 0;

                positions[i] = new Vector3(
                    a ? -lengthHalf : lengthHalf,
                    b ? lengthHalf : -lengthHalf,
                    c ? lengthHalf : -lengthHalf
                    );
                normals[i] = a ? Vector3.left : Vector3.right;
                uv[i] = new Vector2(
                    a ^ c ? 1 : 0,
                    b ? 0 : 1
                );

                positions[i + 8] = new Vector3(
                    b ? lengthHalf : -lengthHalf,
                    c ? lengthHalf : -lengthHalf,
                    a ? -lengthHalf : lengthHalf
                );
                normals[i + 8] = a ? Vector3.back : Vector3.forward;
                uv[i + 8] = new Vector2(
                    a ^ b ? 0 : 1,
                    c ? 0 : 1
                );

                positions[i + 16] = new Vector3(
                    b ? lengthHalf : -lengthHalf,
                    a ? lengthHalf : -lengthHalf,
                    c ? lengthHalf : -lengthHalf
                );
                normals[i + 16] = a ? Vector3.up : Vector3.down;
                uv[i + 16] = new Vector2(
                    a ^ b ? 0 : 1,
                    c ? 0 : 1
                );
            }

            // Assign arrays to mesh
            m.indexFormat = indexFormat;
            m.vertices = positions;
            m.normals = normals;
            m.uv = uv;
            m.subMeshCount = 6;
            for (var i = 0; i < m.subMeshCount; i++)
            {
                m.SetIndices(
                    i % 2 == 0 ? new[] { 0, 1, 2, 3 } : new[] { 0, 3, 2, 1 },
                    MeshTopology.Quads, i);
                var subMesh = new SubMeshDescriptor
                {
                    indexStart = indicesPerFace * i,
                    indexCount = indicesPerFace,
                    topology = topology,
                    baseVertex = indicesPerFace * i,
                    firstVertex = indicesPerFace * i,
                    vertexCount = indicesPerFace
                };
                m.SetSubMesh(i, subMesh);
            }

            m.RecalculateTangents();
            m.RecalculateBounds();

            return m;
        }
    }
}
