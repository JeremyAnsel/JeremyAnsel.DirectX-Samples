using JeremyAnsel.DirectX.DXMath;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace NBodyGravityCS11
{
    [StructLayout(LayoutKind.Sequential)]
    struct Particle
    {
        public XMFloat4 pos;

        public XMFloat4 velo;

        public static readonly uint Size = (uint)Marshal.SizeOf(typeof(Particle));
    }
}
