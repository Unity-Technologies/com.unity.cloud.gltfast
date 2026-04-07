// SPDX-FileCopyrightText: 2025 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using GLTFast.Export;
using Unity.Mathematics;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace GLTFast.Tests
{
    static class TestGltfGenerator
    {
        public enum Asset
        {
            CylinderWithMaterial
        }

        const string k_TestFileFolder = "gltf-perf";

        internal static string FlatHierarchyPath =>
            Path.Combine(Application.streamingAssetsPath, k_TestFileFolder, "flat-hierarchy.gltf");
        internal static string FlatHierarchyBinaryPath =>
            Path.Combine(Application.streamingAssetsPath, k_TestFileFolder, "flat-hierarchy.glb");
        internal static string BigCylinderPath =>
            Path.Combine(Application.streamingAssetsPath, k_TestFileFolder, "big-cylinder.gltf");
        internal static string BigCylinderBinaryPath =>
            Path.Combine(Application.streamingAssetsPath, k_TestFileFolder, "big-cylinder.glb");

#if UNITY_EDITOR
        static async void CreatePerformanceTestFiles()
        {
            try
            {
                await CreatePerformanceTestFilesAsync();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        static async Task CreatePerformanceTestFilesAsync()
        {
            var folder = Path.Combine(Application.streamingAssetsPath, k_TestFileFolder);
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            if (!File.Exists(FlatHierarchyPath))
                await CreateGltfFlatHierarchy(FlatHierarchyPath, 10_000, GltfFormat.Json);
            if (!File.Exists(FlatHierarchyBinaryPath))
                await CreateGltfFlatHierarchy(FlatHierarchyBinaryPath, 10_000, GltfFormat.Binary);
            if (!File.Exists(BigCylinderPath))
                await CreateGltfBigCylinderMesh(BigCylinderPath, 1_000_000, GltfFormat.Json);
            if (!File.Exists(BigCylinderBinaryPath))
                await CreateGltfBigCylinderMesh(BigCylinderBinaryPath, 1_000_000, GltfFormat.Binary);
            AssetDatabase.Refresh();
        }
#endif

        internal static async Task CertifyPerformanceTestGltfs()
        {
            var testFilesPresent = File.Exists(FlatHierarchyPath)
                && File.Exists(FlatHierarchyBinaryPath)
                && File.Exists(BigCylinderPath)
                && File.Exists(BigCylinderBinaryPath);

            if (!testFilesPresent)
            {
                try
                {
#if UNITY_EDITOR
                    await CreatePerformanceTestFilesAsync();
                    Debug.Log("Created test glTFs");
#else
                    throw new InvalidDataException("Performance test glTFs missing!");
#endif
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        public static string GetAssetPath(Asset asset, GltfFormat format = GltfFormat.Json)
        {
            return Path.Combine(
                Application.streamingAssetsPath,
                k_TestFileFolder,
                $"{asset.ToString()}.{(format == GltfFormat.Binary ? "glb" : "gltf")}"
                );
        }

        internal static async Task CreateTestAssetAsync(Asset asset, GltfFormat format = GltfFormat.Json)
        {
            var folder = Path.Combine(Application.streamingAssetsPath, k_TestFileFolder);
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            var path = GetAssetPath(asset, format);
            if (File.Exists(path))
                return;

            switch (asset)
            {
                case Asset.CylinderWithMaterial:
                    await CreateCylinderWithMaterial(path, format);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(asset), asset, null);
            }
        }

        static async Task CreateCylinderWithMaterial(string path, GltfFormat format)
        {
            var exportSettings = new ExportSettings
            {
                Format = format
            };
            var writer = new GltfWriter(exportSettings);

            var material = new Material(Shader.Find("glTF/PbrMetallicRoughness")) { name = "MyGltfMaterial" };
            var materialExport = MaterialExport.GetDefaultMaterialExport();
            writer.AddMaterial(material, out var materialId, materialExport);

            var cylinderMesh = TestMeshGenerator.GenerateCylinderMesh(6);

            var nodes = new List<uint>();
            var nodeId = writer.AddNode(new float3(0), name: "Cylinder");
            nodes.Add(nodeId);

            writer.AddMeshToNode((int)nodeId, cylinderMesh, new[] { materialId }, null);
            writer.AddScene(nodes.ToArray());
            await writer.SaveToFileAndDisposeInternal(path, true);
        }

        static async Task CreateGltfFlatHierarchy(string path, int nodeCount, GltfFormat format)
        {
            var exportSettings = new ExportSettings
            {
                Format = format
            };
            var writer = new GltfWriter(exportSettings);
            var row = (int)math.ceil(math.pow(nodeCount, 1 / 3f));
            var count = 0;
            var nodes = new List<uint>();
            for (var x = 0; x < row && count < nodeCount; x++)
            {
                for (var y = 0; y < row && count < nodeCount; y++)
                {
                    for (var z = 0; z < row; z++)
                    {
                        if (count++ >= nodeCount)
                            break;
                        var nodeId = writer.AddNode(new float3(x, y, z), name: $"Node-{x}-{y}-{z}");
                        nodes.Add(nodeId);
                    }
                }
            }
            writer.AddScene(nodes.ToArray());
            await writer.SaveToFileAndDisposeInternal(path, true);
        }

        static async Task CreateGltfBigCylinderMesh(string path, uint triangleCount, GltfFormat format)
        {
            var exportSettings = new ExportSettings
            {
                Format = format
            };
            var writer = new GltfWriter(exportSettings);
            var nodeId = writer.AddNode(name: "Cylinder");
            var mesh = TestMeshGenerator.GenerateCylinderMesh(triangleCount);
            writer.AddMeshToNode(
                (int)nodeId,
                mesh,
                null,
                null
                );
            writer.AddScene(new[] { nodeId });
            await writer.SaveToFileAndDisposeInternal(path, true);
        }
    }
}
