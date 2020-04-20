using JeremyAnsel.DirectX.DXMath;
using System.Runtime.InteropServices;

namespace Tutorial04
{
    [StructLayout(LayoutKind.Sequential)]
    struct SimpleVertex
    {
        public XMFloat3 Pos;

        public XMFloat4 Color;

        public static readonly uint Size = (uint)Marshal.SizeOf(typeof(SimpleVertex));

        public SimpleVertex(XMFloat3 pos, XMFloat4 color)
        {
            this.Pos = pos;
            this.Color = color;
        }
    }
}
