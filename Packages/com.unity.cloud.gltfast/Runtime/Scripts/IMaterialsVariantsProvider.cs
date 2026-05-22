// SPDX-FileCopyrightText: 2024 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using GLTFast.Schema;

namespace GLTFast
{
    /// <summary>
    /// Provides access to glTF materials variants.
    /// </summary>
    public interface IMaterialsVariantsProvider
    {
        /// <inheritdoc cref="Root.MaterialsVariantsCount"/>
        int MaterialsVariantsCount { get; }

        /// <inheritdoc cref="Root.GetMaterialsVariantName"/>
        string GetMaterialsVariantName(int index);
    }
}
