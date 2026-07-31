// SPDX-FileCopyrightText: 2026 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using Unity.Gltfast.Text.Json.Serialization;
using UnityEngine.Scripting.APIUpdating;

namespace Unity.Cloud.Gltfast.Schema
{
    /// <summary>
    /// Camera projection type
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter<CameraType>))]
    [MovedFrom(true, sourceNamespace: "GLTFast.Schema", sourceAssembly: "glTFast")]
    public enum CameraType
    {
        /// <summary>Undefined</summary>
        Undefined,

        /// <summary>Orthogonal projection</summary>
        [JsonStringEnumMemberName("orthographic")]
        Orthographic,

        /// <summary>Perspective projection</summary>
        [JsonStringEnumMemberName("perspective")]
        Perspective
    }
}
