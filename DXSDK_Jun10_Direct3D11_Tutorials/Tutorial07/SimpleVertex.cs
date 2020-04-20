using JeremyAnsel.DirectX.DXMath;
using System.Runtime.InteropServices;

namespace Tutorial07
{
    [StructLayout(LayoutKind.Sequential)]
    struct SimpleVertex
    {
        public XMFloat3 Pos;

        public XMFloat2 Tex;

        public static readonly uint Size = (uint)Marshal.SizeOf(typeof(SimpleVertex));

        public SimpleVertex(XMFloat3 pos, XMFloat2 tex)
        {
            this.Pos = pos;
            this.Tex = tex;
        }
    }
}
