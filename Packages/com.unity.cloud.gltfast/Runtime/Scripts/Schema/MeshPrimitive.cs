// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using Unity.Gltfast.Text.Json;
using Unity.Gltfast.Text.Json.Serialization;

namespace GLTFast.Schema
{
    /// <summary>
    /// Geometry to be rendered with the given material.
    /// </summary>
    public class MeshPrimitive : ICloneable, IMaterialsVariantsSlot, IGltfObject
    {
        /// <inheritdoc cref="MeshPrimitiveExtensions"/>
        [JsonPropertyName("extensions")]
        public MeshPrimitiveExtensions Extensions { get; set; }

        /// <summary>
        /// A dictionary object, where each key corresponds to mesh attribute semantic
        /// and each value is the index of the accessor containing attribute's data.
        /// </summary>
        [JsonPropertyName("attributes")]
        public Attributes Attributes { get; set; }

        /// <summary>
        /// The index of the accessor that contains mesh indices.
        /// When this is not defined, the primitives should be rendered without indices
        /// using `drawArrays()`. When defined, the accessor must contain indices:
        /// the `bufferView` referenced by the accessor must have a `target` equal
        /// to 34963 (ELEMENT_ARRAY_BUFFER); a `byteStride` that is tightly packed,
        /// i.e., 0 or the byte size of `componentType` in bytes;
        /// `componentType` must be 5121 (UNSIGNED_BYTE), 5123 (UNSIGNED_SHORT)
        /// or 5125 (UNSIGNED_INT), the latter is only allowed
        /// when `OES_element_index_uint` extension is used; `type` must be `\"SCALAR\"`.
        /// </summary>
        [JsonPropertyName("indices")]
        public int? Indices { get; set; }

        /// <summary>
        /// The index of the material to apply to this primitive when rendering.
        /// </summary>
        [JsonPropertyName("material")]
        public int? Material { get; set; }

        /// <summary>
        /// The type of primitives to render. All valid values correspond to WebGL enums.
        /// </summary>
        [JsonIgnore]
        public PrimitiveMode Mode { get; set; } = PrimitiveMode.Triangles;

        [JsonPropertyName("mode"), JsonInclude]
        internal PrimitiveMode? ModeSerialized
        {
            get => Mode == PrimitiveMode.Triangles ? null : Mode;
            set => Mode = value ?? PrimitiveMode.Triangles;
        }

        /// <summary>
        /// An array of Morph Targets, each  Morph Target is a dictionary mapping
        /// attributes to their deviations
        /// in the Morph Target (index of the accessor containing the attribute
        /// displacements' data).
        /// </summary>
        [JsonPropertyName("targets")]
        public List<MorphTarget> Targets { get; set; }

        /// <inheritdoc cref="Root.Extras"/>
        [JsonPropertyName("extras")]
        public UnclassifiedData Extras { get; set; }

        /// <summary>JSON properties without a matching member.</summary>
        [JsonExtensionData, JsonInclude] internal Dictionary<string, JsonElement> ExtensionsData { get; set; }

        /// <inheritdoc/>
        public bool TryGetValue<T>(string key, out T value)
        {
            return ExtensionsData.TryGetValue(key, out value);
        }

        /// <inheritdoc />
        public int? GetMaterialIndex(int variantIndex)
        {
            var mapping = Extensions?.MaterialsVariants;
            if (mapping != null && mapping.TryGetMaterialIndex(variantIndex, out var materialIndex))
            {
                return materialIndex;
            }
            return Material;
        }

#if DRACO_IS_INSTALLED
        [JsonIgnore]
        public bool IsDracoCompressed => Extensions != null && Extensions.DracoMeshCompression != null;
#endif

        /// <summary>
        /// Clones the object
        /// </summary>
        /// <returns>Member-wise clone</returns>
        public object Clone()
        {
            return MemberwiseClone();
        }

        internal void GltfSerialize(JsonWriter writer)
        {
            writer.AddObject();
            if (Attributes != null)
            {
                writer.AddProperty("attributes");
                Attributes.GltfSerialize(writer);
            }
            if (Indices.HasValue)
            {
                writer.AddProperty("indices", Indices.Value);
            }
            if (Material.HasValue)
            {
                writer.AddProperty("material", Material.Value);
            }
            if (Mode != PrimitiveMode.Triangles)
            {
                writer.AddProperty("mode", (int)Mode);
            }
            if (Targets != null)
            {
                writer.AddArray("targets");
                foreach (var target in Targets)
                {
                    target.GltfSerialize(writer);
                }
                writer.CloseArray();
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
    /// Mesh vertex attribute collection. Each property value is the index of
    /// the accessor containing attribute’s data.
    /// </summary>
    [JsonConverter(typeof(AttributesConverter))]
    public class Attributes : IGltfObject
    {
        /// <summary>Vertex position accessor index.</summary>
        public int? Position { get; set; }

        /// <summary>Vertex normals accessor index.</summary>
        public int? Normal { get; set; }

        /// <summary>Vertex tangents accessor index.</summary>
        public int? Tangent { get; set; }

        /// <summary>
        /// Texture coordinate accessor indices. List index <c>n</c> corresponds
        /// to the glTF semantic <c>TEXCOORD_n</c>. Sparse holes are
        /// <see langword="null"/>. Use the
        /// <see cref="AttributesExtensions.GetTexCoord"/>/<see cref="AttributesExtensions.SetTexCoord"/>
        /// extension methods for bounds-checked index access.
        /// </summary>
        public List<int?> TexCoords { get; set; }

        /// <summary>
        /// Vertex color accessor indices (<c>COLOR_n</c>). Same semantics as
        /// <see cref="TexCoords"/>; see
        /// <see cref="AttributesExtensions.GetColor"/>/<see cref="AttributesExtensions.SetColor"/>.
        /// </summary>
        public List<int?> Colors { get; set; }

        /// <summary>
        /// Bone joint accessor indices (<c>JOINTS_n</c>). Same semantics as
        /// <see cref="TexCoords"/>; see
        /// <see cref="AttributesExtensions.GetJoint"/>/<see cref="AttributesExtensions.SetJoint"/>.
        /// </summary>
        public List<int?> Joints { get; set; }

        /// <summary>
        /// Bone weight accessor indices (<c>WEIGHTS_n</c>). Same semantics as
        /// <see cref="TexCoords"/>; see
        /// <see cref="AttributesExtensions.GetWeight"/>/<see cref="AttributesExtensions.SetWeight"/>.
        /// </summary>
        public List<int?> Weights { get; set; }

        /// <summary>JSON properties without a matching member (e.g. application-defined attribute semantics such as <c>_TEMPERATURE</c>).</summary>
        [JsonExtensionData, JsonInclude] internal Dictionary<string, JsonElement> ExtensionsData { get; set; }

        /// <inheritdoc/>
        public bool TryGetValue<T>(string key, out T value)
        {
            return ExtensionsData.TryGetValue(key, out value);
        }

        /// <summary>
        /// Consolidates all `TEXCOORD_*` accessor fields into a single array.
        /// The result is delimited to the maximum count of texture coordinate sets Unity supports.
        /// </summary>
        /// <param name="uvAccessors">Resulting array of accessor indices.</param>
        /// <param name="limitExceeded">If true, the attributes has more UV sets than Unity supports
        /// and uvAccessors is delimited.</param>
        /// <returns>True if there's one or more UV sets and the result is valid. False otherwise.</returns>
        [Obsolete("Access TexCoords directly instead")]
        public bool TryGetAllUVAccessors(out int[] uvAccessors, out bool limitExceeded)
        {
            var uvCount = TexCoords?.Count ?? 0;
            if (uvCount < 1)
            {
                uvAccessors = null;
                limitExceeded = false;
                return false;
            }

            limitExceeded = uvCount > VertexBufferGeneratorBase.maxUvSetCount;
            if (limitExceeded)
            {
                uvCount = VertexBufferGeneratorBase.maxUvSetCount;
            }

            uvAccessors = new int[uvCount];
            for (var i = 0; i < uvCount; i++)
            {
                uvAccessors[i] = TexCoords[i] ?? -1;
            }

            return true;
        }

        internal void GltfSerialize(JsonWriter writer)
        {
            writer.AddObject();
            if (Position.HasValue) writer.AddProperty("POSITION", Position.Value);
            if (Normal.HasValue) writer.AddProperty("NORMAL", Normal.Value);
            if (Tangent.HasValue) writer.AddProperty("TANGENT", Tangent.Value);
            WriteChannel(writer, "TEXCOORD_", TexCoords);
            WriteChannel(writer, "COLOR_", Colors);
            WriteChannel(writer, "JOINTS_", Joints);
            WriteChannel(writer, "WEIGHTS_", Weights);
            writer.Close();
        }

        static void WriteChannel(JsonWriter writer, string prefix, List<int?> list)
        {
            if (list == null) return;
            for (var i = 0; i < list.Count; i++)
            {
                if (list[i].HasValue)
                {
                    writer.AddProperty($"{prefix}{i}", list[i].Value);
                }
            }
        }
    }

    /// <summary>
    /// Mesh primitive extensions
    /// </summary>
    public class MeshPrimitiveExtensions : IGltfObject
    {
#if DRACO_IS_INSTALLED
        [JsonPropertyName("KHR_draco_mesh_compression")]
        public MeshPrimitiveDracoExtension DracoMeshCompression { get; set; }
#endif

        /// <inheritdoc cref="MaterialsVariantsMeshPrimitiveExtension"/>
        [JsonPropertyName("KHR_materials_variants")]
        public MaterialsVariantsMeshPrimitiveExtension MaterialsVariants { get; set; }

        /// <inheritdoc cref="Root.Extras"/>
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
#if DRACO_IS_INSTALLED
            if (DracoMeshCompression != null)
            {
                writer.AddProperty("KHR_draco_mesh_compression");
                DracoMeshCompression.GltfSerialize(writer);
            }
#endif
            if (MaterialsVariants != null)
            {
                writer.AddProperty("KHR_materials_variants");
                MaterialsVariants.GltfSerialize(writer);
            }
            writer.Close();
        }
    }

#if DRACO_IS_INSTALLED
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
#endif

    /// <summary>
    /// Morph target (blend shape)
    /// </summary>
    public class MorphTarget
    {
        /// <summary>Vertex position deviation accessor index.</summary>
        [JsonPropertyName("POSITION")]
        public int? Position { get; set; }
        /// <summary>Vertex normal deviation accessor index.</summary>
        [JsonPropertyName("NORMAL")]
        public int? Normal { get; set; }
        /// <summary>Vertex tangent deviation accessor index.</summary>
        [JsonPropertyName("TANGENT")]
        public int? Tangent { get; set; }

        internal void GltfSerialize(JsonWriter writer)
        {
            if (Position.HasValue) writer.AddProperty("POSITION", Position.Value);
            if (Normal.HasValue) writer.AddProperty("NORMAL", Normal.Value);
            if (Tangent.HasValue) writer.AddProperty("TANGENT", Tangent.Value);
        }
    }
}
