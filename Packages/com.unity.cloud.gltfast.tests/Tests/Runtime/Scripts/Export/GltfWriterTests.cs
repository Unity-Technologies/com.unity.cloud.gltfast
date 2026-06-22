// SPDX-FileCopyrightText: 2024 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using GLTFast.Export;
using GLTFast.Logging;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace GLTFast.Tests.Export
{
    [Category("Export")]
    class GltfWriterTests
    {
        [UnityTest]
        public IEnumerator ToStreamNotSelfContained()
        {
            var logger = new CollectingLogger();

            yield return AsyncWrapper.WaitForTask(
                ToStreamNotSelfContained(logger)
            );

            LoggerTest.AssertLogger(
                logger,
                new[]
                {
                    new LogItem(
                        LogType.Error,
                        LogCode.None,
                        "Save to Stream currently only works for self-contained glTF-Binary"
                        )
                });
        }

        /// <summary>
        /// Write a non-self-contained glTF to stream is not supported. Previously this resulted in a
        /// NullReferenceException when no logger was provided.
        /// </summary>
        /// <seealso href="https://github.com/Unity-Technologies/com.unity.cloud.gltfast/pull/9"/>
        /// <returns>Coroutine iterator</returns>
        [UnityTest]
        public IEnumerator ToStreamNotSelfContainedNoLogger()
        {
            yield return AsyncWrapper.WaitForTask(
                ToStreamNotSelfContained(null)
            );
        }

        static async Task ToStreamNotSelfContained(ICodeLogger logger)
        {
            var writer = new GltfWriter(
                new ExportSettings
                {
                    Format = GltfFormat.Binary,
                    ImageDestination = ImageDestination.SeparateFile
                },
                logger: logger
            );

            await writer.SaveToStreamAndDispose(new MemoryStream());
        }

        [UnityTest]
        public IEnumerator DracoUncompressedFallback()
        {
#if DRACO_IS_RECENT
            var logger = new CollectingLogger();
            yield return AsyncWrapper.WaitForTask(
                DracoUncompressedFallback(logger)
                );

            LoggerTest.AssertLogger(
                logger,
                new[]
                {
                    new LogItem(
                        LogType.Warning,
                        LogCode.UncompressedFallbackNotSupported
                    )
                });
#else
            Assert.Ignore("Requires Draco for Unity package to be installed.");
            yield return null;
#endif
        }

        [UnityTest]
        public IEnumerator DracoUncompressedFallbackNoLogger()
        {
#if DRACO_IS_RECENT
            yield return AsyncWrapper.WaitForTask(
                DracoUncompressedFallback(null)
            );
#else
            Assert.Ignore("Requires Draco for Unity package to be installed.");
            yield return null;
#endif
        }

        [UnityTest]
        public IEnumerator MeshoptCompression()
        {
#if MESHOPT_IS_RECENT
            yield return AsyncWrapper.WaitForTask(
                MeshoptCompressionTest()
            );
#else
            Assert.Ignore("Requires meshoptimizer decompression for Unity package to be installed.");
            yield return null;
#endif
        }

        [UnityTest]
        public IEnumerator MeshCubeQuadsSubMeshUInt16()
        {
            var logger = new CollectingLogger();

            yield return AsyncWrapper.WaitForTask(
                MeshCubeQuadsSubMesh(IndexFormat.UInt16, logger)
            );
        }

        [UnityTest]
        public IEnumerator MeshCubeQuadsSubMeshUInt32()
        {
            var logger = new CollectingLogger();

            yield return AsyncWrapper.WaitForTask(
                MeshCubeQuadsSubMesh(IndexFormat.UInt32, logger)
            );
        }

        static async Task DracoUncompressedFallback(ICodeLogger logger)
        {
            var writer = new GltfWriter(
                new ExportSettings
                {
                    Format = GltfFormat.Binary,
                    Compression = Compression.Uncompressed | Compression.Draco
                },
                logger: logger
            );

            var node = writer.AddNode();
            var tmpGameObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
            writer.AddMeshToNode((int)node, tmpGameObject.GetComponent<MeshFilter>().sharedMesh, null, (List<uint>)null);

            await writer.SaveToStreamAndDispose(new MemoryStream());

            Object.Destroy(tmpGameObject);
        }

        static async Task MeshoptCompressionTest()
        {
            var logger = new CollectingLogger();
            var writer = new GltfWriter(
                new ExportSettings
                {
                    Format = GltfFormat.Binary,
                    Compression = Compression.MeshOpt
                },
                logger: logger
            );

            var node = writer.AddNode();
            var tmpGameObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
            writer.AddMeshToNode((int)node, tmpGameObject.GetComponent<MeshFilter>().sharedMesh, null, (List<uint>)null);

            await writer.SaveToStreamAndDispose(new MemoryStream());

            LoggerTest.AssertLogger(
                logger,
                new[]
                {
                    new LogItem(
                        LogType.Error,
                        LogCode.None,
                        "Meshopt compression is not supported yet."
                    )
                }
                );
            Object.Destroy(tmpGameObject);
        }

        static async Task MeshCubeQuadsSubMesh(IndexFormat indexFormat, ICodeLogger logger)
        {
            var writer = new GltfWriter(logger: logger);
            var node = writer.AddNode();
            var materials = new int[6];
            var materialExport = MaterialExport.GetDefaultMaterialExport();
            var shader = Shader.Find("Standard");
            writer.AddMaterial(new Material(shader) { color = Color.red }, out materials[0], materialExport);
            writer.AddMaterial(new Material(shader) { color = Color.green }, out materials[1], materialExport);
            writer.AddMaterial(new Material(shader) { color = Color.blue }, out materials[2], materialExport);
            writer.AddMaterial(new Material(shader) { color = Color.yellow }, out materials[3], materialExport);
            writer.AddMaterial(new Material(shader) { color = Color.magenta }, out materials[4], materialExport);
            writer.AddMaterial(new Material(shader) { color = Color.cyan }, out materials[5], materialExport);
            writer.AddMeshToNode((int)node, TestMeshGenerator.GenerateSubMeshCube(indexFormat), materials, (List<uint>)null);
            writer.AddScene(new List<uint> { node }, "CubeScene");
            await writer.SaveToFileAndDispose(Path.Combine(Application.persistentDataPath, $"MeshCubeQuadsSubMesh-{indexFormat}.gltf"));
        }
    }
}
