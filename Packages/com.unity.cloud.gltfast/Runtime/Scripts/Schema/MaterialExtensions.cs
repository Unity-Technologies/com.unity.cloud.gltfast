// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using Unity.Gltfast.Text.Json;
using Unity.Gltfast.Text.Json.Serialization;

namespace GLTFast.Schema
{
    /// <summary>
    /// Material extensions.
    /// </summary>
    public class MaterialExtensions : IGltfObject
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

        /// <inheritdoc cref="Root.Extras"/>
        [JsonPropertyName("extras")]
        public UnclassifiedData Extras { get; set; }

        /// <summary>JSON properties without a matching member.</summary>
        [JsonExtensionData, JsonInclude] internal Dictionary<string, JsonElement> ExtensionsData { get; set; }

        /// <inheritdoc/>
        public bool TryGetValue<T>(string key, out T value)
        {
            return ExtensionsData.TryGetValue(key, out value);
        }
    }
}
