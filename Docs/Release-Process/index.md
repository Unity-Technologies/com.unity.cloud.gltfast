# Release Process

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
1. Post a link in the [shiproom channel] to the pull request with the following
   [template](./Templates/release-pr-message.md).
1. Wait for approval and wait for the Yamato job to complete successfully. If
   any issues arise, communicate with the appropriate owners until resolved.
1. Currently (as of version 6.x) the automatic API validation is muted due to false negatives and has to be manually checked.
   1. Locate the Yamato Job *API Validation - cloud.gltfast - 2021.3 - windows* and unzip its *logs* artifact.
   1. Download `ValidationSuiteReports/ApiValidationReport.json` and compare it against `Docs/Release-Process/<A.B.C>-ApiValidationReportReference.json` (where `<A.B.C>` represent the version of the last release that introduced API changes). Ensure no difference goes undetected by using a specialised comparison tool like [TextCompare](https://www.textcompare.org/).
   1. Make sure no unknown breaking change was added at [JsonPath](https://github.com/json-path/JsonPath) `$.assemblyChanges[*].breakingChanges`
   1. Additions (new entries at the path `$.assemblyChanges[*].additions`) are tolerated but those imply that this release's minor version number is **required** to be increased. If that does not align with the version number chosen, restart the process with the correct version number.
   1. Finally, if there's been any difference in the API validation report, save the `ApiValidationReport.json` file as it is needed in [Update the `develop` branch](#update-the-develop-branch) process later on.
1. Merge `release/X.Y.Z-working` into `release/X.Y.Z`.
1. Proceed to creating the [STAR checklist](#prepare-star-checklist) and [validating the QA artifacts](#validate-qa-artifacts).

## Prepare STAR Checklist

This step needs to be performed by the package owner.

1. Go to the internal [STAR Checklist
   Portal](https://star-checklist.ds.unity3d.com/) and search for
   `com.unity.cloud.gltfast` - `PackageSupported`and open it.
1. Confirm that the previous version matches the previous release.
1. Select `Revalidate`
1. Enter the new version to be released
1. Follow instructions provided by the release management team.

## Validate QA Artifacts

Until this step is automated, it will be performed by one of the glTFast
owners

1. On the [Yamato glTFast project], look for the `release/X.Y.Z` branch and
   find the `Package Pack - cloud.gltfast` job.
1. Look for the latest instance of the job, which should correspond to the
   instance run during the above step.
1. Copy the link to the artifacts page of this job [shiproom channel] using the
   [following template](./Templates/qa-artifacts-message.md)
1. Quality will then start validating:
   - If quality finds bugs, reach out to the team responsible for the faulting
     packages who might apply a hotfix and monitor with the QA team when a new set of artifacts needs to be created.
   - Once a bundle of hotfixes have been submitted, re-run
     `Publish Dry Run cloud.gltfast` job.
   - Once successful, re-execute the steps in this section.

## Update the `develop` branch

1. From develop, create another branch called `chore/update-develop`
1. For this step, we will refer to version `X.Y.W-pre.1` where `W` is one patch
   increment ahead of `Z`. For example, if we were releasing `1.2.3` then this would become `1.2.4-pre.1`
1. Update package version in `Packages/com.unity.cloud.gltfast/package.json` to
   the `X.Y.W-pre.1`.
1. Update `Packages/com.unity.cloud.gltfast/CHANGELOG.md` by applying the same
   `X.Y.Z` release section and date and adding an
   [unreleased section](./Templates/changelog-section.md). Move any entries
   which may have been added to develop since the release to the new unreleased
   section.
1. Update the constant variable `GLTFast.Export.Constants.version`
   (in `Packages/com.unity.cloud.gltfast/Runtime/Scripts/Export/Constants.cs`)
   to `X.Y.W-pre.1`.
1. Update package version in `.yamato/ValidationExceptions.json` (if any API validation exception is required)
1. If an API change has been detected during [Prepare Release Branch](#prepare-release-branch) update the API validation report within folder `Docs/Release-Process`:
   1. Remove the existing report `*-ApiValidationReportReference.json`.
   1. Move the file `ApiValidationReportReference.json` that you kept before into said folder and prefix it with the new version followed by a `-` (`X.Y.Z-ApiValidationReportReference.json`).
1. Open a PR and, once reviewed, merge it into develop and delete the working
   branch

## Publish Internally

1. Go to the [glTFast Package Works portal].
1. View the glTFast repository and add the new release branch.
1. Create a new release stream called `glTFast/X.Y.Z`
1. In the release stream, add a package to the release stream by selecting the
   release branch added before.
1. Confirm that all badges are green. As of now, certain validation jobs are
   instable due to timeouts, so re-running the *Publish Dry Run* might solve
   the issue.
1. Press the *Promote* button, enable the *Hold Promotion After Candidates Upload* option in the following dialog and confirm by pressing *Create Promotion Job*.
1. Press *Start* in the *Promotion Details* view and wait until the *Candidates Upload* is completed.
1. In Git, create an annotated tag `git tag -a release/X.Y.Z`. Use the
   [following template](./Templates/tag-template.md) for the tag's comment.
1. In Git, push your newly created tag `git push origin tag release/X.Y.Z`. You may have to delete the identically named branch locally first to avoid a refspec conflict.
1. Use [this template](./Templates/completed-internal-release-message.md) to
   send a post on the Then post it on the
   [shiproom channel] slack channels.

## Promote Package Externally

When packages are on Artifactory, they are accessible to Unity developers only.
In order to make these packages accessible to the public, they need to be
promoted to UPM. In order for this to be effective, one of the glTFast release
managers must promote the package on the Go to the
[glTFast Package Works portal].

## Update the `main` branch

Branch `main` serves as base for public contributions, so it has to be updated just like `develop`:

1. Set `main` to the commit that the `release/X.Y.Z` branch was based on (has to be on `develop`'s timeline; see first step of [Prepare Release Branch](#prepare-release-branch)). This can be achieved via a local fast-forward merge (or git reset).
1. Push `main` internally.
    1. Temporarily un-protect the `main` branch by disabling the *Lock branch* and *Do not allow bypassing the above settings* settings in `main`'s branch protection rules.
    1. Git push `main` to the internal Git repository.
    1. Revert the branch protection settings.

## Update the Public Repository

1. Push the release tag created in [Publish Internally](#publish-internally) to the [public Git repository]([glTFastPublic]).
1. Push branch `main` externally
    1. Temporarily un-protect the `main` branch by disabling the *Do not allow bypassing the above settings* settings in `main`'s branch protection rules.
    1. Git push `main` to the [public Git repository]([glTFastPublic]).
    1. Revert the branch protection settings.

[glTFast Package Works portal]: https://package-works.prd.cds.internal.unity3d.com/project?id=6135
[shiproom channel]: https://unity.slack.com/archives/C043U33AY3B
[Yamato glTFast project]: https://unity-ci.cds.internal.unity3d.com/project/2268?nav=branches
[glTFastPublic]: https://github.com/Unity-Technologies/com.unity.cloud.gltfast
