using JeremyAnsel.DirectX.DXMath;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace SimpleBezier11
{
    [StructLayout(LayoutKind.Sequential)]
    struct ConstantBufferConstants
    {
        public XMFloat4X4 ViewProjection;

        public XMFloat3 CameraPosWorld;

        public float TessellationFactor;

        public static readonly uint Size = (uint)Marshal.SizeOf(typeof(ConstantBufferConstants));
    }
}
