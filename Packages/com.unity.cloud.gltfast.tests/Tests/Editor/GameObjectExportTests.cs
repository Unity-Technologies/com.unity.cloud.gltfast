// SPDX-FileCopyrightText: 2025 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Cloud.Gltfast.Tests;
using Unity.Cloud.Gltfast.Tests.Export;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Cloud.Gltfast.Editor.Tests.Export
{
    [Category("Export")]
    class GameObjectExportTests
    {
        static GameObject s_NonReadableTriangle;

        [UnityTest]
        public IEnumerator NonReadableMesh()
        {
            ExportNonReadableTests.Certify();
            Selection.activeGameObject = s_NonReadableTriangle;
            var path = Path.Combine(Application.temporaryCachePath, "NonReadableMesh.gltf");
            var task = MenuEntries.Export(path, false, "NonReadableMesh", new[] { s_NonReadableTriangle });
            yield return AsyncWrapper.WaitForTask(task);
#if GLTF_VALIDATOR
            // glTF Validation has been performed in `MenuEntries.Export`
#else
            Assert.Inconclusive("glTF-Validator for Unity is not installed. Cannot validate exported glTF.");
#endif
        }

        [UnityTest]
        public IEnumerator MixedReadableMesh()
        {
            const string name = "MixedReadableMesh";
            ExportNonReadableTests.Certify();
            var nonReadable = Object.Instantiate(s_NonReadableTriangle);
            var readable = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var mesh = readable.GetComponent<MeshFilter>().sharedMesh;
            Assert.IsTrue(mesh.isReadable);

            var task = ExportObjects(
                $"{name}-01",
                new[] { nonReadable, readable },
                false
            );
            yield return AsyncWrapper.WaitForTask(task);

            task = ExportObjects(
                $"{name}-10",
                new[] { readable, nonReadable },
                false
            );
            yield return AsyncWrapper.WaitForTask(task);
        }

        [Test]
        public void ExportForceSync()
        {
            const string name = "ExportForceSync";
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            var path = Path.Combine(Application.temporaryCachePath, $"{name}.gltf");
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            try
            {
                var export = new global::Unity.Cloud.Gltfast.Export.GameObjectExport();
                Assert.IsTrue(export.AddScene(new[] { cube }, name));

                var task = export.SaveToFileAndDisposeAsync(path, forceSync: true);
                Assert.IsTrue(task.IsCompleted, "SaveToFileAndDisposeAsync(forceSync: true) should complete synchronously.");
                Assert.IsTrue(task.Result, "Export returned false.");
                FileAssert.Exists(path);
            }
            finally
            {
                Object.DestroyImmediate(cube);
            }
        }

        [Test]
        public void ExportStreamForceSync()
        {
            const string name = "ExportStreamForceSync";
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;

            try
            {
                var settings = new global::Unity.Cloud.Gltfast.Export.ExportSettings
                {
                    Format = global::Unity.Cloud.Gltfast.Export.GltfFormat.Binary
                };
                var export = new global::Unity.Cloud.Gltfast.Export.GameObjectExport(settings);
                Assert.IsTrue(export.AddScene(new[] { cube }, name));

                using var stream = new MemoryStream();
                var task = export.SaveToStreamAndDisposeAsync(stream, forceSync: true);
                Assert.IsTrue(task.IsCompleted, "SaveToStreamAndDisposeAsync(forceSync: true) should complete synchronously.");
                Assert.IsTrue(task.Result, "Export returned false.");
                Assert.Greater(stream.Length, 0, "Expected stream to contain exported glTF data.");
            }
            finally
            {
                Object.DestroyImmediate(cube);
            }
        }

        static async Task ExportObjects(
            string name,
            GameObject[] gameObjects,
            bool binary
        )
        {
            var ext = binary ? Constants.gltfBinaryExtension : Constants.gltfExtension;
            var path = Path.Combine(Application.temporaryCachePath, $"{name}.{ext}");
            await MenuEntries.Export(path, false, name, gameObjects);
#if GLTF_VALIDATOR
            // glTF Validation has been performed in `MenuEntries.Export`
#else
            Assert.Inconclusive("glTF-Validator for Unity is not installed. Cannot validate exported glTF.");
#endif
        }

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            s_NonReadableTriangle = AssetDatabase.LoadAssetAtPath<GameObject>(
                $"Packages/{GltfGlobals.GltfPackageName}.tests/Tests/Resources/Export/Models/NonReadableTriangle.fbx");
            var mesh = s_NonReadableTriangle.GetComponent<MeshFilter>().sharedMesh;
            Assert.IsFalse(mesh.isReadable);
        }
    }
}
