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
            private bool showNoiseConfig = false;
            private SerializedProperty m_NoiseProp;
            private SerializedProperty m_NoiseGenre;
            private SerializedProperty m_Dimension;
            private SerializedProperty m_VoronoiFunctionType;
            private SerializedProperty m_VoronoiDistanceType;
            private SerializedProperty m_GradientFunctionType;
            private SerializedProperty m_SimplexFunctionType;
            private SerializedProperty m_ContinuousType;
            private SerializedProperty m_LatticeType;
            private SerializedProperty m_Turbulence;

            public void OnEnable()
            {
               m_NoiseProp = serializedObject.FindProperty("noiseResolver");
               m_NoiseGenre = m_NoiseProp.FindPropertyRelative("noiseGenre");
               m_Dimension = m_NoiseProp.FindPropertyRelative("dimension");
               m_VoronoiFunctionType = m_NoiseProp.FindPropertyRelative("voronoiFunctionType");
               m_VoronoiDistanceType = m_NoiseProp.FindPropertyRelative("voronoiDistanceType");
               m_GradientFunctionType = m_NoiseProp.FindPropertyRelative("gradientFunctionType");
               m_SimplexFunctionType = m_NoiseProp.FindPropertyRelative("simplexFunctionType");
               m_ContinuousType = m_NoiseProp.FindPropertyRelative("continuousType");
               m_LatticeType = m_NoiseProp.FindPropertyRelative("latticeType");
               m_Turbulence = m_NoiseProp.FindPropertyRelative("turbulence");
            }

            private void OnNoiseResolverGUI()
            {
                showNoiseConfig = EditorGUILayout.Foldout(showNoiseConfig, "Noise Resolver", true);
                if (!showNoiseConfig)
                {
                    return;
                }
                // var genreProp = m_NoiseProp.FindPropertyRelative("noiseGenre");
                EditorGUILayout.PropertyField(m_NoiseGenre);
                var genre = (NoiseGenre) m_NoiseGenre.enumValueIndex;
                EditorGUILayout.PropertyField(m_Dimension);
                switch (genre)
                {
                    case NoiseGenre.Simplex:
                        EditorGUILayout.PropertyField(m_SimplexFunctionType);
                        EditorGUILayout.PropertyField(m_Turbulence);
                        break;
                    case NoiseGenre.Gradient:
                        EditorGUILayout.PropertyField(m_LatticeType);
                        EditorGUILayout.PropertyField(m_ContinuousType);
                        EditorGUILayout.PropertyField(m_GradientFunctionType);
                        EditorGUILayout.PropertyField(m_Turbulence);
                        break;
                    case NoiseGenre.Voronoi:
                        EditorGUILayout.PropertyField(m_LatticeType);
                        EditorGUILayout.PropertyField(m_VoronoiFunctionType);
                        EditorGUILayout.PropertyField(m_VoronoiDistanceType);
                        break;
                }
            }

            public override void OnInspectorGUI()
            {
                EditorGUI.BeginChangeCheck();
                serializedObject.UpdateIfRequiredOrScript();
                SerializedProperty iterator = serializedObject.GetIterator();
                for (bool enterChildren = true; iterator.NextVisible(enterChildren); enterChildren = false)
                {
                    if (iterator.propertyPath == m_NoiseProp.propertyPath)
                    {
                        continue;
                    }

                    using (new EditorGUI.DisabledScope("m_Script" == iterator.propertyPath))
                    {
                        EditorGUILayout.PropertyField(iterator, true);
                    }
                }
                OnNoiseResolverGUI();
                serializedObject.ApplyModifiedProperties();
                EditorGUI.EndChangeCheck();
            }
        }
    }
}