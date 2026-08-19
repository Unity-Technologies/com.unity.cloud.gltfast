// SPDX-FileCopyrightText: 2026 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

#if USING_GRAPHICS_TEST_FRAMEWORK
using System;
using UnityEngine.TestTools.Graphics;

namespace GLTFast.Tests.Graphics
{
    /// <summary>
    /// Generates one graphics test case per glTF asset in a <see cref="Import.GltfTestCaseSet"/> and
    /// <see cref="ViewType"/>.
    /// </summary>
    sealed class GltfGraphicsTestAttribute : GraphicsTestAttributeBase
    {
        /// <param name="testCaseSetName">Name of the <see cref="Import.GltfTestCaseSet"/> asset to source assets from.</param>
        /// <param name="testCaseCount">
        /// Expected number of assets the set yields after applying <paramref name="includeFilter"/>. A mismatch fails
        /// test case generation, so that assets added to or removed from the set can't silently skip coverage.
        /// </param>
        /// <param name="includeFilter">Optional regular expression an asset's relative URI has to match.</param>
        /// <param name="views">Views to render each asset from. Defaults to all <see cref="ViewType"/> values.</param>
        public GltfGraphicsTestAttribute(
            string testCaseSetName,
            int testCaseCount,
            string includeFilter = null,
            params ViewType[] views)
            : base(typeof(GltfGraphicsTestCaseSource))
        {
            TestCaseSetName = testCaseSetName;
            TestCaseCount = testCaseCount;
            IncludeFilter = includeFilter;
            Views = views is { Length: > 0 } ? views : (ViewType[])Enum.GetValues(typeof(ViewType));
        }

        public string TestCaseSetName { get; }

        public int TestCaseCount { get; }

        public string IncludeFilter { get; }

        public ViewType[] Views { get; }
    }
}
#endif // USING_GRAPHICS_TEST_FRAMEWORK
