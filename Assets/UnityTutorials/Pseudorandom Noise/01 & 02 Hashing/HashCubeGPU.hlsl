#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
    StructuredBuffer<uint> _Hashes;
    StructuredBuffer<float3> _Positions;
#endif

float4 _Config;

void ConfigureProcedural()
{
    #if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)

        unity_ObjectToWorld = 0.0;
        // translate, move the cube
        unity_ObjectToWorld._m03_m13_m23_m33 = float4(_Positions[unity_InstanceID],1.0);
        // scale to 1.0 / resolution
        // z offset
        unity_ObjectToWorld._m13 += _Config.z * ((1.0 / 255.0) * (_Hashes[unity_InstanceID] >> 24) - 0.5);
        unity_ObjectToWorld._m00_m11_m22 = _Config.y;
    #endif
}

float3 GetHashColor()
{

    #if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
        // return _Positions[unity_InstanceID].xyz;
        uint hash = _Hashes[unity_InstanceID];
        // hash 的范围是 [0,resolution*resolution)
        // 这里直接进行一个归一化就行了
        return (1.0 / 255.0) * float3(
            hash & 255,
            (hash >> 8) & 255,
            (hash >> 16) & 255);
    #else
        return float3(1.0,0.0,1.0);
    #endif
}

void ShaderGraphFunction_float (float3 In, out float3 Out, out float3 Color) {
    Out = In;
    Color = GetHashColor();
}

void ShaderGraphFunction_half (half3 In, out half3 Out, out half3 Color) {
    Out = In;
    Color = GetHashColor();
}

/*
#pragma instancing_options assumeuniformscaling procedural:ConfigureProcedural
#pragma editor_sync_compilation

Out = In;
 */