// SPDX-FileCopyrightText: 2026 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace GLTFast.Editor
{

    /// <summary>
    /// Compresses design-time imported textures to the GPU format matching the active build
    /// target, right after glTFast has decoded them (from KTX2, WebP, Jpeg or PNG alike).
    /// Compression happens in place on the same <see cref="Texture2D"/> object, so material
    /// references created earlier during import remain valid.
    /// </summary>
    static class EditorTextureCompression
    {
        /// <summary>
        /// Name of the custom dependency that ties glTF texture imports to the active build
        /// target's texture compression format. Registered/updated by
        /// <see cref="TextureCompressionDependency"/>.
        /// </summary>
        internal const string CustomDependencyName = "glTFast/TextureCompressionTarget";

        static readonly TextureFormat[] k_BlockFormatsRequiringMultipleOfFour =
        {
            TextureFormat.DXT1,
            TextureFormat.DXT5,
            TextureFormat.BC4,
            TextureFormat.BC5,
            TextureFormat.BC7,
            TextureFormat.ETC_RGB4,
            TextureFormat.ETC2_RGB,
            TextureFormat.ETC2_RGBA8,
        };

        /// <summary>
        /// Computes a hash that changes whenever the active build target's texture compression
        /// format changes. Used as the value of the <see cref="CustomDependencyName"/> custom
        /// dependency, so glTF assets only get reimported when that actually changes.
        /// </summary>
        internal static Hash128 GetTargetHash()
        {
            var target = EditorUserBuildSettings.activeBuildTarget;
            var compression = GetCompressionFormat(target);
            // Quality tier isn't included here: it's a per-asset serialized field on
            // EditorImportSettings, so changing it already triggers Unity's own reimport of
            // that asset. This hash only needs to cover the build-target-wide format family.
            if (target == BuildTarget.Android)
            {
                // Android's compression format is a project setting (possibly a list of
                // several, in priority order), not implied by the build target alone.
                var formats = PlayerSettings.Android.textureCompressionFormats;
                return Hash128.Compute($"{target}|{compression}|{string.Join(",", formats ?? Array.Empty<TextureCompressionFormat>())}");
            }
            return Hash128.Compute($"{target}|{compression}");
        }

        /// <summary>
        /// Resolves the texture compression format to target for a given build target.
        /// Unlike Android, most build targets don't expose a configurable texture compression
        /// format via public API, since the GPU format is either fixed (Metal on iOS/tvOS always
        /// supports ASTC) or there's no first-class multi-format concept (Desktop uses BCn).
        /// </summary>
        static TextureCompressionFormat GetCompressionFormat(BuildTarget target)
        {
            switch (target)
            {
                case BuildTarget.Android:
                    // Player Settings > Android > Texture Compression Format is a priority-ordered
                    // list (a project can target more than one). The first entry matches what
                    // Unity's own texture importer treats as the primary/default format.
                    var formats = PlayerSettings.Android.textureCompressionFormats;
                    return formats is { Length: > 0 } ? formats[0] : TextureCompressionFormat.ETC2;
                case BuildTarget.iOS:
                case BuildTarget.tvOS:
                    // Every Metal-capable Apple device supports ASTC; Unity doesn't expose a
                    // configurable choice here the way it does for Android.
                    return TextureCompressionFormat.ASTC;
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                case BuildTarget.StandaloneOSX:
                case BuildTarget.StandaloneLinux64:
                case BuildTarget.WSAPlayer:
                    // Quality tier (Normal: DXT1/DXT5, High: BC7) is applied in GetTargetFormat.
                    return TextureCompressionFormat.DXTC;
                case BuildTarget.WebGL:
                    return TextureCompressionFormat.DXTC;
                default:
                    return TextureCompressionFormat.Unknown;
            }
        }

        /// <summary>
        /// Resolves the compressed <see cref="TextureFormat"/> to use for a given build target.
        /// </summary>
        /// <param name="target">Build target to resolve the format for.</param>
        /// <param name="hasAlpha">Whether the source texture has a meaningful alpha channel.</param>
        /// <param name="qualityTier">Quality tier to use on build targets that support more than
        /// one compressed format family (currently Desktop/UWP).</param>
        /// <returns>A compressed texture format, or null if none could be determined (in which
        /// case compression should be skipped rather than guessing a potentially unsupported format).</returns>
        internal static TextureFormat? GetTargetFormat(
            BuildTarget target,
            bool hasAlpha,
            EditorImportSettings.TextureCompressionQualityTier qualityTier)
        {
            switch (GetCompressionFormat(target))
            {
                case TextureCompressionFormat.ASTC:
                    return TextureFormat.ASTC_6x6;
                case TextureCompressionFormat.ETC2:
                    return hasAlpha ? TextureFormat.ETC2_RGBA8 : TextureFormat.ETC2_RGB;
                case TextureCompressionFormat.ETC:
                    return hasAlpha ? TextureFormat.ETC2_RGBA8 : TextureFormat.ETC_RGB4;
#pragma warning disable CS0618 // PVRTC is obsolete, but still a valid, occasionally selected target.
                case TextureCompressionFormat.PVRTC:
                    return hasAlpha ? TextureFormat.PVRTC_RGBA4 : TextureFormat.PVRTC_RGB4;
#pragma warning restore CS0618
                case TextureCompressionFormat.BPTC:
                    return TextureFormat.BC7;
                case TextureCompressionFormat.DXTC:
                    if (qualityTier == EditorImportSettings.TextureCompressionQualityTier.High)
                    {
                        return TextureFormat.BC7;
                    }
                    return hasAlpha ? TextureFormat.DXT5 : TextureFormat.DXT1;
                default:
                    // Unknown/unconfigured compression format for this build target. Returning
                    // null causes the caller to skip compression rather than guess a format that
                    // might not even be supported on the actual build target.
                    return null;
            }
        }

        /// <summary>
        /// Determines whether <paramref name="texture"/> has any pixel with alpha different from
        /// fully opaque. Unlike <see cref="GraphicsFormatUtility.HasAlphaChannel"/>, which only
        /// reports whether the pixel format carries an alpha channel (true for every texture
        /// glTFast decodes, since they all decode to RGBA32), this looks at the actual pixel data.
        /// </summary>
        /// <param name="texture">Texture to inspect. Must be readable.</param>
        /// <returns>True if the texture has a meaningful (non-opaque) alpha channel.</returns>
        internal static bool HasRealAlpha(Texture2D texture)
        {
            if (!GraphicsFormatUtility.HasAlphaChannel(texture.graphicsFormat))
            {
                return false;
            }
            var pixels = texture.GetPixelData<Color32>(0);
            foreach (var pixel in pixels)
            {
                if (pixel.a != 255)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Compresses <paramref name="texture"/> in place to the GPU format matching the active
        /// build target.
        /// </summary>
        /// <param name="texture">Texture to compress. Modified in place. Must be readable - the
        /// caller is responsible for decoding it readable and for locking it back down (or not)
        /// once compression is done.</param>
        /// <param name="qualityTier">Quality tier to use on build targets that support more than
        /// one compressed format family (currently Desktop/UWP).</param>
        /// <param name="warning">Set to a human readable reason when compression was skipped.</param>
        /// <returns>True if the texture was compressed.</returns>
        internal static bool TryCompress(
            Texture2D texture,
            EditorImportSettings.TextureCompressionQualityTier qualityTier,
            out string warning)
        {
            warning = null;

            if (GraphicsFormatUtility.IsCompressedFormat(texture.graphicsFormat))
            {
                // Already GPU compressed (e.g. a KTX2 source transcoded straight to a device format).
                return false;
            }

            var target = EditorUserBuildSettings.activeBuildTarget;
            var hasAlpha = HasRealAlpha(texture);
            var targetFormat = GetTargetFormat(target, hasAlpha, qualityTier);
            if (targetFormat == null)
            {
                warning = $"Skipped texture compression: could not determine a compressed format for build target {target}.";
                return false;
            }

            if ((texture.width % 4 != 0 || texture.height % 4 != 0)
                && Array.IndexOf(k_BlockFormatsRequiringMultipleOfFour, targetFormat.Value) >= 0)
            {
                warning = $"Skipped texture compression: {texture.width}x{texture.height} is not a multiple of 4, required for {targetFormat.Value}.";
                return false;
            }

            EditorUtility.CompressTexture(texture, targetFormat.Value, TextureCompressionQuality.Normal);

            return true;
        }
    }
}
