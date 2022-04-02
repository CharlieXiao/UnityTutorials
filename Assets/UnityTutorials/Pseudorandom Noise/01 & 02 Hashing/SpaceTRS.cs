using Unity.Mathematics;

namespace UnityTutorials.Pseudorandom_Noise._01_Hashing
{
    [System.Serializable]
    public struct SpaceTRS
    {
        // 由于我们的hash是在zox平面上进行的，只能进行一些操作
        // translate x，z
        // scale x,z
        // rotate y
        public float3 translation, rotation, scale;
        public float3x4 Matrix {
            get
            {
                float4x4 m = float4x4.TRS(translation, quaternion.EulerZXY(math.radians(rotation)), scale);
                // the last row is always (0,0,0,1), so we don't need to store it
                return math.float3x4(m.c0.xyz, m.c1.xyz, m.c2.xyz, m.c3.xyz);
            }
        }
    }
}