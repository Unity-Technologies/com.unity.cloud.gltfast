// SPDX-FileCopyrightText: 2026 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using Unity.Mathematics;

namespace GLTFast.Schema
{
    /// <summary>
    /// glTF's core material model is constrained to an LDR (low dynamic range) range for the
    /// emissive channel (the maximum emissive value is 1.0). This extension allows the emissive
    /// value to be scaled beyond 1.0, providing a much wider range of emissive HDR values.
    /// </summary>
    /// <seealso href="https://github.com/KhronosGroup/glTF/tree/main/extensions/2.0/Khronos/KHR_materials_emissive_strength"/>
    [Serializable]
    public class MaterialEmissiveStrength
    {
        /// <summary>
        /// Default emissive strength multiplier.
        /// </summary>
        public const float defaultEmissiveStrength = 1f;

        /// <summary>
        /// The strength adjustment to be multiplied with the material's emissive value.
        /// </summary>
        public float emissiveStrength = defaultEmissiveStrength;

        internal void GltfSerialize(JsonWriter writer)
        {
            writer.AddObject();
            if (math.abs(emissiveStrength - defaultEmissiveStrength) > Constants.epsilon)
            {
                writer.AddProperty("emissiveStrength", emissiveStrength);
            }
            writer.Close();
        }
    }
}
