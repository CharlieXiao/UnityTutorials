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

            [ReadOnly] public int resolution;
            [ReadOnly] public float invResolution;
            [ReadOnly] public SmallXXHash hash;

            public void Execute(int i)
            {
                // xxHash fast digest algorithm
                // 最简单的hash函数
                // using uint so that all bit is treated in the same way
                // frac(i*0.381f) produce a float in [0,1)
                // than scale it by 256 to extract 8 bit
                // [-r/2,r/2] x [-r/2,r/2]
                int v = (int)floor(invResolution * (i + 0.5f));
                int u = i - resolution * v - resolution / 2;
                v -= resolution / 2;
                
                hashes[i] = hash.Eat(u).Eat(v);
            }
        }

        private static int hashedId = Shader.PropertyToID("_Hashes"),
            configId = Shader.PropertyToID("_Config");

        [SerializeField] private Mesh instanceMesh;

        [SerializeField] private Material material;

        [SerializeField] private int seed;

        [SerializeField, Range(1, 512)] private int resolution;

        [SerializeField, Range(-2.0f, 2.0f)] private float verticalOffset = 1.0f;

        private float invResolution;

        private NativeArray<uint> hashes;

        // transfer data to shader
        private ComputeBuffer hashesBuffer;

        // material property
        private MaterialPropertyBlock propertyBlock;

        private void OnEnable()
        {
            int length = resolution * resolution;
            hashes = new NativeArray<uint>(length, Allocator.Persistent);
            // uint is 4 byte, so the stride is 4
            hashesBuffer = new ComputeBuffer(length, 4);
            
            invResolution = 1.0f / resolution;

            // 执行Burst任务（并行）
            new HashJob
            {
                hashes = hashes,
                resolution = resolution,
                invResolution = invResolution,
                hash = SmallXXHash.Seed(seed)
            }.ScheduleParallel(hashes.Length, resolution, default).Complete();

            hashesBuffer.SetData(hashes);

            propertyBlock ??= new MaterialPropertyBlock();
            propertyBlock.SetBuffer(hashedId, hashesBuffer);
            // config 存储了resolution和resolution的倒数，在shader中使用
            propertyBlock.SetVector(configId, new Vector4(
                resolution, invResolution,verticalOffset * invResolution));

        }

        private void OnDisable()
        {
            hashes.Dispose();
            hashesBuffer.Release();
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