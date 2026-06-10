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
        public int Indices { get; set; } = -1;

        /// <summary>
        /// The index of the material to apply to this primitive when rendering.
        /// </summary>
        [JsonPropertyName("material")]
        public int Material { get; set; } = -1;

        /// <summary>
        /// The type of primitives to render. All valid values correspond to WebGL enums.
        /// </summary>
        [JsonPropertyName("mode")]
        public PrimitiveMode Mode { get; set; } = PrimitiveMode.Triangles;

        /// <summary>
        /// An array of Morph Targets, each  Morph Target is a dictionary mapping
        /// attributes to their deviations
        /// in the Morph Target (index of the accessor containing the attribute
        /// displacements' data).
        /// </summary>
        [JsonPropertyName("targets")]
        public MorphTarget[] Targets { get; set; }

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
        public int GetMaterialIndex(int variantIndex)
        {
            var mapping = Extensions?.MaterialsVariants;
            if (mapping != null && mapping.TryGetMaterialIndex(variantIndex, out var materialIndex))
            {
                return materialIndex;
            }
            return Material;
        }

#if DRACO_IS_INSTALLED
        public bool IsDracoCompressed => Extensions != null && Extensions.DracoMeshCompression != null;
#endif

        /// <summary>
        /// Determines whether two object instances are equal.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns>true if the specified object is equal to the current object; otherwise, false.</returns>
        // TODO: Remove upon next major release. This serves no purpose anymore except keeping the API intact.
        public override bool Equals(object obj)
        {
            // ReSharper disable once BaseObjectEqualsIsObjectEquals
            return base.Equals(obj);
        }

        /// <summary>
        /// Default hash function.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        // TODO: Remove upon next major release. This serves no purpose anymore except keeping the API intact.
        public override int GetHashCode()
        {
            // ReSharper disable once BaseObjectGetHashCodeCallInGetHashCode
            return base.GetHashCode();
        }

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
            if (Indices >= 0)
            {
                writer.AddProperty("indices", Indices);
            }
            if (Material >= 0)
            {
                writer.AddProperty("material", Material);
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
    public class Attributes
    {
        /// <summary>Vertex position accessor index.</summary>
        [JsonPropertyName("POSITION")]
        public int Position { get; set; } = -1;

        /// <summary>Vertex normals accessor index.</summary>
        [JsonPropertyName("NORMAL")]
        public int Normal { get; set; } = -1;

        /// <summary>Vertex tangents accessor index.</summary>
        [JsonPropertyName("TANGENT")]
        public int Tangent { get; set; } = -1;

        /// <summary>Texture coordinates accessor index.</summary>
        [JsonPropertyName("TEXCOORD_0")]
        public int TexCoord0 { get; set; } = -1;

        /// <summary>Texture coordinates accessor index (second UV set).</summary>
        [JsonPropertyName("TEXCOORD_1")]
        public int TexCoord1 { get; set; } = -1;

        /// <summary>Texture coordinates accessor index (third UV set).</summary>
        [JsonPropertyName("TEXCOORD_2")]
        public int TexCoord2 { get; set; } = -1;

        /// <summary>Texture coordinates accessor index (fourth UV set).</summary>
        [JsonPropertyName("TEXCOORD_3")]
        public int TexCoord3 { get; set; } = -1;

        /// <summary>Texture coordinates accessor index (fifth UV set).</summary>
        [JsonPropertyName("TEXCOORD_4")]
        public int TexCoord4 { get; set; } = -1;

        /// <summary>Texture coordinates accessor index (sixth UV set).</summary>
        [JsonPropertyName("TEXCOORD_5")]
        public int TexCoord5 { get; set; } = -1;

        /// <summary>Texture coordinates accessor index (seventh UV set).</summary>
        [JsonPropertyName("TEXCOORD_6")]
        public int TexCoord6 { get; set; } = -1;

        /// <summary>Texture coordinates accessor index (eighth UV set).</summary>
        [JsonPropertyName("TEXCOORD_7")]
        public int TexCoord7 { get; set; } = -1;

        /// <summary>Texture coordinates accessor index (ninth UV set).</summary>
        [JsonPropertyName("TEXCOORD_8")]
        public int TexCoord8 { get; set; } = -1;

        /// <summary>Vertex color accessor index.</summary>
        [JsonPropertyName("COLOR_0")]
        public int Color0 { get; set; } = -1;

        /// <summary>Bone joints accessor index.</summary>
        [JsonPropertyName("JOINTS_0")]
        public int Joints0 { get; set; } = -1;

        /// <summary>Bone weights accessor index.</summary>
        [JsonPropertyName("WEIGHTS_0")]
        public int Weights0 { get; set; } = -1;


        /// <summary>
        /// Determines whether two object instances are equal.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns>true if the specified object is equal to the current object; otherwise, false.</returns>
        // TODO: Remove upon next major release. This serves no purpose anymore except keeping the API intact.
        public override bool Equals(object obj)
        {
            // ReSharper disable once BaseObjectEqualsIsObjectEquals
            return base.Equals(obj);
        }

        /// <summary>
        /// Default hash function.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        // TODO: Remove upon next major release. This serves no purpose anymore except keeping the API intact.
        public override int GetHashCode()
        {
            // ReSharper disable once BaseObjectGetHashCodeCallInGetHashCode
            return base.GetHashCode();
        }

        /// <summary>
        /// Calculates the texture coordinate set quantity.
        /// </summary>
        /// <returns>Texture coordinate set quantity.</returns>
        public int GetTexCoordsCount()
        {
            if (TexCoord0 < 0) return 0;
            if (TexCoord1 < 0) return 1;
            if (TexCoord2 < 0) return 2;
            if (TexCoord3 < 0) return 3;
            if (TexCoord4 < 0) return 4;
            if (TexCoord5 < 0) return 5;
            if (TexCoord6 < 0) return 6;
            if (TexCoord7 < 0) return 7;
            return TexCoord8 < 0 ? 8 : 9;
        }

        /// <summary>
        /// Consolidates all `TEXCOORD_*` accessor fields into a single array.
        /// The result is delimited to the maximum count of texture coordinate sets Unity supports.
        /// </summary>
        /// <param name="uvAccessors">Resulting array of accessor indices.</param>
        /// <param name="limitExceeded">If true, the attributes has more UV sets than Unity supports
        /// and uvAccessors is delimited.</param>
        /// <returns>True if there's one or more UV sets and the result is valid. False otherwise.</returns>
        public bool TryGetAllUVAccessors(out int[] uvAccessors, out bool limitExceeded)
        {
            var uvCount = GetTexCoordsCount();
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
            uvAccessors[0] = TexCoord0;
            if (uvAccessors.Length >= 2)
            {
                uvAccessors[1] = TexCoord1;
            }
            if (uvAccessors.Length >= 3)
            {
                uvAccessors[2] = TexCoord2;
            }
            if (uvAccessors.Length >= 4)
            {
                uvAccessors[3] = TexCoord3;
            }
            if (uvAccessors.Length >= 5)
            {
                uvAccessors[4] = TexCoord4;
            }
            if (uvAccessors.Length >= 6)
            {
                uvAccessors[5] = TexCoord5;
            }
            if (uvAccessors.Length >= 7)
            {
                uvAccessors[6] = TexCoord6;
            }
            if (uvAccessors.Length >= 8)
            {
                uvAccessors[7] = TexCoord7;
            }
            return true;
        }

        internal void GltfSerialize(JsonWriter writer)
        {
            writer.AddObject();
            if (Position >= 0) writer.AddProperty("POSITION", Position);
            if (Normal >= 0) writer.AddProperty("NORMAL", Normal);
            if (Tangent >= 0) writer.AddProperty("TANGENT", Tangent);
            if (TexCoord0 >= 0) writer.AddProperty("TEXCOORD_0", TexCoord0);
            if (TexCoord1 >= 0) writer.AddProperty("TEXCOORD_1", TexCoord1);
            if (TexCoord2 >= 0) writer.AddProperty("TEXCOORD_2", TexCoord2);
            if (TexCoord3 >= 0) writer.AddProperty("TEXCOORD_3", TexCoord3);
            if (TexCoord4 >= 0) writer.AddProperty("TEXCOORD_4", TexCoord4);
            if (TexCoord5 >= 0) writer.AddProperty("TEXCOORD_5", TexCoord5);
            if (TexCoord6 >= 0) writer.AddProperty("TEXCOORD_6", TexCoord6);
            if (TexCoord7 >= 0) writer.AddProperty("TEXCOORD_7", TexCoord7);
            if (Color0 >= 0) writer.AddProperty("COLOR_0", Color0);
            if (Joints0 >= 0) writer.AddProperty("JOINTS_0", Joints0);
            if (Weights0 >= 0) writer.AddProperty("WEIGHTS_0", Weights0);
            writer.Close();
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
        public int Position { get; set; } = -1;
        /// <summary>Vertex normal deviation accessor index.</summary>
        [JsonPropertyName("NORMAL")]
        public int Normal { get; set; } = -1;
        /// <summary>Vertex tangent deviation accessor index.</summary>
        [JsonPropertyName("TANGENT")]
        public int Tangent { get; set; } = -1;

        /// <summary>
        /// Determines whether two object instances are equal.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns>true if the specified object is equal to the current object; otherwise, false.</returns>
        // TODO: Remove upon next major release. This serves no purpose anymore except keeping the API intact.
        public override bool Equals(object obj)
        {
            // ReSharper disable once BaseObjectEqualsIsObjectEquals
            return base.Equals(obj);
        }

        /// <summary>
        /// Default hash function.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        // TODO: Remove upon next major release. This serves no purpose anymore except keeping the API intact.
        public override int GetHashCode()
        {
            // ReSharper disable once BaseObjectGetHashCodeCallInGetHashCode
            return base.GetHashCode();
        }

        internal void GltfSerialize(JsonWriter writer)
        {
            if (Position >= 0) writer.AddProperty("POSITION", Position);
            if (Normal >= 0) writer.AddProperty("NORMAL", Normal);
            if (Tangent >= 0) writer.AddProperty("TANGENT", Tangent);
        }
    }
}
