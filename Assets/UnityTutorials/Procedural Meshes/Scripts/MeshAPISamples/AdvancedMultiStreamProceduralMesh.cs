using UnityEngine;
using Unity.Collections;
using UnityEngine.Rendering;
using Unity.Mathematics;
using static Unity.Mathematics.math;

// there is also a function to create float3, which will be confused by ide
// so we will manually include this expression
using float3 = Unity.Mathematics.float3;

namespace ProceduralMeshes.MeshAPISamples
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class AdvancedMultiStreamProceduralMesh : MonoBehaviour
    {
        private void OnEnable()
        {
            // position,normal,tangent,uv
            int vertexAttributeCount = 4;
            int vertexCount = 4;
            int triangleIndexCount = 6;
            // instead of using Array, we will using MeshDataArray to create data
            // 1 means we will create just 1 mesh
            Mesh.MeshDataArray meshDataArray = Mesh.AllocateWritableMeshData(1);
            Mesh.MeshData meshData = meshDataArray[0];

            // manually create and dispose array data
            var vertexAttributes = new NativeArray<VertexAttributeDescriptor>(
                vertexAttributeCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

            // multi-stream approach
            // 一个顶点的数据必须是内存对齐的，如果我们将法线的大小叶转换成float16的话，我们一个顶点的大小就是
            // 3 * 4 * 4 + 3 * 4 * 4 + 4 * 2 * 4 + 2 * 2 * 4 => 144 bytes
            vertexAttributes[0] = new VertexAttributeDescriptor(dimension: 3, stream: 0);
            vertexAttributes[1] = new VertexAttributeDescriptor(VertexAttribute.Normal, dimension: 3, stream: 1);
            vertexAttributes[2] = new VertexAttributeDescriptor(VertexAttribute.Tangent, dimension: 4, stream: 2,format:VertexAttributeFormat.Float16);
            vertexAttributes[3] = new VertexAttributeDescriptor(VertexAttribute.TexCoord0, dimension: 2, stream: 3,format:VertexAttributeFormat.Float16);

            // the data will be passed by PPPP NNNN TTTT XXXX
            
            meshData.SetVertexBufferParams(vertexCount, vertexAttributes);
            vertexAttributes.Dispose();

            // set data now, we are using NativeArray so there is no conversion
            NativeArray<float3> positions = meshData.GetVertexData<float3>(0);
            positions[0] = float3(0.0f, 0.0f, 0.0f);
            positions[1] = float3(1.0f, 0.0f, 0.0f);
            positions[2] = float3(0.0f, 1.0f, 0.0f);
            positions[3] = float3(1.0f, 1.0f, 0.0f);

            NativeArray<float3> normals = meshData.GetVertexData<float3>(1);
            normals[0] = float3(0.0f, 0.0f, -1.0f);
            normals[1] = float3(0.0f, 0.0f, -1.0f);
            normals[2] = float3(0.0f, 0.0f, -1.0f);
            normals[3] = float3(0.0f, 0.0f, -1.0f);

            half h0 = half(0.0f), h1 = half((1.0f));

            NativeArray<half4> tangents = meshData.GetVertexData<half4>(2);
            tangents[0] = half4(h1, h0, h0, half(-1.0f));
            tangents[1] = half4(h1, h0, h0, half(-1.0f));
            tangents[2] = half4(h1, h0, h0, half(-1.0f));
            tangents[3] = half4(h1, h0, h0, half(-1.0f));

            NativeArray<half2> texCoords = meshData.GetVertexData<half2>(3);
            texCoords[0] = half2(h0, h0);
            texCoords[1] = half2(h1, h0);
            texCoords[2] = half2(h0, h1);
            texCoords[3] = half2(h1, h1);

            meshData.SetIndexBufferParams(triangleIndexCount, IndexFormat.UInt16);

            NativeArray<ushort> triangleIndices = meshData.GetIndexData<ushort>();
            triangleIndices[0] = 0;
            triangleIndices[1] = 2;
            triangleIndices[2] = 1;
            triangleIndices[3] = 1;
            triangleIndices[4] = 2;
            triangleIndices[5] = 3;

            var bounds = new Bounds(new Vector3(0.5f, 0.5f), new Vector3(1.0f, 1.0f));

            meshData.subMeshCount = 1;
            meshData.SetSubMesh(0, new SubMeshDescriptor(0, triangleIndexCount)
            {
                bounds = bounds,
                vertexCount = vertexCount
            }, MeshUpdateFlags.DontRecalculateBounds);

            var mesh = new Mesh
            {
                name = "Procedural Mesh",
                bounds = bounds
            };
            // apply mesh data to mesh
            Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, mesh);
            GetComponent<MeshFilter>().mesh = mesh;
        }
    }
}