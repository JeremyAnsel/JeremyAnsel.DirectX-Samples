using JeremyAnsel.DirectX.DXMath;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace AdaptiveTessellationCS40
{
    [StructLayout(LayoutKind.Sequential)]
    struct EdgeFactorConstantBuffer
    {
        public XMFloat4X4 MatWVP;

        public XMFloat2 TessEdgeLengthScale;

        public uint NumTriangles;

        public float Dummy;

        public static readonly uint Size = (uint)Marshal.SizeOf(typeof(EdgeFactorConstantBuffer));
    }
}
