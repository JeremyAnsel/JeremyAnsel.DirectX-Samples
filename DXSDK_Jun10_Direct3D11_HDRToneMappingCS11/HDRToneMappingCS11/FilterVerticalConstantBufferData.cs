using JeremyAnsel.DirectX.DXMath;
using System.Runtime.InteropServices;

namespace HDRToneMappingCS11
{
    /// <summary>
    /// Constant buffer layout for transferring data to the CS for vertical convolution
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    struct FilterVerticalConstantBufferData
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 15)]
        public XMVector[] avSampleWeights;

        public XMUInt2 outputsize;

        public XMUInt2 inputsize;

        public static readonly uint Size = (uint)Marshal.SizeOf(typeof(FilterVerticalConstantBufferData));
    }
}
