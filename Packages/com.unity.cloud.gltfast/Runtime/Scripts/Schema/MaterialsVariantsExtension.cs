// SPDX-FileCopyrightText: 2024 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using Unity.Gltfast.Text.Json.Serialization;

namespace GLTFast.Schema
{
    /// <summary>
    /// KHR_materials_variants extension.
    /// </summary>
    /// <seealso href="https://github.com/KhronosGroup/glTF/tree/main/extensions/2.0/Khronos/KHR_materials_variants">KHR_materials_variants extension</seealso>
    public class MaterialsVariantsRootExtension
    {
        /// <summary>
        /// Collection of material variants
        /// </summary>
        [JsonPropertyName("variants")]
        public List<MaterialsVariant> Variants { get; set; }

        internal void GltfSerialize(JsonWriter writer)
        {
            writer.AddObject();
            writer.AddArray("variants");
            foreach (var variant in Variants)
            {
                variant.GltfSerialize(writer);
            }
            writer.CloseArray();
            writer.Close();
        }
    }

    /// <summary>
    /// Named materials variant.
    /// </summary>
    /// <seealso href="https://github.com/KhronosGroup/glTF/tree/main/extensions/2.0/Khronos/KHR_materials_variants">KHR_materials_variants extension</seealso>
    public class MaterialsVariant : NamedObject
    {
        internal void GltfSerialize(JsonWriter writer)
        {
            GltfSerializeName(writer);
        }
    }

    /// <summary>
    /// Mesh primitive level KHR_materials_variants extension.
    /// </summary>
    /// <seealso href="https://github.com/KhronosGroup/glTF/tree/main/extensions/2.0/Khronos/KHR_materials_variants">KHR_materials_variants extension</seealso>
    public class MaterialsVariantsMeshPrimitiveExtension
    {
        /// <summary>
        /// List of material to variants mapping.
        /// </summary>
        [JsonPropertyName("mappings")]
        public List<MaterialVariantsMapping> Mappings { get; set; }

        /// <summary>
        /// Retrieves the index of the material that corresponds to a material variant.
        /// If there's no match for this variant index, it returns false. In this case the default material has to be
        /// applied.
        /// </summary>
        /// <param name="variantIndex">glTF materials variant index.</param>
        /// <param name="materialIndex">glTF material index.</param>
        /// <returns>True if there's a matching mapping with the provided variant index. False otherwise.</returns>
        public bool TryGetMaterialIndex(int variantIndex, out int materialIndex)
        {
            foreach (var mapping in Mappings)
            {
                foreach (var i in mapping.Variants)
                {
                    if (variantIndex == i)
                    {
                        materialIndex = mapping.Material;
                        return true;
                    }
                }
            }

            materialIndex = -1;
            return false;
        }

        internal void GltfSerialize(JsonWriter writer)
        {
            writer.AddObject();
            writer.AddArray("mappings");
            foreach (var mapping in Mappings)
            {
                mapping.GltfSerialize(writer);
            }
            writer.CloseArray();
            writer.Close();
        }
    }

    /// <summary>
    /// Maps a material index to one or more materials variants indices.
    /// </summary>
    public class MaterialVariantsMapping
    {
        /// <summary>Material index.</summary>
        [JsonPropertyName("material")]
        public int Material { get; set; }

        /// <summary>Materials variants indices.</summary>
        [JsonPropertyName("variants")]
        public int[] Variants { get; set; }

        internal void GltfSerialize(JsonWriter writer)
        {
            writer.AddObject();
            writer.AddProperty("material", Material);
            writer.AddArrayProperty("variants", Variants);
            writer.Close();
        }
    }
}
