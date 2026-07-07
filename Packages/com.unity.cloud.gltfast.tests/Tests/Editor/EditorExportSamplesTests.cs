// SPDX-FileCopyrightText: 2026 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

namespace GLTFast.Editor.Tests.DocExamples
{
    [Category("DocExamples")]
    class EditorExportSamplesTests
    {
        [UnityTest]
        public IEnumerator BatchExportAllObjects()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cube1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube1.name = "BatchExportTestCube1";
            var cube2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube2.name = "BatchExportTestCube2";

            var path1 = $"Assets/{cube1.name}.glb";
            var path2 = $"Assets/{cube2.name}.glb";

            try
            {
                var executed = EditorApplication.ExecuteMenuItem("Tools/glTFast Examples/Batch Export");
                Assert.IsTrue(executed, "Could not execute Batch Export menu item.");

                Assert.IsTrue(File.Exists(path1), $"Expected exported file at {path1}.");
                Assert.IsTrue(File.Exists(path2), $"Expected exported file at {path2}.");
            }
            finally
            {
                AssetDatabase.DeleteAsset(path1);
                AssetDatabase.DeleteAsset(path2);
                Object.DestroyImmediate(cube1);
                Object.DestroyImmediate(cube2);
            }
            yield break;
        }
    }
}
