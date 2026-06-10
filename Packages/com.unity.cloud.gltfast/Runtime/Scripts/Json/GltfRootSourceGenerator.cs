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
    partial class GltfRootSourceGenerator : JsonSerializerContext { }
}
