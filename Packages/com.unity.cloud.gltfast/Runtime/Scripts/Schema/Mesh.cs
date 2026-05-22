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
    [Serializable]
    public class Mesh : NamedObject, IGltfObject, ICloneable
    {
        /// <inheritdoc cref="Extras"/>
        public MeshExtras extras;

        /// <inheritdoc cref="Primitives"/>
        public MeshPrimitive[] primitives;

        /// <summary>
        /// An array of primitives, each defining geometry to be rendered with
        /// a material.
        /// </summary>
        public IReadOnlyList<MeshPrimitive> Primitives => primitives;

        /// <inheritdoc cref="MeshExtras"/>
        public MeshExtras Extras => extras;

        /// <summary>
        /// Clones the Mesh object
        /// </summary>
        /// <returns>Member-wise clone</returns>
        public object Clone()
        {
            var clone = (Mesh)MemberwiseClone();
            if (Primitives != null)
            {
                clone.primitives = new MeshPrimitive[primitives.Length];
                for (var i = 0; i < primitives.Length; i++)
                {
                    clone.primitives[i] = (MeshPrimitive)primitives[i].Clone();
                }
            }
            return clone;
        }

        /// <summary>
        /// Array of weights to be applied to the Morph Targets.
        /// </summary>
        public float[] weights;

        /// <inheritdoc cref="Asset.extensions"/>
        public UnclassifiedData extensions;

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

            if (weights != null)
            {
                writer.AddArrayProperty("weights", weights);
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
    [Serializable]
    public class MeshExtras : IGltfObject
    {
        /// <summary>
        /// Morph targets' names
        /// </summary>
        public string[] targetNames;

        /// <summary>JSON properties without a matching member.</summary>
        [JsonExtensionData, JsonInclude] internal Dictionary<string, JsonElement> ExtensionsData { get; set; }

        /// <inheritdoc/>
        public bool TryGetValue<T>(string key, out T value)
        {
            return ExtensionsData.TryGetValue(key, out value);
        }

        internal void GltfSerialize(JsonWriter writer)
        {
            if (targetNames != null)
            {
                writer.AddArrayPropertySafe("targetNames", targetNames);
            }
        }
    }
}
