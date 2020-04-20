using JeremyAnsel.DirectX.DXMath;
using System.Runtime.InteropServices;

namespace Tutorial07
{
    [StructLayout(LayoutKind.Sequential)]
    struct ConstantBufferChangesOnResizeData
    {
        public XMFloat4X4 Projection;

        public static readonly uint Size = (uint)Marshal.SizeOf(typeof(ConstantBufferChangesOnResizeData));
    }
}
