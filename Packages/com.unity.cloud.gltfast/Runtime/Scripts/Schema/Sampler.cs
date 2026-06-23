// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using Unity.Gltfast.Text.Json;
using Unity.Gltfast.Text.Json.Serialization;
using UnityEngine;

namespace GLTFast.Schema
{
    /// <summary>
    /// Texture sampler properties for filtering and wrapping modes.
    /// </summary>
    public class Sampler : NamedObject, IGltfObject
    {
        /// <summary>
        /// Magnification filter mode.
        /// </summary>
        public enum MagFilterMode
        {
            /// <summary>Undefined</summary>
            Undefined = 0,
            /// <summary>Nearest pixel sampling</summary>
            Nearest = 9728,
            /// <summary>Linear pixel interpolation sampling</summary>
            Linear = 9729,
        }

        /// <summary>
        /// Minification filter mode.
        /// </summary>
        public enum MinFilterMode
        {
            /// <summary>Undefined</summary>
            Undefined = 0,
            /// <summary>Nearest pixel sampling</summary>
            Nearest = 9728,
            /// <summary>Linear pixel interpolation sampling</summary>
            Linear = 9729,
            /// <summary>Nearest pixel and nearest mipmap sampling</summary>
            NearestMipmapNearest = 9984,
            /// <summary>Linear pixel interpolation and nearest mipmap sampling</summary>
            LinearMipmapNearest = 9985,
            /// <summary>Nearest pixel and linear mipmap interpolation sampling</summary>
            NearestMipmapLinear = 9986,
            /// <summary>Linear pixel interpolation and linear mipmap interpolation sampling</summary>
            LinearMipmapLinear = 9987
        }

        /// <summary>
        /// Texture wrap mode.
        /// </summary>
        public enum WrapMode
        {
            /// <summary>Undefined</summary>
            Undefined = 0,
            /// <summary>Clamp to edge</summary>
            ClampToEdge = 33071,
            /// <summary>Mirrored repeat</summary>
            MirroredRepeat = 33648,
            /// <summary>Repeat</summary>
            Repeat = 10497
        }

        /// <summary>
        /// Magnification filter.
        /// Valid values correspond to WebGL enums: `9728` (NEAREST) and `9729` (LINEAR).
        /// </summary>
        [JsonPropertyName("magFilter")]
        public MagFilterMode MagFilter { get; set; } = MagFilterMode.Undefined;

        /// <summary>
        /// Minification filter. All valid values correspond to WebGL enums.
        /// </summary>
        [JsonPropertyName("minFilter")]
        public MinFilterMode MinFilter { get; set; } = MinFilterMode.Undefined;

        /// <summary>
        /// s wrapping mode.  All valid values correspond to WebGL enums.
        /// </summary>
        [JsonIgnore]
        public WrapMode WrapS { get; set; } = WrapMode.Repeat;

        [JsonPropertyName("wrapS"), JsonInclude]
        internal WrapMode? WrapSSerialized
        {
            get => WrapS is WrapMode.Repeat or WrapMode.Undefined ? null : WrapS;
            set => WrapS = value ?? WrapMode.Repeat;
        }

        /// <summary>
        /// t wrapping mode.  All valid values correspond to WebGL enums.
        /// </summary>
        [JsonIgnore]
        public WrapMode WrapT { get; set; } = WrapMode.Repeat;

        [JsonPropertyName("wrapT"), JsonInclude]
        internal WrapMode? WrapTSerialized
        {
            get => WrapT is WrapMode.Repeat or WrapMode.Undefined ? null : WrapT;
            set => WrapT = value ?? WrapMode.Repeat;
        }

        /// <inheritdoc cref="Asset.Extensions"/>
        [JsonPropertyName("extensions")]
        public UnclassifiedData Extensions { get; set; }

        /// <inheritdoc cref="Root.Extras"/>
        [JsonPropertyName("extras")]
        public UnclassifiedData Extras { get; set; }

        /// <summary>JSON properties without a matching member.</summary>
        [JsonExtensionData, JsonInclude] internal Dictionary<string, JsonElement> ExtensionsData { get; set; }

        /// <inheritdoc/>
        public bool TryGetValue<T>(string key, out T value)
        {
            return ExtensionsData.TryGetValue(key, out value);
        }

        /// <summary>
        /// Unity filter mode, derived from glTF's
        /// <see cref="MinFilter"/> and <see cref="MagFilter"/>.
        /// </summary>
        [JsonIgnore]
        public FilterMode FilterMode => ConvertFilterMode(MinFilter, MagFilter);

        /// <summary>
        /// Unity texture wrap mode (horizontal), derived from glTF's
        /// <see cref="WrapS"/> value.
        /// </summary>
        [JsonIgnore]
        public TextureWrapMode WrapU => ConvertWrapMode(WrapS);

        /// <summary>
        /// Unity texture wrap mode (vertical), derived from glTF's
        /// <see cref="WrapT"/> value.
        /// </summary>
        [JsonIgnore]
        public TextureWrapMode WrapV => ConvertWrapMode(WrapT);

        static FilterMode ConvertFilterMode(MinFilterMode minFilterToConvert, MagFilterMode magFilterToConvert)
        {
            switch (minFilterToConvert)
            {
                case MinFilterMode.LinearMipmapLinear:
                    return FilterMode.Trilinear;
                case MinFilterMode.Nearest:
                case MinFilterMode.NearestMipmapNearest:
                case MinFilterMode.NearestMipmapLinear: // incorrect mip-map filtering in this case!
                    return FilterMode.Point;
            }
            switch (magFilterToConvert)
            {
                case MagFilterMode.Nearest:
                    return FilterMode.Point;
                default:
                    return FilterMode.Bilinear;
            }
        }

        static TextureWrapMode ConvertWrapMode(WrapMode wrapMode)
        {
            switch (wrapMode)
            {
                case WrapMode.Undefined:
                case WrapMode.Repeat:
                default:
                    return TextureWrapMode.Repeat;
                case WrapMode.ClampToEdge:
                    return TextureWrapMode.Clamp;
                case WrapMode.MirroredRepeat:
                    return TextureWrapMode.Mirror;
            }
        }

        static WrapMode ConvertWrapMode(TextureWrapMode wrapMode)
        {
            switch (wrapMode)
            {
                case TextureWrapMode.Clamp:
                    return WrapMode.ClampToEdge;
                case TextureWrapMode.Mirror:
                case TextureWrapMode.MirrorOnce:
                    return WrapMode.MirroredRepeat;
                case TextureWrapMode.Repeat:
                default:
                    return WrapMode.Repeat;
            }
        }


        /// <summary>
        /// Parameter-less constructor
        /// </summary>
        public Sampler() { }

        /// <summary>
        /// Constructs a Sampler with filter and wrap modes.
        /// </summary>
        /// <param name="filterMode">Unity texture filter mode</param>
        /// <param name="wrapModeU">Unity texture wrap mode (horizontal)</param>
        /// <param name="wrapModeV">Unity texture wrap mode (vertical)</param>
        public Sampler(FilterMode filterMode, TextureWrapMode wrapModeU, TextureWrapMode wrapModeV)
        {
            switch (filterMode)
            {
                case FilterMode.Point:
                    MagFilter = MagFilterMode.Nearest;
                    MinFilter = MinFilterMode.Nearest;
                    break;
                case FilterMode.Bilinear:
                    MagFilter = MagFilterMode.Linear;
                    MinFilter = MinFilterMode.Linear;
                    break;
                case FilterMode.Trilinear:
                    MagFilter = MagFilterMode.Linear;
                    MinFilter = MinFilterMode.LinearMipmapLinear;
                    break;
            }

            WrapS = ConvertWrapMode(wrapModeU);
            WrapT = ConvertWrapMode(wrapModeV);
        }

        /// <summary>
        /// Applies the Sampler's settings to a Unity texture.
        /// </summary>
        /// <param name="image">Texture to apply the settings to</param>
        /// <param name="defaultMinFilter">Fallback minification filter</param>
        /// <param name="defaultMagFilter">Fallback magnification filter</param>
        public void Apply(Texture2D image,
                          MinFilterMode defaultMinFilter = MinFilterMode.Linear,
                          MagFilterMode defaultMagFilter = MagFilterMode.Linear)
        {
            if (image == null) return;
            image.wrapModeU = WrapU;
            image.wrapModeV = WrapV;

            // Use the default filtering mode for textures that have no such specification in data
            image.filterMode = ConvertFilterMode(
                MinFilter == MinFilterMode.Undefined ? defaultMinFilter : MinFilter,
                MagFilter == MagFilterMode.Undefined ? defaultMagFilter : MagFilter
            );
        }

        internal void GltfSerialize(JsonWriter writer)
        {
            writer.AddObject();
            GltfSerializeName(writer);
            // Assuming MagFilterMode.Linear is the project's default, only
            // serialize valid, non-default values
            if (MagFilter == MagFilterMode.Nearest)
            {
                writer.AddProperty("magFilter", (int)MagFilter);
            }
            // Assuming MinFilterMode.Linear is the project's default, only
            // serialize valid, non-default values
            if (MinFilter != MinFilterMode.Undefined && MinFilter != MinFilterMode.Linear)
            {
                writer.AddProperty("minFilter", (int)MinFilter);
            }
            if (WrapS != WrapMode.Undefined && WrapS != WrapMode.Repeat)
            {
                writer.AddProperty("wrapS", (int)WrapS);
            }
            if (WrapT != WrapMode.Undefined && WrapT != WrapMode.Repeat)
            {
                writer.AddProperty("wrapT", (int)WrapT);
            }
            writer.Close();
        }
    }
}
