using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

using static Unity.Mathematics.math;
using float3 = Unity.Mathematics.float3;
using float3x4 = Unity.Mathematics.float3x4;

namespace UnityTutorials.Pseudorandom_Noise._01_Hashing
{
    public static class Shapes
    {
        [BurstCompile(FloatPrecision.Standard,FloatMode.Fast,CompileSynchronously = true)]
        public struct Job : IJobFor
        {
            [WriteOnly] public NativeArray<float3> positions, normals;

            [ReadOnly] public float3x4 positionTRS;

            public float resolution, invResolution;

            public void Execute (int i) {
                float2 uv;
                uv.y = floor(invResolution * (i + 0.5f));
                uv.x = invResolution * (i - resolution * uv.y + 0.5f) - 0.5f;
                uv.y = invResolution * (uv.y + 0.5f) - 0.5f;

                positions[i] = mul(positionTRS,float4(uv.x, 0.0f, uv.y,1.0f));
                normals[i] = normalize(mul(positionTRS, float4(0f, 1f, 0f, 1f)));
            }

            public static JobHandle ScheduleParallel(
                NativeArray<float3> positions, 
                NativeArray<float3> normals,
                int resolution,
                float4x4 trs,
                JobHandle dependency)
            {
                return new Job
                {
                    positions = positions,
                    normals = normals,
                    resolution = resolution,
                    invResolution = 1.0f / resolution,
                    positionTRS = float3x4(trs.c0.xyz,trs.c1.xyz,trs.c2.xyz,trs.c3.xyz)
                }.ScheduleParallel(positions.Length, resolution, dependency);
            }
        }
    }
}