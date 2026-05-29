// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using Unity.Gltfast.Text.Json;
using Unity.Gltfast.Text.Json.Serialization;

namespace GLTFast.Schema
{
    /// <summary>
    /// A texture is defined by an image and a sampler.
    /// </summary>
    public class Texture : NamedObject, IGltfObject
    {
        /// <inheritdoc cref="TextureExtensions"/>
        [JsonPropertyName("extensions")]
        public TextureExtensions Extensions { get; set; }

        /// <summary>
        /// The index of the sampler used by this texture.
        /// </summary>
        [JsonPropertyName("sampler")]
        public int Sampler { get; set; } = -1;

        /// <summary>
        /// The index of the image used by this texture.
        /// </summary>
        [JsonPropertyName("source")]
        public int Source { get; set; } = -1;

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

        /// <summary>
        /// Retrieves the final image index.
        /// </summary>
        /// <returns>Final image index</returns>
        public int GetImageIndex()
        {
            if (Extensions != null)
            {
                if (Extensions.BasisU != null && Extensions.BasisU.Source >= 0)
                {
                    return Extensions.BasisU.Source;
                }
            }
            return Source;
        }

        /// <summary>
        /// True, if the texture is of the KTX format.
        /// </summary>
        public bool IsKtx => Extensions?.BasisU != null;

        internal void GltfSerialize(JsonWriter writer)
        {
            writer.AddObject();
            GltfSerializeName(writer);
            if (Source >= 0)
            {
                writer.AddProperty("source", Source);
            }
            if (Sampler >= 0)
            {
                writer.AddProperty("sampler", Sampler);
            }
            if (Extensions != null)
            {
                writer.AddProperty("extensions");
                Extensions.GltfSerialize(writer);
            }
            writer.Close();
        }

        /// <summary>
        /// Determines whether two object instances are equal.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns>true if the specified object is equal to the current object; otherwise, false.</returns>
        // TODO: Remove upon next major release. This serves no purpose anymore except keeping the API intact.
        public override bool Equals(object obj)
        {
            // ReSharper disable once BaseObjectEqualsIsObjectEquals
            return base.Equals(obj);
        }

        /// <summary>
        /// Default hash function.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        // TODO: Remove upon next major release. This serves no purpose anymore except keeping the API intact.
        public override int GetHashCode()
        {
            // ReSharper disable once BaseObjectGetHashCodeCallInGetHashCode
            return base.GetHashCode();
        }
    }
}
