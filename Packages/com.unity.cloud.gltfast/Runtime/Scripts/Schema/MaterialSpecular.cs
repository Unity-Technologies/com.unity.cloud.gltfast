// SPDX-FileCopyrightText: 2024 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using Unity.Gltfast.Text.Json.Serialization;
using UnityEngine;

namespace GLTFast.Schema
{
    /// <summary>
    /// This extension allows configuring the specular reflection.
    /// </summary>
    /// <seealso href="https://github.com/KhronosGroup/glTF/tree/main/extensions/2.0/Khronos/KHR_materials_specular"/>
    public class MaterialSpecular
    {
        /// <summary>
        /// The strength of the specular reflection.
        /// </summary>
        [JsonIgnore]
        public float SpecularFactor { get; set; } = 1f;

        [JsonPropertyName("specularFactor"), JsonInclude]
        internal float? SpecularFactorSerialized
        {
            get => Mathematics.ApproximatelyOne(SpecularFactor) ? null : SpecularFactor;
            set => SpecularFactor = value ?? 1f;
        }

        /// <summary>
        /// A texture that defines the strength of the specular reflection, stored in the alpha (A) channel.
        /// This will be multiplied by specularFactor.
        /// </summary>
        [JsonPropertyName("specularTexture")]
        public TextureInfo SpecularTexture { get; set; }

        /// <summary>
        /// The F0 color of the specular reflection (linear RGB).
        /// </summary>
        [JsonPropertyName("specularColorFactor")]
        [JsonConverter(typeof(ColorConverter))]
        public Color SpecularColorFactor { get; set; } = Color.White;

        /// <summary>
        /// A texture that defines the F0 color of the specular reflection, stored in the RGB channels and encoded in
        /// sRGB. This texture will be multiplied by specularColorFactor.
        /// </summary>
        [JsonPropertyName("specularColorTexture")]
        public TextureInfo SpecularColorTexture { get; set; }

        internal void GltfSerialize(JsonWriter writer)
        {
            writer.AddObject();
            if (!Mathematics.ApproximatelyOne(SpecularFactor))
            {
                writer.AddProperty("specularFactor", SpecularFactor);
            }
            if (SpecularTexture != null)
            {
                writer.AddProperty("specularTexture");
                SpecularTexture.GltfSerialize(writer);
            }
            if (SpecularColorFactor != Color.White)
            {
                writer.AddColorProperty("specularColorFactor", SpecularColorFactor);
            }
            if (SpecularColorTexture != null)
            {
                writer.AddProperty("specularColorTexture");
                SpecularColorTexture.GltfSerialize(writer);
            }
            writer.Close();
        }
    }
}
