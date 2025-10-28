// SPDX-FileCopyrightText: 2024 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using GLTFast.Export;
using GLTFast.Logging;
using NUnit.Framework;
#if GLTF_VALIDATOR && UNITY_EDITOR
using UnityEditor.Formats.Gltf.Validation;
#endif // GLTF_VALIDATOR
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace GLTFast.Tests.Export
{
    [Category("Export")]
    class ExportNonReadableTests
    {
        static GameObject s_NonReadableTriangle;

        [UnityTest]
        public IEnumerator NonReadableMesh()
        {
            Certify();
            var model = Object.Instantiate(s_NonReadableTriangle);
            var task = ExportObjects(
                "NonReadableMesh",
                new[] { model }
            );
            yield return AsyncWrapper.WaitForTask(task);
        }

        [UnityTest]
        public IEnumerator MixedReadableMesh()
        {
            Certify();
            var model = Object.Instantiate(s_NonReadableTriangle);
            var readable = GameObject.CreatePrimitive(PrimitiveType.Cube);

            var mesh = readable.GetComponent<MeshFilter>().sharedMesh;
            Assert.IsTrue(mesh.isReadable);

            var task = ExportObjects(
                "MixedReadableMesh-01",
                new[] { model, readable }
                );
            yield return AsyncWrapper.WaitForTask(task);

            task = ExportObjects(
                "MixedReadableMesh-10",
                new[] { readable, model }
                );
            yield return AsyncWrapper.WaitForTask(task);
        }

        [UnityTest]
        public IEnumerator DracoMixedReadableMesh()
        {
#if !DRACO_IS_RECENT
            Assert.Ignore("Test requires Draco for Unity (com.unity.cloud.draco) to be installed");
#endif
            Certify();
            var model = Object.Instantiate(s_NonReadableTriangle);
            var readable = GameObject.CreatePrimitive(PrimitiveType.Cube);

            var mesh = readable.GetComponent<MeshFilter>().sharedMesh;
            Assert.IsTrue(mesh.isReadable);

            var dracoSettings = new ExportSettings
            {
                Compression = Compression.Draco
            };

            var task = ExportObjects(
                "DracoMixedReadableMesh-01",
                new[] { model, readable },
                dracoSettings,
                new[] { LogCode.MeshNotReadable }
                );
            yield return AsyncWrapper.WaitForTask(task);

            task = ExportObjects(
                "DracoMixedReadableMesh-10",
                new[] { readable, model },
                dracoSettings,
                new[] { LogCode.MeshNotReadable }
                );
            yield return AsyncWrapper.WaitForTask(task);
        }

        static async Task ExportObjects(
            string name,
            GameObject[] nodes,
            ExportSettings settings = null,
            IEnumerable<LogCode> expectedLogCodes = null
            )
        {
            var logger = new CollectingLogger();
            var export = new GameObjectExport(exportSettings: settings, logger: logger);
            export.AddScene(nodes);
            var path = Path.Combine(Application.persistentDataPath, $"{name}.gltf");
            var success = await export.SaveToFileAndDispose(path);
            Assert.IsTrue(success);
            LoggerTest.AssertLogger(logger, expectedLogCodes);
            ExportTests.ValidateGltf(path);
        }

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            s_NonReadableTriangle = Resources.Load<GameObject>($"Export/Models/NonReadableTriangle");
            var mesh = s_NonReadableTriangle.GetComponent<MeshFilter>().sharedMesh;
            Assert.IsFalse(mesh.isReadable);
        }

        internal static void Certify()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLCore
                || SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES3
#if !UNITY_2023_1_OR_NEWER
                || SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES2
#endif
                )
            {
                Assert.Ignore("Exporting non-readable meshes is unreliable on OpenGL/OpenGLES.");
            }
#if !(UNITY_6000_0_OR_NEWER || UNITY_EDITOR)
            Assert.Ignore("Exporting non-readable meshes is unreliable on Unity versions before 6.");
#endif
        }
    }
}
