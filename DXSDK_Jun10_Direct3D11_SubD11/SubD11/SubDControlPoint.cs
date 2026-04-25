using JeremyAnsel.DirectX.DXMath;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SubD11
{
    unsafe struct SubDControlPoint
    {
        public XMFloat3 m_vPosition;

        public fixed byte m_Weights[4];

        public fixed byte m_Bones[4];

        // Normal is not used for patch computation.
        public XMFloat3 m_vNormal;

        public XMFloat2 m_vUV;

        public XMFloat3 m_vTanU;

        public static readonly uint Size = (uint)Marshal.SizeOf(typeof(SubDControlPoint));
    }
}
