// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using Unity.Gltfast.Text.Json;
using Unity.Gltfast.Text.Json.Serialization;

namespace GLTFast.Schema
{
    /// <summary>
    /// Mesh primitive extensions
    /// </summary>
    public class MeshPrimitiveExtensions : IGltfObject
    {
#if DRACO_IS_INSTALLED
        [JsonPropertyName("KHR_draco_mesh_compression")]
        public MeshPrimitiveDracoExtension DracoMeshCompression { get; set; }
#endif

        /// <inheritdoc cref="MaterialsVariantsMeshPrimitiveExtension"/>
        [JsonPropertyName("KHR_materials_variants")]
        public MaterialsVariantsMeshPrimitiveExtension MaterialsVariants { get; set; }

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
