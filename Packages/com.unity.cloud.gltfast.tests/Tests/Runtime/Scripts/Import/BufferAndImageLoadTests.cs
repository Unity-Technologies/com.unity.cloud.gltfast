// SPDX-FileCopyrightText: 2025 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Cloud.Gltfast.Logging;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Cloud.Gltfast.Tests.Import
{
    [Category("Import")]
    class BufferAndImageLoadTests
    {
        const string k_GltfInvalidBufferUri = @"{""buffers"":[{""uri"":""DoesNotExist.bin""}]}";
        const string k_GltfInvalidImageUri = @"{""images"":[{""uri"":""DoesNotExist.png"",""mimeType"":""image/png""}],""textures"":[{""source"":0}]}";
        const string k_Png1PxDataUri = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAIAAACQd1PeAAAANmVYSWZNTQAqAAAAGAAAAEgAAAABAAAASAAAAAEAAgEaAAUAAAABAAAACAEbAAUAAAABAAAAEAAAAACQeO+8AAAACW9GRnMAAAAAAAAAAADaKrbOAAAACXBIWXMAAAsSAAALEgHS3X78AAAADElEQVQIHWNgYGAAAAAEAAFkMlP+AAAAAElFTkSuQmCC";

        [UnityTest]
        public IEnumerator BufferDoesNotExist()
        {
            var task = Test(
                import => import.LoadGltfJsonAsync(k_GltfInvalidBufferUri, new Uri("file:///nonExistingFolder")),
                false, LogCode.BufferLoadFailed
                );
            yield return AsyncWrapper.WaitForTask(task);
        }

        [UnityTest]
        public IEnumerator BufferDoesNotExistBinary()
        {
            var gltf = CreateGltfBinaryFromJson(k_GltfInvalidBufferUri);
            var task = Test(
                import => import.LoadAsync(gltf, new Uri("file:///nonExistingFolder")),
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
                import => import.LoadFileAsync(path, new Uri("file:///nonExistingFolder")),
                false, LogCode.BufferLoadFailed
                );
            yield return AsyncWrapper.WaitForTask(task);
            File.Delete(path);
        }

        [UnityTest]
        public IEnumerator ImageDoesNotExist()
        {
            var task = Test(
                import => import.LoadGltfJsonAsync(k_GltfInvalidImageUri, new Uri("file:///nonExistingFolder")),
                true, LogCode.TextureDownloadFailed
            );
            yield return AsyncWrapper.WaitForTask(task);
        }

        [UnityTest]
        public IEnumerator ImageDoesNotExistUri()
        {
            var path = Path.Combine(Application.temporaryCachePath, "GltfInvalidImageUri.gltf");
            File.WriteAllText(path, k_GltfInvalidImageUri);
            var task = Test(import => import.LoadAsync(path), true, LogCode.TextureDownloadFailed);
            yield return AsyncWrapper.WaitForTask(task);
            File.Delete(path);
        }

        [UnityTest]
        public IEnumerator ImageDoesNotExistUriBinary()
        {
            var path = Path.Combine(Application.temporaryCachePath, "GltfInvalidImageUri.glb");
            File.WriteAllBytes(path, CreateGltfBinaryFromJson(k_GltfInvalidImageUri));
            var task = Test(import => import.LoadAsync(path), true, LogCode.TextureDownloadFailed);
            yield return AsyncWrapper.WaitForTask(task);
            File.Delete(path);
        }

        [UnityTest]
        public IEnumerator ImageEmpty()
        {
            var path = Path.Combine(Application.temporaryCachePath, "GltfImageEmpty.gltf");
            const string gltfImageEmpty = @"{""images"":[{""mimeType"":""image/png""}],""textures"":[{""source"":0}]}";
            File.WriteAllText(path, gltfImageEmpty);
            var task = Test(import => import.LoadAsync(path), true, LogCode.MissingImageURL);
            yield return AsyncWrapper.WaitForTask(task);
            File.Delete(path);
        }

        [UnityTest]
        public IEnumerator ImageUnsupported()
        {
            var path = Path.Combine(Application.temporaryCachePath, "GltfImageUnsupported.gltf");
            const string gltfImageEmpty = @"{""extensionsRequired"":[""EXT_texture_webp""],""extensionsUsed"":[""EXT_texture_webp""],""images"":[{""uri"":""DoesNotExist.webp"",""mimeType"":""image/webp""}],""textures"":[{""source"":0}]}";
            File.WriteAllText(path, gltfImageEmpty);
            var task = Test(import => import.LoadAsync(path), false, LogCode.ExtensionUnsupported);
            yield return AsyncWrapper.WaitForTask(task);
            File.Delete(path);
        }

        [UnityTest]
        public IEnumerator ImageUnsupportedFallback()
        {
            var path = Path.Combine(Application.temporaryCachePath, "GltfImageUnsupportedFallback.gltf");
            var gltfImageEmpty = $@"
{{
    ""extensionsUsed"":[""EXT_texture_webp""],
    ""images"":[
        {{""uri"":""{k_Png1PxDataUri}""}},
        {{""uri"":""DoesNotExist.webp"",""mimeType"":""image/webp""}}
    ],
    ""textures"":[
        {{""source"":0,""extensions"":{{""EXT_texture_webp"":{{""source"":1}}}}}}
    ]
}}";
            File.WriteAllText(path, gltfImageEmpty);
            var task = Test(import => import.LoadAsync(path), true, logger =>
            {
                Assert.AreEqual(2, logger.Count);
                var items = logger.Items.ToList();
                Assert.AreEqual(LogCode.ExtensionUnsupported, items[0].Code);
                Assert.AreEqual(LogType.Warning, items[0].Type);
                Assert.AreEqual(LogCode.EmbedSlow, items[1].Code);
                Assert.AreEqual(LogType.Warning, items[1].Type);
            });
            yield return AsyncWrapper.WaitForTask(task);
            File.Delete(path);
        }

        [UnityTest]
        public IEnumerator ImageContentBasedDetection()
        {
            const string base64Prefix = "data:image/png;base64,";
            var data = Convert.FromBase64String(k_Png1PxDataUri[base64Prefix.Length..]);
            const string imageFileName = "FileWithNoExtension";
            var imagePath = Path.Combine(Application.temporaryCachePath, imageFileName);
            File.WriteAllBytes(imagePath, data);
            var path = Path.Combine(Application.temporaryCachePath, "GltfImageContentBasedDetection.gltf");
            var gltfNoMimeType = $@"
{{
    ""images"":[
        {{""uri"":""{imageFileName}""}}
    ],
    ""textures"":[
        {{""source"":0}}
    ]
}}";
            File.WriteAllText(path, gltfNoMimeType);
            var task = Test(
                async import =>
                {
                    var success = await import.LoadAsync(path);
                    Assert.AreEqual(1, import.TextureCount);
                    var image = import.GetTexture(0);
                    Assert.NotNull(image);
                    Assert.AreEqual(1, image.width);
                    Assert.AreEqual(1, image.height);
                    return success;
                },
                true,
                logger =>
                {
                    Assert.AreEqual(0, logger.Count);
                });
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

        static async Task Test(Func<GltfImport, Task<bool>> loadMethod, bool expectSuccess, Action<CollectingLogger> loggerAssertion)
        {
            var logger = new CollectingLogger();
            var import = new GltfImport(logger: logger);
            Assert.AreEqual(expectSuccess, await loadMethod(import));
            loggerAssertion(logger);
        }

        [UnityTest]
        public IEnumerator DataUriImageSharedByMultipleTextures()
        {
            var path = Path.Combine(Application.temporaryCachePath, "GltfDataUriImageSharedByMultipleTextures.gltf");
            // One image (base-64 PNG), two samplers (different magFilter), two textures sharing the image.
            var gltf = $@"
{{
    ""images"":[{{""uri"":""{k_Png1PxDataUri}""}}],
    ""samplers"":[
        {{""magFilter"":9728}},
        {{""magFilter"":9729}}
    ],
    ""textures"":[
        {{""source"":0,""sampler"":0}},
        {{""source"":0,""sampler"":1}}
    ]
}}";
            File.WriteAllText(path, gltf);
            var task = Test(
                async import =>
                {
                    var success = await import.LoadAsync(path);
                    Assert.AreEqual(2, import.TextureCount);
                    Assert.NotNull(import.GetTexture(0), "Texture 0 must be loaded.");
                    Assert.NotNull(import.GetTexture(1), "Texture 1 must be loaded (shares image with texture 0).");
                    return success;
                },
                true,
                logger =>
                {
                    // EmbedSlow (data URI) is always emitted. ImageMultipleSamplers is DEBUG-only.
                    foreach (var item in logger.Items)
                    {
                        Assert.AreNotEqual(LogCode.EmbedImageLoadFailed, item.Code,
                            "Image load must not fail when the same image is referenced by multiple textures.");
                    }
                });
            yield return AsyncWrapper.WaitForTask(task);
            File.Delete(path);
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
