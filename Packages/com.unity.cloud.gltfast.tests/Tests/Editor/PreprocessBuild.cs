// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using GLTFast.Tests.Export;
#if USING_GRAPHICS_TEST_FRAMEWORK
using GLTFast.Tests.Graphics;
#endif
using GLTFast.Tests.Import;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
#if USING_URP
using UnityEngine.Rendering.Universal;
#endif
using Object = UnityEngine.Object;

namespace GLTFast.Editor.Tests
{
    public class PreprocessBuild : IPreprocessBuildWithReport
    {
        /// <summary>
        /// One less than URP's URPPreprocessBuild, to make sure Render Compatibility Mode is set prior.
        /// </summary>
        /// <seealso cref="EnableLegacyRenderCompatibilityMode"/>
        // TODO: Restore to `0` when dropping 6000.0 / removing EnableLegacyRenderCompatibilityMode.
        public int callbackOrder => int.MinValue + 99;


        static string pkgPath => $"Packages/{GltfGlobals.GltfPackageName}";

        public void OnPreprocessBuild(BuildReport target)
        {
            if ((target.summary.options & BuildOptions.IncludeTestAssemblies) != 0)
            {
                SyncTestAssets();
                EnableLegacyRenderCompatibilityMode();
                AddShaderVariantCollections();
                ExportTests.CertifyStreamingAssetsFolder();
                ExportTests.SetupTests();
#if USING_GRAPHICS_TEST_FRAMEWORK
                ImportGraphicsTests.SetupTests();
#endif
                CopyExportTargetsToStreamingAssets();
                AssetDatabase.Refresh();
            }
        }

        static void SyncTestAssets()
        {
            var streamingAssets = Application.streamingAssetsPath;
            if (!Directory.Exists(streamingAssets))
            {
                Directory.CreateDirectory(streamingAssets);
            }
            foreach (var testCaseSet in IterateAssets<GltfTestCaseSet>("Tests/Runtime/TestCaseSets"))
            {
                testCaseSet.SerializeToStreamingAssets();
                if (GltfTestCaseSet.IsStreamingAssetsPlatform)
                {
                    testCaseSet.CopyToStreamingAssets();
                }
            }

            AssetDatabase.Refresh();
        }

        static IEnumerable<T> IterateAssets<T>(string inPackageLocation, string name = null) where T : Object
        {
            var guids = FindAssets($"t:{typeof(T).Name} {name ?? ""}", inPackageLocation);
            if (guids == null || guids.Length < 1)
            {
                throw new InvalidDataException($"No {typeof(T).Name} asset set was found in {inPackageLocation}!");
            }
            foreach (var guid in guids)
            {
                yield return AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
            }
        }

        static IEnumerable<ShaderVariantCollection> IterateAllShaderVariantCollections()
        {
            string name;
            switch (RenderPipelineUtils.RenderPipeline)
            {
                case RenderPipeline.BuiltIn:
                    name = "birp";
                    break;
                case RenderPipeline.HighDefinition:
                    name = "hdrp";
                    break;
                case RenderPipeline.Unknown:
                case RenderPipeline.Universal:
                default:
                    name = "urp";
                    break;
            }

            foreach (var collection in IterateAssets<ShaderVariantCollection>("Tests/Runtime/TestCaseSets", name))
            {
                yield return collection;
            }

            // Shaders required for export tests.
            var export = AssetDatabase.LoadAssetAtPath<ShaderVariantCollection>($"{pkgPath}/Runtime/Shader/Export/glTFExport.shadervariants");
            Assert.IsNotNull(export);
            yield return export;
        }

        static void AddShaderVariantCollections()
        {
            var settings = GraphicsSettings.GetGraphicsSettings();
            var obj = new SerializedObject(settings);
            var preloadedShaders = obj.FindProperty("m_PreloadedShaders");

            foreach (var svc in IterateAllShaderVariantCollections())
            {
                var found = false;
                var arraySize = preloadedShaders.arraySize;
                for (var i = 0; i < arraySize; i++)
                {
                    var e = preloadedShaders.GetArrayElementAtIndex(i);
                    if (e.objectReferenceValue == svc)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    preloadedShaders.InsertArrayElementAtIndex(arraySize);
                    var entry = preloadedShaders.GetArrayElementAtIndex(arraySize);
                    entry.objectReferenceValue = svc;
                }
            }

            obj.ApplyModifiedProperties();
        }

        static void EnableLegacyRenderCompatibilityMode()
        {
#if USING_URP && !UNITY_6000_3_OR_NEWER && URP_COMPATIBILITY_MODE
            // The combination URP Render Graphs + project tests on Yamato + macOS + Unity 6.0 leads to floods of this
            // error on the console, failing the tests:
            //
            // > BlitFinalToBackBuffer/Draw UIToolkit/uGUI Overlay: Attachment 0 was created with 2 samples but 1 samples were requested.
            //
            // Something MSAA related I couldn't fix. This turns off render graphs for Unity 6.0 macOS only.
            // TODO: Remove RenderCompatibilityMode when support for Unity 6000.0 is removed.
            // Also remove the URP_COMPATIBILITY_MODE scripting define from all test projects,
            // which was required for this crutch to work.
            if (RenderPipelineUtils.RenderPipeline == RenderPipeline.Universal)
            {
                var s = GraphicsSettings.GetRenderPipelineSettings<RenderGraphSettings>();
                if (s != null)
                {
                    s.enableRenderCompatibilityMode = true;
                }
                else
                {
                    Debug.LogError("Failed to get RenderGraphSettings from GraphicsSettings. " +
                        "URP compatibility mode may not enabled.");
                }
            }
#endif
        }

        static void CopyExportTargetsToStreamingAssets()
        {
            const string inPackageLocation = "Tests/Resources/ExportTargets";
            var sourceRoot = $"{pkgPath}/{inPackageLocation}";
            ExportTests.TryFixPackageAssetPath(ref sourceRoot);
            var exportTargetFolder = $"Assets/StreamingAssets/{ExportTests.exportTargetFolder}";
            if (!AssetDatabase.IsValidFolder(exportTargetFolder))
            {
                AssetDatabase.CreateFolder("Assets/StreamingAssets", ExportTests.exportTargetFolder);
            }

            foreach (var target in IterateAssets<TextAsset>(inPackageLocation))
            {
                var sourcePath = AssetDatabase.GetAssetPath(target);
                Assert.IsTrue(sourcePath.StartsWith(sourceRoot), $"{sourcePath} is not relative to {sourceRoot}");
                var relativePath = sourcePath.Substring(sourceRoot.Length + 1);
                Debug.Log($"Relative export target path {relativePath}\n{sourcePath}\n{sourceRoot}");
                var destinationPath = Path.Combine(exportTargetFolder, relativePath);
                var destinationDir = Path.GetDirectoryName(destinationPath);
                Assert.IsFalse(string.IsNullOrEmpty(destinationDir));
                Directory.CreateDirectory(destinationDir);
                File.Copy(sourcePath, destinationPath, true);
            }
        }

        static string[] FindAssets(string filter, string inPackageLocation)
        {
            var guids = AssetDatabase.FindAssets(
                filter,
                new[] { $"{pkgPath}/{inPackageLocation}" }
            );
            if (guids?.Length < 1)
            {
                // Try again with separate tests package
                guids = AssetDatabase.FindAssets(
                    filter,
                    new[] { $"{pkgPath}.tests/{inPackageLocation}" }
                );
            }

            return guids;
        }
    }
}
