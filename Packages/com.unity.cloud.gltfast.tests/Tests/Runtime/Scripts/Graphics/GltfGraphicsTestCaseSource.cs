// SPDX-FileCopyrightText: 2026 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

#if USING_GRAPHICS_TEST_FRAMEWORK
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using GLTFast.Tests.Import;
using NUnit.Framework.Interfaces;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine.TestTools.Graphics;
using UnityEngine.TestTools.Graphics.TestCases;

namespace GLTFast.Tests.Graphics
{
    /// <summary>
    /// Sources graphics test cases from a <see cref="GltfTestCaseSet"/>, as configured via
    /// <see cref="GltfGraphicsTestAttribute"/> on the test method.
    /// </summary>
    class GltfGraphicsTestCaseSource : GraphicsTestCaseSource
    {
        public override IEnumerable<GraphicsTestCase> GetTestCases(IMethodInfo methodInfo, ITest suite)
        {
            var attributes = methodInfo.GetCustomAttributes<GltfGraphicsTestAttribute>(false);
            if (attributes == null || attributes.Length < 1)
            {
                throw new InvalidDataException(
                    $"{nameof(GltfGraphicsTestCaseSource)} requires a {nameof(GltfGraphicsTestAttribute)} on " +
                    $"{methodInfo.Name}.");
            }

            var attribute = attributes[0];
            var testCaseSet = LoadTestCaseSet(attribute.TestCaseSetName);
            var filter = attribute.IncludeFilter == null
                ? null
                : new GltfTestCaseFilter(new Regex(attribute.IncludeFilter));

            var actualTestCaseCount = (int)testCaseSet.GetTestCaseCount(filter);
            if (attribute.TestCaseCount != actualTestCaseCount)
            {
                throw new InvalidDataException(
                    $"Incorrect number of test cases in {attribute.TestCaseSetName}. " +
                    $"Expected {attribute.TestCaseCount}, but found {actualTestCaseCount} " +
                    (attribute.IncludeFilter == null ? "" : $"(includeFilter: \"{attribute.IncludeFilter}\")"));
            }

            return IterateTestCases(methodInfo, suite, attribute, testCaseSet, filter);
        }

        static IEnumerable<GraphicsTestCase> IterateTestCases(
            IMethodInfo methodInfo,
            ITest suite,
            GltfGraphicsTestAttribute attribute,
            GltfTestCaseSet testCaseSet,
            GltfTestCaseFilter filter)
        {
            foreach (var view in attribute.Views)
            {
                var nameCounts = new Dictionary<string, int>();
                foreach (var gltfTestCase in testCaseSet.IterateTestCases(filter))
                {
                    // The name doubles as the reference image file name, so it has to be unique per view.
                    var name = $"gfx-{view}-{gltfTestCase.Filename}";
                    if (nameCounts.TryGetValue(name, out var count))
                    {
                        nameCounts[name] = count + 1;
                        name = $"{name}-{count}";
                    }
                    else
                    {
                        nameCounts[name] = 1;
                    }

                    yield return new GltfGraphicsTestCase(
                        name,
                        methodInfo,
                        suite,
                        testCaseSet,
                        gltfTestCase,
                        view);
                }
            }
        }

        static GltfTestCaseSet LoadTestCaseSet(string testCaseSetName)
        {
#if UNITY_EDITOR
            var path = $"Packages/{GltfGlobals.GltfPackageName}/Tests/Runtime/TestCaseSets/{testCaseSetName}.asset";
            var testCaseSet = AssetDatabase.LoadAssetAtPath<GltfTestCaseSet>(path);
            if (testCaseSet == null)
            {
                path = $"Packages/{GltfGlobals.GltfPackageName}.tests/Tests/Runtime/TestCaseSets/{testCaseSetName}.asset";
                testCaseSet = AssetDatabase.LoadAssetAtPath<GltfTestCaseSet>(path);
            }
#else
            var path = $"{testCaseSetName}.json";
            var testCaseSet = GltfTestCaseSet.DeserializeFromStreamingAssets(path);
#endif
            if (testCaseSet == null)
            {
                throw new InvalidDataException($"Test case collection not found at {path}");
            }

            return testCaseSet;
        }
    }
}
#endif // USING_GRAPHICS_TEST_FRAMEWORK
