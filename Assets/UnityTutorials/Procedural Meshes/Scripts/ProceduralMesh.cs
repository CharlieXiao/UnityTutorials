using System;
using ProceduralMeshes.Meshes;
using ProceduralMeshes.Streams;
using UnityEngine;

namespace ProceduralMeshes
{
    [RequireComponent(typeof(MeshFilter),typeof(MeshRenderer))]
    public class ProceduralMesh : MonoBehaviour
    {
        private Mesh mesh;

        [SerializeField,Range(1,50)]
        private int resolution = 1;

        enum MeshType
        {
            SquareGrid,SharedSquareGrid
        }

        [SerializeField] private MeshType meshType;

        private static Type[] _MeshTypes =
        {
            typeof(SquareGrid), typeof(SharedSquareGrid)
        };

        [SerializeField] private StreamType streamType;

        enum StreamType
        {
            Single,Multiple
        }

        private static Type[] _StreamTypes =
        {
            typeof(SingleStream), typeof(MultiStream)
        };

        private MeshJobScheduleParallel Resolve()
        {
            Type G = _MeshTypes[(int)meshType];
            Type S = _StreamTypes[(int)streamType];
            Type jobType = typeof(MeshJob<,>).MakeGenericType(G, S);
            return (MeshJobScheduleParallel) jobType
                .GetMethod("ScheduleParallel")
                .CreateDelegate(typeof(MeshJobScheduleParallel));
        }
        
        private void Awake()
        {
            mesh = new Mesh
            {
                name = "Procedural Mesh"
            };
            GetComponent<MeshFilter>().mesh = mesh;
        }

        void OnValidate() => enabled = true;

        private void Update()
        {
            Debug.Log("Update Mesh...");
            GenerateMesh();
            // through this way, the component will not update in every frame
            // then it's update when necessary
            enabled = false;
        }

        void GenerateMesh()
        {
            Mesh.MeshDataArray meshDataArray = Mesh.AllocateWritableMeshData(meshCount: 1);
            Mesh.MeshData meshData = meshDataArray[0];
            
            Resolve()(mesh,meshData,resolution,default).Complete();
            
            Mesh.ApplyAndDisposeWritableMeshData(meshDataArray,mesh);
        }
    }
}