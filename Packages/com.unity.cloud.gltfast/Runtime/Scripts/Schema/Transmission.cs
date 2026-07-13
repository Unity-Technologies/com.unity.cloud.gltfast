// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using Unity.Gltfast.Text.Json.Serialization;

namespace GLTFast.Schema
{

    /// <summary>
    /// Extension for optical transparency (transmission)
    /// </summary>
    /// <seealso href="https://github.com/KhronosGroup/glTF/tree/main/extensions/2.0/Khronos/KHR_materials_transmission"/>
    public class Transmission
    {

        /// <summary>
        /// The base fraction of light that is transmitted through the surface.
        /// </summary>
        [JsonPropertyName("transmissionFactor")]
        public float TransmissionFactor { get; set; }

        /// <summary>
        /// A texture that defines the transmission fraction of the surface,
        /// stored in the R channel. This will be multiplied by
        /// transmissionFactor.
        /// </summary>
        [JsonPropertyName("transmissionTexture")]
        public TextureInfo TransmissionTexture { get; set; }
    }
}
