using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using static Unity.Mathematics.math;

namespace ProceduralMeshes.Streams
{
    public struct MultiStream : IMeshStream
    {
        [NativeDisableContainerSafetyRestriction]
        private NativeArray<float3> positions;
        
        [NativeDisableContainerSafetyRestriction]
        private NativeArray<float3> normals;
        
        [NativeDisableContainerSafetyRestriction]
        private NativeArray<float4> tangents;
        
        [NativeDisableContainerSafetyRestriction]
        private NativeArray<float2> texCoords;

        [NativeDisableContainerSafetyRestriction]
        private NativeArray<TriangleUInt16> triangles;

        public void Setup(Mesh.MeshData data, Bounds bounds, int vertexCount, int indexCount)
        {
            var descriptor =
                new NativeArray<VertexAttributeDescriptor>(
                    4, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

            // position float3
            descriptor[0] = new VertexAttributeDescriptor(
                dimension: 3, stream: 0, format: VertexAttributeFormat.Float32);
            // normal float3
            descriptor[1] = new VertexAttributeDescriptor(
                VertexAttribute.Normal, dimension: 3,
                stream: 1, format: VertexAttributeFormat.Float32);
            // tangent float4
            descriptor[2] = new VertexAttributeDescriptor(
                VertexAttribute.Tangent, dimension: 4,
                stream: 2, format: VertexAttributeFormat.Float32);

            // uv1 float2
            descriptor[3] = new VertexAttributeDescriptor(
                VertexAttribute.TexCoord0, dimension: 2,
                stream: 3, format: VertexAttributeFormat.Float32);
            
            data.SetVertexBufferParams(vertexCount,descriptor);
            descriptor.Dispose();
            
            data.SetIndexBufferParams(indexCount,IndexFormat.UInt16);

            data.subMeshCount = 1;
            data.SetSubMesh(0,new SubMeshDescriptor(0,indexCount)
            {
                bounds = bounds,
                vertexCount = vertexCount
            },MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices);

            positions = data.GetVertexData<float3>(stream: 0);
            normals = data.GetVertexData<float3>(stream: 1);
            tangents = data.GetVertexData<float4>(stream: 2);
            texCoords = data.GetVertexData<float2>(stream: 3);

            triangles = data.GetIndexData<ushort>().Reinterpret<TriangleUInt16>(2);

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetVertex(int index, Vertex data)
        {
            positions[index] = data.position;
            normals[index] = data.normal;
            tangents[index] = data.tangent;
            texCoords[index] = data.texCoord0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetTriangle(int index, int3 triangle)
        {
            triangles[index] = triangle;
        }
    }
}