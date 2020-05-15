using JeremyAnsel.DirectX.DXMath;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace DDSWithoutD3DX11
{
    [StructLayout(LayoutKind.Sequential)]
    struct ConstantBufferPerObject
    {
        public XMFloat4X4 m_mWorldViewProjection;

        public XMFloat4X4 m_mWorld;

        public static readonly uint Size = (uint)Marshal.SizeOf(typeof(ConstantBufferPerObject));
    }
}
