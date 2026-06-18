// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using Unity.Gltfast.Text.Json.Serialization;

namespace GLTFast.Schema
{

    /// <summary>
    /// Assigns a light to a node
    /// </summary>
    public class NodeLightsPunctual
    {

        /// <summary>
        /// Light index
        /// </summary>
        [JsonPropertyName("light")]
        public int? Light { get; set; }

        internal void GltfSerialize(JsonWriter writer)
        {
            writer.AddObject();
            if (Light.HasValue)
            {
                writer.AddProperty("light", Light.Value);
            }
            writer.Close();
        }
    }
}
