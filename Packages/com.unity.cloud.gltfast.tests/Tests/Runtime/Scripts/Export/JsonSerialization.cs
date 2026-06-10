// SPDX-FileCopyrightText: 2024 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.IO;
using System.Text;
using GLTFast.Schema;
using NUnit.Framework;
using UnityEngine;
using Mesh = GLTFast.Schema.Mesh;

namespace GLTFast.Tests.Export
{
    [Category("Export")]
    class JsonSerialization
    {
        [Test]
        public void MaterialsVariantsExtension()
        {
            var gltf = new Root
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
                                            new() {Material = 0, Variants = new [] { 0 }},
                                            new() {Material = 1, Variants = new [] { 1 }},
                                            new() {Material = 2, Variants = new [] { 2 }},
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };

            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            gltf.GltfSerialize(writer);
            writer.Close();
            var jsonString = Encoding.Default.GetString((stream.ToArray()));
            Assert.AreEqual(
                @"{""meshes"":[{""primitives"":[{""extensions"":{""KHR_materials_variants"":{""mappings"":[{""material"":0,""variants"":[0]},{""material"":1,""variants"":[1]},{""material"":2,""variants"":[2]}]}}}]}],""extensions"":{""KHR_materials_variants"":{""variants"":[""name"":""red"",""name"":""green"",""name"":""blue""]}}}",
                jsonString
                );
        }
    }
}
