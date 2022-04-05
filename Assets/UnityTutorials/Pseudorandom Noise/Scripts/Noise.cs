using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace UnityTutorials.Pseudorandom_Noise
{
    public static partial class Noise
    {
        public interface INoise
        {
            float4 GetNoise4(float4x3 positions, SmallXXHash4 hash, int frequency);
        }

        [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
        public struct Job<N> : IJobFor where N : struct, INoise
        {
            [ReadOnly] public NativeArray<float3x4> positions;

            [WriteOnly] public NativeArray<float4> noise;

            public Settings settings;

            public float3x4 domainTRS;

            public void Execute(int index)
            {
                float4x3 position = domainTRS.TransformVectors(transpose(positions[index]));
                var hash = SmallXXHash4.Seed(settings.seed);
                int frequency = settings.frequency;
                float amplitude = 1.0f,amplitudeSum = 0.0f;
                float4 sum = 0.0f;
                for (int i = 0; i < settings.octaves; ++i)
                {
                    // 累加不同频率的noise，使得最终结果在大小上区分度更高
                    // 对于每一个octave可以使用不同的hash，这样最终随机效果更好
                    sum += amplitude * default(N).GetNoise4(position, hash + i,frequency);
                    amplitudeSum += amplitude;
                    frequency *= settings.lacunarity;
                    amplitude *= settings.persistence;
                }
                noise[index] = sum / amplitudeSum;
            }

            public static JobHandle ScheduleParallel(
                NativeArray<float3x4> positions,
                NativeArray<float4> noise, Settings settings,
                DomainTransform domainTRS, int resolution,
                JobHandle dependency) => new Job<N>
            {
                positions = positions,
                noise = noise,
                settings = settings,
                domainTRS = domainTRS.Matrix
            }.ScheduleParallel(positions.Length, resolution, dependency);
        }

        public delegate JobHandle ScheduleDelegate(
            NativeArray<float3x4> positions, 
            NativeArray<float4> noise, Settings settings,
            DomainTransform domainTRS, int resolution, JobHandle dependency);
    }
}