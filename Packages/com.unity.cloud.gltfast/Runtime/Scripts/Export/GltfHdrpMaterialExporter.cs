// SPDX-FileCopyrightText: 2024 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

#if USING_HDRP

using System;
using GLTFast.Materials;
using GLTFast.Schema;
using UnityEngine;
using GltfMaterial = GLTFast.Schema.Material;
using Material = UnityEngine.Material;

namespace GLTFast.Export
{
    /// <summary>
    /// Converts Unity Materials that use a glTFast Built-In shader to glTF materials
    /// </summary>
    public class GltfHdrpMaterialExporter : GltfShaderGraphMaterialExporter
    {
        protected override bool IsDoubleSided(Material material)
        {
            if (TryGetValue(material, MaterialProperty.DoubleSidedEnable, out int doubleSided))
            {
                return doubleSided != 0;
            }
            return false;
        }

        protected override AlphaMode GetAlphaMode(Material material)
        {
            if (TryGetValue(material, MaterialProperty.AlphaCutoffEnable, out int alphaClip)
                && alphaClip == 1)
            {
                return AlphaMode.Mask;
            }

            if (TryGetValue(material, MaterialProperty.SurfaceType, out int surface))
            {
                return surface == 0
                    ? AlphaMode.Opaque
                    : AlphaMode.Blend;
            }

            return AlphaMode.Opaque;
        }
    }
}
#endif // USING_HDRP
