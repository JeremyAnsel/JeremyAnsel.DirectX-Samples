using JeremyAnsel.DirectX.DXMath;
using System.Runtime.InteropServices;

namespace BasicHLSL11
{
    [StructLayout(LayoutKind.Sequential)]
    struct ConstantBufferPSPerFrame
    {
        public XMFloat4 m_vLightDirAmbient;

        public static readonly uint Size = (uint)Marshal.SizeOf(typeof(ConstantBufferPSPerFrame));
    }
}
