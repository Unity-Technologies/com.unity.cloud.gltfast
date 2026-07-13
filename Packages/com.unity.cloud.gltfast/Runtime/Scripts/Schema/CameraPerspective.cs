// SPDX-FileCopyrightText: 2026 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using Unity.Gltfast.Text.Json;
using Unity.Gltfast.Text.Json.Serialization;

namespace GLTFast.Schema
{
    /// <summary>
    /// A perspective camera containing properties to create a perspective projection matrix.
    /// </summary>
    public class CameraPerspective : IGltfObject
    {

        /// <summary>
        /// The floating-point aspect ratio of the field of view.
        /// </summary>
        [JsonPropertyName("aspectRatio")]
        public float? AspectRatio { get; set; }

        /// <summary>
        /// The floating-point vertical field of view in radians.
        /// </summary>
        // ReSharper disable once IdentifierTypo
        [JsonPropertyName("yfov")]
        public float Yfov { get; set; }

        /// <summary>
        /// The floating-point distance to the far clipping plane.
        /// </summary>
        // ReSharper disable once IdentifierTypo
        [JsonPropertyName("zfar")]
        public float? Zfar { get; set; }

        /// <summary>
        /// The floating-point distance to the near clipping plane.
        /// </summary>
        // ReSharper disable once IdentifierTypo
        [JsonPropertyName("znear")]
        public float Znear { get; set; }

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
