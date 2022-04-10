using System;
using ProceduralMeshes.Meshes;
using ProceduralMeshes.Streams;
using UnityEngine;
using UnityEngine.Rendering;

namespace ProceduralMeshes
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class ProceduralMesh : MonoBehaviour
    {
        private Mesh mesh;

        [SerializeField, Range(1, 50)] private int resolution = 1;

        [System.Flags]
        enum GizmoMode
        {
            // 必须是二进制掩码才行，否则会出大问题的
            Nothing = 0b0,
            Vertices = 0b1,
            Normals = 0b10,
            Tangents = 0b100,
            BiTangents = 0b1000,
            Bounds = 0b10000,
            Triangles = 0b100000,
        }

        [SerializeField] private GizmoMode gizmoMode;

        enum MeshType
        {
            SquareGrid,
            SharedSquareGrid,
            SharedTriangleGrid,
            PointyHexagonGrid,
            FlatHexagonGrid,
            UVSphere,
            CubeSphere,
            SharedCubeSphere
        }

        [SerializeField] private MeshType meshType = MeshType.SquareGrid;

        private static MeshJobScheduleDelegate[] jobs =
        {
            MeshJob<SquareGrid, SingleStream>.ScheduleParallel,
            MeshJob<SharedSquareGrid, SingleStream>.ScheduleParallel,
            MeshJob<SharedTriangleGrid, SingleStream>.ScheduleParallel,
            MeshJob<PointyHexagonGrid, SingleStream>.ScheduleParallel,
            MeshJob<FlatHexagonGrid, SingleStream>.ScheduleParallel,
            MeshJob<UVSphere, SingleStream>.ScheduleParallel,
            MeshJob<CubeSphere, SingleStream>.ScheduleParallel,
            MeshJob<SharedCubeSphere, PositionStream>.ScheduleParallel,
        };

        enum MaterialMode
        {
            Flat,
            Ripple,

            [Tooltip("Latitude Longitude Map (just like the world map)")]
            LatLonMap,
            CubeMap
        }

        [SerializeField] private MaterialMode materialMode;

        [SerializeField] private Material[] materials;

        [System.Flags]
        public enum MeshOptimizationMode
        {
            Nothing = 0,
            ReorderIndices = 1,
            ReorderVertices = 0b10
        }

        [SerializeField] MeshOptimizationMode meshOptimization;

        [System.NonSerialized] private Vector3[] vertices;

        [System.NonSerialized] private Vector3[] normals;

        [System.NonSerialized] private Vector4[] tangents;

        [System.NonSerialized] private int[] triangles;

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
            GenerateMesh();
            // through this way, the component will not update in every frame
            // then it's update when necessary
            enabled = false;
            // update result here has potential problem after we introduce positionOnlyStream
            // in this case, normals and tangents is [], when we try to access it by index, we will get OutOfBound Exception
            GetComponent<MeshRenderer>().material = materials[(int)materialMode];
        }

        private void OnDrawGizmosSelected()
        {
            if (mesh == null || gizmoMode == GizmoMode.Nothing) return;
            Transform t = transform;
            bool hasNormals = mesh.HasVertexAttribute(VertexAttribute.Normal);
            bool hasTangents = mesh.HasVertexAttribute(VertexAttribute.Tangent);
            bool drawVertices = (gizmoMode & GizmoMode.Vertices) != 0;
            bool drawTriangles = (gizmoMode & GizmoMode.Triangles) != 0;
            bool drawBounds = (gizmoMode & GizmoMode.Bounds) != 0;
            bool drawNormals = (gizmoMode & GizmoMode.Normals) != 0 && hasNormals;
            bool drawTangents = (gizmoMode & GizmoMode.Tangents) != 0 && hasTangents;
            bool drawBiTangents = (gizmoMode & GizmoMode.BiTangents) != 0 && hasNormals && hasTangents;

            vertices = mesh.vertices;
            normals = mesh.normals;
            tangents = mesh.tangents;
            triangles = mesh.triangles;

            for (int i = 0; i < vertices.Length; ++i)
            {
                Vector3 p = t.TransformPoint(vertices[i]);
                if (drawVertices)
                {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawSphere(p, 0.02f);
                }

                // DrawRay中，会使用direction的长度决定射线的长度
                if (drawNormals)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawRay(p, t.TransformVector(normals[i]) * 0.2f);
                }

                if (drawTangents)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawRay(p, t.TransformVector(tangents[i]) * 0.2f);
                }

                if (drawBiTangents)
                {
                    // 需要现场计算
                    Gizmos.color = Color.magenta;
                    Vector3 biTangent = Vector3.Normalize(Vector3.Cross(normals[i], tangents[i]) * tangents[i].w);
                    Gizmos.DrawRay(p, t.TransformVector(biTangent) * 0.2f);
                }
            }

            if (drawTriangles)
            {
                float colorStep = 1f / (triangles.Length - 3);
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    float c = i * colorStep;
                    Gizmos.color = new Color(c, 0f, c);
                    Gizmos.DrawSphere(
                        t.TransformPoint(
                            vertices[triangles[i]] +
                            vertices[triangles[i + 1]] +
                            vertices[triangles[i + 2]]
                        ) * (1f / 3f),
                        0.02f
                    );
                }
            }

            if (drawBounds)
            {
                Gizmos.color = Color.yellow;
                Bounds bounds = mesh.bounds;
                Gizmos.DrawWireCube(t.TransformPoint(bounds.center), bounds.size);
            }
        }

        void GenerateMesh()
        {
            Mesh.MeshDataArray meshDataArray = Mesh.AllocateWritableMeshData(meshCount: 1);
            Mesh.MeshData meshData = meshDataArray[0];

            jobs[(int)meshType](mesh, meshData, resolution, default).Complete();

            Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, mesh);

            if (meshOptimization == MeshOptimizationMode.ReorderIndices)
            {
                mesh.OptimizeIndexBuffers();
            }
            else if (meshOptimization == MeshOptimizationMode.ReorderVertices)
            {
                mesh.OptimizeReorderVertexBuffer();
            }
            else if (meshOptimization != MeshOptimizationMode.Nothing)
            {
                mesh.Optimize();
            }
        }
    }
}