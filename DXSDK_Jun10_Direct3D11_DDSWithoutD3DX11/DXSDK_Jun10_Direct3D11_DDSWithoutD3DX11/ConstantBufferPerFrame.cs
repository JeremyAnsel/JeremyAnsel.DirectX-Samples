﻿using JeremyAnsel.DirectX.DXMath;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace DXSDK_Jun10_Direct3D11_DDSWithoutD3DX11
{
    [StructLayout(LayoutKind.Sequential)]
    struct ConstantBufferPerFrame
    {
        public XMFloat4 m_vLightDir;

        public static readonly uint Size = (uint)Marshal.SizeOf(typeof(ConstantBufferPerFrame));
    }
}
