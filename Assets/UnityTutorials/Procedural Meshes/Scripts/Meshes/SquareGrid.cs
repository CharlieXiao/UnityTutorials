using ProceduralMeshes.Streams;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;
using float2 = Unity.Mathematics.float2;

namespace ProceduralMeshes.Meshes
{
    public struct SquareGrid : IMeshGenerator
    {
        public int Resolution { get; set; }
        public int VertexCount => 4 * Resolution * Resolution;
        public int IndexCount => 6 * Resolution * Resolution;
        public int JobLength => Resolution;

        public Bounds Bounds => new Bounds(Vector3.zero, new Vector3(1.0f,0.0f, 1.0f));

        // this can be called from job thread many times
        public void Execute<S>(int u, S streams) where S : struct, IMeshStream
        {
            // vertexId,triangleId
            // every square contains 4 vertices, and two triangles
            // in one row, there will be resolution * original
            int vi = 4 * Resolution * u, ti = 2 * Resolution * u;
            
            var vertex = new Vertex();
            vertex.normal.y = 1.0f;
            vertex.tangent.xw = float2(1.0f, 1.0f);
            
            var zCoordinates = float2(u, u + 1.0f) / Resolution - 0.5f;
            
            // 每一行其对应的z值都是固定的
            for (int x = 0; x < Resolution; ++x, vi+=4,ti+=2)
            {
                // scaled
                var xCoordinates = float2(x, x + 1.0f) / Resolution - 0.5f;

                vertex.position.z = zCoordinates.x;
                
                vertex.position.x = xCoordinates.x;
                // here we need to manually set texture coordinate
                vertex.texCoord0 = float2.zero;
                streams.SetVertex(vi + 0, vertex);
                
                vertex.position.x = xCoordinates.y;
                vertex.texCoord0 = float2(1.0f, 0.0f);
                streams.SetVertex(vi + 1, vertex);

                vertex.position.z = zCoordinates.y;
                
                vertex.position.x = xCoordinates.x;
                vertex.texCoord0 = float2(0.0f, 1.0f);
                streams.SetVertex(vi + 2, vertex);

                vertex.position.x = xCoordinates.y;
                vertex.texCoord0 = float2(1.0f,1.0f);
                streams.SetVertex(vi + 3, vertex);
            
                // actually there are many redundant vertices
                // so we need to share the vertices
                streams.SetTriangle(ti + 0,vi + int3(0,2,1));
                streams.SetTriangle(ti + 1,vi + int3(1,2,3));   
            }
        }
    }
}