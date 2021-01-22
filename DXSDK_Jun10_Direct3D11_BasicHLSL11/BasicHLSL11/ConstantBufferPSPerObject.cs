using JeremyAnsel.DirectX.DXMath;
using System.Runtime.InteropServices;

namespace BasicHLSL11
{
    [StructLayout(LayoutKind.Sequential)]
    struct ConstantBufferPSPerObject
    {
        public XMFloat4 m_vObjectColor;

        public static readonly uint Size = (uint)Marshal.SizeOf(typeof(ConstantBufferPSPerObject));
    }
}
