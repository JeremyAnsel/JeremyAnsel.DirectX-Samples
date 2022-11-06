using JeremyAnsel.DirectX.DXMath;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FluidCS11
{
    [StructLayout(LayoutKind.Sequential)]
    struct SimulationConstantBuffer
    {
        public uint NumParticles;

        private XMUInt3 padding;

        public float TimeStep;

        public float Smoothlen;

        public float PressureStiffness;

        public float RestDensity;

        public float DensityCoef;

        public float GradPressureCoef;

        public float LapViscosityCoef;

        public float WallStiffness;

        public XMFloat2 Gravity;

        private XMFloat2 padding2;

        public XMFloat4 GridDim;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public XMFloat4[] Planes;

        public static readonly uint Size = (uint)Marshal.SizeOf(typeof(SimulationConstantBuffer));
    }
}
