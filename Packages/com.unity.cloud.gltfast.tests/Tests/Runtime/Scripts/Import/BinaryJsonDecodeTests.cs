// SPDX-FileCopyrightText: 2026 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;

namespace GLTFast.Tests.Import
{
    /// <summary>
    /// Tests for glTF-binary JSON chunk decoding. A large JSON chunk is decoded off the main
    /// thread when the defer agent signals the predicted decode time exceeds the frame budget;
    /// small chunks keep decoding in-frame. Either path must produce identical results —
    /// including correct UTF-8 handling across the thread boundary — and honor cancellation.
    /// </summary>
    [Category("Import")]
    class BinaryJsonDecodeTests
    {
        /// <summary>Unicode node names crossing the threaded decode must survive intact.</summary>
        static readonly string[] k_NodeNames = { "Café", "node/中文", "emoji❤♻" };

        /// <summary>
        /// A defer agent that always defers, routing a binary JSON chunk of any size through
        /// the threaded decode path.
        /// </summary>
        class AlwaysDeferAgent : IDeferAgent
        {
            public bool ShouldDefer() => true;
            public bool ShouldDefer(float duration) => true;
            public async Task BreakPoint() => await Task.Yield();
            public async Task BreakPoint(float duration) => await Task.Yield();
        }

        [UnityEngine.TestTools.UnityTest]
        public IEnumerator ThreadedJsonDecodeMatchesInFrameDecode()
        {
            yield return AsyncWrapper.WaitForTask(ThreadedJsonDecodeMatchesInFrameDecodeInternal());
        }

        static async Task ThreadedJsonDecodeMatchesInFrameDecodeInternal()
        {
            var glb = CreateLargeBinaryGltf();

            // UninterruptedDeferAgent never defers => in-frame decode; AlwaysDeferAgent defers
            // any predicted duration => threaded decode. Results must be identical.
            var inFrame = await LoadAndDescribeAsync(glb, new UninterruptedDeferAgent());
            var threaded = await LoadAndDescribeAsync(glb, new AlwaysDeferAgent());

            if (inFrame != threaded)
                Debug.Log($"IN-FRAME:\n{inFrame}\n\nTHREADED:\n{threaded}");
            Assert.AreEqual(inFrame, threaded,
                "Threaded binary JSON decode produced a different scene than in-frame decode.");

            // Unicode must survive the thread boundary byte-for-byte.
            foreach (var name in k_NodeNames)
                StringAssert.Contains(name, threaded,
                    $"Node name '{name}' was corrupted by the threaded JSON decode.");
        }

        [UnityEngine.TestTools.UnityTest]
        public IEnumerator ThreadedJsonDecodeHonorsCancellation()
        {
            yield return AsyncWrapper.WaitForTask(ThreadedJsonDecodeHonorsCancellationInternal());
        }

        static async Task ThreadedJsonDecodeHonorsCancellationInternal()
        {
            var glb = CreateLargeBinaryGltf();
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            using var gltf = new GltfImport(deferAgent: new AlwaysDeferAgent());
            try
            {
                await gltf.LoadGltfBinary(glb, cancellationToken: cts.Token);
                Assert.Fail("Loading with a cancelled token did not raise OperationCanceledException.");
            }
            catch (OperationCanceledException)
            {
                // expected — cancellation before/at the threaded decode must surface cleanly.
            }
        }

        static async Task<string> LoadAndDescribeAsync(byte[] glb, IDeferAgent deferAgent)
        {
            // The import stays alive until after the description — disposing destroys its meshes.
            using var gltf = new GltfImport(deferAgent: deferAgent);
            var success = await gltf.LoadGltfBinary(glb);
            Assert.IsTrue(success, "Loading the binary test asset failed.");
            var root = new GameObject("BinaryJsonDecodeResult");
            try
            {
                success = await gltf.InstantiateMainSceneAsync(root.transform);
                Assert.IsTrue(success, "Instantiation failed.");
                return DescribeHierarchy(root);
            }
            finally
            {
                UnityEngine.Object.Destroy(root);
            }
        }

        /// <summary>Stable, order-independent hierarchy description: path per transform plus its
        /// renderer's mesh shape when present.</summary>
        static string DescribeHierarchy(GameObject root)
        {
            var lines = new List<string>();
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
            {
                if (transform == root.transform)
                    continue;
                var parts = new List<string>();
                for (var t = transform; t != null && t != root.transform; t = t.parent)
                    parts.Add(t.name);
                parts.Reverse();
                var line = string.Join("/", parts);
                var filter = transform.GetComponent<MeshFilter>();
                if (filter != null && filter.sharedMesh != null)
                    line += $" [verts={filter.sharedMesh.vertexCount} submeshes={filter.sharedMesh.subMeshCount}]";
                lines.Add(line);
            }
            lines.Sort(StringComparer.Ordinal);
            return string.Join("\n", lines);
        }

        /// <summary>
        /// Self-contained glTF-binary whose JSON chunk is ~1 MB (padded via root "extras"), so
        /// its predicted decode time is meaningful, carrying a single-triangle mesh (data-URI
        /// buffer, no BIN chunk) and unicode node names.
        /// </summary>
        static byte[] CreateLargeBinaryGltf()
        {
            var buffer = new List<byte>();
            foreach (var value in new float[] { 0, 0, 0, 1, 0, 0, 0, 1, 0 })
                buffer.AddRange(BitConverter.GetBytes(value));
            foreach (var index in new ushort[] { 0, 1, 2 })
                buffer.AddRange(BitConverter.GetBytes(index));
            var bufferUri = $"data:application/octet-stream;base64,{Convert.ToBase64String(buffer.ToArray())}";

            var padding = new string('x', 1024 * 1024);

            var json = @"{
    ""asset"": { ""version"": ""2.0"" },
    ""extras"": { ""padding"": """ + padding + @""" },
    ""scene"": 0,
    ""scenes"": [ { ""nodes"": [0] } ],
    ""nodes"": [
        { ""name"": """ + k_NodeNames[0] + @""", ""children"": [1] },
        { ""name"": """ + k_NodeNames[1].Replace("/", "\\/") + @""", ""children"": [2] },
        { ""name"": """ + k_NodeNames[2] + @""", ""mesh"": 0 }
    ],
    ""meshes"": [ { ""primitives"": [ { ""attributes"": { ""POSITION"": 0 }, ""indices"": 1 } ] } ],
    ""accessors"": [
        { ""bufferView"": 0, ""componentType"": 5126, ""count"": 3, ""type"": ""VEC3"", ""min"": [0,0,0], ""max"": [1,1,0] },
        { ""bufferView"": 1, ""componentType"": 5123, ""count"": 3, ""type"": ""SCALAR"" }
    ],
    ""bufferViews"": [
        { ""buffer"": 0, ""byteOffset"": 0, ""byteLength"": 36 },
        { ""buffer"": 0, ""byteOffset"": 36, ""byteLength"": 6 }
    ],
    ""buffers"": [ { ""uri"": """ + bufferUri + @""", ""byteLength"": 42 } ]
}";

            var jsonBytes = Encoding.UTF8.GetBytes(json);
            var paddedLength = (jsonBytes.Length + 3) & ~3;

            var glb = new List<byte>();
            glb.AddRange(BitConverter.GetBytes(0x46546C67u));                    // 'glTF'
            glb.AddRange(BitConverter.GetBytes(2u));                             // version
            glb.AddRange(BitConverter.GetBytes((uint)(12 + 8 + paddedLength)));  // total length
            glb.AddRange(BitConverter.GetBytes((uint)paddedLength));             // chunk length
            glb.AddRange(BitConverter.GetBytes(0x4E4F534Au));                    // 'JSON'
            glb.AddRange(jsonBytes);
            for (var i = jsonBytes.Length; i < paddedLength; i++)
                glb.Add(0x20);                                                   // pad with spaces
            return glb.ToArray();
        }
    }
}
