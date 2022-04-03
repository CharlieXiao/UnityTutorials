﻿#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
    StructuredBuffer<float> _Noise;
    StructuredBuffer<float3> _Positions;
    StructuredBuffer<float3> _Normals;
#endif

float4 _Config;

void ConfigureProcedural()
{
    #if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)

        unity_ObjectToWorld = 0.0;
        // translate, move the cube
        unity_ObjectToWorld._m03_m13_m23_m33 = float4(_Positions[unity_InstanceID],1.0);
        // scale to 1.0 / resolution
        // normals displacement
        unity_ObjectToWorld._m03_m13_m23 += _Config.z * _Noise[unity_InstanceID] * _Normals[unity_InstanceID];
        // scale
        unity_ObjectToWorld._m00_m11_m22 = _Config.y;
    #endif
}

float3 GetNoiseColor()
{

    #if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
        float noise = _Noise[unity_InstanceID];
        return noise < 0.0 ? float3(-noise,0.0,0.0) : noise;
    #else
        return float3(1.0,0.0,1.0);
    #endif
}

void ShaderGraphFunction_float (float3 In, out float3 Out, out float3 Color) {
    Out = In;
    Color = GetNoiseColor();
}

void ShaderGraphFunction_half (half3 In, out half3 Out, out half3 Color) {
    Out = In;
    Color = GetNoiseColor();
}

/*
#pragma instancing_options assumeuniformscaling procedural:ConfigureProcedural
#pragma editor_sync_compilation

Out = In;
 */