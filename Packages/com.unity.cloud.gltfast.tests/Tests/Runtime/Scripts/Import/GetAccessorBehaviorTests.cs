// SPDX-FileCopyrightText: 2026 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections;
using NUnit.Framework;
using Unity.Cloud.Gltfast.Logging;
using UnityEngine.TestTools;

namespace Unity.Cloud.Gltfast.Tests.Import
{
    [Category("Import")]
    class GetAccessorBehaviorTests
    {
        const string k_MinimalGltfJson =
            "{\"asset\":{\"version\":\"2.0\"}," +
            "\"buffers\":[{\"byteLength\":4,\"uri\":\"data:application/octet-stream;base64,AAAAAA==\"}]," +
            "\"bufferViews\":[{\"buffer\":0,\"byteLength\":4}]," +
            "\"accessors\":[{\"bufferView\":0,\"componentType\":5121,\"count\":4,\"type\":\"SCALAR\"}]}";

        [UnityTest]
        public IEnumerator GetAccessorDataInvalidIndexReturnsDefault()
        {
            yield return AsyncWrapper.WaitForTask(GetAccessorDataInvalidIndexReturnsDefaultAsync());
        }

        static async System.Threading.Tasks.Task GetAccessorDataInvalidIndexReturnsDefaultAsync()
        {
            using var gltf = new GltfImport(logger: new CollectingLogger());
            var success = await gltf.LoadGltfJson(k_MinimalGltfJson);
            Assert.IsTrue(success);
#pragma warning disable CS0618 // Type or member is obsolete
            Assert.IsFalse(gltf.GetAccessorData(-1).IsCreated);
            Assert.IsFalse(gltf.GetAccessorData(int.MaxValue).IsCreated);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        [UnityTest]
        public IEnumerator GetAccessorInvalidIndexReturnsDefault()
        {
            yield return AsyncWrapper.WaitForTask(GetAccessorInvalidIndexReturnsDefaultAsync());
        }

        static async System.Threading.Tasks.Task GetAccessorInvalidIndexReturnsDefaultAsync()
        {
            using var gltf = new GltfImport(logger: new CollectingLogger());
            var success = await gltf.LoadGltfJson(k_MinimalGltfJson);
            Assert.IsTrue(success);
#pragma warning disable CS0618 // Type or member is obsolete
            Assert.IsFalse(gltf.GetAccessor(-1).IsCreated);
            Assert.IsFalse(gltf.GetAccessor(int.MaxValue).IsCreated);
#pragma warning restore CS0618 // Type or member is obsolete
        }
    }
}
