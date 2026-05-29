// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using Unity.Gltfast.Text.Json;
using Unity.Gltfast.Text.Json.Serialization;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;

// GLTF_EXPORT
using UnityEngine.Rendering;

namespace GLTFast.Schema
{
    /// <summary>
    /// An accessor defines a method for retrieving data as typed arrays from
    /// within a buffer view.
    /// See <a href="https://www.khronos.org/registry/glTF/specs/2.0/glTF-2.0.html#accessors">.
    /// accessor in the glTF 2.0 specification</a>.
    /// </summary>
    public class Accessor : NamedObject, IGltfObject
    {
        /// <summary>
        /// Sparse storage of attributes that deviate from their initialization value.
        /// </summary>
        [JsonPropertyName("sparse")]
        public AccessorSparse Sparse { get; set; }

        /// <summary>
        /// The index of the bufferView.
        /// If this is undefined, look in the sparse object for the index and value buffer views.
        /// </summary>
        [JsonPropertyName("bufferView")]
        public int BufferView { get; set; } = -1;

        /// <summary>
        /// The offset relative to the start of the bufferView in bytes.
        /// This must be a multiple of the size of the component datatype.
        /// </summary>
        [JsonPropertyName("byteOffset")]
        public int ByteOffset { get; set; }

        /// <summary>
        /// The datatype of components in the attribute.
        /// All valid values correspond to WebGL enums.
        /// The corresponding typed arrays are: `Int8Array`, `Uint8Array`, `Int16Array`,
        /// `Uint16Array`, `Uint32Array`, and `Float32Array`, respectively.
        /// 5125 (UNSIGNED_INT) is only allowed when the accessor contains indices
        /// i.e., the accessor is only referenced by `primitive.indices`.
        /// </summary>
        [JsonPropertyName("componentType")]
        public GltfComponentType ComponentType { get; set; }

        /// <summary>
        /// Specifies whether integer data values should be normalized
        /// (`true`) to [0, 1] (for unsigned types) or [-1, 1] (for signed types),
        /// or converted directly (`false`) when they are accessed.
        /// Must be `false` when accessor is used for animation data.
        /// </summary>
        [JsonPropertyName("normalized")]
        public bool Normalized { get; set; }

        /// <summary>
        /// The number of attributes referenced by this accessor, not to be confused
        /// with the number of bytes or number of components.
        /// </summary>
        [JsonPropertyName("count")]
        public int Count { get; set; }

        /// <summary>
        /// Specifies if the attribute is a scalar, vector, or matrix,
        /// and the number of elements in the vector or matrix.
        /// </summary>
        // Property is public for unified serialization only. Warn via Obsolete attribute.
        [Obsolete("Use GetAttributeType and SetAttributeType for access.")]
        [JsonPropertyName("type")]
        public string Type { get; set; }

        /// <inheritdoc cref="Root.Extras"/>
        [JsonPropertyName("extras")]
        public UnclassifiedData Extras { get; set; }

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

        GltfAccessorAttributeType m_TypeEnum = GltfAccessorAttributeType.Undefined;

        /// <summary>
        /// <see cref="GltfAccessorAttributeType"/> typed/cached getter from the <see cref="Type"/> string.
        /// </summary>
        /// <returns>The Accessor's attribute type, if it could be retrieved correctly. <see cref="GltfAccessorAttributeType.Undefined"/> otherwise</returns>
        public GltfAccessorAttributeType GetAttributeType()
        {
            if (m_TypeEnum != GltfAccessorAttributeType.Undefined)
                return m_TypeEnum;

#pragma warning disable CS0618 // Type or member is obsolete
            if (Enum.TryParse(Type, true, out m_TypeEnum))
            {
                Type = null;
                return m_TypeEnum;
            }

            Type = null;
#pragma warning restore CS0618 // Type or member is obsolete

            return GltfAccessorAttributeType.Undefined;
        }

        /// <summary>
        /// <see cref="GltfAccessorAttributeType"/> typed setter for the <see cref="Type"/> string.
        /// </summary>
        /// <param name="attributeType">Attribute type</param>
        public void SetAttributeType(GltfAccessorAttributeType attributeType)
        {
            m_TypeEnum = attributeType;
#pragma warning disable CS0618 // Type or member is obsolete
            Type = null;
#pragma warning restore CS0618 // Type or member is obsolete
        }

        /// <summary>
        /// Maximum value of each component in this attribute.
        /// Both min and max arrays have the same length.
        /// The length is determined by the value of the type property;
        /// it can be 1, 2, 3, 4, 9, or 16.
        ///
        /// When `componentType` is `5126` (FLOAT) each array value must be stored as
        /// double-precision JSON number with numerical value which is equal to
        /// buffer-stored single-precision value to avoid extra runtime conversions.
        ///
        /// `normalized` property has no effect on array values: they always correspond
        /// to the actual values stored in the buffer. When accessor is sparse, this
        /// property must contain max values of accessor data with sparse substitution
        /// applied.
        /// </summary>
        [JsonPropertyName("max")]
        public float[] Max { get; set; }

        /// <summary>
        /// Minimum value of each component in this attribute.
        /// Both min and max arrays have the same length.  The length is determined by
        /// the value of the type property; it can be 1, 2, 3, 4, 9, or 16.
        ///
        /// When `componentType` is `5126` (FLOAT) each array value must be stored as
        /// double-precision JSON number with numerical value which is equal to
        /// buffer-stored single-precision value to avoid extra runtime conversions.
        ///
        /// `normalized` property has no effect on array values: they always correspond
        /// to the actual values stored in the buffer. When accessor is sparse, this
        /// property must contain min values of accessor data with sparse substitution
        /// applied.
        /// </summary>
        [JsonPropertyName("min")]
        public float[] Min { get; set; }

        /// <summary>
        /// Provides size of components by type
        /// </summary>
        /// <param name="componentType">glTF component type</param>
        /// <returns>Component size in bytes</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when value of <see cref="ComponentType"/> is unknown</exception>
        public static int GetComponentTypeSize(GltfComponentType componentType)
        {
            switch (componentType)
            {
                case GltfComponentType.Byte:
                case GltfComponentType.UnsignedByte:
                    return 1;
                case GltfComponentType.Short:
                case GltfComponentType.UnsignedShort:
                    return 2;
                case GltfComponentType.Float:
                case GltfComponentType.UnsignedInt:
                    return 4;
                default:
                    throw new ArgumentOutOfRangeException(nameof(componentType), componentType, null);
            }
        }

        /// <summary>
        /// Converts Unity vertex attribute format to glTF component type.
        /// </summary>
        /// <param name="format">vertex attribute format</param>
        /// <returns>glTF component type</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the value of <see cref="format"/> is unknown.</exception>
        public static GltfComponentType GetComponentType(VertexAttributeFormat format)
        {
            switch (format)
            {
                case VertexAttributeFormat.Float32:
                case VertexAttributeFormat.Float16:
                    return GltfComponentType.Float;
                case VertexAttributeFormat.UNorm8:
                case VertexAttributeFormat.UInt8:
                    return GltfComponentType.UnsignedByte;
                case VertexAttributeFormat.SNorm8:
                case VertexAttributeFormat.SInt8:
                    return GltfComponentType.Byte;
                case VertexAttributeFormat.UNorm16:
                case VertexAttributeFormat.UInt16:
                    return GltfComponentType.UnsignedShort;
                case VertexAttributeFormat.SNorm16:
                case VertexAttributeFormat.SInt16:
                    return GltfComponentType.Short;
                case VertexAttributeFormat.UInt32:
                case VertexAttributeFormat.SInt32:
                    return GltfComponentType.UnsignedInt;
                default:
                    throw new ArgumentOutOfRangeException(nameof(format), format, null);
            }
        }

        /// <summary>
        /// Get one-dimensional glTF attribute type by number of components per elements.
        /// Note that this does not support matrix types.
        /// </summary>
        /// <param name="dimension">Number of components per element</param>
        /// <returns>Corresponding one-dimensional glTF attribute type</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <see cref="dimension"/> is not between 1 and 4.</exception>
        public static GltfAccessorAttributeType GetAccessorAttributeType(int dimension)
        {
            if (dimension < 1 || dimension > 4)
            {
                throw new ArgumentOutOfRangeException(nameof(dimension), dimension, null);
            }
            return (GltfAccessorAttributeType)dimension;
        }

        /// <summary>
        /// Get number of components of glTF attribute type.
        /// </summary>
        /// <param name="type">glTF attribute type</param>
        /// <returns>Number of components</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the value of <see cref="type"/> is unknown.</exception>
        public static int GetAccessorAttributeTypeLength(GltfAccessorAttributeType type)
        {
            switch (type)
            {
                case GltfAccessorAttributeType.SCALAR:
                    return 1;
                case GltfAccessorAttributeType.VEC2:
                    return 2;
                case GltfAccessorAttributeType.VEC3:
                    return 3;
                case GltfAccessorAttributeType.VEC4:
                case GltfAccessorAttributeType.MAT2:
                    return 4;
                case GltfAccessorAttributeType.MAT3:
                    return 9;
                case GltfAccessorAttributeType.MAT4:
                    return 16;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        /// <summary>
        /// For 3D positional data, returns accessor's bounding box. Applies coordinate system transform (glTF to Unity)
        /// </summary>
        /// <returns>Bounding box enclosing the minimum and maximum values</returns>
        public Bounds? TryGetBounds()
        {
            Assert.AreEqual(GltfAccessorAttributeType.VEC3, GetAttributeType());
            if (Min != null && Min.Length > 2 && Max != null && Max.Length > 2)
            {
                var maxBounds = new float3(-Min[0], Max[1], Max[2]);
                var minBounds = new float3(-Max[0], Min[1], Min[2]);
                if (Normalized)
                {
                    switch (ComponentType)
                    {
                        case GltfComponentType.Byte:
                            maxBounds = math.max(maxBounds / sbyte.MaxValue, -1);
                            minBounds = math.max(minBounds / sbyte.MaxValue, -1);
                            break;
                        case GltfComponentType.UnsignedByte:
                            maxBounds /= byte.MaxValue;
                            minBounds /= byte.MaxValue;
                            break;
                        case GltfComponentType.Short:
                            maxBounds = math.max(maxBounds / short.MaxValue, -1);
                            minBounds = math.max(minBounds / short.MaxValue, -1);
                            break;
                        case GltfComponentType.UnsignedShort:
                            maxBounds /= ushort.MaxValue;
                            minBounds /= ushort.MaxValue;
                            break;
                        case GltfComponentType.UnsignedInt:
                            maxBounds /= uint.MaxValue;
                            minBounds /= uint.MaxValue;
                            break;
                    }
                }
                return new Bounds
                {
                    max = maxBounds,
                    min = minBounds
                };
            }
            return null;
        }

        /// <summary>
        /// True if the accessor is <a href="https://www.khronos.org/registry/glTF/specs/2.0/glTF-2.0.html#sparse-accessors">sparse</a>
        /// </summary>
        public bool IsSparse => Sparse != null;

        /// <summary>
        /// Byte size of one element
        /// </summary>
        public int ElementByteSize => GetAccessorAttributeTypeLength(GetAttributeType()) * GetComponentTypeSize(ComponentType);

        /// <summary>
        /// Overall, byte size.
        /// Ignores interleaved or sparse accessors
        /// </summary>
        public int ByteSize => ElementByteSize * Count;

        internal void GltfSerialize(JsonWriter writer)
        {
            writer.AddObject();
            if (BufferView >= 0)
            {
                writer.AddProperty("bufferView", BufferView);
            }
            writer.AddProperty("componentType", (int)ComponentType);
            writer.AddProperty("count", Count);
            Assert.AreNotEqual(GltfAccessorAttributeType.Undefined, m_TypeEnum);
            writer.AddProperty("type", m_TypeEnum.ToString());
            if (ByteOffset > 0)
            {
                writer.AddProperty("byteOffset", ByteOffset);
            }
            if (Normalized)
            {
                writer.AddProperty("normalized", Normalized);
            }
            if (Max != null)
            {
                writer.AddArrayProperty("max", Max);
            }
            if (Min != null)
            {
                writer.AddArrayProperty("min", Min);
            }

            if (Sparse != null)
            {
                writer.AddProperty("sparse");
                Sparse.GltfSerialize(writer);
                writer.Close();
            }
            writer.Close();
        }
    }
}
