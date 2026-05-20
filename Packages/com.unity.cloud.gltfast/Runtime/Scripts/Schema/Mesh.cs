// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using Unity.Gltfast.Text.Json;
using Unity.Gltfast.Text.Json.Serialization;

namespace GLTFast.Schema
{
    /// <inheritdoc />
    [Serializable]
    public class Mesh : MeshBase<MeshExtras, MeshPrimitive> { }

    /// <inheritdoc cref="MeshBase"/>
    /// <typeparam name="TExtras">extras type</typeparam>
    /// <typeparam name="TPrimitive">Mesh primitive type</typeparam>
    [Serializable]
    public abstract class MeshBase<TExtras, TPrimitive> : MeshBase, ICloneable
        where TPrimitive : MeshPrimitiveBase
        where TExtras : MeshExtras
    {
        /// <inheritdoc cref="Extras"/>
        public TExtras extras;

        /// <inheritdoc cref="Primitives"/>
        public TPrimitive[] primitives;

        /// <inheritdoc />
        public override MeshExtras Extras => extras;

        /// <inheritdoc />
        public override IReadOnlyList<MeshPrimitiveBase> Primitives => primitives;

        /// <summary>
        /// Clones the Mesh object
        /// </summary>
        /// <returns>Member-wise clone</returns>
        public object Clone()
        {
            var clone = (MeshBase<TExtras, TPrimitive>)MemberwiseClone();
            if (Primitives != null)
            {
                clone.primitives = new TPrimitive[primitives.Length];
                for (var i = 0; i < primitives.Length; i++)
                {
                    clone.primitives[i] = (TPrimitive)primitives[i].Clone();
                }
            }
            return clone;
        }
    }

    /// <summary>
    /// A set of primitives to be rendered. Its global transform is defined by
    /// a node that references it.
    /// </summary>
    [Serializable]
    public abstract class MeshBase : NamedObject, IGltfObject
    {
        /// <summary>
        /// An array of primitives, each defining geometry to be rendered with
        /// a material.
        /// </summary>
        public abstract IReadOnlyList<MeshPrimitiveBase> Primitives { get; }

        /// <summary>
        /// Array of weights to be applied to the Morph Targets.
        /// </summary>
        public float[] weights;

        /// <inheritdoc cref="Asset.extensions"/>
        public UnclassifiedData extensions;

        /// <inheritdoc cref="MeshExtras"/>
        public abstract MeshExtras Extras { get; }

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
