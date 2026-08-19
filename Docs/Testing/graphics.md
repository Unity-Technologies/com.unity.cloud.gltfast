# Graphics Tests for glTFast

## Overview

Graphics tests render imported glTF assets from a set of fixed camera angles and compare the result against
reference images, using the [Unity Graphics Test Framework](https://github.cds.internal.unity3d.com/unity/com.unity.testframework.graphics)
(`com.unity.testframework.graphics`).

The tests live in `Packages/com.unity.cloud.gltfast.tests/Tests/Runtime/Scripts/Graphics`:

| File | Role |
| --- | --- |
| `ImportGraphicsTests.cs` | The tests. Imports an asset, frames a camera on it and calls `ImageAssert.AreEqual`. |
| `GltfGraphicsTestAttribute.cs` | Marks a test method and declares which asset set and views to generate cases from. |
| `GltfGraphicsTestCaseSource.cs` | Expands the asset set into one `GltfGraphicsTestCase` per asset and view. |
| `GltfGraphicsTestCase.cs` | A single case: one asset, one view. |

Each generated case is named `gfx-<View>-<AssetFilename>`, which is also the reference image file name.

## How to add a new graphics test

### Import the files

Assets for graphics testing live in `Packages/com.unity.cloud.gltfast.tests/Assets~/Graphic`. Place the desired
glTF file(s) there.

### Update the test case set

The set is `Packages/com.unity.cloud.gltfast.tests/Tests/Runtime/TestCaseSets/glTF-Graphic-Tests-Assets.asset`.
Add the imported asset either by editing the list in the Inspector, or with the **Scan for glTF test files** button,
which picks up everything in the `Graphic` folder.

### Update the expected test case count

`GltfGraphicsTestAttribute` takes the number of assets the set is expected to yield, so that assets added to or
removed from the set can't silently change coverage. Update it on the test method in `ImportGraphicsTests.cs`:

```csharp
[GltfGraphicsTest("glTF-Graphic-Tests-Assets", 2)]
public IEnumerator Import(GltfGraphicsTestCase testCase)
```

A mismatch fails test case generation with the expected and actual counts.

### Generate the reference images

A test without a reference image fails, and the failure message lists every path that was searched and where the
rendered image was written. Use that image as the new reference:

1. Run the graphics tests (Test Runner, or `unity command run_tests --mode PlayMode --filter Graphics --filter_type category --async_tests true`).
2. Open **Window > General > Graphics Tests** and select the failing test.
3. Pick the tab matching the platform the image was captured on, for example `linear-osxeditor_applesilicon-metal-none`.
4. Check the captured image, then select **Accept Result**. Unity copies it into `Assets/ReferenceImages`.

Alternatively, copy the file from `Assets/ActualImages/<platform path>/` to `Projects/glTFast-Test/Assets/ReferenceImages/`
by hand. Reference images checked into the repository are shared by all platforms (see below), so put them in the
root of `ReferenceImages`, not in a platform subfolder.

`Assets/ActualImages` is generated output and is git-ignored.

## Reference image resolution

Reference images are looked up through a fallback chain, from the most platform-specific path to the least. On an
Apple silicon macOS Editor, for example:

```
Assets/ReferenceImages/Linear/OSXEditor_AppleSilicon/Metal/None/<name>.png
Assets/ReferenceImages/Linear/OSXEditor_AppleSilicon/Metal/<name>.png
Assets/ReferenceImages/Linear/OSXEditor_AppleSilicon/<name>.png
Assets/ReferenceImages/Linear/<name>.png
Assets/ReferenceImages/<name>.png
```

glTFast keeps a single set of images in the last, platform-agnostic location, so the same references are used on
every platform. To override one image for one platform only, drop it into the matching subfolder — it takes
precedence without affecting the others.

The framework imports everything under `ReferenceImages` uncompressed, readable, without mipmaps, in linear color
space and with point filtering, so comparisons aren't affected by texture compression or color space conversion.
Those importer settings are applied automatically; check in the resulting `.meta` changes.

## Running on CI

`.yamato/graphics-tests.yml` defines Windows and macOS jobs, triggered by a `gfx` pull request comment.
