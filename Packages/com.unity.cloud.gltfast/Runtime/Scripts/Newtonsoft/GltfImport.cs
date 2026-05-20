// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using UnityEngine;

namespace GLTFast.Newtonsoft
{
    /// <summary>
    /// Loads a glTF's content, converts it to Unity resources and is able to
    /// feed it to an <see cref="IInstantiator"/> for instantiation.
    /// Before System.Text.Json was used as JSON deserialization, this class used Newtonsoft JSON and is now obsolete.
    /// </summary>
    [Obsolete("Use GLTFast.GltfImport instead.")]
    public class GltfImport : GLTFast.GltfImport { }
}
