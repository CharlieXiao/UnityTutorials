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

        [SerializeField] private bool drawBounds = true;

        [SerializeField] private bool drawVertices = true;

        [SerializeField] private bool drawNormals = true;

        enum MeshType
        {
            SquareGrid,SharedSquareGrid,SharedTriangleGrid,
            PointyHexagonGrid,FlatHexagonGrid
        }

        [SerializeField] private MeshType meshType = MeshType.SquareGrid;

        private static Type[] _MeshTypes =
        {
            typeof(SquareGrid), typeof(SharedSquareGrid), typeof(SharedTriangleGrid), typeof(PointyHexagonGrid), typeof(FlatHexagonGrid)
        };

        [SerializeField] private StreamType streamType = StreamType.Multiple;

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

        private Vector3[] vertices;

        private Vector3[] normals;
        
        private void Awake()
        {
            mesh = new Mesh
            {
                name = "Procedural Mesh"
            };
            GetComponent<MeshFilter>().mesh = mesh;
        }

        void OnValidate() => enabled = true;

        private static void VisualizeBound(Bounds bounds)
        {
            var a = bounds.min;
            var b = bounds.max;

            Gizmos.DrawLine(new Vector3(a.x,a.y,a.z),new Vector3(a.x,b.y,a.z));
            Gizmos.DrawLine(new Vector3(a.x,b.y,a.z),new Vector3(a.x,b.y,b.z));
            Gizmos.DrawLine(new Vector3(a.x,b.y,b.z),new Vector3(a.x,a.y,b.z));
            Gizmos.DrawLine(new Vector3(a.x,a.y,b.z),new Vector3(a.x,a.y,a.z));
            Gizmos.DrawLine(new Vector3(b.x,a.y,a.z),new Vector3(b.x,b.y,a.z));
            Gizmos.DrawLine(new Vector3(b.x,b.y,a.z),new Vector3(b.x,b.y,b.z));
            Gizmos.DrawLine(new Vector3(b.x,b.y,b.z),new Vector3(b.x,a.y,b.z));
            Gizmos.DrawLine(new Vector3(b.x,a.y,b.z),new Vector3(b.x,a.y,a.z));
            Gizmos.DrawLine(new Vector3(a.x,a.y,a.z),new Vector3(b.x,a.y,a.z));
            Gizmos.DrawLine(new Vector3(a.x,b.y,a.z),new Vector3(b.x,b.y,a.z));
            Gizmos.DrawLine(new Vector3(a.x,b.y,b.z),new Vector3(b.x,b.y,b.z));
            Gizmos.DrawLine(new Vector3(a.x,a.y,b.z),new Vector3(b.x,a.y,b.z));
        }

        private void Update()
        {
            GenerateMesh();
            // through this way, the component will not update in every frame
            // then it's update when necessary
            enabled = false;

            vertices = mesh.vertices;
            normals = mesh.normals;
        }

        private void OnDrawGizmos()
        {
            if (mesh == null) return;
            if (drawVertices)
            {
                foreach (Vector3 vertex in vertices)
                {
                    Gizmos.DrawSphere(vertex, 0.02f);
                }
            }

            if (drawNormals)
            {
                for (int i = 0; i < vertices.Length; ++i)
                {
                    Gizmos.DrawRay(vertices[i], normals[i]);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (mesh == null || !drawBounds) return;
            VisualizeBound(mesh.bounds);
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