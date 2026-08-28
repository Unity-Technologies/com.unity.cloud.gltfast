// SPDX-FileCopyrightText: 2026 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

#if USING_GRAPHICS_TEST_FRAMEWORK
using NUnit.Framework.Interfaces;
using Unity.Cloud.Gltfast.Tests.Import;
using UnityEngine.TestTools.Graphics;

namespace Unity.Cloud.Gltfast.Tests.Graphics
{
    /// <summary>
    /// A <see cref="GraphicsTestCase"/> that renders one glTF asset from one <see cref="ViewType"/>.
    /// </summary>
    record GltfGraphicsTestCase : GraphicsTestCase
    {
        public GltfGraphicsTestCase(
            string name,
            IMethodInfo methodInfo,
            ITest fixture,
            GltfTestCaseSet testCaseSet,
            GltfTestCase gltfTestCase,
            ViewType view)
            : base(name, methodInfo, fixture)
        {
            TestCaseSet = testCaseSet;
            GltfTestCase = gltfTestCase;
            View = view;
        }

        public GltfTestCaseSet TestCaseSet { get; }

        public GltfTestCase GltfTestCase { get; }

        public ViewType View { get; }
    }
}
#endif // USING_GRAPHICS_TEST_FRAMEWORK
