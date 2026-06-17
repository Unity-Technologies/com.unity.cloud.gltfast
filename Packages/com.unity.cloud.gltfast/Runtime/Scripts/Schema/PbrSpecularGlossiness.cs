// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using Unity.Gltfast.Text.Json.Serialization;
using UnityEngine;

namespace GLTFast.Schema
{

    /// <summary>
    /// This extension defines the specular-glossiness material model from
    /// Physically-Based Rendering (PBR).
    /// </summary>
    /// <seealso href="https://github.com/KhronosGroup/glTF/tree/main/extensions/2.0/Archived/KHR_materials_pbrSpecularGlossiness"/>
    public class PbrSpecularGlossiness
    {
        /// <summary>
        /// Diffuse color red, green, blue and alpha components in linear space.
        /// </summary>
        [JsonPropertyName("diffuseFactor")]
        [JsonConverter(typeof(ColorAlphaConverter))]
        public ColorAlpha DiffuseFactor { get; set; } = ColorAlpha.White;

        /// <summary>
        /// Diffuse color texture info.
        /// </summary>
        [JsonPropertyName("diffuseTexture")]
        public TextureInfo DiffuseTexture { get; set; }

        /// <summary>
        /// Specular color red, green and blue components in linear space.
        /// </summary>
        [JsonPropertyName("specularFactor")]
        [JsonConverter(typeof(ColorConverter))]
        public Color SpecularFactor { get; set; } = Color.White;

        /// <summary>
        /// The glossiness or smoothness of the material.
        /// </summary>
        [JsonIgnore]
        public float GlossinessFactor { get; set; } = 1f;

        [JsonPropertyName("glossinessFactor"), JsonInclude]
        internal float? GlossinessFactorSerialized
        {
            get => Mathematics.ApproximatelyOne(GlossinessFactor) ? null : GlossinessFactor;
            set => GlossinessFactor = value ?? 1f;
        }

        /// <summary>
        /// The specular-glossiness texture.
        /// </summary>
        [JsonPropertyName("specularGlossinessTexture")]
        public TextureInfo SpecularGlossinessTexture { get; set; }

        internal void GltfSerialize(JsonWriter writer)
        {
            writer.AddObject();
            writer.Close();
            throw new System.NotImplementedException($"GltfSerialize missing on {GetType()}");
        }
    }
}
