using JeremyAnsel.DirectX.DXMath;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace NBodyGravityCS11
{
    [StructLayout(LayoutKind.Sequential)]
    struct ParticleVertex
    {
        public XMFloat4 Color;

        public static readonly uint Size = (uint)Marshal.SizeOf(typeof(ParticleVertex));
    }
}
