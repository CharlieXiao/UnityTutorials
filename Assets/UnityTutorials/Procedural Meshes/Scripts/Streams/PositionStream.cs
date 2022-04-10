using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using static Unity.Mathematics.math;

namespace ProceduralMeshes.Streams
{
    // PositionOnly stream useful for Cube Sphere with cube maps
    public struct PositionStream : IMeshStream
    {
        [NativeDisableContainerSafetyRestriction]
        private NativeArray<float3> positions;

        [NativeDisableContainerSafetyRestriction]
        private NativeArray<TriangleUInt16> triangles;

        public void Setup(Mesh.MeshData data, Bounds bounds, int vertexCount, int indexCount)
        {
            var descriptor =
                new NativeArray<VertexAttributeDescriptor>(
                    1, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

            // position float3
            descriptor[0] = new VertexAttributeDescriptor(
                dimension: 3, stream: 0, format: VertexAttributeFormat.Float32);

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

            triangles = data.GetIndexData<ushort>().Reinterpret<TriangleUInt16>(2);

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetVertex(int index, Vertex data)
        {
            positions[index] = data.position;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetTriangle(int index, int3 triangle)
        {
            triangles[index] = triangle;
        }
    }
}