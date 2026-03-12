// SPDX-FileCopyrightText: 2025 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;

namespace GLTFast
{
    /// <summary>
    /// Image format.
    /// </summary>
    public enum ImageFormat
    {
        /// <summary>Unknown image format</summary>
        Unknown,
        /// <summary>Portable Network Graphics</summary>
        Png,
        /// <summary>JPEG File Interchange Format</summary>
        Jpeg,
        /// <summary>KTX 2.0 GPU Texture Container Format</summary>
        Ktx,
        /// <summary>WebP</summary>
        /// <seealso href="https://developers.google.com/speed/webp"/>
        WebP
    }
}
