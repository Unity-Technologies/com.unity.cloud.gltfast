// SPDX-FileCopyrightText: 2026 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using Unity.Gltfast.Text.Json;
using Unity.Gltfast.Text.Json.Serialization;

namespace GLTFast.Schema
{
    /// <summary>
    /// An orthographic camera containing properties to create an orthographic projection matrix.
    /// </summary>
    public class CameraOrthographic : IGltfObject
    {
        /// <summary>
        /// The floating-point horizontal magnification of the view. Must not be zero.
        /// </summary>
        // ReSharper disable once IdentifierTypo
        [JsonPropertyName("xmag")]
        public float Xmag { get; set; }

        /// <summary>
        /// The floating-point vertical magnification of the view. Must not be zero.
        /// </summary>
        // ReSharper disable once IdentifierTypo
        [JsonPropertyName("ymag")]
        public float Ymag { get; set; }

        /// <summary>
        /// The floating-point distance to the far clipping plane.
        /// <see cref="Zfar"/> must be greater than <see cref="Znear"/>.
        /// </summary>
        // ReSharper disable once IdentifierTypo
        [JsonPropertyName("zfar")]
        public float Zfar { get; set; }

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
