// SPDX-FileCopyrightText: 2024 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using GLTFast.Logging;
using GLTFast.Materials;
using GLTFast.Schema;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using Color = UnityEngine.Color;
using Material = UnityEngine.Material;

namespace GLTFast.Export
{
    /// <summary>
    /// Converts Unity Materials that use the glTFast shader `glTF/Unlit` to glTF materials
    /// </summary>
    public class GltfUnlitMaterialExporter : IMaterialExport
    {
        /// <inheritdoc />
        public bool ConvertMaterial(
            Material unityMaterial,
            out GLTFast.Schema.Material material,
            IGltfWritable gltf,
            ICodeLogger logger)
        {
            gltf.RegisterExtensionUsage(Extension.MaterialsUnlit);

            material = new GLTFast.Schema.Material
            {
                Name = unityMaterial.name,
                Extensions = new MaterialExtensions
                {
                    Unlit = new MaterialUnlit()
                }
            };

            if (GltfMaterialExporter.TryGetValue(unityMaterial, MaterialProperty.Cull, out int cull))
            {
                material.DoubleSided = cull.Equals((int)CullMode.Off);
            }

            material = HandlePbrMetallicRoughness(gltf, material, unityMaterial);

            return true;
        }

        static GLTFast.Schema.Material HandlePbrMetallicRoughness(
            IGltfWritable gltf,
            GLTFast.Schema.Material material,
            Material unityMaterial)
        {
            if (GltfMaterialExporter.TryGetValue(unityMaterial, MaterialProperty.BaseColorTexture, out Texture2D texture2D))
            {
                if (MaterialExport.AddImageExport(gltf, new ImageExport(texture2D), out var textureId))
                {
                    var textureInfo = new TextureInfo
                    {
                        Index = textureId,
                        TexCoord = GltfMaterialExporter.GetValue(unityMaterial, MaterialProperty.BaseColorTextureTexCoord)
                    };

                    material.PbrMetallicRoughness ??= new PbrMetallicRoughness();
                    material.PbrMetallicRoughness.BaseColorTexture = textureInfo;

                    if (GltfMaterialExporter.TryCreateTextureTransform(
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

            if (GltfMaterialExporter.TryGetValue(unityMaterial, MaterialProperty.BaseColor, out Color baseColor)
                && baseColor != Color.white)
            {
                material.PbrMetallicRoughness ??= new PbrMetallicRoughness();
                material.PbrMetallicRoughness.BaseColorFactor = baseColor.linear;
            }

            return material;
        }
    }
}
