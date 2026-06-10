// SPDX-FileCopyrightText: 2024 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using GLTFast.Materials;
using GLTFast.Schema;
using UnityEngine;
using UnityEngine.Rendering;
using GltfMaterial = GLTFast.Schema.Material;
using Material = UnityEngine.Material;

namespace GLTFast.Export
{
    /// <summary>
    /// Converts Unity Materials that use a glTFast Built-In shader to glTF materials
    /// </summary>
    public class GltfBuiltInShaderMaterialExporter : GltfMaterialExporter
    {
        /// <inheritdoc />
        protected override AlphaMode GetAlphaMode(Material material)
        {
            if (TryGetValue(material, MaterialProperty.Mode, out int modeInt))
            {
                var mode = (StandardShaderMode)modeInt;
                switch (mode)
                {
                    case StandardShaderMode.Cutout:
                        return AlphaMode.Mask;
                    case StandardShaderMode.Fade:
                    case StandardShaderMode.Transparent:
                        return AlphaMode.Blend;
                }
            }
            return AlphaMode.Opaque;
        }

        /// <inheritdoc />
        protected override float GetAlphaCutoff(Material material)
        {
            return material.GetFloat(MaterialProperty.AlphaCutoff);
        }

        /// <inheritdoc />
        protected override bool IsDoubleSided(Material material)
        {
            if (TryGetValue(material, MaterialProperty.CullMode, out int cull))
            {
                return cull == (int)CullMode.Off;
            }
            return false;
        }
    }
}
