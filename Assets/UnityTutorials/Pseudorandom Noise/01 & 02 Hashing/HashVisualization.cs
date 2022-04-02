using Unity.Burst;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace UnityTutorials.Pseudorandom_Noise._01_Hashing
{
    public class HashVisualization : MonoBehaviour
    {
        [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
        struct HashJob : IJobFor
        {
            [WriteOnly] public NativeArray<uint> hashes;

            [ReadOnly] public NativeArray<float3> positions;

            [ReadOnly] public SmallXXHash hash;
            [ReadOnly] public float3x4 domainTRS;

            public void Execute(int i)
            {
                float3 p = mul(domainTRS,float4(positions[i],1.0f));
                
                // [vf,uf] is [-0.5,0.5] x [-0.5,0.5]
                int u = (int)floor(p.x);
                int v = (int)floor(p.y);
                int w = (int)floor(p.z);
                hashes[i] = hash.Eat(u).Eat(v).Eat(w);
            }
        }

        [SerializeField] private SpaceTRS domain = new SpaceTRS
        {
            scale = 8.0f
        };

        private static int 
            hashedId = Shader.PropertyToID("_Hashes"),
            configId = Shader.PropertyToID("_Config"),
            positionId = Shader.PropertyToID("_Positions");

        [SerializeField] private Mesh instanceMesh;

        [SerializeField] private Material material;

        [SerializeField] private int seed = 0;

        [SerializeField, Range(1, 512)] private int resolution;

        [SerializeField, Range(-2.0f, 2.0f)] private float verticalOffset = 1.0f;

        private float invResolution;

        private NativeArray<uint> hashes;

        private NativeArray<float3> positions;

        // transfer data to shader
        private ComputeBuffer hashesBuffer,positionsBuffer;

        // material property
        private MaterialPropertyBlock propertyBlock;

        private void OnEnable()
        {
            int length = resolution * resolution;
            hashes = new NativeArray<uint>(length, Allocator.Persistent);
            positions = new NativeArray<float3>(length, Allocator.Persistent);
            // uint is 4 bytes long, so the stride is 4
            hashesBuffer = new ComputeBuffer(length, 4);
            // float3 is 3 * 4 bytes long
            positionsBuffer = new ComputeBuffer(length, 3 * 4);
            invResolution = 1.0f / resolution;

            JobHandle handle = Shapes.Job.ScheduleParallel(positions, resolution, default);

            // 执行Burst任务（并行）
            new HashJob
            {
                hashes = hashes,
                positions = positions,
                hash = SmallXXHash.Seed(seed),
                domainTRS = domain.Matrix
            }.ScheduleParallel(hashes.Length, resolution, handle).Complete();
            
            // copy data to buffer
            hashesBuffer.SetData(hashes);
            positionsBuffer.SetData(positions);
            
            // set property for rendering
            propertyBlock ??= new MaterialPropertyBlock();
            propertyBlock.SetBuffer(hashedId, hashesBuffer);
            propertyBlock.SetBuffer(positionId,positionsBuffer);
            // config 存储了resolution和resolution的倒数，在shader中使用
            propertyBlock.SetVector(configId, new Vector4(
                resolution, invResolution,verticalOffset * invResolution));

        }

        private void OnDisable()
        {
            hashes.Dispose();
            positions.Dispose();
            hashesBuffer.Release();
            positionsBuffer.Release();
            hashesBuffer = null;
        }

        private void OnValidate()
        {
            if (hashesBuffer != null && enabled)
            {
                OnDisable();
                OnEnable();
            }
        }

        private void Update()
        {
            // 直接绘制即可
            Graphics.DrawMeshInstancedProcedural(
                instanceMesh, 0, material, 
                new Bounds(Vector3.zero, Vector3.one),
                hashes.Length, propertyBlock);
        }
    }
}