// SPDX-FileCopyrightText: 2024 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections;
using GLTFast.Tests.Import;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

#if !GLTFAST_EDITOR_IMPORT_OFF
using System.IO;
using UnityEngine.TestTools;
#endif

namespace GLTFast.Editor.Tests
{
    [TestFixture]
    class GltfImporterTest
    {
        const string k_TestPath = "Temp-glTF-tests";

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            if (!AssetDatabase.IsValidFolder($"Assets/{k_TestPath}"))
            {
                AssetDatabase.CreateFolder("Assets", k_TestPath);
                AssetDatabase.Refresh();
            }
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            AssetDatabase.DeleteAsset($"Assets/{k_TestPath}");
            AssetDatabase.Refresh();
        }

        [GltfTestCase("glTF-test-models", 47)]
        public IEnumerator GltfTestModels(GltfTestCaseSet testCaseSet, GltfTestCase testCase)
        {
#if GLTFAST_EDITOR_IMPORT_OFF
            Assert.Ignore("glTF Editor import is disabled via GLTFAST_EDITOR_IMPORT_OFF scripting define.");
#else
            AssetsTests.AssertRequiredExtensions(testCase.requiredExtensions);
            var directories = testCase.relativeUri.Split('/');
            Assert.NotNull(directories);
            Assert.GreaterOrEqual(directories.Length, 2);

            var srcDir = "";
            var destDir = $"Assets/{k_TestPath}";

            for (var i = 0; i < directories.Length - 1; i++)
            {
                var dir = directories[i];
                srcDir = Path.Combine(srcDir, dir);
                destDir = Path.Combine(destDir, dir);
                if (i < directories.Length - 2 && !AssetDatabase.IsValidFolder(destDir))
                {
                    Directory.CreateDirectory(destDir);
                }
            }

            var destination = Path.Combine(destDir, directories[directories.Length - 1]);

            if (testCase.expectedLogCodes.Length > 0)
            {
                LogAssert.Expect(LogType.Error, $"Failed to import {destination.Replace('\\', '/')} (see inspector for details)");
            }

            if (!AssetDatabase.IsValidFolder(destDir))
            {
                var sourcePath = Path.Combine(testCaseSet.RootPath, srcDir);
                FileUtil.CopyFileOrDirectory(sourcePath, destDir);
                AssetDatabase.Refresh();
            }

            var importer = (GltfImporter)AssetImporter.GetAtPath(destination);
            Assert.NotNull(importer, $"No glTF importer at {destination}");

            AssetsTests.AssertLogItems(importer.reportItems, testCase);
#endif
            yield return null;
        }
    }
}
