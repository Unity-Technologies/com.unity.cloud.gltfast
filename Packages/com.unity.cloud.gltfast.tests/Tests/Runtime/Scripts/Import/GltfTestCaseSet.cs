// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0


#if UNITY_ANDROID || UNITY_IOS || UNITY_TVOS || UNITY_VISIONOS || UNITY_WEBGL || UNITY_WSA || UNITY_LUMIN
#define STREAMING_ASSETS_PLATFORM
#endif

#if !UNITY_EDITOR && STREAMING_ASSETS_PLATFORM
#define LOAD_FROM_STREAMING_ASSETS
#endif

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
#if UNITY_EDITOR
using System.Threading;
using UnityEditor;
#endif
using UnityEngine;
#if LOAD_FROM_STREAMING_ASSETS
using UnityEngine.Networking;
#endif

namespace GLTFast.Tests.Import
{
    [CreateAssetMenu(fileName = "glTF-TestCaseCollection", menuName = "ScriptableObjects/glTFast Test Case Collection")]
    class GltfTestCaseSet : ScriptableObject
    {
        public static bool IsStreamingAssetsPlatform =>
#if STREAMING_ASSETS_PLATFORM
            true;
#else
            false;
#endif

        /// <summary>
        /// Path relative to "Assets", a folder at root level of the repository.
        /// </summary>
        public string assetsRelativePath;

        public string assetsAbsolutePath;

        [SerializeField]
        GltfTestCase[] m_TestCases;

        public int TestCaseCount => m_TestCases?.Length ?? 0;

        string StreamingAssetsPath => $"gltfast/{assetsRelativePath}";

        public IEnumerable<GltfTestCase> IterateTestCases(GltfTestCaseFilter filter = null)
        {
            foreach (var testCase in m_TestCases)
            {
                if (filter == null || filter.Matches(testCase))
                {
                    yield return testCase;
                }
            }
        }

        public uint GetTestCaseCount(GltfTestCaseFilter filter = null)
        {
            var count = 0u;
            foreach (var testCase in m_TestCases)
            {
                if (filter == null || filter.Matches(testCase))
                {
                    count++;
                }
            }
            return count;
        }

        public static GltfTestCaseSet DeserializeFromStreamingAssets(string path)
        {
            var fullPath = Path.Combine(Application.streamingAssetsPath, path);
#if LOAD_FROM_STREAMING_ASSETS
            var request = UnityWebRequest.Get(fullPath);
            var it = request.SendWebRequest();
            while(!it.isDone) {}

            if (request.result != UnityWebRequest.Result.Success)
            {
                throw new IOException($"Loading GltfTestCaseSet from {fullPath} failed!");
            }

            var json = request.downloadHandler.text;
#else
            var json = File.ReadAllText(fullPath);
#endif
            var sampleSet = CreateInstance<GltfTestCaseSet>();
            JsonUtility.FromJsonOverwrite(json, sampleSet);
            return sampleSet;
        }

        public string RootPath =>
#if LOAD_FROM_STREAMING_ASSETS
            Path.Combine(Application.streamingAssetsPath, StreamingAssetsPath);
#else
#if UNITY_EDITOR
            SourcePath;
#else
            assetsAbsolutePath;
#endif
#endif

#if UNITY_EDITOR
        /// <summary>
        /// Get assets path from the tests repository.
        /// </summary>
        /// <returns>Path to glTFastTest project specific assets folder</returns>
        static string GetAssetsPath()
        {
            return Path.GetFullPath("Packages/com.unity.cloud.gltfast.tests/Assets~");
        }

        string SourcePath => Path.Combine(GetAssetsPath(), assetsRelativePath);

        public void SerializeToStreamingAssets()
        {
#if !STREAMING_ASSETS_PLATFORM
            assetsAbsolutePath = SourcePath;
#endif
            var jsonPathAbsolute = Path.Combine(Application.streamingAssetsPath, $"{name}.json");
            File.WriteAllText(jsonPathAbsolute, ToJson());
        }

        public void CopyToStreamingAssets(bool force = false)
        {
            var srcPath = SourcePath;
            if (string.IsNullOrEmpty(srcPath) || !Directory.Exists(srcPath))
            {
                Debug.LogError($"Invalid source path: \"{srcPath}\"");
                return;
            }

            var dstPath = Path.Combine(Application.streamingAssetsPath, StreamingAssetsPath);

            if (Directory.Exists(dstPath))
            {
                if (force)
                {
                    Directory.Delete(dstPath);
                }
                else
                {
                    return;
                }
            }
            else
            {
                var parent = Directory.GetParent(dstPath)?.FullName;
                if (parent != null && !Directory.Exists(parent))
                {
                    Directory.CreateDirectory(parent);
                }
            }

            FileUtil.CopyFileOrDirectory(srcPath, dstPath);
        }

        string ToJson()
        {
            return JsonUtility.ToJson(this);
        }

        [ContextMenu("Scan for glTF test files")]
        public void ScanAndUpdateGltfTestCases()
        {
            ScanAndUpdateGltfTestCases("*.gl*");
        }

        async void ScanAndUpdateGltfTestCases(string searchPattern)
        {
            var dir = new DirectoryInfo(SourcePath);
            var dirLength = dir.FullName.Length + 1;

            var newTestCases = new List<GltfTestCase>();

            foreach (var file in dir.GetFiles(searchPattern, SearchOption.AllDirectories))
            {
                var ext = file.Extension;
                if (ext != ".gltf" && ext != ".glb") continue;
                // var i = CreateInstance<GltfTestCase>();
                var i = new GltfTestCase
                {
                    relativeUri = file.FullName.Substring(dirLength),
                    requiredExtensions = await GetRequiredExtensions(file.FullName, new CancellationToken())
                };
                newTestCases.Add(i);
            }

            m_TestCases = newTestCases.ToArray();
            AssetDatabase.Refresh();
        }

        async Task<Extension[]> GetRequiredExtensions(string filePath, CancellationToken cancellationToken)
        {
            var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            if (!stream.CanRead)
            {
                return null;
            }
            var initialStreamPosition = stream.CanSeek
                ? stream.Position
                : -1L;
            var firstBytes = new byte[4];
            if (!await stream.ReadToArrayAsync(firstBytes, 0, firstBytes.Length, cancellationToken))
                return null;
            string json = null;
            if (GltfGlobals.IsGltfBinary(firstBytes))
            {
                // Read the rest of the header
                var glbHeader = new byte[8];
                if (!await stream.ReadToArrayAsync(glbHeader, 0, glbHeader.Length, cancellationToken))
                    return null;
                // Length of the entire glTF, including the header
                var length = BitConverter.ToUInt32(glbHeader, 4);
                if (length >= int.MaxValue)
                {
                    // glTF-binary supports up to 2^32 = 4GB, but C# arrays have a 2^31 (2GB) limit.
                    return null;
                }

                var chunkHeader = new byte[8];
                while (stream.Position < stream.Length)
                {
                    if (!await stream.ReadToArrayAsync(chunkHeader, 0, chunkHeader.Length, cancellationToken))
                        return null;
                    var chLength = BitConverter.ToUInt32(chunkHeader, 0);
                    var chType = (ChunkFormat)BitConverter.ToUInt32(chunkHeader, 4);

                    if (chType == ChunkFormat.Json)
                    {
                        var buffer = new byte[chLength];
                        _ = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                        json = Encoding.UTF8.GetString(buffer);
                        break;
                    }
                    stream.Seek(chLength, SeekOrigin.Current);
                }
            }
            else
            {
                var reader = new StreamReader(stream);
                if (stream.CanSeek)
                {
                    stream.Seek(initialStreamPosition, SeekOrigin.Begin);
                    json = await reader.ReadToEndAsync();
                }
                else
                {
                    // TODO: String concat likely leads to another copy in memory and bad performance.
                    json = Encoding.UTF8.GetString(firstBytes) + await reader.ReadToEndAsync();
                }
                reader.Dispose();
            }

            if (json == null)
                return null;

            var gltf = new GltfJsonUtilityParser().ParseJson(json);

            if (gltf.extensionsRequired != null)
            {
                var extensionsRequired = new List<Extension>();
                foreach (var extensionName in gltf.extensionsRequired)
                {
                    var ext = ExtensionExtensions.FromName(extensionName);
                    if (ext.HasValue)
                    {
                        extensionsRequired.Add(ext.Value);
                    }
                }
                return extensionsRequired.ToArray();
            }

            return null;
        }
#endif
    }
}
