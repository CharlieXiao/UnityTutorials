using System.EnterpriseServices.Internal;
using Unity.Mathematics;

using static Unity.Mathematics.math;

namespace UnityTutorials.Pseudorandom_Noise
{
    public static class MathExtensions
    {
        // now we can call trs.TransformVectors(...)
        // another syntax suger, so sweet
        public static float4x3 TransformVectors(
            this float3x4 trs, float4x3 p, float w = 1.0f) => float4x3(
            trs.c0.x * p.c0 + trs.c1.x * p.c1 + trs.c2.x * p.c2 + trs.c3.x * w,
            trs.c0.y * p.c0 + trs.c1.y * p.c1 + trs.c2.y * p.c2 + trs.c3.y * w,
            trs.c0.z * p.c0 + trs.c1.z * p.c1 + trs.c2.z * p.c2 + trs.c3.z * w);
        
        // return the first three row of matrix
        // expect last row to be (0,0,0,1) in all the case
        public static float3x4 GetCompactMatrix(this float4x4 m) => float3x4(m.c0.xyz, m.c1.xyz, m.c2.xyz, m.c3.xyz);

        public static float4x2 NormalizeVectors(this float4x2 v)
        {
            float4 invLength = rsqrt(v.c0 * v.c0 + v.c1 * v.c1);
            return float4x2(v.c0 * invLength, v.c1 * invLength);
        }
        
        public static float4x3 NormalizeVectors(this float4x3 v)
        {
            float4 invLength = rsqrt(v.c0 * v.c0 + v.c1 * v.c1 + v.c2 * v.c2);
            return float4x3(v.c0 * invLength, v.c1 * invLength,v.c2 * invLength);
        }

        public const float SQRT3 = 1.7320508075688772935274463415059f;
    }
}