using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SubD11
{
    [StructLayout(LayoutKind.Sequential)]
    struct PerSubsetConstantBufferData
    {
        public int m_iPatchStartIndex;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        private int[] m_Padding;

        public static readonly uint Size = (uint)Marshal.SizeOf(typeof(PerSubsetConstantBufferData));
    }
}
