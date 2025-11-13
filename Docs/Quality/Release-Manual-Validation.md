# Release Manual Validation

For the glTFast strategy, manual validation is required before a release can be made. This process ensures that all changes are thoroughly tested and verified.

## Test Suite

This table outlines the tests that need to be performed before releasing the glTFast package.
For evert test a random asset is chosen from the assets list below in order to avoid the pesticide effect from running the same tests with the same assets over and over.

| Test Name               | Description                                                                                                                                                                                                                                                                         | Expected Result |
|-------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|-----------------|
| Smoke test              | 1. Install glTFast on an empty Unity Project.<br>2. Ensure that the package installs without errors.                                                                                                                                                                             | The package should install without any errors or warnings, and all necessary files should be present in the project. |
| Roundtrip test          | 1. Install glTFast on an empty Unity Project.<br>2. Import an asset to the project.<br>3. Export the asset using glTFast.<br>4. Import the exported asset back into the project.<br>5. Verify that the imported asset matches the original asset in terms of structure and content. | The imported asset should match the original asset exactly, with no loss of data or structure. |
| Animated glTF test      | 1. Install glTFast on an empty Unity Project.<br>2. Create a simple scene with an animated glTF asset.<br>3. Play the scene and verify that the animation functions as expected.<br>4. Export the scene using glTFast.<br>5. Import the exported scene back into the project. | The animated glTF asset should function correctly in both the original and imported scenes, with no errors or unexpected behavior. |
| Validation test         | 1. Install glTFast on an empty Unity Project.<br>2. Create a simple scene with various asset types (e.g., meshes, textures, animations).<br>3. Export the scene using glTFast.<br>4. Import the exported scene back into the project.<br>5. Verify that all asset types are correctly imported and function as expected. | All asset types should be correctly imported and function as expected, with no errors or missing data. |
| Build & Compile test    | 1. Install glTFast on an empty Unity Project.<br>2. Create a simple scene with various asset types.<br>3. Export the scene using glTFast.<br>4. Build the project for a target platform (e.g., Windows, Android).<br>5. Run the built project and verify that all asset types are correctly loaded and function as expected. | The built project should run without errors, and all asset types should be correctly loaded and function as expected. |

## Assets

The following assets are required for the manual validation process and can be found in the samples repository:

[glTF-Sample-Assets](https://github.com/KhronosGroup/glTF-Sample-Assets).

| Asset Name              | glTF Feature(s) Demonstrated                                   |
|-------------------------|----------------------------------------------------------------|
| BoxTextured             | Simple mesh, PBR metallic-roughness, textures                  |
| AlphaBlendModeTest      | Alpha blending, transparency, PBR materials                    |
| BoomBox                 | PBR, textures, metallic-roughness, normal maps                 |
| Avocado                 | PBR, textures, metallic-roughness, normal maps                 |
| Duck                    | PBR, single mesh, textures                                     |
| Lantern                 | PBR, emission, alpha mask, transmission, textures              |
| Sponza                  | Large scene, multiple meshes, PBR, textures                    |
| TextureTransformTest    | KHR_texture_transform extension, texture offset/scale/rotation |
| SpecGlossVsMetalRough   | KHR_materials_pbrSpecularGlossiness extension                  |
| MaterialsVariantsShoe   | KHR_materials_variants extension, material switching           |
| AnimatedMorphCube       | Morph targets (blendshapes), animation                         |
| AnimatedTriangle        | Animation, simple mesh                                         |
| BrainStem               | Skinning (skeletal animation), animation                       |
| Fox                     | Skinning, animation, PBR, textures                             |
| UnlitTest               | KHR_materials_unlit extension, unlit material                  |
| TransmissionTest        | KHR_materials_transmission extension (glass-like material)     |
| ClearCoatTest           | KHR_materials_clearcoat extension                              |
| SheenCloth              | KHR_materials_sheen extension                                  |
| IORTestGrid             | KHR_materials_ior extension                                    |
| EmissiveStrengthTest    | KHR_materials_emissive_strength extension                      |
| SpecularTest            | KHR_materials_specular extension                               |
| PointLightIntensityTest | KHR_lights_punctual extension (point lights)                   |
| EnvironmentTest         | KHR_lights_punctual extension (environment, multiple lights)   |
