// SPDX-FileCopyrightText: 2025 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

#if UNITY_IMAGECONVERSION

using System;
using System.Threading;
using System.Threading.Tasks;
using GLTFast.Logging;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Profiling;

#if UNITY_6000_0_OR_NEWER
using GLTFast.Loading;
#else
using System.Runtime.InteropServices;
using GLTFast.Jobs;
using GLTFast.Loading;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
#endif

namespace GLTFast {
    static class ImageConversionImageLoader {

        public static async Task<ImageResult> LoadAsync(
            ImportContext context,
            Uri uri,
            bool readable,
            CancellationToken cancellationToken
        )
        {
            using var download = await context.DownloadProvider.RequestTexture(uri,!readable);
            if (download == null)
            {
                context.Logger?.Error(LogCode.TextureDownloadFailed, "?", uri.ToString());
                return ImageResult.Null;
            }

            if(cancellationToken.IsCancellationRequested)
                return ImageResult.Null;

            if (download.Success)
            {
                while (context.DeferAgent.ShouldDefer())
                    await Task.Yield();
                Profiler.BeginSample("ITextureDownload.Texture");
                var texture = download.Texture;
                Profiler.EndSample();
                return new ImageResult(texture);
            }

            context.Logger?.Error(LogCode.TextureDownloadFailed, download.Error, uri.ToString());
            return ImageResult.Null;
        }

        public static async Task<ImageResult> LoadAsync(
            ImportContext context,
            ImportSettings settings,
            NativeArray<byte>.ReadOnly data,
            bool linear,
            bool readable,
            CancellationToken cancellationToken
        )
        {
#if !UNITY_6000_0_OR_NEWER
            var managedData = new byte[data.Length];
            var gcHandle = GCHandle.Alloc(managedData, GCHandleType.Pinned);
            var job = CreateMemCopyJob(data, gcHandle);
            var jobHandle = job.Schedule();
            while(!jobHandle.IsCompleted)
            {
                await Task.Yield();
            }
            jobHandle.Complete();
            gcHandle.Free();
            if(cancellationToken.IsCancellationRequested)
                return ImageResult.Null;
#endif
            while (context.DeferAgent.ShouldDefer())
            {
                if(cancellationToken.IsCancellationRequested)
                    return ImageResult.Null;
                await Task.Yield();
            }

            Profiler.BeginSample("ImageConversionLoadContext.LoadTexture2D");
            // TODO: Investigate alternative: native texture creation in worker thread
            var texture = CreateEmptyTexture(
                linear,
                settings.GenerateMipMaps,
                settings.AnisotropicFilterLevel
                );
            texture.LoadImage(
#if UNITY_6000_0_OR_NEWER
                data.AsReadOnlySpan(),
#else
                managedData,
#endif
                !readable
            );
            Profiler.EndSample();
            return new ImageResult(texture);
        }

        static Texture2D CreateEmptyTexture(
            bool forceSampleLinear,
            bool generateMipMaps,
            int anisotropicFilterLevel
            )
        {
            var textureCreationFlags = TextureCreationFlags.DontUploadUponCreate | TextureCreationFlags.DontInitializePixels;
            if (generateMipMaps)
            {
                textureCreationFlags |= TextureCreationFlags.MipChain;
            }
            var txt = new Texture2D(
                4, 4,
                forceSampleLinear
                    ? GraphicsFormat.R8G8B8A8_UNorm
                    : GraphicsFormat.R8G8B8A8_SRGB,
                textureCreationFlags
            )
            {
                anisoLevel = anisotropicFilterLevel
            };
            return txt;
        }

#if !UNITY_6000_0_OR_NEWER
        static unsafe MemCopyJob CreateMemCopyJob(
            NativeArray<byte>.ReadOnly data,
            GCHandle gcHandle
        )
        {
            var job = new MemCopyJob
            {
                bufferSize = data.Length,
                input = (byte*)data.GetUnsafeReadOnlyPtr(),
                result = (void*)gcHandle.AddrOfPinnedObject()
            };

            return job;
        }
#endif // !UNITY_6000_0_OR_NEWER

        /// <summary>
        /// UnityWebRequestTexture always loads Jpegs/PNGs in sRGB color space
        /// without mipmaps. This method figures if this is not desired and the
        /// texture data needs to be loaded from raw bytes.
        /// </summary>
        /// <returns>True if image texture had to be loaded manually from bytes, false otherwise.</returns>
        internal static bool LoadImageFromBytes(
            bool forceSampleLinear,
            bool generateMipMaps
            )
        {

#if UNITY_EDITOR
            if (IsEditorImport) {
                // Use the original texture at Editor (asset database) import
                return false;
            }
#endif
#if UNITY_WEBREQUEST_TEXTURE
            return forceSampleLinear || generateMipMaps;
#else
            return true;
#endif
        }

#if UNITY_EDITOR
        /// <summary>
        /// Returns true if this import is for an asset, in contrast to
        /// runtime loading.
        /// </summary>
        static bool IsEditorImport => !EditorApplication.isPlaying;
#endif // UNITY_EDITOR
    }
}

#endif // UNITY_IMAGECONVERSION
