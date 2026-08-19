// SPDX-FileCopyrightText: 2026 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Unity.Cloud.Gltfast.Tests
{
    class AllocationRecorderTests
    {
        const int k_BlockCount = 8;
        const int k_BlockSize = 64 * 1024;

        /// <summary>
        /// Pins the instrument, so a counter that resolves but reads zero cannot pass an allocation budget.
        /// </summary>
        [UnityTest]
        public IEnumerator ObservesKnownAllocation()
        {
            using var recorder = AllocationRecorder.Start();
            recorder.RequireValid();

            var blocks = new byte[k_BlockCount][];
            for (var i = 0; i < k_BlockCount; i++)
            {
                blocks[i] = new byte[k_BlockSize];
                yield return null;
                recorder.SampleFrame();
            }

            Assert.IsNotNull(blocks[k_BlockCount - 1]);
            Assert.GreaterOrEqual(recorder.TotalBytes, (long)k_BlockCount * k_BlockSize);
        }
    }
}
