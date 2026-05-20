// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using Unity.Gltfast.Text.Json;
using Unity.Gltfast.Text.Json.Serialization;

namespace GLTFast.Schema
{
    /// <summary>
    /// BufferView extensions
    /// </summary>
    /// <seealso href="https://registry.khronos.org/glTF/specs/2.0/glTF-2.0.html#reference-bufferview"/>
    [Serializable]
    public class BufferViewExtensions : IGltfObject
    {
#if MESHOPT_IS_RECENT
        // ReSharper disable InconsistentNaming
        public BufferViewMeshoptExtension EXT_meshopt_compression;
        // ReSharper restore InconsistentNaming
#endif
        /// <summary>JSON properties without a matching member.</summary>
        [JsonExtensionData, JsonInclude] internal Dictionary<string, JsonElement> ExtensionsData { get; set; }

        /// <inheritdoc/>
        public bool TryGetValue<T>(string key, out T value)
        {
            return ExtensionsData.TryGetValue(key, out value);
        }
    }
}
