using System;
using UnityEngine;

namespace PseudorandomNoise
{
    public static partial class Noise
    {
        [Serializable]
        public class NoiseResolver
        {
            private static Type[] _StepTypes =
            {
                typeof(C0Step), typeof(C1Step), typeof(C2Step)
            };

            private static Type[] _LatticeTypes =
            {
                typeof(LatticeNormal<>), typeof(LatticeTiling<>)
            };

            private static Type[] _VoronoiFunctionTypes =
            {
                typeof(F1), typeof(F2), typeof(F2MinusF1)
            };

            private static Type[] _VoronoiDistanceTypes =
            {
                typeof(Worley), typeof(Chebyshev), typeof(SquaredEuclidean)
            };
            
            private static Type[] _SimplexDimTypes =
            {
                typeof(Simplex1D<>),typeof(Simplex2D<>),typeof(Simplex3D<>)
            };

            private static Type[] _VoronoiDimTypes =
            {
                typeof(Voronoi1D<,,>), typeof(Voronoi2D<,,>), typeof(Voronoi3D<,,>)
            };
            
            private static Type[] _GradientDimTypes =
            {
                typeof(Lattice1D<,>), typeof(Lattice2D<,>), typeof(Lattice3D<,>)
            };

            private static Type[] _GradientFunctionType =
            {
                typeof(Perlin), typeof(Value)
            };
            
            private static Type[] _SimplexFunctionTypes =
            {
                typeof(Value), typeof(Simplex)
            };

            public enum NoiseGenre
            {
                Simplex,Gradient,Voronoi
            }

            public enum VoronoiFunctionType
            {
                [Tooltip("Min distance")] F1,
                [Tooltip("Second min distance")] F2,
                [Tooltip("Second min distance minus min distance")] F2MinusF1
            }

            public enum VoronoiDistanceType
            {
                [InspectorName("Euclidean(Worley)")] Worley,
                [Tooltip("D(x,y) = cmax(|x-y|)")] Chebyshev,
                SquaredEuclidean
            }

            public enum GradientFunctionType
            {
                Perlin,Value
            }
            
            public enum ContinuousType
            {
                C0,C1,C2
            }
            
            public enum LatticeType
            {
                Normal,Tiling
            }

            public enum SimplexFunctionType
            {
                Value,Simplex
            }
            
            [SerializeField] private NoiseGenre noiseGenre = NoiseGenre.Gradient;
            [SerializeField,Range(1,3)] private int dimension = 1;
            [SerializeField] private VoronoiFunctionType voronoiFunctionType = VoronoiFunctionType.F1;
            [SerializeField] private VoronoiDistanceType voronoiDistanceType = VoronoiDistanceType.Worley;
            [SerializeField] private GradientFunctionType gradientFunctionType = GradientFunctionType.Value;
            [SerializeField] private SimplexFunctionType simplexFunctionType = SimplexFunctionType.Simplex;
            [SerializeField] private ContinuousType continuousType = ContinuousType.C2;
            [SerializeField] private LatticeType latticeType = LatticeType.Normal;
            [SerializeField] private bool turbulence = false;

            public ScheduleDelegate Resolve()
            {
                switch (noiseGenre)
                {
                    case NoiseGenre.Gradient:
                        return ResolveGradient(dimension,
                            continuousType, latticeType,
                            gradientFunctionType, turbulence);
                    case NoiseGenre.Simplex:
                        return ResolveSimplex(dimension, simplexFunctionType, turbulence);
                    case NoiseGenre.Voronoi:
                        return ResolveVoronoi(dimension, latticeType, voronoiFunctionType, voronoiDistanceType);
                }
            
                throw new ArgumentOutOfRangeException("Unrecognized enumeration" + noiseGenre);
            }

            public static ScheduleDelegate ResolveVoronoi(int dimension,LatticeType latticeType,VoronoiFunctionType functionType,VoronoiDistanceType distanceType)
            {
                Type L = _LatticeTypes[(int) latticeType].MakeGenericType(typeof(C0Step));
                Type F = _VoronoiFunctionTypes[(int) functionType];
                Type Dist = _VoronoiDistanceTypes[(int) distanceType];
                Type Dim = _VoronoiDimTypes[dimension - 1].MakeGenericType(L,F,Dist);
                Type JobType = typeof(Job<>).MakeGenericType(Dim);
                return JobType.GetMethod("ScheduleParallel").CreateDelegate(typeof(ScheduleDelegate)) as ScheduleDelegate;
            }

            public static ScheduleDelegate ResolveGradient(int dimension,ContinuousType continuousType,LatticeType latticeType,GradientFunctionType functionType,bool turbulence)
            {
                Type S = _StepTypes[(int)continuousType];
                Type L = _LatticeTypes[(int)latticeType].MakeGenericType(S);
                Type G = _GradientFunctionType[(int)functionType];
                if (turbulence)
                {
                    G = typeof(Turbulence<>).MakeGenericType(G);
                }
                Type D = _GradientDimTypes[dimension - 1].MakeGenericType(L,G);
                Type JobType = typeof(Job<>).MakeGenericType(D);
                return JobType.GetMethod("ScheduleParallel").CreateDelegate(typeof(ScheduleDelegate)) as ScheduleDelegate;
            }

            public static ScheduleDelegate ResolveSimplex(int dimension,SimplexFunctionType functionType,bool turbulence)
            {
                Type G = _SimplexFunctionTypes[(int)functionType];
                if (turbulence)
                {
                    G = typeof(Turbulence<>).MakeGenericType(G);
                }
                Type D = _SimplexDimTypes[dimension - 1].MakeGenericType(G);
                Type JobType = typeof(Job<>).MakeGenericType(D);
                return JobType.GetMethod("ScheduleParallel").CreateDelegate(typeof(ScheduleDelegate)) as ScheduleDelegate;
            }
        }
    }
}