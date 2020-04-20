using JeremyAnsel.DirectX.DXMath;
using System.Runtime.InteropServices;

namespace Tutorial06
{
    [StructLayout(LayoutKind.Sequential)]
    struct SimpleVertex
    {
        public XMFloat3 Pos;

        public XMFloat3 Normal;

        public static readonly uint Size = (uint)Marshal.SizeOf(typeof(SimpleVertex));

        public SimpleVertex(XMFloat3 pos, XMFloat3 normal)
        {
            this.Pos = pos;
            this.Normal = normal;
        }
    }
}
