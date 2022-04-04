using System;
using UnityEngine;
using UnityEngine.UI;

namespace UnityTutorials.Pseudorandom_Noise
{
    public static partial class Noise
    {
        [Serializable]
        public class NoiseResolver
        {
            public enum NoiseType
            {
                Perlin,Value,Voronoi
            }

            public enum ContinuousType
            {
                C0,C1,C2
            }

            public enum LatticeType
            {
                Normal,Tiling
            }
            
            [Range(1,3)]
            public int dimension;
            
            public bool turbulence;
            public NoiseType noiseType;
            // 插值后生成的曲线/曲面的性质，0阶连续，1阶连续和2阶连续，分别表示当前函数连续，导函数连续以及二阶导连续
            public ContinuousType continuousType;
            public LatticeType latticeType;
            
            private static Type[] _NoiseTypes =
            {
                typeof(Perlin), typeof(Value)
            };
            
            private static Type[] _StepTypes =
            {
                typeof(C0Step), typeof(C1Step), typeof(C2Step)
            };

            private static Type[] _LatticeTypes =
            {
                typeof(LatticeNormal<>), typeof(LatticeTiling<>)
            };

            private static Type[] _GradientDimTypes =
            {
                typeof(Lattice1D<,>), typeof(Lattice2D<,>), typeof(Lattice3D<,>)
            };

            private static Type[] _VoronoiDimTypes =
            {
                typeof(Voronoi1D<>), typeof(Voronoi2D<>), typeof(Voronoi3D<>)
            };

            public static NoiseResolver Default => new NoiseResolver
            {
                dimension = 2,
                turbulence = false,
                noiseType = NoiseType.Value,
                continuousType = ContinuousType.C2,
                latticeType = LatticeType.Normal
            };

            public ScheduleDelegate Resolve()
            {
                Type S = _StepTypes[(int)continuousType];
                Type L = _LatticeTypes[(int)latticeType].MakeGenericType(S);
                Type JobType = null;
                switch (noiseType)
                {
                    case NoiseType.Value:
                    case NoiseType.Perlin:
                    {
                        Type G = _NoiseTypes[(int)noiseType];
                        if (turbulence)
                        {
                            G = typeof(Turbulence<>).MakeGenericType(G);
                        }
                        Type D = _GradientDimTypes[dimension - 1].MakeGenericType(L,G);
                        JobType = typeof(Job<>).MakeGenericType(D);
                        break;
                    }
                    case NoiseType.Voronoi:
                    {
                        Type D = _VoronoiDimTypes[dimension - 1].MakeGenericType(L);
                        JobType = typeof(Job<>).MakeGenericType(D);
                        break;
                    }
                    default:
                        throw new NotSupportedException("unexpected NoiseType");
                }
                return JobType.GetMethod("ScheduleParallel").CreateDelegate(typeof(ScheduleDelegate)) as ScheduleDelegate;
            }

        }
        
    }
}