// SPDX-FileCopyrightText: 2026 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using Unity.Gltfast.Text.Json;
using UnityEngine;

namespace Unity.Cloud.Gltfast.Schema
{
    /// <summary>
    /// Allocation-free view into additional properties of a glTF JSON object.
    /// </summary>
    public readonly ref struct Properties
    {
        static readonly JsonSerializerOptions k_Options = new() { IncludeFields = true };
        readonly Dictionary<string, JsonElement> m_Data;

        internal Properties(Dictionary<string, JsonElement> data) => m_Data = data;

        /// <inheritdoc cref="IPropertyContainer.TryGetValue{T}"/>
        public bool TryGetValue<T>(string key, out T value)
        {
            if (m_Data != null && m_Data.TryGetValue(key, out var token))
            {
                value = token.Deserialize<T>(k_Options);
                return true;
            }
            value = default; return false;
        }
    }
}
