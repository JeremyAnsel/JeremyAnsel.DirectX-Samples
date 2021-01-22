using JeremyAnsel.DirectX.DXMath;
using System.Runtime.InteropServices;

namespace BasicHLSL11
{
    [StructLayout(LayoutKind.Sequential)]
    struct ConstantBufferVSPerObject
    {
        public XMFloat4X4 m_WorldViewProj;

        public XMFloat4X4 m_World;

        public static readonly uint Size = (uint)Marshal.SizeOf(typeof(ConstantBufferVSPerObject));
    }
}
