// SPDX-FileCopyrightText: 2026 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

#if KTX_IS_RECENT
#define KTX_IS_ENABLED
#endif

#if KTX_IS_ENABLED

using System.Threading.Tasks;
using KtxUnity;
using UnityEngine.Experimental.Rendering;

namespace GLTFast
{
    /// <summary>
    /// Extends <see cref="KtxTexture"/> to expose loading into an explicit, uncompressed target
    /// format as a readable texture, without the GPU blit + readback that
    /// <see cref="TextureBase.LoadTexture2D(GraphicsFormat, uint, uint, uint, bool)"/> would
    /// otherwise require (it hardcodes readable: false).
    /// </summary>
    class ReadableKtxTexture : KtxTexture
    {
        /// <summary>
        /// Loads the previously opened KTX/Basis Universal texture into an explicit,
        /// uncompressed <paramref name="targetFormat"/>, optionally readable.
        /// </summary>
        public Task<TextureResult> LoadTexture2DUncompressed(GraphicsFormat targetFormat, bool linear, bool readable)
            => LoadTexture2DInternal(linear, 0, 0, 0, true, targetFormat, readable);
    }
}

#endif // KTX_IS_ENABLED
