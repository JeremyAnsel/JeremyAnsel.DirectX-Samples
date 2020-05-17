using JeremyAnsel.DirectX.DXMath;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace NBodyGravityCS11
{
    [StructLayout(LayoutKind.Sequential)]
    struct ConstantBufferCS
    {
        public XMUInt4 param;

        public XMFloat4 paramf;

        public static readonly uint Size = (uint)Marshal.SizeOf(typeof(ConstantBufferCS));
    }
}
