// SPDX-FileCopyrightText: 2026 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Cloud.Gltfast.Logging;
using UnityEngine.TestTools;

namespace Unity.Cloud.Gltfast.Tests.Import
{
    /// <summary>
    /// Drives the accessor conversion jobs in <see cref="GltfImport"/> through
    /// <c>EXT_mesh_gpu_instancing</c>, which is the cheapest way to give an accessor a rotation,
    /// translation or scale usage without building a mesh.
    /// </summary>
    [Category("Import")]
    class AccessorConversionTests
    {
        // 64 zero bytes, enough for any of the rotation component types below.
        const string k_Buffer =
            "\"buffers\":[{\"byteLength\":64,\"uri\":\"data:application/octet-stream;base64," +
            "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
            "AAAAAAAAAAAAAAAAAAAAAA==\"}],";

        static string InstancingGltf(string accessor, string attribute = "ROTATION")
        {
            return "{\"asset\":{\"version\":\"2.0\"}," +
                "\"extensionsUsed\":[\"EXT_mesh_gpu_instancing\"]," +
                k_Buffer +
                "\"bufferViews\":[{\"buffer\":0,\"byteLength\":64}]," +
                "\"accessors\":[" + accessor + "]," +
                "\"nodes\":[{\"extensions\":{\"EXT_mesh_gpu_instancing\":{\"attributes\":{\""
                + attribute + "\":0}}}}]," +
                "\"scenes\":[{\"nodes\":[0]}],\"scene\":0}";
        }

        static async Task<(bool success, CollectingLogger logger)> LoadAsync(string json)
        {
            var logger = new CollectingLogger();
            using var gltf = new GltfImport(logger: logger);
            return (await gltf.LoadGltfJsonAsync(json), logger);
        }

        static bool Logged(CollectingLogger logger, LogCode code)
            => logger.Items?.Any(item => item.Code == code) ?? false;

        // componentType 5126 float, 5122 short, 5120 byte. The quantized ones are what
        // KHR_mesh_quantization produces and are converted by their own job each.
        [UnityTest]
        public IEnumerator RotationsConvertForEveryComponentType(
            [Values(5126, 5122, 5120)] int componentType)
        {
            yield return AsyncWrapper.WaitForTask(
                RotationsConvertForEveryComponentTypeAsync(componentType));
        }

        static async Task RotationsConvertForEveryComponentTypeAsync(int componentType)
        {
            var normalized = componentType != 5126 ? ",\"normalized\":true" : "";
            var json = InstancingGltf(
                $"{{\"bufferView\":0,\"componentType\":{componentType},\"count\":2,\"type\":\"VEC4\"{normalized}}}");

            var (success, logger) = await LoadAsync(json);
            Assert.IsTrue(success, $"componentType {componentType} failed to import.");
            Assert.IsFalse(
                Logged(logger, LogCode.AccessorAccessFailed),
                $"componentType {componentType} reported an access failure.");
        }

        [UnityTest]
        public IEnumerator RotationAccessorBeyondBufferViewAborts(
            [Values(5126, 5122, 5120)] int componentType)
        {
            yield return AsyncWrapper.WaitForTask(
                RotationAccessorBeyondBufferViewAbortsAsync(componentType));
        }

        static async Task RotationAccessorBeyondBufferViewAbortsAsync(int componentType)
        {
            var normalized = componentType != 5126 ? ",\"normalized\":true" : "";
            // 64 elements of any VEC4 type exceed the 64 byte buffer view.
            var json = InstancingGltf(
                $"{{\"bufferView\":0,\"componentType\":{componentType},\"count\":64,\"type\":\"VEC4\"{normalized}}}");

            var (success, logger) = await LoadAsync(json);
            Assert.IsFalse(success, $"componentType {componentType} should not have imported.");
            Assert.IsTrue(
                Logged(logger, LogCode.AccessorAccessFailed),
                $"componentType {componentType} should report an access failure.");
        }

        [UnityTest]
        public IEnumerator RotationAccessorWithUnsupportedComponentTypeAborts()
        {
            yield return AsyncWrapper.WaitForTask(
                RotationAccessorWithUnsupportedComponentTypeAbortsAsync());
        }

        static async Task RotationAccessorWithUnsupportedComponentTypeAbortsAsync()
        {
            // UNSIGNED_INT is not a valid rotation component type, so no conversion job exists.
            var json = InstancingGltf(
                "{\"bufferView\":0,\"componentType\":5125,\"count\":2,\"type\":\"VEC4\"}");

            var (success, logger) = await LoadAsync(json);
            Assert.IsFalse(success, "An unsupported rotation component type must abort the import.");
            Assert.IsTrue(Logged(logger, LogCode.IndexFormatInvalid));
        }

        [UnityTest]
        public IEnumerator RotationAccessorWithNegativeCountAborts()
        {
            yield return AsyncWrapper.WaitForTask(RotationAccessorWithNegativeCountAbortsAsync());
        }

        static async Task RotationAccessorWithNegativeCountAbortsAsync()
        {
            var json = InstancingGltf(
                "{\"bufferView\":0,\"componentType\":5126,\"count\":-1,\"type\":\"VEC4\"}");

            var (success, logger) = await LoadAsync(json);
            Assert.IsFalse(success, "A negative element count must abort the import.");
            Assert.IsTrue(Logged(logger, LogCode.AccessorAccessFailed));
        }

        [UnityTest]
        public IEnumerator InstancingAttributeWithUnresolvableBufferViewAborts(
            [Values("TRANSLATION", "SCALE", "ROTATION")] string attribute)
        {
            yield return AsyncWrapper.WaitForTask(
                InstancingAttributeWithUnresolvableBufferViewAbortsAsync(attribute));
        }

        static async Task InstancingAttributeWithUnresolvableBufferViewAbortsAsync(string attribute)
        {
            var type = attribute == "ROTATION" ? "VEC4" : "VEC3";
            var json = InstancingGltf(
                $"{{\"bufferView\":9,\"componentType\":5126,\"count\":2,\"type\":\"{type}\"}}",
                attribute);

            var (success, logger) = await LoadAsync(json);
            Assert.IsFalse(success, $"{attribute} with an absent buffer view must abort the import.");
            Assert.IsTrue(Logged(logger, LogCode.IndexOutOfRange));
        }
    }
}
