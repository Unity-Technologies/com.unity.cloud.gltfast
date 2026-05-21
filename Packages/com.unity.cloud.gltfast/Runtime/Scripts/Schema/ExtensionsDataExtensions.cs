// SPDX-FileCopyrightText: 2025 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
#if UNITY_6000_5_OR_NEWER
using System.Text.Json;
#else
using Unity.Gltfast.Text.Json;
#endif

namespace GLTFast
{
    /// <summary>
    /// Extension methods for <see cref="IGltfObject"/>.
    /// </summary>
    static class ExtensionsDataExtensions
    {
        static JsonSerializerOptions s_Options = new() { IncludeFields = true };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetValue<T>(this Dictionary<string, JsonElement> extensionsData, string key, out T value)
        {
            if (extensionsData != null
                && extensionsData.TryGetValue(key, out var token))
            {
                value = token.Deserialize<T>(s_Options);
                return true;
            }

            value = default;
            return false;
        }
    }
}
