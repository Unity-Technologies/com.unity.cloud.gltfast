// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using GLTFast.Schema;

namespace GLTFast.Newtonsoft.Schema
{
    [Obsolete("Use GLTFast.Schema.MeshPrimitive instead.")]
    public class MeshPrimitive : MeshPrimitiveBase<MeshPrimitiveExtensions>, IJsonObject { }
}
