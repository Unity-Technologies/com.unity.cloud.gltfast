// SPDX-FileCopyrightText: 2026 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using UnityEngine;

namespace GLTFast
{
    /// <summary>
    /// Provides glTF node hierarchy information.
    /// </summary>
    public interface INodeHierarchyInfo
    {
        /// <summary>
        /// Gets the name of the node at the given index.
        /// </summary>
        /// <param name="nodeIndex">Node index.</param>
        /// <returns>Node name.</returns>
        string GetNodeName(int nodeIndex);

        /// <summary>
        /// Gets the parent index of the node at the given index.
        /// </summary>
        /// <param name="nodeIndex">Node index.</param>
        /// <returns>Parent node index (negative if node has no parent).</returns>
        int GetParentIndex(int nodeIndex);
    }
}
