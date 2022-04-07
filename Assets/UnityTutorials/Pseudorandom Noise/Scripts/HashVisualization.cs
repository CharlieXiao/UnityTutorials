
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Unity.Burst;

using static Unity.Mathematics.math;

namespace PseudorandomNoise
{
    public class HashVisualization : Visualization
    {
        private static int hashesId = Shader.PropertyToID("_Hashes");

        [SerializeField] private int seed;

        [SerializeField] private DomainTransform domain = new DomainTransform { scale = 8.0f };

        private NativeArray<uint4> hashes;

        private ComputeBuffer hashesBuffer;
        
        [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
        struct HashJob : IJobFor
        {
            [WriteOnly] public NativeArray<uint4> hashes;

            [ReadOnly] public NativeArray<float3x4> positions;

            [ReadOnly] public SmallXXHash4 hash;
            [ReadOnly] public float3x4 domainTRS;

            public void Execute(int i)
            {
                // our target is float3x4, transform matrix is float3x4, so we need to transpose it
                float4x3 p = domainTRS.TransformVectors(transpose(positions[i]));
                
                // [vf,uf] is [-0.5,0.5] x [-0.5,0.5]
                int4 u = (int4)floor(p.c0);
                int4 v = (int4)floor(p.c1);
                int4 w = (int4)floor(p.c2);
                hashes[i] = hash.Eat(u).Eat(v).Eat(w);
            }
        }

        protected override void EnableVisualization(int dataLength, MaterialPropertyBlock propertyBlock)
        {
            hashes = new NativeArray<uint4>(dataLength, Allocator.Persistent);
            hashesBuffer = new ComputeBuffer(dataLength * 4, 4);
            propertyBlock.SetBuffer(hashesId,hashesBuffer);
        }

        protected override void DisableVisualization()
        {
            hashes.Dispose();
            hashesBuffer.Release();
            hashesBuffer = null;
        }

        protected override void UpdateVisualization(NativeArray<float3x4> positions, int resolution, JobHandle handle)
        {
            new HashJob
            {
                positions = positions,
                hashes = hashes,
                hash = SmallXXHash4.Seed(seed),
                domainTRS = domain.Matrix
            }.ScheduleParallel(hashes.Length,resolution,handle).Complete();
            
            hashesBuffer.SetData(hashes.Reinterpret<uint>(4*4));
        }
    }
}