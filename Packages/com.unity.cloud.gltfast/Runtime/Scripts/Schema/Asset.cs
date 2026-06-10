// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using Unity.Gltfast.Text.Json;
using Unity.Gltfast.Text.Json.Serialization;

namespace GLTFast.Schema
{

    /// <summary>
    /// Metadata about the glTF asset.
    /// </summary>
    public class Asset : NamedObject, IGltfObject
    {
        /// <summary>
        /// A copyright message suitable for display to credit the content creator.
        /// </summary>
        [JsonPropertyName("copyright")]
        public string Copyright { get; set; }

        /// <summary>
        /// Tool that generated this glTF model. Useful for debugging.
        /// </summary>
        [JsonPropertyName("generator")]
        public string Generator { get; set; }

        /// <summary>
        /// The glTF version.
        /// </summary>
        [JsonPropertyName("version")]
        public string Version { get; set; }

        /// <summary>
        /// The minimum glTF version that this asset targets.
        /// </summary>
        [JsonPropertyName("minVersion")]
        public string MinVersion { get; set; }

        /// <summary>JSON object with extension-specific objects.</summary>
        /// <seealso href="https://registry.khronos.org/glTF/specs/2.0/glTF-2.0.html#reference-extension"/>
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

        internal void GltfSerialize(JsonWriter writer)
        {
            writer.OpenBrackets();
            if (!string.IsNullOrEmpty(Version))
            {
                writer.AddProperty("version", Version);
            }
            if (!string.IsNullOrEmpty(Generator))
            {
                writer.AddPropertySafe("generator", Generator);
            }
            if (!string.IsNullOrEmpty(Copyright))
            {
                writer.AddPropertySafe("copyright", Copyright);
            }
            if (!string.IsNullOrEmpty(MinVersion))
            {
                writer.AddProperty("minVersion", MinVersion);
            }
            writer.Close();
        }
    }
}
