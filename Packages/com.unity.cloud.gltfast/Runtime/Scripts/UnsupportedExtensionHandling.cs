// SPDX-FileCopyrightText: 2026 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;

namespace GLTFast
{
    /// <summary>
    /// Defines how an unsupported <b>required</b> glTF extension (an entry in
    /// <c>extensionsRequired</c> that glTFast and its add-ons don't support) is handled.
    /// Unsupported <b>used</b> (optional) extensions always just log a warning and are ignored,
    /// regardless of this setting.
    /// </summary>
    public enum UnsupportedExtensionHandling
    {
        /// <summary>
        /// Log a warning and continue importing. The result may be incomplete or incorrect,
        /// since the unsupported extension's data is ignored, but most glTF assets degrade
        /// gracefully. This is the default, so arbitrary glTF assets load out of the box.
        /// </summary>
        Warn = 0,

        /// <summary>
        /// Log an error and abort the import, as glTFast did prior to this setting's introduction.
        /// </summary>
        Abort
    }
}
