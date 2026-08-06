// SPDX-FileCopyrightText: 2026 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using Unity.Gltfast.Text.Json;

namespace Unity.Cloud.Gltfast.Schema
{
    /// <summary>
    /// Read-only view over a single JSON value, used to traverse arbitrary JSON data.
    /// </summary>
    public readonly ref struct Value
    {
        readonly JsonElement m_Element;

        /// <summary>
        /// Initializes a new <see cref="Value"/> wrapping the given JSON element.
        /// </summary>
        /// <param name="element">The JSON element to wrap.</param>
        internal Value(JsonElement element)
        {
            m_Element = element;
        }

        /// <summary>The <see cref="ValueKind"/> of this value.</summary>
        public ValueKind Kind => (ValueKind)m_Element.ValueKind;

        /// <summary>Tries to get this value as a double-precision floating point number.</summary>
        /// <param name="value">The resulting number, if successful.</param>
        /// <returns><c>true</c> if the value is a number representable as a <see cref="double"/>; otherwise <c>false</c>.</returns>
        public bool TryGetDouble(out double value) => m_Element.TryGetDouble(out value);

        /// <summary>Tries to get this value as a 64-bit signed integer.</summary>
        /// <param name="value">The resulting integer, if successful.</param>
        /// <returns><c>true</c> if the value is a number representable as a <see cref="long"/>; otherwise <c>false</c>.</returns>
        public bool TryGetInt64(out long value) => m_Element.TryGetInt64(out value);

        /// <summary>Gets this value as a string.</summary>
        /// <returns>The string value, or <c>null</c> if the value is a JSON <c>null</c>.</returns>
        public string GetString() => m_Element.GetString();

        /// <summary>Gets this value as a boolean.</summary>
        /// <returns>The boolean value.</returns>
        public bool GetBoolean() => m_Element.GetBoolean();

        /// <summary>Gets the value of the object property named <paramref name="key"/>.</summary>
        /// <param name="key">The property name.</param>
        /// <value>The property's value.</value>
        public Value this[string key] => new(m_Element.GetProperty(key));

        /// <summary>Gets the array element at <paramref name="index"/>.</summary>
        /// <param name="index">The zero-based element index.</param>
        /// <value>The element's value.</value>
        public Value this[int index] => new(m_Element[index]);

        /// <summary>The number of elements, when this value is a <see cref="ValueKind.Array"/>.</summary>
        public int ArrayLength => m_Element.GetArrayLength();

        /// <summary>Tries to get the value of the object property named <paramref name="key"/>.</summary>
        /// <param name="key">The property name.</param>
        /// <param name="value">The resulting value, if the property exists.</param>
        /// <returns><c>true</c> if the property exists; otherwise <c>false</c>.</returns>
        public bool TryGetValue(string key, out Value value)
        {
            if (m_Element.TryGetProperty(key, out var element))
            {
                value = new Value(element);
                return true;
            }
            value = default;
            return false;
        }

        /// <summary>Returns an enumerator over the properties of this object value.</summary>
        /// <returns>An enumerator yielding each <see cref="Property"/>.</returns>
        public ObjectEnumerator EnumerateObject() => new(m_Element.EnumerateObject());

        internal bool TryGetValue<T>(out T value)
        {
            try
            {
                value = m_Element.Deserialize<T>(JsonOptions.Options);
                return true;
            }
            catch (JsonException)
            {
                value = default;
                return false;
            }
        }

        /// <summary>Enumerates the properties of an object <see cref="Value"/> as <see cref="Property"/> items.</summary>
        public ref struct ObjectEnumerator
        {
            JsonElement.ObjectEnumerator m_Enumerator;

            internal ObjectEnumerator(JsonElement.ObjectEnumerator enumerator)
            {
                m_Enumerator = enumerator;
            }

            /// <summary>Returns this enumerator, enabling use in a <c>foreach</c> loop.</summary>
            /// <returns>This enumerator.</returns>
            public ObjectEnumerator GetEnumerator() => this;

            /// <summary>Advances the enumerator to the next property.</summary>
            /// <returns><c>true</c> if there is another property; otherwise <c>false</c>.</returns>
            public bool MoveNext() => m_Enumerator.MoveNext();

            /// <summary>The property at the current position of the enumerator.</summary>
            public Property Current => new(m_Enumerator.Current);
        }
    }
}
