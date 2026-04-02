// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;

namespace GLTFast.Schema
{
    /// <summary>
    /// glTF animation interpolation algorithm type.
    /// </summary>
    /// <seealso href="https://registry.khronos.org/glTF/specs/2.0/glTF-2.0.html#_animation_sampler_interpolation"/>
    public enum InterpolationType
    {
        /// <summary>Unknown</summary>
        Unknown,
        /// <summary>The animated values are linearly interpolated between keyframes.</summary>
        Linear,
        /// <summary>The animated values remain constant to the output of the first keyframe, until the next keyframe.</summary>
        Step,
        /// <summary>The animation’s interpolation is computed using a cubic spline with specified tangents.</summary>
        CubicSpline
    }
}
