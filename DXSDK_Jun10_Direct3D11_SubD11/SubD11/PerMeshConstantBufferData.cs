using JeremyAnsel.DirectX.DXMath;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SubD11
{
    [StructLayout(LayoutKind.Sequential)]
    struct PerMeshConstantBufferData
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.MaxBoneMatrices)]
        public XMMatrix[] mConstBoneWorld;

        public static readonly uint Size = (uint)Marshal.SizeOf(typeof(PerMeshConstantBufferData));
    }
}
