#!/bin/sh

reset_materials()
{
    echo "Resetting Materials"
    # (potentially altered during previous tests)
    pushd Packages/com.unity.cloud.gltfast.tests/Tests
    git restore 'Runtime/Export/Materials/**/*.mat'
    git restore 'Resources/Export/Materials/**/*.mat'
    git restore 'Runtime/Export/ExportRenderTexture.renderTexture'
    git restore 'Runtime/RenderPipelineAssets/*.asset'
    popd

    pushd Projects/glTFast-Test/Assets/
    git restore 'Settings/*PipelineAsset.asset'
    git restore 'Settings/*ForwardRenderer.asset'
    git restore 'Settings/UniversalRenderPipelineGlobalSettings.asset'
    popd

    git restore 'Projects/glTFast-Test/ProjectSettings/GraphicsSettings.asset'
}
