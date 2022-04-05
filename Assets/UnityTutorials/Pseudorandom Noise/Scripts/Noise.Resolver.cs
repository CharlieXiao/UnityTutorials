using System;
using UnityEngine;

namespace UnityTutorials.Pseudorandom_Noise
{
    public static partial class Noise
    {
        // TODO: using custom editor to manage the noise config
        /*
         *   public class MyScript : MonoBehaviour
             {
               public bool flag;
               public int i = 1;
             }
             
             [CustomEditor(typeof(MyScript))]
             public class MyScriptEditor : Editor
             {
               void OnInspectorGUI()
               {
                 var myScript = target as MyScript;
             
                 myScript.flag = GUILayout.Toggle(myScript.flag, "Flag");
                 
                 if(myScript.flag)
                   myScript.i = EditorGUILayout.IntSlider("I field:", myScript.i , 1 , 100);
             
               }
             }
         */
        // [Serializable]

        private static Type[] _StepTypes =
        {
            typeof(C0Step), typeof(C1Step), typeof(C2Step)
        };

        private static Type[] _LatticeTypes =
        {
            typeof(LatticeNormal<>), typeof(LatticeTiling<>)
        };
        

        // [Range(1,3)]
        // public int dimension;

        [Serializable]
        public struct NoiseResolver
        {
            public SimplexResolver simplexResolver;
            
            public LatticeResolver latticeResolver;
            
            public VoronoiResolver voronoiResolver;

            private InternalResolver noiseConfig;

            
            [Range(1,3)]
            public int dimension;

            public static NoiseResolver Default => new NoiseResolver
            {
                dimension = 2
            };

            public void OnEnable(NoiseVisualization.NoiseType noiseType)
            {
                switch (noiseType)
                {
                    case NoiseVisualization.NoiseType.Simplex:
                        noiseConfig = simplexResolver;
                        break;
                    case NoiseVisualization.NoiseType.Lattice:
                        noiseConfig = latticeResolver;
                        break;
                    case NoiseVisualization.NoiseType.Voronoi:
                        noiseConfig = voronoiResolver;
                        break;
                }
            }

            public ScheduleDelegate Resolve()
            {
                return noiseConfig.Resolve(dimension);
            }
        }

        public abstract class InternalResolver
        { 
            public abstract ScheduleDelegate Resolve(int dimension);
        }

        [Serializable]
        public class VoronoiResolver : InternalResolver
        {
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
            
            public enum FunctionType
            {
                [Tooltip("Min distance")]
                F1,
                [Tooltip("Second min distance")]
                F2,
                [Tooltip("Second min distance minus min distance")]
                F2MinusF1
            }

            public enum DistanceType
            {
                [InspectorName("Euclidean(Worley)")]
                Worley,
                [Tooltip("D(x,y) = cmax(|x-y|)")]
                Chebyshev,
                SquaredEuclidean
            }

            public enum LatticeType
            {
                Normal,Tiling
            }

            public LatticeType latticeType = LatticeType.Normal;

            public FunctionType functionType = FunctionType.F1;

            public DistanceType distanceType = DistanceType.Worley;

            public override ScheduleDelegate Resolve(int dimension)
            {
                Type L = _LatticeTypes[(int) latticeType].MakeGenericType(typeof(C0Step));
                Type F = _VoronoiFunctionTypes[(int) functionType];
                Type Dist = _VoronoiDistanceTypes[(int) distanceType];
                Type Dim = _VoronoiDimTypes[dimension - 1].MakeGenericType(L,F,Dist);
                Type JobType = typeof(Job<>).MakeGenericType(Dim);
                return JobType.GetMethod("ScheduleParallel").CreateDelegate(typeof(ScheduleDelegate)) as ScheduleDelegate;
            }
        }

        [Serializable]
        public class LatticeResolver : InternalResolver
        {
            private static Type[] _GradientDimTypes =
            {
                typeof(Lattice1D<,>), typeof(Lattice2D<,>), typeof(Lattice3D<,>)
            };

            private static Type[] _GradientFunctionType =
            {
                typeof(Perlin), typeof(Value), typeof(Simplex)
            };
            
            public enum GradientFunctionType
            {
                Perlin,Value,Simplex
            }
            
            public enum ContinuousType
            {
                C0,C1,C2
            }
            
            public enum LatticeType
            {
                Normal,Tiling
            }
            
            public ContinuousType continuousType = ContinuousType.C2;

            public LatticeType latticeType = LatticeType.Normal;

            public GradientFunctionType gradientType = GradientFunctionType.Perlin;

            public bool turbulence = false;
            
            public override ScheduleDelegate Resolve(int dimension)
            {
                // gradient based noise
                Type S = _StepTypes[(int)continuousType];
                Type L = _LatticeTypes[(int)latticeType].MakeGenericType(S);
                Type G = _GradientFunctionType[(int)gradientType];
                if (turbulence)
                {
                    G = typeof(Turbulence<>).MakeGenericType(G);
                }
                Type D = _GradientDimTypes[dimension - 1].MakeGenericType(L,G);
                Type JobType = typeof(Job<>).MakeGenericType(D);
                return JobType.GetMethod("ScheduleParallel").CreateDelegate(typeof(ScheduleDelegate)) as ScheduleDelegate;
            }
        }

        [Serializable]
        public class SimplexResolver : InternalResolver
        {
            private static Type[] _SimplexDimTypes =
            {
                typeof(Simplex1D<>),typeof(Simplex2D<>),typeof(Simplex3D<>)
            };

            private static Type[] _SimplexGradientTypes =
            {
                typeof(Value), typeof(Simplex)
            };

            public enum GradientFunctionType
            {
                Value,Simplex
            }

            public GradientFunctionType gradientFunctionType = GradientFunctionType.Value;

            public bool turbulence = false;

            public override ScheduleDelegate Resolve(int dimension)
            {
                Type G = _SimplexGradientTypes[(int)gradientFunctionType];
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