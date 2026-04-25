using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SubD11
{
    [StructLayout(LayoutKind.Sequential)]
    unsafe struct TangentStencilConstantBufferData
    {
        /// <summary>
        /// Tangent patch stencils precomputed by the application
        /// </summary>
        public fixed float TanM[Constants.MaxValence * 64 * 4];

        /// <summary>
        /// Valence coefficients precomputed by the application
        /// </summary>
        public fixed float fCi[Constants.MaxValence * 4];

        public static readonly uint Size = (uint)Marshal.SizeOf(typeof(TangentStencilConstantBufferData));
    }
}
