using JeremyAnsel.DirectX.DXMath;
using System.Runtime.InteropServices;

namespace Tutorial07
{
    [StructLayout(LayoutKind.Sequential)]
    struct ConstantBufferNeverChangesData
    {
        public XMFloat4X4 View;

        public static readonly uint Size = (uint)Marshal.SizeOf(typeof(ConstantBufferNeverChangesData));
    }
}
