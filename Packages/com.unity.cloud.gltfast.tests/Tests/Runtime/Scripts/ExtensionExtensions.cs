// SPDX-FileCopyrightText: 2024 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using UnityEngine;

namespace GLTFast.Tests
{
    static class ExtensionExtensions
    {
        internal static Extension? FromName(string extensionName)
        {
            return extensionName switch
            {
                ExtensionName.DracoMeshCompression => Extension.DracoMeshCompression,
                ExtensionName.LightsPunctual => Extension.LightsPunctual,
                ExtensionName.MaterialsPbrSpecularGlossiness => Extension.MaterialsPbrSpecularGlossiness,
                ExtensionName.MaterialsTransmission => Extension.MaterialsTransmission,
                ExtensionName.MaterialsUnlit => Extension.MaterialsUnlit,
                ExtensionName.MeshGPUInstancing => Extension.MeshGPUInstancing,
                ExtensionName.MeshQuantization => Extension.MeshQuantization,
                ExtensionName.TextureBasisUniversal => Extension.TextureBasisUniversal,
                ExtensionName.TextureTransform => Extension.TextureTransform,
                ExtensionName.TextureWebP => Extension.TextureWebP,
                ExtensionName.MaterialsClearcoat => Extension.MaterialsClearcoat,
                ExtensionName.MaterialsVariants => Extension.MaterialsVariants,
                ExtensionName.MeshoptCompression => Extension.MeshoptCompression,
                ExtensionName.MaterialsIor => Extension.MaterialsIor,
                ExtensionName.MaterialsSpecular => Extension.MaterialsSpecular,
                ExtensionName.MaterialsSheen => Extension.MaterialsSheen,
                _ => null
            };
        }
    }
}
