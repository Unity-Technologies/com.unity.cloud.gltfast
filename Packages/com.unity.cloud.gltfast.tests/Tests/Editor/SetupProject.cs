// SPDX-FileCopyrightText: 2024 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

#if USING_URP || USING_HDRP
#define USING_SRP
#endif

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.Rendering;

namespace GLTFast.Editor.Tests
{
    static class SetupProject
    {
        const string k_RenderPipelineAssetsPath = "Packages/com.unity.cloud.gltfast.tests/Tests/Runtime/RenderPipelineAssets/";

        static readonly Dictionary<string, ProjectSetup> k_ProjectSetups = new Dictionary<string, ProjectSetup>
        {
            ["urp"] = new ProjectSetup(
                    new[] {
                        "com.unity.render-pipelines.universal"
                    }),
            ["hdrp"] = new ProjectSetup(
                    new[] {
                        "com.unity.render-pipelines.high-definition"
                    }),
            ["all_defines"] = new ProjectSetup(
                    new[] {
                        "com.unity.render-pipelines.universal"
                    },
                    new[] {
                        "GLTFAST_EDITOR_IMPORT_OFF",
                        "GLTFAST_SAFE",
                        "GLTFAST_KEEP_MESH_DATA"
                    }),
            ["performance"] = new ProjectSetup(null, new[] { "RUN_PERFORMANCE_TESTS" })
        };

        [MenuItem("Tools/glTFast Test Setup/URP")]
        static async void ApplySetupUniversal()
        {
            await ApplySetup("urp");
        }

        [MenuItem("Tools/glTFast Test Setup/HDRP")]
        static async void ApplySetupHighDefinition()
        {
            await ApplySetup("hdrp");
        }

        [MenuItem("Tools/glTFast Test Setup/All Defines")]
        static async void ApplySetupAllDefines()
        {
            await ApplySetup("all_defines");
        }

        [MenuItem("Tools/glTFast Test Setup/Performance")]
        static async void ApplySetupPerformance()
        {
            await ApplySetup("performance");
        }

        [MenuItem("Tools/glTFast Test Render Pipeline/Built-In")]
        static void SetupRenderPipelineBuiltIn()
        {
            Debug.Log($"Set default render pipeline to Built-In.");
            PlayerSettings.colorSpace = ColorSpace.Linear;
            GraphicsSettings.defaultRenderPipeline = null;
        }

        [MenuItem("Tools/glTFast Test Render Pipeline/Universal (Forward)")]
        static void SetupRenderPipelineUniversalForward()
        {
            SetRenderPipeline("URP-Forward");
        }

        [MenuItem("Tools/glTFast Test Render Pipeline/Universal (Forward+)")]
        static void SetupRenderPipelineUniversalForwardPlus()
        {
            SetRenderPipeline("URP-ForwardPlus");
        }

        [MenuItem("Tools/glTFast Test Render Pipeline/Universal (Forward)", true)]
        [MenuItem("Tools/glTFast Test Render Pipeline/Universal (Forward+)", true)]
        static bool SetupRenderPipelineUniversalValidate()
        {
#if USING_URP
            return true;
#else
            return false;
#endif
        }

        [MenuItem("Tools/glTFast Test Render Pipeline/High Definition (Deferred)")]
        static void SetupRenderPipelineHighDefinitionDeferred()
        {
            SetRenderPipeline("HDRP-Deferred");
        }

        [MenuItem("Tools/glTFast Test Render Pipeline/High Definition (Deferred)", true)]
        static bool SetupRenderPipelineHighDefinitionValidate()
        {
#if USING_HDRP
            return true;
#else
            return false;
#endif
        }

        public static async void ApplySetup()
        {
            var args = Environment.GetCommandLineArgs();
            foreach (var arg in args)
            {
                const string prefix = "glTFastSetup:";
                if (arg.StartsWith(prefix))
                {
                    var name = arg.Substring(prefix.Length);
                    await ApplySetup(name);
                    break;
                }
            }
        }

        static async Task ApplySetup(string name)
        {
            if (k_ProjectSetups.TryGetValue(name, out var setup))
            {
                Debug.Log($"Applying test setup {name}.");
                await setup.Apply();
            }
            else
            {
                throw new ArgumentException($"Test Setup {name} not found!");
            }
        }

        public static void SetRenderPipeline()
        {
            var args = Environment.GetCommandLineArgs();
            foreach (var arg in args)
            {
                const string prefix = "SRP:";
                if (arg.StartsWith(prefix))
                {
                    var subPath = arg.Substring(prefix.Length);
                    SetRenderPipeline(subPath);
                }
            }
        }

        static void SetRenderPipeline(string subPath)
        {
            var assetPath = $"{k_RenderPipelineAssetsPath}{subPath}.asset";
#if USING_SRP
            PlayerSettings.colorSpace = ColorSpace.Linear;
            var asset = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(assetPath);
            if (asset == null)
            {
                throw new InvalidDataException($"Could not find render pipeline asset at {subPath}.");
            }
            Debug.Log($"Set default render pipeline to {subPath}");
            GraphicsSettings.defaultRenderPipeline = asset;
#else
            throw new InvalidOperationException(
                $"Could not set render pipeline asset ({subPath}): No SRP package installed");
#endif
        }
    }

    class ProjectSetup
    {
        public ProjectSetup(string[] dependencies, string[] defines = null)
        {
            Dependencies = dependencies;
            Defines = defines;
        }

        string[] Dependencies { get; }
        string[] Defines { get; }

        public async Task Apply()
        {
            await InstallDependencies();
            if (Defines != null)
            {
                ApplyScriptingDefines(Defines);
            }
        }

        async Task InstallDependencies()
        {
            if (Dependencies != null)
            {
                foreach (var dependency in Dependencies)
                {
                    var request = Client.Add(dependency);
                    while (!request.IsCompleted)
                    {
                        await Task.Yield();
                    }
                }
            }
        }

        static void ApplyScriptingDefines(IEnumerable<string> newDefines)
        {
            var target = EditorUserBuildSettings.activeBuildTarget;
            var group = BuildPipeline.GetBuildTargetGroup(target);

            var namedBuildTarget = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(group);
            var scriptingDefineSymbols = PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget);
            var defines = new HashSet<string>(scriptingDefineSymbols.Split(';'));

            foreach (var define in newDefines)
            {
                Debug.Log($"Adding scripting define {define} ({namedBuildTarget}).");
                defines.Add(define);
            }
            PlayerSettings.SetScriptingDefineSymbols(namedBuildTarget, defines.ToArray());
        }
    }
}
