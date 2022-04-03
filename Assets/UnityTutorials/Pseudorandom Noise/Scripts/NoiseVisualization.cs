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

        [SerializeField, Range(1, 3)] private int dimensions = 3;

        private enum NoiseType
        {
            Perlin,
            Value
        }

        [SerializeField] private NoiseType noiseType = NoiseType.Value;

        private NativeArray<float4> noise;

        private ComputeBuffer noiseBuffer;

        private static Noise.ScheduleDelegate[] noiseJobs =
        {
            
                Job<Lattice1D<Perlin>>.ScheduleParallel,
                Job<Lattice2D<Perlin>>.ScheduleParallel,
                Job<Lattice3D<Perlin>>.ScheduleParallel
            ,
            
                Job<Lattice1D<Value>>.ScheduleParallel,
                Job<Lattice2D<Value>>.ScheduleParallel,
                Job<Lattice3D<Value>>.ScheduleParallel
            
        };

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
            noiseJobs[3*(int)noiseType+dimensions-1](positions, noise, seed, domain, resolution, handle).Complete();
            noiseBuffer.SetData(noise.Reinterpret<float>(4 * 4));
        }
    }
}