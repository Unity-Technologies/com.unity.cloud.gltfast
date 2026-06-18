// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using Unity.Gltfast.Text.Json;
using Unity.Gltfast.Text.Json.Serialization;

namespace GLTFast.Schema
{
    /// <summary>
    /// An object defining the hierarchy relations and the local transform of
    /// its content.
    /// </summary>
    public class Node : NamedObject, IGltfObject
    {
        /// <inheritdoc cref="NodeExtensions"/>
        [JsonPropertyName("extensions")]
        public NodeExtensions Extensions { get; set; }

        /// <summary>
        /// The indices of this node's children.
        /// </summary>
        [JsonPropertyName("children")]
        public uint[] Children { get; set; }

        /// <summary>
        /// The index of the mesh in this node.
        /// </summary>
        [JsonPropertyName("mesh")]
        public int? Mesh { get; set; }

        /// <summary>
        /// A floating-point 4x4 transformation matrix stored in column-major order.
        /// </summary>
        [JsonPropertyName("matrix")]
        [JsonConverter(typeof(Double16ArrayConverter))]
        public double[] Matrix { get; set; }

        /// <summary>
        /// The node's unit quaternion rotation in the order (x, y, z, w),
        /// where w is the scalar.
        /// </summary>
        [JsonPropertyName("rotation")]
        [JsonConverter(typeof(Double4ArrayConverter))]
        public double[] Rotation { get; set; }

        /// <summary>
        /// The node's non-uniform scale.
        /// </summary>
        [JsonPropertyName("scale")]
        [JsonConverter(typeof(Double3ArrayConverter))]
        public double[] Scale { get; set; }

        /// <summary>
        /// The node's translation.
        /// </summary>
        [JsonPropertyName("translation")]
        [JsonConverter(typeof(Double3ArrayConverter))]
        public double[] Translation { get; set; }

        /// <summary>
        /// The weights of the instantiated Morph Target.
        /// Number of elements must match number of Morph Targets of used mesh.
        /// </summary>
        [JsonPropertyName("weights")]
        [JsonConverter(typeof(FloatListConverter))]
        public List<float> Weights { get; set; }

        /// <summary>
        /// The index of the skin (in <see cref="Root.Skins"/>) referenced by this node.
        /// </summary>
        [JsonPropertyName("skin")]
        public int? Skin { get; set; }

        /// <summary>
        /// Camera index
        /// </summary>
        [JsonPropertyName("camera")]
        public int? Camera { get; set; }

        /// <summary>
        /// Application-specific data.
        /// </summary>
        /// <seealso href="https://registry.khronos.org/glTF/specs/2.0/glTF-2.0.html#reference-extras"/>
        [JsonPropertyName("extras")]
        public UnclassifiedData Extras { get; set; }

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

            if (Children != null)
            {
                writer.AddArrayProperty("children", Children);
            }

            if (Mesh.HasValue)
            {
                writer.AddProperty("mesh", Mesh.Value);
            }

            if (Translation != null)
            {
                writer.AddArrayProperty("translation", Translation);
            }

            if (Rotation != null)
            {
                writer.AddArrayProperty("rotation", Rotation);
            }

            if (Scale != null)
            {
                writer.AddArrayProperty("scale", Scale);
            }

            if (Matrix != null)
            {
                writer.AddArrayProperty("matrix", Matrix);
            }

            if (Weights != null)
            {
                writer.AddArrayProperty("weights", Weights);
            }

            if (Skin.HasValue)
            {
                writer.AddProperty("skin", Skin.Value);
            }

            if (Camera.HasValue)
            {
                writer.AddProperty("camera", Camera.Value);
            }

            if (Extensions != null)
            {
                writer.AddProperty("extensions");
                Extensions.GltfSerialize(writer);
            }
            writer.Close();
        }
    }

    /// <summary>
    /// Node extensions
    /// </summary>
    public class NodeExtensions : IGltfObject
    {
        /// <inheritdoc cref="Schema.MeshGpuInstancing"/>
        [JsonPropertyName("EXT_mesh_gpu_instancing")]
        public MeshGpuInstancing MeshGpuInstancing { get; set; }

        /// <inheritdoc cref="Schema.LightsPunctual"/>
        [JsonPropertyName("KHR_lights_punctual")]
        public NodeLightsPunctual LightsPunctual { get; set; }

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
            if (MeshGpuInstancing != null)
            {
                writer.AddProperty("EXT_mesh_gpu_instancing");
                MeshGpuInstancing.GltfSerialize(writer);
            }
            if (LightsPunctual != null)
            {
                writer.AddProperty("KHR_lights_punctual");
                LightsPunctual.GltfSerialize(writer);
            }
            writer.Close();
        }
    }
}
