// SPDX-FileCopyrightText: 2025 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
#if !UNITY_6000_0_OR_NEWER
using System.Runtime.InteropServices;
#endif // !UNITY_6000_0_OR_NEWER
using System.Threading;
using System.Threading.Tasks;
using GLTFast.Addons;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Profiling;
using Unity.Collections;
#if !UNITY_6000_0_OR_NEWER
using Unity.Collections.LowLevel.Unsafe;
#endif // !UNITY_6000_0_OR_NEWER

namespace GLTFast.Documentation.Examples
{
    class PngTextureAddon : ImportAddon<PngTextureAddonInstance> { }

    class PngTextureAddonInstance : ImageLoaderAddonInstance, IDefaultImageFormatLoader
    {
        public override void Inject(GltfImportBase gltfImport)
        {
            gltfImport.AddImportAddonInstance(this);
        }

        public bool IsAbleToLoad(ImageFormat format)
        {
#if UNITY_IMAGECONVERSION
            return format == ImageFormat.Png;
#else
            return false;
#endif
        }

        public bool IsAbleToLoad(ReadOnlySpan<byte> data)
        {
            return ImageFormatDetection.IsPng(data);
        }

        public Task<ImageResult> LoadImage(
            NativeArray<byte>.ReadOnly data,
            bool linear,
            bool readable,
            bool generateMipMaps,
            CancellationToken cancellationToken
            )
        {
#if UNITY_IMAGECONVERSION
#if !UNITY_6000_0_OR_NEWER
            var managedData = NativeToManagedArray(data);
#endif

            Profiler.BeginSample("LoadPNG");
            var texture = CreateEmptyTexture(linear, generateMipMaps);
            var success = texture.LoadImage(
#if UNITY_6000_0_OR_NEWER
                data.AsReadOnlySpan(),
#else
                managedData,
#endif
                !readable
            );
            Profiler.EndSample();
            if (success)
            {
                return Task.FromResult(new ImageResult(texture));
            }
#endif // UNITY_IMAGECONVERSION
            return Task.FromResult(ImageResult.Null);
        }

        static Texture2D CreateEmptyTexture(
            bool forceSampleLinear,
            bool generateMipMaps
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
            );
            return txt;
        }

#if !UNITY_6000_0_OR_NEWER
        static unsafe byte[] NativeToManagedArray(NativeArray<byte>.ReadOnly data)
        {
            Profiler.BeginSample("NativeToManagedArray");
            var managedData = new byte[data.Length];
            var gcHandle = GCHandle.Alloc(managedData, GCHandleType.Pinned);
            fixed (void* dst = &(managedData[0]))
            {
                UnsafeUtility.MemCpy(dst, data.GetUnsafeReadOnlyPtr(), data.Length);
            }
            gcHandle.Free();
            Profiler.EndSample();
            return managedData;
        }
#endif // !UNITY_6000_0_OR_NEWER
    }
}
