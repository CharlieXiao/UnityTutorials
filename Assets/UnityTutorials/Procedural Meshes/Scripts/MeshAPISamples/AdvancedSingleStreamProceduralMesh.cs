using UnityEngine;
using Unity.Collections;
using UnityEngine.Rendering;
using Unity.Mathematics;
using static Unity.Mathematics.math;

using System.Runtime.InteropServices;

namespace ProceduralMeshes.MeshAPISamples
{
    [RequireComponent(typeof(MeshFilter),typeof(MeshRenderer))]
    public class AdvancedSingleStreamProceduralMesh : MonoBehaviour
    {
        // we have the ensure the order is not changed
        [StructLayout(LayoutKind.Sequential)]
        struct Vertex
        {
            public float3 position, normal;
            public half4 tangent;
            public half2 texCoord0;
        }
        
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
            // the data will be passed by PNTX PNTX PNTX PNTX
            vertexAttributes[0] = new VertexAttributeDescriptor(dimension: 3, stream: 0);
            vertexAttributes[1] = new VertexAttributeDescriptor(VertexAttribute.Normal, dimension: 3, stream: 0);
            vertexAttributes[2] = new VertexAttributeDescriptor(VertexAttribute.Tangent, dimension: 4, stream: 0,format:VertexAttributeFormat.Float16);
            vertexAttributes[3] = new VertexAttributeDescriptor(VertexAttribute.TexCoord0, dimension: 2, stream: 0,format:VertexAttributeFormat.Float16);

            meshData.SetVertexBufferParams(vertexCount, vertexAttributes);
            vertexAttributes.Dispose();

            NativeArray<Vertex> vertices = meshData.GetVertexData<Vertex>(stream: 0);
            
            half h0 = half(0.0f), h1 = half(1.0f);

            var vertex = new Vertex {
                normal = back(),
                tangent = half4(h1, h0, h0, half(-1.0f))
            };

            vertex.position = 0f;
            vertex.texCoord0 = h0;
            vertices[0] = vertex;

            vertex.position = right();
            vertex.texCoord0 = half2(h1, h0);
            vertices[1] = vertex;

            vertex.position = up();
            vertex.texCoord0 = half2(h0, h1);
            vertices[2] = vertex;

            vertex.position = float3(1f, 1f, 0f);
            vertex.texCoord0 = h1;
            vertices[3] = vertex;

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