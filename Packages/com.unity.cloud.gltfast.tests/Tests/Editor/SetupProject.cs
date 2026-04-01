// SPDX-FileCopyrightText: 2024 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace GLTFast.Editor.Tests
{
    static class SetupProject
    {
        static readonly Dictionary<string, ProjectSetup> k_ProjectSetups = new()
        {
            ["all_defines"] = new(
                    null,
                    new[] {
                        "GLTFAST_EDITOR_IMPORT_OFF",
                        "GLTFAST_SAFE",
                        "GLTFAST_KEEP_MESH_DATA"
                    }),
            ["performance"] = new(null, new[] { "RUN_PERFORMANCE_TESTS" })
        };

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
