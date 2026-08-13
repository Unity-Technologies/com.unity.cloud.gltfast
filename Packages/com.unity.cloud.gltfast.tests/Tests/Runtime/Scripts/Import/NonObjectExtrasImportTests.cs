// SPDX-FileCopyrightText: 2026 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections;
using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Cloud.Gltfast.Logging;
using Unity.Cloud.Gltfast.Objects;
using UnityEngine.TestTools;

namespace Unity.Cloud.Gltfast.Tests.Import
{
    /// <summary>
    /// The glTF specification permits <c>extras</c> to be any JSON value. Importing such a document
    /// used to abort with an uncaught <c>JsonException</c>.
    /// </summary>
    [Category("Import")]
    class NonObjectExtrasImportTests
    {
        static string MinimalGltfJson(string nodeExtras) =>
            "{\"asset\":{\"version\":\"2.0\",\"extras\":\"v2.1.0\"}," +
            "\"scene\":0,\"scenes\":[{\"nodes\":[0],\"extras\":[1,2,3]}]," +
            $"\"nodes\":[{{\"name\":\"Node\",\"extras\":{nodeExtras}}}]}}";

        [UnityTest]
        public IEnumerator NumberExtras() => AsyncWrapper.WaitForTask(AssertImports("42", ValueKind.Number));

        [UnityTest]
        public IEnumerator StringExtras() => AsyncWrapper.WaitForTask(AssertImports("\"Yadiya\"", ValueKind.String));

        [UnityTest]
        public IEnumerator BooleanExtras() => AsyncWrapper.WaitForTask(AssertImports("true", ValueKind.True));

        [UnityTest]
        public IEnumerator ArrayExtras() => AsyncWrapper.WaitForTask(AssertImports("[1,2,3]", ValueKind.Array));

        [UnityTest]
        public IEnumerator ObjectExtras() => AsyncWrapper.WaitForTask(AssertImports("{\"a\":1}", ValueKind.Object));

        static async Task AssertImports(string nodeExtras, ValueKind expected)
        {
            var logger = new CollectingLogger();
            using var gltf = new GltfImport(logger: logger);

            var success = await gltf.LoadGltfJsonAsync(MinimalGltfJson(nodeExtras));

            Assert.IsTrue(success, "Import failed.");
            Assert.AreEqual(expected, gltf.Root.Nodes[0].Extras.Kind);
            Assert.AreEqual(ValueKind.String, gltf.Root.Asset.Extras.Kind);
            Assert.AreEqual(ValueKind.Array, gltf.Root.Scenes[0].Extras.Kind);
        }
    }
}
