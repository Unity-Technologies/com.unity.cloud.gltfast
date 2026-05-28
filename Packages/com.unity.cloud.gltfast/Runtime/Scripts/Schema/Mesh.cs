// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
#if UNITY_6000_5_OR_NEWER
using System.Text.Json;
using System.Text.Json.Serialization;
#else
using Unity.Gltfast.Text.Json;
using Unity.Gltfast.Text.Json.Serialization;
#endif

namespace GLTFast.Schema
{
    /// <summary>
    /// A set of primitives to be rendered. Its global transform is defined by
    /// a node that references it.
    /// </summary>
    public class Mesh : NamedObject, IGltfObject, ICloneable
    {
        /// <summary>
        /// An array of primitives, each defining geometry to be rendered with
        /// a material.
        /// </summary>
        [JsonPropertyName("primitives")]
        public List<MeshPrimitive> Primitives { get; set; }

        /// <inheritdoc cref="MeshExtras"/>
        [JsonPropertyName("extras")]
        public MeshExtras Extras { get; set; }

        /// <summary>
        /// Clones the Mesh object
        /// </summary>
        /// <returns>Member-wise clone</returns>
        public object Clone()
        {
            var clone = (Mesh)MemberwiseClone();
            if (Primitives != null)
            {
                clone.Primitives = new List<MeshPrimitive>(Primitives.Count);
                for (var i = 0; i < Primitives.Count; i++)
                {
                    clone.Primitives.Add((MeshPrimitive)Primitives[i].Clone());
                }
            }
            return clone;
        }

        /// <summary>
        /// Array of weights to be applied to the Morph Targets.
        /// </summary>
        [JsonPropertyName("weights")]
        public float[] Weights { get; set; }

        /// <inheritdoc cref="Asset.Extensions"/>
        [JsonPropertyName("extensions")]
        public UnclassifiedData Extensions { get; set; }

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
            if (Primitives != null)
            {
                writer.AddArray("primitives");
                foreach (var primitive in Primitives)
                {
                    primitive.GltfSerialize(writer);
                }
                writer.CloseArray();
            }

            if (Weights != null)
            {
                writer.AddArrayProperty("weights", Weights);
            }

            if (Extras != null)
            {
                writer.AddProperty("extras");
                Extras.GltfSerialize(writer);
                writer.Close();
            }
            writer.Close();
        }
    }

    /// <summary>
    /// Application-specific data for meshes
    /// </summary>
    public class MeshExtras : IGltfObject
    {
        /// <summary>
        /// Morph targets' names
        /// </summary>
        [JsonPropertyName("targetNames")]
        public string[] TargetNames { get; set; }

        /// <summary>JSON properties without a matching member.</summary>
        [JsonExtensionData, JsonInclude] internal Dictionary<string, JsonElement> ExtensionsData { get; set; }

        /// <inheritdoc/>
        public bool TryGetValue<T>(string key, out T value)
        {
            return ExtensionsData.TryGetValue(key, out value);
        }

        internal void GltfSerialize(JsonWriter writer)
        {
            if (TargetNames != null)
            {
                writer.AddArrayPropertySafe("targetNames", TargetNames);
            }
        }
    }
}
