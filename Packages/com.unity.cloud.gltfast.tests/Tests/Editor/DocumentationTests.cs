// SPDX-FileCopyrightText: 2026 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace Unity.Cloud.Gltfast.Editor.Tests
{
    /// <summary>
    /// Verifies that the documentation's code samples still point at compiled code. A renamed
    /// region or moved file leaves the rendered page with an empty snippet and no build error.
    /// </summary>
    class DocumentationTests
    {
        // DocFX code snippet reference: [!code-cs [name](relative/path.cs#RegionName)]
        static readonly Regex k_CodeInclude = new Regex(
            @"\[!code-\w+\s*\[[^\]]*\]\(([^)#]+)#([^)\s]+)\)\]",
            RegexOptions.Compiled);

        static string DocumentationPath => $"Packages/{GltfGlobals.GltfPackageName}/Documentation~";

        // A single test rather than one case per include: NUnit evaluates a TestCaseSource when the
        // test list is built, so cases would carry the arguments a markdown file had at discovery
        // time and keep passing after it changed.
        [Test]
        public void CodeIncludesResolve()
        {
            Assert.IsTrue(
                Directory.Exists(DocumentationPath),
                $"Documentation folder is missing: {Path.GetFullPath(DocumentationPath)}");

            var problems = new List<string>();
            var includeCount = 0;

            foreach (var markdownFile in
                     Directory.EnumerateFiles(DocumentationPath, "*.md", SearchOption.AllDirectories))
            {
                foreach (Match match in k_CodeInclude.Matches(File.ReadAllText(markdownFile)))
                {
                    includeCount++;
                    var referencedPath = match.Groups[1].Value;
                    var region = match.Groups[2].Value;
                    var sourcePath = Path.GetFullPath(
                        Path.Combine(Path.GetDirectoryName(markdownFile), referencedPath));
                    var name = Path.GetFileName(markdownFile);

                    if (!File.Exists(sourcePath))
                    {
                        problems.Add($"{name}: \"{referencedPath}\" does not exist ({sourcePath}).");
                        continue;
                    }

                    var source = File.ReadAllText(sourcePath);
                    if (!Regex.IsMatch(
                            source,
                            $@"^[ \t]*#region[ \t]+{Regex.Escape(region)}[ \t]*$",
                            RegexOptions.Multiline))
                    {
                        problems.Add(
                            $"{name}: \"{referencedPath}\" does not define region \"{region}\".");
                    }
                }
            }

            // Without this the loop above proves nothing when enumeration comes up empty.
            Assert.Greater(includeCount, 0, "No code snippet references were found in the documentation.");

            if (problems.Count <= 0)
            {
                return;
            }

            var message = new StringBuilder(
                $"{problems.Count} of {includeCount} documentation code snippet reference(s) do not resolve:");
            foreach (var problem in problems)
            {
                message.Append("\n  ").Append(problem);
            }
            Assert.Fail(message.ToString());
        }
    }
}
