using JeremyAnsel.DirectX.DXMath;
using System.Runtime.InteropServices;

namespace Collision
{
    [StructLayout(LayoutKind.Sequential)]
    struct ConstantBufferData
    {
        public XMFloat4X4 WorldViewProjection;

        public XMFloat4 Color;

        public static readonly uint Size = (uint)Marshal.SizeOf(typeof(ConstantBufferData));
    }
}
