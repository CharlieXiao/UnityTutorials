using ProceduralMeshes.Streams;
using UnityEngine;
using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace ProceduralMeshes.Meshes
{
    public struct SharedTriangleGrid : IMeshGenerator
    {
        public int Resolution { get; set; }
        public int VertexCount => (Resolution + 1) * (Resolution + 1);
        public int IndexCount => 6 * Resolution * Resolution;
        public int JobLength => Resolution + 1;

        public Bounds Bounds =>
            new Bounds(Vector3.zero, new Vector3(1.0f + 0.5f / Resolution, 0.0f, sqrt(3.0f) / 2.0f));

        public void Execute<S>(int u, S streams) where S : struct, IMeshStream
        {
            // as usual, we will draw the grid row by row
            int vi = (Resolution + 1) * u, ti = 2 * Resolution * (u - 1);

            int iA = -Resolution - 2, iB = -Resolution - 1, iC = -1, iD = 0;
            var tA = int3(iA, iC, iD);
            var tB = int3(iA, iD, iB);

            float xOffset = -0.25f;
            float uOffset = 0.0f;

            if ((u & 1) == 1)
            {
                xOffset = 0.25f;
                uOffset = 0.5f / (Resolution + 0.5f);
                tA = int3(iA, iC, iB);
                tB = int3(iB, iC, iD);
            }

            xOffset = xOffset / Resolution - 0.5f;

            var vertex = new Vertex();
            vertex.normal.y = 1.0f;
            vertex.tangent.xw = float2(1.0f, 1.0f);

            vertex.position.x = xOffset;
            // scale height, to make a equalateral
            vertex.position.z = ((float)u / Resolution - 0.5f) * sqrt(3f) / 2f;

            vertex.texCoord0.x = uOffset;
            // 1 + 0.5 * Resolution is the actual length of grid
            vertex.texCoord0.y = vertex.position.z / (1f + 0.5f / Resolution) + 0.5f;
            // vertex.texCoord0.y = (float)z / Resolution;
            streams.SetVertex(vi, vertex);

            vi += 1;
            for (int x = 1; x <= Resolution; ++x, ++vi, ti += 2)
            {
                vertex.position.x = (float)x / Resolution + xOffset;
                vertex.texCoord0.x = x / (Resolution + 0.5f) + uOffset;
                streams.SetVertex(vi, vertex);
                if (u < 1)
                {
                    continue;
                }

                streams.SetTriangle(ti + 0, vi + tA);
                streams.SetTriangle(ti + 1, vi + tB);
            }
        }
    }
}