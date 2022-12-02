using JeremyAnsel.DirectX.DXMath;
using System.Runtime.InteropServices;

namespace HDRToneMappingCS11
{
    /// <summary>
    /// Constant buffer layout for transferring data to the PS for bloom effect
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    struct BloomPSConstantBufferData
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 15)]
        public XMVector[] avSampleOffsets;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 15)]
        public XMVector[] avSampleWeights;

        public static readonly uint Size = (uint)Marshal.SizeOf(typeof(BloomPSConstantBufferData));
    }
}
