using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace UnityTutorials.Pseudorandom_Noise
{
    // this file also contains definition for Noise
    // some kind of distributed code :)
    public static partial class Noise
    {
        private struct LatticeSpan4
        {
            public int4 p0, p1;
            public float4 t;
        }

        enum ContinuousOrder
        {
            C0,C1,C2
        }

        private static LatticeSpan4 GetLatticeSpan4(float4 coords,ContinuousOrder c = ContinuousOrder.C1)
        {
            float4 points = floor(coords);
            LatticeSpan4 span;
            span.p0 = (int4)points;
            span.p1 = span.p0 + 1;
            float4 t = coords - points;
            switch (c)
            {
                default:
                case ContinuousOrder.C0:
                    span.t = t;
                    break;
                case ContinuousOrder.C1:
                    span.t = smoothstep(0.0f, 1.0f, t);
                    break;
                case ContinuousOrder.C2:
                    span.t = t * t * t * (t * (t * 6.0f - 15.0f) + 10.0f);
                    break;
            }
            return span;
        }

        // one kind of Noise, so implement interface INoise
        public struct Lattice1D : INoise
        {
            public float4 GetNoise4(float4x3 positions, SmallXXHash4 hash)
            {
                LatticeSpan4 x = GetLatticeSpan4(positions.c0);
                return lerp(hash.Eat(x.p0).Floats01A, hash.Eat(x.p1).Floats01A, x.t) * 2.0f - 1.0f;
            }
        }

        public struct Lattice2D : INoise
        {
            public float4 GetNoise4(float4x3 positions, SmallXXHash4 hash)
            {
                LatticeSpan4 x = GetLatticeSpan4(positions.c0), z = GetLatticeSpan4(positions.c2);
                // bilinear interpolate
                SmallXXHash4 h0 = hash.Eat(x.p0),h1 = hash.Eat(x.p1);
                return lerp(
                    lerp(h0.Eat(z.p0).Floats01A, h0.Eat(z.p1).Floats01A, z.t),
                    lerp(h1.Eat(z.p0).Floats01A, h1.Eat(z.p1).Floats01A, z.t), x.t) * 2.0f - 1.0f;
            }
        }

        public struct Lattice3D : INoise
        {
            public float4 GetNoise4(float4x3 positions, SmallXXHash4 hash)
            {
                LatticeSpan4 
                    x = GetLatticeSpan4(positions.c0),
                    y = GetLatticeSpan4(positions.c1),
                    z = GetLatticeSpan4(positions.c2);
                SmallXXHash4
                    h0 = hash.Eat(x.p0),
                    h1 = hash.Eat(x.p1),
                    h00 = h0.Eat(y.p0),
                    h01 = h0.Eat(y.p1),
                    h10 = h1.Eat(y.p0),
                    h11 = h1.Eat(y.p1);
                
                // bicubic interpolation
                return lerp(
                    lerp(
                        lerp(h00.Eat(z.p0).Floats01A, h00.Eat(z.p1).Floats01A, z.t),
                        lerp(h01.Eat(z.p0).Floats01A, h01.Eat(z.p1).Floats01A, z.t),
                        y.t
                    ),
                    lerp(
                        lerp(h10.Eat(z.p0).Floats01A, h10.Eat(z.p1).Floats01A, z.t),
                        lerp(h11.Eat(z.p0).Floats01A, h11.Eat(z.p1).Floats01A, z.t),
                        y.t
                    ),
                    x.t
                ) * 2f - 1f;
            }
        }
    }
}