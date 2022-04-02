using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Random = UnityEngine.Random;

using quaternion = Unity.Mathematics.quaternion;

namespace Basics_6
{
    public class Fractal : MonoBehaviour
    {

        private const int MaxDepth = 8;

        [SerializeField, Range(2, MaxDepth)] private int depth = 4;

        [SerializeField] private Mesh mesh;

        [SerializeField] private Material material;

        [SerializeField] private Gradient gradient;
        
        private static readonly float3[] Directions = {
            up(), right(), left(), forward(), back()
        };

        private static readonly quaternion[] Rotations = {
            quaternion.identity,
            quaternion.RotateZ(-0.5f * PI), quaternion.RotateZ(0.5f * PI),
            quaternion.RotateX(0.5f * PI), quaternion.RotateX(-0.5f * PI)
        };

        private readonly int MatricsId = Shader.PropertyToID("_Matrices"),
            // Fractal的颜色
            BaseColorId = Shader.PropertyToID("_BaseColor"),
            // 随机数
            SequenceNumbersId = Shader.PropertyToID("_SequenceNumbers");

        private static MaterialPropertyBlock PropertyBlock;
        
        struct FractalPart {
            public float3 worldPosition,direction;
            public quaternion worldRotation,rotation;
            public float spinAngle;
        }

        private NativeArray<FractalPart>[] Parts;
        
        // 对应每一个part矩阵
        private NativeArray<float3x4>[] Matrics;
        
        // 传输数据到shader
        private ComputeBuffer[] MatricsBuffers;

        private Vector4[] SequenceNumbers;
        
        [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
        struct UpdateFractalLevelJob : IJobFor
        {
            public float SpinAngleDelta;
            public float Scale;
            
            [ReadOnly]
            public NativeArray<FractalPart> ParentParts;
            public NativeArray<FractalPart> LevelParts;
            [WriteOnly]
            public NativeArray<float3x4> LevelMatrics;
            
            public void Execute(int i)
            {
                FractalPart parentPart = ParentParts[i / 5];
                FractalPart part = LevelParts[i];
                part.spinAngle += SpinAngleDelta;
                part.worldRotation = mul(parentPart.worldRotation,
                    mul(part.rotation, quaternion.RotateY(part.spinAngle)));
                part.worldPosition =
                    parentPart.worldPosition + mul(parentPart.worldRotation,1.5f * Scale * part.direction);
                LevelParts[i] = part;
                float3x3 r = float3x3(part.worldRotation) * Scale;
                LevelMatrics[i] = float3x4(r.c0, r.c1, r.c2, part.worldPosition);
            }
        }
        

        FractalPart CreatePart(int ci)
        {
            return new FractalPart
            {
                spinAngle = 0.0f,
                worldPosition = Vector3.zero,
                worldRotation = Quaternion.identity,
                rotation = Rotations[ci],
                direction = Directions[ci]
            };
        }

        private void OnEnable()
        {
            Debug.Log("enabled...");
            // allocate memory
            Parts = new NativeArray<FractalPart>[depth];
            Matrics = new NativeArray<float3x4>[depth];
            MatricsBuffers = new ComputeBuffer[depth];
            SequenceNumbers = new Vector4[depth];
            
            const int stride = 12 * 4;
            for (int i = 0, length = 1; i < Parts.Length; ++i, length *= 5)
            {
                // direct access NativeArray in C# has a little extra overhead
                // so we need to change code to Burst-compiled Job
                Parts[i] = new NativeArray<FractalPart>(length,Allocator.Persistent);
                Matrics[i] = new NativeArray<float3x4>(length,Allocator.Persistent);
                MatricsBuffers[i] = new ComputeBuffer(length, stride);
                SequenceNumbers[i] = new Vector4(Random.value, Random.value);
            }
            // initialize
            Parts[0][0] = CreatePart(0);
            for (int li = 1; li < Parts.Length; ++li)
            {
                var levelParts = Parts[li];
                for (int fpi = 0; fpi < levelParts.Length; ++fpi)
                {
                    levelParts[fpi] = CreatePart(fpi%5);
                }
            }

            PropertyBlock ??= new MaterialPropertyBlock();
        }

        private void OnDisable()
        {
            Debug.Log("disabled");
            for (int i=0;i<MatricsBuffers.Length;++i)
            {
                MatricsBuffers[i].Release();
                Parts[i].Dispose();
                Matrics[i].Dispose();
            }
            // release resources
            Parts = null;
            Matrics = null;
            MatricsBuffers = null;
            SequenceNumbers = null;
        }

        private void OnValidate()
        {
            if (Parts != null && enabled)
            {
                Debug.Log("validate");
                // manually release resources
                OnDisable();
                // and than acquire resources again to adopt to changes
                OnEnable();
            }
        }

        private void Update()
        {
            float spinAngleDelta = 0.125f * PI * Time.deltaTime;
            
            // apply rotations to all parts
            FractalPart rootPart = Parts[0][0];
            rootPart.spinAngle += spinAngleDelta;
            rootPart.worldRotation = mul(transform.rotation, quaternion.RotateY(rootPart.spinAngle));
            rootPart.worldPosition = transform.position;
            Parts[0][0] = rootPart;
            float scale = transform.lossyScale.x;
            float3x3 r = float3x3(rootPart.worldRotation) * scale;
            Matrics[0][0] = float3x4(r.c0, r.c1, r.c2, rootPart.worldPosition);
            JobHandle jobHandle = default;
            for (int li = 1; li < Parts.Length; ++li)
            {
                scale *= 0.5f;
                // parallel running
                jobHandle = new UpdateFractalLevelJob
                {
                    SpinAngleDelta = spinAngleDelta,
                    Scale = scale,
                    ParentParts = Parts[li - 1],
                    LevelParts = Parts[li],
                    LevelMatrics = Matrics[li]
                }.ScheduleParallel(Parts[li].Length,8,jobHandle);
            }
            jobHandle.Complete();
            // copy data to compute buffer
            var bounds = new Bounds(rootPart.worldPosition, 3.0f * scale * Vector3.one);
            for (int i = 0; i < MatricsBuffers.Length; ++i)
            {
                ComputeBuffer buffer = MatricsBuffers[i];
                buffer.SetData(Matrics[i]);
                PropertyBlock.SetBuffer(MatricsId,buffer);
                PropertyBlock.SetColor(BaseColorId,gradient.Evaluate(i/(MatricsBuffers.Length-1.0f)));
                PropertyBlock.SetVector(SequenceNumbersId,SequenceNumbers[i]);
                Graphics.DrawMeshInstancedProcedural(mesh,0,material,bounds,buffer.count,PropertyBlock);
            }
        }
    }
}