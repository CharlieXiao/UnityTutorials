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
        private static int 
            hashedId = Shader.PropertyToID("_Hashes"),
            configId = Shader.PropertyToID("_Config"),
            positionId = Shader.PropertyToID("_Positions"),
            normalId = Shader.PropertyToID("_Normals");
        
        [SerializeField] private SpaceTRS domain = new SpaceTRS { scale = 8.0f };

        [SerializeField] private Mesh instanceMesh;

        [SerializeField] private Material material;

        [SerializeField] private int seed = 0;

        [SerializeField, Range(1, 512)] private int resolution;
        
        // move the cube along normal
        [SerializeField, Range(-0.5f, 0.5f)] private float displacement = 0.1f;
        
        [SerializeField, Range(0.1f, 10f)] private float instanceScale = 1.0f;

        [SerializeField] private Shape shape = Shape.Plane;
        
        private float invResolution;

        private NativeArray<uint4> hashes;

        private NativeArray<float3x4> positions,normals;

        // transfer data to shader
        private ComputeBuffer hashesBuffer,positionsBuffer,normalsBuffer;

        // material property
        private MaterialPropertyBlock propertyBlock;

        private bool updatePosition = false;

        private Bounds bounds;
        
        private enum Shape
        {
            Plane,UVSphere,Torus,OctahedronSphere
        }

        private static Shapes.ScheduleDelegate[] shapeJobs =
        {
            Shapes.Job<Shapes.Plane>.ScheduleParallel,
            Shapes.Job<Shapes.UVSphere>.ScheduleParallel,
            Shapes.Job<Shapes.Torus>.ScheduleParallel,
            Shapes.Job<Shapes.OctahedronSphere>.ScheduleParallel
        };

        [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
        struct HashJob : IJobFor
        {
            [WriteOnly] public NativeArray<uint4> hashes;

            [ReadOnly] public NativeArray<float3x4> positions;

            [ReadOnly] public SmallXXHash4 hash;
            [ReadOnly] public float3x4 domainTRS;

            float4x3 TransformPositions(float3x4 trs, float4x3 p)
            {
                return float4x3(
                    // manually apply the transform and utilize the SIMD instructions
                    trs.c0.x * p.c0 + trs.c1.x * p.c1 + trs.c2.x * p.c2 + trs.c3.x,
                    trs.c0.y * p.c0 + trs.c1.y * p.c1 + trs.c2.y * p.c2 + trs.c3.y,
                    trs.c0.z * p.c0 + trs.c1.z * p.c1 + trs.c2.z * p.c2 + trs.c3.z
                );
            }

            public void Execute(int i)
            {
                // our target is float3x4, transform matrix is float3x4, so we need to transpose it
                float4x3 p = TransformPositions(domainTRS,transpose(positions[i]));
                
                // [vf,uf] is [-0.5,0.5] x [-0.5,0.5]
                int4 u = (int4)floor(p.c0);
                int4 v = (int4)floor(p.c1);
                int4 w = (int4)floor(p.c2);
                hashes[i] = hash.Eat(u).Eat(v).Eat(w);
            }
        }

        private void OnEnable()
        {
            int length = resolution * resolution;
            // make vector work for odd length
            length = length / 4 + (length & 1);
            
            // CPU buffer
            hashes = new NativeArray<uint4>(length, Allocator.Persistent);
            positions = new NativeArray<float3x4>(length, Allocator.Persistent);
            normals = new NativeArray<float3x4>(length, Allocator.Persistent);

            // GPU buffer, the size is still uint,float3 and float3
            hashesBuffer = new ComputeBuffer(length*4, 4);
            positionsBuffer = new ComputeBuffer(length*4, 3 * 4);
            normalsBuffer = new ComputeBuffer(length*4, 3 * 4);
            
            invResolution = 1.0f / resolution;
            updatePosition = true;

            // set property for rendering
            propertyBlock ??= new MaterialPropertyBlock();
        }

        private void OnDisable()
        {
            hashes.Dispose();
            positions.Dispose();
            normals.Dispose();
            hashesBuffer.Release();
            positionsBuffer.Release();
            normalsBuffer.Release();
            hashesBuffer = null;
            positionsBuffer = null;
            normalsBuffer = null;
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
            if (updatePosition || transform.hasChanged)
            {
                updatePosition = false;
                transform.hasChanged = false;
                // update position
                JobHandle handle = shapeJobs[(int)shape](positions,normals, resolution,transform.localToWorldMatrix, default);

                bounds = new Bounds(
                    transform.position,
                    float3(2.0f * cmax(abs(transform.lossyScale)) + displacement)
                );
                
                // 执行Burst任务（并行）
                new HashJob
                {
                    positions = positions,
                    hashes = hashes,
                    hash = SmallXXHash4.Seed(seed),
                    domainTRS = domain.Matrix
                    // the resolution must be even
                }.ScheduleParallel(hashes.Length, resolution, handle).Complete();
            
                // copy data from CPU to GPU
                hashesBuffer.SetData(hashes.Reinterpret<uint>(4 * 4));
                positionsBuffer.SetData(positions.Reinterpret<float3>(3*4*4));
                normalsBuffer.SetData(normals.Reinterpret<float3>(3*4*4));
                
                propertyBlock.SetBuffer(hashedId, hashesBuffer);
                propertyBlock.SetBuffer(positionId,positionsBuffer);
                propertyBlock.SetBuffer(normalId,normalsBuffer);
                // config 存储了resolution和resolution的倒数，在shader中使用
                propertyBlock.SetVector(configId, new Vector4(
                    resolution, instanceScale * invResolution,displacement));
            }
            // 直接绘制即可
            Graphics.DrawMeshInstancedProcedural(
                instanceMesh, 0, material, 
                bounds, resolution * resolution, propertyBlock);
        }
    }
}