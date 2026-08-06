// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using Unity.Gltfast.Text.Json.Serialization;
using UnityEngine.Scripting.APIUpdating;

namespace Unity.Cloud.Gltfast.Schema
{
    /// <summary>
    /// Material extensions.
    /// </summary>
    [MovedFrom(true, sourceNamespace: "GLTFast.Schema", sourceAssembly: "glTFast")]
    public class MaterialExtensions : AdditionalPropertyContainer
    {
        /// <inheritdoc cref="Schema.PbrSpecularGlossiness"/>
        [JsonPropertyName("KHR_materials_pbrSpecularGlossiness")]
        public PbrSpecularGlossiness PbrSpecularGlossiness { get; set; }

        /// <inheritdoc cref="MaterialUnlit"/>
        [JsonPropertyName("KHR_materials_unlit")]
        public MaterialUnlit Unlit { get; set; }

        /// <inheritdoc cref="Schema.Transmission"/>
        [JsonPropertyName("KHR_materials_transmission")]
        public Transmission Transmission { get; set; }

        /// <inheritdoc cref="ClearCoat"/>
        [JsonPropertyName("KHR_materials_clearcoat")]
        public ClearCoat Clearcoat { get; set; }

        /// <inheritdoc cref="Schema.Sheen"/>
        [JsonPropertyName("KHR_materials_sheen")]
        public Sheen Sheen { get; set; }

        /// <inheritdoc cref="MaterialSpecular"/>
        [JsonPropertyName("KHR_materials_specular")]
        public MaterialSpecular Specular { get; set; }

        /// <inheritdoc cref="MaterialIor"/>
        [JsonPropertyName("KHR_materials_ior")]
        public MaterialIor IndexOfRefraction { get; set; }
    }
}
