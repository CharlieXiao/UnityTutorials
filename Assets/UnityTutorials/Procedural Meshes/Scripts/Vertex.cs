using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine;

namespace ProceduralMeshes
{
    public struct Vertex
    {
        public float3 position, normal;
        public float4 tangent;
        public float2 texCoord0;
    }
}