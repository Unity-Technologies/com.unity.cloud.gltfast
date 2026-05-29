// SPDX-FileCopyrightText: 2024 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using GLTFast.Logging;
using GLTFast.Materials;
using GLTFast.Schema;
using Unity.Mathematics;
using UnityEngine;
using Material = UnityEngine.Material;
using GltfMaterial = GLTFast.Schema.Material;

namespace GLTFast.Export
{
    /// <summary>
    /// Converts Unity Materials that use a glTFast shader to glTF materials
    /// </summary>
    public abstract class GltfMaterialExporter : MaterialExportBase
    {
        /// <inheritdoc />
        public override bool ConvertMaterial(
            Material unityMaterial,
            out GLTFast.Schema.Material material,
            IGltfWritable gltf,
            ICodeLogger logger)
        {
            material = new GLTFast.Schema.Material
            {
                Name = unityMaterial.name,
                PbrMetallicRoughness = new PbrMetallicRoughness(),
                doubleSided = IsDoubleSided(unityMaterial)
            };

            var alphaMode = GetAlphaMode(unityMaterial);
            material.SetAlphaMode(alphaMode);
            if (alphaMode == GltfMaterial.AlphaMode.Mask)
            {
                material.alphaCutoff = GetAlphaCutoff(unityMaterial);
            }

            material = HandlePbrMetallicRoughness(gltf, material, unityMaterial);
            material = HandleNormal(gltf, material, unityMaterial);
            material = HandleOcclusion(gltf, material, unityMaterial);
            material = HandleEmission(gltf, material, unityMaterial);

            return true;
        }

        /// <summary>
        /// Extracts the glTF alpha mode from a Unity material.
        /// </summary>
        /// <seealso href="https://registry.khronos.org/glTF/specs/2.0/glTF-2.0.html#_material_alphamode"/>
        /// <param name="material">Unity material.</param>
        /// <returns>glTF alpha mode.</returns>
        protected abstract GltfMaterial.AlphaMode GetAlphaMode(Material material);

        /// <summary>
        /// Returns that material's alpha cutoff threshold.
        /// </summary>
        /// <param name="material">Unity material.</param>
        /// <seealso href="https://registry.khronos.org/glTF/specs/2.0/glTF-2.0.html#_material_alphacutoff"/>
        /// <returns>Alpha cutoff threshold value.</returns>
        protected abstract float GetAlphaCutoff(Material material);

        /// <summary>
        /// Indicates whether (back-face) culling should be disabled.
        /// </summary>
        /// <seealso href="https://registry.khronos.org/glTF/specs/2.0/glTF-2.0.html#_material_doublesided"/>
        /// <param name="material">Unity material.</param>
        /// <returns>True if material does not do back-face culling. False otherwise.</returns>
        protected abstract bool IsDoubleSided(Material material);

        static GLTFast.Schema.Material HandlePbrMetallicRoughness(
            IGltfWritable gltf,
            GLTFast.Schema.Material material,
            Material unityMaterial)
        {
            if (TryGetValue(unityMaterial, MaterialProperty.BaseColorTexture, out Texture2D texture2D))
            {
                if (MaterialExport.AddImageExport(gltf, new ImageExport(texture2D), out var textureId))
                {
                    var textureInfo = new TextureInfo
                    {
                        Index = textureId,
                        TexCoord = GetValue(unityMaterial, MaterialProperty.BaseColorTextureTexCoord)
                    };

                    material.PbrMetallicRoughness.BaseColorTexture = textureInfo;

                    if (TryCreateTextureTransform(
                            gltf,
                            unityMaterial,
                            MaterialProperty.BaseColorTextureScaleTransform,
                            MaterialProperty.BaseColorTextureRotation,
                            out var textureTransform
                        ))
                    {
                        material.PbrMetallicRoughness.BaseColorTexture.Extensions = new TextureInfoExtensions
                        {
                            TextureTransform = textureTransform
                        };
                    }
                }
            }

            if (TryGetValue(unityMaterial, MaterialProperty.BaseColor, out Color baseColor))
            {
                material.PbrMetallicRoughness.BaseColor = baseColor.linear;
            }

            material = HandleMetallicRoughness(gltf, material, unityMaterial);

            return material;
        }

        static GLTFast.Schema.Material HandleMetallicRoughness(
            IGltfWritable gltf,
            GLTFast.Schema.Material material,
            Material unityMaterial)
        {
            if (TryGetValue(unityMaterial, MaterialProperty.MetallicRoughnessMap, out Texture2D texture2D)
                && MaterialExport.AddImageExport(gltf, new ImageExport(texture2D), out var textureId))
            {
                var textureInfo = new TextureInfo
                {
                    Index = textureId,
                    TexCoord = GetValue(unityMaterial, MaterialProperty.MetallicRoughnessMapTexCoord)
                };

                if (TryCreateTextureTransform(
                        gltf,
                        unityMaterial,
                        MaterialProperty.MetallicRoughnessMapScaleTransform,
                        MaterialProperty.MetallicRoughnessMapRotation,
                        out var textureTransform)
                    )
                {
                    textureInfo.Extensions = new TextureInfoExtensions
                    {
                        TextureTransform = textureTransform
                    };
                }

                material.PbrMetallicRoughness.MetallicRoughnessTexture = textureInfo;
            }

            if (TryGetValue(unityMaterial, MaterialProperty.Metallic, out float metallicFactor))
            {
                material.PbrMetallicRoughness.metallicFactor = metallicFactor;
            }

            if (TryGetValue(unityMaterial, MaterialProperty.RoughnessFactor, out float roughnessFactor))
            {
                material.PbrMetallicRoughness.roughnessFactor = roughnessFactor;
            }

            return material;
        }

        static GLTFast.Schema.Material HandleNormal(
            IGltfWritable gltf,
            GLTFast.Schema.Material material,
            Material unityMaterial)
        {
            if (!TryGetValue(unityMaterial, MaterialProperty.NormalTexture, out Texture2D texture2D))
            {
                return material;
            }

            if (!MaterialExport.AddImageExport(gltf, new NormalImageExport(texture2D), out var textureId))
            {
                return material;
            }

            TryGetValue(unityMaterial, MaterialProperty.NormalTextureScale, out float normalScale);
            var textureInfo = new NormalTextureInfo
            {
                Index = textureId,
                TexCoord = GetValue(unityMaterial, MaterialProperty.NormalTextureTexCoord),
                Scale = normalScale
            };

            material.NormalTexture = textureInfo;

            if (TryCreateTextureTransform(
                    gltf,
                    unityMaterial,
                    MaterialProperty.NormalTextureScaleTransform,
                    MaterialProperty.NormalTextureRotation,
                    out var textureTransform
                ))
            {
                material.NormalTexture.Extensions = new TextureInfoExtensions
                {
                    TextureTransform = textureTransform
                };
            }

            return material;
        }

        static GLTFast.Schema.Material HandleOcclusion(
            IGltfWritable gltf,
            GLTFast.Schema.Material material,
            Material unityMaterial)
        {
            if (!TryGetValue(unityMaterial, MaterialProperty.OcclusionTexture, out Texture2D texture2D))
            {
                return material;
            }

            if (!MaterialExport.AddImageExport(gltf, new ImageExport(texture2D), out var textureId))
            {
                return material;
            }

            TryGetValue(unityMaterial, MaterialProperty.OcclusionTextureStrength, out float occlusionStrength);
            var info = new OcclusionTextureInfo
            {
                Index = textureId,
                TexCoord = GetValue(unityMaterial, MaterialProperty.OcclusionTextureTexCoord),
                Strength = occlusionStrength
            };

            material.OcclusionTexture = info;

            if (TryCreateTextureTransform(
                    gltf,
                    unityMaterial,
                    MaterialProperty.OcclusionTextureScaleTransform,
                    MaterialProperty.OcclusionTextureRotation,
                    out var textureTransform
                ))
            {
                material.OcclusionTexture.Extensions = new TextureInfoExtensions
                {
                    TextureTransform = textureTransform
                };
            }

            return material;
        }

        static GLTFast.Schema.Material HandleEmission(
            IGltfWritable gltf,
            GLTFast.Schema.Material material,
            Material unityMaterial)
        {
            if (TryGetValue(unityMaterial, MaterialProperty.EmissiveTexture, out Texture2D texture2D))
            {
                if (MaterialExport.AddImageExport(gltf, new ImageExport(texture2D), out var textureId))
                {
                    var info = new TextureInfo
                    {
                        Index = textureId,
                        TexCoord = GetValue(unityMaterial, MaterialProperty.EmissiveTextureTexCoord)
                    };

                    material.EmissiveTexture = info;

                    if (TryCreateTextureTransform(
                            gltf,
                            unityMaterial,
                            MaterialProperty.EmissiveTextureScaleTransform,
                            MaterialProperty.EmissiveTextureRotation,
                            out var textureTransform
                        ))
                    {
                        material.EmissiveTexture.Extensions = new TextureInfoExtensions
                        {
                            TextureTransform = textureTransform
                        };
                    }
                }
            }

            if (TryGetValue(unityMaterial, MaterialProperty.EmissiveFactor, out Color emissiveFactor))
            {
                material.Emissive = emissiveFactor;
            }

            return material;
        }

        internal static bool TryCreateTextureTransform(
            IGltfWritable gltf,
            Material uMaterial,
            int scaleTransformPropertyId,
            int rotationPropertyId,
            out TextureTransform result
            )
        {
            result = null;
            if (!uMaterial.IsKeywordEnabled("_TEXTURE_TRANSFORM"))
            {
                return false;
            }

            var st = uMaterial.GetVector(scaleTransformPropertyId);
            var r = uMaterial.GetVector(rotationPropertyId);

            if (math.abs(st.z) >= float.Epsilon || math.abs(st.w) >= float.Epsilon)
            {
                result ??= new TextureTransform();
                result.Offset = new[] { st.z, st.w };
            }

            var uvTransform = UvTransform.FromMatrix(new float2x2(st.x, st.y, r.x, r.y));

            if (math.abs(uvTransform.rotation) > float.Epsilon)
            {
                result ??= new TextureTransform();
                result.Rotation = uvTransform.rotation;
            }

            if (math.abs(uvTransform.scale.x - 1) > math.EPSILON
                || math.abs(uvTransform.scale.y - 1) > math.EPSILON)
            {
                result ??= new TextureTransform();
                result.Scale = new[] { uvTransform.scale[0], uvTransform.scale[1] };
            }

            if (result != null)
            {
                gltf.RegisterExtensionUsage(Extension.TextureTransform);
                return true;
            }

            return false;
        }

        internal static bool TryGetValue(Material material, int propertyId, out float value)
        {
            if (!material.HasProperty(propertyId))
            {
                value = default;
                return false;
            }
            value = material.GetFloat(propertyId);
            return true;
        }

        internal static bool TryGetValue(Material material, int propertyId, out int value)
        {
            if (TryGetValue(material, propertyId, out float floatValue))
            {
                value = (int)floatValue;
                return true;
            }

            value = default;
            return false;
        }

        internal static int GetValue(Material material, int propertyId)
        {
            return !TryGetValue(material, propertyId, out int value) ? default : value;
        }

        internal static bool TryGetValue(Material material, int propertyId, out Color value)
        {
            if (!material.HasProperty(propertyId))
            {
                value = default;
                return false;
            }
            value = material.GetColor(propertyId);
            return true;
        }

        internal static bool TryGetValue(Material material, int propertyId, out Texture2D value)
        {
            if (!material.HasProperty(propertyId))
            {
                value = default;
                return false;
            }
            value = (Texture2D)material.GetTexture(propertyId);
            return !(value is null);
        }
    }
}
