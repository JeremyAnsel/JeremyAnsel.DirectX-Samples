using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace OIT11
{
    [StructLayout(LayoutKind.Sequential)]
    struct OitComputeShaderConstantBufferData
    {
        public uint FrameWidth;

        public uint FrameHeight;

        public uint PassSize;

        public uint Reserved;

        public static readonly uint Size = (uint)Marshal.SizeOf(typeof(OitComputeShaderConstantBufferData));
    }
}
