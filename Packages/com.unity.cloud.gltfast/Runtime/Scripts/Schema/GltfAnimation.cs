// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

#if UNITY_ANIMATION || GLTFAST_ANIMATION

using System;
using System.Collections.Generic;
#if UNITY_6000_5_OR_NEWER
using System.Text.Json;
using System.Text.Json.Serialization;
#else
using Unity.Gltfast.Text.Json;
using Unity.Gltfast.Text.Json.Serialization;
#endif
#endif

namespace GLTFast.Schema
{

#if UNITY_ANIMATION || GLTFAST_ANIMATION
    /// <summary>
    /// A keyframe animation.
    /// </summary>
    /// <seealso href="https://registry.khronos.org/glTF/specs/2.0/glTF-2.0.html#reference-animation"/>
    [Serializable]
    public class Animation : NamedObject, IGltfObject
    {
        /// <inheritdoc cref="Channels"/>
        public AnimationChannel[] channels;

        /// <inheritdoc cref="Samplers"/>
        public AnimationSampler[] samplers;

        /// <summary>
        /// An array of channels, each of which targets an animation's sampler at a
        /// node's property. Different channels of the same animation can't have equal
        /// targets.
        /// </summary>
        public IReadOnlyList<AnimationChannel> Channels => channels;

        /// <summary>
        /// An array of samplers that combines input and output accessors with an
        /// interpolation algorithm to define a keyframe graph (but not its target).
        /// </summary>
        public IReadOnlyList<AnimationSampler> Samplers => samplers;

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

        internal void GltfSerialize(JsonWriter writer)
        {
            writer.AddObject();
            GltfSerializeName(writer);
            writer.Close();
            throw new NotImplementedException($"GltfSerialize missing on {GetType()}");
        }
    }
#else
    // Empty placeholder class used when animation is not enabled.
    /// <summary>
    /// A keyframe animation.
    /// </summary>
    /// <seealso href="https://registry.khronos.org/glTF/specs/2.0/glTF-2.0.html#reference-animation"/>
    public class Animation { }
#endif // UNITY_ANIMATION || GLTFAST_ANIMATION
}
