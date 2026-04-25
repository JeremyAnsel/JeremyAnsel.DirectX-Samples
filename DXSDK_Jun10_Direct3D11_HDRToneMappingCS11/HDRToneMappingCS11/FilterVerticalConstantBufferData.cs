using JeremyAnsel.DirectX.DXMath;
using System.Runtime.InteropServices;

namespace HDRToneMappingCS11
{
    /// <summary>
    /// Constant buffer layout for transferring data to the CS for vertical convolution
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    unsafe struct FilterVerticalConstantBufferData
    {
        public fixed float avSampleWeights[4 * 15];

        public XMUInt2 outputsize;

        public XMUInt2 inputsize;

        public static readonly uint Size = (uint)Marshal.SizeOf(typeof(FilterVerticalConstantBufferData));
    }
}
