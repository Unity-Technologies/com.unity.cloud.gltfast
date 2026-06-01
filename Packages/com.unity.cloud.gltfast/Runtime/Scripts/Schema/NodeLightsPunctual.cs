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
        public int Light { get; set; } = -1;

        internal void GltfSerialize(JsonWriter writer)
        {
            writer.AddObject();
            if (Light >= 0)
            {
                writer.AddProperty("light", Light);
            }
            writer.Close();
        }
    }
}
