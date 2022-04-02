#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
    StructuredBuffer<uint> _Hashes;
#endif

float4 _Config;

void ConfigureProcedural()
{
    #if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
        // unity_InstanceID / resolution
        // safe floor, make sure the value is strictly small than unity_InstanceID / _Config.y
        float v = floor(_Config.y * (unity_InstanceID + 0.5));
        // unity_InstanceID % resolution = unity_InstanceID - v * resolution
        float u = unity_InstanceID - _Config.x * v;

        unity_ObjectToWorld = 0.0;
        // translate, move the cube
        unity_ObjectToWorld._m03_m13_m23_m33 = float4(
            (u + 0.5) * _Config.y - 0.5,
            // vertical offset
            _Config.z * (1.0 / 255.0) * (_Hashes[unity_InstanceID] >> 24) - 0.5,
            (v + 0.5) * _Config.y - 0.5,
            1.0
        );
        // scale to 1.0 / resolution
        unity_ObjectToWorld._m00_m11_m22 = _Config.y;
    #endif
}

float3 GetHashColor()
{
    #if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
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