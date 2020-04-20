using JeremyAnsel.DirectX.DXMath;
using System.Runtime.InteropServices;

namespace Tutorial02
{
    [StructLayout(LayoutKind.Sequential)]
    struct SimpleVertex
    {
        public XMFloat3 Position;

        public static readonly uint Size = (uint)Marshal.SizeOf(typeof(SimpleVertex));

        public SimpleVertex(XMFloat3 position)
        {
            this.Position = position;
        }
    }
}
