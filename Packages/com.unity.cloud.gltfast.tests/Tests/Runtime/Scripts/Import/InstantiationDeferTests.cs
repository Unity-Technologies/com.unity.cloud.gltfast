// SPDX-FileCopyrightText: 2026 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;

namespace GLTFast.Tests.Import
{
    /// <summary>
    /// Tests that scene instantiation honors the defer agent per mesh assignment (not just per
    /// node) and that yielding at those points does not alter the instantiation result.
    /// A node referencing a mesh whose primitives use differing vertex buffer layouts gets one
    /// mesh assignment per layout cluster, so a single node can carry many assignments.
    /// </summary>
    [Category("Import")]
    class InstantiationDeferTests
    {
        /// <summary>Primitive layout clusters in the test asset's single mesh (see the JSON
        /// below: POSITION+NORMAL, +TEXCOORD_0, +COLOR_0, +TEXCOORD_1 — four distinct layouts,
        /// each becoming its own mesh assignment).</summary>
        const int k_AssignmentsPerMeshNode = 4;

        /// <summary>Nodes in the test asset referencing the mesh (root and grandchild).</summary>
        const int k_MeshNodes = 2;

        /// <summary>Total nodes in the test asset (root, child, grandchild).</summary>
        const int k_NodeCount = 3;

        /// <summary>
        /// A defer agent that always defers and counts how often the loading/instantiation
        /// procedure offered to yield.
        /// </summary>
        class CountingDeferAgent : IDeferAgent
        {
            public int BreakPointCount { get; private set; }

            public void Reset() => BreakPointCount = 0;

            public bool ShouldDefer() => true;
            public bool ShouldDefer(float duration) => true;

            public async Task BreakPoint()
            {
                BreakPointCount++;
                await Task.Yield();
            }

            public async Task BreakPoint(float duration)
            {
                BreakPointCount++;
                await Task.Yield();
            }
        }

        [UnityEngine.TestTools.UnityTest]
        public IEnumerator InstantiationYieldsPerMeshAssignment()
        {
            yield return AsyncWrapper.WaitForTask(InstantiationYieldsPerMeshAssignmentInternal());
        }

        static async Task InstantiationYieldsPerMeshAssignmentInternal()
        {
            var deferAgent = new CountingDeferAgent();
            using var gltf = new GltfImport(deferAgent: deferAgent);
            var success = await gltf.LoadGltfJson(CreateMultiLayoutMeshJson());
            Assert.IsTrue(success, "Loading the multi-layout test asset failed.");

            var root = new GameObject(nameof(InstantiationYieldsPerMeshAssignment));
            try
            {
                deferAgent.Reset();
                success = await gltf.InstantiateMainSceneAsync(root.transform);
                Assert.IsTrue(success, "Instantiation failed.");

                // Hierarchy creation and population each iterate all nodes with a break point per
                // node; population additionally breaks once per mesh assignment. A regression to
                // per-node-only yielding lands well below this floor.
                var expectedMinimum = 2 * k_NodeCount + k_MeshNodes * k_AssignmentsPerMeshNode;
                Assert.GreaterOrEqual(
                    deferAgent.BreakPointCount,
                    expectedMinimum,
                    "Instantiation did not offer to yield once per mesh assignment. A node with " +
                    "many mesh assignments must not populate them all in one un-yielding stretch."
                );
            }
            finally
            {
                UnityEngine.Object.Destroy(root);
            }
        }

        [UnityEngine.TestTools.UnityTest]
        public IEnumerator DeferredInstantiationMatchesUninterrupted()
        {
            yield return AsyncWrapper.WaitForTask(DeferredInstantiationMatchesUninterruptedInternal());
        }

        static async Task DeferredInstantiationMatchesUninterruptedInternal()
        {
            var json = CreateMultiLayoutMeshJson();

            // The imports stay alive until AFTER the comparison — disposing a GltfImport destroys
            // the meshes it owns, which would strip the described hierarchies mid-test.
            var (uninterruptedGltf, uninterrupted) = await InstantiateAsync(json, new UninterruptedDeferAgent());
            var (deferredGltf, deferred) = await InstantiateAsync(json, new CountingDeferAgent());
            try
            {
                // Yielding to the player loop between mesh assignments (and nodes) must not
                // change the result: same hierarchy, same renderers, same mesh/material shapes.
                var expected = DescribeHierarchy(uninterrupted);
                var actual = DescribeHierarchy(deferred);
                if (expected != actual)
                    Debug.Log($"UNINTERRUPTED:\n{expected}\n\nDEFERRED:\n{actual}");
                Assert.AreEqual(expected, actual,
                    "Frame-deferred instantiation produced a different scene than uninterrupted instantiation.");
            }
            finally
            {
                uninterruptedGltf.Dispose();
                deferredGltf.Dispose();
                UnityEngine.Object.Destroy(uninterrupted);
                UnityEngine.Object.Destroy(deferred);
            }
        }

        static async Task<(GltfImport gltf, GameObject root)> InstantiateAsync(string json, IDeferAgent deferAgent)
        {
            var gltf = new GltfImport(deferAgent: deferAgent);
            var success = await gltf.LoadGltfJson(json);
            Assert.IsTrue(success, "Loading the multi-layout test asset failed.");
            var root = new GameObject("InstantiationResult");
            success = await gltf.InstantiateMainSceneAsync(root.transform);
            Assert.IsTrue(success, "Instantiation failed.");
            return (gltf, root);
        }

        /// <summary>
        /// Stable, order-independent description of an instantiated hierarchy: one line per
        /// transform (path relative to the root) plus its renderer's mesh shape when present.
        /// Parent paths double as the parent-before-child check — an orphaned or re-parented
        /// node changes its path.
        /// </summary>
        static string DescribeHierarchy(GameObject root)
        {
            var lines = new List<string>();
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
            {
                if (transform == root.transform)
                    continue;
                var line = GetPath(transform, root.transform);
                var renderer = transform.GetComponent<MeshRenderer>();
                var filter = transform.GetComponent<MeshFilter>();
                if (renderer != null && filter != null && filter.sharedMesh != null)
                {
                    var mesh = filter.sharedMesh;
                    line += $" [verts={mesh.vertexCount} submeshes={mesh.subMeshCount} materials={renderer.sharedMaterials.Length}]";
                }
                lines.Add(line);
            }
            lines.Sort(StringComparer.Ordinal);
            return string.Join("\n", lines);
        }

        static string GetPath(Transform transform, Transform root)
        {
            var parts = new List<string>();
            for (var t = transform; t != null && t != root; t = t.parent)
                parts.Add(t.name);
            parts.Reverse();
            return string.Join("/", parts);
        }

        /// <summary>
        /// Self-contained glTF (data-URI buffer): one mesh with four single-triangle primitives
        /// in four distinct vertex buffer layouts (so it yields four mesh assignments), assigned
        /// to two nodes of a three-node hierarchy (root → child → grandchild).
        /// </summary>
        static string CreateMultiLayoutMeshJson()
        {
            // Buffer layout: positions (36 B) | normals (36 B) | uvs (24 B) | colors (48 B) | indices (6 B)
            var buffer = new List<byte>();

            void AddFloats(params float[] values)
            {
                foreach (var value in values)
                    buffer.AddRange(BitConverter.GetBytes(value));
            }

            AddFloats(0, 0, 0, 1, 0, 0, 0, 1, 0);                     // POSITION (VEC3)
            AddFloats(0, 0, 1, 0, 0, 1, 0, 0, 1);                     // NORMAL (VEC3)
            AddFloats(0, 0, 1, 0, 0, 1);                              // TEXCOORD (VEC2)
            AddFloats(1, 0, 0, 1, 0, 1, 0, 1, 0, 0, 1, 1);            // COLOR_0 (VEC4)
            foreach (var index in new ushort[] { 0, 1, 2 })            // indices (SCALAR ushort)
                buffer.AddRange(BitConverter.GetBytes(index));

            var uri = $"data:application/octet-stream;base64,{Convert.ToBase64String(buffer.ToArray())}";

            return @"{
    ""asset"": { ""version"": ""2.0"" },
    ""scene"": 0,
    ""scenes"": [ { ""nodes"": [0] } ],
    ""nodes"": [
        { ""name"": ""Root"", ""mesh"": 0, ""children"": [1] },
        { ""name"": ""Child"", ""children"": [2] },
        { ""name"": ""Grandchild"", ""mesh"": 0 }
    ],
    ""meshes"": [ {
        ""name"": ""MultiLayout"",
        ""primitives"": [
            { ""attributes"": { ""POSITION"": 0, ""NORMAL"": 1 }, ""indices"": 4, ""material"": 0 },
            { ""attributes"": { ""POSITION"": 0, ""NORMAL"": 1, ""TEXCOORD_0"": 2 }, ""indices"": 4, ""material"": 1 },
            { ""attributes"": { ""POSITION"": 0, ""NORMAL"": 1, ""COLOR_0"": 3 }, ""indices"": 4, ""material"": 2 },
            { ""attributes"": { ""POSITION"": 0, ""NORMAL"": 1, ""TEXCOORD_0"": 2, ""TEXCOORD_1"": 2 }, ""indices"": 4, ""material"": 3 }
        ]
    } ],
    ""materials"": [
        { ""pbrMetallicRoughness"": { ""baseColorFactor"": [1,0,0,1] } },
        { ""pbrMetallicRoughness"": { ""baseColorFactor"": [0,1,0,1] } },
        { ""pbrMetallicRoughness"": { ""baseColorFactor"": [0,0,1,1] } },
        { ""pbrMetallicRoughness"": { ""baseColorFactor"": [1,1,0,1] } }
    ],
    ""accessors"": [
        { ""bufferView"": 0, ""componentType"": 5126, ""count"": 3, ""type"": ""VEC3"", ""min"": [0,0,0], ""max"": [1,1,0] },
        { ""bufferView"": 1, ""componentType"": 5126, ""count"": 3, ""type"": ""VEC3"" },
        { ""bufferView"": 2, ""componentType"": 5126, ""count"": 3, ""type"": ""VEC2"" },
        { ""bufferView"": 3, ""componentType"": 5126, ""count"": 3, ""type"": ""VEC4"" },
        { ""bufferView"": 4, ""componentType"": 5123, ""count"": 3, ""type"": ""SCALAR"" }
    ],
    ""bufferViews"": [
        { ""buffer"": 0, ""byteOffset"": 0, ""byteLength"": 36 },
        { ""buffer"": 0, ""byteOffset"": 36, ""byteLength"": 36 },
        { ""buffer"": 0, ""byteOffset"": 72, ""byteLength"": 24 },
        { ""buffer"": 0, ""byteOffset"": 96, ""byteLength"": 48 },
        { ""buffer"": 0, ""byteOffset"": 144, ""byteLength"": 6 }
    ],
    ""buffers"": [ { ""uri"": """ + uri + @""", ""byteLength"": 150 } ]
}";
        }
    }
}
