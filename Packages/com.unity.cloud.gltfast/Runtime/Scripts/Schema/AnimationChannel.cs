// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

#if UNITY_ANIMATION || GLTFAST_ANIMATION

using System.Collections.Generic;
using Unity.Gltfast.Text.Json;
using Unity.Gltfast.Text.Json.Serialization;

namespace GLTFast.Schema
{
    public class AnimationChannel : IGltfObject
    {
        /// <summary>
        /// The index of a sampler in this animation used to compute the value for the
        /// target, e.g., a node's translation, rotation, or scale (TRS).
        /// </summary>
        [JsonIgnore]
        public int Sampler { get; set; } = Constants.UnsetIndex;

        [JsonPropertyName("sampler"), JsonInclude]
        internal int? SamplerSerialized
        {
            get => Sampler < 0 ? null : Sampler;
            set => Sampler = value ?? Constants.UnsetIndex;
        }

        /// <summary>
        /// The index of the node and TRS property to target.
        /// </summary>
        [JsonPropertyName("target")]
        public AnimationChannelTarget Target { get; set; }

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
#endif // UNITY_ANIMATION || GLTFAST_ANIMATION
