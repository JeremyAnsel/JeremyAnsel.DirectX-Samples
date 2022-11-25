using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SubD11
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct PatchData
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] val;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] pre;

        public static readonly uint Size = (uint)Marshal.SizeOf(typeof(PatchData));
    }
}
