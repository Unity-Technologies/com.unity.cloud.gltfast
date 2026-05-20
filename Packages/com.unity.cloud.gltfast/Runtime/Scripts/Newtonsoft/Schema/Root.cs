// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using GLTFast.Schema;

namespace GLTFast.Newtonsoft.Schema
{
    [Obsolete("Use GLTFast.Schema.Root instead.")]
    public class Root : RootBase<
        Accessor,
        Animation,
        Asset,
        Buffer,
        BufferView,
        Camera,
        RootExtensions,
        Image,
        Material,
        Mesh,
        Node,
        Sampler,
        Scene,
        Skin,
        Texture
    >, IJsonObject
    { }
}
