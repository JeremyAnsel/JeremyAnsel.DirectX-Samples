using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FluidCS11
{
    [StructLayout(LayoutKind.Sequential)]
    struct ParticleDensity
    {
        public float Density;

        public static readonly uint Size = (uint)Marshal.SizeOf(typeof(ParticleDensity));
    }
}
