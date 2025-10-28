// SPDX-FileCopyrightText: 2025 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System.IO;
using System.Threading.Tasks;
using GLTFast.Logging;

namespace GLTFast.Documentation.Examples
{

    using GLTFast;
    using System;
    using UnityEngine;

    class LoadGltfFromMemory : MonoBehaviour
    {
        // Path to the gltf asset to be loaded
        public string filePath;

        // ReSharper disable once Unity.IncorrectMethodSignature
        // ReSharper disable once UnusedMember.Local
        async Task Start()
        {
            await LoadGltfFile();
        }

        #region LoadGltfFromMemory
        async Task LoadGltfFile()
        {
            var gltfDataAsByteArray = await File.ReadAllBytesAsync(filePath);
            var gltf = new GltfImport(logger: new ConsoleLogger());
            var success = await gltf.Load(
                gltfDataAsByteArray,
                // The URI of the original data is important for resolving relative URIs within the glTF
                new Uri(filePath)
                );
            if (success)
            {
                await gltf.InstantiateMainSceneAsync(transform);
            }
        }
        #endregion
    }
}
