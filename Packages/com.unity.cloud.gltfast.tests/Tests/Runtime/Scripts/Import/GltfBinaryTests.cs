// SPDX-FileCopyrightText: 2025 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using GLTFast.Logging;
using NUnit.Framework;
using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Networking;
#endif
using UnityEngine.TestTools;

namespace GLTFast.Tests.Import
{
    [Category("Import")]
    class GltfBinaryTests : IPrebuildSetup
    {
        public async void Setup()
        {
            await TestGltfGenerator.CertifyPerformanceTestGltfs();
        }

        [UnityTest]
        public IEnumerator UnexpectedEndOfContent()
        {
            var path = Path.Combine(Application.temporaryCachePath, "UnexpectedEndOfFile.glb");
            var file = new FileStream(path, FileMode.Create);
            using var gltf = CreateGltfBinary(totalLengthOffset: 1000);
            gltf.CopyTo(file);
            file.Close();
            var task = Test(import => import.Load(path), false, LogCode.UnexpectedEndOfContent);
            yield return AsyncWrapper.WaitForTask(task);
        }

        [UnityTest]
        public IEnumerator UnexpectedEndOfContentStream()
        {
            var gltf = CreateGltfBinary(totalLengthOffset: 1000);
            var task = Test(import => import.LoadStream(gltf), false, LogCode.UnexpectedEndOfContent);
            yield return AsyncWrapper.WaitForTask(task);
        }

        [UnityTest]
        public IEnumerator TooMuchData()
        {
            var path = Path.Combine(Application.temporaryCachePath, "TooMuchData.glb");
            var file = new FileStream(path, FileMode.Create);
            using var gltf = CreateGltfBinary(appendGarbage: true);
            gltf.CopyTo(file);
            file.Close();
            var task = Test(import => import.Load(path), true);
            yield return AsyncWrapper.WaitForTask(task);
        }

        [UnityTest]
        public IEnumerator TooMuchDataStream()
        {
            var gltf = CreateGltfBinary(appendGarbage: true);
            var task = Test(import => import.LoadStream(gltf), true);
            yield return AsyncWrapper.WaitForTask(task);
        }

        [UnityTest]
        public IEnumerator ChunkHeaderIncomplete()
        {
            var gltf = CreateGltfBinary(appendGarbage: true);
            // truncate to cut off part of chunk header
            var incompleteGltf = new byte[16];
            Assert.AreEqual(incompleteGltf.Length, gltf.Read(incompleteGltf, 0, incompleteGltf.Length));
            // fake valid length in glb header
            incompleteGltf[8] = (byte)incompleteGltf.Length;
            var task = Test(import => import.Load(incompleteGltf), false, LogCode.ChunkIncomplete);
            yield return AsyncWrapper.WaitForTask(task);
        }

        [UnityTest]
        public IEnumerator ChunkContentIncomplete()
        {
            var gltf = CreateGltfBinary(appendGarbage: true);
            // truncate to cut off part of chunk content
            var incompleteGltf = new byte[21];
            Assert.AreEqual(incompleteGltf.Length, gltf.Read(incompleteGltf, 0, incompleteGltf.Length));
            // fake valid length in glb header
            incompleteGltf[8] = (byte)incompleteGltf.Length;
            var task = Test(import => import.Load(incompleteGltf), false, LogCode.ChunkIncomplete);
            yield return AsyncWrapper.WaitForTask(task);
        }

        [UnityTest]
        public IEnumerator BigFile()
        {
            var path = TestGltfGenerator.BigCylinderBinaryPath;
#if UNITY_ANDROID && !UNITY_EDITOR
            // On Android streaming assets are packed in a jar file and cannot be accessed via file stream directly.
            // So we copy the file to a temporary location first.
            var copyTask = CopyToTempFile(path);
            yield return AsyncWrapper.WaitForTask(copyTask);
            path = copyTask.Result;
#endif
            // Test copying glb from stream to memory in a thread (enforced by large content)
            var task = Test(import => import.LoadFile(path), true);
            yield return AsyncWrapper.WaitForTask(task);
        }

        static async Task Test(Func<GltfImport, Task<bool>> loadMethod, bool expectSuccess, params LogCode[] expectedLogCodes)
        {
            var logger = new CollectingLogger();
            var import = new GltfImport(logger: logger);
            Assert.AreEqual(expectSuccess, await loadMethod(import));
            LoggerTest.AssertLogger(logger, expectedLogCodes);
        }

        static Stream CreateGltfBinary(
            int totalLengthOffset = 0,
            bool appendGarbage = false
            )
        {
            const string json = "{}";
            var stream = new MemoryStream();
            var writer = new BinaryWriter(stream);
            // Header
            writer.Write(GltfGlobals.GltfBinaryMagic); // ASCII "glTF"
            writer.Write(2u); // glTF version
            writer.Write(12 + 8 + json.Length + totalLengthOffset); // total length
            // JSON chunk
            writer.Write((uint)json.Length); // chunk length
            writer.Write((uint)ChunkFormat.Json); // chunk type
            writer.Write(System.Text.Encoding.UTF8.GetBytes(json)); // chunk data
            // No binary chunk required for the test
            if (appendGarbage)
            {
                writer.Write("Garbage"); // extra byte
            }
            writer.Flush();
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        internal static async Task<string> CopyToTempFile(string sourcePath)
        {
            var request = UnityWebRequest.Get(sourcePath);
            request.SendWebRequest();
            while (!request.isDone) await Task.Yield();
            if (request.result != UnityWebRequest.Result.Success)
                throw new IOException($"Failed loading URI {sourcePath}: {request.downloadHandler.text}");
            var data = request.downloadHandler.data;
            var destinationPath = Path.Combine(Application.temporaryCachePath, Path.GetFileName(sourcePath));
            await File.WriteAllBytesAsync(destinationPath, data);
            return destinationPath;
        }
#endif
    }
}
