// SPDX-FileCopyrightText: 2026 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;

namespace Gltfast.Tools
{
    // Copied into a glTFast 6.x project and run headless; it never compiles on the 7.0 line, so no
    // local gate on that line covers it.
    static class GeneratePreRenameTypes
    {
        const string k_RootNamespace = "GLTFast";
        const string k_OutputArgument = "-preRenameOutput";
        const string k_AssembliesArgument = "-preRenameAssemblies";

        public static void Run()
        {
            try
            {
                var path = ReadArgument(k_OutputArgument)
                    ?? throw new ArgumentException($"missing {k_OutputArgument} <path>");
                var assemblies = ReadArgument(k_AssembliesArgument)?.Split(',')
                    ?? throw new ArgumentException($"missing {k_AssembliesArgument} <name,name,…>");
                var names = Collect(assemblies, k_RootNamespace);
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
                File.WriteAllText(path, string.Concat(names.Select(n => n + "\n")));
                Console.WriteLine($"GeneratePreRenameTypes: wrote {names.Count} type(s) to {path}");
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine($"GeneratePreRenameTypes: {exception.Message}");
                EditorApplication.Exit(1);
            }
        }

        static string ReadArgument(string name)
        {
            var args = Environment.GetCommandLineArgs();
            var index = Array.IndexOf(args, name);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
        }

        static SortedSet<string> Collect(IReadOnlyList<string> assemblyNames, string rootNamespace)
        {
            var names = new SortedSet<string>(StringComparer.Ordinal);
            var loaded = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assemblyName in assemblyNames)
            {
                // Skipping an unloaded assembly would write a partial baseline that reads as a complete
                // one, and the fixture would then demand that every missing type's [MovedFrom] be dropped.
                var assembly = loaded.FirstOrDefault(a => a.GetName().Name == assemblyName)
                    ?? throw new InvalidOperationException($"assembly '{assemblyName}' is not loaded");

                foreach (var type in assembly.GetExportedTypes())
                {
                    if (BelongsToPackage(type, rootNamespace))
                        names.Add(type.FullName);
                }
            }

            return names;
        }

        static bool BelongsToPackage(Type type, string rootNamespace)
        {
            var ns = type.Namespace;
            return ns != null
                && (ns == rootNamespace || ns.StartsWith(rootNamespace + ".", StringComparison.Ordinal));
        }
    }
}
