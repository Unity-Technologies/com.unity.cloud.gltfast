// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

#if KTX_IS_RECENT
#define KTX_IS_ENABLED
#endif

#if KTX_IS_ENABLED

using System;
using System.Threading;
using System.Threading.Tasks;
using GLTFast.Logging;
using KtxUnity;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace GLTFast
{
    static class KtxImageLoader
    {

        public static async Task<ImageResult> LoadAsync(
            ImportContext context,
            ImportSettings settings,
            NativeArray<byte>.ReadOnly data,
            bool linear,
            bool readable,
            CancellationToken cancellationToken
            )
        {
            // ReadableKtxTexture is only needed for the uncompressed-target-format path below,
            // which is the only one whose readable texture can't be produced by the public API
            // directly (see the branch below for why).
            var ktx = settings.UncompressedTextures ? (KtxTexture)new ReadableKtxTexture() : new KtxTexture();
            var errorCode = ktx.Open(data);
            if (errorCode != ErrorCode.Success)
            {
                context.Logger?.Error(LogCode.EmbedImageLoadFailed);
                ktx.Dispose();
                return default;
            }

            // TODO implement cancellation in KTX package
            TextureResult result;
            if (settings.UncompressedTextures)
            {
                // Request an uncompressed target format instead of one selected by
                // TranscodeFormatHelper based on the currently running graphics device.
                // This is required for design-time (Editor) imports, where the Editor's GPU
                // is not necessarily the build target's. RGBA32 is supported by every device,
                // unlike a GPU compressed format (e.g. DXT), which may not be supported on the
                // eventual build target.
                var targetFormat = linear
                    ? GraphicsFormat.R8G8B8A8_UNorm
                    : GraphicsFormat.R8G8B8A8_SRGB;
                // KtxTexture.LoadTexture2D(GraphicsFormat, ...) always creates a non-readable
                // texture, regardless of what was requested, and requires a real graphics device
                // to make it readable via GPU blit + readback afterwards - which produces black
                // textures with no error when run headless (-batchmode -nographics), the common
                // case for Editor imports/CI. LoadTexture2DUncompressed calls the protected
                // low-level API directly with the requested readable flag instead, so no GPU
                // device or readback is needed and the KTX file's real mip chain is preserved.
                result = await ((ReadableKtxTexture)ktx).LoadTexture2DUncompressed(targetFormat, linear, readable);
                if (result.errorCode != ErrorCode.Success)
                {
                    // Fall back to graphics-device based format selection. Also covers a future
                    // KTX package version that changes the protected LoadTexture2DInternal signature.
                    result = await ktx.LoadTexture2D(linear, readable);
                }
            }
            else
            {
                result = await ktx.LoadTexture2D(linear, readable);
            }

            if (result.errorCode == ErrorCode.Success)
            {
                if (settings.GenerateMipMaps && result.texture.mipmapCount <= 1)
                {
                    Debug.LogWarning("KTX texture does not contain mipmaps.");
                }
            }
            ktx.Dispose();
            return new ImageResult(result.texture, result.orientation.IsYFlipped());
        }
    }
}
#endif // KTX_IS_ENABLED
