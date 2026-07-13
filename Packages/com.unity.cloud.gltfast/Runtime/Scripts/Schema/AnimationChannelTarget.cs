// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

#if UNITY_ANIMATION || GLTFAST_ANIMATION

using System.Collections.Generic;
using Unity.Gltfast.Text.Json;
using Unity.Gltfast.Text.Json.Serialization;

namespace GLTFast.Schema
{
    public class AnimationChannelTarget : IGltfObject
    {
        /// <summary>
        /// The index of the node to target.
        /// </summary>
        /// <remarks>
        /// Optional per the glTF specification. <see langword="null"/> signals an absent target;
        /// when undefined, the animated object may be defined by an extension.
        /// </remarks>
        [JsonPropertyName("node")]
        public int? Node { get; set; }

        /// <inheritdoc cref="AnimationPath"/>
        [JsonPropertyName("path")]
        [JsonConverter(typeof(AnimationPathValueConverter))]
        public EnumOrRawValue<AnimationPath> Path { get; set; }

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
