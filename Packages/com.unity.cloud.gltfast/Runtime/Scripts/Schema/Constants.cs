// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

namespace Unity.Cloud.Gltfast.Schema
{
    static class Constants
    {
        public const float epsilon = .001f;

        /// <summary>
        /// Sentinel value for spec-required integer index/count fields when no value has been
        /// provided (e.g. the JSON property was absent or an extension chose to omit it).
        /// Negative values are spec-invalid for these fields, so the negative half of the value
        /// space is reused to encode absence.
        /// </summary>
        public const int UnsetIndex = -1;

        /// <summary>
        /// <see cref="UnsetIndex"/> equivalent for <see cref="long"/>-typed byte-length fields.
        /// </summary>
        public const long UnsetByteLength = -1L;
    }
}
