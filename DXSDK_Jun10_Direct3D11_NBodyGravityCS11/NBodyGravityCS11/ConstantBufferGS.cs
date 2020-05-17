using JeremyAnsel.DirectX.DXMath;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace NBodyGravityCS11
{
    [StructLayout(LayoutKind.Sequential)]
    struct ConstantBufferGS
    {
        public XMFloat4X4 m_WorldViewProj;

        public XMFloat4X4 m_InvView;

        public static readonly uint Size = (uint)Marshal.SizeOf(typeof(ConstantBufferGS));
    }
}
