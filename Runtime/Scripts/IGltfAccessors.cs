// SPDX-FileCopyrightText: 2026 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using Unity.Collections;

namespace GLTFast
{
    /// <summary>
    /// Provides read-only access to typed glTF accessor data.
    /// </summary>
    public interface IGltfAccessors
    {
        /// <summary>
        /// Provides an accessors typed data.
        /// </summary>
        /// <param name="accessorIndex">glTF accessor index.</param>
        /// <typeparam name="T">Accessor member type.</typeparam>
        /// <returns>The requested data or a non-initialized readonly native array
        /// if the request couldn't be handled.</returns>
        NativeArray<T>.ReadOnly GetAccessorData<T>(int accessorIndex)
            where T : unmanaged;
    }
}
