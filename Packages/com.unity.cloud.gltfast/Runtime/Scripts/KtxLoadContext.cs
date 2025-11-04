// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

#if KTX_IS_RECENT
#define KTX_IS_ENABLED
#endif

#if KTX_IS_ENABLED

using System.Threading.Tasks;
using KtxUnity;
using Unity.Collections;

namespace GLTFast {
    class KtxLoadContext : KtxLoadContextBase {

        readonly KtxTexture m_KtxTexture;

        public KtxLoadContext(int imageIndex, NativeArray<byte>.ReadOnly data) : base(imageIndex, data)
        {
            m_KtxTexture = new KtxTexture();
        }

        public override async Task<TextureResult> LoadTexture2D(bool linear, bool readable) {
            var errorCode = m_KtxTexture.Open(m_Data);
            if (errorCode != ErrorCode.Success) {
                return new TextureResult(errorCode);
            }

            var result = await m_KtxTexture.LoadTexture2D(linear, readable);

            m_KtxTexture.Dispose();
            return result;
        }
    }
}
#endif // KTX_IS_INSTALLED
