using ProceduralMeshes.Meshes;
using ProceduralMeshes.Streams;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using UnityEngine;

namespace ProceduralMeshes
{
    public delegate JobHandle MeshJobScheduleParallel(
        Mesh mesh, Mesh.MeshData meshData, 
        int resolution, JobHandle dependency);
    
    [BurstCompile(FloatPrecision.Standard,FloatMode.Fast,CompileSynchronously = true)]
    public struct MeshJob<G, S> : IJobFor
        where G : struct, IMeshGenerator
        where S : struct, IMeshStream
    {
        private G generator;
        
        [WriteOnly]
        private S streams;
        
        public void Execute(int index) => generator.Execute(index,streams);



        public static JobHandle ScheduleParallel(Mesh mesh,Mesh.MeshData meshData,int resolution, JobHandle dependency)
        {
            var job = new MeshJob<G, S>();
            job.generator.Resolution = resolution;
            // 执行之前先准备好数据
            job.streams.Setup(
                meshData,
                // 设置mesh bounds，要不然读取不到bounds
                mesh.bounds = job.generator.Bounds,
                job.generator.VertexCount,
                job.generator.IndexCount);
            return job.ScheduleParallel(job.generator.JobLength, 1, dependency);
        }
    }
}