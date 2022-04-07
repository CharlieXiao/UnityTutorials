using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace ProceduralMeshes.Streams
{
    public struct SingleStream : IMeshStream
    {
        [StructLayout(LayoutKind.Sequential)]
        struct Stream0
        {
            public float3 position, normal;
            public float4 tangent;
            public float2 texCoord0;
        }
        
        // these NativeArray is just memory blocks, so unity may warn us
        [NativeDisableContainerSafetyRestriction]
        private NativeArray<Stream0> stream0;

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
                stream: 0, format: VertexAttributeFormat.Float32);
            // tangent float4
            descriptor[2] = new VertexAttributeDescriptor(
                VertexAttribute.Tangent, dimension: 4,
                stream: 0, format: VertexAttributeFormat.Float32);

            // uv1 float2
            descriptor[3] = new VertexAttributeDescriptor(
                VertexAttribute.TexCoord0, dimension: 2,
                stream: 0, format: VertexAttributeFormat.Float32);

            data.SetVertexBufferParams(vertexCount, descriptor);
            descriptor.Dispose();

            // index uint32
            data.SetIndexBufferParams(indexCount, IndexFormat.UInt16);

            data.subMeshCount = 1;
            data.SetSubMesh(0, new SubMeshDescriptor(0, indexCount)
                {
                    bounds = bounds,
                    vertexCount = vertexCount
                },
                MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices);

            stream0 = data.GetVertexData<Stream0>(stream: 0);
            triangles = data.GetIndexData<ushort>().Reinterpret<TriangleUInt16>(2);
        }

        //always inline method
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetVertex(int index, Vertex data)
        {
            stream0[index] =
                new Stream0
                {
                    position = data.position,
                    normal = data.normal,
                    tangent = data.tangent,
                    texCoord0 = data.texCoord0
                };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetTriangle(int index, int3 triangle)
        {
            triangles[index] = triangle;
        }
    }
}