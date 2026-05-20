// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using GLTFast.Schema;

namespace GLTFast.Newtonsoft.Schema
{
    [Obsolete("Use GLTFast.Schema.Mesh instead.")]
    public class Mesh : MeshBase<MeshExtras, MeshPrimitive>, IJsonObject { }
}
