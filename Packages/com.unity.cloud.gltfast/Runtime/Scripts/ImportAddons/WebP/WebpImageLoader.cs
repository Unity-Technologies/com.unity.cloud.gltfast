// SPDX-FileCopyrightText: 2025 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

#if WEBP_IS_INSTALLED

using System;
using System.Threading;
using System.Threading.Tasks;
using GLTFast.Logging;
using Unity.Collections;
using UnityEngine;
using WebP;

namespace GLTFast.Addons
{
    /// <summary>
    /// Decodes WebP image data into a <see cref="Texture2D"/> using the unity.webp package.
    /// </summary>
    static class WebpImageLoader
    {
        public static async Task<ImageResult> LoadAsync(
            NativeArray<byte>.ReadOnly data,
            bool linear,
            bool readable,
            bool generateMipMaps,
            ICodeLogger logger,
            CancellationToken cancellationToken
            )
        {
            cancellationToken.ThrowIfCancellationRequested();

            var bytes = data.ToArray();
            int width = 0;
            int height = 0;
            byte[] rawData = null;
            Error error = Error.Success;

            // Offload heavy decompression to a background thread
            await Task.Run(() =>
            {
                try
                {
                    Texture2DExt.GetWebPDimensions(bytes, out width, out height);
                    // Decode WITH mipmaps when requested: LoadRGBAFromWebP allocates the extra
                    // 1/3 buffer space mip generation needs precisely when lMipmaps is true.
                    rawData = Texture2DExt.LoadRGBAFromWebP(bytes, ref width, ref height, generateMipMaps, out error, null);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[glTFast] WebP background decode exception: {e.Message}");
                    error = Error.DecodingError;
                }
            }, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            if (error != Error.Success || rawData == null)
            {
                logger?.Error(LogCode.EmbedImageLoadFailed);
                return ImageResult.Null;
            }

            // Construct the Texture2D directly (not via Texture2DExt.CreateWebpTexture2D, whose
            // mip-count math is wrong) so its mip chain storage matches generateMipMaps.
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, generateMipMaps, linear);
            texture.LoadRawTextureData(rawData);
            texture.Apply(generateMipMaps, !readable);

            // unity.webp returns textures in Unity's standard orientation (bottom-up),
            // so isYFlipped is false.
            return new ImageResult(texture, false);
        }
    }
}

#endif // WEBP_IS_INSTALLED
