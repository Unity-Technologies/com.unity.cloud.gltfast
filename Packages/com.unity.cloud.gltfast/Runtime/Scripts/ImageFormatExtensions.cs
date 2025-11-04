// SPDX-FileCopyrightText: 2025 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using UnityEngine;

namespace GLTFast
{
    static class ImageFormatExtensions
    {
        public static ImageFormat FromMimeType(ReadOnlySpan<char> mimeType)
        {
            if (mimeType == null || !mimeType.StartsWith("image/"))
                return ImageFormat.Unknown;
            var subType = mimeType[6..];
            if (subType.SequenceEqual("jpeg"))
                return ImageFormat.Jpeg;
            if (subType.SequenceEqual("png"))
                return ImageFormat.PNG;
            if (subType.SequenceEqual("ktx") || subType.SequenceEqual("ktx2"))
                return ImageFormat.Ktx;
            if (subType.SequenceEqual("webp"))
                return ImageFormat.WebP;
            return ImageFormat.Unknown;
        }
    }
}
