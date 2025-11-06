// SPDX-FileCopyrightText: 2025 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using GLTFast.Logging;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GLTFast.Tests.Import
{
    [Category("Import")]
    class BufferAndImageLoadTests
    {
        const string k_GltfInvalidBufferUri = @"{""buffers"":[{""uri"":""DoesNotExist.bin""}]}";
        const string k_GltfInvalidImageUri = @"{""images"":[{""uri"":""DoesNotExist.png"",""mimeType"":""image/png""}],""textures"":[{""source"":0}]}";

        [UnityTest]
        public IEnumerator BufferDoesNotExist()
        {
            var task = Test(
                import => import.LoadGltfJson(k_GltfInvalidBufferUri, new Uri("file:///nonExistingFolder")),
                false, LogCode.BufferLoadFailed
                );
            yield return AsyncWrapper.WaitForTask(task);
        }

        [UnityTest]
        public IEnumerator BufferDoesNotExistBinary()
        {
            var gltf = CreateGltfBinaryFromJson(k_GltfInvalidBufferUri);
            var task = Test(
                import => import.Load(gltf, new Uri("file:///nonExistingFolder")),
                false, LogCode.BufferLoadFailed
                );
            yield return AsyncWrapper.WaitForTask(task);
        }

        [UnityTest]
        public IEnumerator BufferDoesNotExistFile()
        {
            var path = Path.Combine(Application.temporaryCachePath, "GltfInvalidBufferUri.gltf");
            File.WriteAllText(path, k_GltfInvalidBufferUri);
            var task = Test(
                import => import.LoadFile(path, new Uri("file:///nonExistingFolder")),
                false, LogCode.BufferLoadFailed
                );
            yield return AsyncWrapper.WaitForTask(task);
            File.Delete(path);
        }

        [UnityTest]
        public IEnumerator ImageDoesNotExist()
        {
            var task = Test(
                import => import.LoadGltfJson(k_GltfInvalidImageUri, new Uri("file:///nonExistingFolder")),
                true, LogCode.TextureDownloadFailed
            );
            yield return AsyncWrapper.WaitForTask(task);
        }

        [UnityTest]
        public IEnumerator ImageDoesNotExistUri()
        {
            var path = Path.Combine(Application.temporaryCachePath, "GltfInvalidImageUri.gltf");
            File.WriteAllText(path, k_GltfInvalidImageUri);
            var task = Test(import => import.Load(path), true, LogCode.TextureDownloadFailed);
            yield return AsyncWrapper.WaitForTask(task);
            File.Delete(path);
        }

        [UnityTest]
        public IEnumerator ImageDoesNotExistUriBinary()
        {
            var path = Path.Combine(Application.temporaryCachePath, "GltfInvalidImageUri.glb");
            File.WriteAllBytes(path, CreateGltfBinaryFromJson(k_GltfInvalidImageUri));
            var task = Test(import => import.Load(path), true, LogCode.TextureDownloadFailed);
            yield return AsyncWrapper.WaitForTask(task);
            File.Delete(path);
        }

        static async Task Test(Func<GltfImport, Task<bool>> loadMethod, bool expectSuccess, params LogCode[] expectedLogCodes)
        {
            var logger = new CollectingLogger();
            var import = new GltfImport(logger: logger);
            Assert.AreEqual(expectSuccess, await loadMethod(import));
            Assert.AreEqual(1, logger.Count);
            LoggerTest.AssertLogger(logger, expectedLogCodes);
        }

        static byte[] CreateGltfBinaryFromJson(string json)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            // Header
            writer.Write(GltfGlobals.GltfBinaryMagic); // ASCII "glTF"
            writer.Write(2u); // glTF version
            writer.Write(12 + 8 + json.Length); // total length
            // JSON chunk
            writer.Write((uint)json.Length); // chunk length
            writer.Write((uint)ChunkFormat.Json); // chunk type
            writer.Write(System.Text.Encoding.UTF8.GetBytes(json)); // chunk data
            // No binary chunk required for the test
            return stream.ToArray();
        }
    }
}
