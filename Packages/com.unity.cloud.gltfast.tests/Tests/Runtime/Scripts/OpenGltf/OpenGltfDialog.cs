// SPDX-FileCopyrightText: 2024 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using System.Threading.Tasks;
using GLTFast.Logging;
#if UNITY_ENTITIES_GRAPHICS
using Unity.Entities;
using Unity.Transforms;
#endif
using Unity.Mathematics;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace GLTFast.Tests
{
    public class OpenGltfDialog : MonoBehaviour
    {
        enum LoadMethod
        {
            File,
            Uri
        }

        [SerializeField]
        [Tooltip("glTF file path or URI to load glTF from. Only used in builds, in the editor the file dialog is used.")]
        public string uri;

        [SerializeField]
        int sceneIndex = -1;

        [SerializeField]
        bool m_AutoLoadLastFile;

        [SerializeField]
        LoadMethod loadMethod = LoadMethod.File;

        [SerializeField]
        ImportSettings importSettings;

        [SerializeField]
        InstantiationSettings instantiationSettings;

        // TODO: Update bounds when instantiating Entities as well.
#pragma warning disable CS0067 // Event is never used
        public event Action<Bounds> BoundsUpdated;
#pragma warning restore CS0067 // Event is never used

        GltfImport m_Gltf;

#if UNITY_ENTITIES_GRAPHICS
        Entity m_SceneRoot;
#else
        GameObjectSceneInstance m_SceneInstance;
#endif

        MaterialsVariantsComponent m_MaterialsVariantsComponent;

#if UNITY_EDITOR
        const string k_LastFilePathKey = "GLTFast.Tests.OpenGltfDialog.LastFilePath";

        static string LastFilePath
        {
            get => EditorPrefs.GetString(k_LastFilePathKey);
            set => EditorPrefs.SetString(k_LastFilePathKey, value);
        }
#else
        string LastFilePath
        {
            get => uri;
            set => uri = value;
        }
#endif

        async void Start()
        {
            if (m_AutoLoadLastFile)
            {
                var lastFilePath = LastFilePath;
                if (!string.IsNullOrEmpty(lastFilePath))
                {
                    if (loadMethod == LoadMethod.Uri || File.Exists(lastFilePath))
                    {
                        await LoadGltfFile(lastFilePath);
                        return;
                    }

                    Debug.LogWarning($"Could not load glTF file from {lastFilePath}");
                }
            }
#if UNITY_EDITOR
            await OpenFilePanel();
#endif
        }

        async void Update()
        {
            if (Input.GetKeyDown(KeyCode.G)
                && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                )
            {
#if UNITY_EDITOR
                await OpenFilePanel();
#else
                await LoadGltfFile(LastFilePath);
#endif
            }
            else if (Input.GetKeyDown(KeyCode.X)
                       && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                      )
            {
                await Clear();
            }
        }

        async Task LoadGltfFile(string path)
        {
            await Clear();
            var startTime = Time.realtimeSinceStartup;
            var startFrame = Time.frameCount;
            var logger = new ConsoleLogger();
            m_Gltf = new GltfImport(logger: logger);
            var success = loadMethod switch
            {
                LoadMethod.File => await m_Gltf.LoadFile(path, new Uri(path, UriKind.RelativeOrAbsolute), importSettings),
                LoadMethod.Uri => await m_Gltf.Load(path, importSettings),
                _ => throw new ArgumentOutOfRangeException()
            };
            var filename = Path.GetFileName(path);
            if (success)
            {
#if UNITY_ENTITIES_GRAPHICS
                var world = World.DefaultGameObjectInjectionWorld;
                m_SceneRoot = EntityUtils.CreateSceneRootEntity(world, filename);
                var instantiator = new EntityInstantiator(m_Gltf, m_SceneRoot, logger, instantiationSettings);
#else
                var instantiator = new GameObjectBoundsInstantiator(m_Gltf, transform, logger, instantiationSettings);
#endif
                success = sceneIndex >= 0
                    ? await m_Gltf.InstantiateSceneAsync(instantiator, sceneIndex)
                    : await m_Gltf.InstantiateMainSceneAsync(instantiator);
                if (success)
                {
                    var duration = Time.realtimeSinceStartup - startTime;
                    Debug.Log($"Opened glTF {filename} in {duration:F2} seconds ({Time.frameCount - startFrame} frames).\n{path}");

#if !UNITY_ENTITIES_GRAPHICS
                    m_SceneInstance = instantiator.SceneInstance;

                    var sceneBounds = instantiator.CalculateBounds();
                    if (sceneBounds.HasValue)
                    {
                        BoundsUpdated?.Invoke(sceneBounds.Value);
                    }
#if UNITY_ANIMATION
                    m_SceneInstance?.LegacyAnimation?.Play();
#endif

                    var materialsVariantsControl = m_SceneInstance?.MaterialsVariantsControl;
                    if (materialsVariantsControl != null)
                    {
                        m_MaterialsVariantsComponent ??= gameObject.AddComponent<MaterialsVariantsComponent>();
                        m_MaterialsVariantsComponent.Control = materialsVariantsControl;
                        m_MaterialsVariantsComponent.enabled = true;
                    }
#endif
                }
                else
                {
                    Debug.LogError($"Instantiating glTF scene {filename} failed.");
                }
                return;
            }
            Debug.LogError($"Loading glTF file {filename} failed.");
        }

        async Task Clear()
        {
#if UNITY_ENTITIES_GRAPHICS
            if (m_SceneRoot != Entity.Null)
            {
                var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
                EntityUtils.DestroyChildren(ref m_SceneRoot, ref entityManager);
                entityManager.DestroyEntity(m_SceneRoot);
                m_SceneRoot = Entity.Null;
            }
#endif
            if (m_MaterialsVariantsComponent is not null)
            {
                Destroy(m_MaterialsVariantsComponent);
                m_MaterialsVariantsComponent = null;
            }
            await Task.Yield();
            m_Gltf?.Dispose();
        }

#if UNITY_EDITOR
        async Task OpenFilePanel()
        {

            var file = EditorUtility.OpenFilePanel("Open glTF file", LastFilePath, "gltf,glb");
            if (string.IsNullOrEmpty(file))
                return;

            LastFilePath = file;
            await LoadGltfFile(file);
        }

        [ContextMenu("Copy last file path to uri")]
        public void ScanAndUpdateGltfTestCases()
        {
            uri = LastFilePath;
            AssetDatabase.Refresh();
        }
#endif
    }
}
