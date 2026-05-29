// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using Unity.Gltfast.Text.Json.Serialization;
using Unity.Mathematics;

namespace GLTFast.Schema
{
    /// <summary>
    /// Occlusion map specific texture info
    /// </summary>
    public class OcclusionTextureInfo : TextureInfo
    {
        /// <summary>
        /// A scalar multiplier controlling the amount of occlusion applied.
        /// A value of 0.0 means no occlusion.
        /// A value of 1.0 means full occlusion.
        /// This value is ignored if the corresponding texture is not specified.
        /// This value is linear.
        /// </summary>
        [JsonPropertyName("strength")]
        public float Strength { get; set; } = 1.0f;

        internal override void GltfSerialize(JsonWriter writer)
        {
            writer.AddObject();
            GltfSerializeTextureInfo(writer);
            if (math.abs(Strength - 1f) > Constants.epsilon)
            {
                writer.AddProperty("strength", Strength);
            }
            writer.Close();
        }
    }
}
