// SPDX-FileCopyrightText: 2026 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using UnityEngine;

namespace Unity.Cloud.Gltfast.Tests
{
    [TestFixture]
    class TestGltfJsonGeneratorTests
    {
        /// <summary>
        /// SHA-256 of each scenario's bytes, pinned per file name. Empty
        /// strings are treated as "not yet pinned" — the test will still
        /// verify within-session determinism and log the actual hash so a
        /// developer can paste it in. When you intentionally change the
        /// generator, regenerate, copy the actual hashes from the test
        /// output, and update this map.
        /// </summary>
        static readonly Dictionary<string, string> k_PinnedHashes = new()
        {
            { "hierarchy-complex", "71876c681bbfbfe4d693bc337a14c93e629677c4850cfda8beb128ea15c5991d" },
            { "hierarchy-complex-matrix", "bdfcc0b8a7e8714824b60b5f7f1a49ab12cd948f5c401631e4fe7197ad36352f" },
            { "hierarchy-complex-no-names", "50063c6cc2dd5fdd723472c1aff32c9b22c47f94265080746053a5928ece0bce" },
            { "hierarchy-complex-min-transforms", "10178d184b50a008a08ad9bd3d1f8d7aa7bf8a3edf889125ce24f35aa2f9b912" },
            { "hierarchy-complex-shallow", "78099a4bff85e399b54535b393b53877d91df3c27048d82d619c3f403423c4b8" },
            { "materials", "3dcfdf16c0d4126f977b1046e82d1feb852af83d0f56f3945e06d75e4ae96b22" },
            { "extras", "9148dc924428ed6637975c9b50b407f52112a7682234b57bbfb05957cba1a247" },
            { "xmp", "8189effe5a75fec03cfedaa2ccf32da1f810afb1d8d5af911a71320b9f3dac2f" },
            { "animation", "10f8d97d76e3921acf14aaaf04f95199e41d5b729179348d19898766e1211856" },
            { "cameras", "b3f8b19f8336fc93d55c565d0006e499ec786a2ed878277abde509a582f7e38a" },
            { "lights", "0d5af46df45ba8bf53726e5a260a821f50001f80fcdf1204590cde4ead6eb742" },
            { "sub-meshes", "93433367680b7d28bd3960de5eba9aa38aa21bc286da91ce8fbb3549687ab103" },
            { "data-uri", "da8c9ddc9b3e0ddc52c19ca8d47c8207172e4c43b140a8d04d53863e2ce1d828" },
            { "omni", "1d3a7ead41b3d39a5b86ff8f0949260a9e4b82ad2b517143d73dbc7b40006a35" }
        };

        [Test]
        public void GeneratorOutputIsDeterministic()
        {
#if !UNITY_EDITOR
            Assert.Ignore("Test glTF JSON generator produces different results in builds " +
                "(presumably floating point arithmetics of the random number generator are the cause). " +
                "Tests run in Editor only.");
#endif

            var folderA = NewTempFolder("a");
            var folderB = NewTempFolder("b");
            try
            {
                TestGltfJsonGenerator.GenerateAll(folderA);
                TestGltfJsonGenerator.GenerateAll(folderB);

                var pinDrift = new List<string>();
                foreach (var config in TestGltfJsonGenerator.Configurations)
                {
                    var name = config.name;
                    var pathA = config.GetPath(folderA);
                    var pathB = config.GetPath(folderB);
                    Assert.IsTrue(File.Exists(pathA), $"{name} was not generated in run A");
                    Assert.IsTrue(File.Exists(pathB), $"{name} was not generated in run B");

                    var hashA = Sha256OfFile(pathA);
                    var hashB = Sha256OfFile(pathB);

                    // Within-session determinism: two runs in the same process
                    // must produce byte-identical output.
                    Assert.AreEqual(
                        hashA, hashB,
                        $"{name} is not deterministic within a single session "
                        + $"(run A: {hashA}, run B: {hashB})");

                    // Cross-machine determinism: compare against the pinned
                    // hash if one was recorded; otherwise log so a developer
                    // can pin it.
                    var pinned = k_PinnedHashes.GetValueOrDefault(name);
                    if (string.IsNullOrEmpty(pinned))
                    {
                        Debug.LogWarning($"[TestGltfJsonGenerator] {name} hash not pinned. "
                            + $"Actual: {hashA}. To enable cross-machine drift detection, add this to k_PinnedHashes.");
                    }
                    else if (pinned != hashA)
                    {
                        pinDrift.Add(
                            $"{name}: pinned={pinned}, actual={hashA}");
                    }
                }

                if (pinDrift.Count > 0)
                {
                    Assert.Fail(
                        "Generator output drifted from pinned hashes. "
                        + "If the change was intentional, update k_PinnedHashes:\n  "
                        + string.Join("\n  ", pinDrift));
                }
            }
            finally
            {
                SafeDeleteDirectory(folderA);
                SafeDeleteDirectory(folderB);
            }
        }

        static string NewTempFolder(string suffix)
        {
            var folder = Path.Combine(
                Application.temporaryCachePath,
                $"gltfast-perf-determinism-{suffix}");
            if (Directory.Exists(folder))
                Directory.Delete(folder, recursive: true);
            Directory.CreateDirectory(folder);
            return folder;
        }

        static void SafeDeleteDirectory(string folder)
        {
            try
            {
                if (Directory.Exists(folder))
                    Directory.Delete(folder, recursive: true);
            }
            catch
            {
                // Best-effort cleanup; teardown failures shouldn't mask test failures.
            }
        }

        static string Sha256OfFile(string path)
        {
            using var sha = SHA256.Create();
            using var stream = File.OpenRead(path);
            var bytes = sha.ComputeHash(stream);
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes)
                sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }
}
