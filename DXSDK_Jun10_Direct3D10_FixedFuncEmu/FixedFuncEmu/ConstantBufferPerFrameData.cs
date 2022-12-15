using JeremyAnsel.DirectX.DXMath;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FixedFuncEmu
{
    [StructLayout(LayoutKind.Sequential)]
    struct ConstantBufferPerFrameData
    {
        public ConstantBufferPerFrameData()
        {
        }

        // cbLights

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public XMVector[] g_clipplanes = new XMVector[3];

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public SceneLight[] g_lights = new SceneLight[8];

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
