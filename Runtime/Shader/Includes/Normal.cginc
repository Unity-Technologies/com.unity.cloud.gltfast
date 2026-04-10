//UNITY_SHADER_NO_UPGRADE
#ifndef GLTF_HLSL_INCLUDE_NORMAL
#define GLTF_HLSL_INCLUDE_NORMAL

/// This is a replacement for UnityStandardUtils UnpackScaleNormalRGorAG to use XYZ normals even with DXT5nm enabled
void NormalInTangentSpace_float(UnityTexture2D normal_texture, float2 uv, float normal_scale, bool front_face, out float3 normal)
{
    float4 packed_normal = tex2D(normal_texture, uv);
    normal.xy = packed_normal.xy * 2 - 1;
    normal.xy *= normal_scale;
    normal.z = sqrt(1.0 - saturate(dot(normal.xy, normal.xy)));
#if defined(UNIVERSAL_PIPELINE_CORE_INCLUDED)
    normal *= lerp(-1, 1, front_face);
#endif
}

#endif
