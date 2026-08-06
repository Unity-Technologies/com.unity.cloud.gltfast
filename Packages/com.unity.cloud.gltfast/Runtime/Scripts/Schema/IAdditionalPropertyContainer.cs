// SPDX-FileCopyrightText: 2026 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;

namespace Unity.Cloud.Gltfast.Schema
{
    /// <summary>
    /// Provides access to additional properties on glTF JSON objects.
    /// </summary>
    public interface IAdditionalPropertyContainer
    {
        /// <summary>
        /// Additional properties on glTF JSON objects.
        /// Those properties may have been added by a new, unsupported version of the glTF specification.
        /// For extending glTF, please use extensions or extras instead.
        /// </summary>
        Properties AdditionalProperties { get; }
    }
}
