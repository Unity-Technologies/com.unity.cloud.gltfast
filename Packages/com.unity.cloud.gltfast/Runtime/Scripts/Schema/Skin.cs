// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using Unity.Gltfast.Text.Json;
using Unity.Gltfast.Text.Json.Serialization;

namespace GLTFast.Schema
{

    /// <summary>
    /// Joints and matrices defining a skinned mesh.
    /// </summary>
    [System.Serializable]
    public class Skin : NamedObject, IGltfObject
    {
        /// <summary>
        /// The index of the accessor containing the
        /// floating-point 4x4 inverse-bind matrices.
        /// </summary>
        public int inverseBindMatrices = -1;

        /// <summary>
        /// The index of the node used as a skeleton root.
        /// </summary>
        public int skeleton = -1;

        /// <summary>
        /// Indices of skeleton nodes, used as joints in this skin.
        /// </summary>
        public uint[] joints;

        /// <inheritdoc cref="Asset.extensions"/>
        public UnclassifiedData extensions;

        /// <inheritdoc cref="Root.extras"/>
        public UnclassifiedData extras;

        /// <summary>JSON properties without a matching member.</summary>
        [JsonExtensionData, JsonInclude] internal Dictionary<string, JsonElement> ExtensionsData { get; set; }

        /// <inheritdoc/>
        public bool TryGetValue<T>(string key, out T value)
        {
            return ExtensionsData.TryGetValue(key, out value);
        }

        internal void GltfSerialize(JsonWriter writer)
        {
            writer.AddObject();
            GltfSerializeName(writer);

            if (inverseBindMatrices != -1)
            {
                writer.AddProperty("inverseBindMatrices", inverseBindMatrices);
            }

            if (skeleton != -1)
            {
                writer.AddProperty("skeleton", skeleton);
            }

            if (joints != null)
            {
                writer.AddArrayProperty("joints", joints);
            }

            writer.Close();
        }
    }
}
