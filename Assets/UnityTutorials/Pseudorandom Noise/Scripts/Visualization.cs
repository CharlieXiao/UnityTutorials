using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace UnityTutorials.Pseudorandom_Noise
{
    public abstract class Visualization : MonoBehaviour
    {
        private static int
            configId = Shader.PropertyToID("_Config"),
            positionId = Shader.PropertyToID("_Positions"),
            normalId = Shader.PropertyToID("_Normals");

        [SerializeField] private Mesh instanceMesh;

        [SerializeField] private Material material;

        [SerializeField, Range(2, 512)] private int resolution;

        // move the cube along normal
        [SerializeField, Range(-0.5f, 0.5f)] private float displacementScale = 0.1f;

        [SerializeField, Range(0.1f, 10f)] private float instanceScale = 1.0f;

        [SerializeField] private Shape shape = Shape.Plane;

        [SerializeField, Range(0.0f,1.0f)] private float speed = 0.0f;
        
        private float invResolution;

        private NativeArray<float3x4> positions, normals;

        // transfer data to shader
        private ComputeBuffer positionsBuffer, normalsBuffer;

        // material property
        private MaterialPropertyBlock propertyBlock;

        private bool updatePosition = false;

        private Bounds bounds;

        private enum Shape
        {
            Plane,
            UVSphere,
            Torus,
            OctahedronSphere
        }

        private static Shapes.ScheduleDelegate[] shapeJobs =
        {
            Shapes.Job<Shapes.Plane>.ScheduleParallel,
            Shapes.Job<Shapes.UVSphere>.ScheduleParallel,
            Shapes.Job<Shapes.Torus>.ScheduleParallel,
            Shapes.Job<Shapes.OctahedronSphere>.ScheduleParallel
        };

        protected abstract void EnableVisualization(int dataLength, MaterialPropertyBlock propertyBlock);

        protected abstract void DisableVisualization();

        protected abstract void UpdateVisualization(NativeArray<float3x4> positions, int resolution, JobHandle handle);

        private void OnEnable()
        {
            int length = resolution * resolution;
            // make vector work for odd length
            length = length / 4 + (length & 1);

            // CPU buffer
            positions = new NativeArray<float3x4>(length, Allocator.Persistent);
            normals = new NativeArray<float3x4>(length, Allocator.Persistent);

            // GPU buffer, the size is still uint,float3 and float3
            positionsBuffer = new ComputeBuffer(length * 4, 3 * 4);
            normalsBuffer = new ComputeBuffer(length * 4, 3 * 4);

            invResolution = 1.0f / resolution;
            updatePosition = true;

            // set property for rendering
            propertyBlock ??= new MaterialPropertyBlock();
            // update hash visualization data
            EnableVisualization(length, propertyBlock);
            // bind buffer
            propertyBlock.SetBuffer(positionId, positionsBuffer);
            propertyBlock.SetBuffer(normalId, normalsBuffer);
        }

        private void OnDisable()
        {
            positions.Dispose();
            normals.Dispose();

            positionsBuffer.Release();
            normalsBuffer.Release();

            positionsBuffer = null;
            normalsBuffer = null;

            DisableVisualization();
        }

        private void OnValidate()
        {
            if (positionsBuffer != null && enabled)
            {
                OnDisable();
                OnEnable();
            }
        }

        private void Update()
        {
            // using cos so that we can stop animating, but still got displacement
            float displacement = cos(2.0f * PI * Time.time * speed) * displacementScale;
            
            if (updatePosition || transform.hasChanged)
            {
                updatePosition = false;
                transform.hasChanged = false;
                // 感觉此处不应该提供positions？直接使用一个固定的坐标不就行了吗？
                var shapeJobHandle = shapeJobs[(int)shape](
                    positions, normals, resolution,
                    transform.localToWorldMatrix, default);
                
                UpdateVisualization(positions, resolution,shapeJobHandle);

                bounds = new Bounds(
                    transform.position,
                    float3(2.0f * cmax(abs(transform.lossyScale)) + displacement)
                );

                // copy data from CPU to GPU
                positionsBuffer.SetData(positions.Reinterpret<float3>(3 * 4 * 4));
                normalsBuffer.SetData(normals.Reinterpret<float3>(3 * 4 * 4));

                // update rendering parameter
                // (resolution,size,normal displacement)
            }
            
            // displacement 随着时间不断跳动
            // displacement = ;
            
            propertyBlock.SetVector(configId, new Vector4(
                resolution, instanceScale * invResolution, displacement));

            // 直接绘制即可
            Graphics.DrawMeshInstancedProcedural(
                instanceMesh, 0, material,
                bounds, resolution * resolution, propertyBlock);
        }
    }
    
}