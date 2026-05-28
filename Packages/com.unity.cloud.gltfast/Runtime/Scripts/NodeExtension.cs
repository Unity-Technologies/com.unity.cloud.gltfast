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

            if (node.Matrix != null)
            {
                var m = new double4x4(
                    node.Matrix[0],
                    -node.Matrix[4],
                    -node.Matrix[8],
                    -node.Matrix[12],
                    -node.Matrix[1],
                    node.Matrix[5],
                    node.Matrix[9],
                    node.Matrix[13],
                    -node.Matrix[2],
                    node.Matrix[6],
                    node.Matrix[10],
                    node.Matrix[14],
                    node.Matrix[3],
                    node.Matrix[7],
                    node.Matrix[11],
                    node.Matrix[15]
                );

                m.Decompose(out var t, out var r, out var s);
                position = t;
                rotation = r;
                scale = s;

            }
            else
            {
                if (node.Translation != null)
                {
                    Assert.AreEqual(node.Translation.Length, 3);
                    position = new double3(
                        -node.Translation[0],
                        node.Translation[1],
                        node.Translation[2]
                    );
                }
                if (node.Rotation != null)
                {
                    Assert.AreEqual(node.Rotation.Length, 4);
                    rotation = new double4(
                        node.Rotation[0],
                        -node.Rotation[1],
                        -node.Rotation[2],
                        node.Rotation[3]
                    );
                }
                if (node.Scale != null)
                {
                    Assert.AreEqual(node.Scale.Length, 3);
                    scale = new double3(
                        node.Scale[0],
                        node.Scale[1],
                        node.Scale[2]
                    );
                }
            }
        }
    }
}
