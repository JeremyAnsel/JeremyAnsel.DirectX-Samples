using JeremyAnsel.DirectX.DXMath;
using System.Runtime.InteropServices;

namespace DeferredParticles
{
    [StructLayout(LayoutKind.Sequential)]
    struct Particle
    {
        public XMFloat3 vPos;

        public XMFloat3 vDir;

        public XMFloat3 vMass;

        public uint Color;

        public float Radius;

        public float Life;

        public float Fade;

        public float Rot;

        public float RotRate;

        public bool Visible;

        public static readonly uint Size = (uint)Marshal.SizeOf(typeof(Particle));
    }
}
