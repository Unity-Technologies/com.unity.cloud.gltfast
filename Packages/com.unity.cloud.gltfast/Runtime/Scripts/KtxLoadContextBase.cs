// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

#if KTX_IS_RECENT
#define KTX_IS_ENABLED
#endif

#if KTX_IS_ENABLED

using System.Threading.Tasks;
using KtxUnity;
using Unity.Collections;
using UnityEngine;

namespace GLTFast {
    abstract class KtxLoadContextBase {

        public readonly int imageIndex;
        protected readonly NativeArray<byte>.ReadOnly m_Data;

        protected KtxLoadContextBase(int imageIndex, NativeArray<byte>.ReadOnly data)
        {
            this.imageIndex = imageIndex;
            m_Data = data;
        }

        public abstract Task<TextureResult> LoadTexture2D(bool linear, bool readable);
    }
}
#endif // KTX_IS_INSTALLED
