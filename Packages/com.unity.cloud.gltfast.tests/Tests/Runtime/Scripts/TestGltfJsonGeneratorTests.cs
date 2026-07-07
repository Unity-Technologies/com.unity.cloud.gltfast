// SPDX-FileCopyrightText: 2026 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using UnityEngine;

namespace GLTFast.Tests
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
            { "hierarchy-complex", "176c262b2cdaa141a9f8b86dfe8d629fe674b6c4e918fecae62e9d7e6dcdc194" },
            { "hierarchy-complex-matrix", "47d32e21475f438b11f1b6189afb8a68937cb616e3f4b9d87d3aa2c4ac02eb40" },
            { "hierarchy-complex-no-names", "6a51a65fc8990692e49c1c04b87d8daf2fe926c1ddc9238e623c66d0f617175d" },
            { "hierarchy-complex-min-transforms", "abbaaf9194b32735db91c123d634b8ba7aed0760f3292cb07312d145b4fc2f3a" },
            { "hierarchy-complex-shallow", "73881181c5547efbb03dcb309bdb91e58ee5434f37cfd871d595aa15fcce7ecc" },
            { "materials", "da9d0d53d1015a82f3b85dc1a73cb69d1884bd9d07cad997e186d7b525477aa4" },
            { "extras", "78e16a52970f6af1da3d7e0bf69cc1f0d49961e0e87c30458fbdc3dc95495485" },
            { "xmp", "88e147c0bcc453c14ef1d952c4215ce4541f47c9db93a4618044c9662433ea8b" },
            { "animation", "80348d98c36354b5f2a907aab803ea7452cf83922e7e37bdee41774540383bf2" },
            { "cameras", "05a8ee7d12c931ba5f80d1ad7cda8c1c5e9da567ce7a14f9f6f6267b21f0d0eb" },
            { "lights", "447fd4517e078f24a967f146c693e1019f23bb2382f8acd25e3bec1fc9f3d3c4" },
            { "sub-meshes", "fadeca88a203c6c1345169812a47267d0cfe4a29df02484f9f378243bcff0d11" },
            { "data-uri", "39c41d0939dca50a4ad709f9109ff237b22d36a265e6f632aede8fee942e929d" },
            { "omni", "85fb73d02d188a69648b422ff73ab67604878e1975d60af8139b0b84e4d6e482" }
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
