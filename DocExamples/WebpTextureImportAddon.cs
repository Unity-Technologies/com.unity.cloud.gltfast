// SPDX-FileCopyrightText: 2025 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

#if WEBP_IS_INSTALLED

using System;
using System.Threading;
using System.Threading.Tasks;
using GLTFast.Addons;
using GLTFast.Schema;
using Unity.Collections;
using UnityEngine;
using WebP;

namespace GLTFast.Documentation.Examples
{
    /// <summary>
    /// Import add-on that registers native WebP texture support.
    /// </summary>
    class WebpTextureImportAddon : ImportAddon<WebpTextureImportAddonInstance> { }

    /// <summary>
    /// Handles the EXT_texture_webp glTF extension and WebP image format detection.
    /// Decodes WebP texture data using the unity.webp package.
    /// </summary>
    class WebpTextureImportAddonInstance : ImportAddonInstance, ITextureImageLoader
    {
        /// <inheritdoc />
        public override void Inject(GltfImportBase gltfImport)
        {
            gltfImport.AddImportAddonInstance(this);
        }

        /// <inheritdoc />
        public override void Inject(IInstantiator instantiator) { }

        /// <inheritdoc />
        public override void Dispose() { }

        /// <inheritdoc />
        public override bool SupportsGltfExtension(string extensionName)
        {
            return extensionName == ExtensionName.TextureWebP;
        }

        /// <summary>
        /// Checks if the texture has the EXT_texture_webp extension and returns the WebP image index.
        /// </summary>
        public bool IsAbleToLoad(TextureBase texture, out int imageIndex)
        {
            if (texture.Extensions?.EXT_texture_webp is { source: >= 0 } webp)
            {
                imageIndex = webp.source;
                return true;
            }
            imageIndex = -1;
            return false;
        }

        /// <summary>
        /// Detects WebP format from raw byte data (RIFF????WEBP header).
        /// </summary>
        public bool IsAbleToLoad(ReadOnlySpan<byte> data)
        {
            return ImageFormatDetection.IsWebP(data);
        }

        /// <summary>
        /// Decodes WebP image data into a Unity Texture2D.
        /// </summary>
        public Task<ImageResult> LoadImage(
            NativeArray<byte>.ReadOnly data,
            bool linear,
            bool readable,
            bool generateMipMaps,
            CancellationToken cancellationToken
            )
        {
            return WebpImageLoader.LoadAsync(data, linear, readable, generateMipMaps, null, cancellationToken);
        }
    }
}

#endif // WEBP_IS_INSTALLED
