using JeremyAnsel.DirectX.DXMath;
using System.Runtime.InteropServices;

namespace HDRToneMappingCS11
{
    [StructLayout(LayoutKind.Sequential)]
    struct SkyboxVertex
    {
        public XMVector pos;

        public static readonly uint Size = (uint)Marshal.SizeOf(typeof(SkyboxVertex));
    }
}
