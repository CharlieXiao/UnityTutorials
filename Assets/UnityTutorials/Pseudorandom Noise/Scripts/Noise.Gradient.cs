using Unity.Mathematics;
using static Unity.Mathematics.math;

using static UnityTutorials.Pseudorandom_Noise.MathExtensions;

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

            // turbulence effect
            float4 EvaluateCombined(float4 value);

        }
        
        // the gradient is constant at lattice point
        public struct Value : IGradient
        {
            public float4 Evaluate(SmallXXHash4 hash, float4 x) => hash.Floats01A * 2.0f - 1.0f;
            public float4 Evaluate(SmallXXHash4 hash, float4 x,float4 y) => hash.Floats01A* 2.0f - 1.0f;
            public float4 Evaluate(SmallXXHash4 hash, float4 x,float4 y,float4 z) => hash.Floats01A* 2.0f - 1.0f;

            public float4 EvaluateCombined(float4 value) => value;
        }

        public struct Perlin : IGradient
        {
            public float4 Evaluate(SmallXXHash4 hash, float4 x) =>
                BaseGradients.Line(hash, x);

            public float4 Evaluate(SmallXXHash4 hash, float4 x, float4 y) => 
                BaseGradients.Square(hash, x, y) * (2f / 0.53528f);

            public float4 Evaluate(SmallXXHash4 hash, float4 x, float4 y, float4 z) =>
                BaseGradients.Octahedron(hash, x, y, z) * (1.0f / 0.56290f);

            public float4 EvaluateCombined(float4 value) => value;
        }

        public struct Simplex : IGradient
        {
            public float4 Evaluate (SmallXXHash4 hash, float4 x) =>
                BaseGradients.Line(hash, x) * (32f / 27f);

            public float4 Evaluate (SmallXXHash4 hash, float4 x, float4 y) =>
                BaseGradients.Circle(hash, x, y) * (5.832f / SQRT2);

            public float4 Evaluate (SmallXXHash4 hash, float4 x, float4 y, float4 z) =>
                BaseGradients.Sphere(hash, x, y, z) * (1024f / (125f * SQRT3));

            public float4 EvaluateCombined (float4 value) => value;
        }

        public struct FastSimplex : IGradient
        {
            public float4 Evaluate (SmallXXHash4 hash, float4 x) =>
                BaseGradients.Line(hash, x) * (32f / 27f);

            public float4 Evaluate (SmallXXHash4 hash, float4 x, float4 y) =>
                BaseGradients.SquareCircle(hash, x, y) * (5.832f / SQRT2);

            public float4 Evaluate (SmallXXHash4 hash, float4 x, float4 y, float4 z) =>
                BaseGradients.OctahedronSphere(hash, x, y, z) * (1024f / (125f * SQRT3));

            public float4 EvaluateCombined (float4 value) => value;
        }

        public static class BaseGradients
        {
            public static float4 Line(SmallXXHash4 hash, float4 x) =>
                (1.0f + hash.Floats01A) * select(-x, x, ((uint4)hash & (1 << 8)) == 0);
            
            static float4x2 SquareVectors (SmallXXHash4 hash) {
                float4x2 v;
                v.c0 = hash.Floats01A * 2f - 1f;
                v.c1 = 0.5f - abs(v.c0);
                v.c0 -= floor(v.c0 + 0.5f);
                return v;
            }
		
            static float4x3 OctahedronVectors (SmallXXHash4 hash) {
                float4x3 g;
                g.c0 = hash.Floats01A * 2f - 1f;
                g.c1 = hash.Floats01D * 2f - 1f;
                g.c2 = 1f - abs(g.c0) - abs(g.c1);
                float4 offset = max(-g.c2, 0f);
                g.c0 += select(-offset, offset, g.c0 < 0f);
                g.c1 += select(-offset, offset, g.c1 < 0f);
                return g;
            }

            static float4x2 CircleVectors(SmallXXHash4 hash)
            {
                float4 epsilon = hash.Floats01A;
                return float4x2(sin(2.0f * PI * epsilon), cos(2.0f * PI * epsilon));
            }

            static float4x3 SphereVectors(SmallXXHash4 hash)
            {
                float4 epsilon1 = hash.Floats01A;
                float4 epsilon2 = hash.Floats01D;
                float4 phi = 2.0f * PI * epsilon2;
                float4 cosTheta = 1.0f - 2.0f * epsilon1;
                float4 sinTheta = sqrt(1.0f - cosTheta * cosTheta);
                return float4x3(cos(phi) * sinTheta, sin(phi) * sinTheta, cosTheta);
            }

            public static float4 Square (SmallXXHash4 hash, float4 x, float4 y) {
                float4x2 v = SquareVectors(hash);
                return v.c0 * x + v.c1 * y;
            }
	
            public static float4 Circle (SmallXXHash4 hash, float4 x, float4 y)
            {
                // this is truly normally distributed, but takes longer to calculate sin and cos
                float4x2 v = CircleVectors(hash);
                // this is not truly normally distributed, many try something else to create a normally distributed vector
                // float4x2 v = SquareVectors(hash);
                return v.c0 * x + v.c1 * y;
            }

            public static float4 SquareCircle(SmallXXHash4 hash, float4 x, float4 y)
            {
                float4x2 v = SquareVectors(hash).NormalizeVectors();
                return v.c0 * x + v.c1 * y;
            }
	
            public static float4 Octahedron (
                SmallXXHash4 hash, float4 x, float4 y, float4 z
            ) {
                float4x3 v = OctahedronVectors(hash);
                return v.c0 * x + v.c1 * y + v.c2 * z;
            }

            public static float4 Sphere (SmallXXHash4 hash, float4 x, float4 y, float4 z)
            {
                float4x3 v = SphereVectors(hash);
                return v.c0 * x + v.c1 * y + v.c2 * z;
            }

            public static float4 OctahedronSphere(SmallXXHash4 hash, float4 x, float4 y, float4 z)
            {
                float4x3 v = OctahedronVectors(hash).NormalizeVectors();
                return v.c0 * x + v.c1 * y + v.c2 * z;
            }
        }

        public struct Turbulence<G> : IGradient where G : struct, IGradient
        {
            // when evaluating, just forward this to G
            public float4 Evaluate (SmallXXHash4 hash, float4 x) =>
                default(G).Evaluate(hash, x);

            public float4 Evaluate (SmallXXHash4 hash, float4 x, float4 y) =>
                default(G).Evaluate(hash, x, y);
            
            public float4 Evaluate (SmallXXHash4 hash, float4 x, float4 y, float4 z) =>
                default(G).Evaluate(hash, x, y, z);

            // add turbulence effect here
            public float4 EvaluateCombined (float4 value) =>
                abs(default(G).EvaluateCombined(value));
        }
    }
}