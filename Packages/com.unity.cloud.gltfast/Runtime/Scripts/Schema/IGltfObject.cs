// SPDX-FileCopyrightText: 2025 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using UnityEngine;

namespace GLTFast
{
    /// <summary>
    /// Represents a glTF JSON object, containing key-value properties of arbitrary types.
    /// </summary>
    public interface IGltfObject
    {
        /// <summary>
        /// Tries to find a property of a <paramref name="key"/> and cast its <paramref name="value"/> to type <c>T</c>.
        /// </summary>
        /// <param name="key">Property key</param>
        /// <param name="value">Resulting value</param>
        /// <typeparam name="T">Desired target type</typeparam>
        /// <returns>True if the property was found and successfully cast to type T. False otherwise.</returns>
        bool TryGetValue<T>(string key, out T value);
    }
}
