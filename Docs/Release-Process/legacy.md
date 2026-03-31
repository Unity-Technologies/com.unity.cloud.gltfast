# Legacy Release Process

## Prepare Release Branch

1. Create and push a branch called `release/X.Y.Z` where `X.Y.Z` corresponds to
   the release version.
1. Create and switch to another branch called `release/X.Y.Z-working`.
1. Update package version in `Packages/com.unity.cloud.gltfast/package.json` to
   the next version, if there are any breaking changes, bump the major version,
   commit this change.
1. Update `Packages/com.unity.cloud.gltfast/CHANGELOG.md` and replace `Unreleased`
   by the version and the release date, commit this change. Remove empty sub-sections. If you go out
   of a pre-release version, merge all sections in the x.y.z section.
1. Update the constant variable `GLTFast.Export.Constants.version`
   (in `Packages/com.unity.cloud.gltfast/Runtime/Scripts/Export/Constants.cs`)
   to the release version.
1. Update package version in `.yamato/ValidationExceptions.json` (if any API validation exception is required)
1. Push the branch and open a Pull Request targeting the previously created
   release branch. Add glTFast owners as approvers for this PR.
1. On the [Yamato glTFast project], look for the `release/X.Y.Z-working` branch
   certify that the `Publish Dry Run cloud.gltfast` job has been triggered
   automatically.
