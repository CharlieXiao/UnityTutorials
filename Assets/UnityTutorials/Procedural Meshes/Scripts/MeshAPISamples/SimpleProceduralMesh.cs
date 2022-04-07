using UnityEngine;

namespace ProceduralMeshes.MeshAPISamples
{
    // when we draw meshes, we need some other component
    [RequireComponent(typeof(MeshFilter),typeof(MeshRenderer))]
    public class SimpleProceduralMesh : MonoBehaviour
    {
        private void OnEnable()
        {
            // just create mesh and manually set the data
            var mesh = new Mesh
            {
                name = "Procedural Mesh"
            };
            
            // when update a mesh 
            // float32, the size will be 4 bytes each
            mesh.vertices = new Vector3[]
            {
                // in xy plane
                // (0,0,0) -> (1,0,0) -> (0,1,0)
                Vector3.zero, Vector3.right, Vector3.up, new Vector3(1.0f,1.0f)
            };

            mesh.normals = new Vector3[]
            {
                // so its normal is (0,0,-1)
                Vector3.back, Vector3.back, Vector3.back,
                Vector3.back
            };
            
            // 要想应用法线贴图，还需要定义切线值，否则由unity自动计算切线位置
            mesh.tangents = new Vector4[]
            {
                // the fourth value to control the direction of bi-tangent vector
                // normal cross tangent -> bi-tangent,
                // and use the fourth value to determine the direction
                new Vector4(1.0f,0.0f,0.0f,-1.0f),
                new Vector4(1.0f,0.0f,0.0f,-1.0f),
                new Vector4(1.0f,0.0f,0.0f,-1.0f),
                new Vector4(1.0f,0.0f,0.0f,-1.0f),

            };

            mesh.uv = new Vector2[]
            {
                // (0,0) -> (1,0) -> (0,1) -> (1,1)
                Vector2.zero, Vector2.right, Vector2.up, Vector2.one, 
            };
            
            // this order also defines the orientation of the face
            // By default triangles are only visible when looking at their front face, not their back face.

            // actually it's uint16, so the size will be 2 bytes each
            mesh.triangles = new int[]
            {
                0, 2, 1, 1, 2, 3
            };

            GetComponent<MeshFilter>().mesh = mesh;
        }
    }
}
