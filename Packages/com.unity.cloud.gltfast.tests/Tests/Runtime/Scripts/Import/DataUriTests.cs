// SPDX-FileCopyrightText: 2025 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections;
using System.Threading.Tasks;
using GLTFast.Logging;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GLTFast.Tests.Import
{
    public class DataUriTests
    {
        const string k_TestDataBase64 = "rQbwDQ=="; // AD06F00D, a dog food ;)
        const string k_TestDataBase64Invalid = "rQbw}Q==";

        [UnityTest]
        public IEnumerator BufferDataUriUnexpectedMimeType()
        {
            var task = TestImplementation();
            yield return AsyncWrapper.WaitForTask(task);
            yield break;

            async Task TestImplementation()
            {
                var logger = new CollectingLogger();
                var gltf = $@"{{""buffers"":[{{""uri"":""data:text/plain;base64,{k_TestDataBase64}"",""byteLength"":4}}]}}";
                var import = new GltfImport(logger: logger);
                Assert.IsFalse(await import.LoadGltfJson(gltf));
                Assert.AreEqual(2, logger.Count);
                LoggerTest.AssertLogger(
                    logger,
                    new[] { LogCode.BufferDataUriUnexpectedMimeType }
                    );
            }
        }

        [UnityTest]
        public IEnumerator BufferDataUriMissingMimeTypeDelimiter()
        {
            var task = TestImplementation();
            yield return AsyncWrapper.WaitForTask(task);
            yield break;

            async Task TestImplementation()
            {
                var logger = new CollectingLogger();
                var gltf = $@"{{""buffers"":[{{""uri"":""data:{k_TestDataBase64}"",""byteLength"":4}}]}}";
                var import = new GltfImport(logger: logger);
                Assert.IsFalse(await import.LoadGltfJson(gltf));
                Assert.AreEqual(2, logger.Count);
                LoggerTest.AssertLogger(
                    logger,
                    new[] { LogCode.EmbedBufferLoadFailed }
                );
            }
        }

        [UnityTest]
        public IEnumerator BufferDataUriUnexpectedEncoding()
        {
            var task = TestImplementation();
            yield return AsyncWrapper.WaitForTask(task);
            yield break;

            async Task TestImplementation()
            {
                var logger = new CollectingLogger();
                var gltf = $@"{{""buffers"":[{{""uri"":""data:text/plain;base32,{k_TestDataBase64}"",""byteLength"":4}}]}}";
                var import = new GltfImport(logger: logger);
                Assert.IsFalse(await import.LoadGltfJson(gltf));
                Assert.AreEqual(2, logger.Count);
                LoggerTest.AssertLogger(
                    logger,
                    new[] { LogCode.EmbedBufferLoadFailed }
                );
            }
        }

        [UnityTest]
        public IEnumerator BufferContentUndersized()
        {
            var task = TestImplementation();
            yield return AsyncWrapper.WaitForTask(task);
            yield break;

            async Task TestImplementation()
            {
                var logger = new CollectingLogger();
                var gltf = $@"{{""buffers"":[{{""uri"":""data:application/octet-stream;base64,{k_TestDataBase64}"",""byteLength"":5}}]}}";
                var import = new GltfImport(logger: logger);
                Assert.IsFalse(await import.LoadGltfJson(gltf));
                Assert.AreEqual(2, logger.Count);
                LoggerTest.AssertLogger(
                    logger,
                    new[] { LogCode.BufferContentUndersized }
                );
            }
        }

        [UnityTest]
        public IEnumerator BufferContentInvalid()
        {
            var task = TestImplementation();
            yield return AsyncWrapper.WaitForTask(task);
            yield break;

            async Task TestImplementation()
            {
                var logger = new CollectingLogger();
                var gltf = $@"{{""buffers"":[{{""uri"":""data:application/gltf-buffer;base64,{k_TestDataBase64Invalid}"",""byteLength"":4}}]}}";
                var import = new GltfImport(logger: logger);
                Assert.IsFalse(await import.LoadGltfJson(gltf));
                Assert.AreEqual(2, logger.Count);
                LoggerTest.AssertLogger(
                    logger,
                    new[] { LogCode.EmbedBufferLoadFailed }
                );
            }
        }

        [UnityTest]
        public IEnumerator ImageDataUriUnexpectedEncoding()
        {
            var task = TestImplementation();
            yield return AsyncWrapper.WaitForTask(task);
            yield break;

            async Task TestImplementation()
            {
                var logger = new CollectingLogger();
                var gltf = $@"{{""images"":[{{""uri"":""data:image/webp;base32,{k_TestDataBase64Invalid}""}}],""textures"":[{{""source"":0}}]}}";
                var import = new GltfImport(logger: logger);
                // TODO: LoadGltfJson currently returns true even if image loading fails.
                //       Fix this and change the assert to Assert.IsFalse
                Assert.IsTrue(await import.LoadGltfJson(gltf));
                Assert.AreEqual(2, logger.Count);
                LoggerTest.AssertLogger(
                    logger,
                    new[] { LogCode.EmbedImageLoadFailed }
                );
            }
        }

        [UnityTest]
        public IEnumerator ImageDataUriUnexpectedMimeType()
        {
            var task = TestImplementation();
            yield return AsyncWrapper.WaitForTask(task);
            yield break;

            async Task TestImplementation()
            {
                var logger = new CollectingLogger();
                var gltf = $@"{{""images"":[{{""uri"":""data:image/fantasy-format;base64,{k_TestDataBase64Invalid}""}}],""textures"":[{{""source"":0}}]}}";
                var import = new GltfImport(logger: logger);
                // TODO: LoadGltfJson currently returns true even if image loading fails.
                //       Fix this and change the assert to Assert.IsFalse
                Assert.IsTrue(await import.LoadGltfJson(gltf));
                Assert.AreEqual(2, logger.Count);
                LoggerTest.AssertLogger(
                    logger,
                    new[] { LogCode.EmbedImageLoadFailed }
                );
            }
        }

        [UnityTest]
        public IEnumerator ImageDataUriUnsupportedMimeType()
        {
            var task = TestImplementation();
            yield return AsyncWrapper.WaitForTask(task);
            yield break;

            async Task TestImplementation()
            {
                var logger = new CollectingLogger();
                var gltf = $@"{{""images"":[{{""uri"":""data:image/ktx2;base64,{k_TestDataBase64Invalid}""}}],""textures"":[{{""source"":0}}]}}";
                var import = new GltfImport(logger: logger);
                // TODO: LoadGltfJson currently returns true even if image loading fails.
                //       Fix this and change the assert to Assert.IsFalse
                Assert.IsTrue(await import.LoadGltfJson(gltf));
                Assert.AreEqual(2, logger.Count);
                LoggerTest.AssertLogger(
                    logger,
                    new[] { LogCode.EmbedImageUnsupportedType }
                );
            }
        }

        [UnityTest]
        public IEnumerator ImageContentInvalid()
        {
            var task = TestImplementation();
            yield return AsyncWrapper.WaitForTask(task);
            yield break;

            async Task TestImplementation()
            {
                var logger = new CollectingLogger();
                var gltf = $@"{{""images"":[{{""uri"":""data:image/jpeg;base64,{k_TestDataBase64Invalid}""}}],""textures"":[{{""source"":0}}]}}";
                var import = new GltfImport(logger: logger);
                // TODO: LoadGltfJson currently returns true even if image loading fails.
                //       Fix this and change the assert to Assert.IsFalse
                Assert.IsTrue(await import.LoadGltfJson(gltf));
                Assert.AreEqual(2, logger.Count);
                LoggerTest.AssertLogger(
                    logger,
                    new[] { LogCode.EmbedImageLoadFailed }
                );
            }
        }
    }
}
