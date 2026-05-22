// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;

namespace GLTFast
{

    using Schema;

    /// <summary>
    /// Extension methods for <see cref="Node"/>
    /// </summary>
    public static class NodeExtension
    {
        /// <summary>
        /// Get translation, rotation and scale of a node, regardless of source
        /// properties
        /// </summary>
        /// <param name="node">Input node</param>
        /// <param name="position">Node's translation</param>
        /// <param name="rotation">Node's rotation</param>
        /// <param name="scale">Node's scale</param>
        public static void GetTransform(
            this Node node,
            out double3 position,
            out double4 rotation,
            out double3 scale
            )
        {

            position = double3.zero;
            rotation = Mathematics.k_QuaternionIdentity;
            scale = Mathematics.k_Double3One;

            if (node.matrix != null)
            {
                var m = new double4x4(
                    node.matrix[0],
                    -node.matrix[4],
                    -node.matrix[8],
                    -node.matrix[12],
                    -node.matrix[1],
                    node.matrix[5],
                    node.matrix[9],
                    node.matrix[13],
                    -node.matrix[2],
                    node.matrix[6],
                    node.matrix[10],
                    node.matrix[14],
                    node.matrix[3],
                    node.matrix[7],
                    node.matrix[11],
                    node.matrix[15]
                );

                m.Decompose(out var t, out var r, out var s);
                position = t;
                rotation = r;
                scale = s;

            }
            else
            {
                if (node.translation != null)
                {
                    Assert.AreEqual(node.translation.Length, 3);
                    position = new double3(
                        -node.translation[0],
                        node.translation[1],
                        node.translation[2]
                    );
                }
                if (node.rotation != null)
                {
                    Assert.AreEqual(node.rotation.Length, 4);
                    rotation = new double4(
                        node.rotation[0],
                        -node.rotation[1],
                        -node.rotation[2],
                        node.rotation[3]
                    );
                }
                if (node.scale != null)
                {
                    Assert.AreEqual(node.scale.Length, 3);
                    scale = new double3(
                        node.scale[0],
                        node.scale[1],
                        node.scale[2]
                    );
                }
            }
        }
    }
}
