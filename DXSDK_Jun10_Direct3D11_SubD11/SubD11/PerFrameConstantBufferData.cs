using JeremyAnsel.DirectX.DXMath;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SubD11
{
    [StructLayout(LayoutKind.Sequential)]
    struct PerFrameConstantBufferData
    {
        public XMMatrix mViewProjection;

        public XMVector vCameraPosWorld;

        public XMVector vSolidColor;

        public float fTessellationFactor;

        public float fDisplacementHeight;

        private XMFloat2 padding;

        public static readonly uint Size = (uint)Marshal.SizeOf(typeof(PerFrameConstantBufferData));
    }
}
