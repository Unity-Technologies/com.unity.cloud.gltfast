// SPDX-FileCopyrightText: 2026 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using Unity.Gltfast.Text.Json.Serialization;
using UnityEngine;

namespace GLTFast.Schema
{
    [JsonSourceGenerationOptions(
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
        // // Potentially a lot quicker, but not supported yet!
        // , GenerationMode = JsonSourceGenerationMode.Serialization
        )]
    [JsonSerializable(typeof(Root))]
    [JsonSerializable(typeof(Extension))]
    partial class GltfRootSourceGenerator : JsonSerializerContext { }
}
