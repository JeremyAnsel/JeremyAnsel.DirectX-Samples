using JeremyAnsel.DirectX.DXMath;
using System.Runtime.InteropServices;

namespace DeferredParticles
{
    [StructLayout(LayoutKind.Sequential)]
    struct ParticleVertex
    {
        public XMFloat3 vPos;

        public XMFloat2 vUV;

        public float Life;

        public float Rot;

        public uint Color;

        public static readonly uint Size = (uint)Marshal.SizeOf(typeof(ParticleVertex));
    }
}
