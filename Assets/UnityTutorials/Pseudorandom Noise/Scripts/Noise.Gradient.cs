using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace UnityTutorials.Pseudorandom_Noise
{
    public static partial class Noise
    {
        public interface IGradient
        {
            // 定义了一个关于x(1D)的函数，x代表一个坐标
            float4 Evaluate(SmallXXHash4 hash, float4 x);

            float4 Evaluate(SmallXXHash4 hash, float4 x, float4 y);
            
            float4 Evaluate(SmallXXHash4 hash, float4 x, float4 y,float4 z);

        }
        
        // the gradient is constant at lattice point
        public struct Value : IGradient
        {
            public float4 Evaluate(SmallXXHash4 hash, float4 x) => hash.Floats01A * 2.0f - 1.0f;
            public float4 Evaluate(SmallXXHash4 hash, float4 x,float4 y) => hash.Floats01A* 2.0f - 1.0f;
            public float4 Evaluate(SmallXXHash4 hash, float4 x,float4 y,float4 z) => hash.Floats01A* 2.0f - 1.0f;

        }

        public struct Perlin : IGradient
        {
            public float4 Evaluate(SmallXXHash4 hash, float4 x)
            {
                // varied by hash, this add more variable compared to Value
                // using the 9 bit of hash 
                // using the ninth to choose whether the gradient is negative or positive
                return (1.0f + hash.Floats01A) * select(-x, x, ((uint4)hash & 1 << 8) == 0);
            }

            public float4 Evaluate(SmallXXHash4 hash, float4 x, float4 y)
            {
                // this make the gradient along x
                // return select(-x, x, ((uint4) hash & 1) == 0);
                float4 gx = hash.Floats01A * 2f - 1f;
                // x,y from uniform distribution?
                float4 gy = 0.5f - abs(gx);
                gx -= floor(gx + 0.5f);
                return (gx * x + gy * y) * 2.0f / 0.53528f;
            }

            public float4 Evaluate(SmallXXHash4 hash, float4 x, float4 y, float4 z)
            {
                float4 gx = hash.Floats01A * 2f - 1f, gy = hash.Floats01D * 2f - 1f;
                float4 gz = 1f - abs(gx) - abs(gy);
                float4 offset = max(-gz, 0f);
                gx += select(-offset, offset, gx < 0f);
                gy += select(-offset, offset, gy < 0f);
                // the gradient can have any direction,
                // but the total function is still continuous
                return (gx * x + gy * y + gz * z) / 0.5629f;

            }
        }
    }
}