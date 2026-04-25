using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SubD11
{
    struct PerSubsetConstantBufferData
    {
        public int m_iPatchStartIndex;

        private int m_Padding1;
        private int m_Padding2;
        private int m_Padding3;

        public static readonly uint Size = (uint)Marshal.SizeOf(typeof(PerSubsetConstantBufferData));
    }
}
