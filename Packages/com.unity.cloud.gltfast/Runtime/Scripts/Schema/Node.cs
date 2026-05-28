// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

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
        public int Mesh { get; set; } = -1;

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
        public float[] Weights { get; set; }

        /// <summary>
        /// The index of the skin (in <see cref="Root.Skins"/> referenced by this node.
        /// </summary>
        [JsonPropertyName("skin")]
        public int Skin { get; set; } = -1;

        /// <summary>
        /// Camera index
        /// </summary>
        [JsonPropertyName("camera")]
        public int Camera { get; set; } = -1;

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

            if (Mesh >= 0)
            {
                writer.AddProperty("mesh", Mesh);
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

            if (Skin >= 0)
            {
                writer.AddProperty("skin", Skin);
            }

            if (Camera >= 0)
            {
                writer.AddProperty("camera", Camera);
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
        // Names are identical to glTF specified properties, that's why
        // inconsistent names are ignored.
        // ReSharper disable InconsistentNaming

        /// <inheritdoc cref="MeshGpuInstancing"/>
        public MeshGpuInstancing EXT_mesh_gpu_instancing { get; set; }
        /// <inheritdoc cref="LightsPunctual"/>
        public NodeLightsPunctual KHR_lights_punctual { get; set; }

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
