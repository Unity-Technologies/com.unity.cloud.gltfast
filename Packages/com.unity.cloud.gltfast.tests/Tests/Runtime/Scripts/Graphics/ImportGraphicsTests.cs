// SPDX-FileCopyrightText: 2025 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

#if USING_GRAPHICS_TEST_FRAMEWORK
using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using GLTFast.Logging;
using GLTFast.Tests.Export;
using NUnit.Framework;
using Unity.Mathematics;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Graphics;

namespace GLTFast.Tests.Graphics
{
    [Category("Graphics")]
    class ImportGraphicsTests : IPrebuildSetup
    {
        public Bounds Bounds { get; private set; }
        static int s_FramesToWait;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            SceneManager.LoadScene("GraphicsTestScene", LoadSceneMode.Single);
            // Wait two frames before the first test is run to ensure scene is loaded to completion.
            s_FramesToWait = 2;
        }

        [GltfGraphicsTest("glTF-Graphic-Tests-Assets", 2)]
        public IEnumerator Import(GltfGraphicsTestCase testCase)
        {
            yield return AsyncWrapper.WaitForTask(RunTestCaseAsync(testCase));
        }

        [GltfGraphicsTest("glTF-test-models", 1, @"AlphaColorSpace\.gltf$", ViewType.Front)]
        public IEnumerator AlphaColorSpace(GltfGraphicsTestCase testCase)
        {
            yield return AsyncWrapper.WaitForTask(RunTestCaseAsync(testCase));
        }

        async Task RunTestCaseAsync(GltfGraphicsTestCase testCase)
        {
#if UNITY_ENTITIES_GRAPHICS
            Assert.Ignore("Graphics tests are not implemented with Entities.");
#endif
            while (s_FramesToWait > 0)
            {
                await Task.Yield();
                s_FramesToWait--;
            }

            var gltfTestCase = testCase.GltfTestCase;

            // Create GameObject root for imported glTF scene
            var rootGameObject = new GameObject("GLTF Object");
            var deferAgent = new UninterruptedDeferAgent();
            var loadLogger = new CollectingLogger();

            // Build the file path
            var filePath = Path.Combine(testCase.TestCaseSet.RootPath, gltfTestCase.relativeUri);
            using var gltfImport = new GltfImport(deferAgent: deferAgent, logger: loadLogger);
            var loadSuccess = await gltfImport.Load(filePath);

            // Validate loading process
            if (loadSuccess == gltfTestCase.expectLoadFail)
            {
                throw new AssertionException(loadSuccess
                    ? "glTF import unexpectedly succeeded while failure was expected."
                    : "glTF import failed unexpectedly.");
            }

            // Set up the main instantiator
            var instantiatorLogger = new CollectingLogger();
            var instantiator = CreateInstantiator(gltfImport, instantiatorLogger, rootGameObject.transform);

            // Instantiate the glTF model into the scene
            var instantiateSuccess = await gltfImport.InstantiateMainSceneAsync(instantiator);
            if (!instantiateSuccess)
            {
                instantiatorLogger.LogAll();
                throw new AssertionException("glTF instantiation failed.");
            }
            // Calculate and set the bounds for the instantiated object
            SetBounds(instantiator);

            // Add and configure a main camera
            var cameraGameObject = CreateAndConfigureCamera(rootGameObject, testCase.View);
            ImageAssert.AreEqual(
                testCase.ReferenceImage.Image,
                cameraGameObject.GetComponent<Camera>(),
                new ImageComparisonSettings { AverageCorrectnessThreshold = 5e-4f },
                testCase.ReferenceImage.LoadMessage);
        }

        void SetBounds(GameObjectBoundsInstantiator instantiator)
        {
            var sceneBounds = instantiator.SceneInstance != null ? instantiator.CalculateBounds() : null;
            if (sceneBounds.HasValue)
            {
                Bounds = sceneBounds.Value;
            }
        }

        /// <summary>
        /// Creates and configures a camera for rendering the imported object from different views.
        /// </summary>
        GameObject CreateAndConfigureCamera(GameObject rootGameObject, ViewType view)
        {

            var objectTransform = rootGameObject.transform;
            var cameraGameObject = new GameObject("Main Camera");
            cameraGameObject.transform.SetParent(objectTransform);
            var camera = cameraGameObject.AddComponent<Camera>();
            const float fieldOfView = 60;
            camera.fieldOfView = fieldOfView;
            float3 scale = objectTransform.localScale;
            float3 boundsSize = Bounds.size;

            var distance = math.length(scale * boundsSize);

            var angle = math.radians(20);

            var centerPosition = objectTransform.TransformPoint(Bounds.center);
            var cameraPos = centerPosition +
                            new Vector3(0, distance * math.sin(angle), distance * math.cos(angle));
            camera.transform.position = cameraPos;
            camera.transform.LookAt(centerPosition);

            camera.nearClipPlane = distance * .001f;
            camera.farClipPlane = distance * 3;
            camera.transform.position = view switch
            {
                ViewType.Top => centerPosition + new Vector3(0, distance, 0),
                ViewType.Bottom => centerPosition + new Vector3(0, -distance, 0),
                ViewType.Left => centerPosition + new Vector3(-distance, 0, 0),
                ViewType.Right => centerPosition + new Vector3(distance, 0, 0),
                ViewType.Front => centerPosition + new Vector3(0, 0, distance),
                ViewType.Back => centerPosition + new Vector3(0, 0, -distance),
                ViewType.Perspective => centerPosition + new Vector3(distance, 0, distance),
                _ => camera.transform.position
            };

            camera.transform.LookAt(centerPosition);
            FrameObject(Bounds, camera);
            return cameraGameObject;
        }

        void FrameObject(Bounds bounds, Camera camera)
        {
            var boxCenter = bounds.center;
            var fov = camera.fieldOfView;
            var maxDimension = math.length(Bounds.size);
            var fovRad = fov * Mathf.Deg2Rad;
            var distance = (maxDimension / 2) / Mathf.Tan(fovRad / 2);

            var cameraPosition = boxCenter - camera.transform.forward * distance;
            camera.transform.position = cameraPosition;
            camera.transform.LookAt(boxCenter);
        }

        /// <summary>
        /// Creates an appropriate instantiator based on the runtime environment.
        /// </summary>
        internal static GameObjectBoundsInstantiator CreateInstantiator(
            IGltfReadable gltf,
            ICodeLogger logger,
            Transform parentTransform)
        {
            return new GameObjectBoundsInstantiator(gltf, parentTransform, logger);
        }


        public void Setup()
        {
#if UNITY_EDITOR
            SetupTests();
#endif
        }

#if UNITY_EDITOR
        internal static void SetupTests()
        {
            AddExportTestScene("GraphicsTestScene");
        }

        internal static void AddExportTestScene(string sceneName)
        {
            var scenePath = $"Packages/{GltfGlobals.GltfPackageName}/Tests/Runtime/Scenes/{sceneName}.unity";
            var scenes = EditorBuildSettings.scenes;
            var sceneGuid = ExportTests.TryFixPackageAssetPath(ref scenePath);
            foreach (var scene in scenes)
            {
                if (scene.guid == sceneGuid)
                {
                    return;
                }
            }

            Array.Resize(ref scenes, scenes.Length + 1);
            scenes[^1] = new EditorBuildSettingsScene(sceneGuid, true);
            EditorBuildSettings.scenes = scenes;
        }
#endif // UNITY_EDITOR
    }
}
#endif // USING_GRAPHICS_TEST_FRAMEWORK
