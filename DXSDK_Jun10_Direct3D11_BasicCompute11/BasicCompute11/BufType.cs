// If defined, then the hardware/driver must report support for double-precision CS 5.0 shaders or the sample fails to run
//#define TEST_DOUBLE

using System.Runtime.InteropServices;

namespace BasicCompute11
{
    [StructLayout(LayoutKind.Sequential)]
    struct BufType
    {
        public int i;

        public float f;

#if TEST_DOUBLE
        public double d;
#endif

        public static readonly uint Size = (uint)Marshal.SizeOf<BufType>();
    }
}
