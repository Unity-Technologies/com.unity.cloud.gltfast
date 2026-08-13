// SPDX-FileCopyrightText: 2024 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Cloud.Gltfast.Schema;
using Unity.Cloud.Gltfast.Text.Json;
using UnityEngine;
using Mesh = Unity.Cloud.Gltfast.Schema.Mesh;

namespace Unity.Cloud.Gltfast.Tests.Export
{
    [Category("Export")]
    class JsonSerialization
    {
        const string k_MaterialsVariantsJson =
            @"{""meshes"":[{""primitives"":[{""extensions"":{""KHR_materials_variants"":{""mappings"":[{""material"":0,""variants"":[0]},{""material"":1,""variants"":[1]},{""material"":2,""variants"":[2]}]}}}]}],""extensions"":{""KHR_materials_variants"":{""variants"":[{""name"":""red""},{""name"":""green""},{""name"":""blue""}]}}}";

        static Root CreateMaterialsVariantsRoot()
        {
            return new Root
            {
                Extensions = new RootExtensions
                {
                    MaterialsVariants = new MaterialsVariantsRootExtension
                    {
                        Variants = new List<MaterialsVariant>
                        {
                            new() { Name = "red" },
                            new() { Name = "green" },
                            new() { Name = "blue" },
                        }
                    }
                },
                Meshes = new List<Mesh>
                {
                    new()
                    {
                        Primitives = new List<MeshPrimitive>
                        {
                            new()
                            {
                                Extensions = new MeshPrimitiveExtensions
                                {
                                    MaterialsVariants = new MaterialsVariantsMeshPrimitiveExtension
                                    {
                                        Mappings = new List<MaterialVariantsMapping>
                                        {
                                            new() {Material = 0, Variants = new List<int> { 0 }},
                                            new() {Material = 1, Variants = new List<int> { 1 }},
                                            new() {Material = 2, Variants = new List<int> { 2 }},
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };
        }

        [Test]
        public void MaterialsVariantsExtension()
        {
            var gltf = CreateMaterialsVariantsRoot();

            var stream = new MemoryStream();
            JsonSerializer.Serialize(stream, gltf, GltfJsonContext.Default.Root);
            var jsonString = Encoding.Default.GetString((stream.ToArray()));
            Assert.AreEqual(k_MaterialsVariantsJson, jsonString);
        }

        [Test]
        public void RootSerializeWritesJsonToStream()
        {
            var gltf = CreateMaterialsVariantsRoot();

            using var stream = new MemoryStream();
            gltf.Serialize(stream);

            var jsonString = Encoding.UTF8.GetString(stream.ToArray());
            Assert.AreEqual(k_MaterialsVariantsJson, jsonString);
        }

        [Test]
        public void RootSerializeEmptyRoot()
        {
            using var stream = new MemoryStream();
            new Root().Serialize(stream);

            Assert.AreEqual("{}", Encoding.UTF8.GetString(stream.ToArray()));
        }

#pragma warning disable CS0618 // GltfSerialize is obsolete; verifying backwards-compat shim
        [Test]
        public void RootGltfSerializeObsoleteShimWritesJsonToStream()
        {
            var gltf = CreateMaterialsVariantsRoot();

            using var stream = new MemoryStream();
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false), 1024, leaveOpen: true))
            {
                gltf.GltfSerialize(writer);
            }

            var jsonString = Encoding.UTF8.GetString(stream.ToArray());
            Assert.AreEqual(k_MaterialsVariantsJson, jsonString);
        }
#pragma warning restore CS0618
    }
}
