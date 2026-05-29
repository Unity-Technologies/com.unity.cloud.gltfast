// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using Unity.Gltfast.Text.Json.Serialization;
using Unity.Mathematics;

namespace GLTFast.Schema
{

    /// <inheritdoc cref="Extension.TextureTransform"/>
    public class TextureTransform
    {

        /// <summary>
        /// The offset of the UV coordinate origin as a factor of the texture dimensions.
        /// </summary>
        [JsonPropertyName("offset")]
        [JsonConverter(typeof(Float2ArrayConverter))]
        public float[] Offset { get; set; } = { 0, 0 };

        /// <summary>
        /// Rotate the UVs by this many radians counter-clockwise around the origin. This is equivalent to a similar rotation of the image clockwise.
        /// </summary>
        [JsonPropertyName("rotation")]
        public float Rotation { get; set; }

        /// <summary>
        /// The scale factor applied to the components of the UV coordinates.
        /// </summary>
        [JsonPropertyName("scale")]
        [JsonConverter(typeof(Float2ArrayConverter))]
        public float[] Scale { get; set; } = { 1, 1 };

        /// <summary>
        /// Overrides the textureInfo texCoord value if supplied, and if this extension is supported.
        /// </summary>
        [JsonPropertyName("texCoord")]
        public int TexCoord { get; set; } = -1;

        internal void GltfSerialize(JsonWriter writer)
        {
            writer.AddObject();
            if (Offset != null)
            {
                writer.AddArrayProperty("offset", Offset);
            }
            if (Scale != null)
            {
                writer.AddArrayProperty("scale", Scale);
            }
            if (math.abs(Rotation) >= float.Epsilon)
            {
                writer.AddProperty("rotation", Rotation);
            }
            if (TexCoord >= 0)
            {
                writer.AddProperty("texCoord", TexCoord);
            }
            writer.Close();
        }
    }
}
