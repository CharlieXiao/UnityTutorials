using System;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using static UnityTutorials.Pseudorandom_Noise.Noise;
using static UnityTutorials.Pseudorandom_Noise.Noise.NoiseResolver;


namespace UnityTutorials.Pseudorandom_Noise
{
    public class NoiseVisualization : Visualization
    {
        private static int noiseId = Shader.PropertyToID("_Noise");

        [SerializeField] private DomainTransform domain = DomainTransform.Default;
        [SerializeField] private Settings noiseSettings = Settings.Default;
        [SerializeField] private NoiseResolver noiseResolver = new NoiseResolver();

        private NativeArray<float4> noise;
        private ComputeBuffer noiseBuffer;


        protected override void EnableVisualization(int dataLength, MaterialPropertyBlock propertyBlock)
        {
            noise = new NativeArray<float4>(dataLength, Allocator.Persistent);
            noiseBuffer = new ComputeBuffer(dataLength * 4, 4);
            propertyBlock.SetBuffer(noiseId, noiseBuffer);
        }

        protected override void DisableVisualization()
        {
            noise.Dispose();
            noiseBuffer.Release();
            noiseBuffer = null;
        }

        protected override void UpdateVisualization(NativeArray<float3x4> positions, int resolution, JobHandle handle)
        {
            var noiseJob = noiseResolver.Resolve();
            noiseJob(positions, noise, noiseSettings, domain, resolution, handle).Complete();
            noiseBuffer.SetData(noise.Reinterpret<float>(4 * 4));
        }

        [CustomEditor(typeof(NoiseVisualization))]
        [CanEditMultipleObjects]
        public class NoiseEditor : Editor
        {
            private SerializedProperty m_NoiseProp;

            public void OnEnable()
            {
                m_NoiseProp = serializedObject.FindProperty("noiseResolver");
            }

            public override void OnInspectorGUI()
            {
                // base.OnInspectorGUI();
                EditorGUI.BeginChangeCheck();
                serializedObject.UpdateIfRequiredOrScript();
                NoiseGenre noiseGenre = (NoiseGenre)Enum.ToObject(typeof(NoiseGenre),
                    m_NoiseProp.FindPropertyRelative("noiseGenre").enumValueIndex);
                switch (noiseGenre)
                {
                    case NoiseGenre.Simplex:
                        DrawPropertiesExcluding(serializedObject,
                            "noiseResolver.voronoiFunctionType",
                            "noiseResolver.voronoiDistanceType",
                            "noiseResolver.gradientFunctionType",
                            "noiseResolver.continuousType",
                            "noiseResolver.latticeType");
                        break;
                    case NoiseGenre.Gradient:
                        DrawPropertiesExcluding(serializedObject,
                            "noiseResolver.voronoiFunctionType",
                            "noiseResolver.voronoiDistanceType",
                            "noiseResolver.gradientFunctionType",
                            "noiseResolver.continuousType",
                            "noiseResolver.latticeType");
                        break;
                    case NoiseGenre.Voronoi:
                        DrawPropertiesExcluding(serializedObject,
                            "noiseResolver.voronoiFunctionType",
                            "noiseResolver.voronoiDistanceType",
                            "noiseResolver.gradientFunctionType",
                            "noiseResolver.continuousType",
                            "noiseResolver.latticeType");
                        break;
                }
                serializedObject.ApplyModifiedProperties();
                EditorGUI.EndChangeCheck();
            }
        }
    }
}