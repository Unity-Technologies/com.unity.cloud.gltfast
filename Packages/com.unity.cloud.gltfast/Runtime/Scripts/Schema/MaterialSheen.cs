// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using Unity.Gltfast.Text.Json.Serialization;
using Unity.Mathematics;
using UnityEngine;

namespace GLTFast.Schema
{

    /// <summary>
    /// This extension defines a sheen that can be layered on top of an
    /// existing glTF material definition. A sheen layer is a common technique
    /// used in Physically-Based Rendering to represent cloth and fabric
    /// materials, for example.
    /// </summary>
    /// <seealso href="https://github.com/KhronosGroup/glTF/tree/main/extensions/2.0/Khronos/KHR_materials_sheen"/>
    public class Sheen
    {

        /// <summary>
        /// The sheen color red, green and blue components in linear space.
        /// </summary>
        [JsonPropertyName("sheenColorFactor")]
        [JsonConverter(typeof(Float3ArrayConverter))]
        public float[] SheenColorFactor { get; set; } = { 1, 1, 1 };

        /// <summary>
        /// The sheen color in linear space.
        /// </summary>
        public Color SheenColor
        {
            get =>
                new Color(
                    SheenColorFactor[0],
                    SheenColorFactor[1],
                    SheenColorFactor[2]
                );
            set
            {
                SheenColorFactor = new[] { value.r, value.g, value.b };
            }
        }

        /// <summary>
        /// The sheen color texture.
        /// </summary>
        [JsonPropertyName("sheenColorTexture")]
        public TextureInfo SheenColorTexture { get; set; }

        /// <summary>
        /// The sheen roughness.
        /// </summary>
        [JsonPropertyName("sheenRoughnessFactor")]
        public float SheenRoughnessFactor { get; set; }

        /// <summary>
        /// The sheen roughness (Alpha) texture.
        /// </summary>
        [JsonPropertyName("sheenRoughnessTexture")]
        public TextureInfo SheenRoughnessTexture { get; set; }

        internal void GltfSerialize(JsonWriter writer)
        {
            writer.AddObject();
            if (SheenColorFactor != null && SheenColorFactor.Length > 2 && (
                    math.abs(SheenColorFactor[0] - 1f) > Constants.epsilon ||
                    math.abs(SheenColorFactor[1] - 1f) > Constants.epsilon ||
                    math.abs(SheenColorFactor[2] - 1f) > Constants.epsilon
                ))
            {
                writer.AddArrayProperty("sheenColorFactor", SheenColorFactor);
            }
            if (SheenColorTexture != null)
            {
                writer.AddProperty("sheenColorTexture");
                SheenColorTexture.GltfSerialize(writer);
            }
            if (SheenRoughnessFactor > 0)
            {
                writer.AddProperty("sheenRoughnessFactor", SheenRoughnessFactor);
            }
            if (SheenRoughnessTexture != null)
            {
                writer.AddProperty("sheenRoughnessTexture");
                SheenRoughnessTexture.GltfSerialize(writer);
            }
            writer.Close();
        }
    }
}
