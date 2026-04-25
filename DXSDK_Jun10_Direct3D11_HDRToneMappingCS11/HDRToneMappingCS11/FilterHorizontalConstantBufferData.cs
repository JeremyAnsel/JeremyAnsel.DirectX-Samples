using JeremyAnsel.DirectX.DXMath;
using System.Runtime.InteropServices;

namespace HDRToneMappingCS11
{
    /// <summary>
    /// Constant buffer layout for transferring data to the CS for horizontal convolution
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    unsafe struct FilterHorizontalConstantBufferData
    {
        public fixed float avSampleWeights[4 * 15];

        public uint outputwidth;

        public float finverse;

        public XMUInt2 inputsize;

        public static readonly uint Size = (uint)Marshal.SizeOf(typeof(FilterHorizontalConstantBufferData));
    }
}
