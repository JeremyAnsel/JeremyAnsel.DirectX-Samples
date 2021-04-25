using JeremyAnsel.DirectX.DXMath;
using System.Runtime.InteropServices;

namespace SimpleSample11
{
    [StructLayout(LayoutKind.Sequential)]
    struct PerObjectConstantBuffer
    {
        public XMMatrix WorldViewProjection;

        public XMMatrix World;

        public XMFloat4 MaterialAmbientColor;

        public XMFloat4 MaterialDiffuseColor;

        public static readonly uint Size = (uint)Marshal.SizeOf(typeof(PerObjectConstantBuffer));
    }
}
