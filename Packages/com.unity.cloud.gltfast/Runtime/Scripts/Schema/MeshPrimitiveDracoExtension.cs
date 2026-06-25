// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

#if DRACO_IS_INSTALLED

using System;
using Unity.Gltfast.Text.Json.Serialization;

namespace GLTFast.Schema
{
    public class MeshPrimitiveDracoExtension
    {
        [JsonPropertyName("bufferView")]
        public int BufferView { get; set; }
        [JsonPropertyName("attributes")]
        public Attributes Attributes { get; set; }

        internal void GltfSerialize(JsonWriter writer)
        {
            writer.AddObject();
            writer.AddProperty("bufferView", BufferView);
            writer.AddProperty("attributes");
            Attributes.GltfSerialize(writer);
            writer.Close();
        }
    }
}
#endif // DRACO_IS_INSTALLED
