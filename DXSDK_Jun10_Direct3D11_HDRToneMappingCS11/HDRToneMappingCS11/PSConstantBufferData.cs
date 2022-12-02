using System.Runtime.InteropServices;

namespace HDRToneMappingCS11
{
    /// <summary>
    /// Constant buffer layout for transferring data to the PS
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    struct PSConstantBufferData
    {
        public float param0;

        public float param1;

        public float param2;

        public float param3;

        public static readonly uint Size = (uint)Marshal.SizeOf(typeof(PSConstantBufferData));
    }
}
