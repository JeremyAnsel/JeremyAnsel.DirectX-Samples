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
    struct SubDControlPoint
    {
        public XMFloat3 m_vPosition;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] m_Weights;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] m_Bones;

        // Normal is not used for patch computation.
        public XMFloat3 m_vNormal;

        public XMFloat2 m_vUV;

        public XMFloat3 m_vTanU;

        public static readonly uint Size = (uint)Marshal.SizeOf(typeof(SubDControlPoint));
    }
}
