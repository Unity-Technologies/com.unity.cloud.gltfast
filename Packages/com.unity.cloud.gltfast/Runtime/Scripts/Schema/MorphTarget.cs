// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using Unity.Gltfast.Text.Json.Serialization;

namespace GLTFast.Schema
{
    /// <summary>
    /// Morph target (blend shape)
    /// </summary>
    public class MorphTarget
    {
        /// <summary>Vertex position deviation accessor index.</summary>
        [JsonPropertyName("POSITION")]
        public int? Position { get; set; }
        /// <summary>Vertex normal deviation accessor index.</summary>
        [JsonPropertyName("NORMAL")]
        public int? Normal { get; set; }
        /// <summary>Vertex tangent deviation accessor index.</summary>
        [JsonPropertyName("TANGENT")]
        public int? Tangent { get; set; }

        internal void GltfSerialize(JsonWriter writer)
        {
            if (Position.HasValue) writer.AddProperty("POSITION", Position.Value);
            if (Normal.HasValue) writer.AddProperty("NORMAL", Normal.Value);
            if (Tangent.HasValue) writer.AddProperty("TANGENT", Tangent.Value);
        }
    }
}
