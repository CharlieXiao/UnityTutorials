using System;
using UnityEngine;
using UnityEngine.UI;

namespace UnityTutorials.Pseudorandom_Noise
{
    public static partial class Noise
    {
        // TODO: need refactoring this class, make three type(Gradient,Voronoi,Simplex) has their own resolve class
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

            public enum VoronoiFunctionType
            {
                F1,F2,F2MinusF1
            }

            public enum VoronoiDistanceType
            {
                [InspectorName("Euclidean(Worley)")]
                Worley,
                Chebyshev,SquaredEuclidean
            }
            
            [Range(1,3)]
            public int dimension;
            
            public bool turbulence;
            public NoiseType noiseType;
            // 适用于基于梯度的noise
            // 插值后生成的曲线/曲面的性质，0阶连续，1阶连续和2阶连续，分别表示当前函数连续，导函数连续以及二阶导连续
            public ContinuousType continuousType;
            public LatticeType latticeType;
            public VoronoiFunctionType voronoiFunctionType;
            public VoronoiDistanceType voronoiDistanceType;
            
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

            private static Type[] _VoronoiFunctionTypes =
            {
                typeof(F1), typeof(F2), typeof(F2MinusF1)
            };

            private static Type[] _VoronoiDistanceTypes =
            {
                typeof(Worley),typeof(Chebyshev),typeof(SquaredEuclidean)
            };

            private static Type[] _VoronoiDimTypes =
            {
                typeof(Voronoi1D<,,>), typeof(Voronoi2D<,,>), typeof(Voronoi3D<,,>)
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
                Type JobType = null;
                switch (noiseType)
                {
                    case NoiseType.Value:
                    case NoiseType.Perlin:
                    {
                        // gradient based noise
                        Type S = _StepTypes[(int)continuousType];
                        Type L = _LatticeTypes[(int)latticeType].MakeGenericType(S);
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
                        // distance based noise
                        // Voronoi并不基于gradient，因此其不需要steo
                        Type L = _LatticeTypes[(int) latticeType].MakeGenericType(_StepTypes[(int) ContinuousType.C0]);
                        Type F = _VoronoiFunctionTypes[(int) voronoiFunctionType];
                        Type Dist = _VoronoiDistanceTypes[(int) voronoiDistanceType];
                        Type D = _VoronoiDimTypes[dimension - 1].MakeGenericType(L,F,Dist);
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