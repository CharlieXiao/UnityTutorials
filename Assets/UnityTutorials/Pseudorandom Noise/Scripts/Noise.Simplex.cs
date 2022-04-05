using Unity.Mathematics;
using static Unity.Mathematics.math;
using static UnityTutorials.Pseudorandom_Noise.MathExtensions;

namespace UnityTutorials.Pseudorandom_Noise
{
    // what is simplex really? 
    // A simplex is the simplest possible polytope—an object with flat sides—
    // that takes up space in all available dimensions.
    // line segment 2D, triangle in 2D, tetrahedron in 3D
    public static partial class Noise
    {
        // unlike voronoi and gradient noise, simplex noise use simplex lattice,
        // so this one is not compatible with our previously defined lattice interface
        // also, tiling is not available in Simplex Noise

        // the step function is different in Gradient based noise and Simplex based noise

        public struct Simplex1D<G> : INoise where G : struct, IGradient
        {
            public float4 GetNoise4(float4x3 positions, SmallXXHash4 hash, int frequency)
            {
                //manually apply the frequency
                positions *= frequency;
                // grid start point
                int4 x0 = (int4)floor(positions.c0);
                int4 x1 = x0 + 1;

                return default(G).EvaluateCombined(
                    Kernel(hash.Eat(x0), x0, positions) +
                    Kernel(hash.Eat(x1), x1, positions)
                );
            }

            // tent filter f(x) = 1-|x|
            static float4 Kernel(SmallXXHash4 hash, float4 lx, float4x3 positions)
            {
                // gradient point
                float4 x = positions.c0 - lx;
                float4 f = 1.0f - x * x;
                // 1-|x| or 1-x^2 -> C0 smooth
                // (1-x^2)^2 -> C1 smooth
                // (1-x^2)^3 -> C2 smooth
                f = f * f * f;
                return f * default(G).Evaluate(hash, x);
            }
        }

        public struct Simplex2D<G> : INoise where G : struct, IGradient
        {
            public float4 GetNoise4(float4x3 positions, SmallXXHash4 hash, int frequency)
            {
                // scale down the frequency
                // because the total area is less than square
                // so we need to rescale to match the original frequency
                positions *= frequency * (1.0f / SQRT3);
                // 这里假设的是positions已经是菱形的中心了
                float4 skew = (positions.c0 + positions.c2) * ((SQRT3 - 1.0f) / 2.0f);
                // 进行逆变换，将菱形转换为正方形
                float4 sx = positions.c0 + skew, sz = positions.c2 + skew;
                // 计算正方形lattice的四个坐标
                int4
                    x0 = (int4)floor(sx),
                    z0 = (int4)floor(sz),
                    x1 = x0 + 1,
                    z1 = z0 + 1;
                // 判断当前的三角形（上三角还是下三角）
                bool4 xGz = sx - x0 > sz - z0;
                int4 xC = select(x0, x1, xGz), zC = select(z1, z0, xGz);
                // z0 x1 与 z1 x0 只需要选择一个就可以了
                // 一个三角形包含三条便
                SmallXXHash4 h0 = hash.Eat(x0), h1 = hash.Eat(x1), hc = SmallXXHash4.Select(h0, h1, xGz);
                // x0 x1 
                // z0 z1
                // so combine in four direction
                return default(G).EvaluateCombined(
                    Kernel(h0.Eat(z0), x0, z0, positions) + 
                    Kernel(h1.Eat(z1), x1, z1, positions) + 
                    Kernel(hc.Eat(zC), xC, zC, positions)
                );
            }

            static float4 Kernel(SmallXXHash4 hash, float4 lx, float4 lz, float4x3 positions)
            {
                float4 unskew = (lx + lz) * ((3.0f - SQRT3) / 6.0f);
                // (x,z) is the center of the lattice
                // here is restored to cube, so that all three value is the same
                // 将positions转换回菱形坐标
                float4 x = positions.c0 - lx + unskew;
                float4 z = positions.c2 - lz + unskew;
                // distance falloff function f(d) = (1-d)^3
                float4 f = 0.5f - x * x - z * z;
                // rescale to [0,1] ，本来函数的最小值
                f = f * f * f * 8.0f;
                // we must make the result is [0,1]
                return max(0.0f, f) * default(G).Evaluate(hash, x, z);
                // return max(0.0f,f);
            }
        }

        public struct Simplex3D<G> : INoise where G : struct, IGradient
        {
            public float4 GetNoise4(float4x3 positions, SmallXXHash4 hash, int frequency)
            {
                positions *= frequency * 0.6f;
                float4 skew = (positions.c0 + positions.c1 + positions.c2) * (1.0f / 3.0f);
                float4
                    sx = positions.c0 + skew,
                    sy = positions.c1 + skew,
                    sz = positions.c2 + skew;
                int4
                    x0 = (int4)floor(sx),
                    y0 = (int4)floor(sy),
                    z0 = (int4)floor(sz),
                    x1 = x0 + 1,
                    y1 = y0 + 1,
                    z1 = z0 + 1;
                bool4
                    xGy = sx - x0 > sy - y0,
                    xGz = sx - x0 > sz - z0,
                    yGz = sy - y0 > sz - z0;
                bool4
                    xA = xGy & xGz,
                    xB = xGy | (xGz & yGz),
                    yA = !xGy & yGz,
                    yB = !xGy | (xGz & yGz),
                    zA = (xGy & !xGz) | (!xGy & !yGz),
                    zB = !(xGz & yGz);
                int4
                    xCA = select(x0, x1, xA),
                    xCB = select(x0, x1, xB),
                    yCA = select(y0, y1, yA),
                    yCB = select(y0, y1, yB),
                    zCA = select(z0, z1, zA),
                    zCB = select(z0, z1, zB);
                SmallXXHash4
                    h0 = hash.Eat(x0),
                    h1 = hash.Eat(x1),
                    hA = SmallXXHash4.Select(h0, h1, xA),
                    hB = SmallXXHash4.Select(h0, h1, xB);
                
                return default(G).EvaluateCombined(
                    Kernel(h0.Eat(y0).Eat(z0), x0, y0, z0, positions) +
                    Kernel(h1.Eat(y1).Eat(z1), x1, y1, z1, positions) +
                    Kernel(hA.Eat(yCA).Eat(zCA), xCA, yCA, zCA, positions) +
                    Kernel(hB.Eat(yCB).Eat(zCB), xCB, yCB, zCB, positions)
                );
            }

            static float4 Kernel(SmallXXHash4 hash, float4 lx, float4 ly, float4 lz, float4x3 positions)
            {
                float4 unskew = (lx + ly + lz) * (1.0f / 6.0f);
                float4
                    x = positions.c0 - lx + unskew,
                    y = positions.c1 - ly + unskew,
                    z = positions.c2 - lz + unskew;
                float4 f = 0.5f - x * x - y * y - z * z;
                f = f * f * f * 8.0f;
                return max(0.0f, f) * default(G).Evaluate(hash, x, y, z);
                // return max(0.0f, f);
            }
        }
    }
}