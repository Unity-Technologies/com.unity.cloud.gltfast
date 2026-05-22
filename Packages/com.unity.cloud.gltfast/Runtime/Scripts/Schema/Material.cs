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
using UnityEngine.Serialization;

namespace GLTFast.Schema
{
    /// <summary>
    /// The material appearance of a primitive.
    /// </summary>
    [Serializable]
    public class Material : NamedObject, IGltfObject
    {
        /// <inheritdoc cref="EmissiveTexture"/>
        public TextureInfo emissiveTexture;
        /// <inheritdoc cref="Extensions"/>
        public MaterialExtensions extensions;
        /// <inheritdoc cref="NormalTexture"/>
        public NormalTextureInfo normalTexture;
        /// <inheritdoc cref="OcclusionTexture"/>
        public OcclusionTextureInfo occlusionTexture;
        /// <inheritdoc cref="PbrMetallicRoughness"/>
        public PbrMetallicRoughness pbrMetallicRoughness;

        /// <summary>
        /// Material extensions.
        /// </summary>
        public MaterialExtensions Extensions => extensions;

        /// <summary>
        /// A set of parameter values that are used to define the metallic-roughness
        /// material model from Physically-Based Rendering (PBR) methodology.
        /// </summary>
        public PbrMetallicRoughness PbrMetallicRoughness => pbrMetallicRoughness;

        /// <summary>
        /// A tangent space normal map. Each texel represents the XYZ components of a
        /// normal vector in tangent space.
        /// </summary>
        public NormalTextureInfo NormalTexture => normalTexture;

        /// <summary>
        /// The occlusion map is a greyscale texture, with white indicating areas that
        /// should receive full indirect lighting and black indicating no indirect
        /// lighting.
        /// </summary>
        public OcclusionTextureInfo OcclusionTexture => occlusionTexture;

        /// <summary>
        /// The emissive map controls the color and intensity of the light being emitted
        /// by the material. This texture contains RGB components in sRGB color space.
        /// If a fourth component (A) is present, it is ignored.
        /// </summary>
        public TextureInfo EmissiveTexture => emissiveTexture;


        /// <summary>
        /// The material’s alpha rendering mode enumeration specifying the
        /// interpretation of the alpha value of the base color.
        /// </summary>
        public enum AlphaMode
        {
            /// <summary>
            /// The alpha value is ignored, and the rendered output is fully
            /// opaque.
            /// </summary>
            Opaque,

            /// <summary>
            /// The rendered output is either fully opaque or fully transparent
            /// depending on the alpha value and the specified alphaCutoff
            /// value
            /// </summary>
            Mask,

            /// <summary>
            /// The alpha value is used to composite the source and destination
            /// areas. The rendered output is combined with the background
            /// using the normal painting operation.
            /// </summary>
            Blend
        }

        /// <summary>
        /// The RGB components of the emissive color of the material.
        /// If an emissiveTexture is specified, this value is multiplied with the texel
        /// values.
        /// </summary>
        // Field is public for unified serialization only. Warn via Obsolete attribute.
        [Obsolete("Use Emissive for access.")]
        [JsonConverter(typeof(Float3ArrayConverter))]
        public float[] emissiveFactor = { 0, 0, 0 };

        /// <summary>
        /// Emissive color of the material.
        /// </summary>
        public Color Emissive
        {
#pragma warning disable CS0618 // Type or member is obsolete
            get => new Color(
                emissiveFactor[0],
                emissiveFactor[1],
                emissiveFactor[2]
                );
            set => emissiveFactor = new[] { value.r, value.g, value.b };
#pragma warning restore CS0618 // Type or member is obsolete
        }

        /// <summary>
        /// The material's alpha rendering mode enumeration specifying the interpretation of the
        /// alpha value of the main factor and texture. In `OPAQUE` mode, the alpha value is
        /// ignored and the rendered output is fully opaque. In `MASK` mode, the rendered output
        /// is either fully opaque or fully transparent depending on the alpha value and the
        /// specified alpha cutoff value. In `BLEND` mode, the alpha value is used to composite
        /// the source and destination areas. The rendered output is combined with the background
        /// using the normal painting operation (i.e. the Porter and Duff over operator).
        /// </summary>
        // Field is public for unified serialization only. Warn via Obsolete attribute.
        [Obsolete("Use GetAlphaMode and SetAlphaMode for access.")]
        public string alphaMode;

        AlphaMode? m_AlphaModeEnum;

        /// <summary>
        /// <see cref="AlphaMode"/> typed and cached getter for <see cref="alphaMode"/> string.
        /// </summary>
        /// <returns>Alpha mode if it was retrieved correctly. <see cref="AlphaMode.Opaque"/> otherwise</returns>
        public AlphaMode GetAlphaMode()
        {
            if (m_AlphaModeEnum.HasValue)
            {
                return m_AlphaModeEnum.Value;
            }

#pragma warning disable CS0618 // Type or member is obsolete
            m_AlphaModeEnum = Enum.TryParse<AlphaMode>(alphaMode, true, out var alphaModeEnum)
                ? alphaModeEnum
                : AlphaMode.Opaque;
            alphaMode = null;
#pragma warning restore CS0618 // Type or member is obsolete
            return m_AlphaModeEnum.Value;
        }

        /// <summary>
        /// <see cref="AlphaMode"/> typed setter for <see cref="alphaMode"/> string.
        /// </summary>
        /// <param name="mode">Alpha mode</param>
        public void SetAlphaMode(AlphaMode mode)
        {
            m_AlphaModeEnum = mode;
#pragma warning disable CS0618 // Type or member is obsolete
            alphaMode = null;
#pragma warning restore CS0618 // Type or member is obsolete
        }

        /// <summary>
        /// Specifies the cutoff threshold when in `MASK` mode. If the alpha value is greater than
        /// or equal to this value then it is rendered as fully opaque, otherwise, it is rendered
        /// as fully transparent. This value is ignored for other modes.
        /// </summary>
        public float alphaCutoff = 0.5f;

        /// <summary>
        /// Specifies whether the material is double sided. When this value is false, back-face
        /// culling is enabled. When this value is true, back-face culling is disabled and double
        /// sided lighting is enabled. The back-face must have its normals reversed before the
        /// lighting equation is evaluated.
        /// </summary>
        public bool doubleSided;

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
        /// True if the material requires the mesh to have normals.
        /// </summary>
        public bool RequiresNormals => Extensions?.KHR_materials_unlit == null;

        /// <summary>
        /// True if the material requires the mesh to have tangents.
        /// </summary>
        public bool RequiresTangents => NormalTexture != null && NormalTexture.index >= 0;

        internal void GltfSerialize(JsonWriter writer)
        {
            writer.AddObject();
            GltfSerializeName(writer);
            if (PbrMetallicRoughness != null)
            {
                writer.AddProperty("pbrMetallicRoughness");
                PbrMetallicRoughness.GltfSerialize(writer);
            }
            if (NormalTexture != null)
            {
                writer.AddProperty("normalTexture");
                NormalTexture.GltfSerialize(writer);
            }
            if (OcclusionTexture != null)
            {
                writer.AddProperty("occlusionTexture");
                OcclusionTexture.GltfSerialize(writer);
            }
            if (EmissiveTexture != null)
            {
                writer.AddProperty("emissiveTexture");
                EmissiveTexture.GltfSerialize(writer);
            }
#pragma warning disable CS0618 // Type or member is obsolete
            if (emissiveFactor != null
                && (
                    emissiveFactor[0] > Constants.epsilon
                    || emissiveFactor[1] > Constants.epsilon
                    || emissiveFactor[2] > Constants.epsilon)
                )
            {
                writer.AddArrayProperty("emissiveFactor", emissiveFactor);
            }
#pragma warning restore CS0618 // Type or member is obsolete
            if (m_AlphaModeEnum.HasValue && m_AlphaModeEnum.Value != AlphaMode.Opaque)
            {
                writer.AddProperty("alphaMode", m_AlphaModeEnum.Value.ToString().ToUpperInvariant());
            }
            if (math.abs(alphaCutoff - .5f) > Constants.epsilon)
            {
                writer.AddProperty("alphaCutoff", alphaCutoff);
            }
            if (doubleSided)
            {
                writer.AddProperty("doubleSided", doubleSided);
            }
            if (Extensions != null)
            {
                writer.AddProperty("extensions");
                Extensions.GltfSerialize(writer);
            }
            writer.Close();
        }
    }
}
