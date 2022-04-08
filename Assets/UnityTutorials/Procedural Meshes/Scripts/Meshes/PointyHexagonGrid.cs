using ProceduralMeshes.Streams;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;
using float2 = Unity.Mathematics.float2;
using float4 = Unity.Mathematics.float4;

namespace ProceduralMeshes.Meshes
{
    public struct PointyHexagonGrid : IMeshGenerator
    {
        public int Resolution { get; set; }
        public int VertexCount => 7 * Resolution * Resolution;
        public int IndexCount => 18 * Resolution * Resolution;
        public int JobLength => Resolution;

        public Bounds Bounds => new Bounds(Vector3.zero, new Vector3(
            (Resolution > 1 ? (2.0f * Resolution + 1) / (4.0f * Resolution) : 0.5f ) * sqrt(3.0f),
            0f, 0.75f + 0.25f / Resolution));

        // this can be called from job thread many times
        public void Execute<S>(int z, S streams) where S : struct, IMeshStream
        {
            // 每一个六边形包含7个顶点，6个三角形，计算其实很简单，只需要逐个计算出点坐标，然后添加即可
            int vi = 7 * Resolution * z, ti = 6 * Resolution * z;

            float2 centerOffset = 0.0f;

            // 在正方形内部画出最大的正六边形（pointy，也就是top和bottom出的尖角）
            // 那么每一个正三角形的边长为1/2，对应的高就为sqrt(3)/4
            float h = sqrt(3.0f) / 4.0f;

            if (Resolution > 1)
            {
                // 中心点的偏移，这里要求是mesh对应的bounds中心位于float3(0.0f,0.0f,0.0f)，因此高度和宽度需要进行微调
                centerOffset.x = (((z & 1) == 0 ? 0.5f : 1.5f) - Resolution) * h;
                centerOffset.y = -0.375f * (Resolution - 1);
            }

            var vertex = new Vertex();
            // common data
            vertex.normal.y = 1.0f;
            vertex.tangent.xw = float2(1.0f, 1.0f);

            for (int x = 0; x < Resolution; ++x, vi += 7, ti += 6)
            {
                var center = (float2(2.0f * h * x, 0.75f * z) + centerOffset) / Resolution;
                // all offset by x and y coordinate
                var xCoordinates = center.x + float2(-h, h) / Resolution;
                var zCoordinates = center.y + float4(-0.5f, -0.25f, 0.25f, 0.5f) / Resolution;


                // center point
                vertex.position.xz = center;
                vertex.texCoord0 = 0.5f;
                streams.SetVertex(vi + 0, vertex);

                vertex.position.x = center.x;
                vertex.position.z = zCoordinates.x;
                vertex.texCoord0 = float2(0.5f, 0.0f);
                streams.SetVertex(vi + 1, vertex);

                vertex.position.x = xCoordinates.x;
                vertex.position.z = zCoordinates.y;
                vertex.texCoord0 = float2(0.5f - h, 0.25f);
                streams.SetVertex(vi + 2, vertex);

                vertex.position.x = xCoordinates.x;
                vertex.position.z = zCoordinates.z;
                vertex.texCoord0 = float2(0.5f - h, 0.75f);
                streams.SetVertex(vi + 3, vertex);

                vertex.position.x = center.x;
                vertex.position.z = zCoordinates.w;
                vertex.texCoord0 = float2(0.5f, 1.0f);
                streams.SetVertex(vi + 4, vertex);

                vertex.position.x = xCoordinates.y;
                vertex.position.z = zCoordinates.z;
                vertex.texCoord0 = float2(0.5f + h, 0.75f);
                streams.SetVertex(vi + 5, vertex);

                vertex.position.x = xCoordinates.y;
                vertex.position.z = zCoordinates.y;
                vertex.texCoord0 = float2(0.5f + h, 0.25f);
                streams.SetVertex(vi + 6, vertex);

                streams.SetTriangle(ti + 0, vi + int3(0, 1, 2));
                streams.SetTriangle(ti + 1, vi + int3(0, 2, 3));
                streams.SetTriangle(ti + 2, vi + int3(0, 3, 4));
                streams.SetTriangle(ti + 3, vi + int3(0, 4, 5));
                streams.SetTriangle(ti + 4, vi + int3(0, 5, 6));
                streams.SetTriangle(ti + 5, vi + int3(0, 6, 1));
            }
        }
    }
}