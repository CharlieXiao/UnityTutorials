using Unity.Mathematics;

using static Unity.Mathematics.math;

namespace UnityTutorials.Pseudorandom_Noise
{
    public static partial class Noise
    {
        public interface IVoronoiFunction
        {
            public float4 Evaluate(float4x2 minima);
        }

        public interface IVoronoiDistance
        {
            public float4 GetDistance(float4 x);
            public float4 GetDistance(float4 x,float4 y);
            public float4 GetDistance(float4 x,float4 y,float4 z);

            public float4x2 Finalize1D(float4x2 minima);
            public float4x2 Finalize2D(float4x2 minima);
            public float4x2 Finalize3D(float4x2 minima);

        }

        public struct F1 : IVoronoiFunction
        {
            public float4 Evaluate(float4x2 minima) => minima.c0;
        }

        public struct F2 : IVoronoiFunction
        {
            public float4 Evaluate(float4x2 minima) => minima.c1;
        }

        public struct F2MinusF1 : IVoronoiFunction
        {
            public float4 Evaluate(float4x2 minima) => minima.c1 - minima.c0;
        }

        public struct Worley : IVoronoiDistance
        {
            public float4 GetDistance(float4 x) => abs(x);

            public float4 GetDistance(float4 x, float4 y) => sqrt(x * x + y * y);

            public float4 GetDistance(float4 x, float4 y, float4 z) => sqrt(x * x + y * y + z * z);

            public float4x2 Finalize1D(float4x2 minima) => minima;

            public float4x2 Finalize2D(float4x2 minima)
            {
                minima.c0 = min(minima.c0, 1.0f);
                minima.c1 = min(minima.c1, 1.0f);
                return minima;
            }

            public float4x2 Finalize3D(float4x2 minima) => Finalize2D(minima);
        }

        public struct Chebyshev : IVoronoiDistance
        {
            public float4 GetDistance(float4 x) => abs(x);

            public float4 GetDistance(float4 x, float4 y) => max(abs(x), abs(y));

            public float4 GetDistance(float4 x, float4 y, float4 z) => max(max(abs(x), abs(y)), abs(z));

            public float4x2 Finalize1D(float4x2 minima) => minima;

            public float4x2 Finalize2D(float4x2 minima) => minima;

            public float4x2 Finalize3D(float4x2 minima) => minima;
        }

        public struct SquaredEuclidean : IVoronoiDistance
        {
            public float4 GetDistance(float4 x) => abs(x);

            public float4 GetDistance(float4 x, float4 y) => x * x + y * y;

            public float4 GetDistance(float4 x, float4 y, float4 z) => x * x + y * y + z * z;

            public float4x2 Finalize1D(float4x2 minima) => minima;

            public float4x2 Finalize2D(float4x2 minima)
            {
                minima.c0 = min(minima.c0, 1.0f);
                minima.c1 = min(minima.c1, 1.0f);
                return minima;
            }

            public float4x2 Finalize3D(float4x2 minima) => Finalize2D(minima);
        }

        // keep track of first minimum and second minimum
        static float4x2 UpdateVoronoiMinima(float4x2 minima, float4 distances)
        {
            // update minima
            // minima.c0 means first minimum value
            // minima.c1 means second minimum value
            // minima.c0 < minima.c1
            // if (minima.c0 > distances)
            // {
            //     minima.c1 = minima.c0;
            //     minima.c0 = distances;
            // }
            // else
            // {
            //     if (minima.c1 > distances)
            //     {
            //         minima.c1 = distances;
            //     }
            // }
            bool4 updateC0 = distances < minima.c0;
            bool4 updateC1 = distances < minima.c1;
            // select(a,b,c) means c ? b : a
            minima.c1 = select(select(minima.c1, distances, updateC1),minima.c0, updateC0);
            minima.c0 = select(minima.c0, distances, updateC0);
            return minima;
        }

        // another kind of noise, its not based on gradient
        public struct Voronoi1D<L,F,D> : INoise 
            where L : struct, ILattice
            where F : struct, IVoronoiFunction
            where D : struct, IVoronoiDistance
        {
            public float4 GetNoise4(float4x3 positions, SmallXXHash4 hash, int frequency)
            {
                var l = default(L);
                var f = default(F);
                var d = default(D);
                LatticeSpan4 x = l.GetLatticeSpan4(positions.c0, frequency);
                // 计算随机数咯
                // 最大距离初始化为2.0f
                float4x2 minima = 2.0f;
                // 计算p0这个lattice及其周围的
                // 由于是一维情况，周围就包含两个值
                for (int u = -1; u <= 1; ++u)
                {
                    // make  sure the noise suit for tiling
                    SmallXXHash4 h = hash.Eat(l.ValidateSingleStep(x.p0+u,frequency));
                    minima = UpdateVoronoiMinima(minima, d.GetDistance(h.Floats01A + u - x.g0));
                }
                // SmallXXHash4 h = hash.Eat(x.p0);
                // voronoi point is h.Floats01A - x.g0
                return f.Evaluate(d.Finalize1D(minima));
            }
        }
        
        public struct Voronoi2D<L,F,D> : INoise 
            where L : struct, ILattice
            where F : struct, IVoronoiFunction
            where D : struct, IVoronoiDistance
        {
            public float4 GetNoise4(float4x3 positions, SmallXXHash4 hash, int frequency)
            {
                var l = default(L);
                var f = default(F);
                var d = default(D);
                LatticeSpan4 
                    x = l.GetLatticeSpan4(positions.c0, frequency),
                    z = l.GetLatticeSpan4(positions.c2, frequency);

                float4x2 minima = 2.0f;
                // for 2D, every lattice has 8 neighbour lattice, so we have to loop twice
                for (int u = -1; u <= 1; ++u)
                {
                    SmallXXHash4 hx = hash.Eat(l.ValidateSingleStep(x.p0+u,frequency));
                    float4 xOffset = u - x.g0;
                    for (int v = -1; v <= 1; ++v)
                    {
                        SmallXXHash4 h = hx.Eat(l.ValidateSingleStep(z.p0 + v, frequency));
                        float4 zOffset = v - z.g0;
                        minima = UpdateVoronoiMinima(minima, d.GetDistance(h.Floats01A + xOffset,h.Floats01B + zOffset));
                        minima = UpdateVoronoiMinima(minima, d.GetDistance(h.Floats01C + xOffset,h.Floats01D + zOffset));
                    }
                }
                // clamp minima to 1.0
                return f.Evaluate(d.Finalize2D(minima));
            }
        }
        
        public struct Voronoi3D<L,F,D> : INoise 
            where L : struct, ILattice
            where F : struct, IVoronoiFunction
            where D : struct, IVoronoiDistance
        {
            public float4 GetNoise4(float4x3 positions, SmallXXHash4 hash, int frequency)
            {
                var l = default(L);
                var f = default(F);
                var d = default(D);
                LatticeSpan4 
                    x = l.GetLatticeSpan4(positions.c0, frequency),
                    y = l.GetLatticeSpan4(positions.c1, frequency),
                    z = l.GetLatticeSpan4(positions.c2, frequency);
                float4x2 minima = 2f;
                // loop through 27 cells
                for (int u = -1; u <= 1; u++) {
                    SmallXXHash4 hx = hash.Eat(l.ValidateSingleStep(x.p0 + u, frequency));
                    float4 xOffset = u - x.g0;
                    for (int v = -1; v <= 1; v++) {
                        SmallXXHash4 hy = hx.Eat(l.ValidateSingleStep(y.p0 + v, frequency));
                        float4 yOffset = v - y.g0;
                        for (int w = -1; w <= 1; w++) {
                            SmallXXHash4 h =
                                hy.Eat(l.ValidateSingleStep(z.p0 + w, frequency));
                            float4 zOffset = w - z.g0;
                            minima = UpdateVoronoiMinima(minima, d.GetDistance(
                                h.GetBitsAsFloats01(5,0) + xOffset,
                                h.GetBitsAsFloats01(5,5) + yOffset,
                                h.GetBitsAsFloats01(5,10) + zOffset
                            ));
                            minima = UpdateVoronoiMinima(minima, d.GetDistance(
                                h.GetBitsAsFloats01(5,15) + xOffset,
                                h.GetBitsAsFloats01(5,20) + yOffset,
                                h.GetBitsAsFloats01(5,25) + zOffset
                            ));
                        }
                    }
                }
                return f.Evaluate(d.Finalize3D(minima));
            }
        }
    }
}