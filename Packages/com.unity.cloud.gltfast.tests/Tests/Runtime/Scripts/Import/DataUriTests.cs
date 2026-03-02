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
    [Category("Import")]
    class DataUriTests
    {
        const string k_TestDataBase64 = "rQbwDQ=="; // AD06F00D, a dog food ;)
        const string k_TestDataBase64Invalid = "rQbw}Q==";
        /// <summary>One black pixel WebP</summary>
        const string k_TestWebP = "UklGRiQAAABXRUJQVlA4IBgAAAAwAQCdASoBAAEAAgA0JaQAA3AA/vv9UAA=";
        const string k_TestKtxInvalid = "q0tUWCAyMLsNChoK";
        const string k_TestKtx1Px = "q0tUWCAyMLsNChoKKwAAAAEAAAABAAAAAQAAAAAAAAAAAAAAAQAAAAEAAAAAAAAAaAAAAFwAAADEAAAAMAAAAAAAAAAAAAAAAAAAAAAAAAD0AAAAAAAAAAQAAAAAAAAABAAAAAAAAABcAAAAAAAAAAIAWAABAQIAAAAAAAQAAAAAAAAAAAAHAAAAAAAAAAAA/wAAAAgABwEAAAAAAAAAAP8AAAAQAAcCAAAAAAAAAAD/AAAAGAAHHwAAAAAAAAAA/wAAACwAAABLVFh3cml0ZXIAa3R4IGNyZWF0ZSB2NC40LjAgLyBsaWJrdHggdjQuNC4wAAAAAP8=";

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
            var task = Test(gltf, true, LogCode.EmbedImageLoadFailed);
            yield return AsyncWrapper.WaitForTask(task);
        }

        [UnityTest]
        public IEnumerator ImageDataUriUnexpectedMimeType()
        {
            var gltf = $@"{{""images"":[{{""uri"":""data:image/fantasy-format;base64,{k_TestDataBase64}""}}],""textures"":[{{""source"":0}}]}}";
            var task = Test(gltf, true, LogCode.ImageFormatUnknown);
            yield return AsyncWrapper.WaitForTask(task);
        }

        [UnityTest]
        public IEnumerator ImageDataUriUnsupportedMimeType()
        {
            var gltf = $@"{{""images"":[{{""uri"":""data:image/webp;base64,{k_TestWebP}""}}],""textures"":[{{""source"":0}}]}}";
            var task = Test(gltf, true, LogCode.ImageFormatUnsupported);
            yield return AsyncWrapper.WaitForTask(task);
        }

        [UnityTest]
        public IEnumerator ImageContentInvalid()
        {
            var gltf = $@"{{""images"":[{{""uri"":""data:image/jpeg;base64,{k_TestDataBase64Invalid}""}}],""textures"":[{{""source"":0}}]}}";
            var task = Test(gltf, true, LogCode.EmbedImageLoadFailed);
            yield return AsyncWrapper.WaitForTask(task);
        }

        [UnityTest]
        public IEnumerator ImageKtxContentInvalid()
        {
            var gltf = $@"{{""images"":[{{""uri"":""data:image/ktx2;base64,{k_TestDataBase64Invalid}""}}],""textures"":[{{""source"":0}}]}}";
            var task = Test(
                gltf, true,
                LogCode.EmbedImageLoadFailed
                );
            yield return AsyncWrapper.WaitForTask(task);
        }

        [UnityTest]
        public IEnumerator ImageKtxContentBroken()
        {
#if KTX_IS_RECENT
            LogAssert.Expect(LogType.Error, "KTX error code FileUnexpectedEof");
#endif
            var gltf = $@"{{""images"":[{{""uri"":""data:image/ktx2;base64,{k_TestKtxInvalid}""}}],""textures"":[{{""source"":0}}]}}";
            var task = Test(
                gltf, true,
#if KTX_IS_RECENT
                LogCode.EmbedImageLoadFailed
#else
                LogCode.PackageMissing
#endif
                );
            yield return AsyncWrapper.WaitForTask(task);
        }

        [UnityTest]
        public IEnumerator ImageKtxNoMipMaps()
        {
#if KTX_IS_RECENT
            LogAssert.Expect(LogType.Warning, "KTX texture does not contain mipmaps.");
#endif
            var gltf = $@"{{""images"":[{{""uri"":""data:image/ktx2;base64,{k_TestKtx1Px}""}}],""textures"":[{{""source"":0}}]}}";
            var task = Test(
                gltf, true
#if !KTX_IS_RECENT
                , LogCode.PackageMissing
#endif
                );
            yield return AsyncWrapper.WaitForTask(task);
        }

        static async Task Test(string gltf, bool expectSuccess, params LogCode[] expectedLogCodes)
        {
            var logger = new CollectingLogger();
            var import = new GltfImport(logger: logger);
            var settings = new ImportSettings { GenerateMipMaps = true };
            Assert.AreEqual(expectSuccess, await import.LoadGltfJson(gltf, importSettings: settings));
            Assert.AreEqual(1 + expectedLogCodes.Length, logger.Count);
            LoggerTest.AssertLogger(logger, expectedLogCodes);
        }
    }
}
