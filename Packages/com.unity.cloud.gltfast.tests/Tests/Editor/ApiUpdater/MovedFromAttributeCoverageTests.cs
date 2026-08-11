// SPDX-FileCopyrightText: 2026 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
#if UNITY_6000_6_OR_NEWER
using UnityEngine.Assemblies;
#endif
using UnityEngine.Scripting.APIUpdating;

namespace Unity.Cloud.Gltfast.Editor.Tests
{
    /// <summary>
    /// Guards the API Updater migration for the 7.0 assembly/namespace rename.
    ///
    /// The actual source rewrite performed by Unity's external ScriptUpdater cannot be exercised
    /// in-process (see plans/research/api-updater-testing.md); it is validated end-to-end by the
    /// -batchmode -accept-apiupdate harness. This test guards the input to that updater: every public
    /// type in the renamed shipping assemblies must carry a [MovedFrom] whose source namespace and
    /// assembly match the pre-7.0 names, so consumer code auto-migrates.
    /// </summary>
    [TestFixture]
    class MovedFromAttributeCoverageTests
    {
        // New assembly name -> pre-7.0 (glTFast 6.x) assembly name.
        static readonly (string newAssembly, string oldAssembly)[] k_ShippingAssemblies =
        {
            ("Unity.Cloud.Gltfast", "glTFast"),
            ("Unity.Cloud.Gltfast.Export", "glTFast.Export"),
            ("Unity.Cloud.Gltfast.Newtonsoft", "glTFast.Newtonsoft"),
            ("Unity.Cloud.Gltfast.Dots", "glTFast.dots"),
            ("Unity.Cloud.Gltfast.Editor", "glTFast.Editor"),
        };

        const string k_NewRootNamespace = "Unity.Cloud.Gltfast";
        const string k_OldRootNamespace = "GLTFast";

        // Public types introduced after the 7.0 rename. They never existed in glTFast 6.x, so there is
        // no old namespace/assembly to migrate from and they correctly carry no [MovedFrom]. Every other
        // public type must still be annotated, so a genuinely forgotten [MovedFrom] keeps failing the test.
        // Add a new post-rename public type here deliberately.
        static readonly HashSet<string> k_PostRenameTypes = new(StringComparer.Ordinal)
        {
            "Unity.Cloud.Gltfast.Schema.AccessorExtensions",
            "Unity.Cloud.Gltfast.Schema.AccessorSparseExtensions",
            "Unity.Cloud.Gltfast.Schema.AccessorSparseIndicesExtensions",
            "Unity.Cloud.Gltfast.Schema.AccessorSparseValuesExtensions",
            "Unity.Cloud.Gltfast.Schema.AdditionalPropertyContainer",
            "Unity.Cloud.Gltfast.Schema.AnimationChannelExtensions",
            "Unity.Cloud.Gltfast.Schema.AnimationChannelTargetExtensions",
            "Unity.Cloud.Gltfast.Schema.AnimationExtensions",
            "Unity.Cloud.Gltfast.Schema.AnimationSamplerExtensions",
            "Unity.Cloud.Gltfast.Schema.AssetExtensions",
            "Unity.Cloud.Gltfast.Schema.BufferExtensions",
            "Unity.Cloud.Gltfast.Schema.CameraExtensions",
            "Unity.Cloud.Gltfast.Schema.CameraOrthographicExtensions",
            "Unity.Cloud.Gltfast.Schema.CameraPerspectiveExtensions",
            "Unity.Cloud.Gltfast.Schema.IAdditionalPropertyContainer",
            "Unity.Cloud.Gltfast.Schema.ImageExtensions",
            "Unity.Cloud.Gltfast.Schema.MeshExtensions",
            "Unity.Cloud.Gltfast.Schema.IPropertyContainer",
            "Unity.Cloud.Gltfast.Schema.IReadOnlyPropertyContainer",
            "Unity.Cloud.Gltfast.Schema.PbrMetallicRoughnessExtensions",
            "Unity.Cloud.Gltfast.Schema.Properties",
            "Unity.Cloud.Gltfast.Schema.Property",
            "Unity.Cloud.Gltfast.Schema.PropertyEnumerator",
            "Unity.Cloud.Gltfast.Schema.ReadOnlyProperties",
            "Unity.Cloud.Gltfast.Schema.SamplerExtensions",
            "Unity.Cloud.Gltfast.Schema.SceneExtensions",
            "Unity.Cloud.Gltfast.Schema.SkinExtensions",
            "Unity.Cloud.Gltfast.Schema.Value",
            "Unity.Cloud.Gltfast.Schema.ValueKind",
        };

        static IEnumerable<TestCaseData> PublicShippingTypes()
        {
            foreach (var (newAssembly, oldAssembly) in k_ShippingAssemblies)
            {
#if UNITY_6000_6_OR_NEWER
                var assembly = CurrentAssemblies.GetLoadedAssemblies()
#else
                var assembly = AppDomain.CurrentDomain.GetAssemblies()
#endif
                    .FirstOrDefault(a => a.GetName().Name == newAssembly);
                if (assembly == null)
                {
                    // Assembly absent from this configuration (e.g. optional dependency's define off).
                    // The core assembly must always be present; missing optional ones are skipped.
                    continue;
                }

                foreach (var type in assembly.GetExportedTypes())
                {
                    if (type.IsNested)
                        continue; // nested public types migrate with their [MovedFrom]-annotated parent
                    if (!BelongsToPackage(type))
                        continue; // skip types injected into the assembly by other packages' source
                                  // generators (e.g. Unity.Entities' AssemblyTypeRegistry) — they live
                                  // in their own namespaces and are not part of glTFast's public API
                    if (k_PostRenameTypes.Contains(type.FullName))
                        continue; // introduced after the rename; no old name to migrate from
                    yield return new TestCaseData(type, oldAssembly)
                        .SetName($"{newAssembly}::{type.FullName}");
                }
            }
        }

        [Test]
        public void CoreAssemblyIsLoaded()
        {
#if UNITY_6000_6_OR_NEWER
            var core = CurrentAssemblies.GetLoadedAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "Unity.Cloud.Gltfast");
#else
            var core = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "Unity.Cloud.Gltfast");
#endif
            Assert.IsNotNull(core, "The renamed core assembly 'Unity.Cloud.Gltfast' is not loaded; " +
                "the coverage test cannot run.");
        }

        [TestCaseSource(nameof(PublicShippingTypes))]
        public void PublicTypeHasMovedFrom(Type type, string expectedOldAssembly)
        {
            var attribute = type.GetCustomAttribute<MovedFromAttribute>();
            Assert.IsNotNull(
                attribute,
                $"Public type {type.FullName} is missing [MovedFrom]. Consumers upgrading from " +
                $"glTFast 6.x will not auto-migrate. Add:\n" +
                $"    [MovedFrom(true, sourceNamespace: \"{ExpectedOldNamespace(type)}\", " +
                $"sourceAssembly: \"{expectedOldAssembly}\")]");

            var source = ReadSource(attribute);
            var expectedOldNamespace = ExpectedOldNamespace(type);
            Assert.AreEqual(expectedOldNamespace, source.sourceNamespace,
                $"{type.FullName}: [MovedFrom] sourceNamespace should be \"{expectedOldNamespace}\".");
            Assert.AreEqual(expectedOldAssembly, source.sourceAssembly,
                $"{type.FullName}: [MovedFrom] sourceAssembly should be \"{expectedOldAssembly}\".");
        }

        // A type belongs to glTFast's public API if it lives under the package's root namespace.
        // Types a source generator injects into the assembly (Entities, Burst, …) sit in their own
        // namespaces, so this cleanly excludes them without hard-coding individual type names.
        static bool BelongsToPackage(Type type)
        {
            var ns = type.Namespace;
            return ns != null
                && (ns == k_NewRootNamespace
                    || ns.StartsWith(k_NewRootNamespace + ".", StringComparison.Ordinal));
        }

        static string ExpectedOldNamespace(Type type)
        {
            var ns = type.Namespace ?? string.Empty;
            if (ns == k_NewRootNamespace)
                return k_OldRootNamespace;
            if (ns.StartsWith(k_NewRootNamespace + ".", StringComparison.Ordinal))
                return k_OldRootNamespace + ns.Substring(k_NewRootNamespace.Length);
            return ns;
        }

        static (string sourceNamespace, string sourceAssembly) ReadSource(MovedFromAttribute attribute)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var data = typeof(MovedFromAttribute).GetField("data", flags)?.GetValue(attribute);
            if (data == null)
                throw new InvalidOperationException("ReadSource couldn't read MovedFromAttribute's internal data.");

            var dataType = data.GetType();
            var nameSpaceField = dataType.GetField("nameSpace", flags);
            var assemblyField = dataType.GetField("assembly", flags);
            if (nameSpaceField == null || assemblyField == null)
                throw new InvalidOperationException("ReadSource couldn't read MovedFromAttributeData's fields.");

            return ((string)nameSpaceField.GetValue(data), (string)assemblyField.GetValue(data));
        }
    }
}
