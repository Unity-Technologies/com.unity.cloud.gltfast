// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using Unity.Gltfast.Text.Json.Serialization;

namespace GLTFast.Schema
{

    /// <summary>
    /// This extension defines a clear coating that can be layered on top of an
    /// existing glTF material definition.
    /// </summary>
    /// <seealso href="https://github.com/KhronosGroup/glTF/blob/main/extensions/2.0/Khronos/KHR_materials_clearcoat/README.md"/>
    public class ClearCoat
    {

        /// <summary>
        /// The clearcoat layer intensity.
        /// </summary>
        [JsonPropertyName("clearcoatFactor")]
        public float ClearcoatFactor { get; set; }

        /// <summary>
        /// The clearcoat layer intensity texture.
        /// </summary>
        [JsonPropertyName("clearcoatTexture")]
        public TextureInfo ClearcoatTexture { get; set; }

        /// <summary>
        /// The clearcoat layer roughness.
        /// </summary>
        [JsonPropertyName("clearcoatRoughnessFactor")]
        public float ClearcoatRoughnessFactor { get; set; }

        /// <summary>
        /// The clearcoat layer roughness texture.
        /// </summary>
        [JsonPropertyName("clearcoatRoughnessTexture")]
        public TextureInfo ClearcoatRoughnessTexture { get; set; }

        /// <summary>
        /// The clearcoat normal map texture.
        /// </summary>
        [JsonPropertyName("clearcoatNormalTexture")]
        public NormalTextureInfo ClearcoatNormalTexture { get; set; }

        internal void GltfSerialize(JsonWriter writer)
        {
            writer.AddObject();

            if (ClearcoatFactor > 0)
            {
                writer.AddProperty("clearcoatFactor", ClearcoatFactor);
            }
            if (ClearcoatTexture != null)
            {
                writer.AddProperty("clearcoatTexture");
                ClearcoatTexture.GltfSerialize(writer);
            }
            if (ClearcoatRoughnessFactor > 0)
            {
                writer.AddProperty("clearcoatRoughnessFactor", ClearcoatRoughnessFactor);
            }
            if (ClearcoatRoughnessTexture != null)
            {
                writer.AddProperty("clearcoatRoughnessTexture");
                ClearcoatRoughnessTexture.GltfSerialize(writer);
            }
            if (ClearcoatNormalTexture != null)
            {
                writer.AddProperty("clearcoatNormalTexture");
                ClearcoatNormalTexture.GltfSerialize(writer);
            }

            writer.Close();
        }

    }
}
