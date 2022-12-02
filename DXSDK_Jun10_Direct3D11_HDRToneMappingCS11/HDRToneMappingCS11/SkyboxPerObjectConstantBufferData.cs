using JeremyAnsel.DirectX.DXMath;
using System.Runtime.InteropServices;

namespace HDRToneMappingCS11
{
    [StructLayout(LayoutKind.Sequential)]
    struct SkyboxPerObjectConstantBufferData
    {
        public XMMatrix m_WorldViewProj;

        public static readonly uint Size = (uint)Marshal.SizeOf(typeof(SkyboxPerObjectConstantBufferData));
    }
}
