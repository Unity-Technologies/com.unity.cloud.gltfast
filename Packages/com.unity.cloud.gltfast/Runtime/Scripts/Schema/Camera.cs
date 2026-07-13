// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using Unity.Gltfast.Text.Json;
using Unity.Gltfast.Text.Json.Serialization;

namespace GLTFast.Schema
{

    /// <summary>
    /// A camera’s projection
    /// </summary>
    public class Camera : NamedObject, IGltfObject
    {
        /// <inheritdoc cref="CameraOrthographic"/>
        [JsonPropertyName("orthographic")]
        public CameraOrthographic Orthographic { get; set; }

        /// <inheritdoc cref="CameraPerspective"/>
        [JsonPropertyName("perspective")]
        public CameraPerspective Perspective { get; set; }

        /// <inheritdoc cref="CameraType"/>
        [JsonPropertyName("type")]
        [JsonConverter(typeof(CameraTypeValueConverter))]
        public EnumOrRawValue<CameraType> Type { get; set; }

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
