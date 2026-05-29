// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

#if UNITY_ANIMATION || GLTFAST_ANIMATION

using System;
using System.Collections.Generic;
using Unity.Gltfast.Text.Json;
using Unity.Gltfast.Text.Json.Serialization;

namespace GLTFast.Schema
{
    [Serializable]
    public class AnimationChannelTarget : IGltfObject
    {
        /// <summary>
        /// The index of the node to target.
        /// </summary>
        public int node;

        /// <summary>
        /// The name of the node's TRS property to modify.
        /// </summary>
        // Field is public for unified serialization only. Warn via Obsolete attribute.
        [Obsolete("Use GetPath for access.")]
        public string path;

        AnimationChannel.Path m_Path;

        /// <inheritdoc cref="Asset.extensions"/>
        public UnclassifiedData extensions;

        /// <inheritdoc cref="Root.extras"/>
        public UnclassifiedData extras;

        /// <summary>JSON properties without a matching member.</summary>
        [JsonExtensionData, JsonInclude] internal Dictionary<string, JsonElement> ExtensionsData { get; set; }

        /// <inheritdoc/>
        public bool TryGetValue<T>(string key, out T value)
        {
            return ExtensionsData.TryGetValue(key, out value);
        }

        public AnimationChannel.Path GetPath()
        {
            if (m_Path != AnimationChannel.Path.Unknown)
            {
                return m_Path;
            }

#pragma warning disable CS0618 // Type or member is obsolete
            if (!Enum.TryParse(path, true, out m_Path))
            {
                m_Path = AnimationChannel.Path.Invalid;
            }
            path = null;
#pragma warning restore CS0618 // Type or member is obsolete
            return m_Path;
        }

        internal void GltfSerialize(JsonWriter writer)
        {
            throw new NotImplementedException($"GltfSerialize missing on {GetType()}");
        }
    }
}
#endif // UNITY_ANIMATION || GLTFAST_ANIMATION
