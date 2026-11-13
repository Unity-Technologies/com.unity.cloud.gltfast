// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using UnityEngine;

namespace GLTFast.Editor
{

    /// <summary>
    /// Editor Import specific settings (not relevant at runtime)
    /// </summary>
    [Serializable]
    class EditorImportSettings
    {

        /// <summary>
        /// Determines whether and how imported textures get GPU-compressed for the active
        /// build target.
        /// </summary>
        public enum TextureCompressionMode
        {
            /// <summary>Textures keep the format produced by decoding (e.g. RGBA32). No GPU compression.</summary>
            None,

            /// <summary>
            /// Textures get compressed to the format matching the active build target's texture
            /// compression setting (Project Settings &gt; Player &gt; Other Settings &gt; Texture Compression),
            /// e.g. ASTC on Android/iOS.
            /// </summary>
            BuildTarget,
        }

        /// <summary>
        /// Quality tier for GPU texture compression on build targets that support more than one
        /// compressed format family (Desktop/UWP: BCn).
        /// </summary>
        public enum TextureCompressionQualityTier
        {
            /// <summary>Smaller, faster to compress, lower fidelity (e.g. DXT1/DXT5 on Desktop/UWP).</summary>
            Normal,

            /// <summary>Larger, slower to compress, higher fidelity (e.g. BC7 on Desktop/UWP).</summary>
            High,
        }

        /// <summary>
        /// Creates a secondary UV set on all meshes, if there is none present already.
        /// Often used for lightmaps.
        /// </summary>
        [Tooltip("Generate Lightmap UVs")]
        public bool generateSecondaryUVSet;

        /// <summary>
        /// Compress imported textures to the active build target's GPU texture format
        /// (e.g. ASTC on Android/iOS, BC7/DXT on Desktop). Runs once at import time; the
        /// resulting compressed texture is cached like any other imported sub-asset and is
        /// only re-generated when the source file, these settings, or the active build
        /// target's texture compression format change.
        /// </summary>
        [Tooltip("Compress imported textures to the active build target's GPU texture format (e.g. ASTC on Android/iOS). Runs once at import time.")]
        public TextureCompressionMode textureCompression = TextureCompressionMode.BuildTarget;

        /// <summary>
        /// Quality tier to use on build targets whose GPU compression format depends on it
        /// (currently Desktop/UWP, which can use either DXT1/DXT5 or the higher fidelity BC7).
        /// </summary>
        [Tooltip("Quality tier for GPU texture compression, on build targets that support more than one (Desktop/UWP: Normal = DXT1/DXT5, High = BC7).")]
        public TextureCompressionQualityTier textureCompressionQuality = TextureCompressionQualityTier.Normal;
    }
}
