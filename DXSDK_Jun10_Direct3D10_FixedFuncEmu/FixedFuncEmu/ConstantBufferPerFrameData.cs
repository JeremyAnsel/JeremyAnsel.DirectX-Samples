using JeremyAnsel.DirectX.DXMath;
using System.Runtime.InteropServices;

namespace FixedFuncEmu
{
    unsafe struct ConstantBufferPerFrameData
    {
        public ConstantBufferPerFrameData()
        {
        }

        // cbLights

        public fixed float g_clipplanes[4 * 3];

        public fixed float g_lights[4 * 5 * 8];

        // cbPerFrame

        public XMMatrix g_mWorld = XMMatrix.Identity;

        public XMMatrix g_mView = XMMatrix.Identity;

        public XMMatrix g_mProj = XMMatrix.Identity;

        public XMMatrix g_mInvProj = XMMatrix.Identity;

        public XMMatrix g_mLightViewProj = XMMatrix.Identity;

        // cbPerTechnique

        public bool g_bEnableLighting = true;

        public bool g_bEnableClipping = true;

        public bool g_bPointScaleEnable = false;

        private bool padding1 = false;

        public float g_pointScaleA = 0.0f;

        public float g_pointScaleB = 0.0f;

        public float g_pointScaleC = 0.0f;

        public float g_pointSize = 0.0f;

        // fog params

        public int g_fogMode = (int)FogMode.None;

        public float g_fogStart = 0.0f;

        public float g_fogEnd = 0.0f;

        public float g_fogDensity = 0.0f;

        public XMVector g_fogColor = XMVector.Zero;

        public static readonly uint Size = (uint)Marshal.SizeOf(typeof(ConstantBufferPerFrameData));
    }
}
