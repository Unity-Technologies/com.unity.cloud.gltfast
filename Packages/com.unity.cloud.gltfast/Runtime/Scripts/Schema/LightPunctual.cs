// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using Unity.Gltfast.Text.Json.Serialization;

using UnityEngine.Assertions;

namespace GLTFast.Schema
{
    /// <summary>
    /// Light
    /// </summary>
    public class LightPunctual : NamedObject
    {
        /// <summary>
        /// RGB values for light's color in linear space
        /// </summary>
        // Property is public for unified serialization only. Warn via Obsolete attribute.
        [Obsolete("Use LightColor for access.")]
        [JsonPropertyName("color")]
        [JsonConverter(typeof(Float3ArrayConverter))]
        public float[] Color { get; set; } = { 1, 1, 1 };

        /// <summary>
        /// Light's color in linear space
        /// </summary>
        public UnityEngine.Color LightColor
        {
#pragma warning disable CS0618 // Type or member is obsolete
            get =>
                new UnityEngine.Color(
                    Color[0],
                    Color[1],
                    Color[2]
                );
            set
            {
                Color = new[] { value.r, value.g, value.b };
            }
#pragma warning restore CS0618 // Type or member is obsolete
        }

        /// <summary>
        /// Brightness of light in. The units that this is defined in depend on
        /// the type of light. point and spot lights use luminous intensity in
        /// candela (lm/sr) while directional lights use illuminance
        /// in lux (lm/m2)
        /// </summary>
        [JsonPropertyName("intensity")]
        public float Intensity { get; set; } = 1;

        /// <summary>
        /// Hint defining a distance cutoff at which the light's intensity may
        /// be considered to have reached zero. Supported only for point and
        /// spot lights. Must be > 0. When undefined, range is assumed to be
        /// infinite.
        /// </summary>
        [JsonPropertyName("range")]
        public float Range { get; set; } = -1;

        /// <summary>
        /// Spot light properties (only set on spot lights).
        /// </summary>
        [JsonPropertyName("spot")]
        public SpotLight Spot { get; set; }

        /// <inheritdoc cref="LightType"/>
        [JsonPropertyName("type")]
        public LightType Type { get; set; }

        internal void GltfSerialize(JsonWriter writer)
        {
            writer.AddObject();
            Assert.AreNotEqual(LightType.Undefined, Type);
            writer.AddProperty("type", Type.ToString().ToLowerInvariant());
            GltfSerializeName(writer);
            if (LightColor != UnityEngine.Color.white)
            {
#pragma warning disable CS0618 // Type or member is obsolete
                writer.AddArrayProperty("color", Color);
#pragma warning restore CS0618 // Type or member is obsolete
            }
            if (Math.Abs(Intensity - 1.0) > Constants.epsilon)
            {
                writer.AddProperty("intensity", Intensity);
            }
            if (Range > 0 && Type != LightType.Directional)
            {
                writer.AddProperty("range", Range);
            }
            if (Spot != null)
            {
                writer.AddProperty("spot");
                Spot.GltfSerialize(writer);
            }
            writer.Close();
        }

    }
}
