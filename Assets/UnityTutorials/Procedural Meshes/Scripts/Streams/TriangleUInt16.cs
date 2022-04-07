using System.Runtime.InteropServices;
using Unity.Mathematics;

namespace ProceduralMeshes.Streams
{
    [StructLayout(LayoutKind.Sequential)]
    public struct TriangleUInt16
    {
        // 使用UInt16最多节点数为65535，极端情况下可能不够用，需要注意
        // 直接使用uint进行计算时会有很多不方便，还是统一使用int进行计算，然后统一转换成TriangleUInt16即可
        public ushort a, b, c;

        public static implicit operator TriangleUInt16(int3 t) => new TriangleUInt16
        {
            a = (ushort)t.x,
            b = (ushort)t.y,
            c = (ushort)t.z
        };
    }
}