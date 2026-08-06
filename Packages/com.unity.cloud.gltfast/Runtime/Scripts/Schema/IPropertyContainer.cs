// SPDX-FileCopyrightText: 2026 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using UnityEngine;

namespace Unity.Cloud.Gltfast.Schema
{
    /// <summary>
    /// Provides access to additional properties on glTF extension or extras objects.
    /// Those are neither defined in the glTF specification (the currently supported version)
    /// nor any extension supported by glTFast.
    /// </summary>
    public interface IPropertyContainer
    {
        /// <summary>
        /// Tries to find a property of a <paramref name="key"/>
        /// and deserializes its <paramref name="value"/> to type <c>T</c>.
        /// </summary>
        /// <param name="key">Property key</param>
        /// <param name="value">Resulting value</param>
        /// <typeparam name="T">Desired target type</typeparam>
        /// <returns>True if the property was found and successfully cast to type T. False otherwise.</returns>
        bool TryGetValue<T>(string key, out T value);
    }
}
