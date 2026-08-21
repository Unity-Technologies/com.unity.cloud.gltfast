// SPDX-FileCopyrightText: 2025 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Unity.Cloud.Gltfast.Documentation.Examples;
using Unity.Cloud.Gltfast.Export;
using Unity.Cloud.Gltfast.Objects;
using Unity.Cloud.Gltfast.Tests;
using Unity.Cloud.Gltfast.Tests.Import;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Buffer = Unity.Cloud.Gltfast.Objects.Buffer;

namespace Unity.Cloud.Gltfast.Documentation.Examples.Tests
{
    [Category("DocExamples")]
    class DocExamplesTests : IPrebuildSetup, IPostBuildCleanup
    {
        [UnityTest]
        public IEnumerator LoadGltfFile()
        {
            var component = new GameObject()
                .AddComponent<LoadGltfFromMemory>();
            Assert.NotNull(component);

            var path = TestGltfGenerator.GetAssetPath(TestGltfGenerator.Asset.CylinderWithMaterial, GltfFormat.Binary);
#if UNITY_ANDROID && !UNITY_EDITOR
            // On Android streaming assets are packed in a jar file and cannot be accessed via file stream directly.
            // So we copy the file to a temporary location first.
            var copyTask = GltfBinaryTests.CopyToTempFile(path);
            yield return AsyncWrapper.WaitForTask(copyTask);
            path = copyTask.Result;
#endif
            var task = component.LoadGltfFile(path);
            yield return AsyncWrapper.WaitForTask(task);
            Object.Destroy(component.gameObject);
        }

        [UnityTest]
        public IEnumerator LoadGltfFromNativeArray()
        {
            var component = new GameObject()
                .AddComponent<LoadGltfFromMemory>();
            Assert.NotNull(component);

            var path = TestGltfGenerator.GetAssetPath(TestGltfGenerator.Asset.CylinderWithMaterial);
#if UNITY_ANDROID && !UNITY_EDITOR
            // On Android streaming assets are packed in a jar file and cannot be accessed via file stream directly.
            // So we copy the file to a temporary location first.
            var copyTask = GltfBinaryTests.CopyToTempFile(path);
            yield return AsyncWrapper.WaitForTask(copyTask);
            path = copyTask.Result;
#endif
            var task = component.LoadGltfFromNativeArray(path);
            yield return AsyncWrapper.WaitForTask(task);
            Object.Destroy(component.gameObject);
        }

        [UnityTest]
        public IEnumerator LoadGltfFromNativeArrayInvalid()
        {
#if !ENABLE_UNITY_COLLECTIONS_CHECKS
            // Without safety checks the sample reads deallocated memory, which may corrupt memory or crash.
            Assert.Ignore("Requires collections safety checks to detect the sample's use after dispose.");
#endif
            var component = new GameObject()
                .AddComponent<LoadGltfFromMemory>();
            Assert.NotNull(component);

            var path = TestGltfGenerator.GetAssetPath(TestGltfGenerator.Asset.CylinderWithMaterial, GltfFormat.Binary);
#if UNITY_ANDROID && !UNITY_EDITOR
            // On Android streaming assets are packed in a jar file and cannot be accessed via file stream directly.
            // So we copy the file to a temporary location first.
            var copyTask = GltfBinaryTests.CopyToTempFile(path);
            yield return AsyncWrapper.WaitForTask(copyTask);
            path = copyTask.Result;
#endif
            var task = component.LoadGltfFromNativeArrayInvalid(path);
            while (!task.IsCompleted)
            {
                yield return null;
            }

            // The sample disposes of the data while loading is in progress, so the outcome is undefined by
            // design. Whether the disposal is observed depends on timing: small glTFs complete before the load
            // task yields for the first time. Both outcomes are accepted, but nothing else is.
            if (task.IsFaulted)
            {
                var exception = task.Exception!.GetBaseException();
                Debug.Log($"Use after dispose was detected: {exception.Message}");
                Assert.IsTrue(
                    exception is System.ObjectDisposedException or System.InvalidOperationException,
                    $"Unexpected exception type {exception.GetType().Name}: {exception.Message}"
                    );
            }

            Object.Destroy(component.gameObject);
        }

        [UnityTest]
        public IEnumerator LoadViaComponent()
        {
            var component = new GameObject()
                .AddComponent<LoadGltfFromMemory>();
            Assert.NotNull(component);
            component.LoadViaComponent();
            yield return null;
            Object.Destroy(component.gameObject);
        }

        [UnityTest]
        public IEnumerator ImportSettings()
        {
            var task = LoadGltfFromMemory.ImportSettings(
                TestGltfGenerator.GetAssetPath(TestGltfGenerator.Asset.CylinderWithMaterial));
            yield return AsyncWrapper.WaitForTask(task);
        }

        [UnityTest]
        public IEnumerator ImportSettingsFail()
        {
            LogAssert.Expect(LogType.Error, new Regex(@"Download URL .*NonExistingFile\.gltf failed: .*404"));
            LogAssert.Expect(LogType.Error, "Loading glTF failed!");
            var task = LoadGltfFromMemory.ImportSettings(
                Path.Combine(Application.temporaryCachePath, "NonExistingFile.gltf"));
            yield return AsyncWrapper.WaitForTask(task);
        }

        [UnityTest]
        public IEnumerator Instantiation()
        {
            var component = new GameObject()
                .AddComponent<LoadGltfFromMemory>();
            Assert.NotNull(component);
            var task = component.Instantiation(TestGltfGenerator.GetAssetPath(TestGltfGenerator.Asset.CylinderWithMaterial));
            yield return AsyncWrapper.WaitForTask(task);
            Object.Destroy(component.gameObject);
        }

        [UnityTest]
        public IEnumerator InstantiationFail()
        {
            LogAssert.Expect(LogType.Error, new Regex(@"Download URL .*NonExistingFile\.gltf failed: .*404"));
            LogAssert.Expect(LogType.Error, "Loading glTF failed!");
            var component = new GameObject()
                .AddComponent<LoadGltfFromMemory>();
            Assert.NotNull(component);
            var task = component.Instantiation(Path.Combine(Application.temporaryCachePath, "NonExistingFile.gltf"));
            yield return AsyncWrapper.WaitForTask(task);
            Object.Destroy(component.gameObject);
        }

#if UNITY_ANIMATION
        [GltfTestCase("glTF-test-models", 3, "/(LightsPoint|ColorSpace|RainbowCuboid)\\.gltf$")]
        public IEnumerator SceneInstanceAccess(GltfTestCaseSet testCaseSet, GltfTestCase testCase)
        {
            var component = new GameObject()
                .AddComponent<LoadGltfFromMemory>();
            Assert.NotNull(component);
            var task = component.SceneInstanceAccess(Path.Combine(testCaseSet.RootPath, testCase.relativeUri));
            yield return AsyncWrapper.WaitForTask(task);
            Object.Destroy(component.gameObject);
        }
#endif

        [UnityTest]
        public IEnumerator CustomDeferAgent()
        {
            var component = new GameObject()
                .AddComponent<LoadGltfFromMemory>();
            Assert.NotNull(component);
            var task = component.CustomDeferAgent();
            yield return AsyncWrapper.WaitForTask(task);
            Object.Destroy(component.gameObject);
        }

        [UnityTest]
        public IEnumerator CustomGltfImport()
        {
            var go = new GameObject();
            var import = go.AddComponent<CustomGltfImport>();
            const string json = @"{""scene"":0,""scenes"":[{""nodes"":[0]}],""nodes"":[{""name"":""ExtrasNode"",""extras"":{""some-extra-key"":""some-extra-value""}}]}";
            var path = Path.Combine(Application.temporaryCachePath, "customGltfImportTest.gltf");
            File.WriteAllText(path, json);
            import.uri = path;
            import.enabled = false; // Prevent automatic loading
            yield return AsyncWrapper.WaitForTask(import.LoadGltf());
            var node = go.transform.GetChild(0);
            Assert.NotNull(node);
            Assert.AreEqual("ExtrasNode", node.name);
            var extraData = node.GetComponent<ExtraData>();
            Assert.NotNull(extraData);
            Assert.AreEqual("some-extra-value", extraData.someExtraKey);
            Object.Destroy(go);
        }

        [UnityTest]
        public IEnumerator SumPositions()
        {
            var path = TestGltfGenerator.GetAssetPath(TestGltfGenerator.Asset.CylinderWithMaterial);
            var task = BufferDataAccess.SumPositionsAsync(path);
            yield return AsyncWrapper.WaitForTask(task);
            Assert.AreNotEqual(Vector3.zero, task.Result, "No vertex positions were read.");
        }

        [UnityTest]
        public IEnumerator BufferViewSizeAfterImport()
        {
            var path = TestGltfGenerator.GetAssetPath(TestGltfGenerator.Asset.CylinderWithMaterial);
            var task = BufferDataAccess.BufferViewSizeAfterImportAsync(path, 0);
            yield return AsyncWrapper.WaitForTask(task);
            Assert.Greater(task.Result, 0, "Retained lease provided no data after the import.");
        }

        [Test]
        public void GetBuffer()
        {
            var root = new Root { Buffers = new List<Buffer> { new() { ByteLength = 8 } } };

            Assert.AreSame(root.Buffers[0], GltfObjectAccess.GetBuffer(root, new BufferView { Buffer = 0 }));
            Assert.IsNull(GltfObjectAccess.GetBuffer(root, new BufferView()), "Absent index");
            Assert.IsNull(GltfObjectAccess.GetBuffer(root, new BufferView { Buffer = 1 }), "Out of range");
            Assert.IsNull(GltfObjectAccess.GetBuffer(root, new BufferView { Buffer = -1 }), "Negative");
            Assert.IsNull(GltfObjectAccess.GetBuffer(new Root(), new BufferView { Buffer = 0 }), "No buffers");
        }

        [UnityTest]
        [TestCase("null", false, null, ExpectedResult = null, TestName = "TryGetWeights-Null")]
        [TestCase(@"{""weights"":[1.0,0.5]}", true, new[] { 1.0f, 0.5f }, ExpectedResult = null, TestName = "TryGetWeights-Object")]
        [TestCase("{}", false, null, ExpectedResult = null, TestName = "TryGetWeights-EmptyObject")]
        [TestCase("[1.0,0.5]", true, new[] { 1.0f, 0.5f }, ExpectedResult = null, TestName = "TryGetWeights-Array")]
        [TestCase("42", false, null, ExpectedResult = null, TestName = "TryGetWeights-Number")]
        [TestCase(@"""nope""", false, null, ExpectedResult = null, TestName = "TryGetWeights-String")]
        public IEnumerator TryGetWeights(string extrasJson, bool expected, float[] expectedWeights)
        {
            using var gltf = new GltfImport();
            var task = gltf.LoadGltfJsonAsync(
                $@"{{""asset"":{{""version"":""2.0""}},""nodes"":[{{""extras"":{extrasJson}}}]}}");
            yield return AsyncWrapper.WaitForTask(task);
            Assert.IsTrue(task.Result, "Import failed.");

            Assert.AreEqual(expected, GltfObjectAccess.TryGetWeights(gltf.Root.Nodes[0], out var weights));
            if (expected)
            {
                CollectionAssert.AreEqual(expectedWeights, weights);
            }
        }

        [UnityTest]
        public IEnumerator AdvancedExport()
        {
            var exportTarget = GameObject.CreatePrimitive(PrimitiveType.Cube);
            exportTarget.layer = LayerMask.NameToLayer("MyCustomLayer");
            exportTarget.tag = "ExportMe";

            var go = new GameObject();
            var exportComponent = go.AddComponent<ExportSamples>();
            exportComponent.destinationFilePath =
                Path.Combine(Application.temporaryCachePath, "advancedExportTest.glb");
            exportComponent.enabled = false; // Prevent automatic execution of Start
            yield return AsyncWrapper.WaitForTask(exportComponent.AdvancedExport());
            Assert.IsTrue(File.Exists(exportComponent.destinationFilePath));
            var fileInfo = new FileInfo(exportComponent.destinationFilePath);
            Assert.IsTrue(fileInfo.Length > 2400);
            Object.Destroy(exportTarget);
            Object.Destroy(go);
        }

        [Test]
        public void ExportSettingsDraco()
        {
            var settings = ExportSamples.ExportSettingsDraco();
            Assert.NotNull(settings);
            Assert.AreEqual(Compression.Draco, settings.Compression);
        }

        [UnityTest]
        public IEnumerator LocalTransform()
        {
            var go = new GameObject
            {
                transform =
            {
                position = new Vector3(42, 42, 42),
                rotation = Quaternion.Euler(45, 45, 45),
                localScale = new Vector3(2, 2, 2)
            }
            };
            var exportComponent = go.AddComponent<ExportSamples>();
            exportComponent.destinationFilePath =
                Path.Combine(Application.temporaryCachePath, "advancedExportTest.glb");
            exportComponent.enabled = false; // Prevent automatic execution of Start
            yield return AsyncWrapper.WaitForTask(exportComponent.LocalTransform());
            Assert.IsTrue(File.Exists(exportComponent.destinationFilePath));
            var fileInfo = new FileInfo(exportComponent.destinationFilePath);
            Assert.IsTrue(fileInfo.Length > 240);
            Object.Destroy(go);
        }

        [GltfTestCase("glTF-test-models", 1, "MaterialsVariantsInstanced\\.gltf$")]
        public IEnumerator MaterialsVariantsComponent(GltfTestCaseSet testCaseSet, GltfTestCase testCase)
        {
            var go = new GameObject();
            var import = go.AddComponent<MultipleInstances>();
            import.uri = Path.Combine(testCaseSet.RootPath, testCase.relativeUri);
            import.enabled = false; // Prevent automatic execution of Start
            import.quantity = 2;

            yield return AsyncWrapper.WaitForTask(import.LoadGltf());

            var instance1 = GameObject.Find("glTF-0");
            var material1 = instance1
                ?.transform.GetChild(0)
                ?.transform.GetChild(0)
                ?.GetComponent<MeshRenderer>()
                ?.sharedMaterial;
            Assert.NotNull(material1);
            Assert.AreEqual("Red", material1.name);

            var instance2 = GameObject.Find("glTF-1");
            var material2 = instance2
                ?.transform.GetChild(0)
                ?.transform.GetChild(0)
                ?.GetComponent<MeshRenderer>()
                ?.sharedMaterial;
            Assert.NotNull(material2);
            Assert.AreEqual("Blue", material2.name);

            Object.Destroy(instance1);
            Object.Destroy(instance2);
            Object.Destroy(go);
        }

        [GltfTestCase("glTF-test-models", 3, "TextureVariants-WebP(-Invalid)?\\.gl(tf|b)$", "AddOnsImage")]
        public IEnumerator WebpTextureAddon(GltfTestCaseSet testCaseSet, GltfTestCase testCase)
        {
            LogAssert.Expect(LogType.Error, "For this example to work, you need to compile <a href=\"https://chromium.googlesource.com/webm/libwebp\">libwebp</a> as a native plugin and name it 'webp-unity'.");
            LogAssert.Expect(LogType.Error, "Texture #0 not loaded");
            LogAssert.Expect(LogType.Error, "Texture #1 not loaded");

            var go = new GameObject();
            var import = go.AddComponent<TextureAddOnExample>();
            import.uri = Path.Combine(testCaseSet.RootPath, testCase.relativeUri);
            import.enabled = false; // Prevent automatic execution of Start

            yield return AsyncWrapper.WaitForTask(import.LoadGltf());
            Object.Destroy(go);
        }

        [GltfTestCase("glTF-test-models", 2, "TextureVariants\\.gl(tf|b)$", "AddOnsImage")]
        public IEnumerator PngTextureAddon(GltfTestCaseSet testCaseSet, GltfTestCase testCase)
        {
            var go = new GameObject();
            var import = go.AddComponent<TextureAddOnExample>();
            import.uri = Path.Combine(testCaseSet.RootPath, testCase.relativeUri);
            import.enabled = false; // Prevent automatic execution of Start

            yield return AsyncWrapper.WaitForTask(import.LoadGltf());
            Object.Destroy(go);
        }

        public async void Setup()
        {
#if UNITY_EDITOR
            AddTagAndLayer("MyCustomLayer", "ExportMe");
            await TestGltfGenerator.CreateTestAssetAsync(TestGltfGenerator.Asset.CylinderWithMaterial);
            await TestGltfGenerator.CreateTestAssetAsync(TestGltfGenerator.Asset.CylinderWithMaterial, GltfFormat.Binary);
#endif
        }

        public void Cleanup()
        {
#if UNITY_EDITOR
            RemoveTagAndLayer("MyCustomLayer", "ExportMe");
#endif
        }

#if UNITY_EDITOR
        static void AddTagAndLayer(string layerName, string tagName)
        {
            var tagManager = new SerializedObject(
                AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);

            var tagsProp = tagManager.FindProperty("tags");
            for (var i = 0; i < tagsProp.arraySize; i++)
            {
                if (tagsProp.GetArrayElementAtIndex(i).stringValue == tagName)
                    return;
            }
            tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
            tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tagName;

            var layersProp = tagManager.FindProperty("layers");

            for (var i = 8; i < layersProp.arraySize; i++)
            {
                var prop = layersProp.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(prop.stringValue))
                {
                    prop.stringValue = layerName;
                    break;
                }
            }

            tagManager.ApplyModifiedProperties();
        }

        static void RemoveTagAndLayer(string layerName, string tagName)
        {
            var tagManager = new SerializedObject(
                AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);

            var tagsProp = tagManager.FindProperty("tags");
            for (var i = 0; i < tagsProp.arraySize; i++)
            {
                if (tagsProp.GetArrayElementAtIndex(i).stringValue == tagName)
                {
                    tagsProp.DeleteArrayElementAtIndex(i);
                    break;
                }
            }

            var layersProp = tagManager.FindProperty("layers");

            for (var i = 8; i < layersProp.arraySize; i++)
            {
                var prop = layersProp.GetArrayElementAtIndex(i);
                if (prop.stringValue == layerName)
                {
                    layersProp.DeleteArrayElementAtIndex(i);
                    break;
                }
            }
            tagManager.ApplyModifiedProperties();
        }
#endif
    }
}
