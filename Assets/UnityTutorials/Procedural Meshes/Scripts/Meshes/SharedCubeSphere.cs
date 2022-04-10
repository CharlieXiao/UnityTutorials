using ProceduralMeshes.Streams;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using UnityEngine;

namespace ProceduralMeshes.Meshes
{
    // 这一段代码真的很复杂，里面的逻辑很多，感觉还是使用基于mesh subdivision的方法做这个比较合适
    // 实际上原理是一样的，但是逻辑会清晰很多
    // Loop Subdivision(三角形的曲面细分，基础图形为Octahedron)和Catmull Clark Subdivision(四边形的曲面细分)
    public struct SharedCubeSphere : IMeshGenerator
    {
        private struct Side
        {
            public int id;

            // 由于一个正方体包含六个面，我们需要一个数据结构记录当前表示的是哪一个面
            // uvOrigin表示当前side对应的起点（例如back，其对应的起点就是(-1,-1,-1),uVector和vVector分别表示该面两个方向在世界坐标下的位置
            // back对应的两个方向就是uVector为(1,0,0) vVector为(0,1,0)
            public float3 uvOrigin, uVector, vVector;

            public int seamStep;

            public bool ConnectToSouthPole => (id & 1) == 0;
        }

        public int Resolution { get; set; }

        // 每一个side都包含Resolution*Resolution个正方形，每个正方形包含4个顶点，共有六个面
        // 因此最终顶点个数就是这么多，存在很多重复的顶点，可以进行优化
        // 实际上对于每个side我们只需要正方形的左上角顶点，且每一行/列都是resolution个顶点，
        // 最后还差两个顶点没有出现
        public int VertexCount => 6 * Resolution * Resolution + 2;
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
                seamStep = 4,
            },
            1 => new Side
            {
                id = id,
                uvOrigin = float3(1.0f, -1.0f, -1.0f),
                uVector = 2.0f * forward(),
                vVector = 2.0f * up(),
                seamStep = 4,
            },
            2 => new Side
            {
                id = id,
                uvOrigin = -1.0f,
                uVector = 2.0f * forward(),
                vVector = 2.0f * right(),
                seamStep = -2,
            },
            3 => new Side
            {
                id = id,
                uvOrigin = float3(-1.0f, -1.0f, 1.0f),
                uVector = 2.0f * up(),
                vVector = 2.0f * right(),
                seamStep = -2,
            },
            4 => new Side
            {
                id = id,
                uvOrigin = -1.0f,
                uVector = 2.0f * up(),
                vVector = 2.0f * forward(),
                seamStep = -2,
            },
            // 5
            _ => new Side
            {
                id = id,
                uvOrigin = float3(-1.0f, 1.0f, -1.0f),
                uVector = 2.0f * right(),
                vVector = 2.0f * forward(),
                seamStep = -2,
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
            // 后面加上的两个2代表south pole和north pole的offset
            int vi = Resolution * (Resolution * side.id + u) + 2;
            int ti = 2 * Resolution * (Resolution * side.id + u);

            bool firstColumn = u == 0;
            ++u;
            // 将u自增只是将这个点放到了右下角而已
            float3 pStart = side.uvOrigin + side.uVector * u / Resolution;

            var vertex = new Vertex();

            if (i == 0)
            {
                // 刚开始，添加两个pole
                // south pole
                vertex.position = -sqrt(1.0f / 3.0f);
                streams.SetVertex(0, vertex);
                // north pole
                vertex.position = -vertex.position;
                streams.SetVertex(1, vertex);
            }

            vertex.position = UniformCubeToSphere(pStart);
            streams.SetVertex(vi, vertex);

            var triangle = int3(
                vi,
                firstColumn && side.ConnectToSouthPole ? 0 : vi - Resolution,
                vi + (firstColumn
                    ? side.ConnectToSouthPole ? side.seamStep * Resolution * Resolution :
                    Resolution == 1 ? side.seamStep : -Resolution + 1
                    : -Resolution + 1
                )
            );

            streams.SetTriangle(ti, triangle);
            ++ti;
            ++vi;

            int zAdd = firstColumn && side.ConnectToSouthPole ? Resolution : 1;
            int zAddLast = firstColumn && side.ConnectToSouthPole ? Resolution :
                !firstColumn && !side.ConnectToSouthPole ? Resolution * ((side.seamStep + 1) * Resolution - u) + u :
                (side.seamStep + 1) * Resolution * Resolution - Resolution + 1;

            // 每一行其对应的z值都是固定的
            for (int v = 1; v < Resolution; ++v, ++vi, ti += 2)
            {
                // 每次选择正方形的左上角那个顶点
                vertex.position = UniformCubeToSphere(pStart + side.vVector * v / Resolution);
                streams.SetVertex(vi, vertex);
                triangle.x += 1;
                triangle.y = triangle.z;
                triangle.z += v == Resolution - 1 ? zAddLast : zAdd;

                streams.SetTriangle(ti + 0, int3(triangle.x - 1, triangle.y, triangle.x));
                streams.SetTriangle(ti + 1, triangle);
            }

            streams.SetTriangle(ti, int3(
                triangle.x,
                triangle.z,
                side.ConnectToSouthPole ? triangle.z + Resolution :
                u == Resolution ? 1 : triangle.z + 1
            ));
        }
    }
}