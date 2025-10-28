// SPDX-FileCopyrightText: 2024 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

#if UNITY_ANDROID && !UNITY_EDITOR
#define USE_WEB_REQUEST
#endif

using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;
using GLTFast.Logging;
using Unity.Collections;
#if USE_WEB_REQUEST
using UnityEngine.Networking;
#endif
using UnityEngine.TestTools;

namespace GLTFast.Tests.Import
{
    /// <summary>
    /// Tests all of <see cref="GltfImport"/>'s load methods.
    /// </summary>
    [Category("Import")]
    class LoadTests
    {
        const string k_RelativeUriFilter = @"\/RelativeUri\.gl(b|tf)$";
        const string k_RelativeUriJsonFilter = @"\/RelativeUri\.gltf$";
        const string k_RelativeUriBinaryFilter = @"\/RelativeUri\.glb$";

        enum LoadType
        {
            Path,
            NativeArray,
            ManagedByteArray,
            Uri,
            File,
            Binary,
            Json,
            Stream
        }

        enum InstantiationType
        {
            MainSync,
            Main,
            MainAndFirst
        }

        [GltfTestCase("glTF-test-models", 2, k_RelativeUriFilter)]
        public IEnumerator LoadString(GltfTestCaseSet testCaseSet, GltfTestCase testCase)
        {
            var task = LoadInternal(testCaseSet, testCase, LoadType.Path, InstantiationType.Main);
            yield return Utils.WaitForTask(task);
        }

        [GltfTestCase("glTF-test-models", 2, k_RelativeUriFilter)]
        public IEnumerator LoadUri(GltfTestCaseSet testCaseSet, GltfTestCase testCase)
        {
            var task = LoadInternal(testCaseSet, testCase, LoadType.Uri, InstantiationType.MainAndFirst);
            yield return Utils.WaitForTask(task);
        }

        [GltfTestCase("glTF-test-models", 2, k_RelativeUriFilter)]
        public IEnumerator Load(GltfTestCaseSet testCaseSet, GltfTestCase testCase)
        {
            var task = LoadInternal(testCaseSet, testCase, LoadType.NativeArray, InstantiationType.Main);
            yield return Utils.WaitForTask(task);
        }

        [GltfTestCase("glTF-test-models", 2, k_RelativeUriFilter)]
        public IEnumerator LoadByteArray(GltfTestCaseSet testCaseSet, GltfTestCase testCase)
        {
            var task = LoadInternal(testCaseSet, testCase, LoadType.ManagedByteArray, InstantiationType.Main);
            yield return Utils.WaitForTask(task);
        }

        [GltfTestCase("glTF-test-models", 2, k_RelativeUriFilter)]
        public IEnumerator LoadSyncInstantiation(GltfTestCaseSet testCaseSet, GltfTestCase testCase)
        {
            var task = LoadInternal(testCaseSet, testCase, LoadType.Path, InstantiationType.MainSync);
            yield return Utils.WaitForTask(task);
        }

        [GltfTestCase("glTF-test-models", 2, k_RelativeUriFilter)]
        public IEnumerator LoadFile(GltfTestCaseSet testCaseSet, GltfTestCase testCase)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            Assert.Ignore("Cannot load from StreamingAssets file on Android, as they are in the compressed JAR file.");
#endif
            var task = LoadInternal(testCaseSet, testCase, LoadType.File, InstantiationType.Main);
            yield return Utils.WaitForTask(task);
        }

        [GltfTestCase("glTF-test-models", 1, k_RelativeUriBinaryFilter)]
        public IEnumerator LoadBinary(GltfTestCaseSet testCaseSet, GltfTestCase testCase)
        {
            var task = LoadInternal(testCaseSet, testCase, LoadType.Binary, InstantiationType.Main);
            yield return Utils.WaitForTask(task);
        }

        [GltfTestCase("glTF-test-models", 2, k_RelativeUriFilter)]
        public IEnumerator LoadStream(GltfTestCaseSet testCaseSet, GltfTestCase testCase)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            Assert.Ignore("Cannot stream from StreamingAssets on Android, as they are in the compressed JAR file.");
#endif
            var task = LoadInternal(testCaseSet, testCase, LoadType.Stream, InstantiationType.Main);
            yield return Utils.WaitForTask(task);
        }

        [GltfTestCase("glTF-test-models", 1, k_RelativeUriJsonFilter)]
        public IEnumerator LoadJson(GltfTestCaseSet testCaseSet, GltfTestCase testCase)
        {
            var task = LoadInternal(testCaseSet, testCase, LoadType.Json, InstantiationType.Main);
            yield return Utils.WaitForTask(task);
        }

        [UnityTest]
        public IEnumerator LoadStreamBigGltf()
        {
            // Create header-only glTF-binary that's too big (4GB).
            var stream = new MemoryStream();
            // glTF magic
            stream.Write(BitConverter.GetBytes(GltfGlobals.GltfBinaryMagic), 0, 4);
            // glTF version
            stream.Write(BitConverter.GetBytes(2u), 0, 4);
            // Total size
            stream.Write(BitConverter.GetBytes(uint.MaxValue), 0, 4);
            stream.Seek(0, SeekOrigin.Begin);

            var deferAgent = new UninterruptedDeferAgent();
            var logger = new CollectingLogger();
            using var gltf = new GltfImport(deferAgent: deferAgent, logger: logger);
            var task = gltf.LoadStream(stream);
            yield return Utils.WaitForTask(task);
            stream.Dispose();
            var success = task.Result;
            Assert.IsFalse(success);
            LoggerTest.AssertLogger(
                logger,
                new[]
                {
                    new LogItem(
                        LogType.Error,
                        LogCode.None,
                        "glb exceeds 2GB limit."
                    )
                });
        }

        static async Task LoadInternal(
            GltfTestCaseSet testCaseSet,
            GltfTestCase testCase,
            LoadType loadType,
            InstantiationType instantiationType
            )
        {
            var path = Path.Combine(testCaseSet.RootPath, testCase.relativeUri);
            Debug.Log($"Testing {path}");
            var go = new GameObject();
            var deferAgent = new UninterruptedDeferAgent();
            var logger = new ConsoleLogger();
            using var gltf = new GltfImport(deferAgent: deferAgent, logger: logger);
            bool success;
            switch (loadType)
            {
                case LoadType.NativeArray:
                {
                    using var data = await ReadNativeArrayAsync(path);
                    success = await gltf.Load(data.AsReadOnly(), new Uri(path));
                    break;
                }
                case LoadType.ManagedByteArray:
                {
                    var data = await ReadAllBytesAsync(path);
                    success = await gltf.Load(data, new Uri(path));
                    break;
                }
                case LoadType.Path:
                    success = await gltf.Load(path);
                    break;
                case LoadType.Uri:
                    var uri = new Uri(path, UriKind.RelativeOrAbsolute);
                    success = await gltf.Load(uri);
                    break;
                case LoadType.File:
                    success = await gltf.LoadFile(path, new Uri(path));
                    break;
                case LoadType.Binary:
                {
                    var data = await ReadAllBytesAsync(path);
#pragma warning disable CS0618 // Type or member is obsolete
                    success = await gltf.LoadGltfBinary(data, new Uri(path));
#pragma warning restore CS0618 // Type or member is obsolete
                    break;
                }
                case LoadType.Stream:
                    var stream = new FileStream(path, FileMode.Open);
                    success = await gltf.LoadStream(stream, new Uri(path));
                    await stream.DisposeAsync();
                    break;
                case LoadType.Json:
                    var json = await ReadAllTextAsync(path);
                    success = await gltf.LoadGltfJson(json, new Uri(path));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(loadType), loadType, null);
            }
            Assert.IsTrue(success);
            var instantiator = AssetsTests.CreateInstantiator(gltf, logger, go.transform);
            switch (instantiationType)
            {
                case InstantiationType.Main:
                    success = await gltf.InstantiateMainSceneAsync(instantiator);
                    break;
                case InstantiationType.MainSync:
#pragma warning disable CS0618
                    // ReSharper disable once MethodHasAsyncOverload
                    success = gltf.InstantiateMainScene(instantiator);
#pragma warning restore CS0618
                    break;
                case InstantiationType.MainAndFirst:
                    success = await gltf.InstantiateMainSceneAsync(go.transform);
                    Assert.IsTrue(success);
                    var firstSceneGameObject = new GameObject("firstScene");
                    success = await gltf.InstantiateSceneAsync(firstSceneGameObject.transform);
                    Assert.IsTrue(success);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(instantiationType), instantiationType, null);
            }
            Assert.IsTrue(success);
            Object.Destroy(go);
        }

        // TODO: Remove pragma, as is is required for 2020 LTS and earlier only.
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        internal static async Task<NativeArray<byte>> ReadNativeArrayAsync(string path)
        {
#if USE_WEB_REQUEST
            var downloadHandler = await UnityWebRequestDownload(path);
            // TODO: Use downloadHandler.nativeData directly!
            return new NativeArray<byte>(downloadHandler.data, Allocator.Persistent);
#else
            var data = await File.ReadAllBytesAsync(path);
            // TODO: Read into NativeArray directly!
            return new NativeArray<byte>(data, Allocator.Persistent);
#endif
        }

        // TODO: Remove pragma, as is is required for 2020 LTS and earlier only.
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        internal static async Task<byte[]> ReadAllBytesAsync(string path)
        {
#if USE_WEB_REQUEST
            var downloadHandler = await UnityWebRequestDownload(path);
            return downloadHandler.data;
#else
            // TODO: Read into NativeArray directly!
            return await File.ReadAllBytesAsync(path);
#endif
        }

        static async Task<string> ReadAllTextAsync(string path)
        {
#if USE_WEB_REQUEST
            var downloadHandler = await UnityWebRequestDownload(path);
            return downloadHandler.text;
#else
            return await File.ReadAllTextAsync(path);
#endif
        }
        // TODO: Remove pragma, as is is required for 2020 LTS and earlier only.
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

#if USE_WEB_REQUEST
        static async Task<DownloadHandler> UnityWebRequestDownload(string path)
        {
            var request = UnityWebRequest.Get(path);
            var asyncOp = request.SendWebRequest();
            while (!asyncOp.isDone)
            {
                await Task.Yield();
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                throw new IOException($"UnityWebRequest failed: {request.error}");
            }

            return request.downloadHandler;
        }
#endif
    }
}
