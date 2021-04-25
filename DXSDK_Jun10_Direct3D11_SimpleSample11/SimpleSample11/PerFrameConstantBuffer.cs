using JeremyAnsel.DirectX.DXMath;
using System.Runtime.InteropServices;

namespace SimpleSample11
{
    [StructLayout(LayoutKind.Sequential)]
    struct PerFrameConstantBuffer
    {
        public XMFloat3 LightDir;

        public float Time;

        public XMFloat4 LightDiffuse;

        public static readonly uint Size = (uint)Marshal.SizeOf(typeof(PerFrameConstantBuffer));
    }
}
