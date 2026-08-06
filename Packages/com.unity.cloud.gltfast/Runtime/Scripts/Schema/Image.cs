// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using Unity.Gltfast.Text.Json;
using Unity.Gltfast.Text.Json.Serialization;
using UnityEngine.Scripting.APIUpdating;

namespace Unity.Cloud.Gltfast.Schema
{

    /// <summary>
    /// Image data used to create a texture.
    /// </summary>
    [MovedFrom(true, sourceNamespace: "GLTFast.Schema", sourceAssembly: "glTFast")]
    public class Image : NamedObject, IAdditionalPropertyContainer
    {
        /// <summary>
        /// The uri of the image.  Relative paths are relative to the .gltf file.
        /// Instead of referencing an external file, the uri can also be a data-uri.
        /// The image format must be jpg, png, bmp, or gif.
        /// </summary>
        [JsonPropertyName("uri")]
        public UriValue Uri { get; set; }

        /// <summary>
        /// The image's MIME type.
        /// </summary>
        [JsonPropertyName("mimeType")]
        [JsonConverter(typeof(ImageMimeTypeValueConverter))]
        public EnumOrRawValue<ImageMimeType> MimeType { get; set; }

        /// <summary>
        /// The index of the bufferView that contains the image.
        /// Use this instead of the image's uri property.
        /// </summary>
        [JsonPropertyName("bufferView")]
        public int? BufferView { get; set; }

        /// <inheritdoc cref="Asset.Extensions"/>
        [JsonPropertyName("extensions")]
        public AdditionalPropertyContainer Extensions { get; set; }

        /// <inheritdoc cref="Root.Extras"/>
        [JsonPropertyName("extras")]
        public AdditionalPropertyContainer Extras { get; set; }

        /// <summary>JSON properties without a matching member.</summary>
        [JsonExtensionData, JsonInclude]
        internal Dictionary<string, JsonElement> ExtensionData { get; set; }

        /// <inheritdoc/>
        [JsonIgnore]
        public Properties AdditionalProperties => new(ExtensionData);

    }
}
