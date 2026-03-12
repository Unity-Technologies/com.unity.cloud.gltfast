// SPDX-FileCopyrightText: 2025 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections;
using System.IO;
using System.Threading.Tasks;
using GLTFast.Documentation.Examples;
using GLTFast.Export;
using GLTFast.Tests;
using GLTFast.Tests.Import;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace GLTFast.DocExamples.Tests
{
    [Category("DocExamples")]
    class DocExamplesTests : IPrebuildSetup, IPostBuildCleanup
    {
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
            component.filePath = TestGltfGenerator.GetAssetPath(TestGltfGenerator.Asset.CylinderWithMaterial);
            Assert.NotNull(component);
            var task = component.Instantiation();
            yield return AsyncWrapper.WaitForTask(task);
            Object.Destroy(component.gameObject);
        }

        [UnityTest]
        public IEnumerator InstantiationFail()
        {
            LogAssert.Expect(LogType.Error, "Loading glTF failed!");
            var component = new GameObject()
                .AddComponent<LoadGltfFromMemory>();
            component.filePath = Path.Combine(Application.temporaryCachePath, "NonExistingFile.gltf");
            Assert.NotNull(component);
            var task = component.Instantiation();
            yield return AsyncWrapper.WaitForTask(task);
            Object.Destroy(component.gameObject);
        }

#if UNITY_ANIMATION
        [GltfTestCase("glTF-test-models", 3, "/(LightsPoint|ColorSpace|RainbowCuboid)\\.gltf$")]
        public IEnumerator SceneInstanceAccess(GltfTestCaseSet testCaseSet, GltfTestCase testCase)
        {
            var component = new GameObject()
                .AddComponent<LoadGltfFromMemory>();
            component.filePath = Path.Combine(testCaseSet.RootPath, testCase.relativeUri);
            Assert.NotNull(component);
            var task = component.SceneInstanceAccess();
            yield return AsyncWrapper.WaitForTask(task);
            Object.Destroy(component.gameObject);
        }
#endif

        [UnityTest]
        public IEnumerator CustomDeferAgent()
        {
            var component = new GameObject()
                .AddComponent<LoadGltfFromMemory>();
            component.filePath = TestGltfGenerator.GetAssetPath(TestGltfGenerator.Asset.CylinderWithMaterial);
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
