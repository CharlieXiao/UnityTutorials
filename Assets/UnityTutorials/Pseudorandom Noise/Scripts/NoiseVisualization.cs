using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using static UnityTutorials.Pseudorandom_Noise.Noise;

namespace UnityTutorials.Pseudorandom_Noise
{
    public class NoiseVisualization : Visualization
    {
        private static int noiseId = Shader.PropertyToID("_Noise");

        [SerializeField] private int seed;

        [SerializeField] private DomainTransform domain = new DomainTransform
        {
            scale = 8.0f
        };

        [SerializeField,Range(1,3)] private int dimensions = 3;

        private NativeArray<float4> noise;

        private ComputeBuffer noiseBuffer;

        private static Noise.ScheduleDelegate[] noiseJobs =
        {
            Job<Lattice1D>.ScheduleParallel,
            Job<Lattice2D>.ScheduleParallel,
            Job<Lattice3D>.ScheduleParallel
        };
        protected override void EnableVisualization(int dataLength, MaterialPropertyBlock propertyBlock)
        {
            noise = new NativeArray<float4>(dataLength, Allocator.Persistent);
            noiseBuffer = new ComputeBuffer(dataLength * 4, 4);
            propertyBlock.SetBuffer(noiseId,noiseBuffer);
        }

        protected override void DisableVisualization()
        {
            noise.Dispose();
            noiseBuffer.Release();
            noiseBuffer = null;
        }

        protected override void UpdateVisualization(NativeArray<float3x4> positions, int resolution, JobHandle handle)
        {
            noiseJobs[dimensions-1](positions,noise,seed,domain,resolution,handle).Complete();
            noiseBuffer.SetData(noise.Reinterpret<float>(4*4));
        }
    }
}