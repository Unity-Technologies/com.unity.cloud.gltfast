// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
#if UNITY_6000_5_OR_NEWER
using System.Text.Json;
using System.Text.Json.Serialization;
#else
using Unity.Gltfast.Text.Json;
using Unity.Gltfast.Text.Json.Serialization;
#endif
using UnityEngine.Serialization;

namespace GLTFast.Schema
{
    /// <summary>
    /// Reference to a texture.
    /// </summary>
    [System.Serializable]
    public class TextureInfo : IGltfObject
    {

        /// <summary>
        /// The index of the texture.
        /// </summary>
        public int index = -1;

        /// <summary>
        /// This integer value is used to construct a string in the format
        /// TEXCOORD_&lt;set index&gt; which is a reference to a key in
        /// mesh.primitives.attributes (e.g. A value of 0 corresponds to TEXCOORD_0).
        /// </summary>
        public int texCoord;

        /// <inheritdoc cref="TextureInfoExtensions"/>
        [JsonPropertyName("extensions")]
        public TextureInfoExtensions Extensions { get; set; }

        /// <inheritdoc cref="Root.extras"/>
        public UnclassifiedData extras;

        /// <summary>JSON properties without a matching member.</summary>
        [JsonExtensionData, JsonInclude] internal Dictionary<string, JsonElement> ExtensionsData { get; set; }

        /// <inheritdoc/>
        public bool TryGetValue<T>(string key, out T value)
        {
            return ExtensionsData.TryGetValue(key, out value);
        }

        /// <summary>
        /// Applies a texture transform by initializing <see cref="Extensions" /> (if required) and setting its
        /// <see cref="TextureInfoExtensions.KHR_texture_transform" /> field.
        /// </summary>
        /// <param name="textureTransform">Texture transform to apply.</param>
        internal void SetTextureTransform(TextureTransform textureTransform)
        {
            Extensions ??= new TextureInfoExtensions();
            Extensions.KHR_texture_transform = textureTransform;
        }

        internal void GltfSerializeTextureInfo(JsonWriter writer)
        {
            if (index >= 0)
            {
                writer.AddProperty("index", index);
            }
            if (texCoord > 0)
            {
                writer.AddProperty("texCoord", texCoord);
            }

            if (Extensions != null)
            {
                writer.AddProperty("extensions");
                Extensions.GltfSerialize(writer);
            }
        }

        internal virtual void GltfSerialize(JsonWriter writer)
        {
            writer.AddObject();
            GltfSerializeTextureInfo(writer);
            writer.Close();
        }
    }
}
