using JeremyAnsel.DirectX.DXMath;
using System.Runtime.InteropServices;

namespace Tutorial05
{
    [StructLayout(LayoutKind.Sequential)]
    struct ConstantBufferData
    {
        public XMFloat4X4 World;

        public XMFloat4X4 View;

        public XMFloat4X4 Projection;

        public static readonly uint Size = (uint)Marshal.SizeOf(typeof(ConstantBufferData));
    }
}
