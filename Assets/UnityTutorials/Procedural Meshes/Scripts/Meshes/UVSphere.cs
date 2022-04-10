using ProceduralMeshes.Streams;
using UnityEngine;
using static Unity.Mathematics.math;

namespace ProceduralMeshes.Meshes
{
    public struct UVSphere : IMeshGenerator
    {
        public int Resolution { get; set; }
        private int ResolutionV => 2 * Resolution;
        private int ResolutionU => 4 * Resolution;
        // 虽然其他面的三角形都会共用同一个顶点，且他们的法向量方向相同，但是其切线方向都不一样（便于计算切线空间）
        // 因此每个三角形需要单独存储两个极点
        public int VertexCount => (ResolutionU + 1) * (ResolutionV + 1) - 2;
        public int IndexCount => 6 * ResolutionU * (ResolutionV - 1);
        public int JobLength => ResolutionU + 1;
        public Bounds Bounds => new Bounds(Vector3.zero, new Vector3(2.0f, 2.0f, 2.0f));

        public void Execute<S>(int u, S streams) where S : struct, IMeshStream
        {
            if (u == 0)
            {
                ExecuteSeam(streams);
            }
            else
            {
                ExecuteRegular(u, streams);
            }
        }

        private void ExecuteSeam<S>(S streams) where S : struct, IMeshStream
        {
            // no need to two pole vertex
            // step: uv square -> cylinder -> sphere
            var vertex = new Vertex();
            // setup the north-pole(for many times)
            // fixes the twisting of tangent space
            vertex.tangent.x = 1.0f;
            vertex.tangent.w = -1.0f;
            
            float sinPhi = 0.0f;
            float cosPhi = 1.0f;
            vertex.tangent.xz = float2(cosPhi, sinPhi);
            vertex.texCoord0.x = 0.0f;
            // the vertex count per column is resolutionV + 1

            for (int v = 1; v < ResolutionV; ++v)
            {
                float vt = (float)v / ResolutionV;
                // using inline declaration
                sincos(PI * vt,out float sinTheta,out float cosTheta);
                vertex.position.xz = float2(sinTheta * sinPhi, -sinTheta * cosPhi);
                vertex.position.y = -cosTheta;
                vertex.normal = vertex.position;
                vertex.texCoord0.y = vt;
                streams.SetVertex(v-1, vertex);
            }
        }

        private void ExecuteRegular<S>(int u, S streams) where S : struct, IMeshStream
        {
            // step: uv square -> cylinder -> sphere
            int vi = (ResolutionV + 1) * u - 2, ti = 2 * (ResolutionV - 1) * (u - 1);
            var vertex = new Vertex();

            float ut = (float)u / ResolutionU;

            // setup south pole
            float uSouthPole = ut - 0.5f / ResolutionU;
            vertex.position.y = vertex.normal.y = -1.0f;
            sincos(2.0f * PI * uSouthPole,out vertex.tangent.z,out vertex.tangent.x);
            vertex.tangent.w = -1.0f;
            vertex.texCoord0.x = uSouthPole;
            streams.SetVertex(vi, vertex);

            // setup north pole
            vertex.position.y = vertex.normal.y = 1.0f;
            vertex.texCoord0.y = 1.0f;
            streams.SetVertex(vi + ResolutionV, vertex);
            ++vi;
            
            sincos(2.0f * PI * ut,out float sinPhi,out float cosPhi );
            vertex.tangent.xz = float2(cosPhi, sinPhi);
            vertex.texCoord0.x = ut;

            int shiftLeft = (u == 1 ? 0 : -1) - ResolutionV;
            
            streams.SetTriangle(ti, vi + int3(-1, shiftLeft, 0));
            ++ti;

            for (int v = 1; v < ResolutionV; ++v, ++vi)
            {
                float vt = (float)v / ResolutionV;
                sincos(PI * vt,out float sinTheta,out float cosTheta);
                vertex.position.xz = float2(sinTheta * sinPhi, -sinTheta * cosPhi);
                vertex.position.y = -cosTheta;
                vertex.normal = vertex.position;
                vertex.texCoord0.y = vt;
                streams.SetVertex(vi, vertex);
                if (v > 1)
                {
                    // previous level vertex is vi - (resolution + 1) 
                    // so the triangle is just like this
                    streams.SetTriangle(ti, vi + int3(shiftLeft-1, shiftLeft, -1));
                    streams.SetTriangle(ti + 1, vi + int3(-1, shiftLeft, 0));
                    ti += 2;
                }
            }

            // 对于北极点同理，只需要一个三角形即可（四边形有两个点连在一起了
            streams.SetTriangle(ti, vi + int3(shiftLeft-1, 0, -1));
        }
    }
}