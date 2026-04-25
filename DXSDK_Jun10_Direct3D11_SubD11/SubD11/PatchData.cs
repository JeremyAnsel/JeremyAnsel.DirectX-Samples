using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SubD11
{
    unsafe struct PatchData
    {
        public fixed byte val[4];

        public fixed byte pre[4];

        public static readonly uint Size = (uint)Marshal.SizeOf(typeof(PatchData));
    }
}
