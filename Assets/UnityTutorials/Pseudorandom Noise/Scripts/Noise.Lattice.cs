using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace UnityTutorials.Pseudorandom_Noise
{
    // this file also contains definition for Noise
    // some kind of distributed code :)
    public static partial class Noise
    {
        public struct LatticeSpan4
        {
            public int4 p0, p1;
            public float4 g0,g1;
            public float4 t;
        }
        
        public interface IStepFunction
        {
            public float4 Step(float4 a, float4 b, float4 x);
        }
        
        public interface ILattice
        {
            public LatticeSpan4 GetLatticeSpan4(float4 coordinates, int frequency);
        }
        
        public struct C0Step : IStepFunction
        {
            public float4 Step(float4 a, float4 b, float4 x) => saturate((x-a)/(b-a));
        }

        public struct C1Step : IStepFunction
        {
            public float4 Step(float4 a, float4 b, float4 x) => smoothstep(a, b, x);
        }

        public struct C2Step : IStepFunction
        {
            public float4 Step(float4 a, float4 b, float4 x)
            {
                float4 t = saturate((x-a)/(b-a));
                return t * t * t * (t * (t * 6.0f - 15.0f) + 10.0f);
            }
        }

        public struct LatticeNormal<S> : ILattice where S : struct,IStepFunction
        {
            public LatticeSpan4 GetLatticeSpan4(float4 coordinates, int frequency)
            {
                // coordinates *= frequency;
                float4 points = floor(coordinates);
                LatticeSpan4 span;
                span.p0 = (int4)points;
                span.p1 = span.p0 + 1;
                // relative coordinates
                span.g0 = coordinates - span.p0;
                span.g1 = span.g0 - 1.0f;
                span.t = coordinates - points;
                span.t = default(S).Step(0.0f,1.0f,span.t);
                return span;
            }
        }

        public struct LatticeTiling<S> : ILattice where S : struct, IStepFunction
        {
            public LatticeSpan4 GetLatticeSpan4(float4 coordinates, int frequency)
            {
                // 关键在于产生重复的sequence，只要我们的sequence是重复的，每个数字对应的hash值不会发送变化，
                // 这样最终的画面一定是可以重叠的
                // must scale the coordinate to get a repeated pattern
                coordinates *= frequency;
                float4 points = floor(coordinates);
                LatticeSpan4 span;
                span.p0 = (int4)points;
                // relative coordinates
                span.g0 = coordinates - span.p0;
                // coordinates relative to p1, always negative
                span.g1 = span.g0 - 1.0f;
                // span.p1 = span.p0 + 1;

                // plain old modulo
                // This happens because the remainder is influenced by the sign.
                // To fix this we have to adjust LatticeTiling.GetLatticeSpan4.
                // span.p0 %= frequency;
                // span.p1 %= frequency;

                // vectorized modulo
                span.p0 -= (int4)ceil(points / frequency) * frequency;
                span.p0 = select(span.p0, span.p0 + frequency, span.p0 < 0);
                span.p1 = span.p0 + 1;
                span.p1 = select(span.p1, 0, span.p1 == frequency);

                span.t = coordinates - points;
                span.t = default(S).Step(0.0f,1.0f,span.t);
                return span;
            }
        }

        // one kind of Noise, so implement interface INoise
        public struct Lattice1D<L,G> : INoise where L:struct,ILattice where G : struct, IGradient
        {
            public float4 GetNoise4(float4x3 positions, SmallXXHash4 hash,int frequency)
            {
                var l = default(L);
                LatticeSpan4 x = l.GetLatticeSpan4(positions.c0,frequency);
                var g = default(G);
                return g.EvaluateAfterInterpolation(lerp(g.Evaluate(hash.Eat(x.p0),x.g0), g.Evaluate(hash.Eat(x.p1),x.g1), x.t));
            }
        }

        public struct Lattice2D<L,G> : INoise where L:struct,ILattice where G : struct, IGradient
        {
            public float4 GetNoise4(float4x3 positions, SmallXXHash4 hash,int frequency)
            {
                var l = default(L);
                LatticeSpan4 x = l.GetLatticeSpan4(positions.c0,frequency), z = l.GetLatticeSpan4(positions.c2,frequency);
                // bilinear interpolate
                SmallXXHash4 h0 = hash.Eat(x.p0),h1 = hash.Eat(x.p1);
                var g = default(G);
                return g.EvaluateAfterInterpolation(lerp(
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
                ));
            }
        }

        public struct Lattice3D<L,G> : INoise where L:struct,ILattice where G : struct, IGradient
        {
            public float4 GetNoise4(float4x3 positions, SmallXXHash4 hash,int frequency)
            {
                var l = default(L);
                LatticeSpan4 
                    x = l.GetLatticeSpan4(positions.c0,frequency),
                    y = l.GetLatticeSpan4(positions.c1,frequency),
                    z = l.GetLatticeSpan4(positions.c2,frequency);
                SmallXXHash4
                    h0 = hash.Eat(x.p0),
                    h1 = hash.Eat(x.p1),
                    h00 = h0.Eat(y.p0),
                    h01 = h0.Eat(y.p1),
                    h10 = h1.Eat(y.p0),
                    h11 = h1.Eat(y.p1);
                
                // bicubic interpolation
                var g = default(G);
                return g.EvaluateAfterInterpolation(
                    lerp(
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
                ));
            }
        }
    }
}