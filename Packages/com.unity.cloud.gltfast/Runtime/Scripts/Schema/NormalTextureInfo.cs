// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using Unity.Gltfast.Text.Json.Serialization;
using Unity.Mathematics;

namespace GLTFast.Schema
{
    /// <summary>
    /// Normal map specific texture info
    /// </summary>
    public class NormalTextureInfo : TextureInfo
    {

        /// <summary>
        /// The scalar multiplier applied to each normal vector of the texture.
        /// This value is ignored if normalTexture is not specified.
        /// This value is linear.
        /// </summary>
        [JsonIgnore]
        public float Scale { get; set; } = 1f;

        [JsonPropertyName("scale"), JsonInclude]
        internal float? ScaleSerialized
        {
            get => Mathematics.ApproximatelyOne(Scale) ? null : Scale;
            set => Scale = value ?? 1f;
        }

        internal override void GltfSerialize(JsonWriter writer)
        {
            writer.AddObject();
            GltfSerializeTextureInfo(writer);
            if (!Mathematics.ApproximatelyOne(Scale))
            {
                writer.AddProperty("scale", Scale);
            }
            writer.Close();
        }
    }
}
