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
        /// Light's color in linear space
        /// </summary>
        [JsonPropertyName("color")]
        [JsonConverter(typeof(ColorConverter))]
        public Color Color { get; set; } = Color.White;

        /// <summary>
        /// Brightness of light in. The units that this is defined in depend on
        /// the type of light. point and spot lights use luminous intensity in
        /// candela (lm/sr) while directional lights use illuminance
        /// in lux (lm/m2)
        /// </summary>
        [JsonIgnore]
        public float Intensity { get; set; } = 1f;

        [JsonPropertyName("intensity"), JsonInclude]
        internal float? IntensitySerialized
        {
            get => Mathematics.ApproximatelyOne(Intensity) ? null : Intensity;
            set => Intensity = value ?? 1f;
        }

        /// <summary>
        /// Hint defining a distance cutoff at which the light's intensity may
        /// be considered to have reached zero. Supported only for point and
        /// spot lights. Must be > 0. When undefined, range is assumed to be
        /// infinite.
        /// </summary>
        [JsonIgnore]
        public float Range { get; set; } = -1f;

        [JsonPropertyName("range"), JsonInclude]
        internal float? RangeSerialized
        {
            get => Mathematics.Approximately(Range, -1f) ? null : Range;
            set => Range = value ?? -1f;
        }

        /// <summary>
        /// Spot light properties (only set on spot lights).
        /// </summary>
        [JsonPropertyName("spot")]
        public SpotLight Spot { get; set; }

        /// <inheritdoc cref="LightType"/>
        [JsonPropertyName("type")]
        [JsonConverter(typeof(LightTypeValueConverter))]
        public EnumOrRawValue<LightType> Type { get; set; }

        internal void GltfSerialize(JsonWriter writer)
        {
            writer.AddObject();
            var type = Type.RawValue != null
                ? System.Text.Encoding.UTF8.GetString(Type.RawValue)
                : Type.Value switch
                {
                    LightType.Spot => "spot",
                    LightType.Directional => "directional",
                    LightType.Point => "point",
                    _ => throw new ArgumentOutOfRangeException(nameof(Type), Type.Value, $"Unsupported light type: {Type.Value}")
                };
            writer.AddProperty("type", type);
            GltfSerializeName(writer);
            if (Color != Color.White)
            {
                writer.AddColorProperty("color", Color);
            }
            if (!Mathematics.ApproximatelyOne(Intensity))
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
