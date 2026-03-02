// SPDX-FileCopyrightText: 2025 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using UnityEngine;

namespace GLTFast
{
    /// <summary>
    /// Imported glTF image containing a <see cref="Texture2D" /> and metadata.
    /// </summary>
    struct ImageResult
    {
        /// <summary>
        /// Imported texture.
        /// </summary>
        public readonly Texture2D texture;

        /// <summary>
        /// If true, the image is flipped vertically.
        /// </summary>
        public readonly bool isYFlipped;

        /// <summary>
        /// Empty image. Indicates a failed load.
        /// </summary>
        public static ImageResult Null => new(null);

        /// <summary>
        /// Default Image constructor.
        /// </summary>
        /// <param name="texture">Imported texture.</param>
        /// <param name="isYFlipped">Must be true if texture is flipped vertically.</param>
        public ImageResult(Texture2D texture, bool isYFlipped = false)
        {
            this.texture = texture;
            this.isYFlipped = isYFlipped;
        }
    }
}
