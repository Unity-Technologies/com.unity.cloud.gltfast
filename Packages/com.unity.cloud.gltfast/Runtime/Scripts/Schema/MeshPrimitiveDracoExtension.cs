// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

#if DRACO_IS_INSTALLED

using System;
using Unity.Gltfast.Text.Json.Serialization;

namespace GLTFast.Schema
{
    public class MeshPrimitiveDracoExtension
    {
        [JsonIgnore]
        public int BufferView { get; set; } = Constants.UnsetIndex;

        [JsonPropertyName("bufferView"), JsonInclude]
        internal int? BufferViewSerialized
        {
            get => BufferView < 0 ? null : BufferView;
            set => BufferView = value ?? Constants.UnsetIndex;
        }

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
