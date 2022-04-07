using ProceduralMeshes.Streams;
using UnityEngine;

namespace ProceduralMeshes.Meshes
{
    public interface IMeshGenerator
    {
        int Resolution { get; set; }
        int VertexCount { get; }
        int IndexCount { get; }
        int JobLength { get; }
        
        Bounds Bounds { get; }
        // using this, we could make Generator adapt to different stream types
        // single stream, multi stream, mix stream
        void Execute<S>(int i, S streams) where S : struct, IMeshStream;
    }
}