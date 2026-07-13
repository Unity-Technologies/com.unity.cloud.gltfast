// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using Unity.Gltfast.Text.Json;
using Unity.Gltfast.Text.Json.Serialization;

namespace GLTFast.Schema
{

    /// <summary>
    /// glTF root extensions
    /// </summary>
    public class RootExtensions : IGltfObject
    {

        /// <inheritdoc cref="Schema.LightsPunctual"/>
        [JsonPropertyName("KHR_lights_punctual")]
        public LightsPunctual LightsPunctual { get; set; }

        /// <inheritdoc cref="MaterialsVariantsRootExtension"/>
        [JsonPropertyName("KHR_materials_variants")]
        public MaterialsVariantsRootExtension MaterialsVariants { get; set; }

        /// <summary>
        /// JSON properties without a matching member.
        /// </summary>
        [JsonExtensionData, JsonInclude] internal Dictionary<string, JsonElement> ExtensionsData { get; set; }

        /// <inheritdoc/>
        public bool TryGetValue<T>(string key, out T value)
        {
            return ExtensionsData.TryGetValue(key, out value);
        }
    }
}
