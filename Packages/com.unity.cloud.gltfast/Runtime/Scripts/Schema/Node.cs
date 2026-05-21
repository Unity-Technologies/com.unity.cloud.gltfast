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
    /// <inheritdoc />
    [System.Serializable]
    public class Node : NodeBase<NodeExtensions> { }

    /// <inheritdoc />
    /// <typeparam name="TExtensions">Node extensions type</typeparam>
    [System.Serializable]
    public abstract class NodeBase<TExtensions> : NodeBase
        where TExtensions : NodeExtensions
    {
        /// <inheritdoc cref="Extensions"/>
        public TExtensions extensions;

        /// <inheritdoc />
        public override NodeExtensions Extensions => extensions;
    }

    /// <summary>
    /// An object defining the hierarchy relations and the local transform of
    /// its content.
    /// </summary>
    [System.Serializable]
    public abstract class NodeBase : NamedObject, IGltfObject
    {
        /// <summary>
        /// The indices of this node's children.
        /// </summary>
        public uint[] children;

        /// <summary>
        /// The index of the mesh in this node.
        /// </summary>
        public int mesh = -1;

        /// <summary>
        /// A floating-point 4x4 transformation matrix stored in column-major order.
        /// </summary>
        [JsonConverter(typeof(Float16ArrayConverter))]
        public float[] matrix;

        /// <summary>
        /// The node's unit quaternion rotation in the order (x, y, z, w),
        /// where w is the scalar.
        /// </summary>
        [JsonConverter(typeof(Float4ArrayConverter))]
        public float[] rotation;

        /// <summary>
        /// The node's non-uniform scale.
        /// </summary>
        [JsonConverter(typeof(Float3ArrayConverter))]
        public float[] scale;

        /// <summary>
        /// The node's translation.
        /// </summary>
        [JsonConverter(typeof(Float3ArrayConverter))]
        public float[] translation;

        /// <summary>
        /// The weights of the instantiated Morph Target.
        /// Number of elements must match number of Morph Targets of used mesh.
        /// </summary>
        public float[] weights;

        /// <summary>
        /// The index of the skin (in <see cref="RootBase.Skins"/> referenced by this node.
        /// </summary>
        public int skin = -1;

        /// <summary>
        /// Camera index
        /// </summary>
        public int camera = -1;

        /// <inheritdoc cref="NodeExtensions"/>
        public abstract NodeExtensions Extensions { get; }

        /// <summary>
        /// Application-specific data.
        /// </summary>
        /// <seealso href="https://registry.khronos.org/glTF/specs/2.0/glTF-2.0.html#reference-extras"/>
        public UnclassifiedData extras { get; set; }

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

            if (children != null)
            {
                writer.AddArrayProperty("children", children);
            }

            if (mesh >= 0)
            {
                writer.AddProperty("mesh", mesh);
            }

            if (translation != null)
            {
                writer.AddArrayProperty("translation", translation);
            }

            if (rotation != null)
            {
                writer.AddArrayProperty("rotation", rotation);
            }

            if (scale != null)
            {
                writer.AddArrayProperty("scale", scale);
            }

            if (matrix != null)
            {
                writer.AddArrayProperty("matrix", matrix);
            }

            if (weights != null)
            {
                writer.AddArrayProperty("weights", weights);
            }

            if (skin >= 0)
            {
                writer.AddProperty("skin", skin);
            }

            if (camera >= 0)
            {
                writer.AddProperty("camera", camera);
            }

            if (Extensions != null)
            {
                writer.AddProperty("extensions");
                Extensions.GltfSerialize(writer);
            }
            writer.Close();
        }

        /// <inheritdoc cref="Root.JsonUtilityCleanup"/>
        [Obsolete("Has become obsolete after the transition from JsonUtility to System.Text.Json.")]
        public virtual void JsonUtilityCleanup() { }
    }

    /// <summary>
    /// Node extensions
    /// </summary>
    [System.Serializable]
    public class NodeExtensions : IGltfObject
    {
        // Names are identical to glTF specified properties, that's why
        // inconsistent names are ignored.
        // ReSharper disable InconsistentNaming

        /// <inheritdoc cref="MeshGpuInstancing"/>
        public MeshGpuInstancing EXT_mesh_gpu_instancing;
        /// <inheritdoc cref="LightsPunctual"/>
        public NodeLightsPunctual KHR_lights_punctual;

        // Whenever an extension is added, the JsonParser
        // (specifically step four of JsonParser.ParseJson)
        // needs to be updated!

        /// <summary>JSON properties without a matching member.</summary>
        [JsonExtensionData, JsonInclude] internal Dictionary<string, JsonElement> ExtensionsData { get; set; }

        /// <inheritdoc/>
        public bool TryGetValue<T>(string key, out T value)
        {
            return ExtensionsData.TryGetValue(key, out value);
        }

        // ReSharper restore InconsistentNaming
        internal void GltfSerialize(JsonWriter writer)
        {
            writer.AddObject();
            if (EXT_mesh_gpu_instancing != null)
            {
                writer.AddProperty("EXT_mesh_gpu_instancing");
                EXT_mesh_gpu_instancing.GltfSerialize(writer);
            }
            if (KHR_lights_punctual != null)
            {
                writer.AddProperty("KHR_lights_punctual");
                KHR_lights_punctual.GltfSerialize(writer);
            }
            writer.Close();
        }
    }
}
