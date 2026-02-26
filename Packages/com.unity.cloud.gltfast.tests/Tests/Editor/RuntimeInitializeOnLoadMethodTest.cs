// SPDX-FileCopyrightText: 2026 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace GLTFast.Editor.Tests
{
    class RuntimeInitializeOnLoadMethodTest
    {
        [UnityTest]
        public IEnumerator RuntimeInitializeOnLoadMethod()
        {
            // Entering and exiting play mode triggers static methods with the RuntimeInitializeOnLoadMethodAttribute
            // to be triggered.
            yield return new EnterPlayMode();
            yield return new ExitPlayMode();
        }
    }
}
