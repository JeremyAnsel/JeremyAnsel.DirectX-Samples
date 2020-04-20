using JeremyAnsel.DirectX.DXMath;
using System.Runtime.InteropServices;

namespace Tutorial06
{
    [StructLayout(LayoutKind.Sequential)]
    struct ConstantBufferData
    {
        public XMFloat4X4 World;

        public XMFloat4X4 View;

        public XMFloat4X4 Projection;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public XMFloat4[] LightDir;

        //public XMFloat4 LightDir0;

        //public XMFloat4 LightDir1;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public XMFloat4[] LightColor;

        //public XMFloat4 LightColor0;

        //public XMFloat4 LightColor1;

        public XMFloat4 OutputColor;

        public static readonly uint Size = (uint)Marshal.SizeOf(typeof(ConstantBufferData));
    }
}
