# Release Manual Validation

For the glTFast strategy, manual validation is required before a release can be made. This process ensures that all changes are thoroughly tested and verified.

## Test Suite

This table outlines the tests that need to be performed before releasing the glTFast package.
For evert test a random asset is chosen from the assets list below in order to avoid the pesticide effect from running the same tests with the same assets over and over.

| Test Name               | Description                                                                                                                                                                                                                                                                         | Expected Result |
|-------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|-----------------|
| Smoke test              | 1. Install glTFast on an empty Unity Project.<br>2. Ensure that the package installs without errors.                                                                                                                                                                             | The package should install without any errors or warnings, and all necessary files should be present in the project. |
| Roundtrip test          | 1. Install glTFast on an empty Unity Project.<br>2. Import an asset to the project.<br>3. Export the asset using glTFast.<br>4. Import the exported asset back into the project.<br>5. Verify that the imported asset matches the original asset in terms of structure and content. | The imported asset should match the original asset exactly, with no loss of data or structure. |
| Validation test         | 1. Install glTFast on an empty Unity Project.<br>2. Create a simple scene with various asset types (e.g., meshes, textures, animations).<br>3. Export the scene using glTFast.<br>4. Import the exported scene back into the project.<br>5. Verify that all asset types are correctly imported and function as expected. | All asset types should be correctly imported and function as expected, with no errors or missing data. Animations are not expected to be exported. |
| Build & Compile test    | 1. Install glTFast on an empty Unity Project.<br>2. Create a simple scene with various asset types.<br>3. Export the scene using glTFast.<br>4. Build the project for a target platform (e.g., Windows, Android).<br>5. Run the built project and verify that all asset types are correctly loaded and function as expected. | The built project should run without errors, and all asset types should be correctly loaded and function as expected. |

## Assets

The following assets are required for the manual validation process and can be found in the samples repository:

[glTF-Sample-Assets](https://github.com/KhronosGroup/glTF-Sample-Assets).

| Asset Name              | glTF Feature(s) Demonstrated                                   |
|-------------------------|----------------------------------------------------------------|
| AlphaBlendModeTest      | Alpha blending, transparency, PBR materials                    |
| AnimatedMorphCube       | Morph targets (blendshapes), animation                         |
| AnimatedTriangle        | Animation, simple mesh                                         |
| Avocado                 | PBR, textures, metallic-roughness, normal maps                 |
| BoomBox                 | PBR, textures, metallic-roughness, normal maps                 |
| BoxTextured             | Simple mesh, PBR metallic-roughness, textures                  |
| BrainStem               | Skinning (skeletal animation), animation                       |
| ClearCoatTest           | KHR_materials_clearcoat extension                              |
| Duck                    | PBR, single mesh, textures                                     |
| EmissiveStrengthTest    | KHR_materials_emissive_strength extension                      |
| EnvironmentTest         | KHR_lights_punctual extension (environment, multiple lights)   |
| Fox                     | Skinning, animation, PBR, textures                             |
| IORTestGrid             | KHR_materials_ior extension                                    |
| Lantern                 | PBR, emission, alpha mask, transmission, textures              |
| MaterialsVariantsShoe   | KHR_materials_variants extension, material switching           |
| PointLightIntensityTest | KHR_lights_punctual extension (point lights)                   |
| SheenCloth              | KHR_materials_sheen extension                                  |
| SpecGlossVsMetalRough   | KHR_materials_pbrSpecularGlossiness extension                  |
| SpecularTest            | KHR_materials_specular extension                               |
| Sponza                  | Large scene, multiple meshes, PBR, textures                    |
| TextureTransformTest    | KHR_texture_transform extension, texture offset/scale/rotation |
| TransmissionTest        | KHR_materials_transmission extension (glass-like material)     |
| UnlitTest               | KHR_materials_unlit extension, unlit material                  |
