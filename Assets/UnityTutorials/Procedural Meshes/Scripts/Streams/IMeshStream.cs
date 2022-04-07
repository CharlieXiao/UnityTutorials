using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace ProceduralMeshes.Streams
{
    public interface IMeshStream
    {
        // TODO: make this method read some vertex struct to generate data?
        // like void Setup<V>(Mesh.MeshData data, Bounds bounds,int vertexCount,int indexCount) where V : struct,
        // also, make Vertex<float3,float4,float2,float3,uint> ? so this can be checked ?
        void Setup(Mesh.MeshData data,Bounds bounds, int vertexCount, int indexCount);

        void SetVertex(int index, Vertex data);

        void SetTriangle(int index, int3 triangle);
    }
}