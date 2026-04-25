using JeremyAnsel.DirectX.DXMath;
using System.Runtime.InteropServices;

namespace HDRToneMappingCS11
{
    /// <summary>
    /// Constant buffer layout for transferring data to the PS for bloom effect
    /// </summary>
    unsafe struct BloomPSConstantBufferData
    {
        public fixed float avSampleOffsets[4 * 15];

        public fixed float avSampleWeights[4 * 15];

        public static readonly uint Size = (uint)Marshal.SizeOf(typeof(BloomPSConstantBufferData));
    }
}
