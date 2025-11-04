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
            var gltf = $@"{{""buffers"":[{{""uri"":""data:text/plain;base64,{k_TestDataBase64}"",""byteLength"":4}}]}}";
            var task = Test(gltf, false, LogCode.BufferDataUriUnexpectedMimeType);
            yield return AsyncWrapper.WaitForTask(task);
        }

        [UnityTest]
        public IEnumerator BufferDataUriMissingMimeTypeDelimiter()
        {
            var gltf = $@"{{""buffers"":[{{""uri"":""data:{k_TestDataBase64}"",""byteLength"":4}}]}}";
            var task = Test(gltf, false, LogCode.EmbedBufferLoadFailed);
            yield return AsyncWrapper.WaitForTask(task);
        }

        [UnityTest]
        public IEnumerator BufferDataUriUnexpectedEncoding()
        {
            var gltf = $@"{{""buffers"":[{{""uri"":""data:text/plain;base32,{k_TestDataBase64}"",""byteLength"":4}}]}}";
            var task = Test(gltf, false, LogCode.EmbedBufferLoadFailed);
            yield return AsyncWrapper.WaitForTask(task);
        }

        [UnityTest]
        public IEnumerator BufferContentUndersized()
        {
            var gltf = $@"{{""buffers"":[{{""uri"":""data:application/octet-stream;base64,{k_TestDataBase64}"",""byteLength"":5}}]}}";
            var task = Test(gltf, false, LogCode.BufferContentUndersized);
            yield return AsyncWrapper.WaitForTask(task);
        }

        [UnityTest]
        public IEnumerator BufferContentInvalid()
        {
            var gltf = $@"{{""buffers"":[{{""uri"":""data:application/gltf-buffer;base64,{k_TestDataBase64Invalid}"",""byteLength"":4}}]}}";
            var task = Test(gltf, false, LogCode.EmbedBufferLoadFailed);
            yield return AsyncWrapper.WaitForTask(task);
        }

        [UnityTest]
        public IEnumerator ImageDataUriUnexpectedEncoding()
        {
            var gltf = $@"{{""images"":[{{""uri"":""data:image/webp;base32,{k_TestDataBase64Invalid}""}}],""textures"":[{{""source"":0}}]}}";
            // TODO: LoadGltfJson currently returns true even if image loading fails.
            //       Fix this and change the assert to Assert.IsFalse
            var task = Test(gltf, true, LogCode.EmbedImageLoadFailed);
            yield return AsyncWrapper.WaitForTask(task);
        }

        [UnityTest]
        public IEnumerator ImageDataUriUnexpectedMimeType()
        {
            var gltf = $@"{{""images"":[{{""uri"":""data:image/fantasy-format;base64,{k_TestDataBase64Invalid}""}}],""textures"":[{{""source"":0}}]}}";
            // TODO: LoadGltfJson currently returns true even if image loading fails.
            //       Fix this and change the assert to Assert.IsFalse
            var task = Test(gltf, true, LogCode.EmbedImageLoadFailed);
            yield return AsyncWrapper.WaitForTask(task);
        }

        [UnityTest]
        public IEnumerator ImageDataUriUnsupportedMimeType()
        {
            var gltf = $@"{{""images"":[{{""uri"":""data:image/webp;base64,{k_TestDataBase64Invalid}""}}],""textures"":[{{""source"":0}}]}}";
            // TODO: LoadGltfJson currently returns true even if image loading fails.
            //       Fix this and change the assert to Assert.IsFalse
            var task = Test(gltf, true, LogCode.EmbedImageUnsupportedType);
            yield return AsyncWrapper.WaitForTask(task);
        }

        [UnityTest]
        public IEnumerator ImageContentInvalid()
        {
            var gltf = $@"{{""images"":[{{""uri"":""data:image/jpeg;base64,{k_TestDataBase64Invalid}""}}],""textures"":[{{""source"":0}}]}}";
            // TODO: LoadGltfJson currently returns true even if image loading fails.
            //       Fix this and change the assert to Assert.IsFalse
            var task = Test(gltf, true, LogCode.EmbedImageLoadFailed);
            yield return AsyncWrapper.WaitForTask(task);
        }

        [UnityTest]
        public IEnumerator ImageKtxContentInvalid()
        {
            var gltf = $@"{{""images"":[{{""uri"":""data:image/ktx2;base64,{k_TestDataBase64Invalid}""}}],""textures"":[{{""source"":0}}]}}";
            // TODO: LoadGltfJson currently returns true even if image loading fails.
            //       Fix this and change the assert to Assert.IsFalse
            var task = Test(
                gltf, true,
#if KTX_IS_RECENT
                LogCode.EmbedImageLoadFailed
#else
                LogCode.EmbedImageUnsupportedType
#endif
                );
            yield return AsyncWrapper.WaitForTask(task);
        }

        [UnityTest]
        public IEnumerator ImageKtxContentBroken()
        {
            var gltf = $@"{{""images"":[{{""uri"":""data:image/ktx2;base64,{k_TestDataBase64}""}}],""textures"":[{{""source"":0}}]}}";
            // TODO: LoadGltfJson currently returns true even if image loading fails.
            //       Fix this and change the assert to Assert.IsFalse
            var task = Test(
                gltf, true,
#if KTX_IS_RECENT
                LogCode.EmbedImageLoadFailed
#else
                LogCode.EmbedImageUnsupportedType
#endif
                );
            yield return AsyncWrapper.WaitForTask(task);
#if KTX_IS_RECENT
            LogAssert.Expect(LogType.Error, "KTX error code FileUnexpectedEof");
#endif
        }

        static async Task Test(string gltf, bool expectSuccess, params LogCode[] expectedLogCodes)
        {
            var logger = new CollectingLogger();
            var import = new GltfImport(logger: logger);
            Assert.AreEqual(expectSuccess, await import.LoadGltfJson(gltf));
            Assert.AreEqual(2, logger.Count);
            LoggerTest.AssertLogger(logger, expectedLogCodes);
        }
    }
}
