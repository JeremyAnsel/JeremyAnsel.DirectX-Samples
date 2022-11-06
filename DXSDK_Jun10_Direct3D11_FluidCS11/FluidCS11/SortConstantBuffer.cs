using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FluidCS11
{
    [StructLayout(LayoutKind.Sequential)]
    struct SortConstantBuffer
    {
        public uint Level;

        public uint LevelMask;

        public uint Width;

        public uint Height;

        public static readonly uint Size = (uint)Marshal.SizeOf(typeof(SortConstantBuffer));
    }
}
