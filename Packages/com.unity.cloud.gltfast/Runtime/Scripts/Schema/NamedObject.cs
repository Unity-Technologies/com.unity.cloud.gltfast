// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

#if UNITY_6000_5_OR_NEWER
using System.Text.Json.Serialization;
#else
using Unity.Gltfast.Text.Json.Serialization;
#endif

namespace GLTFast.Schema
{

    /// <summary>
    /// Base class for anything with a name property
    /// </summary>
    public abstract class NamedObject
    {

        /// <summary>
        /// Object's name
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; }

        internal void GltfSerializeName(JsonWriter writer)
        {
            if (!string.IsNullOrEmpty(Name))
            {
                writer.AddPropertySafe("name", Name);
            }
        }
    }
}
