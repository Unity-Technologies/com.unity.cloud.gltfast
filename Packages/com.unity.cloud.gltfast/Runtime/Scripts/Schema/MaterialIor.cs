// SPDX-FileCopyrightText: 2024 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using Unity.Gltfast.Text.Json.Serialization;
using Unity.Mathematics;

namespace GLTFast.Schema
{
    /// <summary>
    /// The dielectric BRDF of the metallic-roughness material in glTF uses a fixed value of 1.5 for the index of
    /// refraction. This is a good fit for many plastics and glass, but not for other materials like water or asphalt,
    /// sapphire or diamond. This extension allows users to set the index of refraction to a certain value.
    /// </summary>
    /// <seealso href="https://github.com/KhronosGroup/glTF/tree/main/extensions/2.0/Khronos/KHR_materials_ior"/>
    public class MaterialIor
    {
        /// <summary>
        /// Default index of refraction. A good compromise for most opaque, dielectric materials.
        /// </summary>
        /// <seealso href="https://registry.khronos.org/glTF/specs/2.0/glTF-2.0.html#dielectrics"/>
        public const float defaultIndexOfRefraction = 1.5f;

        /// <summary>
        /// The index of refraction.
        /// </summary>
        [JsonPropertyName("ior")]
        public float Ior { get; set; } = defaultIndexOfRefraction;

        internal void GltfSerialize(JsonWriter writer)
        {
            writer.AddObject();
            if (math.abs(Ior - defaultIndexOfRefraction) > Constants.epsilon)
            {
                writer.AddProperty("ior", Ior);
            }
            writer.Close();
        }
    }
}
