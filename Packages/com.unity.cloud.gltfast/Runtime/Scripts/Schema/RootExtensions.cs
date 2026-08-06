// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using Unity.Gltfast.Text.Json.Serialization;
using UnityEngine.Scripting.APIUpdating;

namespace Unity.Cloud.Gltfast.Schema
{
    /// <summary>
    /// glTF root extensions
    /// </summary>
    [MovedFrom(true, sourceNamespace: "GLTFast.Schema", sourceAssembly: "glTFast")]
    public class RootExtensions : AdditionalPropertyContainer
    {
        /// <inheritdoc cref="Schema.LightsPunctual"/>
        [JsonPropertyName("KHR_lights_punctual")]
        public LightsPunctual LightsPunctual { get; set; }

        /// <inheritdoc cref="MaterialsVariantsRootExtension"/>
        [JsonPropertyName("KHR_materials_variants")]
        public MaterialsVariantsRootExtension MaterialsVariants { get; set; }
    }
}
