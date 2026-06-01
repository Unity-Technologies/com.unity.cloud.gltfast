// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using Unity.Gltfast.Text.Json.Serialization;
using Unity.Mathematics;

namespace GLTFast.Schema
{
    /// <summary>
    /// glTF spot light properties
    /// </summary>
    public class SpotLight
    {

        /// <summary>
        /// Angle, in radians, from centre of spotlight where falloff begins
        /// Must be greater than or equal to 0 and less than outerConeAngle
        /// </summary>
        [JsonPropertyName("innerConeAngle")]
        public float InnerConeAngle { get; set; }

        /// <summary>
        /// Angle, in radians, from centre of spotlight where falloff ends.
        /// Must be greater than innerConeAngle and less than or equal to
        /// PI / 2.0.
        /// </summary>
        [JsonPropertyName("outerConeAngle")]
        public float OuterConeAngle { get; set; } = math.PI / 4f;

        internal void GltfSerialize(JsonWriter writer)
        {
            writer.AddObject();
            writer.AddProperty("innerConeAngle", InnerConeAngle);
            writer.AddProperty("outerConeAngle", OuterConeAngle);
            writer.Close();
        }
    }
}
