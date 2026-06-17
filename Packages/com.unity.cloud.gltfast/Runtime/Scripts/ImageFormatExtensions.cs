// SPDX-FileCopyrightText: 2025 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using GLTFast.Schema;

namespace GLTFast
{
    static class ImageFormatExtensions
    {
        public static ImageFormat FromMimeType(EnumOrRawValue<ImageMimeType> mimeType)
        {
            return mimeType.Value switch
            {
                ImageMimeType.Jpeg => ImageFormat.Jpeg,
                ImageMimeType.Png => ImageFormat.Png,
                ImageMimeType.Ktx2 => ImageFormat.Ktx,
                ImageMimeType.WebP => ImageFormat.WebP,
                _ => ImageFormat.Unknown,
            };
        }
    }
}
