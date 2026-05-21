using System;
#if UNITY_6000_5_OR_NEWER
using System.Text.Json.Serialization;
#else
using Unity.Gltfast.Text.Json.Serialization;
#endif
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
