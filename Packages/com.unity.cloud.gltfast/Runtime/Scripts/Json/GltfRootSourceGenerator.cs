using System;
using Unity.Gltfast.Text.Json.Serialization;
using UnityEngine;

namespace GLTFast.Schema
{
    [JsonSourceGenerationOptions(
        IncludeFields = true

        // // Potentially a lot quicker, but not supported yet!
        // , GenerationMode = JsonSourceGenerationMode.Serialization
        )]
    [JsonSerializable(typeof(Root))]
    [JsonSerializable(typeof(MeshGpuInstancing.Attributes), TypeInfoPropertyName = "MeshGpuInstancingAttributes")]
    partial class GltfRootSourceGenerator : JsonSerializerContext { }
}
