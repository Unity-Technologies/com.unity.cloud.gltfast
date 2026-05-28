// SPDX-FileCopyrightText: 2024 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

#if UNITY_SHADER_GRAPH

using System;
using GLTFast.Materials;
using UnityEngine;
using UnityEngine.Rendering;
using GltfMaterial = GLTFast.Schema.Material;
using Material = UnityEngine.Material;

namespace GLTFast.Export
{
    /// <summary>
    /// Converts Unity Materials that use a glTFast Built-In shader to glTF materials
    /// </summary>
    public class GltfShaderGraphMaterialExporter : GltfMaterialExporter
    {
        protected override bool IsDoubleSided(Material material)
        {
            if (TryGetValue(material, MaterialProperty.Cull, out int cull))
            {
                return cull == (int)CullMode.Off;
            }
            return false;
        }

        protected override GltfMaterial.AlphaMode GetAlphaMode(Material material)
        {
            if (TryGetValue(material, MaterialProperty.AlphaClip, out int alphaClip)
                && alphaClip == 1)
            {
                return GltfMaterial.AlphaMode.Mask;
            }
            if (TryGetValue(material, MaterialProperty.Surface, out int surface))
            {
                return surface == 0
                    ? GltfMaterial.AlphaMode.Opaque
                    : GltfMaterial.AlphaMode.Blend;
            }
            return GltfMaterial.AlphaMode.Opaque;
        }

        protected override float GetAlphaCutoff(Material material)
        {
            return material.GetFloat(MaterialProperty.AlphaCutoff);
        }
    }
}
#endif // UNITY_SHADER_GRAPH
