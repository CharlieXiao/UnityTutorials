using ProceduralMeshes.Streams;
using UnityEngine;
using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace ProceduralMeshes.Meshes
{
    // Each face has individual
    public struct CubeSphere : IMeshGenerator
    {
        private struct Side
        {
            public int id;
            // 由于一个正方体包含六个面，我们需要一个数据结构记录当前表示的是哪一个面
            // uvOrigin表示当前side对应的起点（例如back，其对应的起点就是(-1,-1,-1),uVector和vVector分别表示该面两个方向在世界坐标下的位置
            // back对应的两个方向就是uVector为(1,0,0) vVector为(0,1,0)
            public float3 uvOrigin, uVector, vVector;
        }

        public int Resolution { get; set; }
        public int VertexCount => 6 * 4 * Resolution * Resolution;
        public int IndexCount => 6 * 6 * Resolution * Resolution;
        public int JobLength => 6 * Resolution;

        public Bounds Bounds => new Bounds(Vector3.zero, new Vector3(2.0f, 2.0f, 2.0f));

        // this can be called from job thread many times

        private static Side GetSide(int id) => id switch
        {
            0 => new Side
            {
                id = id,
                uvOrigin = -1.0f,
                uVector = 2.0f * right(),
                vVector = 2.0f * up(),
            },
            1 => new Side
            {
                id = id,
                uvOrigin = float3(1.0f,-1.0f,-1.0f),
                uVector = 2.0f * forward(),
                vVector = 2.0f * up(),
            },
            2 => new Side
            {
                id = id,
                uvOrigin = -1.0f,
                uVector = 2.0f * forward(),
                vVector = 2.0f * right(),
            },
            3 => new Side
            {
                id = id,
                uvOrigin = float3(-1.0f,-1.0f,1.0f),
                uVector = 2.0f * up(),
                vVector = 2.0f * right(),
            },
            4 => new Side
            {
                id = id,
                uvOrigin = -1.0f,
                uVector = 2.0f * up(),
                vVector = 2.0f * forward(),
            },
            // 5
            _ => new Side
            {
                id = id,
                uvOrigin = float3(-1.0f,1.0f,-1.0f),
                uVector = 2.0f * right(),
                vVector = 2.0f * forward(),
            },
        };

        // stupid way of Cube => Sphere mapping
        private static float3 CubeToSphere(float3 p) => normalize(p);
        
        // more evenly distributed cube to sphere mapping
        // using the formula => 1 = (1-x^2)(1-y^2)(1-z^2)
        // 加上了一个二次映射
        // 也就是说，我们使用了 a = sqrt(1-x^2) 来替换（这样a本来就是服从一个圆周上的分布，有点重要性采样的思想在里面）
        private static float3 UniformCubeToSphere(float3 p) => p * sqrt(
            1f - ((p * p).yxx + (p * p).zzy) / 2f + (p * p).yxx * (p * p).zzy / 3f
        );
        
        public void Execute<S>(int i, S streams) where S : struct, IMeshStream
        {
            int u = i / 6;
            var side = GetSide(i - 6 * u);
            // vertexId,triangleId
            // every square contains 4 vertices, and two triangles
            // in one row, there will be resolution * original
            int vi = 4 * Resolution * (Resolution * side.id + u), ti = 2 * Resolution * (Resolution * side.id + u);


            float3 uA = side.uvOrigin + side.uVector * u / Resolution;
            float3 uB = side.uvOrigin + side.uVector * (u + 1) / Resolution;
            float3 pA = UniformCubeToSphere(uA), pB = UniformCubeToSphere(uB);

            var vertex = new Vertex();
            vertex.tangent = float4(normalize(pB - pA), -1.0f);

            // 每一行其对应的z值都是固定的
            for (int v = 1; v <= Resolution; ++v, vi += 4, ti += 2)
            {
                float3 pC = UniformCubeToSphere(uA + side.vVector * v / Resolution);
                float3 pD = UniformCubeToSphere(uB + side.vVector * v / Resolution);

                vertex.position = pA;
                vertex.normal = normalize(cross(pC-pA,vertex.tangent.xyz));
                vertex.texCoord0 = float2(0.0f, 0.0f);
                streams.SetVertex(vi + 0, vertex);

                vertex.position = pB;
                vertex.normal = normalize(cross(pD-pB,vertex.tangent.xyz));
                vertex.texCoord0 = float2(1.0f, 0.0f);
                streams.SetVertex(vi + 1, vertex);

                // 在中间更新切线位置（由于这一次的pD-pC 就是下一次的pB-pA，可以重复使用计算结果
                vertex.tangent.xyz = normalize(pD - pC);

                vertex.position = pC;
                vertex.normal = normalize(cross(pC-pA,vertex.tangent.xyz));
                vertex.texCoord0 = float2(0.0f, 1.0f);
                streams.SetVertex(vi + 2, vertex);

                vertex.position = pD;
                vertex.normal = normalize(cross(pD-pB,vertex.tangent.xyz));
                vertex.texCoord0 = float2(1.0f, 1.0f);
                streams.SetVertex(vi + 3, vertex);

                // actually there are many redundant vertices
                // so we need to share the vertices
                streams.SetTriangle(ti + 0, vi + int3(0, 2, 1));
                streams.SetTriangle(ti + 1, vi + int3(1, 2, 3));

                pA = pC;
                pB = pD;
            }
        }
    }
}