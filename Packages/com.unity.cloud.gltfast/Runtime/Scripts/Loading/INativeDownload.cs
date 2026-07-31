// SPDX-FileCopyrightText: 2025 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using Unity.Collections;
using UnityEngine.Scripting.APIUpdating;

namespace Unity.Cloud.Gltfast.Loading
{
    /// <summary>
    /// Provides access to downloaded data without creating an implicit copy in memory.
    /// </summary>
    /// <remarks>
    /// This interface is temporary and will get merged into <see cref="IDownload"/>
    /// upon the next API breaking major release!
    /// </remarks>
    // TODO: Merge into IDownload upon next major API breaking release.
    [MovedFrom(true, sourceNamespace: "GLTFast.Loading", sourceAssembly: "glTFast")]
    public interface INativeDownload
    {
        /// <summary>
        /// Resulting data as NativeArray (does not allocate memory).
        /// </summary>
        NativeArray<byte>.ReadOnly NativeData { get; }
    }
}
