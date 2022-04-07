using ProceduralMeshes.Streams;
using Unity.Mathematics;
using UnityEngine;

using static Unity.Mathematics.math;

namespace ProceduralMeshes.Meshes
{
    public struct SharedSquareGrid : IMeshGenerator
    {
        public int Resolution { get; set; }
        public int VertexCount => (Resolution + 1) * (Resolution + 1);
        public int IndexCount => 6 * Resolution * Resolution;
        public int JobLength => Resolution + 1;
        public Bounds Bounds => new Bounds(Vector3.zero, new Vector3(0.5f, 0.0f, 0.5f));
        public void Execute<S>(int z, S streams) where S : struct, IMeshStream
        {
            // Rule of thumb, write simple code, leave the optimization to Burst
            // never optimization too early, you have a lot to do
            // for every row, we have to calculate every vertices it shared
            int vi = (Resolution + 1) * z,ti = 2 * Resolution * (z-1);
            var vertex = new Vertex();
            vertex.normal.y = 1.0f;
            vertex.tangent.xw = float2(1.0f, 1.0f);

            vertex.position.x = -0.5f;
            vertex.position.z = (float)z / Resolution - 0.5f;
            vertex.texCoord0.y = (float)z / Resolution;
            streams.SetVertex(vi,vertex);

            vi += 1;
            for (int x = 1; x <= Resolution; ++x, ++vi,ti+=2)
            {
                vertex.position.x = (float)x / Resolution - 0.5f;
                vertex.texCoord0.x = (float)x / Resolution;
                streams.SetVertex(vi,vertex);
                if (z < 1)
                {
                    continue;
                }
                streams.SetTriangle(ti+0,vi+int3(-Resolution-2,-1,-Resolution-1));
                streams.SetTriangle(ti+1,vi+int3(-Resolution-1,-1,0));
            }
        }
    }
}