using System.Runtime.InteropServices;

namespace HDRToneMappingCS11
{
    /// <summary>
    /// Constant buffer layout for transferring data to the CS
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    struct CSConstantBufferData
    {
        public uint param0;

        public uint param1;

        public uint param2;

        public uint param3;

        public static readonly uint Size = (uint)Marshal.SizeOf(typeof(CSConstantBufferData));
    }
}
