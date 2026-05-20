// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

#if UNITY_ANIMATION || GLTFAST_ANIMATION

using System;
using System.Collections.Generic;
using Unity.Gltfast.Text.Json;
using Unity.Gltfast.Text.Json.Serialization;
#endif

namespace GLTFast.Schema
{

#if UNITY_ANIMATION || GLTFAST_ANIMATION
    /// <inheritdoc />
    [Serializable]
    public class Animation : AnimationBase<AnimationChannel, AnimationSampler> { }

    /// <inheritdoc />
    /// <typeparam name="TChannel">Animation channel type</typeparam>
    /// <typeparam name="TSampler">Animation sampler type</typeparam>
    [Serializable]
    public abstract class AnimationBase<TChannel, TSampler> : AnimationBase
        where TChannel : AnimationChannelBase
        where TSampler : AnimationSampler
    {
        public TChannel[] channels;
        public TSampler[] samplers;

        public override IReadOnlyList<AnimationChannelBase> Channels => channels;

        public override IReadOnlyList<AnimationSampler> Samplers => samplers;
    }

    /// <summary>
    /// A keyframe animation.
    /// </summary>
    /// <seealso href="https://registry.khronos.org/glTF/specs/2.0/glTF-2.0.html#reference-animation"/>
    [Serializable]
    public abstract class AnimationBase : NamedObject, IGltfObject
    {
        /// <summary>
        /// An array of channels, each of which targets an animation's sampler at a
        /// node's property. Different channels of the same animation can't have equal
        /// targets.
        /// </summary>
        public abstract IReadOnlyList<AnimationChannelBase> Channels { get; }

        /// <summary>
        /// An array of samplers that combines input and output accessors with an
        /// interpolation algorithm to define a keyframe graph (but not its target).
        /// </summary>
        public abstract IReadOnlyList<AnimationSampler> Samplers { get; }

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

        internal void GltfSerialize(JsonWriter writer) {
            writer.AddObject();
            GltfSerializeName(writer);
            writer.Close();
            throw new NotImplementedException($"GltfSerialize missing on {GetType()}");
        }
    }
#else
    // Empty placeholder classes used in generic type definitions
    /// <inheritdoc />
    public class Animation : AnimationBase { }

    /// <summary>
    /// A keyframe animation.
    /// </summary>
    /// <seealso href="https://registry.khronos.org/glTF/specs/2.0/glTF-2.0.html#reference-animation"/>
    public abstract class AnimationBase { }
#endif // UNITY_ANIMATION || GLTFAST_ANIMATION
}
