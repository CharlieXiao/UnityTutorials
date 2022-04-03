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
            public float4 g0,g1;
            public float4 t;
        }

        enum ContinuousOrder
        {
            C0,C1,C2
        }

        private static LatticeSpan4 GetLatticeSpan4(float4 coords,ContinuousOrder c = ContinuousOrder.C2)
        {
            float4 points = floor(coords);
            LatticeSpan4 span;
            span.p0 = (int4)points;
            span.p1 = span.p0 + 1;
            // relative coordinates
            span.g0 = coords - span.p0;
            span.g1 = span.g0 - 1.0f;
            float4 t = coords - points;
            switch (c)
            {
                // smooth function from [0,1]
                default:
                case ContinuousOrder.C0:
                    // linear, c0 continuous
                    // y = x
                    span.t = t;
                    break;
                case ContinuousOrder.C1:
                    // t * t * (3.0f - (2.0f * t))
                    // y = 3x^2-2x^3
                    span.t = smoothstep(0.0f, 1.0f, t);
                    break;
                case ContinuousOrder.C2:
                    // y = 6x^5-15x^4+10x^3
                    span.t = t * t * t * (t * (t * 6.0f - 15.0f) + 10.0f);
                    break;
            }
            return span;
        }

        // one kind of Noise, so implement interface INoise
        public struct Lattice1D<G> : INoise where G : struct, IGradient
        {
            public float4 GetNoise4(float4x3 positions, SmallXXHash4 hash)
            {
                LatticeSpan4 x = GetLatticeSpan4(positions.c0);
                var g = default(G);
                return lerp(g.Evaluate(hash.Eat(x.p0),x.g0), g.Evaluate(hash.Eat(x.p1),x.g1), x.t);
            }
        }

        public struct Lattice2D<G> : INoise where G : struct, IGradient
        {
            public float4 GetNoise4(float4x3 positions, SmallXXHash4 hash)
            {
                LatticeSpan4 x = GetLatticeSpan4(positions.c0), z = GetLatticeSpan4(positions.c2);
                // bilinear interpolate
                SmallXXHash4 h0 = hash.Eat(x.p0),h1 = hash.Eat(x.p1);
                var g = default(G);
                return lerp(
                    lerp(
                        g.Evaluate(h0.Eat(z.p0), x.g0, z.g0),
                        g.Evaluate(h0.Eat(z.p1), x.g0, z.g1),
                        z.t
                    ),
                    lerp(
                        g.Evaluate(h1.Eat(z.p0), x.g1, z.g0),
                        g.Evaluate(h1.Eat(z.p1), x.g1, z.g1),
                        z.t
                    ),
                    x.t
                );
            }
        }

        public struct Lattice3D<G> : INoise where G : struct, IGradient
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
                var g = default(G);
                return lerp(
                    lerp(
                        lerp(
                            g.Evaluate(h00.Eat(z.p0), x.g0, y.g0, z.g0),
                            g.Evaluate(h00.Eat(z.p1), x.g0, y.g0, z.g1),
                            z.t
                        ),
                        lerp(
                            g.Evaluate(h01.Eat(z.p0), x.g0, y.g1, z.g0),
                            g.Evaluate(h01.Eat(z.p1), x.g0, y.g1, z.g1),
                            z.t
                        ),
                        y.t
                    ),
                    lerp(
                        lerp(
                            g.Evaluate(h10.Eat(z.p0), x.g1, y.g0, z.g0),
                            g.Evaluate(h10.Eat(z.p1), x.g1, y.g0, z.g1),
                            z.t
                        ),
                        lerp(
                            g.Evaluate(h11.Eat(z.p0), x.g1, y.g1, z.g0),
                            g.Evaluate(h11.Eat(z.p1), x.g1, y.g1, z.g1),
                            z.t
                        ),
                        y.t
                    ),
                    x.t
                );
            }
        }
    }
}