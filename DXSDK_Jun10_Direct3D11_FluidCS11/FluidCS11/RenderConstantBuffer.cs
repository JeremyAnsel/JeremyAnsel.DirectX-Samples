using JeremyAnsel.DirectX.DXMath;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FluidCS11
{
    [StructLayout(LayoutKind.Sequential)]
    struct RenderConstantBuffer
    {
        public XMFloat4X4 ViewProjection;

        public float ParticleSize;

        private XMFloat3 padding;

        public static readonly uint Size = (uint)Marshal.SizeOf(typeof(RenderConstantBuffer));
    }
}
