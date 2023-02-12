using JeremyAnsel.DirectX.DXMath;
using System.Runtime.InteropServices;

namespace DeferredParticles
{
    [StructLayout(LayoutKind.Sequential)]
    struct GlowLightsConstantBuffer
    {
        public uint g_NumGlowLights;

        private XMUInt3 unused0;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = MainGameComponent.MaxFlashLights)]
        public XMVector[] g_vGlowLightPosIntensity;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = MainGameComponent.MaxFlashLights)]
        public XMVector[] g_vGlowLightColor;

        public XMVector g_vGlowLightAttenuation;

        public XMVector g_vMeshLightAttenuation;

        public static readonly uint Size = (uint)Marshal.SizeOf(typeof(GlowLightsConstantBuffer));
    }
}
