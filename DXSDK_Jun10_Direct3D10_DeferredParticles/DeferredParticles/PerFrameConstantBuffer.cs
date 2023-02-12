using JeremyAnsel.DirectX.DXMath;
using System.Runtime.InteropServices;

namespace DeferredParticles
{
    [StructLayout(LayoutKind.Sequential)]
    struct PerFrameConstantBuffer
    {
        public float g_fTime;

        private XMFloat3 unused0;

        public XMVector g_LightDir;

        public XMVector g_vEyePt;

        public XMVector g_vRight;

        public XMVector g_vUp;

        public XMVector g_vForward;

        public XMMatrix g_mWorldViewProjection;

        public XMMatrix g_mViewProj;

        public XMMatrix g_mInvViewProj;

        public XMMatrix g_mWorld;

        public static readonly uint Size = (uint)Marshal.SizeOf(typeof(PerFrameConstantBuffer));
    }
}
