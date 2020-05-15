using JeremyAnsel.DirectX.DXMath;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace ContactHardeningShadows11
{
    [StructLayout(LayoutKind.Sequential)]
    struct ConstantBufferConstants
    {
        // World * View * Projection matrix
        public XMFloat4X4 WorldViewProjection;

        // World * ViewLight * Projection Light matrix
        public XMFloat4X4 WorldViewProjLight;

        // shadow map dimensions
        public XMFloat4 ShadowMapDimensions;

        // light direction
        public XMFloat4 LightDir;

        public float SunWidth;

        private XMFloat3 Padding;

        public static readonly uint Size = (uint)Marshal.SizeOf(typeof(ConstantBufferConstants));
    }
}
