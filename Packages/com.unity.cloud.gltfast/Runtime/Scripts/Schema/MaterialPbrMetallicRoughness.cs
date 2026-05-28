// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
#if UNITY_6000_5_OR_NEWER
using System.Text.Json;
using System.Text.Json.Serialization;
#else
using Unity.Gltfast.Text.Json;
using Unity.Gltfast.Text.Json.Serialization;
#endif
using Unity.Mathematics;
using UnityEngine;

namespace GLTFast.Schema
{
    /// <summary>
    /// A set of parameter values that are used to define the metallic-roughness
    /// material model from Physically-Based Rendering (PBR) methodology.
    /// </summary>
    [Serializable]
    public class PbrMetallicRoughness : IGltfObject
    {
        /// <summary>
        /// The base color texture.
        /// This texture contains RGB(A) components in sRGB color space.
        /// The first three components (RGB) specify the base color of the material.
        /// If the fourth component (A) is present, it represents the opacity of the
        /// material. Otherwise, an opacity of 1.0 is assumed.
        /// </summary>
        [JsonPropertyName("baseColorTexture")]
        public TextureInfo BaseColorTexture { get; set; }

        /// <summary>
        /// The metallic-roughness texture has two components.
        /// The first component (R) contains the metallic-ness of the material.
        /// The second component (G) contains the roughness of the material.
        /// These values are linear.
        /// If the third component (B) and/or the fourth component (A) are present,
        /// they are ignored.
        /// </summary>
        [JsonPropertyName("metallicRoughnessTexture")]
        public TextureInfo MetallicRoughnessTexture { get; set; }

        /// <summary>
        /// The RGBA components of the base color of the material.
        /// The fourth component (A) is the opacity of the material.
        /// These values are linear.
        /// </summary>
        public float[] baseColorFactor = { 1, 1, 1, 1 };

        /// <summary>
        /// Base color of the material in linear color space.
        /// </summary>
        public Color BaseColor
        {
            get =>
                new Color(
                    baseColorFactor[0],
                    baseColorFactor[1],
                    baseColorFactor[2],
                    baseColorFactor[3]
                );
            set
            {
                baseColorFactor = new[] { value.r, value.g, value.b, value.a };
            }
        }

        /// <summary>
        /// The metalness of the material.
        /// A value of 1.0 means the material is a metal.
        /// A value of 0.0 means the material is a dielectric.
        /// Values in between are for blending between metals and dielectrics such as
        /// dirty metallic surfaces.
        /// This value is linear.
        /// </summary>
        public float metallicFactor = 1;

        /// <summary>
        /// The roughness of the material.
        /// A value of 1.0 means the material is completely rough.
        /// A value of 0.0 means the material is completely smooth.
        /// This value is linear.
        /// </summary>
        public float roughnessFactor = 1;

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
            if (baseColorFactor != null && (
                math.abs(baseColorFactor[0] - 1f) > Constants.epsilon ||
                math.abs(baseColorFactor[1] - 1f) > Constants.epsilon ||
                math.abs(baseColorFactor[2] - 1f) > Constants.epsilon ||
                math.abs(baseColorFactor[3] - 1f) > Constants.epsilon
                ))
            {
                writer.AddArrayProperty("baseColorFactor", baseColorFactor);
            }

            if (metallicFactor < 1f)
            {
                writer.AddProperty("metallicFactor", metallicFactor);
            }
            if (roughnessFactor < 1f)
            {
                writer.AddProperty("roughnessFactor", roughnessFactor);
            }
            if (BaseColorTexture != null)
            {
                writer.AddProperty("baseColorTexture");
                BaseColorTexture.GltfSerialize(writer);
            }
            if (MetallicRoughnessTexture != null)
            {
                writer.AddProperty("metallicRoughnessTexture");
                MetallicRoughnessTexture.GltfSerialize(writer);
            }

            writer.Close();
        }
    }
}
