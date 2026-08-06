// SPDX-FileCopyrightText: 2026 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using Unity.Gltfast.Text.Json;
using Unity.Gltfast.Text.Json.Serialization;

namespace Unity.Cloud.Gltfast.Schema
{
    /// <summary>
    /// glTF object base class
    /// </summary>
    public class AdditionalPropertyContainer : IPropertyContainer
    {
        /// <summary>
        /// JSON properties without a matching member.
        /// </summary>
        [JsonExtensionData, JsonInclude]
        internal Dictionary<string, JsonElement> ExtensionData { get; set; }

        [JsonIgnore]
        Properties AdditionalProperties => new(ExtensionData);

        /// <inheritdoc/>
        public bool TryGetValue<T>(string key, out T value)
        {
            return AdditionalProperties.TryGetValue(key, out value);
        }
    }
}
