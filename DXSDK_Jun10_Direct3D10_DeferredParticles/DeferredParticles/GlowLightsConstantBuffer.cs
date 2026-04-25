using JeremyAnsel.DirectX.DXMath;
using System.Runtime.InteropServices;

namespace DeferredParticles
{
    unsafe struct GlowLightsConstantBuffer
    {
        public uint g_NumGlowLights;

        private XMUInt3 unused0;

        public struct GlowLightPosIntensityBuffer
        {
            public fixed byte Buffer[TotalSize];
            public const int Length = MainGameComponent.MaxFlashLights;
            public const int TotalSize = sizeof(float) * 4 * Length;
        }

        public GlowLightPosIntensityBuffer g_vGlowLightPosIntensity;

        public struct GlowLightColorBuffer
        {
            public fixed byte Buffer[TotalSize];
            public const int Length = MainGameComponent.MaxFlashLights;
            public const int TotalSize = sizeof(float) * 4 * Length;
        }

        public GlowLightColorBuffer g_vGlowLightColor;

        public XMVector g_vGlowLightAttenuation;

        public XMVector g_vMeshLightAttenuation;
    }
}
