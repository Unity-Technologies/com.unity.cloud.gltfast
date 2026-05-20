// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using GLTFast.Schema;

namespace GLTFast.Newtonsoft.Schema
{
    [Obsolete("Use GLTFast.Schema.Material instead.")]
    public class Material : MaterialBase<
        MaterialExtensions,
        NormalTextureInfo,
        OcclusionTextureInfo,
        PbrMetallicRoughness,
        TextureInfo,
        TextureInfoExtensions
    >, IJsonObject
    { }
}
