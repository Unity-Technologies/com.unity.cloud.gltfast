// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using Unity.Gltfast.Text.Json;
using Unity.Gltfast.Text.Json.Serialization;
using UnityEngine.Scripting.APIUpdating;

namespace Unity.Cloud.Gltfast.Schema
{

    /// <summary>
    /// Joints and matrices defining a skinned mesh.
    /// </summary>
    [MovedFrom(true, sourceNamespace: "GLTFast.Schema", sourceAssembly: "glTFast")]
    public class Skin : NamedObject, IGltfObject
    {
        /// <summary>
        /// The index of the accessor containing the
        /// floating-point 4x4 inverse-bind matrices.
        /// </summary>
        [JsonPropertyName("inverseBindMatrices")]
        public int? InverseBindMatrices { get; set; }

        /// <summary>
        /// The index of the node used as a skeleton root.
        /// </summary>
        [JsonPropertyName("skeleton")]
        public int? Skeleton { get; set; }

        /// <summary>
        /// Indices of skeleton nodes, used as joints in this skin.
        /// </summary>
        [JsonPropertyName("joints")]
        public List<uint> Joints { get; set; }

        /// <inheritdoc cref="Asset.Extensions"/>
        [JsonPropertyName("extensions")]
        public UnclassifiedData Extensions { get; set; }

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
