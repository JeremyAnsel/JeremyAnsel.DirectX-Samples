using JeremyAnsel.DirectX.DXMath;
using System.Runtime.InteropServices;

namespace ShadowVolume10
{
    [StructLayout(LayoutKind.Sequential)]
    struct ConstantBufferData
    {
        public XMMatrix mWorldViewProjection;

        public XMMatrix mViewProjection;

        public XMMatrix mWorld;

        public XMVector vLightPosition;

        public XMVector vLightColor;

        public XMVector vAmbient;

        public XMVector vShadowColor;

        public float fExtrudeAmt;

        public float fExtrudeBias;

        private XMFloat2 padding;

        public static readonly uint Size = (uint)Marshal.SizeOf(typeof(ConstantBufferData));
    }
}
