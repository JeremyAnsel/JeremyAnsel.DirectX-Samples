using JeremyAnsel.DirectX.DXMath;
using System.Runtime.InteropServices;

namespace Tutorial07
{
    [StructLayout(LayoutKind.Sequential)]
    struct ConstantBufferChangesEveryFrameData
    {
        public XMFloat4X4 World;

        public XMFloat4 MeshColor;

        public static readonly uint Size = (uint)Marshal.SizeOf(typeof(ConstantBufferChangesEveryFrameData));
    }
}
