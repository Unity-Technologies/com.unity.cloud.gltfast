// SPDX-FileCopyrightText: 2024 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using GLTFast.Schema;
using UnityEngine;
#if USING_HDRP
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
#endif

namespace GLTFast.Export
{
    /// <summary>
    /// Provides conversion from Unity light components to glTF lights.
    /// </summary>
    public static class KhrLightsPunctual
    {
        /// <summary>
        /// Converts a Unity light component to a glTF light.
        /// </summary>
        /// <param name="uLight">Unity light component.</param>
        /// <returns>glTF light.</returns>
        public static LightPunctual ConvertToLight(Light uLight)
        {
            var light = new LightPunctual
            {
                name = uLight.name
            };

            var renderPipeline = RenderPipelineUtils.RenderPipeline;

            var lightType = uLight.type;

#if USING_HDRP
            HDAdditionalLightData lightHd = null;

            if (renderPipeline == RenderPipeline.HighDefinition)
            {
                lightHd = uLight.gameObject.GetComponent<HDAdditionalLightData>();
            }
#endif

            switch (lightType)
            {
                case LightType.Spot:
                    light.SetLightType(LightPunctual.Type.Spot);
                    light.spot = new SpotLight
                    {
                        outerConeAngle = uLight.spotAngle * Mathf.Deg2Rad * .5f,
                    };

#if USING_HDRP && !UNITY_6000_3_OR_NEWER
                    if (renderPipeline == RenderPipeline.HighDefinition)
                    {
                        // Up until Unity 6.2/HDRP 17.2 lightHd.innerSpotPercent was used
                        // instead of uLight.innerSpotAngle.
                        light.spot.innerConeAngle = lightHd != null
                            ? uLight.spotAngle * Mathf.Deg2Rad * .5f * lightHd.innerSpotPercent01
                            : 0;
                    }
                    else
#endif
                    {
                        light.spot.innerConeAngle = uLight.innerSpotAngle * Mathf.Deg2Rad * .5f;
                    }
                    break;
                case LightType.Directional:
                    light.SetLightType(LightPunctual.Type.Directional);
                    break;
                case LightType.Point:
                    light.SetLightType(LightPunctual.Type.Point);
                    break;
                case LightType.Rectangle:
                case LightType.Disc:
                default:
                    light.SetLightType(LightPunctual.Type.Spot);
                    light.spot = new SpotLight
                    {
                        outerConeAngle = 45 * Mathf.Deg2Rad * .5f,
                        innerConeAngle = 35 * Mathf.Deg2Rad * .5f
                    };
                    break;
            }

            light.LightColor = uLight.color.linear;
            light.range = uLight.range;

            // Set Light intensity
            switch (renderPipeline)
            {
                case RenderPipeline.BuiltIn:
                    light.intensity = uLight.intensity * Mathf.PI;
                    break;
                case RenderPipeline.Universal:
                    light.intensity = uLight.intensity;
                    break;
#if USING_HDRP
                case RenderPipeline.HighDefinition:

                    if (lightHd == null)
                    {
                        light.intensity = uLight.intensity;
                    }
                    else
                    {
                        switch (lightType)
                        {
                            case LightType.Spot:
                            case LightType.Point:
                                light.intensity = LightUnitUtils.ConvertIntensity(uLight, uLight.intensity, uLight.lightUnit, LightUnit.Candela);
                                break;
                            case LightType.Directional:
                                light.intensity = LightUnitUtils.ConvertIntensity(uLight, uLight.intensity, uLight.lightUnit, LightUnit.Lux);
                                break;
                            case LightType.Rectangle:
                            default:
                                light.intensity = uLight.intensity;
                                break;
                        }
                    }
                    break;
#endif // USING_HDRP
                default:
                    light.intensity = uLight.intensity;
                    break;
            }

            return light;
        }
    }
}
