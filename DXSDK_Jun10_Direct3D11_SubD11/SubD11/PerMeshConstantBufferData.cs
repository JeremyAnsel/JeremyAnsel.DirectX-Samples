using JeremyAnsel.DirectX.DXMath;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SubD11
{
    unsafe struct PerMeshConstantBufferData
    {
        public fixed float mConstBoneWorld[16 * Constants.MaxBoneMatrices];

        public static readonly uint Size = (uint)Marshal.SizeOf(typeof(PerMeshConstantBufferData));
    }
}
