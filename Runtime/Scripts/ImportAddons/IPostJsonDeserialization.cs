// SPDX-FileCopyrightText: 2026 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using UnityEngine;

namespace GLTFast.Addons
{
    /// <summary>
    /// Useful for analysis, verification and manipulation of the deserialized glTF JSON data
    /// before any further loading of resources (buffers, images) has started.
    /// To use this, implement the interface in an <see cref="ImportAddonInstance"/> and inject that instance.
    /// </summary>
    /// <seealso cref="ImportAddonRegistry"/>
    /// <seealso cref="GltfImportBase.AddImportAddonInstance"/>
    public interface IPostJsonDeserialization
    {
        /// <summary>
        /// Called right after JSON deserialization is complete, but before any loading of resources has started.
        /// </summary>
        /// <returns>False if the loading process has to be aborted due to a critical error. True otherwise.</returns>
        bool PostJsonDeserialization();
    }
}
