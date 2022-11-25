using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SubD11
{
    [StructLayout(LayoutKind.Sequential)]
    struct TangentStencilConstantBufferData
    {
        /// <summary>
        /// Tangent patch stencils precomputed by the application
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.MaxValence * 64 * 4)]
        public float[] TanM;

        /// <summary>
        /// Valence coefficients precomputed by the application
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.MaxValence * 4)]
        public float[] fCi;

        public static readonly uint Size = (uint)Marshal.SizeOf(typeof(TangentStencilConstantBufferData));
    }
}
