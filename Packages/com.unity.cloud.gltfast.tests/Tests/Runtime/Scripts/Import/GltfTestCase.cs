// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using Unity.Cloud.Gltfast.Logging;
using UnityEngine;

namespace Unity.Cloud.Gltfast.Tests.Import
{
    [Serializable]
    class GltfTestCase
    {
        public string relativeUri;

        public bool expectLoadFail;
        public bool expectInstantiationFail;

        public LogCode[] expectedLogCodes;

        public Extension[] requiredExtensions;

        public string Filename
        {
            get
            {
                var lastSeparatorIndex = relativeUri.LastIndexOfAny(new[] { '/', '\\' });
                return lastSeparatorIndex >= 0
                    ? relativeUri.Substring(lastSeparatorIndex + 1)
                    : relativeUri;
            }
        }

        public override string ToString()
        {
            return Filename;
        }
    }
}
